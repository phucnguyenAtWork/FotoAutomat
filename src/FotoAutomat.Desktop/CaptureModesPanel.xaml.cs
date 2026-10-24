using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FotoAutomat.Core;
using FotoAutomat.Infrastructure;
using Microsoft.Win32;

namespace FotoAutomat.Desktop;

/// <summary>
/// Cài đặt chế độ chụp, bản gọn: thẻ "Phiên chụp" (thông số chung), hàng chip chọn chế độ,
/// thiết lập của chế độ đang chọn bằng NumericStepper / danh sách phân đoạn. Dữ liệu và validation giữ nguyên.
/// </summary>
public partial class CaptureModesPanel : UserControl
{
    private readonly CaptureConfigurationStore store = new(CaptureConfigurationStore.DefaultPath);
    private CaptureConfiguration configuration = new();
    private string saved = "";
    // true trong lúc dựng giao diện (kể cả khi XAML đang khởi tạo) để các sự kiện không ghi đè cấu hình.
    private bool building = true;
    private int selected;
    private readonly HashSet<TextBox> invalid = [];
    private static readonly string[] titles = ["Ảnh chụp", "GIF", "Boomerang", "Video", "360° / Slow-motion", "Timelapse", "Lời chúc", "Ảnh thẻ"];
    private readonly System.Windows.Shapes.Ellipse[] dots = new System.Windows.Shapes.Ellipse[titles.Length];
    private Grid? section;
    private int cell;
    private CaptureModeOptions Mode => configuration.Modes[selected];
    private string Snapshot() => JsonSerializer.Serialize(configuration);

    public CaptureModesPanel()
    {
        InitializeComponent();
        try { configuration = store.Load(); } catch (Exception error) { Feedback.Text = error.Message; }
        saved = Snapshot();
        LoadSession();
        for (int i = 0; i < titles.Length; i++)
        {
            dots[i] = new System.Windows.Shapes.Ellipse { Width = 8, Height = 8, Margin = new(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center, Fill = Brushes.Transparent };
            var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.Children.Add(dots[i]);
            row.Children.Add(new TextBlock { Text = titles[i], TextWrapping = TextWrapping.NoWrap, VerticalAlignment = VerticalAlignment.Center });
            ModesList.Items.Add(new ListBoxItem { Content = row });
        }
        building = false;
        ModesList.SelectedIndex = 0;
    }

    private void LoadSession()
    {
        bool was = building;
        building = true;
        CountdownStepper.Value = configuration.CountdownSeconds;
        ReviewStepper.Value = configuration.ReviewSeconds;
        ShotsStepper.Value = configuration.Shots;
        RetakeStepper.Value = configuration.RetakeLimit;
        SessionStepper.Value = configuration.SessionMinutes;
        building = was;
    }

    private void OnSessionChanged(object? sender, EventArgs e)
    {
        if (building) return;
        configuration = configuration with
        {
            CountdownSeconds = (int)CountdownStepper.Value,
            ReviewSeconds = (int)ReviewStepper.Value,
            Shots = (int)ShotsStepper.Value,
            RetakeLimit = (int)RetakeStepper.Value,
            SessionMinutes = (int)SessionStepper.Value
        };
        Changed();
    }

    private void OnModeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (building || ModesList.SelectedIndex < 0) return;
        if (invalid.Count > 0) { building = true; ModesList.SelectedIndex = selected; building = false; Feedback.Text = "Kiểm tra ô nhập đang báo lỗi."; return; }
        selected = ModesList.SelectedIndex; Render();
    }

    private void Changed()
    {
        if (building) return;
        State.Text = invalid.Count > 0 || Snapshot() != saved ? "Có thay đổi chưa lưu" : "Đã lưu";
    }

    private void UpdateDots()
    {
        for (int i = 0; i < dots.Length && i < configuration.Modes.Count; i++)
            dots[i].Fill = (Brush)FindResource(configuration.Modes[i].Enabled ? "AccentBrush" : "LineStrongBrush");
    }

    // ----- Bố cục hai cột: Section() mở một lưới mới (có thể kèm tiêu đề), Place() đặt ô vào cột trái/phải -----
    private void Section(string? title)
    {
        if (title is not null)
            Fields.Children.Add(new TextBlock { Text = title, Style = (Style)FindResource("TextSectionTitle"), Margin = new(0, Fields.Children.Count == 0 ? 0 : 12, 0, 12) });
        section = new Grid();
        section.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
        section.ColumnDefinitions.Add(new() { Width = new GridLength(32) });
        section.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
        cell = 0;
        Fields.Children.Add(section);
    }

    private void Place(UIElement element, bool wide = false)
    {
        if (section is null) Section(null);
        Grid grid = section!;
        if (wide && cell % 2 == 1) cell++;
        int row = cell / 2;
        while (grid.RowDefinitions.Count <= row) grid.RowDefinitions.Add(new() { Height = GridLength.Auto });
        Grid.SetRow(element, row);
        Grid.SetColumn(element, wide ? 0 : cell % 2 * 2);
        if (wide) Grid.SetColumnSpan(element, 3);
        grid.Children.Add(element);
        cell += wide ? 2 : 1;
    }

    // Hàng "nhãn bên trái, control bên phải".
    private static Grid Row(string label, FrameworkElement control)
    {
        var row = new Grid { MinHeight = 44, Margin = new(0, 0, 0, 12) };
        row.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        row.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, Margin = new(0, 0, 12, 0) });
        control.VerticalAlignment = VerticalAlignment.Center;
        control.HorizontalAlignment = HorizontalAlignment.Right;
        Grid.SetColumn(control, 1);
        row.Children.Add(control);
        return row;
    }

    private void Number(string label, double value, double min, double max, Action<double> set, double step = 1, string unit = "")
    {
        var input = new NumericStepper { Minimum = min, Maximum = max, Step = step, Unit = unit, Value = value };
        System.Windows.Automation.AutomationProperties.SetName(input, label);
        input.ValueChanged += (_, _) => { set(input.Value); Changed(); };
        Place(Row(label, input));
    }

    private ListBox Segmented<T>(IEnumerable<T> values, T value, Action<T> set, string? format = null)
    {
        var input = new ListBox { Style = (Style)FindResource("SegmentedListStyle"), ItemsSource = values.ToArray() };
        if (format is not null) input.ItemStringFormat = format;
        input.SelectedItem = value;
        input.SelectionChanged += (_, _) => { if (input.SelectedItem is T chosen) { set(chosen); Changed(); } };
        return input;
    }

    private void Choice<T>(string label, IEnumerable<T> values, T value, Action<T> set, bool wide = false, string? format = null)
    {
        var input = Segmented(values, value, set, format);
        System.Windows.Automation.AutomationProperties.SetName(input, label);
        Place(Row(label, input), wide);
    }

    private void Toggle(string label, bool value, Action<bool> set)
    {
        var input = new CheckBox { Content = label, IsChecked = value, Margin = new(0, 0, 0, 12) };
        input.Click += (_, _) => { set(input.IsChecked == true); Changed(); };
        Place(input);
    }

    private void Asset(string label, string path, string filter, Action<string> set)
    {
        var row = new Grid { Margin = new(0, 0, 0, 12) };
        row.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        var choose = new Button { Content = string.IsNullOrEmpty(path) ? label : Path.GetFileName(path), HorizontalContentAlignment = HorizontalAlignment.Left };
        var clear = new Button { Content = "Bỏ", Margin = new(8, 0, 0, 0) };
        Grid.SetColumn(clear, 1);
        clear.Click += (_, _) => { set(""); choose.Content = label; Changed(); };
        choose.Click += (_, _) =>
        {
            var picker = new OpenFileDialog { Filter = filter };
            if (picker.ShowDialog(Window.GetWindow(this)) == true) { set(picker.FileName); choose.Content = Path.GetFileName(picker.FileName); Changed(); }
        };
        row.Children.Add(choose);
        row.Children.Add(clear);
        Place(row);
    }

    private void Render()
    {
        building = true; Fields.Children.Clear(); section = null; invalid.Clear(); ModeTitle.Text = titles[selected]; EnabledMode.IsChecked = Mode.Enabled;
        if (Mode.Id is "photo" or "idphoto")
        {
            Section("Tùy chọn chụp");
            Toggle("Tạo thêm GIF từ bộ ảnh", Mode.AlsoGif, v => Mode.AlsoGif = v);
            Toggle("Âm báo đếm ngược", Mode.Beep, v => Mode.Beep = v);
            Toggle("Lấy nét trước khi chụp", Mode.Autofocus, v => Mode.Autofocus = v);
        }
        else
        {
            Section(null);
            Number("Đếm ngược trước khi bắt đầu", Mode.Countdown, 0, 30, v => Mode.Countdown = (int)v, 1, "giây");
            if (Mode.Id is "gif" or "boomerang")
            {
                Number("Số khung hình", Mode.FrameCount, 2, 60, v => Mode.FrameCount = (int)v);
                Number("Khoảng cách phát khung hình", Mode.FrameDelayMs, 50, 2000, v => Mode.FrameDelayMs = (int)v, 50, "ms");
            }
            else if (Mode.Id != "slowmotion") Number("Thời lượng ghi", Mode.DurationSeconds, 1, 120, v => Mode.DurationSeconds = (int)v, 1, "giây");
            if (Mode.Id == "timelapse") Number("Khoảng cách lấy ảnh", Mode.IntervalSeconds, 1, 60, v => Mode.IntervalSeconds = (int)v, 1, "giây");
            if (Mode.Id is "gif" or "boomerang" or "slowmotion") Toggle("Phát xuôi rồi ngược", Mode.Reverse, v => Mode.Reverse = v);
            if (Mode.Id is "video" or "greeting") Toggle("Thu âm thanh", Mode.Audio, v => Mode.Audio = v);
            if (Mode.Id == "slowmotion") RenderSegments();
            Section("Đầu ra");
            Choice("Kích thước (px)", new[] { "1280 × 720", "1920 × 1080", "1080 × 1080", "1080 × 1920" }, Mode.Size, v => Mode.Size = v, wide: true);
            Choice("Xoay (độ)", new[] { 0, 90, 180, 270 }, Mode.Rotation, v => Mode.Rotation = v, format: "{0}°");
            Number("Chất lượng", Mode.Quality, 1, 100, v => Mode.Quality = (int)v, 5, "%");
            Section("Trang trí");
            Asset("Chọn lớp phủ PNG…", Mode.OverlayPath, "PNG|*.png", v => Mode.OverlayPath = v);
            if (Mode.Id is "video" or "slowmotion" or "greeting") Asset("Chọn nhạc MP3…", Mode.SoundtrackPath, "MP3|*.mp3", v => Mode.SoundtrackPath = v);
        }
        building = false; UpdateDots(); Changed();
    }

    private void RenderSegments()
    {
        Section("Dòng thời gian");
        for (int i = 0; i < Mode.Segments.Count; i++)
        {
            var segment = Mode.Segments[i];
            var line = new WrapPanel { Margin = new(0, 0, 0, 12) };
            line.Children.Add(new TextBlock { Text = $"Đoạn {i + 1}", FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center, MinWidth = 72 });

            var duration = new NumericStepper { Minimum = 1, Maximum = 30, Step = 1, Unit = "giây", Value = segment.Duration, VerticalAlignment = VerticalAlignment.Center };
            System.Windows.Automation.AutomationProperties.SetName(duration, "Thời lượng gốc");
            duration.ValueChanged += (_, _) => { segment.Duration = duration.Value; Changed(); };
            var durationPair = new StackPanel { Orientation = Orientation.Horizontal, Margin = new(0, 0, 24, 0) };
            durationPair.Children.Add(new TextBlock { Text = "Thời lượng gốc", VerticalAlignment = VerticalAlignment.Center, Margin = new(0, 0, 12, 0) });
            durationPair.Children.Add(duration);
            line.Children.Add(durationPair);

            var speed = Segmented(new[] { 0.25, 0.5, 1, 2 }, segment.Speed, v => segment.Speed = v, "{0:0.##}×");
            System.Windows.Automation.AutomationProperties.SetName(speed, "Tốc độ phát");
            var speedPair = new StackPanel { Orientation = Orientation.Horizontal };
            speedPair.Children.Add(new TextBlock { Text = "Tốc độ phát", VerticalAlignment = VerticalAlignment.Center, Margin = new(0, 0, 12, 0) });
            speedPair.Children.Add(speed);
            line.Children.Add(speedPair);

            Place(line, wide: true);
        }
        var actions = new WrapPanel { Margin = new(0, 0, 0, 12) };
        var add = new Button { Content = "Thêm đoạn", IsEnabled = Mode.Segments.Count < 12 };
        var remove = new Button { Content = "Bỏ đoạn cuối", IsEnabled = Mode.Segments.Count > 1, Margin = new(8, 0, 0, 0) };
        add.Click += (_, _) => { if (invalid.Count > 0) return; Mode.Segments.Add(new()); Render(); Changed(); };
        remove.Click += (_, _) => { if (invalid.Count > 0) return; Mode.Segments.RemoveAt(Mode.Segments.Count - 1); Render(); Changed(); };
        actions.Children.Add(add);
        actions.Children.Add(remove);
        Place(actions, wide: true);
    }

    private void OnEnabled(object sender, RoutedEventArgs e)
    {
        if (building) return;
        Mode.Enabled = EnabledMode.IsChecked == true;
        UpdateDots();
        Changed();
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        try { if (invalid.Count > 0) throw new ArgumentException("Kiểm tra ô nhập đang báo lỗi."); configuration.Validate(); store.Save(configuration); saved = Snapshot(); Changed(); Feedback.Text = "Đã lưu thiết lập."; }
        catch (Exception error) { Feedback.Text = error.Message; }
    }

    private void OnRevert(object sender, RoutedEventArgs e)
    {
        configuration = JsonSerializer.Deserialize<CaptureConfiguration>(saved)!;
        LoadSession();
        Render();
        Feedback.Text = "";
    }
}
