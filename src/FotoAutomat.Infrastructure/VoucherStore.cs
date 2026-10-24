using System.Security.Cryptography;
using FotoAutomat.Core;
using Microsoft.Data.Sqlite;

namespace FotoAutomat.Infrastructure;

/// <summary>Mã giảm giá 6 chữ số. Mã thường dùng một lần; mã nhân viên (Unlimited) dùng không giới hạn và chỉ đếm số lượt.
/// Mã bù (CompensatesSessionId) do nhân viên cấp cho phiên gặp sự cố.</summary>
public sealed record Voucher(string Code, int Discount, bool Free, bool Unlimited, DateTimeOffset CreatedAt)
{
    public DateTimeOffset? UsedAt { get; init; }
    public int Uses { get; init; }
    public DateTimeOffset? LastUsedAt { get; init; }
    public bool Revoked { get; init; }
    public Guid? CompensatesSessionId { get; init; }
    public bool Usable => !Revoked && (Unlimited || UsedAt is null);
    public string ValueLabel => Free ? (CompensatesSessionId is null ? "Miễn phí" : "Miễn phí · bù phiên lỗi") : "Giảm " + CustomerPreviewFlow.FormatVnd(Discount);
    public string StatusLabel => Revoked ? "Đã hủy" : Unlimited ? $"Nhân viên · {Uses} lượt" : UsedAt is null ? "Chưa dùng" : "Đã dùng";
    public AppliedVoucher ToApplied() => new(Code, Discount, Free);
}

/// <summary>Đọc/ghi voucher trong booth.db; dùng chung transaction với tạo phiên (SqliteCustomerSessionStore).</summary>
internal static class VoucherSql
{
    private const string Columns = "code, discount, free, unlimited, created_at, used_at, uses, last_used_at, revoked, compensates";

    public static void Insert(SqliteConnection connection, SqliteTransaction transaction, Voucher v, bool ignoreExisting = false) =>
        BoothDatabase.Execute(connection, transaction,
            $"INSERT {(ignoreExisting ? "OR IGNORE " : "")}INTO vouchers ({Columns}) VALUES ($code, $discount, $free, $unlimited, $created, $used, $uses, $last, $revoked, $compensates)",
            ("$code", v.Code), ("$discount", v.Discount), ("$free", v.Free ? 1 : 0), ("$unlimited", v.Unlimited ? 1 : 0),
            ("$created", BoothDatabase.Time(v.CreatedAt)), ("$used", v.UsedAt is { } u ? BoothDatabase.Time(u) : null), ("$uses", v.Uses),
            ("$last", v.LastUsedAt is { } l ? BoothDatabase.Time(l) : null), ("$revoked", v.Revoked ? 1 : 0), ("$compensates", v.CompensatesSessionId?.ToString()));

    public static List<Voucher> All(SqliteConnection connection, SqliteTransaction? transaction = null)
    {
        using var command = BoothDatabase.Command(connection, transaction, $"SELECT {Columns} FROM vouchers");
        using var reader = command.ExecuteReader();
        var list = new List<Voucher>();
        while (reader.Read()) list.Add(Map(reader));
        return list;
    }

    public static Voucher? Find(SqliteConnection connection, SqliteTransaction? transaction, string code)
    {
        using var command = BoothDatabase.Command(connection, transaction, $"SELECT {Columns} FROM vouchers WHERE code = $code", ("$code", code));
        using var reader = command.ExecuteReader();
        return reader.Read() ? Map(reader) : null;
    }

    private static Voucher Map(SqliteDataReader r) => new(r.GetString(0), r.GetInt32(1), r.GetInt32(2) == 1, r.GetInt32(3) == 1, BoothDatabase.Time(r.GetString(4)))
    {
        UsedAt = BoothDatabase.TimeOrNull(r, 5),
        Uses = r.GetInt32(6),
        LastUsedAt = BoothDatabase.TimeOrNull(r, 7),
        Revoked = r.GetInt32(8) == 1,
        CompensatesSessionId = r.IsDBNull(9) ? null : Guid.Parse(r.GetString(9)),
    };

    // Không trùng với bất kỳ mã nào đã phát, kể cả mã đã dùng hoặc đã hủy.
    public static string NewCode(HashSet<string> taken)
    {
        if (taken.Count > 900_000) throw new InvalidOperationException("Đã hết mã 6 chữ số còn trống.");
        string code;
        do code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6"); while (!taken.Add(code));
        return code;
    }

    public static HashSet<string> TakenCodes(SqliteConnection connection, SqliteTransaction? transaction)
    {
        using var command = BoothDatabase.Command(connection, transaction, "SELECT code FROM vouchers");
        using var reader = command.ExecuteReader();
        var taken = new HashSet<string>();
        while (reader.Read()) taken.Add(reader.GetString(0));
        return taken;
    }

    /// <summary>Trừ một lượt dùng. Mã thường không dùng lại được; mã nhân viên chỉ tăng số lượt.</summary>
    public static Voucher Redeem(SqliteConnection connection, SqliteTransaction transaction, string code, DateTimeOffset now)
    {
        var voucher = Find(connection, transaction, code);
        if (voucher is not { Usable: true }) throw new InvalidOperationException("Mã không còn dùng được.");
        BoothDatabase.Execute(connection, transaction,
            voucher.Unlimited
                ? "UPDATE vouchers SET uses = uses + 1, last_used_at = $now WHERE code = $code"
                : "UPDATE vouchers SET used_at = $now, uses = 1, last_used_at = $now WHERE code = $code AND used_at IS NULL",
            ("$now", BoothDatabase.Time(now)), ("$code", code));
        return voucher;
    }
}

public sealed class VoucherStore(string? directory = null)
{
    public static IReadOnlyList<int> Discounts { get; } = [10000, 20000, 30000, 50000];
    public const int MaxBatch = 200;
    private readonly string root = directory ?? BoothDatabase.DefaultRoot;
    // Chặn dò mã trên kiosk: 5 lần sai liên tiếp thì khóa 30 giây (như PIN quản trị). Đếm theo thư mục dữ liệu.
    private static readonly Dictionary<string, (int Failed, DateTimeOffset BlockedUntil)> attempts = [];

    public IReadOnlyList<Voucher> All()
    {
        using var connection = BoothDatabase.Open(root);
        return VoucherSql.All(connection);
    }

    private static void ValidateValue(int discount, bool free)
    {
        if (!free && !Discounts.Contains(discount)) throw new ArgumentException("Chọn mức giảm 10.000đ, 20.000đ, 30.000đ, 50.000đ hoặc miễn phí.");
    }

    public IReadOnlyList<Voucher> Generate(int count, int discount, bool free)
    {
        if (count is < 1 or > MaxBatch) throw new ArgumentException($"Số mã mỗi lần từ 1 đến {MaxBatch}.");
        ValidateValue(discount, free);
        using var connection = BoothDatabase.Open(root);
        using var transaction = connection.BeginTransaction();
        var taken = VoucherSql.TakenCodes(connection, transaction);
        var now = DateTimeOffset.UtcNow;
        var created = Enumerable.Range(0, count).Select(_ => new Voucher(VoucherSql.NewCode(taken), free ? 0 : discount, free, false, now)).ToList();
        foreach (var v in created) VoucherSql.Insert(connection, transaction, v);
        transaction.Commit();
        return created;
    }

    public Voucher CreateStaffCode(int discount, bool free, string? code = null)
    {
        ValidateValue(discount, free);
        using var connection = BoothDatabase.Open(root);
        using var transaction = connection.BeginTransaction();
        var taken = VoucherSql.TakenCodes(connection, transaction);
        if (code is not null)
        {
            if (code.Length != 6 || !code.All(char.IsAsciiDigit)) throw new ArgumentException("Mã cần đúng 6 chữ số.");
            if (!taken.Add(code)) throw new ArgumentException("Mã này đã tồn tại.");
        }
        var voucher = new Voucher(code ?? VoucherSql.NewCode(taken), free ? 0 : discount, free, true, DateTimeOffset.UtcNow);
        VoucherSql.Insert(connection, transaction, voucher);
        transaction.Commit();
        return voucher;
    }

    /// <summary>Kiểm tra mã khi khách nhập; chưa trừ lượt dùng. Lượt chỉ bị trừ khi phiên được tạo (đã qua bước thanh toán).</summary>
    public AppliedVoucher Check(string code)
    {
        attempts.TryGetValue(root, out var state);
        if (DateTimeOffset.UtcNow < state.BlockedUntil) throw new ArgumentException("Nhập sai nhiều lần. Vui lòng thử lại sau 30 giây.");
        Voucher? voucher;
        using (var connection = BoothDatabase.Open(root)) voucher = VoucherSql.Find(connection, null, code);
        if (voucher is not { Usable: true })
        {
            attempts[root] = state.Failed + 1 >= 5 ? (0, DateTimeOffset.UtcNow.AddSeconds(30)) : (state.Failed + 1, state.BlockedUntil);
            throw new ArgumentException("Mã không hợp lệ hoặc đã được dùng.");
        }
        attempts[root] = default;
        return voucher.ToApplied();
    }

    /// <summary>Trừ một lượt dùng ngoài phiên khách. Phiên khách trừ lượt trong SqliteCustomerSessionStore.StartPaid.</summary>
    public void Redeem(string code)
    {
        using var connection = BoothDatabase.Open(root);
        using var transaction = connection.BeginTransaction();
        VoucherSql.Redeem(connection, transaction, code, DateTimeOffset.UtcNow);
        transaction.Commit();
    }

    public void Revoke(string code)
    {
        using var connection = BoothDatabase.Open(root);
        if (BoothDatabase.Execute(connection, null, "UPDATE vouchers SET revoked = 1 WHERE code = $code", ("$code", code)) == 0)
            throw new ArgumentException("Không tìm thấy mã.");
    }
}
