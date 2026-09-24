using FotoAutomat.Core;

namespace FotoAutomat.Infrastructure;

public sealed class SimulatedCamera(string outputDirectory, bool failBeforeCapture = false) : ICamera
{
    public async Task<string> CaptureAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (failBeforeCapture)
            throw new DeviceUnavailableException("Simulated camera disconnected before capture.");
        Directory.CreateDirectory(outputDirectory);
        var path = Path.GetFullPath(Path.Combine(outputDirectory, $"{sessionId}.bmp"));
        // A tiny synthetic BMP, not a camera image. No photograph or model download is needed.
        const int width = 96, height = 64, rowBytes = width * 3;
        using var buffer = new MemoryStream();
        using (var writer = new BinaryWriter(buffer, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            writer.Write((ushort)0x4D42);
            writer.Write(54 + rowBytes * height);
            writer.Write(0);
            writer.Write(54);
            writer.Write(40);
            writer.Write(width);
            writer.Write(height);
            writer.Write((ushort)1);
            writer.Write((ushort)24);
            writer.Write(0);
            writer.Write(rowBytes * height);
            writer.Write(2835);
            writer.Write(2835);
            writer.Write(0);
            writer.Write(0);
            for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++)
                {
                    writer.Write((byte)180);
                    writer.Write((byte)(80 + y));
                    writer.Write((byte)(80 + x));
                }
        }
        await File.WriteAllBytesAsync(path, buffer.ToArray(), cancellationToken);
        return path;
    }
}

public sealed class SimulatedPrinter(PrintOutcome outcome = PrintOutcome.Completed) : IPrinter
{
    public int SubmissionCount { get; private set; }

    public Task<PrintOutcome> PrintAsync(PrintRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!File.Exists(request.ImagePath)) throw new FileNotFoundException("Captured image missing.", request.ImagePath);
        SubmissionCount++;
        // Does not call the Windows spooler or a physical printer.
        return Task.FromResult(outcome);
    }
}
