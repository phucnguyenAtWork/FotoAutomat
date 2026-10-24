using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FotoAutomat.Desktop;

internal sealed class WarningWindow : Window
{
    public WarningWindow(Window owner, string message)
    {
        Owner = owner; Title = "Không thể hoàn tất thao tác";
        Width = 490; SizeToContent = SizeToContent.Height; MaxHeight = 600;
        ResizeMode = ResizeMode.NoResize; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false; Background = Brushes.White;
        var red = new SolidColorBrush(Color.FromRgb(185, 28, 28));
        var panel = new StackPanel { Margin = new Thickness(24) };
        var heading = new StackPanel { Orientation = Orientation.Horizontal };
        heading.Children.Add(new TextBlock { Text = "⚠", Foreground = red, FontSize = 28, Margin = new Thickness(0, 0, 12, 0) });
        heading.Children.Add(new TextBlock { Text = "Cần kiểm tra", Foreground = red, FontSize = 21, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center });
        panel.Children.Add(heading);
        panel.Children.Add(new ScrollViewer { MaxHeight = 360, VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, Foreground = red, FontSize = 14, Margin = new Thickness(0, 16, 0, 24) } });
        var close = new Button { Content = "Đã hiểu", IsDefault = true, IsCancel = true, MinWidth = 100, MinHeight = 44, HorizontalAlignment = HorizontalAlignment.Right };
        close.Click += (_, _) => DialogResult = true;
        panel.Children.Add(close); Content = panel;
    }
}
