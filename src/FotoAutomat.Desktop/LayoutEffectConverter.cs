using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Effects;
using FotoAutomat.Core;

namespace FotoAutomat.Desktop;

public sealed class LayoutEffectConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is PrintLayoutSlot { Shadow: true } slot ? new DropShadowEffect
        {
            Color = (Color)ColorConverter.ConvertFromString(slot.ShadowColor), Opacity = 0.45,
            Direction = slot.ShadowAngle, ShadowDepth = slot.ShadowDistance, BlurRadius = slot.ShadowBlur
        } : null;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
