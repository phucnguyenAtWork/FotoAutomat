using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace FotoAutomat.Desktop;

/// <summary>Ô số dạng [-] giá trị [+] cho màn hình cảm ứng. Giá trị luôn nằm trong [Minimum, Maximum].</summary>
public partial class NumericStepper : UserControl
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(double), typeof(NumericStepper),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged, CoerceToRange));
    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum), typeof(double), typeof(NumericStepper), new PropertyMetadata(0d, OnRangeChanged));
    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(double), typeof(NumericStepper), new PropertyMetadata(100d, OnRangeChanged));
    public static readonly DependencyProperty StepProperty = DependencyProperty.Register(
        nameof(Step), typeof(double), typeof(NumericStepper), new PropertyMetadata(1d));
    public static readonly DependencyProperty UnitProperty = DependencyProperty.Register(
        nameof(Unit), typeof(string), typeof(NumericStepper), new PropertyMetadata("", OnDisplayChanged));
    public static readonly DependencyProperty FormatProperty = DependencyProperty.Register(
        nameof(Format), typeof(string), typeof(NumericStepper), new PropertyMetadata("0.##", OnDisplayChanged));

    private static readonly CultureInfo VietnameseNumbers = CultureInfo.GetCultureInfo("vi-VN");

    public NumericStepper()
    {
        InitializeComponent();
        Refresh();
    }

    /// <summary>Xảy ra khi Value đổi (do bấm nút hoặc do mã đặt).</summary>
    public event EventHandler? ValueChanged;

    public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public double Minimum { get => (double)GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
    public double Maximum { get => (double)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
    public double Step { get => (double)GetValue(StepProperty); set => SetValue(StepProperty, value); }
    public string Unit { get => (string)GetValue(UnitProperty); set => SetValue(UnitProperty, value); }
    public string Format { get => (string)GetValue(FormatProperty); set => SetValue(FormatProperty, value); }

    private static object CoerceToRange(DependencyObject d, object baseValue)
    {
        var stepper = (NumericStepper)d;
        double value = (double)baseValue;
        if (double.IsNaN(value)) value = stepper.Minimum;
        double upper = Math.Max(stepper.Minimum, stepper.Maximum);
        return Math.Min(Math.Max(value, stepper.Minimum), upper);
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var stepper = (NumericStepper)d;
        stepper.Refresh();
        stepper.ValueChanged?.Invoke(stepper, EventArgs.Empty);
    }

    private static void OnRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var stepper = (NumericStepper)d;
        stepper.CoerceValue(ValueProperty);
        stepper.Refresh();
    }

    private static void OnDisplayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((NumericStepper)d).Refresh();

    private void Refresh()
    {
        if (Readout is null || Minus is null || Plus is null) return;
        // Luôn theo cách viết số tiếng Việt (90.000 đ, 0,5 mm), không phụ thuộc ngôn ngữ của Windows.
        string text = Value.ToString(string.IsNullOrEmpty(Format) ? "0.##" : Format, VietnameseNumbers);
        Readout.Text = string.IsNullOrEmpty(Unit) ? text : text + " " + Unit;
        Minus.IsEnabled = Value > Minimum;
        Plus.IsEnabled = Value < Maximum;
    }

    private void OnMinus(object sender, RoutedEventArgs e) => Value = Math.Round(Value - Step, 4);

    private void OnPlus(object sender, RoutedEventArgs e) => Value = Math.Round(Value + Step, 4);
}
