using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using FotoAutomat.Infrastructure;

namespace FotoAutomat.Desktop;

/// <summary>Bàn phím số cảm ứng để mở khóa quản trị từ giao diện khách. Bàn phím vật lý: số, Backspace, Enter, Esc.</summary>
public sealed class PinWindow : Window
{
    private const int MaxDigits = 12;
    // Màu của giao diện khách (CustomerTheme.xaml).
    private static readonly Brush Ink = Frozen(0x3F, 0x2D, 0x22), Line = Frozen(0xE5, 0xD3, 0xC4), Wine = Frozen(0x85, 0x26, 0x40);
    private const string IconFont = "Segoe Fluent Icons, Segoe MDL2 Assets";
    private const string KeyStyles = """
        <ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
          <Style x:Key="TextKey" TargetType="Button">
            <Setter Property="Foreground" Value="#796254"/>
            <Setter Property="FontSize" Value="17"/>
            <Setter Property="MinWidth" Value="120"/>
            <Setter Property="Height" Value="48"/>
            <Setter Property="Focusable" Value="False"/>
            <Setter Property="Cursor" Value="Hand"/>
            <Setter Property="Template">
              <Setter.Value>
                <ControlTemplate TargetType="Button">
                  <Border x:Name="Face" Background="Transparent" CornerRadius="12">
                    <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                  </Border>
                  <ControlTemplate.Triggers>
                    <Trigger Property="IsPressed" Value="True"><Setter TargetName="Face" Property="Background" Value="#F7E0E7"/></Trigger>
                  </ControlTemplate.Triggers>
                </ControlTemplate>
              </Setter.Value>
            </Setter>
          </Style>
        </ResourceDictionary>
        """;

    private readonly StringBuilder digits = new();
    private readonly StackPanel dots = new() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Height = 18 };
    private readonly TextBlock error = new() { Foreground = Wine, FontSize = 14, TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap, MinHeight = 20, Margin = new Thickness(0, 14, 0, 10) };
    private readonly NumericKeypad keypad = new() { HorizontalAlignment = HorizontalAlignment.Center };

    public PinWindow()
    {
        Title = "Mở khóa quản trị"; WindowStyle = WindowStyle.None; AllowsTransparency = true; Background = Brushes.Transparent;
        ResizeMode = ResizeMode.NoResize; SizeToContent = SizeToContent.WidthAndHeight; WindowStartupLocation = WindowStartupLocation.CenterOwner; ShowInTaskbar = false;
        FontFamily = new FontFamily("Segoe UI"); UseLayoutRounding = true;
        Stylus.SetIsPressAndHoldEnabled(this, false);
        Resources.MergedDictionaries.Add((ResourceDictionary)XamlReader.Parse(KeyStyles));

        var root = new StackPanel();
        root.Children.Add(new TextBlock { Text = "Nhập mã PIN", FontFamily = new FontFamily("Georgia"), FontSize = 28, Foreground = Ink, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 20) });
        root.Children.Add(dots);
        root.Children.Add(error);

        Resources["KeypadAccent"] = Frozen(0xA9, 0x44, 0x66);
        Resources["KeypadAccentPressed"] = Wine;
        keypad.KeyPressed += (_, e) => { if (e.Key == '\b') RemoveDigit(); else if (e.Key == '\r') Submit(); else AddDigit(e.Key); };
        root.Children.Add(keypad);

        var cancel = Key(new TextBlock { Text = "Hủy" }, "TextKey", "Hủy", () => DialogResult = false);
        cancel.HorizontalAlignment = HorizontalAlignment.Center; cancel.Margin = new Thickness(0, 10, 0, 0);
        root.Children.Add(cancel);

        // Lề ngoài chừa chỗ cho bóng đổ; Grid bọc ngoài để kích thước cửa sổ gồm cả lề.
        var card = new Border
        {
            Background = Brushes.White,
            BorderBrush = Line,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(24),
            Padding = new Thickness(30, 28, 30, 18),
            Margin = new Thickness(28),
            Child = root,
            Effect = new DropShadowEffect { BlurRadius = 32, ShadowDepth = 6, Direction = 270, Opacity = 0.2, Color = Colors.Black },
        };
        var frame = new Grid(); frame.Children.Add(card); Content = frame;

        PreviewTextInput += (_, e) => { if (e.Text.Length == 1 && char.IsAsciiDigit(e.Text[0])) { AddDigit(e.Text[0]); e.Handled = true; } };
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Back) { RemoveDigit(); e.Handled = true; }
            else if (e.Key == System.Windows.Input.Key.Enter) { Submit(); e.Handled = true; }
            else if (e.Key == System.Windows.Input.Key.Escape) { DialogResult = false; e.Handled = true; }
        };
        Loaded += (_, _) => Focus();
        Closed += (_, _) => digits.Clear();
        Refresh();
    }

    private Button Key(TextBlock face, string style, string name, Action action)
    {
        var button = new Button { Content = face, Style = (Style)Resources[style] };
        AutomationProperties.SetName(button, name);
        button.Click += (_, _) => action();
        return button;
    }

    private void AddDigit(char digit)
    {
        if (digits.Length >= MaxDigits) return;
        digits.Append(digit); error.Text = ""; Refresh();
    }

    private void RemoveDigit()
    {
        if (digits.Length == 0) return;
        digits.Length--; Refresh();
    }

    private void Submit()
    {
        if (digits.Length == 0) return;
        try
        {
            if (new GeneralSettingsStore().VerifyPin(digits.ToString())) { DialogResult = true; return; }
            error.Text = "PIN không đúng.";
        }
        catch (Exception ex) { error.Text = ex.Message; }
        finally { digits.Clear(); if (IsLoaded) Refresh(); }
    }

    // Chỉ vẽ chấm cho số đã nhập: không có chấm chờ hay ngưỡng bật nút, để không lộ độ dài PIN.
    private void Refresh()
    {
        dots.Children.Clear();
        for (int i = 0; i < digits.Length; i++)
            dots.Children.Add(new Ellipse { Width = 16, Height = 16, Margin = new Thickness(7, 0, 7, 0), Fill = Ink });
        keypad.CanEnter = keypad.CanErase = digits.Length > 0;
    }

    private static SolidColorBrush Frozen(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}
