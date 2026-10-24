using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Media;

namespace FotoAutomat.Admin.Camera;

public enum ConnectionState { Disconnected, Connecting, Connected, Error }

/// <summary>Danh sách giá trị máy ảnh cho phép + giá trị hiện tại của một thông số.</summary>
public sealed record CameraSettingChoice(IReadOnlyList<string> Options, string Current);

/// <summary>Cấu hình lưu trên PC. Values dùng khoá: mode, iso, shutter, aperture, wb, kelvin.</summary>
public sealed record CameraConfig(
    string Brand,
    string Model,
    string SdkPath,
    IReadOnlyDictionary<string, string> Values);

/// <summary>
/// Cầu nối tới lớp SDK máy ảnh hiện có (Canon EDSDK...). Tôi chưa thấy mã của bạn nên đây chỉ là hợp đồng, cần viết adapter.
/// </summary>
public interface ICameraService
{
    IReadOnlyList<string> Brands { get; }

    /// <summary>Phát ra khung live view. Có thể gọi từ luồng nền, ảnh phải đã Freeze().</summary>
    event Action<ImageSource>? LiveViewFrame;

    Task<IReadOnlyList<string>> DetectAsync(string brand);
    Task ConnectAsync(string brand, string model, string sdkPath);
    Task DisconnectAsync();

    Task StartLiveViewAsync();
    Task StopLiveViewAsync();

    Task<IReadOnlyDictionary<string, CameraSettingChoice>> ReadSettingsAsync();
    Task ApplySettingsAsync(IReadOnlyDictionary<string, string> values);

    /// <summary>Chụp thử và trả ảnh đã Freeze(). Service tự tạm dừng live view nếu máy yêu cầu.</summary>
    Task<ImageSource> CaptureTestAsync();
}

public interface ICameraConfigStore
{
    CameraConfig Load();
    void Save(CameraConfig config);
}

/// <summary>Một thông số máy ảnh (ISO, khẩu độ...). Theo dõi 2 mốc: đã áp lên máy và đã lưu vào cấu hình PC.</summary>
public sealed class CameraParameter : ObservableObject
{
    private string? _selected;
    private string? _applied;
    private string? _saved;
    private bool _isAvailable = true;
    private bool _suspend;

    public CameraParameter(string key, string label)
    {
        Key = key;
        Label = label;
    }

    public string Key { get; }
    public string Label { get; }
    public ObservableCollection<string> Options { get; } = new();

    /// <summary>Phát ra mỗi khi người dùng đổi giá trị, để ViewModel cập nhật trạng thái nút.</summary>
    public event EventHandler? Changed;

    public string? SelectedValue
    {
        get => _selected;
        set
        {
            if (!SetProperty(ref _selected, value)) return;
            if (_suspend) return;
            RaiseState();
        }
    }

    /// <summary>False thì ô bị khoá (ví dụ Nhiệt độ màu khi cân bằng trắng không phải Kelvin).</summary>
    public bool IsAvailable
    {
        get => _isAvailable;
        set => SetProperty(ref _isAvailable, value);
    }

    /// <summary>Đã đọc từ máy và giá trị đang chọn khác giá trị trên máy.</summary>
    public bool IsPendingApply => _applied != null && _applied != _selected;

    /// <summary>Giá trị đang chọn khác giá trị trong cấu hình đã lưu.</summary>
    public bool IsUnsaved => _saved != _selected;

    /// <summary>Nạp danh sách và giá trị từ máy. Không phát Changed ở giữa chừng.</summary>
    public void LoadFromCamera(IEnumerable<string> options, string current)
    {
        _suspend = true;
        try
        {
            // Clear() làm ComboBox đẩy null ngược về SelectedValue, nên đặt lại giá trị sau cùng.
            Options.Clear();
            foreach (var o in options) Options.Add(o);
            _applied = current;
            SelectedValue = current;
        }
        finally { _suspend = false; }
        RaiseState();
    }

    public void SetSaved(string? value, bool alsoSelect)
    {
        _saved = value;
        if (alsoSelect)
        {
            // Trước khi đọc máy, chưa có danh sách: dùng giá trị đã lưu làm lựa chọn duy nhất để ô vẫn hiển thị.
            if (value != null && !Options.Contains(value)) Options.Add(value);
            _suspend = true;
            try { SelectedValue = value; }
            finally { _suspend = false; }
        }
        RaiseState();
    }

    public void MarkApplied()
    {
        _applied = _selected;
        RaiseState();
    }

    public void MarkSaved()
    {
        _saved = _selected;
        RaiseState();
    }

    public void ForgetApplied()
    {
        _applied = null;
        RaiseState();
    }

    public void RevertToSaved()
    {
        if (_saved != null && !Options.Contains(_saved)) Options.Insert(0, _saved);
        SelectedValue = _saved;
    }

    private void RaiseState()
    {
        OnPropertyChanged(nameof(IsPendingApply));
        OnPropertyChanged(nameof(IsUnsaved));
        Changed?.Invoke(this, EventArgs.Empty);
    }
}

public interface ICameraConnectionStatus
{
    string ConnectedModel { get; }
    event Action<string>? ConnectionFailed;
}
