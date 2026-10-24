using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using FotoAutomat.Core;

namespace FotoAutomat.Infrastructure;

public sealed record PresetImportResult(ColorRecipe Recipe, string[] Warnings);
public static class LightroomPresetImporter
{
    public static PresetImportResult Import(string path)
    {
        if (new FileInfo(path).Length > 32_000_000) throw new InvalidDataException("Preset tối đa 32 MB.");
        string name = Path.GetFileNameWithoutExtension(path); if (name.Length > 80) name = name[..80];
        return Path.GetExtension(path).ToLowerInvariant() switch { ".xmp" => Xmp(File.ReadAllText(path), name), ".cube" => Cube(File.ReadAllText(path), name), _ => throw new InvalidDataException("Chỉ nhận .xmp và .cube.") };
    }
    public static PresetImportResult Cube(string text, string name)
    {
        int size = 0; double[] min = [0, 0, 0], max = [1, 1, 1]; var values = new List<double>();
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Split('#')[0].Trim(); if (line.Length == 0 || line.StartsWith("TITLE ", StringComparison.Ordinal)) continue;
            var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts[0] == "LUT_3D_SIZE") { if (size != 0 || parts.Length != 2 || !int.TryParse(parts[1], out size) || size < 2 || size > 65) throw new InvalidDataException("CUBE size phải từ 2 đến 65."); }
            else if (parts[0] is "DOMAIN_MIN" or "DOMAIN_MAX") { if (parts.Length != 4) throw new InvalidDataException("CUBE domain không hợp lệ."); var domain = parts.Skip(1).Select(Parse).ToArray(); if (parts[0] == "DOMAIN_MIN") min = domain; else max = domain; }
            else { if (parts.Length != 3 || size == 0 || values.Count >= size * size * size * 3) throw new InvalidDataException("Chỉ hỗ trợ CUBE 3D RGB; bảng bị lỗi hoặc không đủ header."); values.AddRange(parts.Select(Parse)); }
        }
        var lut = new CubeLut(size, values.ToArray(), min, max); lut.Validate(); return new(new ColorRecipe(name) { Lut = lut }, []);
    }
    private static double Parse(string s) => double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) && double.IsFinite(n) ? n : throw new InvalidDataException("Preset chứa số không hợp lệ.");
    public static PresetImportResult Xmp(string text, string fallbackName)
    {
        using var reader = XmlReader.Create(new StringReader(text), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 32_000_000 });
        var doc = XDocument.Load(reader); XNamespace crs = "http://ns.adobe.com/camera-raw-settings/1.0/";
        var data = new Dictionary<string, string>();
        foreach (var a in doc.Descendants().Attributes().Where(a => a.Name.Namespace == crs)) data[a.Name.LocalName] = a.Value;
        foreach (var e in doc.Descendants().Where(e => e.Name.Namespace == crs)) data[e.Name.LocalName] = e.Value;
        if (data.Count == 0) throw new InvalidDataException("Không tìm thấy thông số Camera Raw trong XMP.");
        var used = new HashSet<string>(); var warnings = new List<string>();
        double N(string key, double def = 0) { if (!data.TryGetValue(key, out var s)) return def; used.Add(key); return Parse(s); }
        string S(string key, string def = "") { if (!data.TryGetValue(key, out var s)) return def; used.Add(key); return s; }
        string name = S("Name", fallbackName).Trim(); if (name.Length > 80) name = name[..80];
        var r = new ColorRecipe(name, N("Exposure2012"), N("Contrast2012"), N("IncrementalTemperature"), N("Saturation"), S("ConvertToGrayscale") == "True", N("IncrementalTint"), Highlights: N("Highlights2012"), Shadows: N("Shadows2012"), Whites: N("Whites2012"), Blacks: N("Blacks2012"))
        {
            Temperature = data.ContainsKey("Temperature") ? N("Temperature") : null,
            Tint = data.ContainsKey("Tint") ? N("Tint") : N("IncrementalTint"),
            Structure = N("Texture"),
            Clarity = N("Clarity2012"),
            Dehaze = N("Dehaze"),
            Vibrance = N("Vibrance"),
            CurveHighlights = N("ParametricHighlights"),
            CurveLights = N("ParametricLights"),
            CurveDarks = N("ParametricDarks"),
            CurveShadows = N("ParametricShadows"),
            GradingBlending = N("ColorGradeBlending", 50),
            GradingBalance = N("SplitToningBalance"),
            Vignette = N("PostCropVignetteAmount"),
            Grain = N("GrainAmount"),
            Hsl = new[] { "Red", "Orange", "Yellow", "Green", "Aqua", "Blue", "Purple", "Magenta" }.Select(c => new HslAdjustment(N("HueAdjustment" + c), N("SaturationAdjustment" + c), N("LuminanceAdjustment" + c))).ToArray(),
            Calibration = new[] { "Red", "Green", "Blue" }.Select(c => new HslAdjustment(N(c + "Hue"), N(c + "Saturation"))).ToArray(),
            Balance = [new(N("ColorGradeGlobalHue"), N("ColorGradeGlobalSat"), N("ColorGradeGlobalLum")), new(N("SplitToningShadowHue"), N("SplitToningShadowSaturation"), N("ColorGradeShadowLum")), new(N("ColorGradeMidtoneHue"), N("ColorGradeMidtoneSat"), N("ColorGradeMidtoneLum")), new(N("SplitToningHighlightHue"), N("SplitToningHighlightSaturation"), N("ColorGradeHighlightLum"))]
        };
        var curves = Enumerable.Range(0, 5).Select(_ => ToneCurve.Identity).ToArray();
        foreach (var (key, index) in new[] { ("ToneCurvePV2012", 0), ("ToneCurvePV2012Red", 2), ("ToneCurvePV2012Green", 3), ("ToneCurvePV2012Blue", 4) })
        {
            var node = doc.Descendants(crs + key).FirstOrDefault(); if (node is null) continue; used.Add(key);
            var points = node.Descendants().Where(e => e.Name.LocalName == "li").Select(e => { var p = e.Value.Split(','); if (p.Length != 2) throw new InvalidDataException("XMP curve không hợp lệ."); return new CurvePoint(Parse(p[0]) / 255, Parse(p[1]) / 255); }).ToArray();
            if (points.Length > 0) { curves[index] = new(points); curves[index].Validate(); }
        }
        r = r with { Curves = curves }; r.Validate();
        if (r.Temperature is not null) warnings.Add("RAW Kelvin được giữ nguyên số; ảnh JPEG dùng phép chuyển white balance gần đúng, không có camera profile.");
        string[] metadata = ["Name", "UUID", "PresetType", "Cluster", "Group", "Description", "Version", "ProcessVersion", "WhiteBalance", "SupportsAmount", "SupportsColor", "SupportsMonochrome", "SupportsHighDynamicRange", "SupportsNormalDynamicRange", "SupportsSceneReferred", "SupportsOutputReferred", "CameraModelRestriction", "ToneCurveName2012", "HasSettings", "HasCrop", "AlreadyApplied"];
        var unsupported = data.Keys.Where(k => !used.Contains(k) && !metadata.Contains(k)).Order().ToArray();
        if (unsupported.Length > 0) warnings.Add("Chưa áp dụng: " + string.Join(", ", unsupported));
        warnings.Add("Thông số đã nhập; engine FotoAutomat không phải Adobe Lightroom. Cần so ảnh để kiểm chứng màu.");
        return new(r, warnings.ToArray());
    }
}
