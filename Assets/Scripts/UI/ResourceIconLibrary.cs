using System.Collections.Generic;
using UnityEngine;

namespace Ashbound
{
    public static class ResourceIconLibrary
    {
        private static readonly Dictionary<ExpeditionResource,Texture2D> icons=new Dictionary<ExpeditionResource,Texture2D>();

        public static Texture2D Icon(ExpeditionResource resource)
        {
            if(icons.TryGetValue(resource,out var icon)&&icon)return icon;
            icon=new Texture2D(32,32,TextureFormat.RGBA32,false){name="Resource icon · "+resource,filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            var clear=new Color32[32*32];icon.SetPixels32(clear);
            Color color=resource==ExpeditionResource.Ash?new Color(.67f,.69f,.72f):resource==ExpeditionResource.EmberShards?new Color(1f,.39f,.08f):resource==ExpeditionResource.AncientAlloy?new Color(.63f,.82f,.91f):new Color(.72f,.26f,.92f);
            if(resource==ExpeditionResource.Ash)Ash(icon,color);else if(resource==ExpeditionResource.EmberShards)Ember(icon,color);else if(resource==ExpeditionResource.AncientAlloy)Alloy(icon,color);else Corruption(icon,color);
            icon.Apply(false,true);icons[resource]=icon;return icon;
        }

        private static void Ash(Texture2D t,Color c)
        {
            Disc(t,10,20,6,c);Disc(t,17,17,8,c);Disc(t,23,21,5,c);Rect(t,7,22,25,26,new Color(c.r*.7f,c.g*.7f,c.b*.7f,1));
            Disc(t,11,12,2,new Color(1,1,1,.7f));Disc(t,21,11,2,new Color(1,1,1,.55f));
        }
        private static void Ember(Texture2D t,Color c)
        {
            for(int y=4;y<28;y++){int half=y<16?(y-4)/2:(28-y)/2;for(int x=16-half;x<=16+half;x++)Set(t,x,y,c);}
            for(int y=11;y<25;y++){int half=y<18?(y-11)/3:(25-y)/3;for(int x=16-half;x<=16+half;x++)Set(t,x,y,new Color(1f,.78f,.2f));}
        }
        private static void Alloy(Texture2D t,Color c)
        {
            Rect(t,5,10,25,24,new Color(c.r*.65f,c.g*.65f,c.b*.65f));Rect(t,8,7,28,21,c);Rect(t,11,10,25,13,new Color(1,1,1,.75f));
            for(int i=0;i<5;i++){Set(t,8+i,8+i,c);Set(t,27-i,20-i,c);}
        }
        private static void Corruption(Texture2D t,Color c)
        {
            Disc(t,16,16,11,new Color(c.r*.38f,c.g*.22f,c.b*.45f,.9f));
            for(int i=5;i<27;i++){for(int w=-2;w<=2;w++){Set(t,i,i+w,c);Set(t,31-i,i+w,c);}}
            Disc(t,16,16,3,new Color(1f,.62f,1f));
        }
        private static void Rect(Texture2D t,int x0,int y0,int x1,int y1,Color c){for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++)Set(t,x,y,c);}
        private static void Disc(Texture2D t,int cx,int cy,int r,Color c){for(int y=-r;y<=r;y++)for(int x=-r;x<=r;x++)if(x*x+y*y<=r*r)Set(t,cx+x,cy+y,c);}
        private static void Set(Texture2D t,int x,int y,Color c){if(x>=0&&x<t.width&&y>=0&&y<t.height)t.SetPixel(x,y,c);}

        public static void Dispose(){foreach(var icon in icons.Values)if(icon)Object.Destroy(icon);icons.Clear();}
    }
}
