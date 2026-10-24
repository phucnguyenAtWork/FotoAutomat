using FotoAutomat.Core;
using FotoAutomat.Infrastructure;
using System.Windows.Input;

namespace FotoAutomat.Desktop.ViewModels;

public sealed partial class LayoutEditorViewModel
{
    private bool pairAndCut = true;
    private string printCopies = "1";
    public bool IsPrinterSettings => Section is "Máy in";
    public bool PairAndCut { get => pairAndCut; set { pairAndCut = value; Changed(); } }
    public string PrintCopies { get => printCopies; set { printCopies = value; Changed(); } }
    public ICommand SavePrinterCommand => new EditorCommand(_ =>
    {
        try
        {
            if (!int.TryParse(PrintCopies, out int copies)) throw new ArgumentException("Nhập số bản in từ 1 đến 10.");
            var store = new PrinterConfigurationStore();
            store.Save(store.Load() with { PairAndCut = PairAndCut, Copies = copies });
            Status = "Đã lưu thiết lập máy in. Chưa gửi lệnh đến driver hoặc máy cắt.";
        }
        catch (Exception error) { Warn($"Không lưu được cài đặt in: {error.Message}"); }
    });
    public ICommand PreviewPrintPlanCommand => new EditorCommand(_ =>
    {
        try
        {
            if (!int.TryParse(PrintCopies, out int copies)) throw new ArgumentException("Số bản in không hợp lệ.");
            var offsets = new PrinterConfiguration(PairAndCut, copies).PlacementOffsets(ExportLayout());
            Status = offsets.Length == 2 ? $"Kế hoạch: {copies} tờ 4 × 6, mỗi tờ hai bản của mẫu 2 × 6. Chưa in thật." : $"Kế hoạch: {copies} tờ, một mẫu mỗi tờ. Chưa in thật.";
        }
        catch (Exception error) { Warn(error.Message); }
    });
    private void LoadPrinterSettings()
    {
        try { var config = new PrinterConfigurationStore().Load(); PairAndCut = config.PairAndCut; PrintCopies = config.Copies.ToString(); }
        catch (Exception error) { Warn($"Không đọc được cài đặt in: {error.Message}"); }
    }
}
