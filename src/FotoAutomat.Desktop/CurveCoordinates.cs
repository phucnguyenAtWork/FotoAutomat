using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FotoAutomat.Desktop;

public sealed partial class ColorEditorPanel
{
    private static FrameworkElement CurveCoordinates(CurveEditor editor)
    {
        var row = new WrapPanel { Margin = new Thickness(0, 4, 0, 4), ToolTip = "Chọn điểm, kéo ngang/dọc hoặc nhập Input (X) và Output (Y), 0–255. Hai đầu đặt điểm đen/trắng riêng biệt." };
        var input = new TextBox { Width = 62, MinHeight = 26, IsEnabled = false, Text = "--" };
        var output = new TextBox { Width = 62, MinHeight = 26, IsEnabled = false, Text = "--" };
        foreach (var pair in new[] { ("Input X", input), ("Output Y", output) })
        {
            row.Children.Add(new TextBlock { Text = pair.Item1, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 6, 0) });
            row.Children.Add(pair.Item2);
            System.Windows.Automation.AutomationProperties.SetName(pair.Item2, pair.Item1);
        }
        void Commit()
        {
            if (editor.SelectedPoint is null) return;
            if (double.TryParse(input.Text, out var x) && double.TryParse(output.Text, out var y) && double.IsFinite(x) && double.IsFinite(y) && x is >= 0 and <= 255 && y is >= 0 and <= 255)
            {
                input.ClearValue(Control.BorderBrushProperty); output.ClearValue(Control.BorderBrushProperty);
                editor.SetSelectedCoordinates(x, y);
            }
            else { input.BorderBrush = Brushes.IndianRed; output.BorderBrush = Brushes.IndianRed; }
        }
        foreach (var field in new[] { input, output })
        {
            field.KeyDown += (_, e) => { if (e.Key == Key.Enter) { Commit(); e.Handled = true; } };
            field.LostKeyboardFocus += (_, _) => Commit();
        }
        editor.SelectionChanged += point =>
        {
            input.IsEnabled = output.IsEnabled = point is not null;
            input.Text = point is null ? "--" : (point.X * 255).ToString("0.##", CultureInfo.CurrentCulture);
            output.Text = point is null ? "--" : (point.Y * 255).ToString("0.##", CultureInfo.CurrentCulture);
        };
        return row;
    }
}
