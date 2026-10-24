using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FotoAutomat.Core;
using FotoAutomat.Infrastructure;
using Microsoft.Win32;

namespace FotoAutomat.Desktop;

/// <summary>Compact Adjust tool. Uses the editor's image, preview, history and preset library.</summary>
public sealed class MatchLookTool : UserControl
{
    private readonly ColorPreviewSession session;
    private readonly Func<int> selected;
    private readonly Action<int> select;
    private MatchImage[] references = [];
    public IReadOnlyList<MatchImage> References => references;
    public event Action? ReferencesChanged;
    private ReferenceMatchResult? result;
    private BitmapSource? sourceAtMatch;
    private string? draftId;
    private ColorRecipe? applied;
    private CancellationTokenSource? running;
    private bool publishing;
    private enum ReferenceType { Auto, SameContent, Scene }
    private ReferenceType referenceType = ReferenceType.Auto;
    private readonly DispatcherTimer debounce = new() { Interval = TimeSpan.FromMilliseconds(90) };
    private readonly WrapPanel referenceStrip = new() { Margin = new Thickness(0, 0, 0, 6) };
    private readonly CheckBox normalize = EditorUi.Check("Normalize"), exposure = EditorUi.Check("Exposure"), whiteBalance = EditorUi.Check("White Balance"), light = EditorUi.Check("Light & Contrast"), color = EditorUi.Check("Color Adjust"), tone = EditorUi.Check("Color Tone"), faces = EditorUi.Check("Ưu tiên khuôn mặt");
    private readonly Dictionary<MatchComponents, CheckBox> componentChecks = new();
    private readonly Slider impact = new() { Minimum = 0, Maximum = 100, Value = 100, Height = EditorUi.RowHeight, TickFrequency = 1, IsSnapToTickEnabled = true, Margin = new Thickness(4, 0, 6, 0) };
    private readonly TextBox amount = new() { Text = "100" };
    private readonly Button apply, exportButton, useCurrent, optionsButton;
    private readonly ProgressBar progress = new() { Height = 4, Maximum = 100, Visibility = Visibility.Collapsed, Margin = new Thickness(0, 2, 0, 2) };
    private readonly TextBlock state = new() { FontSize = EditorUi.TextSize, TextWrapping = TextWrapping.Wrap, Visibility = Visibility.Collapsed, Margin = new Thickness(0, 4, 0, 4) };
    private readonly ComboBox targets = new() { Height = EditorUi.ControlHeight, FontSize = EditorUi.TextSize, VerticalContentAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 4, 0, 4), DisplayMemberPath = "Name", Visibility = Visibility.Collapsed };
    private readonly Dictionary<string, ReferenceMatchResult> results = new(StringComparer.OrdinalIgnoreCase);
    public MatchLookTool(ColorPreviewSession session, Func<int> selected, Action<int> select)
    {
        this.session = session; this.selected = selected; this.select = select;
        FontSize = EditorUi.TextSize; UseLayoutRounding = true; Foreground = EditorUi.Ink;
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/FotoAutomat.Desktop;component/CompactColorControls.xaml", UriKind.Relative) });
        amount.Style = (Style)FindResource("CompactNumericInput"); amount.Width = 56;
        var content = new StackPanel { Margin = new Thickness(6, 4, 6, 8) };
        AllowDrop = true; // drop reference images anywhere on the tool; they are added to the current set
        DragOver += (_, e) => { e.Effects = running is null && e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None; e.Handled = true; };
        Drop += (_, e) => { if (running is null && e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0) Try(() => AddReferences(files)); e.Handled = true; };
        content.Children.Add(referenceStrip);
        useCurrent = EditorUi.Button("Dùng ảnh đang chỉnh làm reference", UseCurrent); useCurrent.HorizontalAlignment = HorizontalAlignment.Stretch; useCurrent.Margin = new Thickness(0, 0, 0, 6); useCurrent.ToolTip = "Lấy preset đang chọn trên ảnh hiện tại làm reference"; content.Children.Add(useCurrent);
        var norm = new StackPanel(); norm.Children.Add(exposure); norm.Children.Add(whiteBalance);
        content.Children.Add(Group(normalize, norm, "Exposure · White Balance (Warmth, Tint)"));
        content.Children.Add(Group(light, ComponentChecks((MatchComponents.Contrast, "Contrast"), (MatchComponents.Highlights, "Highlights"), (MatchComponents.Shadows, "Shadows"), (MatchComponents.Whites, "Whites"), (MatchComponents.Blacks, "Blacks"), (MatchComponents.RgbCurve, "RGB Curve"))));
        content.Children.Add(Group(color, ComponentChecks((MatchComponents.Saturation, "Saturation"), (MatchComponents.HslHue, "HSL Hue"), (MatchComponents.HslSaturation, "HSL Saturation"), (MatchComponents.HslLightness, "HSL Lightness"))));
        content.Children.Add(Group(tone, ComponentChecks((MatchComponents.ShadowBalance, "Shadow Balance"), (MatchComponents.MidtoneBalance, "Midtone Balance"), (MatchComponents.HighlightBalance, "Highlight Balance"), (MatchComponents.RedCurve, "Red Curve"), (MatchComponents.GreenCurve, "Green Curve"), (MatchComponents.BlueCurve, "Blue Curve"))));
        var impactRow = new Grid { Height = EditorUi.RowHeight }; impactRow.ColumnDefinitions.Add(new() { Width = new GridLength(70) }); impactRow.ColumnDefinitions.Add(new()); impactRow.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        impactRow.Children.Add(new TextBlock { Text = "Impact", VerticalAlignment = VerticalAlignment.Center, ToolTip = "Mức áp look (0–100). Exposure và White Balance luôn giữ đủ." }); Grid.SetColumn(impact, 1); impactRow.Children.Add(impact); Grid.SetColumn(amount, 2); impactRow.Children.Add(amount); content.Children.Add(impactRow);
        // Saving the matched look lives in the editor footer ("Lưu preset"); here only export and apply.
        var buttons = new UniformGrid { Columns = 2, Margin = new Thickness(0, 6, 0, 0) };
        exportButton = EditorUi.Button("Xuất .cube", ExportCube); exportButton.ToolTip = "Xuất preset đang chọn thành LUT 33³ .cube (công cụ không gian như Clarity, Grain không có trong LUT)"; exportButton.Margin = new Thickness(0, 0, 4, 6); buttons.Children.Add(exportButton);
        apply = EditorUi.Button("Áp dụng", async () => { if (running is not null) running.Cancel(); else await ApplyAsync(); }, primary: true); apply.Margin = new Thickness(4, 0, 0, 6); buttons.Children.Add(apply); content.Children.Add(buttons);
        content.Children.Add(progress); content.Children.Add(state); content.Children.Add(targets);
        var menu = new ContextMenu(); var add = new MenuItem { Header = "Thêm ảnh cần chỉnh…" }; add.Click += (_, _) => PickTargets(); menu.Items.Add(add);
        var faceItem = new MenuItem { Header = "Ưu tiên khuôn mặt", IsCheckable = true, IsChecked = true, ToolTip = "Ưu tiên vùng mặt khi cả hai ảnh đều có khuôn mặt" }; faceItem.Click += (_, _) => { faces.IsChecked = faceItem.IsChecked; Invalidate(); }; menu.Items.Add(faceItem);
        // Reference type: Auto (v5b step 1) picks per target image from face detection; the others force one engine.
        // Research option: shown only in developer mode; production always uses Auto.
        if (App.DeveloperMode)
        {
            var typeItems = new (ReferenceType Type, string Header, string Tip)[]
            {
                (ReferenceType.Auto, "Reference type: Auto (v5b)", "Uses face detection: if the image has people and no reference does, the references are treated as scenes (v5a); otherwise as the same kind of photo (v4)."),
                (ReferenceType.SameContent, "Reference type: Same content (v4)", "Learn colour from the whole reference. Best when the reference is the same kind of photo (portrait → portrait)."),
                (ReferenceType.Scene, "Reference type: Scene (v5a)", "Ignore the scene's own colours (sea, sky, stone) and learn the look from neutral areas and tone. Experimental.")
            }.Select(t => new MenuItem { Header = t.Header, IsCheckable = true, IsChecked = t.Type == referenceType, ToolTip = t.Tip, Tag = t.Type }).ToArray();
            menu.Items.Add(new Separator());
            foreach (var item in typeItems)
            {
                item.Click += (_, _) => { referenceType = (ReferenceType)item.Tag; foreach (var other in typeItems) other.IsChecked = other == item; Invalidate(); };
                menu.Items.Add(item);
            }
            menu.Items.Add(new Separator());
        }
        var clear = new MenuItem { Header = "Xóa tất cả reference" }; clear.Click += (_, _) => { if (running is not null) return; references = []; draftId = null; ReferencesChanged?.Invoke(); BuildReferenceStrip(); Invalidate(); }; menu.Items.Add(clear);
        optionsButton = EditorUi.Button("···", () => { menu.PlacementTarget = optionsButton; menu.IsOpen = true; }); optionsButton.Width = EditorUi.ControlHeight; optionsButton.Padding = new Thickness(0); optionsButton.Margin = new Thickness(0); optionsButton.ToolTip = "Tùy chọn Match Look";
        var header = new DockPanel { LastChildFill = true }; DockPanel.SetDock(optionsButton, Dock.Right); header.Children.Add(optionsButton); header.Children.Add(new TextBlock { Text = "Match Look", FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center });
        var expander = new Expander { Header = header, HorizontalContentAlignment = HorizontalAlignment.Stretch, Content = content, IsExpanded = true, Padding = new Thickness(4), Margin = new Thickness(0, 1, 0, 3) };
        // An Expander header does not stretch on its own; follow the panel width so "···" stays at the right edge.
        expander.SizeChanged += (_, _) => header.Width = Math.Max(120, expander.ActualWidth - 36);
        Content = expander;
        foreach (var check in new[] { normalize, exposure, whiteBalance, light, color, tone }) check.Click += (_, _) => Invalidate();
        impact.ValueChanged += (_, _) => { amount.Text = impact.Value.ToString("0"); debounce.Stop(); debounce.Start(); };
        void CommitAmount() { if (double.TryParse(amount.Text, out var value) && double.IsFinite(value)) impact.Value = Math.Clamp(value, 0, 100); amount.Text = impact.Value.ToString("0"); }
        amount.LostKeyboardFocus += (_, _) => CommitAmount(); amount.KeyDown += (_, e) => { if (e.Key == System.Windows.Input.Key.Enter) { CommitAmount(); e.Handled = true; } };
        debounce.Tick += (_, _) => { debounce.Stop(); Publish(); };
        targets.SelectionChanged += (_, _) => { if (running is not null || targets.SelectedItem is not MatchImage image) return; publishing = true; try { session.LoadMatchImage(image); } finally { publishing = false; } result = results.GetValueOrDefault(image.Path); sourceAtMatch = session.Original; if (result is not null) Publish(); else { applied = null; Refresh(); } };
        Loaded += (_, _) => { session.Changed -= SessionChanged; session.Changed += SessionChanged; Refresh(); };
        Unloaded += (_, _) => { session.Changed -= SessionChanged; running?.Cancel(); debounce.Stop(); };
        BuildReferenceStrip(); Refresh();
    }
    private StackPanel ComponentChecks(params (MatchComponents Component, string Label)[] components)
    {
        var panel = new StackPanel();
        foreach (var item in components)
        {
            var check = EditorUi.Check(item.Label, true); componentChecks.Add(item.Component, check);
            check.Click += (_, _) => Invalidate(); panel.Children.Add(check);
        }
        return panel;
    }
    private static TextBlock Detail(string text) => new() { Text = text, FontSize = 12, TextWrapping = TextWrapping.Wrap, Foreground = EditorUi.Muted };
    private static FrameworkElement Group(CheckBox check, UIElement body, string? tip = null) => new Expander { Header = check, Content = new Border { Child = body, Padding = new Thickness(28, 0, 4, 4) }, IsExpanded = false, ToolTip = tip ?? (body as TextBlock)?.Text };
    /// <summary>One tile per reference (each with a remove button) plus an add tile; 1–8 references.</summary>
    private void BuildReferenceStrip()
    {
        referenceStrip.Children.Clear();
        for (int i = 0; i < references.Length; i++)
        {
            int index = i; var image = references[i];
            var tile = new Grid { Width = 64, Height = 64, Margin = new Thickness(0, 0, 6, 6), Background = EditorUi.Surface, ToolTip = image.Name, ClipToBounds = true };
            tile.Children.Add(new Image { Source = image.Bitmap, Stretch = Stretch.UniformToFill });
            var remove = new Button { Content = "×", Width = 30, Height = 30, MinHeight = 30, Padding = new Thickness(0), FontSize = 16, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 2, 2, 0), Background = Brushes.White, ToolTip = "Bỏ reference này" };
            remove.Click += (_, _) => { if (running is not null) return; references = references.Where((_, k) => k != index).ToArray(); draftId = null; ReferencesChanged?.Invoke(); BuildReferenceStrip(); Invalidate(); };
            tile.Children.Add(remove); referenceStrip.Children.Add(tile);
        }
        if (references.Length >= 8) return;
        bool empty = references.Length == 0;
        var addTile = new Button
        {
            Height = 64,
            MinHeight = 64,
            Width = empty ? double.NaN : 64,
            Margin = empty ? new Thickness(0, 0, 0, 6) : new Thickness(0, 0, 6, 6),
            Background = EditorUi.Surface,
            BorderBrush = EditorUi.Line,
            ToolTip = "Thêm ảnh reference (1–8) · kéo thả ảnh",
            Content = new TextBlock { Text = empty ? "+ Thêm ảnh reference (1–8)" + Environment.NewLine + "hoặc kéo thả ảnh vào đây" : "+", TextAlignment = TextAlignment.Center, FontSize = empty ? EditorUi.TextSize : 24, Foreground = EditorUi.Muted }
        };
        addTile.Click += (_, _) => PickReference();
        // Empty: the add tile spans the panel width (a WrapPanel never stretches its children).
        if (empty) addTile.SetBinding(WidthProperty, new System.Windows.Data.Binding(nameof(ActualWidth)) { Source = referenceStrip });
        referenceStrip.Children.Add(addTile);
    }
    // Reads only the new files: a reference made with "Dùng ảnh đang chỉnh" is a rendered bitmap, not a file on disk.
    private void AddReferences(IEnumerable<string> files)
    {
        var known = references.Select(r => r.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var added = files.Distinct(StringComparer.OrdinalIgnoreCase).Where(f => !known.Contains(f)).ToArray();
        if (added.Length == 0) return;
        if (references.Length + added.Length > 8) throw new ArgumentException("Tối đa 8 ảnh reference.");
        SetReferences(references.Concat(added.Select(MatchImageReader.Read)).ToArray());
    }
    private void Try(Action action) { try { action(); } catch (Exception e) { Error(e); } }
    private void Error(Exception e) { state.Text = e.Message; state.Foreground = EditorUi.Danger; state.Visibility = Visibility.Visible; state.ToolTip = e.Message; }
    public void LoadReference(string path) => LoadReferences([path]);
    public void LoadReferences(IEnumerable<string> paths)
    {
        if (running is not null) return;
        var files = paths.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (files.Length is < 1 or > 8) throw new ArgumentException("Chọn 1–8 ảnh reference.");
        SetReferences(files.Select(MatchImageReader.Read).ToArray());
    }
    private void SetReferences(MatchImage[] images)
    {
        draftId = null; // a new reference set is a new look
        references = images; BuildReferenceStrip(); Invalidate(); ReferencesChanged?.Invoke();
    }
    private void PickReference()
    {
        if (running is not null) return;
        var dialog = new OpenFileDialog { Title = "Chọn ảnh reference (1–8)", Filter = "Ảnh|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff", Multiselect = true };
        if (dialog.ShowDialog(Window.GetWindow(this)) == true) Try(() => AddReferences(dialog.FileNames));
    }
    private void ExportCube()
    {
        if (running is not null || session.Workspace.Looks.ElementAtOrDefault(selected()) is not { } look) return;
        var dialog = new SaveFileDialog { Title = "Xuất LUT", Filter = "3D LUT|*.cube", FileName = string.Concat(look.Name.Split(Path.GetInvalidFileNameChars())) + ".cube" };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;
        Try(() =>
        {
            var baked = RecipeLutBaker.Bake(look, 33); CubeLutFile.Save(dialog.FileName, baked.Lut, look.Name);
            state.Foreground = EditorUi.Muted; state.Text = baked.Excluded.Length == 0 ? "Đã xuất .cube" : "Đã xuất .cube · không có trong LUT: " + string.Join(", ", baked.Excluded);
            state.ToolTip = state.Text; state.Visibility = Visibility.Visible;
        });
    }
    private void UseCurrent()
    {
        if (session.Busy || running is not null) return;
        if (session.Previews.ElementAtOrDefault(selected()) is { } bitmap) SetReferences([new(session.Workspace.SamplePath ?? "Ảnh đang chỉnh", bitmap, "Rendered sRGB")]);
    }
    private void PickTargets()
    {
        if (running is not null) return;
        var dialog = new OpenFileDialog { Title = "Chọn ảnh cần chỉnh (1–16)", Filter = "Ảnh|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff", Multiselect = true };
        if (dialog.ShowDialog(Window.GetWindow(this)) == true) Try(() => LoadTargets(dialog.FileNames));
    }
    public void LoadTargets(IEnumerable<string> paths)
    {
        if (running is not null) return;
        var files = paths.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (files.Length is < 1 or > 16) throw new ArgumentException("Chọn 1–16 ảnh.");
        var images = files.Select(MatchImageReader.Read).ToArray();
        Invalidate(); targets.ItemsSource = images; targets.Visibility = Visibility.Visible; targets.SelectedIndex = 0;
    }
    private void SessionChanged()
    {
        if (!publishing && sourceAtMatch is not null && sourceAtMatch != session.Original) { running?.Cancel(); Invalidate(); }
        if (!publishing && applied is not null && session.Workspace.Looks.ElementAtOrDefault(selected()) != applied) { result = null; applied = null; debounce.Stop(); }
        Refresh();
    }
    public void SelectionChanged() => SessionChanged();
    private void Invalidate() { result = null; applied = null; sourceAtMatch = null; results.Clear(); debounce.Stop(); state.Visibility = Visibility.Collapsed; Refresh(); }
    private void Refresh()
    {
        bool busy = running is not null;
        optionsButton.IsEnabled = !busy;
        apply.Content = busy ? "Hủy" : "Áp dụng"; apply.IsEnabled = busy || references.Length > 0 && session.Original is not null;
        exportButton.IsEnabled = !busy && session.Workspace.Looks.ElementAtOrDefault(selected()) is not null;
        useCurrent.IsEnabled = !busy && !session.Busy && session.Original is not null;
        foreach (var check in new[] { normalize, exposure, whiteBalance, light, color, tone }) check.IsEnabled = !busy;
        impact.IsEnabled = amount.IsEnabled = !busy && result is not null; targets.IsEnabled = !busy;
        progress.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
    }
    public async Task ApplyAsync()
    {
        if (running is not null || references.Length == 0 || session.Original is null) return;
        debounce.Stop(); sourceAtMatch = session.Original;
        using var cts = new CancellationTokenSource(); running = cts; Refresh(); state.Visibility = Visibility.Collapsed; progress.Value = 0;
        try
        {
            var options = new MatchOptions(Normalize: normalize.IsChecked == true, Tone: light.IsChecked == true, Color: color.IsChecked == true) { NormalizeExposure = exposure.IsChecked == true, NormalizeWhiteBalance = whiteBalance.IsChecked == true, ColorTone = tone.IsChecked == true, Components = componentChecks.Where(x => x.Value.IsChecked == true).Aggregate(MatchComponents.None, (a, x) => a | x.Key) }; options.Validate();
            // Auto needs faces on every image to decide the reference type, even when Face priority is off; regions are
            // then dropped again so Face priority keeps its meaning for the fit itself.
            bool detect = faces.IsChecked == true || referenceType == ReferenceType.Auto;
            MatchPhoto ForFit(MatchPhoto p) => faces.IsChecked == true ? p : p with { Regions = [] };
            var targetPhotos = new List<MatchPhoto>(); foreach (var r in references) targetPhotos.Add(await PhotoAsync(r, detect, cts.Token));
            var chosen = new List<bool>();
            var images = targets.ItemsSource is MatchImage[] batch && batch.Any(i => i.Bitmap == session.Original) ? batch : [new MatchImage(session.Workspace.SamplePath ?? "Image", session.Original, "sRGB")];
            var completed = new Dictionary<string, ReferenceMatchResult>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < images.Length; i++)
            {
                int item = i; var detected = await PhotoAsync(images[i], detect, cts.Token);
                bool scene = referenceType == ReferenceType.Scene || referenceType == ReferenceType.Auto && ReferenceMatchEngine.IsSceneReference(detected, targetPhotos);
                chosen.Add(scene); var input = ForFit(detected); var fitReferences = targetPhotos.Select(ForFit).ToArray(); var fitOptions = options with { SceneReference = scene };
                var report = new Progress<MatchProgress>(p => { if (running == cts && !cts.IsCancellationRequested) progress.Value = 100d * (item + (double)p.Completed / p.Total) / images.Length; });
                completed[images[i].Path] = await Task.Run(() => ReferenceMatchEngine.FitReferences(input, fitReferences, fitOptions, report, cts.Token), cts.Token);
            }
            cts.Token.ThrowIfCancellationRequested(); if (sourceAtMatch != session.Original) return;
            results.Clear(); foreach (var pair in completed) results.Add(pair.Key, pair.Value);
            result = results.GetValueOrDefault(session.Workspace.SamplePath ?? "Image") ?? completed.Values.First();
            Publish(newLook: true); state.Foreground = EditorUi.Muted;
            // Production shows plain language; engine versions only in developer mode.
            string kind = chosen.All(x => x) ? "reference phong cảnh" : chosen.Any(x => x) ? "reference hỗn hợp" : "reference cùng loại ảnh";
            if (App.DeveloperMode) kind += " · " + (referenceType == ReferenceType.Auto ? "Auto → " : "") + (chosen.All(x => x) ? "Scene (v5a)" : chosen.Any(x => x) ? "Scene/Same mixed" : "Same content (v4)");
            state.Text = (images.Length > 1 ? $"Đã áp dụng · {images.Length} ảnh" : "Đã áp dụng") + $" · {references.Length} reference · " + kind + (result.UsedRegions ? " · ưu tiên khuôn mặt" : "");
            state.ToolTip = result.UsedRegions ? "Vùng mặt + màu tổng thể" : "Màu tổng thể · không có vùng mặt ở cả hai ảnh"; state.Visibility = Visibility.Visible;
        }
        catch (OperationCanceledException) { state.Text = "Đã hủy"; state.Foreground = EditorUi.Muted; state.Visibility = Visibility.Visible; }
        catch (Exception e) { Error(e); }
        finally { running = null; Refresh(); }
    }
    public static async Task<MatchPhoto> PhotoAsync(MatchImage image, bool useFaces, CancellationToken ct = default)
    {
        var photo = new MatchPhoto(image.Pixels(), image.Bitmap.PixelWidth, image.Bitmap.PixelHeight);
        if (!useFaces) return photo;
        try { var detected = await WindowsFaceDetection.DetectAsync(photo.Pixels, photo.Width, photo.Height, ct); return photo with { Regions = detected.Select(f => new MatchRegion(f.X, f.Y, f.Width, f.Height)).ToArray() }; }
        catch (Exception e) when (e is NotSupportedException or System.Runtime.InteropServices.COMException) { return photo; }
    }
    // newLook: only Apply may start a new "Matched look N"; Impact and target switching only update the unsaved draft.
    private void Publish(bool newLook = false)
    {
        if (result is null || sourceAtMatch != session.Original) return;
        publishing = true;
        try
        {
            // Only the unsaved draft of this match may be overwritten; saved presets are never touched by matching.
            if (!MatchLookDrafts.CanReuse(draftId, session.Workspace.Looks, session.Published.Looks))
            {
                if (!newLook) { result = null; applied = null; return; } // draft was saved or removed: Apply again for a new look
                draftId = null;
            }
            int index = Array.FindIndex(session.Workspace.Looks, r => r.PresetId == draftId);
            var recipe = MatchRecipeComposer.Compose(result.Recipe, impact.Value, asLayer: false) with
            {
                Name = index < 0 ? MatchLookDrafts.NextName(session.Workspace.Looks.Select(r => r.Name)) : session.Workspace.Looks[index].Name,
                PresetId = draftId ?? Guid.NewGuid().ToString("N"),
                MatchOrigin = new("Unknown", "Reference", result.Engine, result.AfterDistributionRmse) { Metric = ReferenceMatchEngine.MetricName }
            };
            if (index < 0) index = session.AddPreset(recipe); else session.Update(index, recipe);
            draftId = recipe.PresetId; applied = recipe; select(index);
        }
        catch (Exception e) { Error(e); }
        finally { publishing = false; Refresh(); }
    }
}
