namespace FotoAutomat.Core;

/// <summary>Điểm trên ảnh, tọa độ chuẩn hóa 0–1 theo cả ảnh.</summary>
public sealed record FacePoint(double X, double Y);

/// <summary>Mức thông tin nhận diện được cho một mặt; quyết định công cụ Retouch nào được chạy.</summary>
public enum FaceEvidence { Box, Landmarks, Mesh }

/// <summary>Mặt nhận diện trên một ảnh khách, tọa độ chuẩn hóa theo ảnh đó.
/// Không có giá trị mặc định cho mắt/miệng: thiếu điểm nào thì công cụ cần điểm đó không chạy.</summary>
public sealed record DetectedFace(double X, double Y, double Width, double Height, double? Score = null,
    FacePoint? LeftEye = null, FacePoint? RightEye = null, FacePoint? Mouth = null, FacePoint[]? InnerLips = null)
{
    public FaceEvidence Evidence => LeftEye is null || RightEye is null || Mouth is null ? FaceEvidence.Box
        : InnerLips is { Length: >= 3 } ? FaceEvidence.Mesh : FaceEvidence.Landmarks;
}

/// <summary>Nhận diện mặt trên ảnh BGRA8. Trả về danh sách rỗng khi không có mặt; không bao giờ đoán vị trí.</summary>
public interface IFaceAnalyzer
{
    string Name { get; }
    Task<IReadOnlyList<DetectedFace>> DetectAsync(byte[] bgra, int width, int height, CancellationToken cancellationToken = default);
}

/// <summary>Công cụ Retouch được chạy cho một mặt và lý do bỏ qua (nếu có), để ghi log từng ảnh.</summary>
public sealed record RetouchFacePlan(int Index, bool Skin, bool Features, bool Teeth, string? Skipped = null);
