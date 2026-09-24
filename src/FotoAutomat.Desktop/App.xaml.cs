using System.Threading;
using System.Windows;

namespace FotoAutomat.Desktop;

public partial class App : System.Windows.Application
{
    private Mutex? instanceMutex;
    private bool ownsMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        instanceMutex = new Mutex(true, @"Local\FotoAutomat.Simulator", out ownsMutex);
        if (!ownsMutex)
        {
            MessageBox.Show("The FotoAutomat simulator is already running.", "FotoAutomat");
            Shutdown();
            return;
        }
        try
        {
            Window window = e.Args.Contains("--simulator") ? new SimulatorWindow() : new MainWindow();
            window.Show();
        }
        catch (Exception exception)
        {
            MessageBox.Show($"Unable to start the simulator: {exception.Message}", "FotoAutomat");
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (ownsMutex) instanceMutex?.ReleaseMutex();
        instanceMutex?.Dispose();
        base.OnExit(e);
    }
}
