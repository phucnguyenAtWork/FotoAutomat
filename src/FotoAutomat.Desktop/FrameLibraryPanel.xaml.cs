using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FotoAutomat.Core;
using FotoAutomat.Infrastructure;

namespace FotoAutomat.Desktop;

/// <summary>Một mẫu đã lưu từ Bố cục in, hiển thị thành thẻ trong thư viện khung.</summary>
public sealed record FrameCard(string Path, string Name, ImageSource Thumbnail, string Meta, bool Published, bool Outdated)
{
    public string Badge => Outdated ? "Bản phát hành cũ" : "Đã phát hành";
    public string ActionLabel => !Published ? "Phát hành" : Outdated ? "Phát hành bản mới" : "Ngừng phát hành";
}

/// <summary>
/// Thư viện khung: liệt kê các mẫu .fotolayout đã lưu từ Bố cục in. Tái dùng đúng nguồn dữ liệu và logic của Bố cục in:
/// danh sách lấy từ PrintLayoutStore.LibraryDirectory, "Phát hành" chép mẫu vào PrintLayoutStore.PublishedDirectory
/// (khách chọn trong các khung đã phát hành cùng khổ),
/// "Sửa" nhờ cửa sổ chủ mở mẫu trong Bố cục in, "Xóa" xóa file mẫu sau khi xác nhận.
/// </summary>
public partial class FrameLibraryPanel : UserControl
{
    private readonly PrintLayoutStore store = new();

    public FrameLibraryPanel() => InitializeComponent();

    /// <summary>Đường dẫn mẫu cần mở trong Bố cục in; null nghĩa là chỉ chuyển sang Bố cục in để tạo mẫu mới.</summary>
    public event EventHandler<string?>? EditRequested;

    /// <summary>Đã xóa một file mẫu (đường dẫn file), để cửa sổ chủ làm mới thư viện của trình chỉnh.</summary>
    public event EventHandler<string>? Deleted;

    private static bool IsReadError(Exception error) =>
        error is IOException or UnauthorizedAccessException or JsonException or ArgumentException or InvalidDataException or FormatException or NotSupportedException;

    private void Say(string text)
    {
        Feedback.Text = text;
        FeedbackBar.Visibility = text.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    private void OnVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true) { Say(""); Reload(); }
    }

    private void Reload()
    {
        try { store.LoadPublished(); }
        catch (Exception error) when (IsReadError(error)) { Say($"Không chuyển được khung đang dùng: {error.Message}"); }
        var cards = new List<FrameCard>();
        int unreadable = 0;
        if (Directory.Exists(PrintLayoutStore.LibraryDirectory))
        {
            foreach (string path in Directory.EnumerateFiles(PrintLayoutStore.LibraryDirectory, "*.fotolayout").OrderByDescending(File.GetLastWriteTimeUtc))
            {
                try
                {
                    PrintLayout layout = LegacyStripMigration.Convert(store.Load(path));
                    int photos = layout.Slots.Count(slot => slot.Kind == LayoutElementKind.Photo);
                    string meta = $"{layout.PageWidth / 100:0.##} × {layout.PageHeight / 100:0.##} inch · {photos} ô";
                    string? publishedJson = PublishedJson(path);
                    cards.Add(new FrameCard(path, layout.Name, LayoutThumbnail.Create(layout), meta, publishedJson is not null, publishedJson is not null && publishedJson != JsonSerializer.Serialize(layout)));
                }
                catch (Exception error) when (IsReadError(error)) { unreadable++; }
            }
        }
        Cards.ItemsSource = cards;
        int published = cards.Count(c => c.Published);
        CountText.Text = cards.Count == 0 ? "" : $"{cards.Count} khung đã lưu · {published} đang phát hành";
        EmptyText.Visibility = cards.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        if (unreadable > 0) Say($"Có {unreadable} mẫu không đọc được.");
    }

    private string? PublishedJson(string libraryPath)
    {
        string path = store.PublishedPath(libraryPath);
        if (!File.Exists(path)) return null;
        try { return JsonSerializer.Serialize(store.Load(path)); }
        catch (Exception error) when (IsReadError(error)) { return ""; }
    }

    private void OnNew(object sender, RoutedEventArgs e) => EditRequested?.Invoke(this, null);

    private void OnCardAction(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not Button { Tag: string action, DataContext: FrameCard card }) return;
        e.Handled = true;
        switch (action)
        {
            case "publish": TogglePublish(card); break;
            case "edit": EditRequested?.Invoke(this, card.Path); break;
            case "delete": Delete(card); break;
        }
    }

    private void TogglePublish(FrameCard card)
    {
        try
        {
            bool unpublish = card.Published && !card.Outdated;
            if (unpublish) store.Unpublish(card.Path);
            else store.Publish(card.Path, LegacyStripMigration.Convert(store.Load(card.Path)));
            Reload();
            Say(unpublish ? "Đã ngừng phát hành. Mở lại cửa sổ khách để áp dụng." : "Đã phát hành cho khách. Mở lại cửa sổ khách để áp dụng.");
        }
        catch (Exception error) when (IsReadError(error)) { Say($"Không phát hành được: {error.Message}"); }
    }

    private void Delete(FrameCard card)
    {
        if (MessageBox.Show(Window.GetWindow(this)!, "Bạn có muốn xóa bố cục này không?", "Xóa bố cục", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes) return;
        try
        {
            store.Unpublish(card.Path);
            File.Delete(card.Path);
            Deleted?.Invoke(this, card.Path);
            Reload();
            Say("Đã xóa bố cục.");
        }
        catch (Exception error) when (IsReadError(error)) { Say($"Không xóa được: {error.Message}"); }
    }
}
