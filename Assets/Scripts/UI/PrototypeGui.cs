using System.Collections.Generic;
using UnityEngine;

namespace Ashbound
{
    public static class PrototypeGui
    {
        public const float CanvasWidth=1280,CanvasHeight=720,MinimumButtonHeight=30,PreparationButtonHeight=40;
        public static GUIStyle Title,Heading,Text,Small,Button,Center,CardTitle,Tooltip,Resource,Metadata;
        private static readonly Dictionary<string,GUIStyle> FittedStyles=new Dictionary<string,GUIStyle>();
        private static Font interfaceFont;
        private static GameLanguage loadedLanguage;
        public static string FontName=>interfaceFont?interfaceFont.name:"Unavailable";
        public static bool SupportsEnglish=>interfaceFont&&interfaceFont.HasCharacter('A');
        public static bool SupportsSimplifiedChinese=>interfaceFont&&interfaceFont.HasCharacter('汉');

        public static void Initialize()=>Initialize(LocalizationService.Current);
        public static void Initialize(GameLanguage language)
        {
            if(Title!=null&&loadedLanguage==language)return;if(Title!=null)ResetStyles();loadedLanguage=language;
            string[] fonts=language==GameLanguage.SimplifiedChinese
                ?new[]{"Microsoft YaHei UI","Microsoft YaHei","DengXian","SimHei","Noto Sans CJK SC","Arial Unicode MS"}
                :new[]{"Microsoft YaHei UI","Microsoft YaHei","Noto Sans CJK SC","Arial Unicode MS","Segoe UI","Arial"};
            interfaceFont=Font.CreateDynamicFontFromOSFont(fonts,18);
            bool drawing=Event.current!=null;if(interfaceFont&&drawing)GUI.skin.font=interfaceFont;
            GUIStyle labelBase=drawing?new GUIStyle(GUI.skin.label):new GUIStyle();GUIStyle buttonBase=drawing?new GUIStyle(GUI.skin.button):new GUIStyle();
            Title=Style(labelBase,"Title",38,Palette.Gold,FontStyle.Bold,TextAnchor.MiddleLeft,true);
            Heading=Style(labelBase,"Heading",22,Color.white,FontStyle.Bold,TextAnchor.MiddleLeft,true);
            Text=Style(labelBase,"Body",16,new Color(.87f,.89f,.91f),FontStyle.Normal,TextAnchor.UpperLeft,true);
            Small=Style(labelBase,"Small metadata",12,new Color(.72f,.77f,.82f),FontStyle.Normal,TextAnchor.UpperLeft,true);
            Metadata=Style(labelBase,"Metadata",12,new Color(.66f,.72f,.77f),FontStyle.Normal,TextAnchor.MiddleLeft,true);
            Resource=Style(labelBase,"Resource",16,Palette.Gold,FontStyle.Bold,TextAnchor.MiddleLeft,false);
            Tooltip=Style(labelBase,"Tooltip",12,new Color(.9f,.92f,.94f),FontStyle.Normal,TextAnchor.UpperLeft,true);
            Center=Style(labelBase,"Centered body",18,Color.white,FontStyle.Normal,TextAnchor.MiddleCenter,true);
            CardTitle=Style(labelBase,"Section header",17,Palette.Gold,FontStyle.Bold,TextAnchor.MiddleLeft,true);
            Button=new GUIStyle(buttonBase){name="Button",font=interfaceFont,fontSize=14,fontStyle=FontStyle.Normal,padding=new RectOffset(9,9,4,4),margin=new RectOffset(2,2,2,2),alignment=TextAnchor.MiddleCenter,wordWrap=true,clipping=TextClipping.Clip};
        }

        private static GUIStyle Style(GUIStyle basis,string name,int size,Color color,FontStyle weight,TextAnchor alignment,bool wrap)
        {
            var style=new GUIStyle(basis){name=name,font=interfaceFont,fontSize=size,fontStyle=weight,alignment=alignment,wordWrap=wrap,clipping=TextClipping.Clip,padding=new RectOffset(2,2,2,2)};
            style.normal.textColor=color;return style;
        }

        private static GUIStyle Fit(GUIStyle source,string text,float width,float height,int minimumSize)
        {
            Initialize();if(string.IsNullOrEmpty(text)||width<=4||height<=4)return source;
            var content=new GUIContent(text);int size=source.fontSize;string key=source.name+"|"+size+"|"+Mathf.RoundToInt(width)+"|"+Mathf.RoundToInt(height)+"|"+text;
            if(FittedStyles.TryGetValue(key,out var cached))return cached;
            var fitted=new GUIStyle(source);
            while(size>minimumSize&&!ContentFits(fitted,content,width,height)){size--;fitted.fontSize=size;}
            FittedStyles[key]=fitted;return fitted;
        }

        private static bool ContentFits(GUIStyle style,GUIContent content,float width,float height)
        {
            if(style.CalcHeight(content,width)>height+.1f)return false;
            return style.wordWrap||style.CalcSize(content).x<=width+.1f;
        }

        public static float RequiredHeight(string text,float width,GUIStyle style=null){Initialize();return (style??Text).CalcHeight(new GUIContent(text??""),Mathf.Max(1,width));}
        public static bool Fits(string text,Rect rect,GUIStyle style=null,int minimumSize=10){Initialize();var content=new GUIContent(text??"");var fitted=Fit(style??Text,text,rect.width,rect.height,minimumSize);return ContentFits(fitted,content,rect.width,rect.height);}
        public static float ButtonHeight(string text,float width,float minimum=MinimumButtonHeight){Initialize();return Mathf.Max(minimum,Button.CalcHeight(new GUIContent(text??""),Mathf.Max(1,width))+4);}
        public static Rect LogicalViewport(Vector2Int resolution)
        {
            float scale=Mathf.Min(resolution.x/CanvasWidth,resolution.y/CanvasHeight);float width=CanvasWidth*scale,height=CanvasHeight*scale;
            return new Rect((resolution.x-width)*.5f,(resolution.y-height)*.5f,width,height);
        }
        public static void Box(Rect rect,Color color){var old=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=old;}
        public static void Panel(Rect rect){Box(rect,new Color(.045f,.06f,.08f,.96f));Box(new Rect(rect.x,rect.y,rect.width,2),new Color(.45f,.39f,.26f));}
        public static bool Click(Rect rect,string label)=>GUI.Button(rect,label,Fit(Button,label,rect.width,rect.height,10));
        public static void Label(float x,float y,float width,float height,string text,GUIStyle style=null){var rect=new Rect(x,y,width,height);GUI.Label(rect,text,Fit(style??Text,text,width,height,10));}
        public static Matrix4x4 Scale()
        {
            Initialize();var old=GUI.matrix;float scale=Mathf.Min(Screen.width/CanvasWidth,Screen.height/CanvasHeight);float x=(Screen.width-CanvasWidth*scale)*.5f,y=(Screen.height-CanvasHeight*scale)*.5f;
            GUI.matrix=Matrix4x4.TRS(new Vector3(x,y,0),Quaternion.identity,new Vector3(scale,scale,1));return old;
        }
        public static void Bar(Rect rect,float fraction,Color color){Box(rect,new Color(.16f,.19f,.23f));Box(new Rect(rect.x,rect.y,rect.width*Mathf.Clamp01(fraction),rect.height),color);}
        public static void ResetStyles(){Title=Heading=Text=Small=Button=Center=CardTitle=Tooltip=Resource=Metadata=null;FittedStyles.Clear();if(interfaceFont){if(Application.isPlaying)Object.Destroy(interfaceFont);else Object.DestroyImmediate(interfaceFont);}interfaceFont=null;}
    }
}
