namespace FotoAutomat.Desktop;

// Nối trang Thư viện khung với trình chỉnh Bố cục in. Chỉ thêm xử lý sự kiện của FrameLibraryPanel.
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
}
