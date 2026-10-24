using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FotoAutomat.Core;

namespace FotoAutomat.Desktop;

internal static class CreateFrameFromImage
{
    internal sealed record Result(PrintLayout Layout, BitmapImage Artwork, SlotDetectionResult Detection);
    public static Result Read(string path)
        => ReadFrame(ArtworkImport.ReadFrame(path),Path.GetFileNameWithoutExtension(path));

    public static Result Scan(PrintLayout current)
    {
        if(current.ArtworkPng is null)throw new InvalidDataException("Nhập khung ảnh trước khi quét.");
        using var stream=new MemoryStream(Convert.FromBase64String(current.ArtworkPng));
        var frame=BitmapDecoder.Create(stream,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad).Frames[0];
        return ReadFrame(frame,current.Name,current);
    }

    private static Result ReadFrame(BitmapFrame frame,string name,PrintLayout? current=null)
    {
        int w=frame.PixelWidth,h=frame.PixelHeight;
        if((long)w*h>24_000_000)throw new InvalidDataException("Chọn ảnh tối đa 24 triệu pixel.");
        var bitmap=new FormatConvertedBitmap(frame,PixelFormats.Bgra32,null,0);var data=new byte[w*h*4];bitmap.CopyPixels(data,w*4,0);
        for(int i=0;i<data.Length;i+=4)(data[i],data[i+2])=(data[i+2],data[i]);
        var result=TemplateSlotDetector.DetectStandardGray(data,w,h);
        if(result.Slots.Length==0)throw new InvalidDataException("Không tìm thấy ô ảnh màu #C6C6C6. Dùng các ô màu đặc, tách rời nhau và không chạm mép ảnh.");
        var overlay=TemplateAperture.CreateOverlay(data,w,h,result);
        for(int i=0;i<overlay.Length;i+=4)(overlay[i],overlay[i+2])=(overlay[i+2],overlay[i]);
        var source=BitmapSource.Create(w,h,96,96,PixelFormats.Bgra32,null,overlay,w*4);
        var format=ArtworkPaperFormat.Detect(w,h);
        bool landscape=format.Landscape,strip=format.SingleStrip;
        double pw=landscape?600:strip?200:400,ph=landscape?(strip?200:400):600;
        if(current is not null){pw=current.PageWidth;ph=current.PageHeight;landscape=current.Landscape;strip=current.SingleStrip;}
        double scale=Math.Min(pw/w,ph/h),ox=(pw-w*scale)/2,oy=(ph-h*scale)/2;
        var slots=result.Slots.Select((s,i)=>new PrintLayoutSlot(i+1,ox+s.X*scale,oy+s.Y*scale,s.Width*scale,s.Height*scale,s.Rotation)).ToArray();
        var visual=new DrawingVisual();using(var dc=visual.RenderOpen())dc.DrawImage(source,new Rect(ox*3,oy*3,w*scale*3,h*scale*3));
        var rendered=new RenderTargetBitmap((int)(pw*3),(int)(ph*3),96,96,PixelFormats.Pbgra32);rendered.Render(visual);
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(rendered));using var stream=new MemoryStream();encoder.Save(stream);byte[] bytes=stream.ToArray();
        if(name.Length>80)name=name[..80];
        var layout=new PrintLayout(name,false,slots,Convert.ToBase64String(bytes),false,strip,Landscape:landscape);
        if(current is not null)layout=current with
        {
            Slots=slots.Concat(current.Slots.Where(s=>s.Kind!=LayoutElementKind.Photo)).ToArray(),
            ArtworkPng=layout.ArtworkPng,ArtworkBehindPhotos=false
        };
        layout.Validate();
        var image=new BitmapImage();image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;image.StreamSource=new MemoryStream(bytes);image.EndInit();image.Freeze();
        return new(layout,image,result);
    }
    public static bool Review(Window owner,Result result,bool scan=false)
    {
        var window=new Window {Title=scan?"Quét ô ảnh":"Tạo khung từ ảnh",Owner=owner,Width=800,Height=750,MinWidth=600,MinHeight=500,WindowStartupLocation=WindowStartupLocation.CenterOwner,Background=Brushes.White};
        var root=new DockPanel {Margin=new Thickness(20)};
        var footer=new StackPanel {Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};DockPanel.SetDock(footer,Dock.Bottom);
        var cancel=new Button {Content="Hủy",Padding=new Thickness(20,12,20,12),Margin=new Thickness(8),IsCancel=true};var create=new Button {Content=scan?"Áp dụng ô ảnh":"Tạo bố cục",Padding=new Thickness(20,12,20,12),Margin=new Thickness(8),IsDefault=true};create.Click+=(_,_)=>window.DialogResult=true;footer.Children.Add(cancel);footer.Children.Add(create);root.Children.Add(footer);
        var title=new TextBlock {Text=$"{result.Detection.Slots.Length} ô ảnh · {result.Detection.Method}",FontSize=20,Margin=new Thickness(0,0,0,16)};DockPanel.SetDock(title,Dock.Top);root.Children.Add(title);
        var canvas=new Canvas {Width=result.Layout.PageWidth,Height=result.Layout.PageHeight,Background=Brushes.White};
        foreach(var slot in result.Layout.Slots){var visual=new LayoutElementVisual {Element=slot,Preview=true,Width=slot.Width,Height=slot.Height,RenderTransformOrigin=new Point(.5,.5),RenderTransform=new RotateTransform(slot.Rotation)};Canvas.SetLeft(visual,slot.X);Canvas.SetTop(visual,slot.Y);canvas.Children.Add(visual);}
        canvas.Children.Add(new Image {Source=result.Artwork,Width=result.Layout.PageWidth,Height=result.Layout.PageHeight,Stretch=Stretch.Fill,IsHitTestVisible=false});
        root.Children.Add(new Viewbox {Child=canvas,Stretch=Stretch.Uniform,Margin=new Thickness(10)});window.Content=root;
        return window.ShowDialog()==true;
    }
}
