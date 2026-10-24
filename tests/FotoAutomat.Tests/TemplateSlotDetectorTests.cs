using FotoAutomat.Core;
namespace FotoAutomat.Tests;
public class TemplateSlotDetectorTests
{
    private const int W=200,H=300;
    [Fact]
    public void StandardGrayIgnoresTransparentBackgroundAndKeepsFourSeparateSlots()
    {
        var data=new byte[W*H*4];
        foreach(int y in new[]{30,112})foreach(int x in new[]{20,101})Rect(data,x,y,80,80,198,198,198);
        Rect(data,20,230,80,40,255,0,255);
        var found=TemplateSlotDetector.DetectStandardGray(data,W,H);
        Assert.Equal(4,found.Slots.Length);
        Assert.All(found.Slots,s=>{Assert.Equal(80,s.Width);Assert.Equal(80,s.Height);});
        var output=TemplateAperture.CreateOverlay(data,W,H,found);
        Assert.Equal(0,output[(40*W+40)*4+3]);
        Assert.Equal(255,output[(240*W+40)*4+3]);
        Assert.Equal(255,data[(40*W+40)*4+3]);
    }
    [Fact]
    public void StandardGrayRejectsWrongColorsAndPageBackground()
    {
        var data=Image();Rect(data,20,20,160,100,217,217,217);
        Assert.Empty(TemplateSlotDetector.DetectStandardGray(data,W,H).Slots);
        Rect(data,0,0,W,H,198,198,198);
        Assert.Empty(TemplateSlotDetector.DetectStandardGray(data,W,H).Slots);
    }
    private static byte[] Image(){var data=new byte[W*H*4];Array.Fill(data,(byte)255);return data;}
    private static void Rect(byte[] data,int x,int y,int width,int height,byte r,byte g,byte b,byte alpha=255)
    {for(int cy=y;cy<y+height;cy++)for(int cx=x;cx<x+width;cx++){int i=(cy*W+cx)*4;data[i]=r;data[i+1]=g;data[i+2]=b;data[i+3]=alpha;}}
    [Theory]
    [InlineData(255,0,255,255)]
    [InlineData(0,0,0,0)]
    [InlineData(198,198,198,255)]
    public void ThreeSlotsDetectedAndPunchedWithoutErasingPaper(byte r,byte g,byte b,byte alpha)
    {
        var data=Image();for(int y=20;y<270;y+=90)Rect(data,20,y,160,65,r,g,b,alpha);
        var found=TemplateSlotDetector.Detect(data,W,H);Assert.Equal(3,found.Slots.Length);
        var overlay=TemplateAperture.CreateOverlay(data,W,H,found);
        Assert.Equal(255,overlay[3]);foreach(var slot in found.Slots)Assert.Equal(0,overlay[((int)(slot.Y+20)*W+40)*4+3]);
        Assert.Equal(alpha,data[(40*W+40)*4+3]);
    }
    [Fact]
    public void WhitePaperAndBrandPinkAreNotSlots()
    {
        var data=Image();Rect(data,30,30,130,130,232,160,184);Assert.Empty(TemplateSlotDetector.Detect(data,W,H).Slots);
    }
    [Fact]
    public void BlankSlotKeepsColoredDecorationAndWhiteOutsideRegion()
    {
        var data=Image();Rect(data,20,20,160,100,198,198,198);Rect(data,60,50,10,10,220,40,90);
        var result=TemplateSlotDetector.Detect(data,W,H);Assert.Single(result.Slots);
        var output=TemplateAperture.CreateOverlay(data,W,H,result);Assert.Equal(255,output[(55*W+65)*4+3]);Assert.Equal(0,output[(40*W+40)*4+3]);Assert.Equal(255,output[(150*W+40)*4+3]);
    }
    [Fact]
    public void ApertureOnWhitePaperDoesNotLeakOutsideChosenRectangle()
    {
        var pixels=TemplateAperture.Extract(Image(),W,H,new(40,50,100,80));Assert.Equal(8000,pixels.Length);Assert.All(pixels,i=>Assert.True(i%W>=40&&i%W<140&&i/W>=50&&i/W<130));
    }
    [Fact]
    public void PlaceholderSkyGrassIsFallbackAndCloudIsRemoved()
    {
        var data=Image();Rect(data,20,20,160,65,170,200,240);Rect(data,20,85,160,35,120,180,90);Rect(data,50,40,10,10,255,255,255);
        var result=TemplateSlotDetector.Detect(data,W,H);Assert.Single(result.Slots);Assert.Equal("Placeholder trời–cỏ",result.Method);
        var output=TemplateAperture.CreateOverlay(data,W,H,result);Assert.Equal(0,output[(45*W+55)*4+3]);Assert.Equal(255,output[3]);
    }
    [Fact]
    public void MatteRecoveryIsConfinedToExplicitRegion()
    {
        var data=Image();var output=(byte[])data.Clone();WhiteMatteRecovery.Recover(data,output,W,H,new(20,20,160,100),[new(40,40,10,10,200,40,80)]);
        Assert.Equal(0,output[(45*W+45)*4+3]);Assert.Equal(255,output[(45*W+55)*4+3]);Assert.Equal(255,data[(45*W+45)*4+3]);
    }
}
