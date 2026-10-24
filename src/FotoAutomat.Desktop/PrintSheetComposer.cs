using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FotoAutomat.Core;

namespace FotoAutomat.Desktop;

public static class PrintSheetComposer
{
    // Input is one finished layout; pairing happens only at the print-output boundary.
    public static BitmapSource Compose(BitmapSource singleLayout, PrintLayout layout, PrinterConfiguration configuration)
    {
        var plan = configuration.CreateSheetPlan(layout);
        var first = plan.Placements[0];
        if (singleLayout.PixelWidth != first.Width || singleLayout.PixelHeight != first.Height)
            throw new ArgumentException("Ảnh đầu vào phải đúng kích thước pixel và DPI của mẫu.");
        var visual = new DrawingVisual();
        double toDip = 96d / plan.Dpi;
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, plan.Width * toDip, plan.Height * toDip));
            foreach (var placement in plan.Placements)
                dc.DrawImage(singleLayout, new Rect(placement.X * toDip, placement.Y * toDip, placement.Width * toDip, placement.Height * toDip));
        }
        var result = new RenderTargetBitmap(plan.Width, plan.Height, plan.Dpi, plan.Dpi, PixelFormats.Pbgra32);
        result.Render(visual); result.Freeze(); return result;
    }
}
