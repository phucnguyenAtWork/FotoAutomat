using FotoAutomat.Core;
namespace FotoAutomat.Tests;
public class ArtworkPaperFormatTests
{
    [Theory]
    [InlineData(600,1800,true,false)]
    [InlineData(1200,3600,true,false)]
    [InlineData(1800,600,true,true)]
    [InlineData(1200,1800,false,false)]
    [InlineData(2400,3600,false,false)]
    [InlineData(1800,1200,false,true)]
    [InlineData(610,1800,true,false)]
    public void DetectsFormatAndOrientation(int w,int h,bool strip,bool landscape)
    {
        var result=ArtworkPaperFormat.Detect(w,h);
        Assert.Equal(strip,result.SingleStrip);Assert.Equal(landscape,result.Landscape);
    }
    [Theory]
    [InlineData(1200,1200)]
    [InlineData(1600,900)]
    public void DoesNotGuessUnknownRatio(int w,int h)=>Assert.Throws<ArgumentException>(()=>ArtworkPaperFormat.Detect(w,h));
}
