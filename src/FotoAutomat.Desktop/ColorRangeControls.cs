using System.Windows;
using System.Windows.Controls;
using FotoAutomat.Core;

namespace FotoAutomat.Desktop;

public sealed partial class ColorEditorPanel
{
    private int colorEditorTab, advancedRange, skinRange;
    private static ColorRangeAdjustment[] Ranges(ColorRecipe r, bool skin) => skin ? r.SkinColors ?? (r.SkinColor is { } v ? new[] { v } : []) : r.AdvancedColors ?? [];
    private int RangeIndex(bool skin) => skin ? skinRange : advancedRange;
    private void BuildColorEditor()
    {
        var group = Group("Color Editor"); var tabs = new System.Windows.Controls.Primitives.UniformGrid { Rows = 1 }; var body = new StackPanel();
        string[] names = ["Basic", "Advanced", "Skin Tone"];
        void Build()
        {
            CancelColorPick(); refreshColorSample = null; tabs.Children.Clear(); body.Children.Clear();
            for (int i = 0; i < names.Length; i++) { int index = i; tabs.Children.Add(Tab(names[i], i == colorEditorTab, () => { colorEditorTab = index; Build(); })); }
            if (colorEditorTab == 0) { viewSelectedColorRange = false; BuildBasicColorEditor(body); RefreshSelectedColorView(); }
            else BuildRangeControls(body, colorEditorTab == 2);
        }
        group.Children.Add(tabs); group.Children.Add(body); Build();
    }
    private void BuildRangeControls(StackPanel body, bool skin)
    {
        void Build()
        {
            ComboBox? selector = null;
            bool syncingSelector = false;
            CancelColorPick(); refreshColorSample = null; body.Children.Clear(); var ranges = Ranges(CurrentRecipe, skin);
            int index = Math.Clamp(RangeIndex(skin), 0, Math.Max(0, ranges.Length - 1)); if (skin) skinRange = index; else advancedRange = index;
            if (ranges.Length > 1)
            {
                selector = EditorUi.Combo(ranges.Select((r, i) => $"{i + 1}  ·  ΔH {r.Hue:0.#}   ΔS {r.Saturation:0.#}   ΔL {r.Lightness:0.#}"), index);
                selector.SelectionChanged += (_, _) => { if (syncingSelector || selector.SelectedIndex < 0) return; if (skin) skinRange = selector.SelectedIndex; else advancedRange = selector.SelectedIndex; Build(); }; body.Children.Add(selector);
            }
            var value = ranges.ElementAtOrDefault(index) ?? (skin ? new ColorRangeAdjustment(Width: 20, SaturationRadius: .4, Smoothness: 25) : new ColorRangeAdjustment());
            ColorRecipe Edit(ColorRecipe r, Func<ColorRangeAdjustment, ColorRangeAdjustment> edit)
            {
                
                var existing = Ranges(r, skin); var a = existing.Length > 0 ? existing.ToArray() : new[] { value }; a[index] = edit(a[index]); return skin ? r with { SkinColor = null, SkinColors = a } : r with { AdvancedColors = a };
            }
            var wheel = new ColorRangeWheel(value, skin);
            var wheelArea = new Grid { Margin = new Thickness(0, 4, 0, 8) };
            wheelArea.Children.Add(wheel);
            var picker = ColorPickerButton(skin, Build);
            picker.Style = (Style)FindResource("AdjustResetButton");
            if (picker.Content is System.Windows.Shapes.Path pickerIcon) pickerIcon.Stroke = System.Windows.Media.Brushes.LightGray;
            picker.HorizontalAlignment = HorizontalAlignment.Right; picker.VerticalAlignment = VerticalAlignment.Bottom;
            picker.Margin = new Thickness(0); wheelArea.Children.Add(picker); body.Children.Add(wheelArea);
            wheel.Edited += next => UpdateRecipe(Edit(CurrentRecipe, x => x with { CenterHue = next.CenterHue, CenterSaturation = next.CenterSaturation, Width = next.Width, Saturation = next.Saturation, Lightness = next.Lightness }));
            wheel.Released += Build;
            var delta = new TextBlock { Text = $"ΔH  {value.Hue:0.0}     ΔS  {value.Saturation:0.0}     ΔL  {value.Lightness:0.0}", Margin = new Thickness(0, 8, 0, 8), HorizontalAlignment = HorizontalAlignment.Center };
            ColorRecipe Change(ColorRecipe r, Func<ColorRangeAdjustment, ColorRangeAdjustment> edit)
            {
                var next = Edit(r, edit); var v = Ranges(next, skin)[index]; wheel.Set(v);
                delta.Text = $"ΔH  {v.Hue:0.0}     ΔS  {v.Saturation:0.0}     ΔL  {v.Lightness:0.0}";
                if (selector is not null)
                {
                    syncingSelector = true;
                    selector.ItemsSource = Ranges(next, skin).Select((x, i) => $"{i + 1}  ·  ΔH {x.Hue:0.#}   ΔS {x.Saturation:0.#}   ΔL {x.Lightness:0.#}").ToArray();
                    selector.SelectedIndex = index;
                    syncingSelector = false;
                }
                return next;
            }
            AddSlider("Smoothness", 1, 30, value.Smoothness, (r, v) => Change(r, x => x with { Smoothness = v }), body, reset: skin ? 25 : 20);
            if (skin) body.Children.Add(new TextBlock { Text = "Amount", Margin = new Thickness(0, 8, 0, 4) });
            AddSlider("Hue", -30, 30, value.Hue, (r, v) => Change(r, x => x with { Hue = v }), body, track: HueTrack());
            AddSlider("Saturation", -100, 200, value.Saturation, (r, v) => Change(r, x => x with { Saturation = v }), body);
            AddSlider("Lightness", -100, 100, value.Lightness, (r, v) => Change(r, x => x with { Lightness = v }), body);
            if (skin)
            {
                body.Children.Add(new TextBlock { Text = "Uniformity", Margin = new Thickness(0, 8, 0, 4) });
                AddSlider("Hue", 0, 100, value.UniformHue, (r, v) => Change(r, x => x with { UniformHue = v }), body);
                AddSlider("Saturation", 0, 100, value.UniformSaturation, (r, v) => Change(r, x => x with { UniformSaturation = v }), body);
                AddSlider("Lightness", 0, 100, value.UniformLightness, (r, v) => Change(r, x => x with { UniformLightness = v }), body);
            }
            body.Children.Add(delta);
            var selectedView = EditorUi.Check("View selected color range", viewSelectedColorRange);
            selectedView.Click += (_, _) => { viewSelectedColorRange = selectedView.IsChecked == true; RefreshSelectedColorView(); };
            body.Children.Add(selectedView);
            body.Children.Add(ColorSampleReadout(skin));
            body.Children.Add(Button("Đặt lại dải", () => { UpdateRecipe(Edit(CurrentRecipe, _ => new())); Build(); }));
            RefreshSelectedColorView();
        }
        Build();
    }
}
