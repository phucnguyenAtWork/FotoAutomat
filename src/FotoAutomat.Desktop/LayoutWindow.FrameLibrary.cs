using System.Windows;
using System.Windows.Input;

namespace FotoAutomat.Desktop;

// Nối trang Thư viện khung với trình chỉnh Bố cục in. Chỉ thêm xử lý sự kiện của FrameLibraryPanel.
// Thêm OnLayerListWheel: danh sách LỚP không còn thanh cuộn riêng, con lăn chuyển lên vùng cuộn của cột thuộc tính.
public partial class LayoutWindow
{
    private void OnFrameEditRequested(object? sender, string? path)
    {
        if (path is null) { Model.Section = "Bố cục in"; return; }
        if (!ConfirmDiscard()) return;
        Model.Section = "Bố cục in";
        OpenLayoutFile(path, true);
        Dispatcher.BeginInvoke(new Action(FitCanvas));
    }

    private void OnFrameDeleted(object? sender, string path)
    {
        if (libraryPath == path) libraryPath = null;
        RefreshLibrary();
    }

    private void OnLayerListWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Handled || sender is not FrameworkElement { Parent: UIElement parent }) return;
        e.Handled = true;
        parent.RaiseEvent(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta) { RoutedEvent = UIElement.MouseWheelEvent, Source = sender });
    }
}
