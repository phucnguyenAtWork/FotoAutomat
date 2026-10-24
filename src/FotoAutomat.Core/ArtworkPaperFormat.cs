namespace FotoAutomat.Core;

public sealed record ArtworkPaperFormat(bool SingleStrip, bool Landscape)
{
    public string Label => SingleStrip ? "2 × 6 inch" : "4 × 6 inch";
    public static ArtworkPaperFormat Detect(int width, int height)
    {
        if(width<=0||height<=0)throw new ArgumentException("Kích thước ảnh không hợp lệ.");
        double ratio=Math.Min(width,height)/(double)Math.Max(width,height);
        if(Math.Abs(ratio-1d/3)/(1d/3)<=.02)return new(true,width>height);
        if(Math.Abs(ratio-2d/3)/(2d/3)<=.02)return new(false,width>height);
        throw new ArgumentException($"Ảnh {width} × {height} px chưa khớp khổ 2 × 6 hoặc 4 × 6. Xuất thiết kế theo tỉ lệ 1:3 hoặc 2:3, ngang hoặc dọc.");
    }
}
