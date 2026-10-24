using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using FotoAutomat.Core;
using FotoAutomat.Infrastructure;

namespace FotoAutomat.Desktop;

// Trạng thái "chưa lưu" và nút Hoàn tác của thanh lưu. OnSave, OnConfigure, OnReset và PrinterConfigurationStore không thay đổi.
public partial class PrinterSettingsPanel
{
    private string savedSnapshot = "";
    private bool tracking;

    private static string Serialize(PrinterConfiguration value) => JsonSerializer.Serialize(value);

    private string Current()
    {
        _ = int.TryParse(Copies.Text, out int copies);
        return Serialize(settings with { PairAndCut = Pair.IsChecked == true, Copies = copies });
    }

    private void Recompute()
    {
        bool dirty = Current() != savedSnapshot;
        DirtyText.Text = dirty ? "Có thay đổi chưa lưu" : "Đã lưu";
        RevertButton.IsEnabled = dirty;
    }

    // Binding hai chiều cập nhật cấu hình sau khi sự kiện nổi lên, nên tính lại ở mức ưu tiên thấp hơn.
    private void RecomputeLater() => Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(Recompute));

    private void OnPanelLoaded(object sender, RoutedEventArgs e)
    {
        if (tracking) return;
        tracking = true;
        savedSnapshot = Serialize(settings);
        AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler((_, _) => RecomputeLater()));
        AddHandler(ToggleButton.CheckedEvent, new RoutedEventHandler((_, _) => RecomputeLater()));
        AddHandler(ToggleButton.UncheckedEvent, new RoutedEventHandler((_, _) => RecomputeLater()));
        AddHandler(RangeBase.ValueChangedEvent, new RoutedPropertyChangedEventHandler<double>((_, _) => RecomputeLater()));
        // Đăng ký sau khi XAML đã gắn OnSave nên chạy sau OnSave: đọc lại store để biết lưu có thành công không.
        SaveButton.Click += (_, _) =>
        {
            try { savedSnapshot = Serialize(new PrinterConfigurationStore().Load()); }
            catch (Exception ex) { Feedback.Text = ex.Message; }
            Recompute();
        };
        Recompute();
    }

    private void OnRevert(object sender, RoutedEventArgs e)
    {
        try
        {
            settings = new PrinterConfigurationStore().Load();
            SettingsContent.DataContext = settings; Pair.IsChecked = settings.PairAndCut; Copies.Text = settings.Copies.ToString();
            RefreshProfile(); savedSnapshot = Serialize(settings); Feedback.Text = "";
        }
        catch (Exception ex) { Feedback.Text = ex.Message; }
        Recompute();
    }
}
