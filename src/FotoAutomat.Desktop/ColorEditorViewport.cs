using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using FotoAutomat.Infrastructure;

namespace FotoAutomat.Desktop;
public sealed partial class ColorEditorPanel
{
    private double zoom = 1, panX, panY;
    private Point? panStart;
    private void ResetView() { zoom = 1; panX = panY = 0; TransformView(); }
    private FrameworkElement? imageSurface;
    private void TransformView()
    {
        if (imageSurface is { } surface) EditorImageViewport.Apply(surface, session.Original, ref zoom, ref panX, ref panY, 8, preview, maskOverlay);
    }
    private void WireViewInput(FrameworkElement surface)
    {
        imageSurface = surface; surface.SizeChanged += (_, _) => TransformView();
        void Move(Point point) { if (panStart is not { } start) return; panX += point.X - start.X; panY += point.Y - start.Y; panStart = point; TransformView(); }
        surface.MouseWheel += (_, e) => { zoom = Math.Clamp(zoom * (e.Delta > 0 ? 1.2 : 1 / 1.2), 1, 8); if (zoom == 1) panX = panY = 0; TransformView(); e.Handled = true; };
        surface.MouseLeftButtonDown += (_, e) => { if (painting) return; if (e.ClickCount == 2) { ResetView(); return; } panStart = e.GetPosition(surface); surface.CaptureMouse(); e.Handled = true; };
        surface.MouseMove += (_, e) => { if (!painting && surface.IsMouseCaptured && e.LeftButton == MouseButtonState.Pressed) Move(e.GetPosition(surface)); };
        surface.MouseLeftButtonUp += (_, _) => { panStart = null; surface.ReleaseMouseCapture(); };
        surface.TouchDown += (_, e) => { if (painting) return; panStart = e.GetTouchPoint(surface).Position; surface.CaptureTouch(e.TouchDevice); e.Handled = true; };
        surface.TouchMove += (_, e) => { if (!painting && e.TouchDevice.Captured == surface) Move(e.GetTouchPoint(surface).Position); };
        surface.TouchUp += (_, e) => { panStart = null; surface.ReleaseTouchCapture(e.TouchDevice); };
    }
    private void ImportPresets()
    {
        var dialog = new OpenFileDialog { Filter = "Lightroom / LUT|*.xmp;*.cube", Multiselect = true, Title = "Nhập preset Lightroom" };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;
        var messages = new List<string>(); int imported = 0;
        foreach (var path in dialog.FileNames)
        {
            try { var result = LightroomPresetImporter.Import(path); selected = session.AddPreset(result.Recipe); imported++; messages.AddRange(result.Warnings.Select(w => Path.GetFileName(path) + ": " + w)); }
            catch (Exception e) { messages.Add(Path.GetFileName(path) + ": " + e.Message); }
        }
        selectedLayer = -1; painting = false; BuildControls(); Refresh();
        if (messages.Count > 0) new WarningWindow(Window.GetWindow(this), $"Đã nhập {imported} preset vào bản nháp.\n" + string.Join("\n", messages)).ShowDialog();
    }
    private void SavePreset()
    {
        var name = new TextBox { Text = session.Workspace.Looks[selected].Name, MaxLength = 80, Margin = new Thickness(0, 8, 0, 12), Height = EditorUi.ControlHeight, VerticalContentAlignment = VerticalAlignment.Center };
        var dialog = new Window { Title = "Lưu preset", Owner = Window.GetWindow(this), Width = 360, SizeToContent = SizeToContent.Height, ResizeMode = ResizeMode.NoResize, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var panel = new StackPanel { Margin = new Thickness(20) }; panel.Children.Add(new TextBlock { Text = "Tên preset" }); panel.Children.Add(name);
        var error = new TextBlock { Foreground = Brushes.Red, TextWrapping = TextWrapping.Wrap }; panel.Children.Add(error);
        panel.Children.Add(Button("Lưu", () =>
        {
            try
            {
                string text = name.Text.Trim(); if (text.Length == 0) throw new ArgumentException("Nhập tên preset.");
                var current = session.Workspace.Looks[selected]; var recipe = current with { Name = text };
                // A new name on an already saved preset saves a copy and leaves the original untouched; an unsaved draft
                // (e.g. a fresh Matched look) is simply renamed, so no stray draft is left behind.
                bool saved = session.Published.Looks.Any(r => r.PresetId == current.PresetId);
                if (text != current.Name && saved) selected = session.AddPreset(recipe with { PresetId = Guid.NewGuid().ToString("N") }); else session.Update(selected, recipe);
                session.Save(selected); dialog.DialogResult = true;
            }
            catch (Exception e) { error.Text = e.Message; }
        })); dialog.Content = panel; if (dialog.ShowDialog() == true) { BuildControls(); Refresh(); }
    }
}
