using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace FotoAutomat.Admin.Camera;

public sealed class CameraPageViewModel : ObservableObject
{
    // GIẢ ĐỊNH: giá trị cân bằng trắng "Kelvin" do SDK trả về đúng chữ này. Sai thì sửa hằng số này.
    private const string KelvinLabel = "Kelvin";

    private readonly ICameraService _camera;
    private readonly ICameraConfigStore _store;
    private CameraConfig _saved;

    private ConnectionState _state = ConnectionState.Disconnected;
    private string? _brand;
    private string? _selectedCamera;
    private string _preferredModel;
    private string _sdkPath;
    private string _connectedName = "";
    private bool _isBusy;
    private bool _hasRead;
    private bool _detectDone;
    private bool _initialized;
    private bool _isReviewing;
    private ImageSource? _liveViewImage;
    private string _errorMessage = "";
    private string _actionMessage = "";

    public CameraPageViewModel(ICameraService camera, ICameraConfigStore store)
    {
        _camera = camera;
        _store = store;

        Brands = new ObservableCollection<string>(camera.Brands);

        var loaded = store.Load();
        _brand = Brands.Contains(loaded.Brand) ? loaded.Brand : Brands.FirstOrDefault();
        _saved = loaded with { Brand = _brand ?? "" };
        _preferredModel = _saved.Model;
        _sdkPath = _saved.SdkPath;

        Parameters = new ObservableCollection<CameraParameter>
        {
            // Thứ tự = thứ tự hiển thị trên lưới 2 cột: mỗi hàng là một cặp liên quan.
            new CameraParameter("mode", "Chế độ chụp"),
            new CameraParameter("iso", "ISO"),
            new CameraParameter("shutter", "Tốc độ màn trập"),
            new CameraParameter("aperture", "Khẩu độ"),
            new CameraParameter("wb", "Cân bằng trắng"),
            new CameraParameter("kelvin", "Nhiệt độ màu"),
        };
        foreach (var p in Parameters)
        {
            _saved.Values.TryGetValue(p.Key, out var savedValue);
            p.SetSaved(savedValue, alsoSelect: true);
            p.Changed += OnParameterChanged;
        }
        UpdateKelvinAvailability();

        Cameras.CollectionChanged += (_, _) => OnPropertyChanged(nameof(NoCameraFound));
        _camera.LiveViewFrame += OnLiveViewFrame;
        if (_camera is ICameraConnectionStatus status) status.ConnectionFailed += message => { ResetSession(); State = ConnectionState.Error; ErrorMessage = message; };

        DetectCommand = new AsyncRelayCommand(DetectAsync, () => !IsBusy && State != ConnectionState.Connected && State != ConnectionState.Connecting);
        ConnectCommand = new AsyncRelayCommand(ConnectAsync, () => !IsBusy && State != ConnectionState.Connected && !string.IsNullOrEmpty(SelectedCamera));
        DisconnectCommand = new AsyncRelayCommand(DisconnectAsync, () => IsConnected && !IsBusy);
        ReadCommand = new AsyncRelayCommand(ReadAsync, () => IsConnected && !IsBusy);
        ApplyCommand = new AsyncRelayCommand(ApplyAsync, () => IsConnected && _hasRead && HasPendingApply && !IsBusy);
        CaptureTestCommand = new AsyncRelayCommand(CaptureTestAsync, () => IsConnected && !IsBusy);
        ReturnToLiveViewCommand = new AsyncRelayCommand(ReturnToLiveViewAsync, () => IsConnected && IsReviewing && !IsBusy);
        UndoCommand = new RelayCommand(Undo, () => HasUnsavedConfig && !IsBusy);
        SaveCommand = new RelayCommand(Save, () => HasUnsavedConfig && !IsBusy);
        ShowGuideCommand = new RelayCommand(() => GuideRequested?.Invoke());
    }

    /// <summary>Trang gọi hàm này khi hiển thị lần đầu để dò máy ảnh.</summary>
    public async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;
        await DetectAsync();
    }

    /// <summary>Phát ra khi bấm "Hướng dẫn". Nơi chứa trang tự mở cửa sổ hướng dẫn.</summary>
    public event Action? GuideRequested;

    public async Task DeactivateAsync()
    {
        while (IsBusy) await Task.Delay(50);
        IsBusy = true;
        try { await TryDisconnectQuietlyAsync(); State = ConnectionState.Disconnected; _initialized = false; }
        finally { IsBusy = false; }
    }
    // ===== Danh sách =====
    public ObservableCollection<string> Brands { get; }
    public ObservableCollection<string> Cameras { get; } = new();
    public ObservableCollection<CameraParameter> Parameters { get; }

    // ===== Lệnh =====
    public ICommand DetectCommand { get; }
    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand ReadCommand { get; }
    public ICommand ApplyCommand { get; }
    public ICommand CaptureTestCommand { get; }
    public ICommand ReturnToLiveViewCommand { get; }
    public ICommand UndoCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand ShowGuideCommand { get; }

    // ===== Kết nối =====
    public ConnectionState State
    {
        get => _state;
        private set
        {
            if (SetProperty(ref _state, value)) RaiseStateDependents();
        }
    }

    public bool IsConnected => State == ConnectionState.Connected;
    public bool ShowSummary => State == ConnectionState.Connected;
    public bool ShowPicker => State != ConnectionState.Connected;
    public bool CanEditConnection => State != ConnectionState.Connected && State != ConnectionState.Connecting;

    public string StatusText => State switch
    {
        ConnectionState.Connecting => "Đang kết nối…",
        ConnectionState.Connected => "Đã kết nối",
        ConnectionState.Error => "Lỗi kết nối",
        _ => "Chưa kết nối",
    };

    /// <summary>Khớp Tag của StatusBadgeStyle: Neutral | Warning | Success | Error.</summary>
    public string StatusKind => State switch
    {
        ConnectionState.Connecting => "Warning",
        ConnectionState.Connected => "Success",
        ConnectionState.Error => "Error",
        _ => "Neutral",
    };

    public string? SelectedBrand
    {
        get => _brand;
        set
        {
            if (!SetProperty(ref _brand, value)) return;
            RaiseDirty();
            if (_initialized && !IsConnected && !string.IsNullOrEmpty(value)) _ = DetectAsync();
        }
    }

    public string? SelectedCamera
    {
        get => _selectedCamera;
        set
        {
            if (!SetProperty(ref _selectedCamera, value)) return;
            // ComboBox đẩy null khi danh sách bị xoá; chỉ ghi nhớ lựa chọn thật của người dùng.
            if (value != null) _preferredModel = value;
            RaiseDirty();
        }
    }

    public string SdkPath
    {
        get => _sdkPath;
        set
        {
            if (SetProperty(ref _sdkPath, value ?? "")) RaiseDirty();
        }
    }

    public string ConnectedName
    {
        get => _connectedName;
        private set => SetProperty(ref _connectedName, value);
    }

    /// <summary>Đã dò xong mà không thấy máy nào.</summary>
    public bool NoCameraFound => _detectDone && Cameras.Count == 0 && State != ConnectionState.Connected && !IsBusy;

    // ===== Live view =====
    public ImageSource? LiveViewImage
    {
        get => _liveViewImage;
        private set
        {
            if (!SetProperty(ref _liveViewImage, value)) return;
            OnPropertyChanged(nameof(HasFrame));
            OnPropertyChanged(nameof(LiveViewCaption));
        }
    }

    public bool HasFrame => LiveViewImage != null;

    public bool IsReviewing
    {
        get => _isReviewing;
        private set
        {
            if (!SetProperty(ref _isReviewing, value)) return;
            OnPropertyChanged(nameof(LiveViewCaption));
            Requery();
        }
    }

    public string LiveViewCaption =>
        !IsConnected ? "Chưa có tín hiệu"
        : IsReviewing ? "Đang xem ảnh chụp thử"
        : HasFrame ? "Live view đang chạy"
        : "Đang chờ tín hiệu";

    // ===== Thông số =====
    public string ParamsHint =>
        !IsConnected ? "Kết nối máy ảnh để đọc và chỉnh thông số."
        : !_hasRead ? "Bấm Đọc từ máy để lấy thông số hiện tại."
        : "";

    public bool HasParamsHint => ParamsHint.Length > 0;

    /// <summary>Có thông số đã đổi nhưng chưa áp lên máy.</summary>
    public bool HasPendingApply => Parameters.Any(p => p.IsPendingApply);

    // ===== Trạng thái chung =====
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetProperty(ref _isBusy, value)) return;
            OnPropertyChanged(nameof(NoCameraFound));
            Requery();
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (!SetProperty(ref _errorMessage, value ?? "")) return;
            OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => ErrorMessage.Length > 0;

    /// <summary>Kết quả của thao tác vừa xong, ví dụ "Đã áp lên máy · 21:04:12".</summary>
    public string ActionMessage
    {
        get => _actionMessage;
        private set => SetProperty(ref _actionMessage, value ?? "");
    }

    // ===== Cấu hình PC (thanh lưu ở dưới) =====
    public bool HasUnsavedConfig =>
        (SelectedBrand ?? "") != _saved.Brand
        || _preferredModel != _saved.Model
        || SdkPath != _saved.SdkPath
        || Parameters.Any(p => p.IsUnsaved);

    public string SaveStatusText => HasUnsavedConfig ? "Có thay đổi chưa lưu" : "Cấu hình đã lưu";

    // ===== Thao tác =====
    private async Task RunAsync(Func<Task> work, string? doneMessage = null)
    {
        IsBusy = true;
        ErrorMessage = "";
        ActionMessage = "";
        try
        {
            await work();
            if (doneMessage != null) ActionMessage = Stamp(doneMessage);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string Stamp(string message) => $"{message} · {DateTime.Now:HH:mm:ss}";

    private Task DetectAsync() => RunAsync(async () =>
    {
        var brand = SelectedBrand;
        if (string.IsNullOrEmpty(brand)) return;

        Cameras.Clear(); SelectedCamera = null;
        var found = await _camera.DetectAsync(brand);
        Cameras.Clear();
        foreach (var name in found) Cameras.Add(name);
        _detectDone = true;
        OnPropertyChanged(nameof(NoCameraFound));
        SelectedCamera = found.Contains(_preferredModel) ? _preferredModel : found.FirstOrDefault();
    });

    private async Task ConnectAsync()
    {
        IsBusy = true;
        ErrorMessage = "";
        ActionMessage = "";
        State = ConnectionState.Connecting;
        try
        {
            await _camera.ConnectAsync(SelectedBrand!, SelectedCamera!, SdkPath);
            ConnectedName = (_camera as ICameraConnectionStatus)?.ConnectedModel ?? SelectedCamera!;
            IsReviewing = false;
            State = ConnectionState.Connected;

            await _camera.StartLiveViewAsync();
            await ReadCoreAsync(); // đọc luôn thông số để người dùng không phải bấm thêm
            ActionMessage = Stamp("Đã kết nối");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            await TryDisconnectQuietlyAsync();
            State = ConnectionState.Error;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task TryDisconnectQuietlyAsync()
    {
        try
        {
            try { await _camera.StopLiveViewAsync(); } finally { await _camera.DisconnectAsync(); }
        }
        catch
        {
            // Đã có lỗi gốc để hiển thị, bỏ qua lỗi dọn dẹp.
        }
        ResetSession();
    }

    private Task DisconnectAsync() => RunAsync(async () =>
    {
        try { await _camera.StopLiveViewAsync(); } finally { await _camera.DisconnectAsync(); }
        ResetSession();
        State = ConnectionState.Disconnected;
    }, "Đã ngắt kết nối");

    private void ResetSession()
    {
        LiveViewImage = null;
        IsReviewing = false;
        _hasRead = false;
        ConnectedName = "";
        foreach (var p in Parameters) p.ForgetApplied();
        RaiseStateDependents();
    }

    private Task ReadAsync() => RunAsync(ReadCoreAsync, "Đã đọc thông số từ máy");

    private async Task ReadCoreAsync()
    {
        var snapshot = await _camera.ReadSettingsAsync();
        foreach (var p in Parameters)
        {
            if (snapshot.TryGetValue(p.Key, out var choice))
            { p.LoadFromCamera(choice.Options, choice.Current); p.IsAvailable = choice.Options.Count > 0; }
        }
        _hasRead = true;
        UpdateKelvinAvailability();
        RaiseStateDependents();
        RaiseDirty();
    }

    private Task ApplyAsync() => RunAsync(async () =>
    {
        var values = Parameters
            .Where(p => p.IsAvailable && p.IsPendingApply && p.SelectedValue != null)
            .ToDictionary(p => p.Key, p => p.SelectedValue!);

        try { await _camera.ApplySettingsAsync(values); }
        catch { try { await ReadCoreAsync(); } catch { _hasRead = false; foreach (var p in Parameters) p.ForgetApplied(); RaiseStateDependents(); } throw; }
        await ReadCoreAsync();
    }, "Đã áp lên máy");

    private Task CaptureTestAsync() => RunAsync(async () =>
    {
        var image = await _camera.CaptureTestAsync();
        IsReviewing = true;
        LiveViewImage = image;
    }, "Đã chụp thử");

    private Task ReturnToLiveViewAsync() => RunAsync(async () =>
    {
        await _camera.StartLiveViewAsync();
        IsReviewing = false;
    });

    private void Save()
    {
        try
        {
            var config = BuildConfig();
            _store.Save(config);
            _saved = config;
            foreach (var p in Parameters) p.MarkSaved();
            ErrorMessage = "";
            ActionMessage = Stamp("Đã lưu cấu hình PC");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        RaiseDirty();
    }

    private void Undo()
    {
        SdkPath = _saved.SdkPath;
        _preferredModel = _saved.Model;

        if (SelectedBrand != _saved.Brand)
            SelectedBrand = _saved.Brand; // kích hoạt dò lại và tự chọn lại model đã lưu
        else if (_saved.Model.Length > 0 && Cameras.Contains(_saved.Model))
            SelectedCamera = _saved.Model;

        foreach (var p in Parameters) p.RevertToSaved();

        ErrorMessage = "";
        ActionMessage = Stamp("Đã hoàn tác");
        RaiseDirty();
    }

    private CameraConfig BuildConfig() => new(
        SelectedBrand ?? "",
        _preferredModel,
        SdkPath,
        Parameters.Where(p => p.SelectedValue != null).ToDictionary(p => p.Key, p => p.SelectedValue!));

    // ===== Nội bộ =====
    private void OnLiveViewFrame(ImageSource frame)
    {
        // Có thể gọi từ luồng nền của SDK.
        Application.Current?.Dispatcher.BeginInvoke((Action)(() =>
        {
            if (IsConnected && !IsReviewing) LiveViewImage = frame;
        }));
    }

    private void OnParameterChanged(object? sender, EventArgs e)
    {
        UpdateKelvinAvailability();
        RaiseDirty();
    }

    private void UpdateKelvinAvailability()
    {
        var whiteBalance = Parameters.First(p => p.Key == "wb");
        var kelvin = Parameters.First(p => p.Key == "kelvin");
        kelvin.IsAvailable = kelvin.Options.Count > 0 && string.Equals(whiteBalance.SelectedValue, KelvinLabel, StringComparison.OrdinalIgnoreCase);
    }

    private void RaiseDirty()
    {
        OnPropertyChanged(nameof(HasPendingApply));
        OnPropertyChanged(nameof(HasUnsavedConfig));
        OnPropertyChanged(nameof(SaveStatusText));
        Requery();
    }

    private void RaiseStateDependents()
    {
        OnPropertyChanged(nameof(IsConnected));
        OnPropertyChanged(nameof(ShowSummary));
        OnPropertyChanged(nameof(ShowPicker));
        OnPropertyChanged(nameof(CanEditConnection));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(StatusKind));
        OnPropertyChanged(nameof(ParamsHint));
        OnPropertyChanged(nameof(HasParamsHint));
        OnPropertyChanged(nameof(LiveViewCaption));
        OnPropertyChanged(nameof(NoCameraFound));
        Requery();
    }

    private static void Requery() => CommandManager.InvalidateRequerySuggested();
}



