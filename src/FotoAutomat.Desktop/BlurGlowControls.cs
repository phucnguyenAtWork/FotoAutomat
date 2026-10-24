using System.Windows.Controls;
using FotoAutomat.Core;

namespace FotoAutomat.Desktop;

public sealed partial class ColorEditorPanel
{
    private int blurGlowTab;

    private void BuildBlurGlow()
    {
        var group = Group("Blur & Glow");
        var tabs = new TabControl { SelectedIndex = blurGlowTab };
        var mist = new StackPanel(); var glow = new StackPanel(); var gaussian = new StackPanel();
        tabs.Items.Add(new TabItem { Header = "Mist", Content = mist });
        tabs.Items.Add(new TabItem { Header = "Glow", Content = glow });
        tabs.Items.Add(new TabItem { Header = "Gaussian", Content = gaussian });
        tabs.SelectedIndex = blurGlowTab;
        tabs.SelectionChanged += (_, e) => { if (ReferenceEquals(e.Source, tabs)) blurGlowTab = tabs.SelectedIndex; };
        group.Children.Add(tabs);
        var s = CurrentRecipe.BlurGlow ?? new();
        void Slider(StackPanel panel, string name, double value, Func<BlurGlowSettings, double, BlurGlowSettings> edit, double reset = 0, double min = 0)
            => AddSlider(name, min, 100, value, (r, v) => r with { BlurGlow = edit(r.BlurGlow ?? new(), v) }, panel, reset: reset);
        Slider(mist, "Amount", s.MistAmount, (b, v) => b with { MistAmount = v });
        Slider(mist, "Spread", s.MistSpread, (b, v) => b with { MistSpread = v }, 35);
        Slider(mist, "Haze", s.Haze, (b, v) => b with { Haze = v }, 20);
        Slider(mist, "Preserve Blacks", s.PreserveBlacks, (b, v) => b with { PreserveBlacks = v }, 70);
        Slider(glow, "Amount", s.GlowAmount, (b, v) => b with { GlowAmount = v });
        Slider(glow, "Radius", s.GlowRadius, (b, v) => b with { GlowRadius = v }, 35);
        Slider(glow, "Threshold", s.Threshold, (b, v) => b with { Threshold = v }, 70);
        Slider(glow, "Softness", s.Softness, (b, v) => b with { Softness = v }, 40);
        Slider(glow, "Warmth", s.Warmth, (b, v) => b with { Warmth = v }, min: -100);
        Slider(gaussian, "Radius", s.GaussianRadius, (b, v) => b with { GaussianRadius = v }, 20);
        Slider(gaussian, "Mix", s.GaussianMix, (b, v) => b with { GaussianMix = v });
    }
}
