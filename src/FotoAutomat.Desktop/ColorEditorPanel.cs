using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using FotoAutomat.Core;

namespace FotoAutomat.Desktop;

public sealed partial class ColorEditorPanel : UserControl
{
    private readonly ColorPreviewSession session;
    private readonly MatchLookTool matchLook;
    private readonly Image preview = new() { Stretch = Stretch.Uniform };
    private readonly StackPanel controls = new();
    private readonly TextBlock status = new() { Margin = new Thickness(0, 6, 0, 2), FontSize = 12, Foreground = EditorUi.Muted, TextWrapping = TextWrapping.Wrap, TextTrimming = TextTrimming.CharacterEllipsis, MaxHeight = 34 };
    private Image[] thumbnails = [];
    private TextBlock[] labels = [];
    private Border[] cards = [];
    private readonly StackPanel choices = new() { Orientation = Orientation.Horizontal };
    private readonly ComboBox presetSelector = new() { Height = EditorUi.ControlHeight, MinHeight = EditorUi.ControlHeight, FontSize = EditorUi.TextSize, VerticalContentAlignment = VerticalAlignment.Center };
    private readonly TextBlock clipping = new() { FontSize = 11, Foreground = EditorUi.Warning, Margin = new Thickness(0, 0, 0, 4) };
    private bool building;
    private readonly ScrollViewer toolScroll = new() { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    private int selected;
    private bool original;
    private readonly CheckBox before = new() { Content = "Xem ảnh gốc", MinHeight = EditorUi.ControlHeight, FontSize = EditorUi.TextSize, VerticalAlignment = VerticalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 10, 6) };
    private readonly ColorHistogram histogram = new() { Height = 96, Margin = new Thickness(0, 0, 0, 4) };
    private Button makeDefault = null!, removePreset = null!;
    public void SelectPreset(int index)
    {
        if (index < 0 || index >= session.Workspace.Looks.Length) throw new ArgumentOutOfRangeException(nameof(index));
        selected = index; selectedLayer = -1; painting = false; BuildControls(); toolScroll.ScrollToTop(); Refresh();
    }
    public ColorEditorPanel(ColorPreviewSession? previewSession = null)
    {
        session = previewSession ?? ColorPreviewSession.Shared;
        matchLook = new MatchLookTool(session, () => selected, index => { selected = index; selectedLayer = -1; painting = false; original = false; before.IsChecked = false; BuildControls(); Refresh(); });
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/FotoAutomat.Desktop;component/CompactColorControls.xaml", UriKind.Relative) });
        FontSize = EditorUi.TextSize; UseLayoutRounding = true; SnapsToDevicePixels = true; Background = Brushes.Transparent;
        // Responsive: fills the page (WorkspaceToolsPanel gives it the remaining height); preview and tools share the width.
        MinHeight = 560;
        var root = new Grid(); root.ColumnDefinitions.Add(new() { Width = new GridLength(2.2, GridUnitType.Star) }); root.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star), MinWidth = 340, MaxWidth = 480 });
        var left = new Grid { Margin = new Thickness(0, 0, 16, 0) };
        left.RowDefinitions.Add(new() { Height = GridLength.Auto }); left.RowDefinitions.Add(new() { Height = new GridLength(1, GridUnitType.Star), MinHeight = 260 }); left.RowDefinitions.Add(new() { Height = GridLength.Auto }); left.RowDefinitions.Add(new() { Height = GridLength.Auto });
        var actions = new WrapPanel();
        actions.Children.Add(Button("Đổi ảnh", Import));
        before.Click += (_, _) => { original = before.IsChecked == true; Refresh(); };
        var fit = Button("Vừa khung", ResetView); fit.ToolTip = "Đặt lại zoom (hoặc nhấp đúp vào ảnh)"; actions.Children.Add(fit);
        var undoButton = Button("Hoàn tác", UndoEdit); undoButton.ToolTip = "Hoàn tác (Ctrl+Z)"; actions.Children.Add(undoButton);
        var redoButton = Button("Làm lại", RedoEdit); redoButton.ToolTip = "Làm lại (Ctrl+Y hoặc Ctrl+Shift+Z)"; actions.Children.Add(redoButton);
        actions.Children.Add(new Border { Width = 1, Height = 20, Margin = new Thickness(6, 0, 10, 6), Background = EditorUi.Line });
        actions.Children.Add(before);
        PreviewKeyDown += OnShortcut;
        left.Children.Add(actions); Grid.SetRow(preview, 1); var previewGrid = new Grid { Background = new SolidColorBrush(Color.FromRgb(28, 30, 31)), ClipToBounds = true }; EditorImageViewport.Attach(previewGrid, preview); EditorImageViewport.Attach(previewGrid, maskOverlay); Grid.SetRow(previewGrid, 1); left.Children.Add(BuildReferenceComparison(previewGrid, actions)); WireMaskInput(previewGrid); WireViewInput(previewGrid); WireColorPicker(previewGrid);
        // Preset strip: one horizontal row, swipe with a finger on a touch monitor (PanningMode), or scroll with the bar/wheel.
        var choiceScroll = new ScrollViewer { Content = choices, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled, PanningMode = PanningMode.HorizontalOnly, Margin = new Thickness(0, 8, 0, 0) };
        choiceScroll.PreviewMouseWheel += (_, e) => { choiceScroll.ScrollToHorizontalOffset(choiceScroll.HorizontalOffset - e.Delta); e.Handled = true; };
        Grid.SetRow(choiceScroll, 2); left.Children.Add(choiceScroll);
        Grid.SetRow(status, 3); left.Children.Add(status); root.Children.Add(left);
        var side = new DockPanel(); var sideCard = new Border { Background = Brushes.White, BorderBrush = EditorUi.Line, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 10, 8, 10), Child = side }; Grid.SetColumn(sideCard, 1); root.Children.Add(sideCard);
        // One row manages the selected preset: choose it, make it the booth default, or remove it.
        var presetRow = new Grid { Margin = new Thickness(0, 0, 0, 6) };
        presetRow.ColumnDefinitions.Add(new()); presetRow.ColumnDefinitions.Add(new() { Width = GridLength.Auto }); presetRow.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        makeDefault = Button("☆ Mặc định", MakeSelectedDefault); makeDefault.Margin = new Thickness(6, 0, 0, 0);
        removePreset = Button("Xóa", RemoveSelectedPreset); removePreset.Margin = new Thickness(6, 0, 0, 0); removePreset.ToolTip = "Xóa preset đang chọn khỏi thư viện";
        Grid.SetColumn(makeDefault, 1); Grid.SetColumn(removePreset, 2); presetRow.Children.Add(presetSelector); presetRow.Children.Add(makeDefault); presetRow.Children.Add(removePreset);
        var header = new StackPanel(); header.Children.Add(presetRow);
        header.Children.Add(histogram); header.Children.Add(clipping); DockPanel.SetDock(header, Dock.Top); side.Children.Add(header);
        presetSelector.SelectionChanged += (_, _) => { if (building || presetSelector.SelectedIndex < 0) return; selected = presetSelector.SelectedIndex; selectedLayer = -1; painting = false; BuildControls(); toolScroll.ScrollToTop(); Refresh(); };
        var footer = new UniformGrid { Columns = 2, Margin = new Thickness(0, 6, 0, 0) };
        var import = Button("Nhập preset (.xmp/.cube)", ImportPresets); import.Margin = new Thickness(0, 0, 3, 0);
        var saveButton = EditorUi.Button("Lưu preset", SavePreset, primary: true); saveButton.Margin = new Thickness(3, 0, 0, 0);
        footer.Children.Add(import); footer.Children.Add(saveButton); DockPanel.SetDock(footer, Dock.Bottom); side.Children.Add(footer);
        var toolStack = new StackPanel(); toolStack.Children.Add(matchLook); toolStack.Children.Add(controls);
        toolScroll.Content = toolStack; side.Children.Add(toolScroll); Content = root;
        Loaded += (_, _) => { session.Changed -= Refresh; session.Changed += Refresh; Refresh(); };
        Unloaded += (_, _) => session.Changed -= Refresh;
        BuildControls(); Refresh();
    }
    private void BuildChoices()
    {
        choices.Children.Clear(); int count = session.Workspace.Looks.Length;
        thumbnails = Enumerable.Range(0, count).Select(_ => new Image()).ToArray(); labels = Enumerable.Range(0, count).Select(_ => new TextBlock()).ToArray(); cards = Enumerable.Range(0, count).Select(_ => new Border()).ToArray();
        for (int i = 0; i < count; i++)
        {
            int index = i; var stack = new StackPanel(); thumbnails[i].Height = 60; thumbnails[i].Stretch = Stretch.Uniform; stack.Children.Add(thumbnails[i]);
            labels[i].HorizontalAlignment = HorizontalAlignment.Center; labels[i].Margin = new Thickness(0, 4, 0, 0); labels[i].FontSize = 12; stack.Children.Add(labels[i]);
            // Touch card: large tap area; a horizontal swipe on the strip scrolls instead of selecting.
            var button = Button("", () => { selected = index; selectedLayer = -1; original = false; BuildControls(); Refresh(); }); button.Content = stack; button.Height = double.NaN; button.Padding = new Thickness(4); button.Margin = new Thickness(0);
            cards[i].Child = button; cards[i].Width = 112; cards[i].BorderThickness = new Thickness(2); cards[i].CornerRadius = new CornerRadius(6); cards[i].Margin = new Thickness(0, 0, 6, 6); choices.Children.Add(cards[i]);
        }
    }
    private static Button Button(string label, Action action) => EditorUi.Button(label, action);
    private void UndoEdit() { if (!session.CanUndo) return; session.Undo(); selected = Math.Min(selected, session.Workspace.Looks.Length - 1); selectedLayer = -1; painting = false; BuildControls(); Refresh(); }
    private void RedoEdit() { if (!session.CanRedo) return; session.Redo(); selectedLayer = -1; painting = false; BuildControls(); Refresh(); }
    // Ctrl+Z / Ctrl+Y / Ctrl+Shift+Z; a focused text box keeps its own text undo.
    private void OnShortcut(object sender, KeyEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0 || Keyboard.FocusedElement is TextBoxBase) return;
        bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
        if (e.Key == Key.Z && !shift) { UndoEdit(); e.Handled = true; }
        else if (e.Key == Key.Y || e.Key == Key.Z && shift) { RedoEdit(); e.Handled = true; }
    }
    private void Import()
    {
        var dialog = new OpenFileDialog { Filter = "Ảnh|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff", Title = "Chọn ảnh mẫu cho tất cả bộ màu" };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;
        try { session.LoadImage(dialog.FileName); }
        catch (Exception e) { new WarningWindow(Window.GetWindow(this), e.Message).ShowDialog(); }
    }
    private void BuildControls()
    {
        CancelColorPick(); refreshColorSample = null; viewSelectedColorRange = false;
        matchLook.SelectionChanged();
        building = true; presetSelector.ItemsSource = session.Workspace.Looks.Select(r => r.Name).ToArray(); presetSelector.SelectedIndex = selected; building = false;
        BuildChoices();
        if (selectedLayer >= 0 && CurrentLayer is null) selectedLayer = -1;
        controls.Children.Clear();
        BuildLayerControls();
        BuildLightControls();
        BuildAdvancedControls();
        BuildFinishingControls();
        controls.Children.Add(Button("Đặt lại preset", () => { UpdateRecipe(new ColorRecipe(CurrentRecipe.Name) { PresetId = CurrentRecipe.PresetId }); BuildControls(); }));
    }
    // track: optional gradient drawn behind the slider (Color Editor / Color Balance), as in Capture One.
    private void AddSlider(string label, double min, double max, double value, Func<ColorRecipe, double, ColorRecipe> change, StackPanel? destination = null, Func<ColorRecipe, double>? readBack = null, Brush? track = null, double? reset = null)
    {
        var text = new TextBlock { Text = label, FontSize = 12, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, ToolTip = label }; var slider = new Slider { Minimum = min, Maximum = max, Value = value, TickFrequency = label == "Exposure" ? .01 : max == 5 ? .1 : 1, IsSnapToTickEnabled = false, Height = EditorUi.RowHeight, Margin = new Thickness(4, 0, 6, 0) };
        // Double-click a slider to reset it to its neutral value (0, or the minimum when 0 is out of range).
        slider.MouseDoubleClick += (_, _) => slider.Value = Math.Clamp(reset ?? 0, min, max);
        System.Windows.Automation.AutomationProperties.SetName(slider, label);
        slider.ValueChanged += (_, _) => { original = false; before.IsChecked = false; UpdateRecipe(change(CurrentRecipe, slider.Value)); };
        var row = new Grid { Height = EditorUi.RowHeight };
        row.ColumnDefinitions.Add(new() { Width = new GridLength(88) }); row.ColumnDefinitions.Add(new()); row.ColumnDefinitions.Add(new() { Width = new GridLength(72) });
        string format = label == "Exposure" ? "0.00" : "0.##";
        var number = new TextBox { Text = value.ToString(format, System.Globalization.CultureInfo.CurrentCulture), Style = (Style)FindResource("CompactNumericInput") }; Grid.SetColumn(number, 2); row.Children.Add(text); row.Children.Add(number);
        if (track is null) { Grid.SetColumn(slider, 1); row.Children.Add(slider); }
        else
        {
            slider.Style = (Style)FindResource("GradientSlider");
            var host = new Grid(); host.Children.Add(new Border { Height = 6, CornerRadius = new CornerRadius(3), Background = track, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 16, 0), IsHitTestVisible = false });
            host.Children.Add(slider); Grid.SetColumn(host, 1); row.Children.Add(host);
        }
        System.Windows.Automation.AutomationProperties.SetName(number, label + " giá trị");
        void Commit() { if (double.TryParse(number.Text, out double input) && double.IsFinite(input) && input >= min && input <= max) { number.ClearValue(Border.BorderBrushProperty); slider.Value = input; } else { number.BorderBrush = Brushes.Red; } }
        number.KeyDown += (_, e) => { if (e.Key == System.Windows.Input.Key.Enter) { Commit(); e.Handled = true; } }; number.LostKeyboardFocus += (_, _) => Commit();
        slider.ValueChanged += (_, _) =>
        {
            double actual = readBack?.Invoke(CurrentRecipe) ?? slider.Value;
            if (Math.Abs(slider.Value - actual) > .001) slider.Value = actual;
            number.Text = actual.ToString(format, System.Globalization.CultureInfo.CurrentCulture);
        };
        (destination ?? controls).Children.Add(row);
    }
    private void Refresh()
    {
        selected = Math.Min(selected, session.Workspace.Looks.Length - 1);
        bool isDefault = IsDefault(selected);
        makeDefault.Content = isDefault ? "★ Mặc định" : "☆ Mặc định"; makeDefault.IsEnabled = !isDefault;
        makeDefault.ToolTip = isDefault ? "Đây là tông mặc định của màn hình khách" : "Đặt preset này làm tông mặc định của màn hình khách (lưu preset nếu là bản nháp)";
        preview.Source = original ? session.Original : session.Previews.ElementAtOrDefault(selected); refreshColorSample?.Invoke(); histogram.ExposureMetadata = session.ExposureMetadata; histogram.Update(preview.Source as System.Windows.Media.Imaging.BitmapSource); activeCurveEditor?.SetHistogram(histogram.Snapshot()); clipping.Text = $"Clipping: vùng sáng {histogram.HighlightClipping:0.0}%  ·  vùng tối {histogram.ShadowClipping:0.0}%"; RefreshMask(); RefreshSelectedColorView(); TransformView(); status.Text = (original ? "Ảnh gốc · " : "") + session.Status;
        for (int i = 0; i < Math.Min(thumbnails.Length, session.Workspace.Looks.Length); i++) { thumbnails[i].Source = session.Previews.ElementAtOrDefault(i); labels[i].Text = (IsDefault(i) ? "★ " : "") + session.Workspace.Looks[i].Name; labels[i].TextTrimming = TextTrimming.CharacterEllipsis; labels[i].ToolTip = session.Workspace.Looks[i].Name; cards[i].BorderBrush = i == selected ? EditorUi.Accent : Brushes.Transparent; }
    }
    // The booth's default is a published preset; compare by PresetId because drafts and the published library differ.
    private bool IsDefault(int index) =>
        session.Workspace.Looks.ElementAtOrDefault(index) is { } look && session.Published.Looks.ElementAtOrDefault(session.Published.DefaultIndex)?.PresetId == look.PresetId;
    private void MakeSelectedDefault()
    {
        try { session.SetDefault(selected); Refresh(); }
        catch (Exception e) { new WarningWindow(Window.GetWindow(this), e.Message).ShowDialog(); }
    }
    private void RemoveSelectedPreset()
    {
        if (session.Workspace.Looks.ElementAtOrDefault(selected) is not { } recipe) return;
        string note = (recipe.Lut is not null ? "\nFile .cube gốc vẫn được giữ trên ổ đĩa." : "") + (IsDefault(selected) ? "\nĐây là tông mặc định; preset còn lại đầu tiên sẽ thành mặc định." : "");
        if (MessageBox.Show(Window.GetWindow(this), $"Xóa preset ‘{recipe.Name}’ khỏi thư viện?{note}\nThao tác này không có Undo.", "Xóa preset", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes) return;
        try
        {
            session.RemovePreset(selected); selected = Math.Clamp(selected, 0, session.Workspace.Looks.Length - 1);
            selectedLayer = -1; painting = false; original = false; before.IsChecked = false;
            BuildControls(); Refresh();
        }
        catch (Exception e) { new WarningWindow(Window.GetWindow(this), e.Message).ShowDialog(); }
    }
}
