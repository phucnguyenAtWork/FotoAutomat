using System.Globalization;
using System.IO;
using System.Windows.Media.Imaging;

namespace FotoAutomat.Desktop;
public sealed record ImageExposureMetadata(string Iso = "ISO —", string Shutter = "Shutter —", string Aperture = "f/—")
{
    public static ImageExposureMetadata Read(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
            if (decoder.Frames[0].Metadata is not BitmapMetadata metadata) return new();
            object? Query(int tag)
            {
                foreach (var prefix in new[] { "/app1/ifd/exif/", "/ifd/exif/" })
                {
                    try { if (metadata.GetQuery(prefix + "{ushort=" + tag + "}") is { } value) return value; }
                    catch (Exception e) when (e is NotSupportedException or ArgumentException or IOException) { }
                }
                return null;
            }
            double Number(object? value) => value switch { ulong rational => (rational >> 32) == 0 ? 0 : (rational & uint.MaxValue) / (double)(rational >> 32), ushort[] array when array.Length > 0 => array[0], ushort n => n, uint n => n, double n => n, _ => 0 };
            double iso = Number(Query(34855)), shutter = Number(Query(33434)), aperture = Number(Query(33437));
            string exposure = shutter > 0 && shutter < 1 ? "1/" + Math.Round(1 / shutter).ToString(CultureInfo.InvariantCulture) + " s" : shutter >= 1 ? shutter.ToString("0.##", CultureInfo.InvariantCulture) + " s" : "Shutter —";
            return new(iso > 0 ? "ISO " + iso.ToString("0", CultureInfo.InvariantCulture) : "ISO —", exposure, aperture > 0 ? "f/" + aperture.ToString("0.#", CultureInfo.InvariantCulture) : "f/—");
        }
        catch (Exception e) when (e is IOException or NotSupportedException or ArgumentException or System.Runtime.InteropServices.COMException) { return new(); }
    }
}
