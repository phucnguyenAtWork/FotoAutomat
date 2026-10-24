using System.Windows;
using FotoAutomat.Desktop.ViewModels;
using FotoAutomat.Infrastructure;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace FotoAutomat.Desktop;

public partial class LayoutWindow : Window
{
    private readonly PrintLayoutStore layoutStore = new();
    private readonly string editorPath;
    private Point dragStart;
    private double startX, startY, startWidth, startHeight;
    private string savedLayout = "";
    private string? libraryPath;
    private bool updatingGallery;
    private string? originalScanArtwork, scannedArtwork;
    private LayoutEditorViewModel Model => (LayoutEditorViewModel)DataContext;
    public LayoutWindow() : this(null) { }
    internal LayoutWindow(string? editorPathOverride)
    {
        editorPath = editorPathOverride ?? PrintLayoutStore.EditorPath;
        InitializeComponent();
        DataContext = new LayoutEditorViewModel();
        Model.WarningRequested += ShowWarning;
        Model.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Model.Section))
            {
                Management.ShowSection(Model.Section);
                WorkspaceTools.Record($"Mở {Model.Section}");
                WorkspaceTools.ShowSection(Model.Section);
            }
            if (e.PropertyName == nameof(Model.Status)) WorkspaceTools.Record(Model.Status);
        };
        Management.ShowSection(Model.Section);
        try
        {
            var active = File.Exists(editorPath) ? layoutStore.Load(editorPath) : editorPathOverride is null ? layoutStore.LoadActive() : null;
            if (active is not null)
            {
                active = LegacyStripMigration.Convert(active);
                byte[]? bytes = active.ArtworkPng is null ? null : Convert.FromBase64String(active.ArtworkPng);
                BitmapImage? image = bytes is null ? null : DecodeArtwork(bytes);
                Model.LoadLayout(active); Model.SetArtwork(bytes, image);
            }
        }
        catch (Exception error) { Model.Warn($"Không đọc được mẫu đang dùng: {error.Message}"); }
        RefreshLibrary();
        savedLayout = Snapshot();
        Model.ResetHistory();
        Model.ConfirmReplaceLayout = () => { if (!ConfirmDiscard()) return false; libraryPath = null; return true; };
        Model.PresetOpened = () => { savedLayout = Snapshot(); Model.ResetHistory(); };
        Closing += (_, e) => e.Cancel = !ConfirmDiscard();
        Closed += (_, _) => Model.StopHistory();
    }

    private string Snapshot() => JsonSerializer.Serialize(Model.ExportLayout());
    private bool ConfirmDiscard()
    {
        CommitLayoutInputs(LayoutEditorRoot);
        if (Snapshot() == savedLayout) return true;
        var dialog = new UnsavedLayoutWindow { Owner = this };
        if (dialog.ShowDialog() != true) return false;
        if (!dialog.SaveRequested) return true;
        if (SaveLayout()) return true;
        return false;
    }

    private void OnSlotDragStarted(object sender, DragStartedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: PhotoSlotViewModel slot }) return;
        Model.SelectedSlot = slot;
        Keyboard.Focus(this);
        Model.BeginGesture();
        dragStart = Mouse.GetPosition(PaperSurface);
        startX = slot.X; startY = slot.Y; startWidth = slot.Width; startHeight = slot.Height;
        e.Handled = true;
    }
    private void OnSlotMove(object sender, DragDeltaEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: PhotoSlotViewModel slot }) return;
        if (slot.Locked || Model.Preview) return;
        Point position = Mouse.GetPosition(PaperSurface);
        slot.X = startX + position.X - dragStart.X;
        slot.Y = startY + position.Y - dragStart.Y;
        e.Handled = true;
    }
    private void OnSlotResize(object sender, DragDeltaEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: PhotoSlotViewModel slot }) return;
        if (slot.Locked || Model.Preview) return;
        Point position = Mouse.GetPosition(PaperSurface);
        double angle = slot.Rotation * Math.PI / 180;
        double dx = position.X - dragStart.X, dy = position.Y - dragStart.Y;
        slot.Width = startWidth + dx * Math.Cos(angle) + dy * Math.Sin(angle);
        if (!slot.KeepAspect) slot.Height = startHeight - dx * Math.Sin(angle) + dy * Math.Cos(angle);
        e.Handled = true;
    }
    private void OnRotationEnter(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || sender is not System.Windows.Controls.TextBox input) return;
        Model.BeginGesture();
        var binding = input.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty);
        binding?.UpdateSource();
        if (!System.Windows.Controls.Validation.GetHasError(input)) binding?.UpdateTarget();
        Model.EndGesture(); e.Handled = true;
    }
    private void OnEditorKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Delete || !Model.IsLayout || Model.Preview || Keyboard.FocusedElement is System.Windows.Controls.TextBox || Keyboard.FocusedElement is System.Windows.Controls.Primitives.TextBoxBase || Keyboard.FocusedElement is System.Windows.Controls.ComboBox) return;
        if (Model.SelectedSlot is not { Locked: false }) return;
        Model.BeginGesture(); Model.RemoveSlotCommand.Execute(null); Model.EndGesture(); e.Handled = true;
    }
    private void OnCanvasBackground(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource == PaperSurface || e.OriginalSource == ZoomPaper || e.OriginalSource == sender) { Model.SelectedSlot = null; Keyboard.Focus(this); }
    }
    private void OnSlotDragCompleted(object sender, DragCompletedEventArgs e) => Model.EndGesture();
    private void OnGallerySelected(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (updatingGallery || e.AddedItems.Count == 0 || e.AddedItems[0] is not LayoutGalleryItem item) return;
        if (item.Path is not null && item.Path == libraryPath) return;
        if (item.Preset is not null) Model.SelectedPreset = item.Preset;
        else if (item.Path is not null && ConfirmDiscard()) OpenLayoutFile(item.Path, true);
        Dispatcher.BeginInvoke(new Action(FitCanvas));
    }
    private void OnResizeTools(object sender, DragDeltaEventArgs e) { double width = Math.Clamp(ToolsColumn.ActualWidth + e.HorizontalChange, 0, 360); ToolsColumn.Width = new GridLength(width); ToolsPanel.Visibility = width < 45 ? Visibility.Hidden : Visibility.Visible; }
    private void OnResizeInspector(object sender, DragDeltaEventArgs e) { double width = Math.Clamp(InspectorColumn.ActualWidth - e.HorizontalChange, 0, 420); InspectorColumn.Width = new GridLength(width); InspectorPanel.Visibility = width < 45 ? Visibility.Hidden : Visibility.Visible; }
    private void OnToggleTools(object sender, RoutedEventArgs e) { bool show = ToolsPanel.Visibility != Visibility.Visible; ToolsPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed; ToolsColumn.Width = new GridLength(show ? 180 : 0); }
    private void OnToggleInspector(object sender, RoutedEventArgs e) { bool show = InspectorPanel.Visibility != Visibility.Visible; InspectorPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed; InspectorColumn.Width = new GridLength(show ? 250 : 0); }
    private string HiddenPresetsPath => Path.Combine(PrintLayoutStore.LibraryDirectory, "hidden-presets.json");
    private HashSet<string> HiddenPresets() { try { return JsonSerializer.Deserialize<HashSet<string>>(File.ReadAllText(HiddenPresetsPath)) ?? []; } catch (IOException) { return []; } catch (JsonException) { return []; } }
    private void OnDeleteLayout(object sender, RoutedEventArgs e)
    {
        if (TemplateGallery.SelectedItem is not LayoutGalleryItem item) return;
        if (Model.Gallery.Count <= 1) { Model.Warn("Phải giữ lại ít nhất một mẫu."); return; }
        if (MessageBox.Show(this, "Bạn có muốn xóa bố cục này không?", "Xóa bố cục", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes) return;
        try
        {
            if (item.Path is not null) File.Delete(item.Path);
            else { var hidden = HiddenPresets(); hidden.Add(item.Name); Directory.CreateDirectory(PrintLayoutStore.LibraryDirectory); File.WriteAllText(HiddenPresetsPath, JsonSerializer.Serialize(hidden)); }
            libraryPath = null; savedLayout = Snapshot(); RefreshLibrary();
            TemplateGallery.SelectedItem = Model.Gallery.FirstOrDefault();
        }
        catch (IOException error) { Model.Warn(error.Message); }
    }
    private void OnMore(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && button.ContextMenu is { } menu)
        { menu.PlacementTarget = button; menu.DataContext = Model; menu.IsOpen = true; }
    }
    private void OnTrialImage(object sender, RoutedEventArgs e)
    {
        if (Model.SelectedSlot is not { IsPhoto: true } slot) { Model.Warn("Chọn ô ảnh khách để chèn ảnh thử."); return; }
        var picker = new OpenFileDialog { Filter = "Ảnh thử|*.png;*.jpg;*.jpeg" };
        if (picker.ShowDialog(this) != true) return;
        try
        {
            if (new FileInfo(picker.FileName).Length > 10_000_000) throw new InvalidDataException("Chọn ảnh nhỏ hơn 10 MB.");
            byte[] bytes = File.ReadAllBytes(picker.FileName);
            using var stream = new MemoryStream(bytes);
            _ = BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            slot.SetTestPhoto(bytes);
            Model.Warn("Đang xem ảnh thử trong ô. Ảnh thử không lưu vào template.");
        }
        catch (Exception error) { Model.Warn($"Không mở được ảnh thử: {error.Message}"); }
    }
    internal static BitmapImage DecodeArtwork(byte[] bytes) => ArtworkImport.Read(bytes).Image;
    private void OnImportArtwork(object sender, RoutedEventArgs e)
    {
        var picker = new OpenFileDialog { Filter = ArtworkImport.FileFilter, Title = "Nhập khung ảnh" };
        if (picker.ShowDialog(this) != true) return;
        try
        {
            var imported = ArtworkImport.ReadFile(picker.FileName);
            Model.BeginGesture();
            Model.SetArtwork(imported.Bytes, imported.Image);
            Model.CutIntoStrips = false;
            Model.ApplyPaperFormat(imported.Strips, imported.Image.PixelWidth > imported.Image.PixelHeight);
            Model.ArtworkBehindPhotos = imported.Opaque;
            Model.EndGesture();
            FitCanvas();
            Model.Status = $"Đã nhận diện khung {Model.PaperSizeLabel} · {(Model.OrientationIndex == 1 ? "Ngang" : "Dọc")}.";
        }
        catch (Exception error) { Model.Warn($"Không nhập được khung: {error.Message}"); }
    }
    private async void OnScanArtwork(object sender, RoutedEventArgs e)
    {
        if (!Model.HasArtwork) { OnCreateFrameFromImage(sender, e); return; }
        var button = sender as System.Windows.Controls.Button;
        if (button is not null) button.IsEnabled = false;
        var before = Snapshot();
        var layout = Model.ExportLayout();
        Model.Status = "Đang quét ô ảnh…";
        ScanLoading.Visibility = Visibility.Visible;
        try
        {
            var sourceLayout = layout.ArtworkPng == scannedArtwork && originalScanArtwork is not null
                ? layout with { ArtworkPng = originalScanArtwork } : layout;
            var result = await Task.Run(() => CreateFrameFromImage.Scan(sourceLayout));
            if (!IsLoaded) return;
            if (before != Snapshot()) { Model.Warn("Bố cục đã thay đổi. Bấm Quét ô ảnh để quét lại."); return; }
            if (!CreateFrameFromImage.Review(this, result, scan: true)) { Model.Status = "Đã hủy quét ô ảnh."; return; }
            Model.BeginGesture();
            try
            {
                Model.LoadLayout(result.Layout);
                Model.SetArtwork(Convert.FromBase64String(result.Layout.ArtworkPng!), result.Artwork);
                originalScanArtwork = sourceLayout.ArtworkPng;
                scannedArtwork = result.Layout.ArtworkPng;
                Model.Preview = false;
            }
            finally { Model.EndGesture(); }
            FitCanvas();
            Model.Status = $"Đã cập nhật {result.Detection.Slots.Length} ô ảnh.";
        }
        catch (Exception error) { Model.Warn(error.Message); }
        finally { ScanLoading.Visibility = Visibility.Collapsed; if (button is not null) button.IsEnabled = true; }
    }
    private async void OnCreateFrameFromImage(object sender, RoutedEventArgs e)
    {
        var picker = new OpenFileDialog { Filter = ArtworkImport.FileFilter, Title = "Tạo khung từ ảnh" };
        if (picker.ShowDialog(this) != true) return;
        var button = sender as System.Windows.Controls.Button;
        if (button is not null) button.IsEnabled = false;
        Model.Status = "Đang quét ô ảnh…";
        ScanLoading.Visibility = Visibility.Visible;
        try
        {
            var result = await Task.Run(() => CreateFrameFromImage.Read(picker.FileName));
            if (!IsLoaded || !CreateFrameFromImage.Review(this, result) || !ConfirmDiscard()) return;
            libraryPath = null;
            Model.LoadLayout(result.Layout);
            Model.SetArtwork(Convert.FromBase64String(result.Layout.ArtworkPng!), result.Artwork);
            Model.Preview = false;
            Model.ResetHistory(); FitCanvas(); RefreshLibrary();
            Model.Status = $"Đã tạo {result.Layout.Slots.Length} ô ảnh. Lưu để thêm vào thư viện mẫu.";
        }
        catch (Exception error) { Model.Warn(error.Message); }
        finally { ScanLoading.Visibility = Visibility.Collapsed; if (button is not null) button.IsEnabled = true; }
    }
    private void OnClearArtwork(object sender, RoutedEventArgs e) => Model.SetArtwork(null, null);
    private void OnSaveLayout(object sender, RoutedEventArgs e) => SaveLayout();
    private bool SaveLayout()
    {
        CommitLayoutInputs(LayoutEditorRoot);
        if (!CheckInputs()) return false;
        try
        {
            var layout = Model.ExportLayout();
            string destination = libraryPath ?? Path.Combine(PrintLayoutStore.LibraryDirectory, Guid.NewGuid() + ".fotolayout");
            layoutStore.Save(destination, layout);
            libraryPath = destination;
            layoutStore.Save(editorPath, layout);
            savedLayout = JsonSerializer.Serialize(layout);
            RefreshLibrary();
            Model.Status = "Đã lưu vào danh sách bố cục. Mở lại ứng dụng vẫn giữ mẫu này.";
            return true;
        }
        catch (Exception error) { Model.Warn($"Không lưu được mẫu: {error.Message}"); return false; }
    }
    private void RefreshLibrary()
    {
        updatingGallery = true;
        Model.SavedLayouts.Clear(); Model.Gallery.Clear();
        foreach (var preset in Model.Presets.Where(p => !HiddenPresets().Contains(p.Name)))
        {
            var layout = preset.CreateLayout();
            Model.Gallery.Add(new(preset.Name, LayoutThumbnail.Create(layout), Preset: preset));
        }
        if (!Directory.Exists(PrintLayoutStore.LibraryDirectory)) { SelectCurrentGallery(); return; }
        foreach (string path in Directory.EnumerateFiles(PrintLayoutStore.LibraryDirectory, "*.fotolayout").OrderByDescending(File.GetLastWriteTimeUtc))
        {
            try
            {
                var layout = LegacyStripMigration.Convert(layoutStore.Load(path));
                Model.SavedLayouts.Add(new(path, layout.Name));
                Model.Gallery.Add(new(layout.Name, LayoutThumbnail.Create(layout), Path: path));
                if (libraryPath is null && JsonSerializer.Serialize(layout) == Snapshot()) libraryPath = path;
            }
            catch (Exception error) when (error is IOException or JsonException or ArgumentException)
            { Model.Warn($"Có mẫu không đọc được: {Path.GetFileName(path)}"); }
        }
        SelectCurrentGallery();
    }
    private void SelectCurrentGallery()
    {
        TemplateGallery.SelectedItem = Model.Gallery.FirstOrDefault(item => libraryPath is not null ? item.Path == libraryPath : item.Preset is not null && item.Preset == Model.SelectedPreset);
        updatingGallery = false;
    }
    private void OnOpenSaved(object sender, RoutedEventArgs e)
    {
        var selected = Model.SelectedSavedLayout;
        if (selected is null || !ConfirmDiscard()) return;
        OpenLayoutFile(selected.Path, true);
    }
    private void OpenLayoutFile(string path, bool fromLibrary)
    {
        try
        {
            var layout = LegacyStripMigration.Convert(layoutStore.Load(path));
            byte[]? bytes = layout.ArtworkPng is null ? null : Convert.FromBase64String(layout.ArtworkPng);
            var image = bytes is null ? null : DecodeArtwork(bytes);
            Model.LoadLayout(layout); Model.SetArtwork(bytes, image); Model.Preview = false; FitCanvas();
            libraryPath = fromLibrary ? path : null;
            savedLayout = Snapshot(); Model.ResetHistory();
            Model.Status = "Đã mở bố cục.";
        }
        catch (Exception error) { Model.Warn($"Không mở được mẫu: {error.Message}"); }
    }
    private void OnUseLayout(object sender, RoutedEventArgs e)
    {
        if (!SaveLayout()) return;
        try
        {
            layoutStore.Publish(libraryPath!, Model.ExportLayout()); savedLayout = Snapshot();
            Model.Status = "Đã phát hành cho khách. Mở lại cửa sổ khách để áp dụng.";
        }
        catch (Exception error) { Model.Warn($"Không áp dụng được mẫu: {error.Message}"); }
    }
    private bool CheckInputs()
    {
        if (!Model.Slots.Any(s => s.IsPhoto)) { Model.Warn("Thêm ít nhất một ô ảnh vào khung trước khi lưu."); return false; }
        bool HasError(DependencyObject node)
        {
            if (System.Windows.Controls.Validation.GetHasError(node)) return true;
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(node); i++)
                if (HasError(System.Windows.Media.VisualTreeHelper.GetChild(node, i))) return true;
            return false;
        }
        if (!HasError(LayoutEditorRoot)) return true;
        Model.Warn("Kiểm tra lại các ô nhập số trước khi lưu.");
        return false;
    }
    private static void CommitLayoutInputs(DependencyObject node)
    {
        if (node is System.Windows.Controls.TextBox input)
        {
            var binding = input.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty);
            if (binding is { IsDirty: true }) binding.UpdateSource();
        }
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(node); i++)
            CommitLayoutInputs(System.Windows.Media.VisualTreeHelper.GetChild(node, i));
    }
    private void ShowWarning(string message)
    {
        if (!IsLoaded)
        {
            RoutedEventHandler? show = null;
            show = (_, _) => { Loaded -= show; ShowWarning(message); };
            Loaded += show;
            return;
        }
        ScanLoading.Visibility = Visibility.Collapsed;
        new WarningWindow(this, message).ShowDialog();
    }
    private void OnOpenLayout(object sender, RoutedEventArgs e)
    {
        var picker = new OpenFileDialog { Filter = "Mẫu FotoAutomat|*.fotolayout" };
        if (picker.ShowDialog(this) != true || !ConfirmDiscard()) return;
        OpenLayoutFile(picker.FileName, false);
    }


    private bool fitMode = true;
    private void OnViewportSized(object sender, SizeChangedEventArgs e) { if (fitMode) FitCanvas(); }
    private void FitCanvas()
    {
        if (CanvasViewport is null || ZoomSlider is null || DataContext is not LayoutEditorViewModel) return;
        fitMode = true;
        SetZoom(Math.Min((CanvasViewport.ActualWidth - 48) / (Model.PageWidth + 2), (CanvasViewport.ActualHeight - 48) / (Model.PageHeight + 2)) * 100);
    }
    private void SetZoom(double percent)
    {
        if (ZoomSlider is null) return;
        ZoomSlider.Value = Math.Clamp(percent, 75, 400);
    }
    private void OnFitCanvas(object sender, RoutedEventArgs e) => FitCanvas();
    private void OnZoomIn(object sender, RoutedEventArgs e) { fitMode = false; SetZoom(ZoomSlider.Value * 1.25); }
    private void OnZoomOut(object sender, RoutedEventArgs e) { fitMode = false; SetZoom(ZoomSlider.Value / 1.25); }
    private void OnActualSize(object sender, RoutedEventArgs e) { fitMode = false; SetZoom(100); }
    private void OnZoomChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ZoomPaper is null || ZoomLabel is null) return;
        ZoomPaper.LayoutTransform = new System.Windows.Media.ScaleTransform(e.NewValue / 100, e.NewValue / 100);
        ZoomLabel.Text = $"{e.NewValue:0}%";
    }
    private void OnZoomInteraction(object sender, MouseButtonEventArgs e) => fitMode = false;
    private void OnZoomKey(object sender, KeyEventArgs e) => fitMode = false;
    private void OnZoomWheel(object sender, MouseWheelEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
        {
            fitMode = false; SetZoom(ZoomSlider.Value * (e.Delta > 0 ? 1.15 : 1 / 1.15)); e.Handled = true; return;
        }
        if (Model.Preview || Model.SelectedSlot is not { Locked: false, IsVisible: true } slot) return;
        Model.BeginGesture();
        double step = (Keyboard.Modifiers & ModifierKeys.Shift) != 0 ? 0.1 : 1;
        slot.Rotation = Math.Round(((slot.Rotation + Math.Sign(e.Delta) * step + 540) % 360) - 180, 1);
        Model.EndGesture(); e.Handled = true;
    }
    private void OnImportDecoration(object sender, RoutedEventArgs e)
    {
        var picker = new OpenFileDialog { Filter = "Ảnh PNG / JPEG|*.png;*.jpg;*.jpeg" };
        if (picker.ShowDialog(this) != true) return;
        try
        {
            if (new FileInfo(picker.FileName).Length > 5_000_000) throw new InvalidDataException("Chọn ảnh nhỏ hơn 5 MB.");
            byte[] bytes = File.ReadAllBytes(picker.FileName);
            using var stream = new MemoryStream(bytes);
            var bitmap = BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            if (bitmap.Frames.Count == 0) throw new InvalidDataException("Ảnh không hợp lệ.");
            Model.AddDecoration(FotoAutomat.Core.LayoutElementKind.Image, Convert.ToBase64String(bytes));
            if (Model.SelectedSlot is { } added)
            {
                double ratio = bitmap.Frames[0].PixelWidth / (double)bitmap.Frames[0].PixelHeight;
                double width = Math.Min(160, Model.PageWidth - 40), height = width / ratio;
                if (height > 500) { height = 500; width = height * ratio; }
                added.Width = width; added.Height = height; added.KeepAspect = true;
            }
        }
        catch (Exception error) { Model.Warn($"Không nhập được ảnh: {error.Message}"); }
    }
    private async void OnCustomer(object sender, RoutedEventArgs e)
    {
        if (!new GeneralSettingsStore().HasPin) { Model.Section = "Quyền quản trị"; Model.Warn("Thiết lập PIN trước khi mở giao diện khách."); return; }
        // Trang Máy ảnh có thể đang giữ USB; giao diện khách cần tự kết nối.
        try { await FujifilmPageCameraService.ReleaseActiveAsync(); }
        catch (Exception error) { Model.Warn("Không ngắt được máy ảnh của trang quản trị: " + error.Message); return; }
        OpenOwned<MainWindow>();
        if (OwnedWindows.OfType<MainWindow>().FirstOrDefault() is { } customer) { customer.Owner = null; Hide(); customer.Show(); customer.Activate(); }
    }
    private void OpenOwned<T>() where T : Window, new()
    {
        T? existing = OwnedWindows.OfType<T>().FirstOrDefault();
        if (existing is not null) { existing.Activate(); return; }
        try { new T { Owner = this }.Show(); }
        catch (Exception error)
        {
            MessageBox.Show(this, $"Không mở được cửa sổ: {error.GetBaseException().Message}", "FotoAutomat");
        }
    }
}
