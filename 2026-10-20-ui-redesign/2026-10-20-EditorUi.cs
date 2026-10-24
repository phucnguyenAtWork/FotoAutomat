using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FotoAutomat.Desktop;

/// <summary>Shared sizes and colours for the color editor panels. Controls are 36 px high and rows 32 px (compact editor);
/// labels are Vietnamese, parameter and tool names stay in English (Capture One naming).</summary>
internal static class EditorUi
{
    public const double ControlHeight = 36, RowHeight = 32, TextSize = 13;
    public static readonly Brush Ink = Frozen(0x20, 0x27, 0x27), Muted = Frozen(0x5E, 0x6B, 0x6A), Accent = Frozen(0x0F, 0x76, 0x6E),
        Line = Frozen(0xCD, 0xD6, 0xD5), Surface = Frozen(0xF4, 0xF7, 0xF7), Danger = Frozen(0xB4, 0x23, 0x18), Warning = Frozen(0xB4, 0x53, 0x09);
    private static SolidColorBrush Frozen(byte r, byte g, byte b) { var brush = new SolidColorBrush(Color.FromRgb(r, g, b)); brush.Freeze(); return brush; }

    public static Button Button(string text, Action action, bool primary = false)
    {
        var button = new Button { Content = text, Height = ControlHeight, MinHeight = ControlHeight, FontSize = TextSize, Padding = new Thickness(12, 4, 12, 4), Margin = new Thickness(0, 0, 6, 6) };
        if (primary) { button.Background = Accent; button.Foreground = Brushes.White; button.BorderBrush = Accent; }
        button.Click += (_, _) => action(); return button;
    }
    public static CheckBox Check(string text, bool on = true) =>
        new() { Content = text, IsChecked = on, MinHeight = ControlHeight, FontSize = TextSize, VerticalContentAlignment = VerticalAlignment.Center, Foreground = Ink };
    public static ComboBox Combo(IEnumerable<string> items, int selected = 0) =>
        new() { ItemsSource = items.ToArray(), SelectedIndex = selected, Height = ControlHeight, MinHeight = ControlHeight, FontSize = TextSize, VerticalContentAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 0, 6) };
}
