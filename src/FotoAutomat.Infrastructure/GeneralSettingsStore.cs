using System.Security.Cryptography;
using System.Text.Json;
using FotoAutomat.Core;

namespace FotoAutomat.Infrastructure;

public sealed record GeneralSettings
{
    public string BrandName { get; init; } = "Foto Hunter";
    /// <summary>Logo file name inside the operator folder, or empty for no logo. Never a path.</summary>
    public string LogoFile { get; init; } = "";
    /// <summary>Màu nhấn của giao diện khách (#RRGGBB): nút chính, lựa chọn, tiến trình.</summary>
    public string AccentColor { get; init; } = BrandColors.DefaultAccent;
    /// <summary>Màu nền của giao diện khách (#RRGGBB), trừ màn chụp.</summary>
    public string BackgroundColor { get; init; } = BrandColors.DefaultBackground;
    /// <summary>Ảnh nền trong thư mục quản trị, hoặc rỗng. Chỉ là tên tệp.</summary>
    public string BackgroundFile { get; init; } = "";
    /// <summary>Ảnh của màn chờ trước khi chụp, hoặc rỗng (dùng ảnh xem trước của chế độ đã chọn). Chỉ là tên tệp.</summary>
    public string LoadingFile { get; init; } = "";
    public bool FullScreen { get; init; } = true;
    public bool HardwareAcceleration { get; init; } = true;
    public string DataDirectory { get; init; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "FotoAutomat");
    public string ExportDirectory { get; init; } = "";
    public bool ExportOriginals { get; init; } = true;
    public bool ExportPrints { get; init; } = true;
    public bool ExportGif { get; init; }
    public bool ExportTimelapse { get; init; }
}

public sealed class GeneralSettingsStore(string? directory = null)
{
    private readonly string root = directory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FotoAutomat", "Operator");
    private sealed record PinRecord(string Salt, string Hash);
    private static int failedAttempts;
    private static DateTimeOffset blockedUntil;
    public bool HasPin => File.Exists(Path.Combine(root, "admin-pin.json"));
    public GeneralSettings Load() => File.Exists(Path.Combine(root, "general.json")) ? JsonSerializer.Deserialize<GeneralSettings>(File.ReadAllText(Path.Combine(root, "general.json"))) ?? throw new InvalidDataException("Không đọc được cài đặt chung.") : new();
    private void Write<T>(string file, T value)
    {
        Directory.CreateDirectory(root);
        string target = Path.Combine(root, file);
        File.WriteAllText(target + ".tmp", JsonSerializer.Serialize(value)); File.Move(target + ".tmp", target, true);
    }
    public void Save(GeneralSettings value)
    {
        if (string.IsNullOrWhiteSpace(value.BrandName) || value.BrandName.Length > 80) throw new ArgumentException("Tên thương hiệu cần từ 1 đến 80 ký tự.");
        if (!Path.IsPathFullyQualified(value.DataDirectory) || (value.ExportDirectory.Length > 0 && !Path.IsPathFullyQualified(value.ExportDirectory))) throw new ArgumentException("Chọn đường dẫn thư mục đầy đủ.");
        if (value.LogoFile.Length > 0 && (!IsImageName(value.LogoFile, LogoPrefix) || !File.Exists(Path.Combine(root, value.LogoFile)))) throw new ArgumentException("Không tìm thấy tệp logo.");
        if (value.BackgroundFile.Length > 0 && (!IsImageName(value.BackgroundFile, BackgroundPrefix) || !File.Exists(Path.Combine(root, value.BackgroundFile)))) throw new ArgumentException("Không tìm thấy ảnh nền.");
        if (value.LoadingFile.Length > 0 && (!IsImageName(value.LoadingFile, LoadingPrefix) || !File.Exists(Path.Combine(root, value.LoadingFile)))) throw new ArgumentException("Không tìm thấy ảnh màn chờ.");
        BrandColors.ValidateAccent(value.AccentColor);
        BrandColors.ValidateBackground(value.BackgroundColor);
        value = value with { AccentColor = BrandColors.Normalize(value.AccentColor), BackgroundColor = BrandColors.Normalize(value.BackgroundColor) };
        Write("general.json", value);
        RemoveUnused(LogoPrefix, value.LogoFile);
        RemoveUnused(BackgroundPrefix, value.BackgroundFile);
        RemoveUnused(LoadingPrefix, value.LoadingFile);
    }

    public const long MaxLogoBytes = 5 * 1024 * 1024, MaxBackgroundBytes = 10 * 1024 * 1024;
    private const string LogoPrefix = "brand-logo-", BackgroundPrefix = "brand-background-", LoadingPrefix = "brand-loading-";
    private static readonly string[] ImageExtensions = [".png", ".jpg", ".jpeg"];
    private static bool IsImageName(string name, string prefix) =>
        name == Path.GetFileName(name) && name.StartsWith(prefix, StringComparison.Ordinal) && ImageExtensions.Contains(Path.GetExtension(name).ToLowerInvariant());

    private void RemoveUnused(string prefix, string current)
    {
        foreach (string old in Directory.EnumerateFiles(root, prefix + "*"))
            if (!string.Equals(Path.GetFileName(old), current, StringComparison.OrdinalIgnoreCase)) TryDelete(old);
    }

    private string? ImagePath(string name, string prefix)
    {
        if (name.Length == 0 || !IsImageName(name, prefix)) return null;
        string path = Path.Combine(root, name);
        return File.Exists(path) ? path : null;
    }

    /// <summary>Full path of the saved logo, or null when none is set or the file is missing.</summary>
    public string? LogoPath(GeneralSettings value) => ImagePath(value.LogoFile, LogoPrefix);

    /// <summary>Full path of the saved background image, or null when none is set or the file is missing.</summary>
    public string? BackgroundPath(GeneralSettings value) => ImagePath(value.BackgroundFile, BackgroundPrefix);

    /// <summary>Full path of the saved loading-screen image, or null when none is set or the file is missing.</summary>
    public string? LoadingPath(GeneralSettings value) => ImagePath(value.LoadingFile, LoadingPrefix);

    private string Import(string source, string prefix, long maxBytes, string label)
    {
        string extension = Path.GetExtension(source).ToLowerInvariant();
        if (!ImageExtensions.Contains(extension)) throw new ArgumentException($"{label} cần là tệp PNG hoặc JPEG.");
        if (new FileInfo(source).Length > maxBytes) throw new ArgumentException($"{label} cần nhỏ hơn {maxBytes / (1024 * 1024)} MB.");
        Directory.CreateDirectory(root);
        string name = $"{prefix}{Guid.NewGuid():N}{extension}";
        File.Copy(source, Path.Combine(root, name));
        return name;
    }

    /// <summary>Copies a PNG or JPEG into the operator folder under a new name and returns that name. Save makes it active.</summary>
    public string ImportLogo(string source) => Import(source, LogoPrefix, MaxLogoBytes, "Logo");

    /// <summary>Copies a PNG or JPEG background into the operator folder under a new name and returns that name. Save makes it active.</summary>
    public string ImportBackground(string source) => Import(source, BackgroundPrefix, MaxBackgroundBytes, "Ảnh nền");

    /// <summary>Copies a PNG or JPEG for the loading screen into the operator folder under a new name. Save makes it active.</summary>
    public string ImportLoading(string source) => Import(source, LoadingPrefix, MaxBackgroundBytes, "Ảnh màn chờ");

    /// <summary>Removes an imported logo or background that was never saved.</summary>
    public void DiscardImage(string name)
    {
        if (IsImageName(name, LogoPrefix) || IsImageName(name, BackgroundPrefix) || IsImageName(name, LoadingPrefix)) TryDelete(Path.Combine(root, name));
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
    public void SetPin(string pin, string currentPin = "")
    {
        if (HasPin && !VerifyPin(currentPin)) throw new ArgumentException("PIN hiện tại không đúng.");
        if (pin.Length is < 6 or > 12 || !pin.All(c => c is >= '0' and <= '9')) throw new ArgumentException("PIN cần từ 6 đến 12 chữ số.");
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(pin, salt, 210000, HashAlgorithmName.SHA256, 32);
        Write("admin-pin.json", new PinRecord(Convert.ToBase64String(salt), Convert.ToBase64String(hash)));
    }
    public bool VerifyPin(string pin)
    {
        if (DateTimeOffset.UtcNow < blockedUntil) throw new ArgumentException("Nhập sai nhiều lần. Vui lòng thử lại sau 30 giây.");
        if (!HasPin) return false;
        var record = JsonSerializer.Deserialize<PinRecord>(File.ReadAllText(Path.Combine(root, "admin-pin.json"))) ?? throw new InvalidDataException("Không đọc được PIN.");
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(pin, Convert.FromBase64String(record.Salt), 210000, HashAlgorithmName.SHA256, 32);
        bool valid = CryptographicOperations.FixedTimeEquals(hash, Convert.FromBase64String(record.Hash));
        if (valid) failedAttempts = 0;
        else if (++failedAttempts >= 5) { blockedUntil = DateTimeOffset.UtcNow.AddSeconds(30); failedAttempts = 0; }
        return valid;
    }
}
