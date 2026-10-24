using System.Security.Cryptography;
using System.Text.Json;

namespace FotoAutomat.Infrastructure;

public sealed record BoothSessionReport(string Id, DateTimeOffset CompletedAt, int PrintedSheets, decimal Revenue, string DownloadFolder, string? ShareUrl);
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
    public List<BoothSessionReport> Sessions() => Read<List<BoothSessionReport>>("sessions.json") ?? [];
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
