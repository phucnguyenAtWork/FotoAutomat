using FotoAutomat.Core;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace FotoAutomat.Infrastructure;

/// <summary>
/// Nhận diện nhiều mặt bằng MediaPipe (Apache 2.0, chuyển sang ONNX; nguồn và SHA256 trong models/mediapipe/README.md):
/// BlazeFace full-range tìm mặt và 6 điểm, rồi Face Mesh V2 cho từng mặt (478 điểm, viền môi trong).
/// Tham số anchor, chuẩn hóa và giải mã lấy từ face_detection.pbtxt / face_landmarker của MediaPipe.
/// Ảnh được dò ở toàn khung và các ô chồng nhau để không sót mặt nhỏ trong ảnh nhóm.
/// </summary>
public sealed class MediaPipeFaceAnalyzer : IFaceAnalyzer, IDisposable
{
    private const int DetectorSize = 192, MeshSize = 256, AnchorGrid = DetectorSize / 4;
    private const float MinScore = .6f, NmsThreshold = .3f, MinPresence = .5f;
    // Chỉ số Face Mesh: viền môi trong (theo chiều kim đồng hồ), tâm hai mống mắt, môi trên/dưới ở giữa.
    private static readonly int[] InnerLip = [78, 191, 80, 81, 82, 13, 312, 311, 310, 415, 308, 324, 318, 402, 317, 14, 87, 178, 88, 95];
    private const int IrisA = 468, IrisB = 473, UpperLip = 13, LowerLip = 14;

    private readonly InferenceSession detector, mesh;
    private readonly string detectorInput, meshInput;
    private readonly object gate = new();

    public MediaPipeFaceAnalyzer(string? modelDirectory = null)
    {
        string root = modelDirectory ?? DefaultModelDirectory;
        var options = new SessionOptions { IntraOpNumThreads = Math.Clamp(Environment.ProcessorCount / 2, 1, 4) };
        detector = new InferenceSession(Path.Combine(root, "face_detection_full_range.onnx"), options);
        mesh = new InferenceSession(Path.Combine(root, "face_landmarks_detector.onnx"), options);
        detectorInput = detector.InputMetadata.Keys.First(); meshInput = mesh.InputMetadata.Keys.First();
    }

    public static string DefaultModelDirectory => Path.Combine(AppContext.BaseDirectory, "models", "mediapipe");
    public static bool Available(string? modelDirectory = null) =>
        File.Exists(Path.Combine(modelDirectory ?? DefaultModelDirectory, "face_detection_full_range.onnx")) && File.Exists(Path.Combine(modelDirectory ?? DefaultModelDirectory, "face_landmarks_detector.onnx"));

    public string Name => "MediaPipe";

    public Task<IReadOnlyList<DetectedFace>> DetectAsync(byte[] bgra, int width, int height, CancellationToken cancellationToken = default) =>
        Task.Run(() => Detect(bgra, width, height, cancellationToken), cancellationToken);

    public IReadOnlyList<DetectedFace> Detect(byte[] bgra, int width, int height, CancellationToken ct = default)
    {
        if (width <= 0 || height <= 0 || (long)width * height * 4 != bgra.Length) throw new ArgumentException("Ảnh nhận diện không hợp lệ.");
        lock (gate)
        {
            var candidates = new List<Candidate>();
            foreach (var tile in Tiles(width, height)) { ct.ThrowIfCancellationRequested(); candidates.AddRange(DetectTile(bgra, width, height, tile)); }
            var faces = new List<DetectedFace>();
            foreach (var face in WeightedNms(candidates))
            {
                ct.ThrowIfCancellationRequested();
                faces.Add(Refine(bgra, width, height, face));
            }
            return faces;
        }
    }

    /// <summary>Một khung dò: (x, y, cạnh) theo pixel ảnh, hình vuông có thể vượt mép (phần ngoài ảnh đệm đen như MediaPipe).</summary>
    public readonly record struct Tile(double X, double Y, double Size);

    /// <summary>Toàn ảnh (giữ tỉ lệ) và lưới ô vuông cạnh = 1/2 cạnh ngắn, chồng 25%.</summary>
    public static IEnumerable<Tile> Tiles(int width, int height)
    {
        double whole = Math.Max(width, height);
        yield return new((width - whole) / 2, (height - whole) / 2, whole);
        double size = Math.Min(width, height) / 2d, step = size * .75;
        if (size < DetectorSize * 2) yield break;
        for (double y = 0; y < height - size * .25; y += step)
            for (double x = 0; x < width - size * .25; x += step)
                yield return new(Math.Min(x, width - size), Math.Min(y, height - size), size);
    }

    /// <summary>Mặt ứng viên, tọa độ pixel ảnh: khung và 6 điểm (mắt, mắt, mũi, miệng, tai, tai).</summary>
    public sealed record Candidate(float Score, float X0, float Y0, float X1, float Y1, float[] Keypoints);

    private IEnumerable<Candidate> DetectTile(byte[] bgra, int width, int height, Tile tile)
    {
        var input = new DenseTensor<float>([1, DetectorSize, DetectorSize, 3]);
        Sample(bgra, width, height, input, DetectorSize, (u, v) => (tile.X + u * tile.Size, tile.Y + v * tile.Size), -1, 1);
        using var outputs = detector.Run([NamedOnnxValue.CreateFromTensor(detectorInput, input)]);
        var regressors = outputs.First(o => o.Name.Contains("regressor")).AsEnumerable<float>().ToArray();
        var scores = outputs.First(o => o.Name.Contains("classifier")).AsEnumerable<float>().ToArray();
        return Decode(regressors, scores, tile);
    }

    /// <summary>Giải mã theo TensorsToDetections: anchor 48 × 48, bước 4, cỡ cố định; thứ tự x, y, w, h; tỉ lệ 192.</summary>
    public static IEnumerable<Candidate> Decode(float[] regressors, float[] scores, Tile tile)
    {
        for (int a = 0; a < scores.Length; a++)
        {
            float score = 1 / (1 + MathF.Exp(-Math.Clamp(scores[a], -100, 100)));
            if (score < MinScore) continue;
            float ax = (a % AnchorGrid + .5f) / AnchorGrid, ay = (a / AnchorGrid + .5f) / AnchorGrid;
            int o = a * 16;
            float cx = regressors[o] / DetectorSize + ax, cy = regressors[o + 1] / DetectorSize + ay, w = regressors[o + 2] / DetectorSize, h = regressors[o + 3] / DetectorSize;
            var keypoints = new float[12];
            for (int k = 0; k < 6; k++)
            {
                keypoints[k * 2] = (float)(tile.X + (regressors[o + 4 + k * 2] / DetectorSize + ax) * tile.Size);
                keypoints[k * 2 + 1] = (float)(tile.Y + (regressors[o + 5 + k * 2] / DetectorSize + ay) * tile.Size);
            }
            yield return new(score, (float)(tile.X + (cx - w / 2) * tile.Size), (float)(tile.Y + (cy - h / 2) * tile.Size),
                (float)(tile.X + (cx + w / 2) * tile.Size), (float)(tile.Y + (cy + h / 2) * tile.Size), keypoints);
        }
    }

    /// <summary>NMS có trọng số (WEIGHTED, IoU 0,3): gộp khung chồng nhau, trung bình theo điểm số.</summary>
    public static List<Candidate> WeightedNms(IEnumerable<Candidate> candidates)
    {
        var remaining = candidates.OrderByDescending(c => c.Score).ToList();
        var result = new List<Candidate>();
        while (remaining.Count > 0)
        {
            var top = remaining[0];
            var cluster = remaining.Where(c => IoU(top, c) > NmsThreshold).ToList();
            remaining.RemoveAll(cluster.Contains);
            float total = cluster.Sum(c => c.Score);
            float Avg(Func<Candidate, float> f) => cluster.Sum(c => f(c) * c.Score) / total;
            var keypoints = Enumerable.Range(0, 12).Select(k => Avg(c => c.Keypoints[k])).ToArray();
            result.Add(new(top.Score, Avg(c => c.X0), Avg(c => c.Y0), Avg(c => c.X1), Avg(c => c.Y1), keypoints));
        }
        return result;
    }

    private static float IoU(Candidate a, Candidate b)
    {
        float w = Math.Max(0, Math.Min(a.X1, b.X1) - Math.Max(a.X0, b.X0)), h = Math.Max(0, Math.Min(a.Y1, b.Y1) - Math.Max(a.Y0, b.Y0));
        float inter = w * h, union = (a.X1 - a.X0) * (a.Y1 - a.Y0) + (b.X1 - b.X0) * (b.Y1 - b.Y0) - inter;
        return union <= 0 ? 0 : inter / union;
    }

    // Face Mesh trên vùng mặt: nới 1,5 lần, vuông theo cạnh dài, xoay cho hai mắt nằm ngang. Không đạt độ tin cậy thì giữ 6 điểm của bộ dò.
    private DetectedFace Refine(byte[] bgra, int width, int height, Candidate face)
    {
        var k = face.Keypoints;
        // Mắt bên trái ảnh trước, để vùng cắt luôn đứng thẳng.
        bool swap = k[0] > k[2];
        float ex0 = swap ? k[2] : k[0], ey0 = swap ? k[3] : k[1], ex1 = swap ? k[0] : k[2], ey1 = swap ? k[1] : k[3], mx = k[6], my = k[7];
        double angle = Math.Atan2(ey1 - ey0, ex1 - ex0);
        double cx = (face.X0 + face.X1) / 2, cy = (face.Y0 + face.Y1) / 2, size = Math.Max(face.X1 - face.X0, face.Y1 - face.Y0) * 1.5;
        double cos = Math.Cos(angle), sin = Math.Sin(angle);
        (double X, double Y) ToImage(double u, double v)
        {
            double dx = (u - .5) * size, dy = (v - .5) * size;
            return (cx + dx * cos - dy * sin, cy + dx * sin + dy * cos);
        }
        FacePoint Normalized(double x, double y) => new(x / width, y / height);
        (FacePoint Left, FacePoint Right) Order(FacePoint a, FacePoint b) => a.X <= b.X ? (a, b) : (b, a);

        // Khung BlazeFace bắt đầu ở giữa trán. Hình học Retouch (elip da, contour, cổ) được căn theo khung mặt kiểu Windows,
        // cao hơn khoảng 30% về phía trán (đo trên 4 ảnh chân dung mẫu), nên nới khung cho cùng tỉ lệ.
        float boxHeight = face.Y1 - face.Y0;
        var detected = new DetectedFace(face.X0 / width, (face.Y0 - boxHeight * .25f) / height, (face.X1 - face.X0) / width, boxHeight * 1.3f / height, face.Score);
        var (left, right) = Order(Normalized(ex0, ey0), Normalized(ex1, ey1));
        var withKeypoints = detected with { LeftEye = left, RightEye = right, Mouth = Normalized(mx, my) };
        try
        {
            var input = new DenseTensor<float>([1, MeshSize, MeshSize, 3]);
            Sample(bgra, width, height, input, MeshSize, ToImage, 0, 1);
            using var outputs = mesh.Run([NamedOnnxValue.CreateFromTensor(meshInput, input)]);
            var list = outputs.ToList();
            var landmarks = list[0].AsEnumerable<float>().ToArray();
            float presence = 1 / (1 + MathF.Exp(-list[1].AsEnumerable<float>().First()));
            if (presence < MinPresence || landmarks.Length < 478 * 3) return withKeypoints;
            FacePoint Landmark(int index)
            {
                var (x, y) = ToImage(landmarks[index * 3] / MeshSize, landmarks[index * 3 + 1] / MeshSize);
                return Normalized(x, y);
            }
            var (irisLeft, irisRight) = Order(Landmark(IrisA), Landmark(IrisB));
            var upper = Landmark(UpperLip); var lower = Landmark(LowerLip);
            return withKeypoints with
            {
                LeftEye = irisLeft,
                RightEye = irisRight,
                Mouth = new((upper.X + lower.X) / 2, (upper.Y + lower.Y) / 2),
                InnerLips = [.. InnerLip.Select(Landmark)],
            };
        }
        catch (OnnxRuntimeException) { return withKeypoints; }
    }

    // BGRA → tensor RGB NHWC trong khoảng [lo, hi]; điểm ngoài ảnh là đen (BORDER_ZERO).
    // Thu nhỏ mạnh thì lấy trung bình n × n mẫu song tuyến trong mỗi pixel đích để tránh răng cưa.
    private static void Sample(byte[] bgra, int width, int height, DenseTensor<float> tensor, int size, Func<double, double, (double X, double Y)> map, float lo, float hi)
    {
        var (ax, ay) = map(0, 0); var (bx, by) = map(1d / size, 0);
        int n = Math.Clamp((int)Math.Ceiling(Math.Sqrt((bx - ax) * (bx - ax) + (by - ay) * (by - ay))), 1, 8);
        float scale = (hi - lo) / 255f;
        Span<double> sum = stackalloc double[3];
        for (int ty = 0; ty < size; ty++)
            for (int tx = 0; tx < size; tx++)
            {
                sum.Clear();
                for (int sy = 0; sy < n; sy++)
                    for (int sx = 0; sx < n; sx++)
                    {
                        var (x, y) = map((tx + (sx + .5) / n) / size, (ty + (sy + .5) / n) / size);
                        x -= .5; y -= .5;
                        int x0 = (int)Math.Floor(x), y0 = (int)Math.Floor(y);
                        double fx = x - x0, fy = y - y0;
                        for (int c = 0; c < 3; c++)
                        {
                            double Pixel(int px, int py) => px < 0 || py < 0 || px >= width || py >= height ? 0 : bgra[(py * width + px) * 4 + 2 - c];
                            sum[c] += (Pixel(x0, y0) * (1 - fx) + Pixel(x0 + 1, y0) * fx) * (1 - fy) + (Pixel(x0, y0 + 1) * (1 - fx) + Pixel(x0 + 1, y0 + 1) * fx) * fy;
                        }
                    }
                for (int c = 0; c < 3; c++) tensor[0, ty, tx, c] = lo + (float)(sum[c] / (n * n)) * scale;
            }
    }

    public void Dispose() { detector.Dispose(); mesh.Dispose(); }
}
