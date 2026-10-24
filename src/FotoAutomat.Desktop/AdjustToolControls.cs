using System.Windows;
using System.Windows.Controls;
using FotoAutomat.Core;

namespace FotoAutomat.Desktop;

public sealed partial class ColorEditorPanel
{
    private void SelectMode(StackPanel panel, string label, string[] values, int selectedIndex, Action<int> change)
    {
        var row = new Grid { Margin = new Thickness(0, 2, 0, 6) };
        row.ColumnDefinitions.Add(new() { Width = new GridLength(88) }); row.ColumnDefinitions.Add(new());
        row.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, FontSize = 12 });
        var choice = EditorUi.Combo(values, selectedIndex); Grid.SetColumn(choice, 1); row.Children.Add(choice);
        choice.SelectionChanged += (_, _) => { if (choice.SelectedIndex >= 0) change(choice.SelectedIndex); }; panel.Children.Add(row);
    }
    private void BuildLightControls()
    {
        var wbGroup = Group("White Balance"); var wbBody = new StackPanel(); wbGroup.Children.Add(wbBody);
        void BuildWb()
        {
            wbBody.Children.Clear(); var r = CurrentRecipe;
            var wb = r.WhiteBalance ?? new WhiteBalanceSettings(r.Temperature is not null || r.Warmth != 0 || r.Tint != 0 ? WhiteBalanceMode.Custom : WhiteBalanceMode.Shot,
                r.Temperature ?? 5000, Math.Clamp(r.Tint, -50, 50));
            var mode = EditorUi.Combo(["Shot", "Custom"], (int)wb.Mode); wbBody.Children.Add(mode);
            mode.SelectionChanged += (_, _) => { UpdateRecipe(CurrentRecipe with { WhiteBalance = (CurrentRecipe.WhiteBalance ?? wb) with { Mode = (WhiteBalanceMode)mode.SelectedIndex } }); };
            ColorRecipe Edit(ColorRecipe recipe, Func<WhiteBalanceSettings, WhiteBalanceSettings> edit)
            {
                var next = edit(recipe.WhiteBalance ?? wb) with { Mode = WhiteBalanceMode.Custom };
                mode.SelectedIndex = 1; return recipe with { WhiteBalance = next };
            }
            AddSlider("Kelvin", 800, 140000, wb.Kelvin, (r, v) => Edit(r, x => x with { Kelvin = v }), wbBody, reset: 5000);
            AddSlider("Tint", -50, 50, wb.Tint, (r, v) => Edit(r, x => x with { Tint = v }), wbBody);
            wbBody.Children.Add(Button("Đặt lại", () => { UpdateRecipe(CurrentRecipe with { WhiteBalance = new() }); BuildWb(); }));
        }
        BuildWb();
        var recipe = CurrentRecipe;
        var exposure = Group("Exposure", true);
        AddSlider("Exposure", -4, 4, recipe.Exposure, (r, v) => r with { Exposure = v }, exposure);
        AddSlider("Contrast", -50, 50, recipe.Contrast, (r, v) => r with { Contrast = v }, exposure);
        AddSlider("Brightness", -50, 50, recipe.Brightness, (r, v) => r with { Brightness = v }, exposure);
        AddSlider("Saturation", -100, 100, recipe.Saturation, (r, v) => r with { Saturation = v }, exposure);
        var hdr = Group("High Dynamic Range");
        AddSlider("Highlight", -100, 100, recipe.Highlights, (r, v) => r with { Highlights = v }, hdr);
        AddSlider("Shadow", -100, 100, recipe.Shadows, (r, v) => r with { Shadows = v }, hdr);
        AddSlider("White", -100, 100, recipe.Whites, (r, v) => r with { Whites = v }, hdr);
        AddSlider("Black", -100, 100, recipe.Blacks, (r, v) => r with { Blacks = v }, hdr);
    }
    private void BuildFinishingControls()
    {
        BuildBlackWhite();
        var r = CurrentRecipe;
        var clarity = Group("Clarity");
        string[] methods = ["Natural", "Punch", "Neutral", "Classic"];
        ClarityMethod[] methodValues = [ClarityMethod.Natural, ClarityMethod.Punch, ClarityMethod.Neutral, ClarityMethod.Classic];
        SelectMode(clarity, "Method", methods, Array.IndexOf(methodValues, r.ClarityMethod), i => UpdateRecipe(CurrentRecipe with { ClarityMethod = methodValues[i] }));
        AddSlider("Clarity", -100, 100, r.Clarity, (r, v) => r with { Clarity = v }, clarity);
        AddSlider("Structure", -100, 100, r.Structure, (r, v) => r with { Structure = v }, clarity);
        BuildBlurGlow();
        var dehaze = Group("Dehaze");
        AddSlider("Amount", -100, 100, r.Dehaze, (r, v) => r with { Dehaze = v }, dehaze);
        var manual = new StackPanel { Visibility = r.DehazeShadowMode == ShadowToneMode.Manual ? Visibility.Visible : Visibility.Collapsed };
        SelectMode(dehaze, "Shadow tone", ["Auto", "Manual"], (int)r.DehazeShadowMode, i => { UpdateRecipe(CurrentRecipe with { DehazeShadowMode = (ShadowToneMode)i }); manual.Visibility = i == 1 ? Visibility.Visible : Visibility.Collapsed; });
        AddSlider("Hue", 0, 360, r.DehazeShadowHue, (r, v) => r with { DehazeShadowHue = v }, manual, track: HueTrack());
        AddSlider("Saturation", 0, 100, r.DehazeShadowSaturation, (r, v) => r with { DehazeShadowSaturation = v }, manual);
        dehaze.Children.Add(manual);
        var vignette = Group("Vignetting");
        AddSlider("Amount", -4, 4, r.VignetteEv ?? 0, (r, v) => r with { VignetteEv = v, Vignette = 0 }, vignette);
        SelectMode(vignette, "Method", ["Elliptic on Crop", "Circular on Crop", "Circular"], (int)r.VignetteMethod, i => UpdateRecipe(CurrentRecipe with { VignetteMethod = (VignetteMethod)i }));
    }
    private static System.Windows.Media.Brush HueTrack() => Gradient((0, 1, 1), (60, 1, 1), (120, 1, 1), (180, 1, 1), (240, 1, 1), (300, 1, 1), (360, 1, 1));
    private void BuildBlackWhite()
    {
        var group = Group("Black & White"); var body = new StackPanel();
        var sensitivity = new StackPanel { IsEnabled = CurrentRecipe.Monochrome }; var tones = new StackPanel { IsEnabled = CurrentRecipe.Monochrome };
        var enabled = EditorUi.Check("Enable Black & White", CurrentRecipe.Monochrome); group.Children.Add(enabled);
        enabled.Click += (_, _) => { bool on = enabled.IsChecked == true; UpdateRecipe(CurrentRecipe with { Monochrome = on, BlackWhite = CurrentRecipe.BlackWhite ?? new() }); sensitivity.IsEnabled = tones.IsEnabled = on; };
        var settings = CurrentRecipe.BlackWhite ?? new();
        ColorRecipe Edit(ColorRecipe r, Func<BlackWhiteSettings, BlackWhiteSettings> edit) => r with { BlackWhite = edit(r.BlackWhite ?? new()) };

        var tabs = new TabControl(); tabs.Items.Add(new TabItem { Header = "Color Sensitivity", Content = sensitivity }); tabs.Items.Add(new TabItem { Header = "Split Tones", Content = tones }); body.Children.Add(tabs);
        string[] names = ["Red", "Yellow", "Green", "Cyan", "Blue", "Magenta"];
        for (int i = 0; i < names.Length; i++)
        {
            int index = i;
            AddSlider(names[i], -100, 100, settings.Sensitivity[i], (r, v) => Edit(r, b => { var values = b.Sensitivity.ToArray(); values[index] = v; return b with { Sensitivity = values }; }), sensitivity);
        }
        tones.Children.Add(new TextBlock { Text = "Highlights", Margin = new Thickness(0, 8, 0, 4) });
        AddSlider("Hue", 0, 360, settings.HighlightHue, (r, v) => Edit(r, b => b with { HighlightHue = v }), tones, track: HueTrack());
        AddSlider("Saturation", 0, 100, settings.HighlightSaturation, (r, v) => Edit(r, b => b with { HighlightSaturation = v }), tones, track: Gradient((0, 0, .8), (settings.HighlightHue, 1, 1)));
        tones.Children.Add(new TextBlock { Text = "Shadows", Margin = new Thickness(0, 8, 0, 4) });
        AddSlider("Hue", 0, 360, settings.ShadowHue, (r, v) => Edit(r, b => b with { ShadowHue = v }), tones, track: HueTrack());
        AddSlider("Saturation", 0, 100, settings.ShadowSaturation, (r, v) => Edit(r, b => b with { ShadowSaturation = v }), tones, track: Gradient((0, 0, .8), (settings.ShadowHue, 1, 1)));
        group.Children.Add(body);
    }
}
