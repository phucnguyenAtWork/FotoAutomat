namespace FotoAutomat.Core;

// Port of Photobooth main 34ef975, Templates/Service.ts. Input uses straight RGBA bytes.
public sealed record DetectedSlot(double X, double Y, double Width, double Height, double Rotation = 0, int ContentRotation = 0);
public sealed record SlotDetectionResult(DetectedSlot[] Slots, string Method);
public static class TemplateSlotDetector
{
    public const string StandardGrayMethod = "Ô ảnh #C6C6C6";
    internal static bool StandardGray(byte[] data,int i) => data[i*4+3]>=250 &&
        Math.Abs(data[i*4]-198)<=2 && Math.Abs(data[i*4+1]-198)<=2 && Math.Abs(data[i*4+2]-198)<=2;

    public static SlotDetectionResult DetectStandardGray(byte[] data,int w,int h)
    {
        Validate(data,w,h);
        var mask=new bool[w*h];var seen=new bool[w*h];var slots=new List<DetectedSlot>();
        for(int i=0;i<mask.Length;i++)mask[i]=StandardGray(data,i);
        for(int i=0;i<mask.Length;i++)
        {
            if(!mask[i]||seen[i])continue;
            var cells=Flood(i,seen,w,h,n=>mask[n]);
            if(cells.Count<w*h*.005)continue;
            var box=Bounds(cells,w);
            if(box.X==0||box.Y==0||box.Right==w||box.Bottom==h)continue;
            var slot=Oriented(mask,w,box);
            if(cells.Count/(slot.Width*slot.Height)<.9)continue;
            slots.Add(slot);
        }
        if(slots.Count>16)throw new ArgumentException("Nhận diện hơn 16 ô màu #C6C6C6. Kiểm tra lại ảnh thiết kế.");
        return new(slots.OrderBy(s=>s.Y).ThenBy(s=>s.X).ToArray(),StandardGrayMethod);
    }
    internal static int Round(double n) => (int)Math.Floor(n + .5);
    internal static bool Marker(byte r, byte g, byte b) => r >= 180 && g <= 90 && b >= 180;
    internal static bool Sky(byte r, byte g, byte b) => b >= 200 && r <= 242 && b-r >= 15 && g >= r;
    internal static bool Grass(byte r, byte g, byte b) => g >= 140 && g >= r+5 && g > b+10;
    internal static bool Art(byte r, byte g, byte b) => Sky(r,g,b) || Grass(r,g,b);
    internal static void Validate(byte[] data, int w, int h)
    {
        if (w < 1 || h < 1 || (long)w*h > 24_000_000 || data.LongLength != (long)w*h*4) throw new ArgumentException("Ảnh không hợp lệ hoặc vượt 24 triệu pixel.");
    }
    internal sealed record Box(int X, int Y, int Right, int Bottom)
    {
        public int Width => Right-X; public int Height => Bottom-Y;
    }
    internal static IEnumerable<int> Neighbours(int i, int w, int h)
    {
        if(i%w>0)yield return i-1; if(i%w<w-1)yield return i+1;
        if(i>=w)yield return i-w; if(i<w*(h-1))yield return i+w;
    }
    internal static List<int> Flood(int start, bool[] seen, int w, int h, Func<int,bool> match)
    {
        List<int> cells=[start];seen[start]=true;
        for(int head=0;head<cells.Count;head++)foreach(int n in Neighbours(cells[head],w,h))if(!seen[n]&&match(n)){seen[n]=true;cells.Add(n);}
        return cells;
    }
    private static Box Bounds(List<int> cells,int w) => new(cells.Min(i=>i%w),cells.Min(i=>i/w),cells.Max(i=>i%w)+1,cells.Max(i=>i/w)+1);
    internal static bool[] Outside(bool[] inside,int w,int h)
    {
        var seen=new bool[w*h];var queue=new Queue<int>();
        void Seed(int i){if(!inside[i]&&!seen[i]){seen[i]=true;queue.Enqueue(i);}}
        for(int x=0;x<w;x++){Seed(x);Seed((h-1)*w+x);}for(int y=0;y<h;y++){Seed(y*w);Seed(y*w+w-1);}
        while(queue.TryDequeue(out int i))foreach(int n in Neighbours(i,w,h))Seed(n);
        return seen;
    }
    private static bool[] BlankMask(byte[] data,int w,int h)
    {
        var seen=new bool[w*h];var accepted=new bool[w*h];
        for(int start=0;start<seen.Length;start++)
        {
            if(seen[start])continue;int o=start*4,r=data[o],g=data[o+1],b=data[o+2];
            if(Math.Min(r,Math.Min(g,b))<120||Math.Max(r,Math.Max(g,b))-Math.Min(r,Math.Min(g,b))>25){seen[start]=true;continue;}
            var cells=Flood(start,seen,w,h,i=>Math.Abs(data[i*4]-r)<=2&&Math.Abs(data[i*4+1]-g)<=2&&Math.Abs(data[i*4+2]-b)<=2);
            if(cells.Count<w*h*.005||cells.Any(i=>i%w==0||i%w==w-1||i/w==0||i/w==h-1))continue;
            var box=Bounds(cells,w);var inside=new bool[box.Width*box.Height];
            foreach(int i in cells)inside[(i/w-box.Y)*box.Width+i%w-box.X]=true;
            var outside=Outside(inside,box.Width,box.Height);int holes=0;
            for(int i=0;i<inside.Length;i++)if(!inside[i]&&!outside[i])holes++;
            int filled=cells.Count+holes;
            if(filled/(double)inside.Length<.6||holes/(double)filled>.03)continue;
            for(int i=0;i<inside.Length;i++)if(!outside[i])accepted[(box.Y+i/box.Width)*w+box.X+i%box.Width]=true;
        }
        return accepted;
    }
    internal static void Close(bool[] mask,byte[] data,int w,int h,int gx,int gy,bool checkDark)
    {
        for(int axis=0;axis<2;axis++)for(int line=0;line<(axis==0?h:w);line++)
        {
            int last=-1;for(int p=0;p<(axis==0?w:h);p++)
            {
                int Index(int k)=>axis==0?line*w+k:k*w+line;
                if(!mask[Index(p)])continue;
                if(last>=0&&p-last>1&&p-last<=(axis==0?gx:gy))
                {
                    bool dark=false;for(int k=last+1;k<p;k++){int o=Index(k)*4;if(checkDark&&data[o]+data[o+1]+data[o+2]<330){dark=true;break;}}
                    if(!dark)for(int k=last+1;k<p;k++)mask[Index(k)]=true;
                }
                last=p;
            }
        }
    }
    public static SlotDetectionResult Detect(byte[] data,int w,int h)
    {
        Validate(data,w,h);var mask=new bool[w*h];var art=new bool[w*h];int markers=0,transparent=0;
        for(int i=0;i<mask.Length;i++){int o=i*4;bool m=Marker(data[o],data[o+1],data[o+2]),t=data[o+3]<12; if(m)markers++;if(t)transparent++;mask[i]=m||t;art[i]=Art(data[o],data[o+1],data[o+2]);}
        bool strong=markers>=w*h*.001||transparent>=w*h*.001,placeholder=false;
        string method="Trong suốt / magenta";
        if(!strong){mask=BlankMask(data,w,h);method="Ô màu phẳng";if(!mask.Any(v=>v)){mask=art;placeholder=true;method="Placeholder trời–cỏ";}}
        int gapX=Math.Max(4,Round(w*.002)),gapY=Math.Max(6,Round(h*.0025));
        Close(mask,data,w,h,gapX,gapY,true);
        var seen=new bool[w*h];var boxes=new List<Box>();
        for(int i=0;i<mask.Length;i++)if(mask[i]&&!seen[i])boxes.Add(Bounds(Flood(i,seen,w,h,n=>mask[n]),w));
        double Fraction(int x1,int y1,int x2,int y2,bool isArt)
        {
            int count=0,total=0;for(int y=y1;y<y2;y+=3)for(int x=x1;x<x2;x+=3){int o=(y*w+x)*4;int r=data[o],g=data[o+1],b=data[o+2];total++;if(isArt?Art((byte)r,(byte)g,(byte)b):Math.Min(r,Math.Min(g,b))>=205&&Math.Max(r,Math.Max(g,b))-Math.Min(r,Math.Min(g,b))<=25)count++;}
            return total==0?(isArt?0:1):count/(double)total;
        }
        bool changed=true;while(changed)
        {
            changed=false;for(int i=0;i<boxes.Count&&!changed;i++)for(int j=i+1;j<boxes.Count;j++)
            {
                var a=boxes[i];var b=boxes[j];int xo=Math.Min(a.Right,b.Right)-Math.Max(a.X,b.X),yo=Math.Min(a.Bottom,b.Bottom)-Math.Max(a.Y,b.Y);bool merge=xo>0&&yo>0;
                if(!merge&&xo>=.9*Math.Max(a.Width,b.Width)&&yo<=0){int x1=Math.Max(a.X,b.X),x2=Math.Min(a.Right,b.Right),y1=Math.Min(a.Bottom,b.Bottom),y2=Math.Max(a.Y,b.Y);merge=y2-y1<=Math.Max(2*gapY,Round(h*.004))&&Fraction(x1,y1,x2,y2,false)>=.5&&Fraction(x1,y1,x2,y2,true)>=.02;}
                else if(!merge&&yo>=.9*Math.Max(a.Height,b.Height)&&xo<=0){int x1=Math.Min(a.Right,b.Right),x2=Math.Max(a.X,b.X),y1=Math.Max(a.Y,b.Y),y2=Math.Min(a.Bottom,b.Bottom);merge=x2-x1<=Round(w*.08)&&Fraction(x1,y1,x2,y2,false)>=.55&&Fraction(x1,y1,x2,y2,true)>=.02;}
                if(merge){boxes[i]=new(Math.Min(a.X,b.X),Math.Min(a.Y,b.Y),Math.Max(a.Right,b.Right),Math.Max(a.Bottom,b.Bottom));boxes.RemoveAt(j);changed=true;break;}
            }
        }
        var split=new List<Box>();foreach(var box in boxes)
        {
            int start=box.Y,grassRun=0,skyRun=0,skyStart=0,minRun=Math.Max(8,Round(box.Height*.06));bool armed=false;
            if(placeholder)for(int y=box.Y;y<box.Bottom;y++)
            {
                int sky=0,grass=0,total=0;for(int x=box.X;x<box.Right;x+=3){int o=(y*w+x)*4;if(Sky(data[o],data[o+1],data[o+2]))sky++;else if(Grass(data[o],data[o+1],data[o+2]))grass++;total++;}
                int kind=sky/(double)total>=.4&&grass/(double)total<.08?1:grass/(double)total>=.35?2:0;
                if(kind==2){grassRun++;if(grassRun>=minRun)armed=true;skyRun=0;}
                else if(kind==1){if(skyRun==0)skyStart=y;skyRun++;if(armed&&skyRun>=minRun){split.Add(new(box.X,start,box.Right,skyStart));start=skyStart;armed=false;grassRun=0;}}
                else{skyRun=0;grassRun=0;}
            }
            split.Add(new(box.X,start,box.Right,box.Bottom));
        }
        var slots=new List<DetectedSlot>();foreach(var box in split.Where(b=>(long)b.Width*b.Height>=w*h*.005).OrderBy(b=>b.Y).ThenBy(b=>b.X).Take(16))
        {
            var slot=Oriented(mask,w,box);int pad=placeholder?Math.Max(3,Round(w*.001)):0;
            int contentRotation=0;
            if(placeholder){double sx=0,sy=0,gx=0,gy=0,sn=0,gn=0;for(int y=box.Y;y<box.Bottom;y+=4)for(int x=box.X;x<box.Right;x+=4){int o=(y*w+x)*4;if(Sky(data[o],data[o+1],data[o+2])){sx+=x;sy+=y;sn++;}else if(Grass(data[o],data[o+1],data[o+2])){gx+=x;gy+=y;gn++;}}if(sn>0&&gn>0){double dx=gx/gn-sx/sn,dy=gy/gn-sy/sn;contentRotation=Math.Abs(dy)>=Math.Abs(dx)?(dy>=0?0:180):(dx>=0?-90:90);}}
            slots.Add(slot with {X=Math.Max(0,slot.X-pad),Y=Math.Max(0,slot.Y-pad),Width=Math.Min(w,slot.Width+2*pad),Height=Math.Min(h,slot.Height+2*pad),ContentRotation=contentRotation});
        }
        return new(slots.ToArray(),method);
    }
    private static DetectedSlot Oriented(bool[] mask,int w,Box b)
    {
        var flat=new DetectedSlot(b.X,b.Y,b.Width,b.Height);double n=0,sx=0,sy=0;
        for(int y=b.Y;y<b.Bottom;y++)for(int x=b.X;x<b.Right;x++)if(mask[y*w+x]){n++;sx+=x+.5;sy+=y+.5;}
        if(n<16)return flat;double mx=sx/n,my=sy/n,xx=0,xy=0,yy=0;
        for(int y=b.Y;y<b.Bottom;y++)for(int x=b.X;x<b.Right;x++)if(mask[y*w+x]){double dx=x+.5-mx,dy=y+.5-my;xx+=dx*dx;xy+=dx*dy;yy+=dy*dy;}
        double angle=.5*Math.Atan2(2*xy,xx-yy)*180/Math.PI;if(angle>45)angle-=90;else if(angle< -45)angle+=90;if(Math.Abs(angle)<1.25||Math.Abs(angle)>35)return flat;
        double cos=Math.Cos(angle*Math.PI/180),sin=Math.Sin(angle*Math.PI/180),lx=double.PositiveInfinity,ly=lx,hx=double.NegativeInfinity,hy=hx;
        for(int y=b.Y;y<b.Bottom;y++)for(int x=b.X;x<b.Right;x++)if(mask[y*w+x]){double dx=x+.5-mx,dy=y+.5-my,a=dx*cos+dy*sin,c=-dx*sin+dy*cos;lx=Math.Min(lx,a);hx=Math.Max(hx,a);ly=Math.Min(ly,c);hy=Math.Max(hy,c);}
        double width=hx-lx+1,height=hy-ly+1;if(n/(width*height)<.72)return flat;double cx=mx+(lx+hx)/2*cos-(ly+hy)/2*sin,cy=my+(lx+hx)/2*sin+(ly+hy)/2*cos;
        return new(cx-width/2,cy-height/2,width,height,Round(angle*10)/10d);
    }
}
