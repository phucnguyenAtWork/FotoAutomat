using System.Windows;
using FotoAutomat.Infrastructure;

namespace FotoAutomat.Desktop;

// Voucher trên trang Bảng giá & thanh toán: tạo lô mã dùng một lần, mã nhân viên không giới hạn, hủy mã.
public partial class ManagementPanel
{
    private readonly VoucherStore vouchers = new();

    private void LoadVouchers()
    {
        var all = vouchers.All().OrderByDescending(v => v.CreatedAt).ThenBy(v => v.Code).ToList();
        VoucherList.ItemsSource = all;
        VoucherEmpty.Visibility = all.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private (int Discount, bool Free) SelectedVoucherValue() =>
        VoucherValue.SelectedIndex is >= 0 and < 4 ? (VoucherStore.Discounts[VoucherValue.SelectedIndex], false) : (0, true);

    private void OnGenerateVouchers(object sender, RoutedEventArgs e)
    {
        try
        {
            var (discount, free) = SelectedVoucherValue();
            var created = vouchers.Generate((int)VoucherCount.Value, discount, free);
            Feedback.Text = $"Đã tạo {created.Count} mã.";
            LoadVouchers();
        }
        catch (Exception ex) { Feedback.Text = ex.Message; }
    }

    private void OnCreateStaffCode(object sender, RoutedEventArgs e)
    {
        try
        {
            var (discount, free) = SelectedVoucherValue();
            var staff = vouchers.CreateStaffCode(discount, free);
            Feedback.Text = $"Mã nhân viên: {staff.Code}";
            LoadVouchers();
        }
        catch (Exception ex) { Feedback.Text = ex.Message; }
    }

    // Phiên "Cần kiểm tra" (máy tắt, lỗi máy ảnh sau khi thu tiền): cấp mã miễn phí một lần, gắn với phiên gốc.
    private void OnCompensate(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not BoothSessionReport report) return;
        try
        {
            var voucher = new SqliteCustomerSessionStore().Compensate(report.SessionId);
            CompensationCode.Text = voucher.Code;
            CompensationDetail.Text = $"Miễn phí một lượt chụp · bù phiên {report.ShortId}";
            CompensationBanner.Visibility = Visibility.Visible;
            Feedback.Text = "";
            sessions = store.Sessions(); SessionsGrid.ItemsSource = sessions; LoadVouchers();
        }
        catch (Exception ex) { Feedback.Text = ex.Message; }
    }

    private void OnRevokeVoucher(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not string code) return;
        try { vouchers.Revoke(code); Feedback.Text = $"Đã hủy mã {code}."; LoadVouchers(); }
        catch (Exception ex) { Feedback.Text = ex.Message; }
    }
}
