using System.Runtime.InteropServices;
using System.Text.Json;

namespace FotoAutomat.Infrastructure;

public sealed record CameraSettingOption(int Value, string Label);
public sealed record CameraSetting(string Key, string Label, int? Current, IReadOnlyList<CameraSettingOption> Options, string? Error);

public sealed partial class FujifilmCameraConnection
{
    private static readonly (string Key, string Label)[] SettingNames =
    [ ("AEMode", "Chế độ chụp"), ("ShutterSpeed", "Tốc độ màn trập"), ("Aperture", "Khẩu độ"),
      ("Sensitivity", "ISO"), ("WBMode", "Cân bằng trắng"), ("WBColorTemp", "Nhiệt độ màu") ];

    public Task<IReadOnlyList<CameraSetting>> ReadSettingsAsync(CancellationToken token) => Run<IReadOnlyList<CameraSetting>>(() =>
    {
        EnsureConnected();
        return SettingNames.Select(entry =>
        {
            try { return ReadSetting(entry.Key, entry.Label); }
            catch (IOException e) { return new CameraSetting(entry.Key, entry.Label, null, [], e.Message); }
        }).ToArray();
    }, token);

    private void EnsureConnected()
    {
        if (camera == IntPtr.Zero) throw new InvalidOperationException("Camera chưa kết nối.");
    }
    private CameraSetting ReadSetting(string key, string label)
    {
        int value = 0, bulb = 0, count = 0, zoom = 0;
        if (key == "ShutterSpeed") Check(Api<GetPairCall>("XSDK_GetShutterSpeed")(camera, ref value, ref bulb), "GetShutterSpeed");
        else Check(Api<GetIntCall>("XSDK_Get" + key)(camera, ref value), "Get" + key);
        if (key == "Aperture") Check(Api<GetIntCall>("XSDK_GetLensZoomPos")(camera, ref zoom), "GetLensZoomPos");
        int Cap(IntPtr buffer, ref int n)
        {
            if (key == "ShutterSpeed") { int capable = 0; return Api<CapShutterCall>("XSDK_CapShutterSpeed")(camera, ref n, buffer, ref capable); }
            if (key == "Aperture") return Api<CapApertureCall>("XSDK_CapAperture")(camera, zoom, ref n, buffer);
            return Api<CapValuesCall>("XSDK_Cap" + key)(camera, ref n, buffer);
        }
        Check(Cap(IntPtr.Zero, ref count), "Cap" + key);
        if (count is < 0 or > 4096) throw new IOException("Danh sách thông số SDK không hợp lệ.");
        int capacity = count;
        var buffer = Marshal.AllocHGlobal(Math.Max(1, capacity) * sizeof(int));
        try
        {
            if (count > 0) Check(Cap(buffer, ref count), "Cap" + key);
            if (count < 0 || count > capacity) throw new IOException("Danh sách thông số đã thay đổi; hãy đọc lại.");
            var values = new int[count]; Marshal.Copy(buffer, values, 0, count);
            // Bulb requires a separate timed-shutter workflow, not the booth capture button.
            var options = bulb != 0 && key == "ShutterSpeed" ? Array.Empty<CameraSettingOption>() : values.Distinct().Select(v => new CameraSettingOption(v, FormatSetting(key, v))).ToArray();
            return new(key, label, value, options, bulb != 0 && key == "ShutterSpeed" ? "Bulb: đổi tốc độ trên camera trước." : null);
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    public static void ValidateSettingChanges(IReadOnlyDictionary<string, int> changes)
    {
        if (changes.Keys.Any(k => !SettingNames.Any(s => s.Key == k))) throw new ArgumentException("Thông số không hỗ trợ.");
        if (changes.Count > 1 && (changes.ContainsKey("AEMode") || changes.ContainsKey("WBMode")))
            throw new ArgumentException("Áp riêng chế độ chụp hoặc WB trước, đọc lại rồi chỉnh các thông số còn lại.");
    }

    public Task ApplySettingsAsync(IReadOnlyDictionary<string, int> changes, string journalDirectory, CancellationToken token)
    {
        var requested = changes.ToDictionary(x => x.Key, x => x.Value);
        ValidateSettingChanges(requested);
        return Run(() =>
        {
            EnsureConnected();
            foreach (var (key, value) in requested)
            {
                var setting = ReadSetting(key, key);
                if (setting.Error is not null || !setting.Options.Any(o => o.Value == value)) throw new InvalidOperationException($"Camera không cho phép giá trị {key} đã chọn; hãy đọc lại.");
                if (key == "WBColorTemp")
                {
                    int wb = 0; Check(Api<GetIntCall>("XSDK_GetWBMode")(camera, ref wb), "GetWBMode");
                    if (wb != 0x8007) throw new InvalidOperationException("Chọn và áp WB Kelvin trước khi chỉnh nhiệt độ màu.");
                }
            }
            if (requested.Count == 0) return true;
            Directory.CreateDirectory(journalDirectory);
            string path = Path.Combine(journalDirectory, Guid.NewGuid() + ".settings-intent.json");
            using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
            { JsonSerializer.Serialize(file, new { Device.Model, Requested = requested, At = DateTimeOffset.UtcNow }); file.Flush(true); }
            foreach (var (key, value) in requested)
            {
                token.ThrowIfCancellationRequested();
                Check(key == "ShutterSpeed" ? Api<SetPairCall>("XSDK_SetShutterSpeed")(camera, value, 0) : Api<SetIntCall>("XSDK_Set" + key)(camera, value), "Set" + key);
                if (ReadSetting(key, key).Current != value) throw new IOException($"{key}: giá trị đọc lại khác yêu cầu. Có thể một phần đã áp; không tự gửi lại.");
            }
            File.WriteAllText(path + ".confirmed", DateTimeOffset.UtcNow.ToString("O"));
            return true;
        }, token);
    }

    public static string FormatSetting(string key, int value) => key switch
    {
        "AEMode" => value switch { 1 => "M · Thủ công", 3 => "A · Ưu tiên khẩu", 4 => "S · Ưu tiên tốc", 6 => "P · Tự động", _ => $"Mã {value}" },
        "Aperture" => $"f/{value / 100.0:0.##}",
        "Sensitivity" => value > 0 ? $"ISO {value}" : $"Auto ({value})",
        "WBColorTemp" => $"{value} K",
        "WBMode" => value switch { 2 => "Auto", 4 => "Daylight", 6 => "Incandescent", 8 => "Underwater", 0x8006 => "Shade", 0x8007 => "Kelvin", >= 0x8008 and <= 0x800c => $"Custom {value - 0x8007}", >= 0x8001 and <= 0x8003 => $"Fluorescent {value - 0x8000}", 0x8020 => "Auto · White priority", 0x8021 => "Auto · Ambience priority", _ => $"WB 0x{value:X}" },
        "ShutterSpeed" => ShutterLabels.TryGetValue(value, out var name) ? name : $"SDK {value}",
        _ => value.ToString()
    };
    // Display labels from XAPI.H; preserve exact SDK values instead of rounding reciprocal seconds.
    private static readonly Dictionary<int, string> ShutterLabels = new()
    {
        [122]="1/8000 s", [244]="1/4000 s", [488]="1/2000 s", [976]="1/1000 s", [1953]="1/500 s",
        [3100]="1/320 s", [3906]="1/250 s", [4921]="1/200 s", [5524]="1/180 s", [6200]="1/160 s",
        [7812]="1/125 s", [9843]="1/100 s", [11048]="1/90 s", [12401]="1/80 s", [15625]="1/60 s",
        [19686]="1/50 s", [24803]="1/40 s", [31250]="1/30 s", [39372]="1/25 s", [49606]="1/20 s",
        [62500]="1/15 s", [99212]="1/10 s", [125000]="1/8 s", [250000]="1/4 s", [500000]="1/2 s", [1000000]="1 s"
    };
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int CapValuesCall(IntPtr handle, ref int count, IntPtr values);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int CapShutterCall(IntPtr handle, ref int count, IntPtr values, ref int bulb);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int CapApertureCall(IntPtr handle, int zoom, ref int count, IntPtr values);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int GetPairCall(IntPtr handle, ref int value, ref int other);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int SetPairCall(IntPtr handle, int value, int other);
}
