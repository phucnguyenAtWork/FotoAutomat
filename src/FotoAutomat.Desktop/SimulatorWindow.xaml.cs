using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using FotoAutomat.Core;
using FotoAutomat.Infrastructure;

namespace FotoAutomat.Desktop;

public partial class SimulatorWindow : Window
{
    private readonly SqliteBoothStore store;
    private readonly BoothWorkflow workflow;
    private BoothSession? session;
    private bool busy;

    public SimulatorWindow()
    {
        InitializeComponent();
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FotoAutomat", "Simulator");
        store = new SqliteBoothStore(Path.Combine(directory, "booth.db"));
        var recovered = store.RecoverInterruptedSessions();
        workflow = new BoothWorkflow(store, new SimulatedCamera(Path.Combine(directory, "captures")), new SimulatedPrinter());
        session = store.GetLatest();
        DataLabel.Text = $"Simulator data: {directory}";
        MessageLabel.Text = recovered > 0
            ? "An interrupted operation needs review. Nothing was automatically printed again."
            : "Start a demo session to exercise the local workflow. Internet is not required.";
        Refresh();
    }

    private async void OnStart(object sender, RoutedEventArgs e) =>
        await Execute(() => Task.FromResult(workflow.Start()), "New simulator session created.");

    private async void OnPayment(object sender, RoutedEventArgs e)
    {
        if (session is null) return;
        var id = session.Id;
        await Execute(() => Task.FromResult(workflow.ConfirmDemoPayment(id, $"demo-{id}")),
            "Demo receipt recorded. This is not a real payment.");
    }

    private async void OnCapture(object sender, RoutedEventArgs e)
    {
        if (session is null) return;
        var id = session.Id;
        await Execute(() => workflow.CaptureAsync(id), "Synthetic image saved locally.");
    }

    private async void OnPrint(object sender, RoutedEventArgs e)
    {
        if (session is null) return;
        var id = session.Id;
        await Execute(() => workflow.PrintAsync(id), "Simulated print completed. No physical output was produced.");
    }

    private async Task Execute(Func<Task<BoothSession>> action, string successMessage)
    {
        if (busy) return;
        busy = true;
        Refresh();
        try
        {
            session = await Task.Run(action);
            MessageLabel.Text = successMessage;
        }
        catch (Exception exception)
        {
            MessageLabel.Text = exception.Message;
            if (session is not null)
            {
                try { session = store.Get(session.Id); }
                catch { MessageLabel.Text += " Unable to reload local state. Restart and inspect the simulator database."; }
            }
        }
        finally
        {
            busy = false;
            Refresh();
        }
    }

    private void Refresh()
    {
        SessionLabel.Text = session is null ? "No active demo session" : $"Session {session.Id}";
        StageLabel.Text = session?.Stage.ToString() ?? "Ready";
        StartButton.IsEnabled = !busy && (session is null || session.Stage is SessionStage.Completed or SessionStage.NeedsReview);
        PayButton.IsEnabled = !busy && session?.Stage == SessionStage.AwaitingPayment;
        CaptureButton.IsEnabled = !busy && session?.Stage == SessionStage.Paid;
        PrintButton.IsEnabled = !busy && session?.Stage == SessionStage.Captured;
        Preview.Source = null;
        if (session?.ImagePath is { } path && File.Exists(path))
        {
            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.DecodePixelWidth = 320;
                image.UriSource = new Uri(path, UriKind.Absolute);
                image.EndInit();
                image.Freeze();
                Preview.Source = image;
            }
            catch (Exception exception) when (exception is IOException or NotSupportedException or FileFormatException)
            {
                MessageLabel.Text = "Unable to read the simulator preview. Inspect the capture file before printing.";
                PrintButton.IsEnabled = false;
            }
        }
    }
}
