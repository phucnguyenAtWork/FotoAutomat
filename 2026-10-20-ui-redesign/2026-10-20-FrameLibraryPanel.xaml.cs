using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FotoAutomat.Core;
using FotoAutomat.Infrastructure;

namespace FotoAutomat.Desktop;

/// <summary>Một mẫu đã lưu từ Bố cục in, hiển thị thành thẻ trong thư viện khung.</summary>
public sealed record FrameCard(string Path, string Name, ImageSource Thumbnail, string Meta, bool InUse)
{
    public bool CanUse => !InUse;
}

/// <summary>
/// Thư viện khung: liệt kê các mẫu .fotolayout đã lưu từ Bố cục in. Tái dùng đúng nguồn dữ liệu và logic của Bố cục in:
/// danh sách lấy từ PrintLayoutStore.LibraryDirectory, "Dùng cho khách" ghi vào PrintLayoutStore.ActivePath,
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
        string? activeJson = null;
        try
        {
            PrintLayout? active = store.LoadActive();
            if (active is not null) activeJson = JsonSerializer.Serialize(LegacyStripMigration.Convert(active));
        }
        catch (Exception error) when (IsReadError(error)) { activeJson = null; }

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
                    bool inUse = activeJson is not null && JsonSerializer.Serialize(layout) == activeJson;
                    cards.Add(new FrameCard(path, layout.Name, LayoutThumbnail.Create(layout), meta, inUse));
                }
                catch (Exception error) when (IsReadError(error)) { unreadable++; }
            }
        }
        Cards.ItemsSource = cards;
        CountText.Text = cards.Count == 0 ? "" : $"{cards.Count} khung đã lưu";
        EmptyText.Visibility = cards.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        if (unreadable > 0) Say($"Có {unreadable} mẫu không đọc được.");
    }

    private void OnNew(object sender, RoutedEventArgs e) => EditRequested?.Invoke(this, null);

    private void OnCardAction(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not Button { Tag: string action, DataContext: FrameCard card }) return;
        e.Handled = true;
        switch (action)
        {
            case "use": Use(card); break;
            case "edit": EditRequested?.Invoke(this, card.Path); break;
            case "delete": Delete(card); break;
        }
    }

    private void Use(FrameCard card)
    {
        try
        {
            PrintLayout layout = LegacyStripMigration.Convert(store.Load(card.Path));
            store.Save(PrintLayoutStore.ActivePath, layout);
            Reload();
            Say("Đã lưu mẫu dùng cho khách. Mở lại cửa sổ khách để áp dụng.");
        }
        catch (Exception error) when (IsReadError(error)) { Say($"Không áp dụng được mẫu: {error.Message}"); }
    }

    private void Delete(FrameCard card)
    {
        if (MessageBox.Show(Window.GetWindow(this)!, "Bạn có muốn xóa bố cục này không?", "Xóa bố cục", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes) return;
        try
        {
            File.Delete(card.Path);
            Deleted?.Invoke(this, card.Path);
            Reload();
            Say("Đã xóa bố cục.");
        }
        catch (Exception error) when (IsReadError(error)) { Say($"Không xóa được: {error.Message}"); }
    }
}
