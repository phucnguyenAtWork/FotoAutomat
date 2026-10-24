using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace FotoAutomat.Desktop;

/// <summary>Phím đã bấm: '0'–'9', '\b' (xóa một số) hoặc '\r' (xác nhận).</summary>
public sealed class KeypadEventArgs(char key) : EventArgs
{
    public char Key { get; } = key;
}

/// <summary>Bàn phím số cảm ứng kiểu ATM dùng chung cho mở khóa PIN và nhập voucher.
/// Màu phím xác nhận lấy từ tài nguyên KeypadAccent / KeypadAccentPressed của cửa sổ chứa nó.</summary>
public sealed class NumericKeypad : UserControl
{
    private const string IconFont = "Segoe Fluent Icons, Segoe MDL2 Assets";
    private const string KeyStyles = """
        <ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
          <Style x:Key="Key" TargetType="Button">
            <Setter Property="Margin" Value="6"/>
            <Setter Property="FontSize" Value="28"/>
            <Setter Property="Background" Value="#FAF3E9"/>
            <Setter Property="BorderBrush" Value="#E5D3C4"/>
            <Setter Property="Foreground" Value="#3F2D22"/>
            <Setter Property="Focusable" Value="False"/>
            <Setter Property="Cursor" Value="Hand"/>
            <Setter Property="Template">
              <Setter.Value>
                <ControlTemplate TargetType="Button">
                  <Border x:Name="Face" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="1" CornerRadius="16">
                    <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                  </Border>
                  <ControlTemplate.Triggers>
                    <Trigger Property="IsPressed" Value="True"><Setter TargetName="Face" Property="Background" Value="#F7E0E7"/></Trigger>
                    <Trigger Property="IsEnabled" Value="False"><Setter TargetName="Face" Property="Opacity" Value="0.4"/></Trigger>
                  </ControlTemplate.Triggers>
                </ControlTemplate>
              </Setter.Value>
            </Setter>
          </Style>
          <Style x:Key="EnterKey" TargetType="Button" BasedOn="{StaticResource Key}">
            <Setter Property="Background" Value="{DynamicResource KeypadAccent}"/>
            <Setter Property="BorderBrush" Value="{DynamicResource KeypadAccent}"/>
            <Setter Property="Foreground" Value="White"/>
            <Setter Property="Template">
              <Setter.Value>
                <ControlTemplate TargetType="Button">
                  <Border x:Name="Face" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="1" CornerRadius="16">
                    <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                  </Border>
                  <ControlTemplate.Triggers>
                    <Trigger Property="IsPressed" Value="True"><Setter TargetName="Face" Property="Background" Value="{DynamicResource KeypadAccentPressed}"/></Trigger>
                    <Trigger Property="IsEnabled" Value="False"><Setter TargetName="Face" Property="Opacity" Value="0.4"/></Trigger>
                  </ControlTemplate.Triggers>
                </ControlTemplate>
              </Setter.Value>
            </Setter>
          </Style>
        </ResourceDictionary>
        """;
    private static readonly ResourceDictionary Styles = (ResourceDictionary)XamlReader.Parse(KeyStyles);

    public static readonly DependencyProperty CanEnterProperty = DependencyProperty.Register(nameof(CanEnter), typeof(bool), typeof(NumericKeypad), new PropertyMetadata(true, (d, e) => ((NumericKeypad)d).enter.IsEnabled = (bool)e.NewValue));
    public static readonly DependencyProperty CanEraseProperty = DependencyProperty.Register(nameof(CanErase), typeof(bool), typeof(NumericKeypad), new PropertyMetadata(true, (d, e) => ((NumericKeypad)d).backspace.IsEnabled = (bool)e.NewValue));
    public static readonly DependencyProperty KeyWidthProperty = DependencyProperty.Register(nameof(KeyWidth), typeof(double), typeof(NumericKeypad), new PropertyMetadata(84d, OnKeySize));
    public static readonly DependencyProperty KeyHeightProperty = DependencyProperty.Register(nameof(KeyHeight), typeof(double), typeof(NumericKeypad), new PropertyMetadata(72d, OnKeySize));

    private readonly UniformGrid keys = new() { Columns = 3, HorizontalAlignment = HorizontalAlignment.Center };
    private readonly Button enter, backspace;

    public event EventHandler<KeypadEventArgs>? KeyPressed;
    public bool CanEnter { get => (bool)GetValue(CanEnterProperty); set => SetValue(CanEnterProperty, value); }
    public bool CanErase { get => (bool)GetValue(CanEraseProperty); set => SetValue(CanEraseProperty, value); }
    public double KeyWidth { get => (double)GetValue(KeyWidthProperty); set => SetValue(KeyWidthProperty, value); }
    public double KeyHeight { get => (double)GetValue(KeyHeightProperty); set => SetValue(KeyHeightProperty, value); }

    public NumericKeypad()
    {
        Resources.MergedDictionaries.Add(Styles);
        for (int n = 1; n <= 9; n++) keys.Children.Add(Key(Digit(n), "Key", n.ToString(CultureInfo.InvariantCulture), (char)('0' + n)));
        backspace = Key(Icon(""), "Key", "Xóa một số", '\b');
        keys.Children.Add(backspace);
        keys.Children.Add(Key(Digit(0), "Key", "0", '0'));
        enter = Key(Icon(""), "EnterKey", "Xác nhận", '\r');
        keys.Children.Add(enter);
        Content = keys;
        Stylus.SetIsPressAndHoldEnabled(this, false);
        ApplyKeySize();
    }

    private static TextBlock Digit(int n) => new() { Text = n.ToString(CultureInfo.InvariantCulture) };
    private static TextBlock Icon(string glyph) => new() { Text = glyph, FontFamily = new FontFamily(IconFont), FontSize = 26 };

    private Button Key(TextBlock face, string style, string name, char key)
    {
        var button = new Button { Content = face, Style = (Style)Styles[style] };
        AutomationProperties.SetName(button, name);
        button.Click += (_, _) => KeyPressed?.Invoke(this, new KeypadEventArgs(key));
        return button;
    }

    private static void OnKeySize(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((NumericKeypad)d).ApplyKeySize();

    private void ApplyKeySize()
    {
        foreach (Button key in keys.Children) { key.Width = KeyWidth; key.Height = KeyHeight; }
    }
}
