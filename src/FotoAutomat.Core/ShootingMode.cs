namespace FotoAutomat.Core;

/// <summary>Tính năng người vận hành bật cho một chế độ chụp. Adjust là mọi chỉnh màu của preset ngoài LUT và Blur &amp; Glow.</summary>
public sealed record ModeFeatures(bool Lut, bool Adjust, bool BlurGlow, bool Retouch, bool Refine)
{
    /// <summary>Chế độ chưa cấu hình: chỉ áp màu (LUT + Adjust), như màn khách trước đây.</summary>
    public static ModeFeatures Default { get; } = new(true, true, false, false, false);
    public static ModeFeatures All { get; } = new(true, true, true, true, true);
}

/// <summary>Chế độ chụp khách chọn: màu, Retouch và Refine dùng chung cho mọi ảnh của phiên.
/// Dữ liệu riêng của ảnh mẫu (vị trí mặt, điểm bụi) không thuộc chế độ và không được áp lên ảnh khách.</summary>
public sealed record ShootingMode(ColorRecipe Color, RetouchSettings Retouch, RefineSettings Refine, ModeFeatures Features, string[] Notes)
{
    /// <summary>Grain/Sharpening cũ trong recipe màu được chuyển sang Refine (nếu Refine chưa đặt giá trị đó) rồi đặt về 0,
    /// để không bị áp hai lần. Quy đổi chỉ gần đúng vì hai thuật toán khác nhau; Notes ghi lại mỗi lần quy đổi.
    /// Tính năng tắt thì phần đó trung tính. Grain/Sharpening cũ thuộc Adjust nên vẫn áp khi Refine tắt.</summary>
    public static ShootingMode Compose(ColorRecipe look, RetouchSettings? retouch, RefineSettings? refine, ModeFeatures? features = null)
    {
        features ??= ModeFeatures.All;
        var notes = new List<string>();
        var result = (features.Refine ? refine ?? new() : new()) with { DustSpots = null };
        var color = features.Adjust ? WithoutLegacy(look) : new ColorRecipe(look.Name) { PresetId = look.PresetId, Lut = look.Lut, BlurGlow = look.BlurGlow };
        if (!features.Lut) color = color with { Lut = null };
        if (!features.BlurGlow) color = color with { BlurGlow = null };
        if (!features.Adjust) return new(color, features.Retouch ? retouch ?? new() : new(), result, features, []);
        if (look.Grain > 0)
        {
            if (result.GrainImpact == 0)
            {
                result = result with { GrainImpact = LegacyGrain(look.Grain), GrainType = FilmGrainType.Fine, Granularity = 0 };
                notes.Add($"Grain {look.Grain:0.#} của preset chuyển sang Refine (Impact {result.GrainImpact:0.#}).");
            }
            else notes.Add($"Bỏ Grain {look.Grain:0.#} của preset vì Refine đã có grain.");
        }
        if (look.Sharpening > 0)
        {
            if (result.SharpenAmount == 0)
            {
                result = result with { SharpenAmount = LegacySharpen(look.Sharpening), Radius = .8, Threshold = 0, HaloSuppression = 0 };
                notes.Add($"Sharpening {look.Sharpening:0.#} của preset chuyển sang Refine (Amount {result.SharpenAmount:0.#}).");
            }
            else notes.Add($"Bỏ Sharpening {look.Sharpening:0.#} của preset vì Refine đã có sharpen.");
        }
        if (look.Layers?.Any(l => l.Recipe.Grain > 0 || l.Recipe.Sharpening > 0) == true)
            notes.Add("Grain/Sharpening trong layer không còn được áp dụng; dùng Refine.");
        result.Validate();
        return new(color, features.Retouch ? retouch ?? new() : new(), result, features, [.. notes]);
    }

    /// <summary>Một bộ Retouch cho mọi mặt: bộ đầu tiên đã chỉnh trên ảnh mẫu.</summary>
    public static RetouchSettings RetouchOf(IEnumerable<FaceRetouch> faces) =>
        faces.Select(f => f.Settings).FirstOrDefault(s => s != new RetouchSettings()) ?? new();

    // Cùng biên độ nhiễu từng pixel: Finish cũ ±0,09·Grain/100, Refine Fine với ô 1 px ±30/255·Impact/100.
    private static double LegacyGrain(double grain) => Math.Round(Math.Clamp(grain * .75, 0, 100), 1);
    // Finish cũ: v + (v − trung bình 3×3)·S/75. Gaussian σ 0,8 gần trung bình 3×3, nên Amount = S·100/75.
    private static double LegacySharpen(double sharpening) => Math.Round(Math.Clamp(sharpening * 4 / 3, 0, 500), 1);

    private static ColorRecipe WithoutLegacy(ColorRecipe look) => look with
    {
        Grain = 0,
        Sharpening = 0,
        Layers = look.Layers?.Select(l => l with { Recipe = WithoutLegacy(l.Recipe) }).ToArray(),
    };
}
