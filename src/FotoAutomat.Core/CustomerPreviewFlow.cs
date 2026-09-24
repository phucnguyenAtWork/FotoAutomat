using System.ComponentModel;
using System.Globalization;

namespace FotoAutomat.Core;

public enum CustomerScreen { Welcome, Filter, Layout, Payment, QrPayment, CashPayment, Instructions, Capture, Selection, Printing, Delivery }

public sealed record PreviewPackage(int Quantity, int Price)
{
    public string PriceLabel => Price.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + "đ";
}

public sealed record PreviewSlot(int Index, int? Photo)
{
    public string Label => Photo is { } number ? $"Ảnh mẫu {number}" : "+";
}

/// <summary>PDF driven UI preview only. Never connected to payments or device ports.</summary>
public sealed class CustomerPreviewFlow : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public static IReadOnlyList<PreviewPackage> Packages { get; } =
        [new(2, 80000), new(4, 140000), new(6, 210000), new(8, 280000)];
    public CustomerScreen Screen { get; private set; }
    private CustomerScreen paymentScreen = CustomerScreen.QrPayment;
    public int FilterIndex { get; private set; }
    public int LayoutIndex { get; private set; }
    public int PackageIndex { get; private set; }
    public int RetakesUsed { get; private set; }
    public int CapturedCount { get; private set; }
    public int Countdown { get; private set; } = 3;
    public int SelectedPhoto { get; private set; } = 1;
    public int PrintProgress { get; private set; }
    public PreviewSlot[] Slots { get; private set; } = EmptySlots();
    public string Amount => Packages[PackageIndex].PriceLabel;
    public string LayoutName => LayoutIndex == 0 ? "Khổ nhỏ 1+1" : "Khổ lớn 1+1";
    public string OrderSummary => $"{LayoutName} · {Packages[PackageIndex].Quantity} ảnh";
    public string FilterName => $"Bộ lọc {FilterIndex + 1}";
    public string CaptureLabel => CapturedCount == 6 ? "Đã chụp đủ 6 ảnh mẫu" : $"Ảnh {CapturedCount + 1} / 6";
    public string RetakeLabel => $"CHỤP LẠI ({4 - RetakesUsed})";
    public bool CanRetake => Screen == CustomerScreen.Selection && RetakesUsed < 4;
    public bool CanSelectPhotos => Screen == CustomerScreen.Capture && CapturedCount == 6;
    public bool CanPrint => Screen == CustomerScreen.Selection && Slots.All(x => x.Photo.HasValue);
    public bool CanFinishPrint => Screen == CustomerScreen.Printing && PrintProgress == 100;
    public string SelectionHint => $"Đã chọn ảnh {SelectedPhoto}. Chạm vào một ô để đặt ảnh vào khung.";
    public string CaptureDisplay => CapturedCount == 6 ? "✓" : Countdown.ToString(CultureInfo.InvariantCulture);
    public int ProgressStep => Screen switch
    {
        CustomerScreen.Welcome => -1,
        CustomerScreen.Filter => 0,
        CustomerScreen.Layout => 1,
        CustomerScreen.Payment => 2,
        CustomerScreen.QrPayment or CustomerScreen.CashPayment => 3,
        CustomerScreen.Instructions => 4,
        CustomerScreen.Capture => 5,
        CustomerScreen.Selection => 6,
        CustomerScreen.Printing => 7,
        _ => 8
    };

    public void SelectFilter(int index) { if (Screen == CustomerScreen.Filter && index is >= 0 and < 3) { FilterIndex = index; Changed(); } }
    public void SelectLayout(int index) { if (Screen == CustomerScreen.Layout && index is >= 0 and < 2) { LayoutIndex = index; Changed(); } }
    public void SelectPackage(int index) { if (Screen == CustomerScreen.Layout && index is >= 0 and < 4) { PackageIndex = index; Changed(); } }
    public void ChoosePayment(bool cash)
    {
        if (Screen != CustomerScreen.Payment) return;
        paymentScreen = cash ? CustomerScreen.CashPayment : CustomerScreen.QrPayment;
        Screen = paymentScreen;
        Changed();
    }

    public void Next()
    {
        Screen = Screen switch
        {
            CustomerScreen.Welcome => CustomerScreen.Filter,
            CustomerScreen.Filter => CustomerScreen.Layout,
            CustomerScreen.Layout => CustomerScreen.Payment,
            CustomerScreen.QrPayment or CustomerScreen.CashPayment => CustomerScreen.Instructions,
            CustomerScreen.Instructions => CustomerScreen.Capture,
            CustomerScreen.Capture when CanSelectPhotos => CustomerScreen.Selection,
            CustomerScreen.Selection when CanPrint => CustomerScreen.Printing,
            CustomerScreen.Printing when CanFinishPrint => CustomerScreen.Delivery,
            _ => Screen
        };
        Changed();
    }

    public void Back()
    {
        Screen = Screen switch
        {
            CustomerScreen.Filter => CustomerScreen.Welcome,
            CustomerScreen.Layout => CustomerScreen.Filter,
            CustomerScreen.Payment => CustomerScreen.Layout,
            CustomerScreen.QrPayment or CustomerScreen.CashPayment => CustomerScreen.Payment,
            CustomerScreen.Capture => CustomerScreen.Instructions,
            CustomerScreen.Instructions => paymentScreen,
            _ => Screen
        };
        Changed();
    }

    public void Tick()
    {
        if (Screen == CustomerScreen.Capture && CapturedCount < 6)
        {
            if (--Countdown == 0) { CapturedCount++; Countdown = 3; }
            Changed();
        }
        else if (Screen == CustomerScreen.Printing && PrintProgress < 100)
        {
            PrintProgress = Math.Min(100, PrintProgress + 20);
            Changed();
        }
    }

    public void SelectPhoto(int number)
    {
        if (Screen != CustomerScreen.Selection || number is < 1 or > 6) return;
        SelectedPhoto = number;
        Changed();
    }

    public void AssignSlot(int index)
    {
        if (Screen != CustomerScreen.Selection || index is < 0 or > 3) return;
        Slots = Slots.Select(x => x.Index == index ? x with { Photo = SelectedPhoto } : x).ToArray();
        Changed();
    }

    public void Retake()
    {
        if (!CanRetake) return;
        RetakesUsed++;
        CapturedCount = 0;
        Countdown = 3;
        SelectedPhoto = 1;
        Slots = EmptySlots();
        Screen = CustomerScreen.Capture;
        Changed();
    }

    public void Restart()
    {
        Screen = CustomerScreen.Welcome;
        paymentScreen = CustomerScreen.QrPayment;
        FilterIndex = LayoutIndex = PackageIndex = RetakesUsed = CapturedCount = PrintProgress = 0;
        Countdown = 3;
        SelectedPhoto = 1;
        Slots = EmptySlots();
        Changed();
    }

    private static PreviewSlot[] EmptySlots() => Enumerable.Range(0, 4).Select(i => new PreviewSlot(i, null)).ToArray();
    private void Changed() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
}
