using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using FotoAutomat.Core;
using FotoAutomat.Infrastructure;

namespace FotoAutomat.Desktop;

// Thẻ chế độ chụp trên giao diện khách: tiêu đề và mô tả cho từng bộ lọc đã phát hành, gắn theo PresetId.
public partial class GeneralSettingsPanel
{
    private readonly ModeCardStore modeCardStore = new();
    private readonly List<(string PresetId, Image Preview, TextBox Title, TextBox Description, CheckBox[] Features)> modeCardFields = [];
    private static readonly string[] FeatureLabels = ["LUT", "Adjust", "Blur & Glow", "Retouch", "Refine"];
    private static bool[] Flags(ModeFeatures f) => [f.Lut, f.Adjust, f.BlurGlow, f.Retouch, f.Refine];
    private static ModeFeatures Features(CheckBox[] boxes) => new(boxes[0].IsChecked == true, boxes[1].IsChecked == true, boxes[2].IsChecked == true, boxes[3].IsChecked == true, boxes[4].IsChecked == true);

    private void BuildModeCards()
    {
        ModeCardRows.Children.Clear(); modeCardFields.Clear();
        Dictionary<string, ModeCard> saved;
        try { saved = modeCardStore.Load(); }
        catch (Exception ex) { saved = []; Feedback.Text = ex.Message; }
        var looks = ColorPreviewSession.Shared.Published.Looks;
        for (int i = 0; i < looks.Length; i++)
        {
            saved.TryGetValue(looks[i].PresetId, out var card);
            var preview = new Image { Width = 112, Height = 84, Stretch = System.Windows.Media.Stretch.UniformToFill, VerticalAlignment = VerticalAlignment.Top };
            var thumb = new Border { Width = 112, Height = 84, CornerRadius = new CornerRadius(6), Background = (System.Windows.Media.Brush)FindResource("SurfaceMutedBrush"), ClipToBounds = true, Child = preview };
            var title = new TextBox { Text = card?.Title ?? "", MaxLength = ModeCardStore.MaxTitle };
            var description = new TextBox { Text = card?.Description ?? "", MaxLength = ModeCardStore.MaxDescription };
            var name = new TextBlock { Text = looks[i].Name, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8), TextTrimming = TextTrimming.CharacterEllipsis };
            var texts = new Grid();
            texts.ColumnDefinitions.Add(new() { Width = new GridLength(2, GridUnitType.Star) }); texts.ColumnDefinitions.Add(new() { Width = new GridLength(12) }); texts.ColumnDefinitions.Add(new() { Width = new GridLength(3, GridUnitType.Star) });
            var titleField = new HeaderedContentControl { Style = (Style)FindResource("FieldStyle"), Header = "Tiêu đề", Content = title };
            var descriptionField = new HeaderedContentControl { Style = (Style)FindResource("FieldStyle"), Header = "Mô tả", Content = description };
            Grid.SetColumn(descriptionField, 2); texts.Children.Add(titleField); texts.Children.Add(descriptionField);
            bool[] flags = Flags(card?.EffectiveFeatures ?? ModeFeatures.Default);
            var boxes = FeatureLabels.Select((label, n) => new CheckBox { Content = label, IsChecked = flags[n], Margin = new Thickness(0, 0, 18, 0) }).ToArray();
            var toggles = new WrapPanel { Margin = new Thickness(0, 12, 0, 0) };
            foreach (var box in boxes) toggles.Children.Add(box);
            var fields = new StackPanel { Margin = new Thickness(16, 0, 0, 0) };
            fields.Children.Add(name); fields.Children.Add(texts); fields.Children.Add(toggles);
            var row = new DockPanel { Margin = new Thickness(0, i == 0 ? 0 : 16, 0, 0) };
            DockPanel.SetDock(thumb, Dock.Left);
            row.Children.Add(thumb); row.Children.Add(fields);
            if (i > 0) ModeCardRows.Children.Add(new Border { Style = (Style)FindResource("DividerStyle"), Margin = new Thickness(0, 16, 0, 0) });
            ModeCardRows.Children.Add(row);
            modeCardFields.Add((looks[i].PresetId, preview, title, description, boxes));
        }
        ModeCardsEmpty.Visibility = looks.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        RefreshModeCardPreviews();
    }

    private void RefreshModeCardPreviews()
    {
        var previews = ColorPreviewSession.Shared.PublishedPreviews;
        for (int i = 0; i < modeCardFields.Count; i++) modeCardFields[i].Preview.Source = previews.ElementAtOrDefault(i) as BitmapSource;
    }

    private Dictionary<string, ModeCard> ModeCardsFromFields() => modeCardFields.ToDictionary(f => f.PresetId, f => new ModeCard(f.Title.Text, f.Description.Text, Features(f.Features)));

    // Chuỗi so sánh "chưa lưu": chỉ mục có chữ, theo PresetId đang phát hành.
    private string ModeCardsKey(IReadOnlyDictionary<string, ModeCard> cards) => JsonSerializer.Serialize(modeCardFields
        .Select(f => cards.TryGetValue(f.PresetId, out var c) ? new ModeCard(c.Title.Trim(), c.Description.Trim(), c.EffectiveFeatures) : new ModeCard("", "", ModeFeatures.Default))
        .ToArray());

    private string SavedModeCardsKey()
    {
        try { return ModeCardsKey(modeCardStore.Load()); }
        catch (Exception) { return ""; }
    }

    private void SaveModeCards()
    {
        var cards = ModeCardsFromFields();
        // Giữ chữ của bộ lọc đang ẩn hoặc chưa phát hành.
        foreach (var (presetId, card) in modeCardStore.Load()) cards.TryAdd(presetId, card);
        modeCardStore.Save(cards);
        ColorPreviewSession.Shared.ModesChanged();
    }
}
