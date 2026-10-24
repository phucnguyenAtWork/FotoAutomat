using System.Windows;
using System.Windows.Controls;
using System.Globalization;
using System.Diagnostics;
using System.IO;
using FotoAutomat.Infrastructure;

namespace FotoAutomat.Desktop;

public partial class ManagementPanel : UserControl
{
    private readonly BoothManagementStore store = new();
    private bool ready, unlocked, changingDates;
    private List<BoothSessionReport> sessions = [];
    private BoothSessionPrices savedPrices = new();
    private bool loadingPrices;
    public ManagementPanel() { InitializeComponent(); ready = true; SetPeriod(); }
    public void ShowSection(string section)
    {
        TitleText.Text = section; Feedback.Text = ""; unlocked = false;
        ReportPanel.Visibility = section == "Tổng quan" ? Visibility.Visible : Visibility.Collapsed;
        PricingPanel.Visibility = section == "Bảng giá & thanh toán" ? Visibility.Visible : Visibility.Collapsed;
        LibraryPanel.Visibility = section == "Phiên chụp & thư viện" ? Visibility.Visible : Visibility.Collapsed;
        RevenuePassword.Clear(); PasswordConfirm.Clear();
        try { sessions = store.Sessions(); LoadPrices(); SessionsGrid.ItemsSource = sessions.OrderByDescending(s => s.CompletedAt); }
        catch (Exception ex) { Feedback.Text = ex.Message; }
        EmptyLibrary.Visibility = sessions.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        UpdateReport();
    }
    private void OnPeriodChanged(object sender, SelectionChangedEventArgs e) { if (ready) SetPeriod(); }
    private void SetPeriod()
    {
        if (!ready) return;
        var today = DateTime.Today;
        var start = Period.SelectedIndex switch { 1 => new DateTime(today.Year, today.Month, 1), 2 => new DateTime(today.Year, 1, 1), 3 => new DateTime(today.Year, (today.Month - 1) / 3 * 3 + 1, 1), _ => today };
        changingDates = true;
        if (Period.SelectedIndex != 4) { FromDate.SelectedDate = start; ToDate.SelectedDate = Period.SelectedIndex switch { 1 => start.AddMonths(1).AddDays(-1), 2 => start.AddYears(1).AddDays(-1), 3 => start.AddMonths(3).AddDays(-1), _ => today }; }
        FromDate.IsEnabled = ToDate.IsEnabled = Period.SelectedIndex == 4;
        changingDates = false; UpdateReport();
    }
    private void OnRangeChanged(object sender, SelectionChangedEventArgs e) { if (ready && !changingDates) UpdateReport(); }
    private void UpdateReport()
    {
        if (!ready) return;
        var from = FromDate.SelectedDate; var to = ToDate.SelectedDate;
        bool valid = from.HasValue && to.HasValue && from <= to;
        var filtered = valid ? sessions.Where(s => s.CompletedAt.LocalDateTime.Date >= from!.Value.Date && s.CompletedAt.LocalDateTime.Date <= to!.Value.Date).ToArray() : [];
        SessionCount.Text = valid ? filtered.Length.ToString() : "—";
        PaperCount.Text = valid ? filtered.Sum(s => s.PrintedSheets).ToString() : "—";
        RevenueText.Text = unlocked && valid ? filtered.Sum(s => s.Revenue).ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " đ" : "";
        RevenueLock.Visibility = unlocked ? Visibility.Collapsed : Visibility.Visible;
        PasswordPanel.Visibility = unlocked ? Visibility.Collapsed : Visibility.Visible;
        RelockButton.Visibility = unlocked ? Visibility.Visible : Visibility.Collapsed;
        PasswordTitle.Text = store.HasPassword ? "Mật khẩu doanh thu" : "Thiết lập mật khẩu doanh thu";
        UnlockButton.Content = store.HasPassword ? "Mở khóa" : "Thiết lập và mở khóa";
        PasswordConfirm.Visibility = store.HasPassword ? Visibility.Collapsed : Visibility.Visible;
    }
    private void OnUnlock(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!store.HasPassword) { if (RevenuePassword.Password != PasswordConfirm.Password) throw new ArgumentException("Mật khẩu nhập lại chưa khớp."); store.SetPassword(RevenuePassword.Password); }
            unlocked = store.VerifyPassword(RevenuePassword.Password);
            Feedback.Text = unlocked ? "" : "Mật khẩu không đúng.";
        }
        catch (Exception ex) { Feedback.Text = ex.Message; }
        RevenuePassword.Clear(); PasswordConfirm.Clear(); UpdateReport();
    }
    private void OnRelock(object sender, RoutedEventArgs e) { unlocked = false; UpdateReport(); }
    private void LoadPrices()
    {
        savedPrices = store.SessionPrices();
        loadingPrices = true;
        SmallPrice.Value = savedPrices.Small; LargePrice.Value = savedPrices.Large;
        loadingPrices = false;
        UpdatePriceState();
    }
    private void UpdatePriceState()
    {
        bool dirty = (int)SmallPrice.Value != savedPrices.Small || (int)LargePrice.Value != savedPrices.Large;
        DirtyText.Text = PricingPanel.Visibility != Visibility.Visible ? "" : dirty ? "Có thay đổi chưa lưu" : "Đã lưu";
        RevertPricesButton.IsEnabled = dirty;
    }
    private void OnPriceChanged(object? sender, EventArgs e)
    {
        if (!ready || loadingPrices) return;
        Feedback.Text = ""; UpdatePriceState();
    }
    private void OnRevertPrices(object sender, RoutedEventArgs e)
    {
        try { LoadPrices(); Feedback.Text = ""; } catch (Exception ex) { Feedback.Text = ex.Message; }
    }
    private void OnSavePrices(object sender, RoutedEventArgs e)
    {
        var prices = new BoothSessionPrices((int)SmallPrice.Value, (int)LargePrice.Value);
        try { store.SaveSessionPrices(prices); savedPrices = prices; Feedback.Text = "Đã lưu bảng giá."; } catch (Exception ex) { Feedback.Text = ex.Message; }
        UpdatePriceState();
    }
    private void OnRefresh(object sender, RoutedEventArgs e) => ShowSection("Phiên chụp & thư viện");
    private void OnOpenFolder(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: BoothSessionReport session }) return;
        try { if (!Directory.Exists(session.DownloadFolder)) { Feedback.Text = "Chưa có thư mục ảnh."; return; } Process.Start(new ProcessStartInfo(Path.GetFullPath(session.DownloadFolder)) { UseShellExecute = true }); } catch (Exception ex) { Feedback.Text = ex.Message; }
    }
}
