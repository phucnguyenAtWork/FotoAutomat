namespace FotoAutomat.Core;

public sealed record WhiteMatteRegion(double X,double Y,double Width,double Height,byte R,byte G,byte B,bool Ellipse=false);
public static class WhiteMatteRecovery
{
    // Explicit operator-selected regions only. Port of Packages/Shared/Source/WhiteMatte.ts.
    public static void Recover(byte[] source,byte[] output,int w,int h,DetectedSlot slot,IEnumerable<WhiteMatteRegion> regions)
    {
        TemplateSlotDetector.Validate(source,w,h);TemplateSlotDetector.Validate(output,w,h);
        double angle=slot.Rotation*Math.PI/180,cos=Math.Cos(angle),sin=Math.Sin(angle);
        foreach(var region in regions)
        {
            int[] ink=[region.R,region.G,region.B];int low=Array.IndexOf(ink,ink.Min()),high=Array.IndexOf(ink,ink.Max()),chroma=ink[high]-ink[low];
            if(chroma<16)throw new ArgumentException("Màu mực phải có độ chênh kênh ít nhất 16.");
            if(region.Width<=0||region.Height<=0)continue;
            for(int y=Math.Max(0,(int)Math.Floor(region.Y));y<Math.Min(h,Math.Ceiling(region.Y+region.Height));y++)for(int x=Math.Max(0,(int)Math.Floor(region.X));x<Math.Min(w,Math.Ceiling(region.X+region.Width));x++)
            {
                if(region.Ellipse&&Math.Pow((x+.5-region.X-region.Width/2)/(region.Width/2),2)+Math.Pow((y+.5-region.Y-region.Height/2)/(region.Height/2),2)>1)continue;
                double dx=x+.5-slot.X-slot.Width/2,dy=y+.5-slot.Y-slot.Height/2;if(Math.Abs(dx*cos+dy*sin)>slot.Width/2||Math.Abs(-dx*sin+dy*cos)>slot.Height/2)continue;
                int o=(y*w+x)*4;double coverage=Math.Clamp((source[o+high]-source[o+low])/(double)chroma,0,1),white=Math.Clamp((source[o+low]-coverage*ink[low])/255,0,1-coverage),alpha=1-white;
                if(Enumerable.Range(0,3).Any(c=>Math.Abs(coverage*ink[c]+white*255-source[o+c])>4))continue;
                for(int c=0;c<3;c++)output[o+c]=(byte)(alpha>0?TemplateSlotDetector.Round(coverage*ink[c]/alpha):0);
                output[o+3]=(byte)TemplateSlotDetector.Round(alpha*source[o+3]);
            }
        }
    }
}
