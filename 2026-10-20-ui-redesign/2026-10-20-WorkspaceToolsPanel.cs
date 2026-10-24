using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FotoAutomat.Infrastructure;

namespace FotoAutomat.Desktop;

/// <summary>UI for drafts and local diagnostics; never operates physical devices.</summary>
public sealed class WorkspaceToolsPanel : UserControl
{
    private readonly StackPanel body = new() { Margin = new Thickness(24, 24, 24, 20) };
    private readonly CreativeDraftStore store = new();
    private readonly TextBlock feedback = new() { Margin = new Thickness(0, 12, 0, 0), TextWrapping = TextWrapping.Wrap };
    private readonly List<string> events = [];
    private readonly ScrollViewer scroller;
    private CreativeDraft draft = new();
    public WorkspaceToolsPanel()
    {
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/FotoAutomat.Desktop;component/TouchControls.xaml", UriKind.Relative) });
        Background = Brushes.White;
        Loaded += (_, _) => { if (TryFindResource("PageBrush") is Brush page) Background = page; };
        Content = scroller = new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        try { draft = store.Load(); }
        catch (Exception e) { events.Add($"{DateTime.Now:HH:mm:ss} · Không đọc được cấu hình: {e.Message}"); }
    }
    public void Record(string message)
    {
        events.Insert(0, $"{DateTime.Now:HH:mm:ss} · {message}");
        if (events.Count > 200) events.RemoveAt(events.Count - 1);
    }
    private void Text(string value, double size = 15) => body.Children.Add(new TextBlock { Text = value, FontSize = size, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 18) });
    private void Button(string label, Action action)
    {
        var button = new Button { Content = label, MinHeight = 48, Padding = new Thickness(18, 8, 18, 8), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 12, 0, 12) };
        button.Click += (_, _) => action(); body.Children.Add(button);
    }
    private void Save()
    {
        try { store.Save(draft); feedback.Text = "Đã lưu bản nháp"; Record("Đã lưu bản nháp máy ảnh và bộ màu."); }
        catch (Exception e) { Record(e.Message); new WarningWindow(Window.GetWindow(this), e.Message).ShowDialog(); }
    }
    public void ShowSection(string section)
    {
        body.Children.Clear(); feedback.Text = ""; Content = scroller; (feedback.Parent as Panel)?.Children.Remove(feedback); Text(section, 26);
        if (section == "Máy ảnh")
        {
            if (App.UiPreview)
            {
                var vm = new FotoAutomat.Admin.Camera.CameraPageViewModel(new FotoAutomat.CameraUiPreview.PreviewCamera(), new FotoAutomat.CameraUiPreview.PreviewStore());
                vm.GuideRequested += () => MessageBox.Show(Window.GetWindow(this), "Camera và cấu hình trên trang này dùng dữ liệu mô phỏng.", "UI Preview");
                Content = new FotoAutomat.Admin.Camera.CameraPage { DataContext = vm };
            }
            else Content = new CameraSettingsPanel();
        }
        else if (section == "Bộ lọc màu")
        {
            // The booth default is chosen next to the preset in the Adjust editor (★ Mặc định); no second selector here.
            var tabs = new TabControl();
            var adjust = new ColorEditorPanel();
            tabs.Items.Add(new TabItem { Header = "Adjust", Content = adjust });
            var retouch = new RetouchEditorPanel();
            var refine = new RefineEditorPanel();
            tabs.Items.Add(new TabItem { Header = "Retouch", Content = retouch });
            tabs.Items.Add(new TabItem { Header = "Refine", Content = refine });
            foreach (TabItem tab in tabs.Items) { tab.MinHeight = 40; tab.Padding = new Thickness(16, 8, 16, 8); }
            // The editors fill the window height (responsive) instead of scrolling inside a fixed-height page.
            body.Children.Clear();
            var page = new Grid { Margin = new Thickness(24, 24, 24, 12) }; page.RowDefinitions.Add(new() { Height = GridLength.Auto }); page.RowDefinitions.Add(new() { Height = new GridLength(1, GridUnitType.Star) }); page.RowDefinitions.Add(new() { Height = GridLength.Auto });
            page.Children.Add(new TextBlock { Text = section, FontSize = 24, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 12) });
            Grid.SetRow(tabs, 1); page.Children.Add(tabs); Grid.SetRow(feedback, 2); page.Children.Add(feedback);
            Content = page; // no outer ScrollViewer: it would give the editor infinite height; the window minimum (680 px) fits it
        }
        else if (section == "Nhật ký hệ thống")
        {
            Text("Hoạt động quản trị trong lần mở ứng dụng này");
            Button("Làm mới", () => ShowSection(section));
            var list = new ListBox { ItemsSource = events.ToArray(), MinHeight = 240, MaxHeight = 600 }; body.Children.Add(list);
            if (events.Count == 0) Text("Chưa có hoạt động");
        }
        else if (section == "Thông tin ứng dụng")
        {
            Text("FotoAutomat", 22);
            Text($"Phiên bản {typeof(App).Assembly.GetName().Version} · Windows / WPF / .NET 10");
            Text("Bản mô phỏng và tinh chỉnh giao diện");
            Text("Máy ảnh, AI, nhận tiền và in thực tế chưa tích hợp. Kích hoạt thương hiệu đang ở mức tài liệu kiến trúc.");
        }
    }
    private static void Slider(StackPanel panel, string label, double min, double max, double value, Action<double> change)
    {
        var text = new TextBlock { Text = $"{label} · {value:0.0}", FontSize = 16 };
        var slider = new Slider { Minimum = min, Maximum = max, Value = value, Height = 48, Margin = new Thickness(0, 8, 0, 20), TickFrequency = max == 3 ? 0.1 : 1, IsSnapToTickEnabled = true };
        slider.ValueChanged += (_, _) => { text.Text = $"{label} · {slider.Value:0.0}"; change(slider.Value); };
        System.Windows.Automation.AutomationProperties.SetName(slider, label);
        panel.Children.Add(text); panel.Children.Add(slider);
    }
}
