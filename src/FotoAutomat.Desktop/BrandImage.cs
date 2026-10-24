using System.IO;
using System.Windows.Media.Imaging;

namespace FotoAutomat.Desktop;

/// <summary>Loads the brand logo for the admin preview and the customer screens.</summary>
internal static class BrandImage
{
    // OnLoad reads the whole file and closes it, so replacing or deleting the old logo does not hit a locked file.
    public static BitmapImage? Load(string? path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            image.UriSource = new Uri(path);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception ex) when (ex is IOException or NotSupportedException or UnauthorizedAccessException or ArgumentException or InvalidOperationException) { return null; }
    }
}
