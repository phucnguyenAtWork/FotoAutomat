using System.Text.Json;
using FotoAutomat.Core;
using Microsoft.Data.Sqlite;

namespace FotoAutomat.Infrastructure;

/// <summary>Phiên khách trong booth.db. Mỗi chuyển trạng thái kiểm tra revision và ghi một dòng nhật ký.</summary>
public sealed class SqliteCustomerSessionStore(string? directory = null) : ICustomerSessionStore
{
    private readonly string root = directory ?? BoothDatabase.DefaultRoot;
    private const string SessionColumns = "s.id, s.started_at, s.stage, s.revision, s.completed_at, s.compensates, s.snapshot, p.price, p.discount, p.amount_due, p.method, p.voucher_code, p.reference, s.share_token";

    public CustomerSession StartPaid(SessionSnapshot snapshot, SessionPayment payment)
    {
        payment.Validate();
        var now = DateTimeOffset.UtcNow;
        using var connection = BoothDatabase.Open(root);
        using var transaction = connection.BeginTransaction();
        Guid? compensates = null;
        if (payment.VoucherCode is { } code)
        {
            var voucher = VoucherSql.Redeem(connection, transaction, code, now);
            int expected = voucher.Free ? payment.Price : Math.Min(payment.Price, voucher.Discount);
            if (payment.Discount != expected) throw new ArgumentException("Số tiền giảm không khớp voucher.");
            compensates = voucher.CompensatesSessionId;
        }
        else if (payment.Discount != 0) throw new ArgumentException("Có giảm giá nhưng không có voucher.");

        var session = new CustomerSession(Guid.NewGuid(), now, CustomerStage.Paid, 0, payment, snapshot) { CompensatesSessionId = compensates, ShareToken = NewShareToken() };
        BoothDatabase.Execute(connection, transaction,
            "INSERT INTO customer_sessions (id, started_at, stage, revision, compensates, snapshot, share_token) VALUES ($id, $at, $stage, 0, $compensates, $snapshot, $token)",
            ("$id", session.Id.ToString()), ("$at", BoothDatabase.Time(now)), ("$stage", session.Stage.ToString()),
            ("$compensates", compensates?.ToString()), ("$snapshot", JsonSerializer.Serialize(snapshot)), ("$token", session.ShareToken));
        BoothDatabase.Execute(connection, transaction,
            "INSERT INTO payments (session_id, price, discount, amount_due, method, voucher_code, reference, paid_at) VALUES ($id, $price, $discount, $due, $method, $voucher, $reference, $at)",
            ("$id", session.Id.ToString()), ("$price", payment.Price), ("$discount", payment.Discount), ("$due", payment.AmountDue),
            ("$method", payment.Method), ("$voucher", payment.VoucherCode), ("$reference", payment.Reference), ("$at", BoothDatabase.Time(now)));
        Log(connection, transaction, session.Id, now, session.Stage, compensates is null ? "paid" : $"paid · bù phiên {compensates}");
        transaction.Commit();
        return session;
    }

    public CustomerSession Get(Guid id)
    {
        using var connection = BoothDatabase.Open(root);
        return Read(connection, null, id);
    }

    public CustomerSession Transition(CustomerSession expected, CustomerStage next, string? note = null)
    {
        var now = DateTimeOffset.UtcNow;
        var updated = expected.MoveTo(next, now);
        using var connection = BoothDatabase.Open(root);
        using var transaction = connection.BeginTransaction();
        int rows = BoothDatabase.Execute(connection, transaction,
            "UPDATE customer_sessions SET stage = $stage, revision = $revision, completed_at = $completed WHERE id = $id AND revision = $expected",
            ("$stage", next.ToString()), ("$revision", updated.Revision), ("$completed", updated.CompletedAt is { } c ? BoothDatabase.Time(c) : null),
            ("$id", expected.Id.ToString()), ("$expected", expected.Revision));
        if (rows != 1) throw new InvalidOperationException("Phiên đã thay đổi ở nơi khác. Tải lại trước khi tiếp tục.");
        Log(connection, transaction, expected.Id, now, next, note);
        transaction.Commit();
        return updated;
    }

    public void RecordShotRequested(Guid sessionId, Guid shotId, int seq)
    {
        using var connection = BoothDatabase.Open(root);
        BoothDatabase.Execute(connection, null, "INSERT INTO shots (id, session_id, seq, state, at) VALUES ($id, $session, $seq, 'Requested', $at)",
            ("$id", shotId.ToString()), ("$session", sessionId.ToString()), ("$seq", seq), ("$at", BoothDatabase.Time(DateTimeOffset.UtcNow)));
    }

    public void RecordShotReceived(Guid shotId, string? path)
    {
        using var connection = BoothDatabase.Open(root);
        if (BoothDatabase.Execute(connection, null, "UPDATE shots SET path = $path, state = 'Received', at = $at WHERE id = $id AND state = 'Requested'",
            ("$path", path), ("$at", BoothDatabase.Time(DateTimeOffset.UtcNow)), ("$id", shotId.ToString())) != 1)
            throw new InvalidOperationException("Shot không ở trạng thái chờ nhận ảnh.");
    }

    public void MarkShot(Guid shotId, ShotState state)
    {
        if (state is not (ShotState.Kept or ShotState.Discarded)) throw new ArgumentException("Chỉ đánh dấu giữ hoặc xóa.");
        using var connection = BoothDatabase.Open(root);
        if (BoothDatabase.Execute(connection, null, "UPDATE shots SET state = $state, at = $at WHERE id = $id AND state = 'Received'",
            ("$state", state.ToString()), ("$at", BoothDatabase.Time(DateTimeOffset.UtcNow)), ("$id", shotId.ToString())) != 1)
            throw new InvalidOperationException("Shot chưa nhận ảnh hoặc đã được đánh dấu.");
    }

    public IReadOnlyList<SessionShot> Shots(Guid sessionId)
    {
        using var connection = BoothDatabase.Open(root);
        using var command = BoothDatabase.Command(connection, null, "SELECT id, session_id, seq, path, state, at FROM shots WHERE session_id = $id ORDER BY seq", ("$id", sessionId.ToString()));
        using var reader = command.ExecuteReader();
        var shots = new List<SessionShot>();
        while (reader.Read())
            shots.Add(new(Guid.Parse(reader.GetString(0)), Guid.Parse(reader.GetString(1)), reader.GetInt32(2), reader.IsDBNull(3) ? null : reader.GetString(3),
                Enum.Parse<ShotState>(reader.GetString(4)), BoothDatabase.Time(reader.GetString(5))));
        return shots;
    }

    public void SaveSelection(Guid sessionId, SessionSelection selection)
    {
        using var connection = BoothDatabase.Open(root);
        BoothDatabase.Execute(connection, null,
            "INSERT INTO selections (session_id, data, saved_at) VALUES ($id, $data, $at) ON CONFLICT(session_id) DO UPDATE SET data = excluded.data, saved_at = excluded.saved_at",
            ("$id", sessionId.ToString()), ("$data", JsonSerializer.Serialize(selection)), ("$at", BoothDatabase.Time(DateTimeOffset.UtcNow)));
    }

    public SessionSelection? Selection(Guid sessionId)
    {
        using var connection = BoothDatabase.Open(root);
        using var command = BoothDatabase.Command(connection, null, "SELECT data FROM selections WHERE session_id = $id", ("$id", sessionId.ToString()));
        return command.ExecuteScalar() is string json ? JsonSerializer.Deserialize<SessionSelection>(json) : null;
    }

    public int RecoverInterrupted()
    {
        var now = DateTimeOffset.UtcNow;
        using var connection = BoothDatabase.Open(root);
        using var transaction = connection.BeginTransaction();
        var open = new List<(string Id, int Revision)>();
        using (var command = BoothDatabase.Command(connection, transaction, "SELECT id, revision FROM customer_sessions WHERE stage IN ('Paid', 'Capturing', 'Selecting', 'Framing')"))
        using (var reader = command.ExecuteReader())
            while (reader.Read()) open.Add((reader.GetString(0), reader.GetInt32(1)));
        foreach (var (id, revision) in open)
        {
            BoothDatabase.Execute(connection, transaction, "UPDATE customer_sessions SET stage = 'NeedsReview', revision = $revision WHERE id = $id",
                ("$revision", revision + 1), ("$id", id));
            Log(connection, transaction, Guid.Parse(id), now, CustomerStage.NeedsReview, "interrupted");
        }
        transaction.Commit();
        return open.Count;
    }

    public IReadOnlyList<SessionSummary> List(DateTimeOffset? from = null, DateTimeOffset? to = null)
    {
        using var connection = BoothDatabase.Open(root);
        using var command = BoothDatabase.Command(connection, null,
            $"""
            SELECT {SessionColumns},
                   (SELECT COUNT(*) FROM shots k WHERE k.session_id = s.id AND k.state = 'Kept'),
                   (SELECT COUNT(*) FROM shots d WHERE d.session_id = s.id AND d.state = 'Discarded')
            FROM customer_sessions s JOIN payments p ON p.session_id = s.id
            WHERE ($from IS NULL OR s.started_at >= $from) AND ($to IS NULL OR s.started_at < $to)
            ORDER BY s.started_at DESC
            """,
            ("$from", from is { } f ? BoothDatabase.Time(f.ToUniversalTime()) : null), ("$to", to is { } t ? BoothDatabase.Time(t.ToUniversalTime()) : null));
        using var reader = command.ExecuteReader();
        var list = new List<SessionSummary>();
        while (reader.Read())
        {
            var session = Map(reader);
            list.Add(new(session, reader.GetInt32(14), reader.GetInt32(15), session.Payment.VoucherCode));
        }
        return list;
    }

    /// <summary>Nhân viên cấp voucher bù cho phiên cần kiểm tra: phiên chuyển Compensated và một mã miễn phí dùng một lần được tạo.</summary>
    public Voucher Compensate(Guid sessionId)
    {
        var now = DateTimeOffset.UtcNow;
        using var connection = BoothDatabase.Open(root);
        using var transaction = connection.BeginTransaction();
        var session = Read(connection, transaction, sessionId);
        var updated = session.MoveTo(CustomerStage.Compensated, now);
        BoothDatabase.Execute(connection, transaction, "UPDATE customer_sessions SET stage = $stage, revision = $revision WHERE id = $id AND revision = $expected",
            ("$stage", updated.Stage.ToString()), ("$revision", updated.Revision), ("$id", sessionId.ToString()), ("$expected", session.Revision));
        var voucher = new Voucher(VoucherSql.NewCode(VoucherSql.TakenCodes(connection, transaction)), 0, true, false, now) { CompensatesSessionId = sessionId };
        VoucherSql.Insert(connection, transaction, voucher);
        Log(connection, transaction, sessionId, now, CustomerStage.Compensated, $"voucher bù {voucher.Code}");
        transaction.Commit();
        return voucher;
    }

    private static CustomerSession Read(SqliteConnection connection, SqliteTransaction? transaction, Guid id)
    {
        using var command = BoothDatabase.Command(connection, transaction,
            $"SELECT {SessionColumns} FROM customer_sessions s JOIN payments p ON p.session_id = s.id WHERE s.id = $id", ("$id", id.ToString()));
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw new KeyNotFoundException($"Không tìm thấy phiên {id}.");
        return Map(reader);
    }

    private static CustomerSession Map(SqliteDataReader r) => new(
        Guid.Parse(r.GetString(0)), BoothDatabase.Time(r.GetString(1)), Enum.Parse<CustomerStage>(r.GetString(2)), r.GetInt32(3),
        new SessionPayment(r.GetInt32(7), r.GetInt32(8), r.GetInt32(9), r.GetString(10), r.IsDBNull(11) ? null : r.GetString(11), r.IsDBNull(12) ? null : r.GetString(12)),
        JsonSerializer.Deserialize<SessionSnapshot>(r.GetString(6))!)
    {
        CompletedAt = BoothDatabase.TimeOrNull(r, 4),
        CompensatesSessionId = r.IsDBNull(5) ? null : Guid.Parse(r.GetString(5)),
        ShareToken = r.IsDBNull(13) ? null : r.GetString(13),
    };

    // 128 bit ngẫu nhiên, base64url: đủ để không đoán được link của nhóm khác.
    private static string NewShareToken() => Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static void Log(SqliteConnection connection, SqliteTransaction transaction, Guid sessionId, DateTimeOffset at, CustomerStage stage, string? note) =>
        BoothDatabase.Execute(connection, transaction, "INSERT INTO session_events (session_id, at, stage, note) VALUES ($id, $at, $stage, $note)",
            ("$id", sessionId.ToString()), ("$at", BoothDatabase.Time(at)), ("$stage", stage.ToString()), ("$note", note));
}
