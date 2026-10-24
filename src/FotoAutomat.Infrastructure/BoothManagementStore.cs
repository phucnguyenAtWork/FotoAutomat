using System.Security.Cryptography;
using System.Text.Json;

namespace FotoAutomat.Infrastructure;

public sealed record BoothSessionReport(string Id, DateTimeOffset CompletedAt, int PrintedSheets, decimal Revenue, string DownloadFolder, string? ShareUrl)
{
    public Guid SessionId { get; init; }
    public string StageLabel { get; init; } = "";
    public bool CanCompensate { get; init; }
    public string ShotsLabel { get; init; } = "";
    public string RevenueLabel => FotoAutomat.Core.CustomerPreviewFlow.FormatVnd((int)Revenue);
    public string PaymentLabel { get; init; } = "";
    public string ShortId => Id.Length > 8 ? Id[..8] : Id;
}
/// <summary>Giá theo khổ cho một lượt chụp. Khách chọn khổ; số bản in lấy từ Cài đặt in.</summary>
public sealed record BoothSessionPrices(int Small = 90000, int Large = 120000);
public sealed class BoothPrice
{
    public string Size { get; set; } = "";
    public int Copies { get; set; }
    public decimal Total { get; set; }
}

public sealed class BoothManagementStore(string? directory = null)
{
    private readonly string root = directory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FotoAutomat", "Operator");
    private sealed record PasswordRecord(string Salt, string Hash);
    public bool HasPassword => File.Exists(Path.Combine(root, "revenue-password.json"));
    private T? Read<T>(string file) => File.Exists(Path.Combine(root, file)) ? JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(root, file))) : default;
    private void Write<T>(string file, T value)
    {
        Directory.CreateDirectory(root);
        string target = Path.Combine(root, file), temp = target + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(value)); File.Move(temp, target, true);
    }
    public void SetPassword(string password)
    {
        if (HasPassword) throw new InvalidOperationException("Mật khẩu đã được thiết lập.");
        if (password.Length < 8) throw new ArgumentException("Mật khẩu cần ít nhất 8 ký tự.");
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 210000, HashAlgorithmName.SHA256, 32);
        Write("revenue-password.json", new PasswordRecord(Convert.ToBase64String(salt), Convert.ToBase64String(hash)));
    }
    public bool VerifyPassword(string password)
    {
        var saved = Read<PasswordRecord>("revenue-password.json");
        if (saved is null) return false;
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, Convert.FromBase64String(saved.Salt), 210000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(hash, Convert.FromBase64String(saved.Hash));
    }
    /// <summary>Phiên đã thanh toán từ booth.db (nguồn dữ liệu duy nhất). Doanh thu là số tiền thực thu; số tờ chỉ tính phiên hoàn tất.</summary>
    public List<BoothSessionReport> Sessions()
    {
        string data;
        try { data = new GeneralSettingsStore(root).Load().DataDirectory; }
        catch (Exception) { data = new GeneralSettings().DataDirectory; }
        return [.. new SqliteCustomerSessionStore(root).List().Select(summary =>
        {
            var session = summary.Session;
            return new BoothSessionReport(session.Id.ToString("N"), (session.CompletedAt ?? session.StartedAt).ToLocalTime(),
                session.Stage == FotoAutomat.Core.CustomerStage.Completed ? session.Snapshot.Copies : 0, session.Payment.AmountDue,
                Path.Combine(data, "Sessions", session.Id.ToString("N")), null)
            {
                SessionId = session.Id,
                StageLabel = StageLabel(session.Stage),
                CanCompensate = session.Stage == FotoAutomat.Core.CustomerStage.NeedsReview,
                ShotsLabel = summary.DiscardedShots > 0 ? $"{summary.KeptShots} giữ · {summary.DiscardedShots} xóa" : $"{summary.KeptShots} ảnh",
                PaymentLabel = session.Payment.Method switch { "cash" => "Tiền mặt", "qr" => "QR", _ => "Voucher" }
                    + (summary.VoucherCode is { } code ? $" · mã {code}" : "")
                    + (session.CompensatesSessionId is { } original ? $" · bù {original.ToString("N")[..8]}" : ""),
            };
        })];
    }

    private static string StageLabel(FotoAutomat.Core.CustomerStage stage) => stage switch
    {
        FotoAutomat.Core.CustomerStage.Paid => "Đã thanh toán",
        FotoAutomat.Core.CustomerStage.Capturing => "Đang chụp",
        FotoAutomat.Core.CustomerStage.Selecting => "Đang chọn ảnh",
        FotoAutomat.Core.CustomerStage.Framing => "Đang ghép khung",
        FotoAutomat.Core.CustomerStage.Completed => "Hoàn tất",
        FotoAutomat.Core.CustomerStage.NeedsReview => "Cần kiểm tra",
        FotoAutomat.Core.CustomerStage.Compensated => "Đã cấp voucher bù",
        _ => stage.ToString()
    };
    public List<BoothPrice> Prices() => Read<List<BoothPrice>>("prices.json") ?? [.. new[] { "Khổ nhỏ", "Khổ lớn" }.SelectMany(size => Enumerable.Range(2, 4).Select(n => new BoothPrice { Size = size, Copies = n }))];
    public void SavePrices(List<BoothPrice> prices)
    {
        if (prices.Count == 0 || prices.Any(p => p.Copies < 2 || p.Total < 0 || p.Total > 100000000 || decimal.Truncate(p.Total) != p.Total)) throw new ArgumentException("Nhập giá nguyên từ 0 đến 100.000.000 đồng.");
        foreach (var group in prices.GroupBy(p => p.Size))
        {
            decimal previous = -1;
            foreach (var row in group.OrderBy(p => p.Copies)) { if (row.Total < previous) throw new ArgumentException("Giá không được giảm khi tăng số bản in."); previous = row.Total; }
        }
        Write("prices.json", prices);
    }
    public BoothSessionPrices SessionPrices() => Read<BoothSessionPrices>("session-prices.json") ?? new();
    public void SaveSessionPrices(BoothSessionPrices prices)
    {
        if (prices.Small is < 0 or > 100000000 || prices.Large is < 0 or > 100000000) throw new ArgumentException("Nhập giá nguyên từ 0 đến 100.000.000 đồng.");
        Write("session-prices.json", prices);
    }
}
