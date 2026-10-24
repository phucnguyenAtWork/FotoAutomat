namespace FotoAutomat.Core;

public static class TemplateAperture
{
    public static int[] Extract(byte[] data,int w,int h,DetectedSlot slot,int tolerance=12)
    {
        TemplateSlotDetector.Validate(data,w,h);
        if(tolerance is < 0 or > 255)throw new ArgumentOutOfRangeException(nameof(tolerance));
        double a=slot.Rotation*Math.PI/180,cos=Math.Cos(a),sin=Math.Sin(a),cx=slot.X+slot.Width/2,cy=slot.Y+slot.Height/2;
        bool rotated=slot.Rotation!=0;int pad=rotated?0:Math.Max(4,Math.Min(20,TemplateSlotDetector.Round(Math.Min(slot.Width,slot.Height)*.015)));
        double sw=slot.Width*(rotated?1.1:1)+pad*2,sh=slot.Height*(rotated?1.1:1)+pad*2;
        double bw=Math.Abs(sw*cos)/2+Math.Abs(sh*sin)/2,bh=Math.Abs(sw*sin)/2+Math.Abs(sh*cos)/2;
        int x1=Math.Max(0,(int)Math.Floor(cx-bw)),y1=Math.Max(0,(int)Math.Floor(cy-bh)),x2=Math.Min(w,(int)Math.Ceiling(cx+bw)),y2=Math.Min(h,(int)Math.Ceiling(cy+bh)),rw=x2-x1,rh=y2-y1;
        if(rw<=0||rh<=0)return [];
        bool Inside(int x,int y,double width,double height){double dx=x+.5-cx,dy=y+.5-cy;return Math.Abs(dx*cos+dy*sin)<=width/2&&Math.Abs(-dx*sin+dy*cos)<=height/2;}
        var tally=new Dictionary<int,(int N,int R,int G,int B)>();int step=Math.Max(1,TemplateSlotDetector.Round(Math.Min(rw,rh)/60d));
        for(int y=y1;y<y2;y+=step)for(int x=x1;x<x2;x+=step)if(Inside(x,y,slot.Width,slot.Height))
        {int o=(y*w+x)*4,r=data[o],g=data[o+1],b=data[o+2],key=((r>>2)<<12)|((g>>2)<<6)|(b>>2);if(tally.TryGetValue(key,out var v))tally[key]=(v.N+1,v.R,v.G,v.B);else tally[key]=(1,r,g,b);}
        if(tally.Count==0)return [];var seed=tally.Values.MaxBy(v=>v.N);
        bool Near(int x,int y,int tol){int o=(y*w+x)*4;return Math.Abs(data[o]-seed.R)<=tol&&Math.Abs(data[o+1]-seed.G)<=tol&&Math.Abs(data[o+2]-seed.B)<=tol;}
        var near=new bool[rw*rh];for(int i=0;i<near.Length;i++){int x=i%rw+x1,y=i/rw+y1;near[i]=Inside(x,y,sw,sh)&&Near(x,y,tolerance);}
        var seen=new bool[near.Length];List<int> best=[];for(int i=0;i<near.Length;i++)if(near[i]&&!seen[i]){var part=TemplateSlotDetector.Flood(i,seen,rw,rh,n=>near[n]);if(part.Count>best.Count)best=part;}
        bool Boundary(int i)=>i%rw==0||i%rw==rw-1||i/rw==0||i/rw==rh-1;
        bool touched=!rotated&&best.Any(Boundary);
        if(touched)best=best.Where(i=>Inside(i%rw+x1,i/rw+y1,slot.Width,slot.Height)).ToList();
        var strict=best.ToList();var aperture=new bool[near.Length];foreach(int i in best)aperture[i]=true;
        int normal=Math.Max(2,Math.Min(6,TemplateSlotDetector.Round(Math.Min(slot.Width,slot.Height)/500))),passes=rotated?normal:Math.Max(normal,pad+1);
        var frontier=best.ToList();for(int pass=0;pass<passes&&frontier.Count>0;pass++)
        {
            List<int> next=[];foreach(int i in frontier)for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
            {
                int nx=i%rw+dx,ny=i/rw+dy;if(nx<0||ny<0||nx>=rw||ny>=rh)continue;int ni=ny*rw+nx;
                if(aperture[ni]||!Inside(nx+x1,ny+y1,touched?slot.Width:sw,touched?slot.Height:sh)||!Near(nx+x1,ny+y1,Math.Max(tolerance,84)))continue;
                aperture[ni]=true;next.Add(ni);
            }
            best.AddRange(next);frontier=next;
        }
        if(!rotated&&best.Any(Boundary))best=strict.Where(i=>Inside(i%rw+x1,i/rw+y1,slot.Width,slot.Height)).ToList();
        return best.Select(i=>(i/rw+y1)*w+i%rw+x1).ToArray();
    }

    public static byte[] CreateOverlay(byte[] rgba,int w,int h,SlotDetectionResult detection)
    {
        TemplateSlotDetector.Validate(rgba,w,h);var output=(byte[])rgba.Clone();
        foreach(var slot in detection.Slots)
        {
            if(detection.Method==TemplateSlotDetector.StandardGrayMethod)
            {
                double angle=slot.Rotation*Math.PI/180,cos=Math.Cos(angle),sin=Math.Sin(angle);
                double cx=slot.X+slot.Width/2,cy=slot.Y+slot.Height/2;
                double bw=(Math.Abs(cos)*slot.Width+Math.Abs(sin)*slot.Height)/2,bh=(Math.Abs(sin)*slot.Width+Math.Abs(cos)*slot.Height)/2;
                for(int y=Math.Max(0,(int)Math.Floor(cy-bh));y<Math.Min(h,Math.Ceiling(cy+bh));y++)
                    for(int x=Math.Max(0,(int)Math.Floor(cx-bw));x<Math.Min(w,Math.Ceiling(cx+bw));x++)
                    {int i=y*w+x;if(TemplateSlotDetector.StandardGray(rgba,i))output[i*4+3]=0;}
                continue;
            }
            if(detection.Method=="Placeholder trời–cỏ") { PunchPlaceholder(rgba,output,w,h,slot);continue; }
            if(detection.Method=="Trong suốt / magenta")
            {
                // Preserve existing alpha; never punch magenta decorations elsewhere on the sheet.
                double angle=slot.Rotation*Math.PI/180,cos=Math.Cos(angle),sin=Math.Sin(angle);
                double cx=slot.X+slot.Width/2,cy=slot.Y+slot.Height/2;
                double bw=(Math.Abs(cos)*slot.Width+Math.Abs(sin)*slot.Height)/2,bh=(Math.Abs(sin)*slot.Width+Math.Abs(cos)*slot.Height)/2;
                for(int y=Math.Max(0,(int)Math.Floor(cy-bh));y<Math.Min(h,Math.Ceiling(cy+bh));y++)for(int x=Math.Max(0,(int)Math.Floor(cx-bw));x<Math.Min(w,Math.Ceiling(cx+bw));x++)
                {
                    double dx=x+.5-cx,dy=y+.5-cy;
                    if(Math.Abs(dx*cos+dy*sin)>slot.Width/2||Math.Abs(-dx*sin+dy*cos)>slot.Height/2)continue;
                    int o=(y*w+x)*4;if(TemplateSlotDetector.Marker(rgba[o],rgba[o+1],rgba[o+2]))output[o+3]=0;
                }
            }
            else foreach(int i in Extract(rgba,w,h,slot))output[i*4+3]=0;
        }
        return output;
    }
    private static void PunchPlaceholder(byte[] source,byte[] output,int w,int h,DetectedSlot slot)
    {
        int inflate=TemplateSlotDetector.Round(Math.Max(slot.Width,slot.Height)*.15);
        int x1=Math.Max(0,TemplateSlotDetector.Round(slot.X)-inflate),y1=Math.Max(0,TemplateSlotDetector.Round(slot.Y)-inflate),x2=Math.Min(w,TemplateSlotDetector.Round(slot.X+slot.Width)+inflate),y2=Math.Min(h,TemplateSlotDetector.Round(slot.Y+slot.Height)+inflate),rw=x2-x1,rh=y2-y1;
        if(rw<=0||rh<=0)return;var local=new byte[rw*rh*4];for(int y=0;y<rh;y++)Array.Copy(source,((y+y1)*w+x1)*4,local,y*rw*4,rw*4);
        var art=new bool[rw*rh];for(int i=0;i<art.Length;i++)art[i]=local[i*4+3]>=12&&TemplateSlotDetector.Art(local[i*4],local[i*4+1],local[i*4+2]);
        int gap=Math.Max(4,TemplateSlotDetector.Round(rw*.004));TemplateSlotDetector.Close(art,local,rw,rh,gap,gap,false);
        var outer=TemplateSlotDetector.Outside(art,rw,rh);var decor=new bool[art.Length];
        for(int i=0;i<art.Length;i++)if(!art[i]&&!outer[i]){int o=i*4,min=Math.Min(local[o],Math.Min(local[o+1],local[o+2])),max=Math.Max(local[o],Math.Max(local[o+1],local[o+2]));if(min>=190&&max-min<=40)art[i]=true;else decor[i]=true;}
        bool White(int i)=>local[i*4]>=225&&local[i*4+1]>=225&&local[i*4+2]>=225;
        var white=new bool[art.Length];for(int i=0;i<art.Length;i++)white[i]=!art[i]&&White(i);
        int erode=Math.Max(2,TemplateSlotDetector.Round(rw*.004));var core=white;
        for(int pass=0;pass<erode;pass++){var next=new bool[art.Length];for(int y=1;y<rh-1;y++)for(int x=1;x<rw-1;x++){int i=y*rw+x;next[i]=core[i]&&core[i-1]&&core[i+1]&&core[i-rw]&&core[i+rw];}core=next;}
        var seen=new bool[art.Length];var depth=new int[art.Length];
        for(int start=0;start<art.Length;start++)if(core[start]&&!seen[start])
        {
            List<int> grown=[start];seen[start]=true;int ac=0,sc=0;bool border=false;
            for(int head=0;head<grown.Count;head++){int i=grown[head];if(i%rw==0||i%rw==rw-1||i/rw==0||i/rw==rh-1)border=true;foreach(int n in TemplateSlotDetector.Neighbours(i,rw,rh)){if(art[n]){ac++;continue;}if(white[n]){int d=core[n]?0:depth[i]+1;if(!seen[n]&&d<=erode){seen[n]=true;depth[n]=d;grown.Add(n);}}else sc++;}}
            if(!border&&ac>3*sc)foreach(int i in grown)art[i]=true;
        }
        int rx0=Math.Max(0,TemplateSlotDetector.Round(slot.X)-x1),ry0=Math.Max(0,TemplateSlotDetector.Round(slot.Y)-y1),rx1=Math.Min(rw-1,TemplateSlotDetector.Round(slot.X+slot.Width)-x1),ry1=Math.Min(rh-1,TemplateSlotDetector.Round(slot.Y+slot.Height)-y1),near=Math.Max(8,TemplateSlotDetector.Round(Math.Min(rw,rh)*.03));
        for(int pass=0;pass<4;pass++)
        {
            var sums=new int[(rw+1)*(rh+1)];for(int y=0;y<rh;y++){int sum=0;for(int x=0;x<rw;x++){if(art[y*rw+x])sum++;sums[(y+1)*(rw+1)+x+1]=sums[y*(rw+1)+x+1]+sum;}}
            List<int> batch=[];for(int y=ry0;y<=ry1;y++)for(int x=rx0;x<=rx1;x++){int i=y*rw+x;if(art[i]||!White(i))continue;int ax=Math.Max(0,x-near),ay=Math.Max(0,y-near),bx=Math.Min(rw-1,x+near),by=Math.Min(rh-1,y+near);int sum=sums[(by+1)*(rw+1)+bx+1]-sums[ay*(rw+1)+bx+1]-sums[(by+1)*(rw+1)+ax]+sums[ay*(rw+1)+ax];if(sum/(double)((bx-ax+1)*(by-ay+1))>.5)batch.Add(i);}
            if(batch.Count==0)break;foreach(int i in batch)art[i]=true;
        }
        var keep=TemplateSlotDetector.Outside(art,rw,rh);
        for(int i=0;i<art.Length;i++)if(!keep[i]&&!decor[i])output[((i/rw+y1)*w+i%rw+x1)*4+3]=0;
    }
}
