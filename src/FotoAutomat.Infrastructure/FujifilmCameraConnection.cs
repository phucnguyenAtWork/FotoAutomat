using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using FotoAutomat.Core;

namespace FotoAutomat.Infrastructure;

/// <summary>Windows x64 XAPI adapter. Native calls have one process owner and one worker thread.
/// ABI and constants verified against SDK reference Rev. 1.34.1.0 and XAPI.H/X-H2.h.</summary>
public sealed partial class FujifilmCameraConnection : ICameraConnection
{
    private static int owner;
    private readonly BlockingCollection<Action> queue = new();
    private readonly Thread worker;
    private readonly string directory;
    private IntPtr library, camera;
    private bool initialized, ownsSdk, live, priorityChanged;
    private int previousPriority, disposed;
    public CameraDevice Device { get; private set; } = new("USB:single", CameraBrand.Fujifilm, "Fujifilm USB (chưa kết nối)", true, true);

    public FujifilmCameraConnection(string sdkDirectory)
    {
        directory = ResolveDirectory(sdkDirectory);
        worker = new Thread(() => { foreach (var action in queue.GetConsumingEnumerable()) action(); }) { IsBackground = true, Name = "Fujifilm SDK" };
        worker.Start();
    }
    public static string ResolveDirectory(string path)
    {
        var root = Path.GetFullPath(path);
        var candidates = new[] { root, Path.Combine(root, "REDISTRIBUTABLES", "Windows", "64bit"), Path.Combine(root, "SDK13410", "REDISTRIBUTABLES", "Windows", "64bit") };
        return candidates.FirstOrDefault(p => File.Exists(Path.Combine(p, "XAPI.dll")) && File.Exists(Path.Combine(p, "XSDK.DAT")))
            ?? throw new DirectoryNotFoundException("Chọn thư mục SDK có REDISTRIBUTABLES/Windows/64bit (XAPI.dll và XSDK.DAT).");
    }
    private Task<T> Run<T>(Func<T> action, CancellationToken token)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) != 0, this);
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        queue.Add(() => { try { token.ThrowIfCancellationRequested(); completion.SetResult(action()); } catch (Exception e) { completion.SetException(e); } });
        return completion.Task;
    }
    private T Api<T>(string name) where T : Delegate => Marshal.GetDelegateForFunctionPointer<T>(NativeLibrary.GetExport(library, name));
    private void Check(int result, string operation)
    {
        if (result == 0) return;
        int api = 0, error = 0;
        Api<ErrorCall>("XSDK_GetErrorNumber")(camera, ref api, ref error);
        throw new IOException($"Fujifilm {operation}: lỗi 0x{error:X8} (API 0x{api:X}). Kiểm tra USB, chế độ tether và phần mềm đang giữ camera.");
    }
    public Task ConnectAsync(CancellationToken token) => Run(() =>
    {
        if (!OperatingSystem.IsWindows() || !Environment.Is64BitProcess) throw new PlatformNotSupportedException("Fujifilm adapter cần Windows x64.");
        if (ownsSdk || Interlocked.CompareExchange(ref owner, 1, 0) != 0) throw new InvalidOperationException("Một phiên Fujifilm khác đang dùng SDK.");
        ownsSdk = true;
        try
        {
            library = LoadLibraryEx(Path.Combine(directory, "XAPI.dll"), IntPtr.Zero, 0x00000100 | 0x00000800);
            if (library == IntPtr.Zero) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Không nạp được XAPI.dll x64 hoặc dependency.");
            Check(Api<HandleCall>("XSDK_Init")(library), "Init"); initialized = true;
            int count = 0;
            Check(Api<DetectCall>("XSDK_Detect")(1, "", "", ref count), "Detect USB");
            if (count != 1) throw new InvalidOperationException($"Tìm thấy {count} camera. Bản thử nghiệm yêu cầu kết nối đúng một máy Fujifilm USB.");
            int mode = 0;
            Check(Api<OpenCall>("XSDK_OpenEx")("ENUM:0", ref camera, ref mode, IntPtr.Zero), "Open");
            if ((mode & 1) == 0) throw new NotSupportedException("Camera chưa ở chế độ USB tether shooting.");
            var info = Marshal.AllocHGlobal(1857);
            try
            {
                Check(Api<PointerCall>("XSDK_GetDeviceInfo")(camera, info), "DeviceInfo");
                var model = Marshal.PtrToStringAnsi(info + 512, 256)!.TrimEnd('\0');
                Device = Device with { Model = model };
            }
            finally { Marshal.FreeHGlobal(info); }
            Check(Api<GetIntCall>("XSDK_GetPriorityMode")(camera, ref previousPriority), "GetPriorityMode");
            // StartLiveView is documented as PC PRIORITY only. Restore on normal disconnect.
            if (previousPriority != 2) { Check(Api<SetIntCall>("XSDK_SetPriorityMode")(camera, 2), "SetPriorityMode"); priorityChanged = true; }
            return true;
        }
        catch { Cleanup(); throw; }
    }, token);
    public Task StartLiveViewAsync(CancellationToken token) => Run(() =>
    {
        if (!live) { Check(Api<PropCall>("XSDK_SetProp")(camera, 0x3301, 0), "StartLiveView"); live = true; }
        return true;
    }, token);
    public Task StopLiveViewAsync(CancellationToken token) => Run(() =>
    {
        StopLive(); DrainLive(token); return true;
    }, token);
    private void StopLive()
    {
        if (!live) return;
        Check(Api<PropCall>("XSDK_SetProp")(camera, 0x3302, 0), "StopLiveView"); live = false;
    }
    private ImageInfo Next()
    {
        var info = new ImageInfo { Name = new byte[32] };
        Check(Api<ImageInfoCall>("XSDK_ReadImageInfo")(camera, ref info), "ReadImageInfo"); return info;
    }
    private byte[] Read(ImageInfo info)
    {
        if (info.Size is <= 0 or > 256 * 1024 * 1024) throw new InvalidDataException("Kích thước ảnh từ SDK không hợp lệ.");
        var data = new byte[info.Size];
        Check(Api<ReadCall>("XSDK_ReadImage")(camera, data, (uint)data.Length), "ReadImage");
        return data;
    }
    private void DrainLive(CancellationToken token)
    {
        for (int i = 0; i < 64; i++)
        {
            token.ThrowIfCancellationRequested(); var info = Next(); int format = info.Format & 255;
            if (format == 5) return;
            if (format != 4) throw new InvalidOperationException("Camera còn ảnh chụp trong buffer. Dừng để xử lý ảnh cũ; không tự xóa hoặc nhận làm ảnh mới.");
            Read(info);
        }
        throw new IOException("Live view chưa dừng cấp frame.");
    }
    public Task<CameraFrame?> ReadLiveViewAsync(CancellationToken token) => Run<CameraFrame?>(() =>
    {
        var info = Next(); int format = info.Format & 255;
        if (format == 5) return null;
        if (format != 4) throw new IOException("Có ảnh chụp ngoài phiên trong buffer. Ngắt kết nối và xử lý ảnh trước khi tiếp tục.");
        return new CameraFrame(Read(info), DateTimeOffset.UtcNow);
    }, token);
    public Task CaptureAsync(CameraShot shot, CancellationToken token) => Run(() =>
    {
        if (live) throw new InvalidOperationException("Dừng live view trước khi chụp.");
        DrainLive(token);
        // Reserve the output before shutter so file errors cannot cause an unrecorded retry.
        using var output = new FileStream(shot.DestinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        int option = 1, autofocus = 0;
        try
        {
            Check(Api<ReleaseCall>("XSDK_Release")(camera, 0x0200, ref option, ref autofocus), "Half shutter");
            if (autofocus == 0) throw new IOException("Camera báo không lấy nét được. Không kích hoạt màn trập; kiểm tra lấy nét rồi kết nối lại.");
            token.ThrowIfCancellationRequested(); option = 1;
            Check(Api<ReleaseCall>("XSDK_Release")(camera, 0x0304, ref option, ref autofocus), "Shutter");
            var watch = Stopwatch.StartNew(); bool rawSaved = false;
            while (watch.Elapsed < TimeSpan.FromSeconds(30))
            {
                token.ThrowIfCancellationRequested(); var info = Next(); int format = info.Format & 255;
                if (format == 5) { Thread.Sleep(80); continue; }
                if (format == 7) { var bytes = Read(info); output.Write(bytes); output.Flush(true); return true; }
                if (format == 1 && !rawSaved)
                {
                    using var raw = new FileStream(Path.ChangeExtension(shot.DestinationPath, ".raf"), FileMode.CreateNew, FileAccess.Write);
                    raw.Write(Read(info)); raw.Flush(true); rawSaved = true; continue;
                }
                throw new NotSupportedException("Ảnh nhận không phải JPEG/RAW+JPEG. Chọn JPEG trên máy; không tự đổi định dạng camera.");
            }
            throw new TimeoutException("Quá 30 giây chưa nhận JPEG. Không tự chụp lại; kiểm tra ảnh và nhật ký.");
        }
        finally { option = 1; Api<ReleaseCall>("XSDK_Release")(camera, 0x0004, ref option, ref autofocus); }
    }, token);
    private void Cleanup()
    {
        try
        {
            if (camera != IntPtr.Zero)
            {
                if (live) Api<PropCall>("XSDK_SetProp")(camera, 0x3302, 0);
                if (priorityChanged) Api<SetIntCall>("XSDK_SetPriorityMode")(camera, previousPriority);
                Api<HandleCall>("XSDK_Close")(camera);
            }
            if (initialized) Api<EmptyCall>("XSDK_Exit")();
        }
        finally
        {
            camera = IntPtr.Zero; initialized = live = priorityChanged = false;
            if (library != IntPtr.Zero) { NativeLibrary.Free(library); library = IntPtr.Zero; }
            if (ownsSdk) { ownsSdk = false; Interlocked.Exchange(ref owner, 0); }
        }
    }
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        queue.Add(() => { try { Cleanup(); completion.SetResult(); } catch (Exception e) { completion.SetException(e); } });
        queue.CompleteAdding(); await completion.Task;
    }
    [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr LoadLibraryEx(string path, IntPtr file, uint flags);
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct ImageInfo
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] Name;
        public int Format, Size, Height, Width, Depth, PreviewSize;
        public IntPtr Handle;
    }
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int HandleCall(IntPtr handle);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int EmptyCall();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)] private delegate int DetectCall(int type, string address, string name, ref int count);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)] private delegate int OpenCall(string name, ref IntPtr handle, ref int mode, IntPtr option);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int PointerCall(IntPtr handle, IntPtr info);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int GetIntCall(IntPtr handle, ref int value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int SetIntCall(IntPtr handle, int value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int PropCall(IntPtr handle, int code, int count);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int ErrorCall(IntPtr handle, ref int api, ref int error);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int ImageInfoCall(IntPtr handle, ref ImageInfo info);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int ReadCall(IntPtr handle, [Out] byte[] data, uint size);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int ReleaseCall(IntPtr handle, int mode, ref int option, ref int autofocus);
}
