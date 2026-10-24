using System.Text.Json;
using FotoAutomat.Core;
using FotoAutomat.Infrastructure;
namespace FotoAutomat.Tests;

public class CustomerSessionStoreTests
{
    private static string Folder() => Path.Combine(Path.GetTempPath(), "FotoAutomat-tests", Guid.NewGuid().ToString());
    private static SessionSnapshot Snapshot(int shots = 4) =>
        new("Khổ nhỏ", 1, shots, 2, "preset-a", "Tự nhiên", "{}", null, JsonSerializer.Serialize(new CaptureConfiguration { Shots = shots }));
    private static SessionPayment Cash(int price = 90000) => new(price, 0, price, "cash");

    [Fact]
    public void Paid_session_records_payment_and_survives_reopening()
    {
        string folder = Folder();
        var created = new SqliteCustomerSessionStore(folder).StartPaid(Snapshot(), Cash());
        var reopened = new SqliteCustomerSessionStore(folder).Get(created.Id);
        Assert.Equal(CustomerStage.Paid, reopened.Stage);
        Assert.Equal(90000, reopened.Payment.AmountDue);
        Assert.Equal("Tự nhiên", reopened.Snapshot.PresetName);
        Assert.Equal(4, reopened.Snapshot.Shots);
    }

    [Fact]
    public void Voucher_is_spent_only_together_with_a_new_session()
    {
        string folder = Folder();
        var vouchers = new VoucherStore(folder);
        var store = new SqliteCustomerSessionStore(folder);
        string code = vouchers.Generate(1, 20000, false)[0].Code;
        Assert.Throws<ArgumentException>(() => store.StartPaid(Snapshot(), new SessionPayment(90000, 30000, 60000, "cash", code)));
        Assert.Equal("Chưa dùng", vouchers.All()[0].StatusLabel);
        Assert.Empty(store.List());

        store.StartPaid(Snapshot(), new SessionPayment(90000, 20000, 70000, "cash", code));
        Assert.Equal("Đã dùng", vouchers.All()[0].StatusLabel);
        Assert.Throws<InvalidOperationException>(() => store.StartPaid(Snapshot(), new SessionPayment(90000, 20000, 70000, "qr", code)));
        Assert.Single(store.List());
    }

    [Fact]
    public void Staff_free_code_starts_free_sessions_and_counts_uses()
    {
        string folder = Folder();
        new VoucherStore(folder).CreateStaffCode(0, true, "135790");
        var store = new SqliteCustomerSessionStore(folder);
        store.StartPaid(Snapshot(), new SessionPayment(120000, 120000, 0, "voucher", "135790"));
        store.StartPaid(Snapshot(), new SessionPayment(90000, 90000, 0, "voucher", "135790"));
        Assert.Equal(2, new VoucherStore(folder).All().Single().Uses);
        Assert.Throws<ArgumentException>(() => store.StartPaid(Snapshot(), new SessionPayment(90000, 0, 90000, "voucher")));
    }

    [Fact]
    public void Shots_move_from_requested_to_kept_or_discarded_and_are_counted()
    {
        var store = new SqliteCustomerSessionStore(Folder());
        var service = new CustomerSessionService(store);
        var session = service.StartPaid(Snapshot(), Cash());
        service.Advance(CustomerStage.Capturing);
        var first = service.RequestShot(); service.ShotReceived(first, "a.jpg"); service.Discard(first);
        var second = service.RequestShot(); service.ShotReceived(second, "b.jpg"); service.Keep(second);
        Assert.Throws<InvalidOperationException>(() => service.Keep(second));
        var shots = store.Shots(session.Id);
        Assert.Equal([1, 2], shots.Select(s => s.Seq));
        Assert.Equal([ShotState.Discarded, ShotState.Kept], shots.Select(s => s.State));
        var summary = store.List().Single();
        Assert.Equal(1, summary.KeptShots);
        Assert.Equal(1, summary.DiscardedShots);
    }

    [Fact]
    public void Interrupted_sessions_need_review_and_staff_compensation_links_the_new_session()
    {
        string folder = Folder();
        var store = new SqliteCustomerSessionStore(folder);
        var service = new CustomerSessionService(store);
        var broken = service.StartPaid(Snapshot(), Cash());
        service.Advance(CustomerStage.Capturing);
        service.RequestShot();

        var restarted = new SqliteCustomerSessionStore(folder);
        Assert.Equal(1, restarted.RecoverInterrupted());
        Assert.Equal(0, restarted.RecoverInterrupted());
        Assert.Equal(CustomerStage.NeedsReview, restarted.Get(broken.Id).Stage);

        var voucher = restarted.Compensate(broken.Id);
        Assert.True(voucher.Free);
        Assert.Equal(broken.Id, voucher.CompensatesSessionId);
        Assert.Equal(CustomerStage.Compensated, restarted.Get(broken.Id).Stage);
        Assert.Throws<InvalidOperationException>(() => restarted.Compensate(broken.Id));

        var replacement = restarted.StartPaid(Snapshot(), new SessionPayment(90000, 90000, 0, "voucher", voucher.Code));
        Assert.Equal(broken.Id, replacement.CompensatesSessionId);
        Assert.Throws<InvalidOperationException>(() => restarted.StartPaid(Snapshot(), new SessionPayment(90000, 90000, 0, "voucher", voucher.Code)));
    }

    [Fact]
    public void Stale_revision_and_invalid_transitions_are_rejected()
    {
        var store = new SqliteCustomerSessionStore(Folder());
        var session = store.StartPaid(Snapshot(), Cash());
        var capturing = store.Transition(session, CustomerStage.Capturing);
        Assert.Throws<InvalidOperationException>(() => store.Transition(session, CustomerStage.Capturing));
        Assert.Throws<InvalidOperationException>(() => store.Transition(capturing, CustomerStage.Completed));
        var completed = store.Transition(store.Transition(store.Transition(capturing, CustomerStage.Selecting), CustomerStage.Framing), CustomerStage.Completed);
        Assert.NotNull(completed.CompletedAt);
        Assert.Throws<InvalidOperationException>(() => store.Transition(completed, CustomerStage.NeedsReview));
    }

    [Fact]
    public void Selection_is_saved_and_replaced()
    {
        var store = new SqliteCustomerSessionStore(Folder());
        var session = store.StartPaid(Snapshot(), Cash());
        store.SaveSelection(session.Id, new SessionSelection([1, 2], [1, null]));
        store.SaveSelection(session.Id, new SessionSelection([2], [2, 2]));
        Assert.Equal([2], store.Selection(session.Id)!.Favorites);
        Assert.Equal([2, 2], store.Selection(session.Id)!.Slots);
    }

    [Fact]
    public void Revenue_report_reads_paid_sessions_and_prints_only_completed_ones()
    {
        string folder = Folder();
        var store = new SqliteCustomerSessionStore(folder);
        var done = store.StartPaid(Snapshot(), Cash(120000));
        store.Transition(store.Transition(store.Transition(store.Transition(done, CustomerStage.Capturing), CustomerStage.Selecting), CustomerStage.Framing), CustomerStage.Completed);
        store.StartPaid(Snapshot(), Cash(90000));
        var report = new BoothManagementStore(folder).Sessions();
        Assert.Equal(2, report.Count);
        Assert.Equal(210000m, report.Sum(r => r.Revenue));
        Assert.Equal(1, report.Sum(r => r.PrintedSheets));
        Assert.Contains(report, r => r.StageLabel == "Hoàn tất");
    }

    [Fact]
    public void Legacy_voucher_file_is_imported_once()
    {
        string folder = Folder(); Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "vouchers.json"),
            """[{"Code":"482913","Discount":20000,"Free":false,"Unlimited":false,"CreatedAt":"2026-10-23T10:00:00+00:00","UsedAt":null,"Uses":0,"LastUsedAt":null,"Revoked":false}]""");
        var vouchers = new VoucherStore(folder);
        Assert.Equal("482913", vouchers.All().Single().Code);
        Assert.False(File.Exists(Path.Combine(folder, "vouchers.json")));
        Assert.True(File.Exists(Path.Combine(folder, "vouchers.json.imported")));
        Assert.Equal(20000, vouchers.Check("482913").Discount);
    }
}
