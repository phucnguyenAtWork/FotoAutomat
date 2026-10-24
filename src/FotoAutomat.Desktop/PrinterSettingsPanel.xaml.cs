using System.Diagnostics;
using System.IO;
using System.Printing;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FotoAutomat.Core;
using FotoAutomat.Infrastructure;

namespace FotoAutomat.Desktop;

public partial class PrinterSettingsPanel : UserControl
{
    private PrinterConfiguration settings = new();
    private string[] installedQueues = [];
    private bool fillingQueues;

    public PrinterSettingsPanel()
    {
        InitializeComponent();
        try { settings = new PrinterConfigurationStore().Load(); }
        catch (Exception e) { Feedback.Text = e.Message; }
        SettingsContent.DataContext = settings; Pair.IsChecked = settings.PairAndCut; Copies.Value = settings.Copies;
        ScanQueues(); RefreshPrinters();
    }

    private PrinterProfile ProfileOf(object sender) => (sender as FrameworkElement)?.Tag as string == "secondary" ? settings.Secondary : settings.Main;

    private void ScanQueues()
    {
        try
        {
            using var server = new LocalPrintServer();
            installedQueues = [.. server.GetPrintQueues([EnumeratedPrintQueueTypes.Local, EnumeratedPrintQueueTypes.Connections]).Select(queue => queue.FullName).Order(StringComparer.CurrentCultureIgnoreCase)];
        }
        catch (Exception error) when (error is PrintSystemException or InvalidOperationException or UnauthorizedAccessException)
        {
            installedQueues = []; Feedback.Text = "Không đọc được danh sách máy in của Windows.";
        }
    }

    // Danh sách máy in đã cài; máy đã lưu nhưng không còn trong Windows vẫn giữ trong danh sách để không mất cấu hình.
    private void FillQueue(ComboBox box, PrinterProfile profile)
    {
        box.Items.Clear();
        box.Items.Add(new ComboBoxItem { Content = "Chưa gán", Tag = "" });
        foreach (string queue in installedQueues) box.Items.Add(new ComboBoxItem { Content = queue, Tag = queue });
        if (profile.QueueName.Length > 0 && !installedQueues.Contains(profile.QueueName))
            box.Items.Add(new ComboBoxItem { Content = profile.QueueName + " · không tìm thấy", Tag = profile.QueueName });
        box.SelectedItem = box.Items.Cast<ComboBoxItem>().First(item => (string)item.Tag == profile.QueueName);
    }

    private void RefreshPrinters()
    {
        fillingQueues = true;
        try { FillQueue(MainQueue, settings.Main); FillQueue(SecondaryQueue, settings.Secondary); }
        finally { fillingQueues = false; }
        MainName.Text = settings.Main.QueueName.Length > 0 ? settings.Main.QueueName : "Chưa gán";
        SecondaryName.Text = settings.Secondary.QueueName.Length > 0 ? settings.Secondary.QueueName : "Chưa gán";
        MainTicketSaved.Visibility = settings.Main.TicketXml.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        SecondaryTicketSaved.Visibility = settings.Secondary.TicketXml.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnQueueChanged(object sender, SelectionChangedEventArgs e)
    {
        if (fillingQueues || sender is not ComboBox { SelectedItem: ComboBoxItem { Tag: string queue } }) return;
        var profile = ProfileOf(sender);
        if (profile.QueueName == queue) return;
        // Thông số driver thuộc về một máy in cụ thể; đổi máy thì bỏ thông số cũ.
        profile.QueueName = queue; profile.TicketXml = "";
        RefreshPrinters(); RecomputeLater();
    }

    private void OnRescan(object sender, RoutedEventArgs e) { Feedback.Text = ""; ScanQueues(); RefreshPrinters(); }

    private void OnConfigure(object sender, RoutedEventArgs e)
    {
        var profile = ProfileOf(sender);
        try
        {
            var dialog = new PrintDialog();
            if (!string.IsNullOrWhiteSpace(profile.QueueName))
            {
                using var server = new LocalPrintServer();
                try { dialog.PrintQueue = new PrintQueue(server, profile.QueueName); }
                catch (PrintSystemException) { Feedback.Text = "Máy in đã lưu không khả dụng. Chọn lại máy in."; }
            }
            if (!string.IsNullOrWhiteSpace(profile.TicketXml))
            {
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(profile.TicketXml));
                dialog.PrintTicket = new PrintTicket(stream);
            }
            if (dialog.ShowDialog() != true) return;
            profile.QueueName = dialog.PrintQueue.FullName;
            using var ticket = dialog.PrintTicket.GetXmlStream();
            using var reader = new StreamReader(ticket);
            profile.TicketXml = reader.ReadToEnd();
            RefreshPrinters(); RecomputeLater(); Feedback.Text = "";
        }
        catch (Exception error) { Feedback.Text = "Không mở được thông số máy in: " + error.Message; }
    }

    private void OnWindowsPrinters(object sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("ms-settings:printers") { UseShellExecute = true }); }
        catch (Exception error) { Feedback.Text = error.Message; }
    }

    private void OnReset(object sender, RoutedEventArgs e)
    {
        var profile = ProfileOf(sender);
        profile.Scale = 100; profile.OffsetX = profile.OffsetY = 0;
    }

    private static bool HasErrors(DependencyObject node)
    {
        if (Validation.GetHasError(node)) return true;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) if (HasErrors(VisualTreeHelper.GetChild(node, i))) return true;
        return false;
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        try
        {
            if (HasErrors(this)) throw new ArgumentException("Kiểm tra lại các giá trị đã nhập.");
            var updated = settings with { PairAndCut = Pair.IsChecked == true, Copies = (int)Copies.Value };
            new PrinterConfigurationStore().Save(updated);
            settings = updated; SettingsContent.DataContext = settings;
            Feedback.Text = "Đã lưu cài đặt in.";
        }
        catch (Exception error) { Feedback.Text = error.Message; }
    }
}
