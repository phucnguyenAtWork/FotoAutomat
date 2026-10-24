using System.Windows.Media;

namespace FotoAutomat.Desktop.ViewModels;

public sealed record LayoutGalleryItem(string Name, ImageSource Thumbnail, LayoutPreset? Preset = null, string? Path = null);
