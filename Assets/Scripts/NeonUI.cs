using System.Collections.Generic;
using UnityEngine;

namespace Neonmoves
{
    public static class NeonUI
    {
        public static bool Interactive=true;
        public static readonly Color Ink=new Color(.025f,.03f,.04f), Panel=new Color(.055f,.065f,.08f,.97f), Muted=new Color(.53f,.57f,.63f), White=new Color(.91f,.93f,.94f), Cyan=new Color(.39f,.78f,.72f), Pink=new Color(.91f,.46f,.41f), Violet=new Color(.61f,.66f,.81f), LineColor=new Color(.18f,.21f,.25f);
        static Texture2D fade,white,logo;
        static Rect logoUV=new Rect(0,0,1,1);
        static float logoAspect=8;
        static Font regular,bold;
        static readonly Dictionary<string,GUIStyle> styles=new Dictionary<string,GUIStyle>();
        public static void Init()
        {
            white=Texture2D.whiteTexture; regular=Resources.Load<Font>("Fonts/NotoSans-Regular"); bold=Resources.Load<Font>("Fonts/NotoSans-Bold");
            if(!regular)regular=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");if(!bold)bold=regular;
            fade=new Texture2D(128,1,TextureFormat.RGBA32,false);fade.wrapMode=TextureWrapMode.Clamp;
            for(int x=0;x<128;x++)fade.SetPixel(x,0,new Color(Ink.r,Ink.g,Ink.b,1-Mathf.SmoothStep(0,1,x/127f)));
            fade.Apply();
            logo=Resources.Load<Texture2D>("Brand/NeonmovesLogo");
            if(logo) {
                // Fit the visible wordmark, not the generator's transparent canvas margins.
                var pixels=logo.GetPixels32();int x0=logo.width,y0=logo.height,x1=-1,y1=-1;
                for(int y=0;y<logo.height;y++)for(int x=0;x<logo.width;x++) {
                    if(pixels[y*logo.width+x].a<=16)continue;
                    x0=Mathf.Min(x0,x);y0=Mathf.Min(y0,y);x1=Mathf.Max(x1,x);y1=Mathf.Max(y1,y);
                }
                if(x1>=x0) {
                    x0=Mathf.Max(0,x0-2);y0=Mathf.Max(0,y0-2);x1=Mathf.Min(logo.width,x1+3);y1=Mathf.Min(logo.height,y1+3);
                    logoUV=new Rect(x0/(float)logo.width,y0/(float)logo.height,(x1-x0)/(float)logo.width,(y1-y0)/(float)logo.height);
                    logoAspect=(x1-x0)/(float)(y1-y0);
                }
            }
        }
        public static void Logo(Rect r,bool centered=false)
        {
            if(!logo){Text(r,"NEONMOVES",24,White,true,centered?TextAnchor.MiddleCenter:TextAnchor.MiddleLeft);return;}
            float width=Mathf.Min(r.width,r.height*logoAspect),height=width/logoAspect;
            var area=new Rect(centered?r.center.x-width/2:r.x,r.center.y-height/2,width,height);
            GUI.color=Color.white;GUI.DrawTextureWithTexCoords(area,logo,logoUV,true);
        }
        static GUIStyle Style(int size,Color color,bool strong,TextAnchor alignment,bool wrap)
        {
            string key=size+"_"+strong+"_"+(int)alignment+"_"+wrap;
            if(!styles.TryGetValue(key,out var s)) { s=new GUIStyle {font=strong?bold:regular,fontSize=size,alignment=alignment,wordWrap=wrap,richText=false,clipping=TextClipping.Clip,padding=new RectOffset(0,0,0,0)};styles[key]=s; }
            s.normal.textColor=QualitySettings.activeColorSpace==ColorSpace.Linear?color.gamma:color;return s;
        }
        public static void Text(Rect r,string text,int size,Color color,bool bold=false,TextAnchor align=TextAnchor.UpperLeft,bool wrap=false)
        { GUI.Label(r,text,Style(size,color,bold,align,wrap)); }
        public static void Fill(Rect r,Color color){GUI.color=color;GUI.DrawTexture(r,white);GUI.color=Color.white;}
        public static void Box(Rect r,Color color)
        { Fill(r,color); }
        public static void Shade(Rect r){GUI.color=Color.white;GUI.DrawTexture(r,fade);}
        public static void Rule(float x,float y,float w,Color? color=null){Fill(new Rect(x,y,w,1),color??LineColor);}
        public static bool Button(Rect r,string text,bool primary=false,bool active=false)
        {
            bool hover=Interactive && GUI.enabled && r.Contains(Event.current.mousePosition);
            var color=!GUI.enabled?Panel:primary?(hover?Color.white:White):hover?new Color(.15f,.18f,.21f):active?new Color(.10f,.18f,.18f):Panel;
            Fill(r,color);
            if(active)Fill(new Rect(r.x,r.y,3,r.height),Cyan);
            Text(r,text,18,!GUI.enabled?Muted:primary?Ink:active?Cyan:White,true,TextAnchor.MiddleCenter);
            return Click(r);
        }
        public static bool Nav(Rect r,string text,bool selected=false)
        {
            bool hover=Interactive && r.Contains(Event.current.mousePosition);
            if(selected){Fill(r,new Color(.13f,.17f,.19f,.7f));Fill(new Rect(r.x,r.y,3,r.height),Cyan);}
            Text(new Rect(r.x+24,r.y,r.width-60,r.height),text,30,hover||selected?White:Muted,true,TextAnchor.MiddleLeft);
            if(selected)Text(new Rect(r.xMax-44,r.y,30,r.height),"›",30,Cyan,false,TextAnchor.MiddleCenter);
            return Click(r);
        }
        static bool Click(Rect r)
        {
            bool enabled=GUI.enabled;GUI.enabled=enabled && Interactive;
            bool clicked=GUI.Button(r,GUIContent.none,GUIStyle.none);
            GUI.enabled=enabled;return clicked;
        }
        public static void Tag(float x,float y,string text,Color color,float width=145)
        {Text(new Rect(x,y,width,30),text,12,color,true,TextAnchor.MiddleLeft);}
        public static void Bar(Rect r,float fraction,Color color)
        {Fill(r,LineColor);if(fraction>0)Fill(new Rect(r.x,r.y,r.width*Mathf.Clamp01(fraction),r.height),color);}
        public static void Line(Vector2 a,Vector2 b,Color color,float width=3)
        {
            Matrix4x4 old=GUI.matrix;float angle=Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg;
            GUI.matrix=old*Matrix4x4.TRS(new Vector3(a.x,a.y,0),Quaternion.Euler(0,0,angle),Vector3.one);
            Fill(new Rect(0,-width/2,Vector2.Distance(a,b),width),color);GUI.matrix=old;
        }
        public static void Pose(Rect r,int pose,Color color)
        {
            float scale=r.height/140f; Vector2 center=new Vector2(r.center.x,r.y+25*scale);
            Box(new Rect(center.x-8*scale,center.y-18*scale,16*scale,18*scale),color);
            Vector2 neck=center+new Vector2(0,9*scale),hip=center+new Vector2(0,62*scale);
            Line(neck,hip,color,4*scale);
            for(int j=0;j<2;j++) {
                float s=j==0?-1:1;Vector2 shoulder=neck+new Vector2(s*15*scale,8*scale);Line(neck,shoulder,color,4*scale);
                var angles=GameRules.Angles[pose];float a=angles[j]*Mathf.Deg2Rad,b=angles[j+2]*Mathf.Deg2Rad;
                Vector2 elbow=shoulder+new Vector2(s*Mathf.Cos(a),-Mathf.Sin(a))*25*scale,wrist=elbow+new Vector2(s*Mathf.Cos(b),-Mathf.Sin(b))*24*scale;
                Line(shoulder,elbow,color,4*scale);Line(elbow,wrist,color,4*scale);
                Box(new Rect(wrist.x-3*scale,wrist.y-3*scale,6*scale,6*scale),color);
            }
        }
        public static void Hand(Rect r,string gesture,Color color,bool mirror=false)
        {
            // Upright palm diagrams keep finger shapes readable independently of arm rotation.
            var fingers=GameRules.Gesture(gesture).fingers;
            float unit=Mathf.Min(r.width/62,r.height/80),side=mirror?-1:1;
            Vector2 origin=new Vector2(r.center.x,r.y+56*unit);
            Vector2 P(float x,float y)=>origin+new Vector2(x*unit*side,y*unit);
            Line(P(-14,0),P(14,0),color,3*unit);Line(P(-14,0),P(-14,-19),color,3*unit);
            Line(P(14,0),P(14,-19),color,3*unit);Line(P(-8,0),P(-8,13),color,3*unit);Line(P(8,0),P(8,13),color,3*unit);
            for(int i=1;i<5;i++) {
                float x=-12+(i-1)*8,tip=x;
                if(gesture=="Victory")tip+=i==1?-5:i==2?5:0;
                float height=fingers[i]==1?(i==3?30:i==4?23:34):8;
                Line(P(x,-19),P(tip,-19-height),color,5*unit);
                if(fingers[i]==0)Line(P(x,-27),P(x,-14),color,3*unit);
            }
            if(gesture=="Thumb_Up") {
                Line(P(-14,-8),P(-23,-22),color,5*unit);Line(P(-23,-22),P(-23,-50),color,5*unit);
            } else if(fingers[0]==1) {
                Line(P(-14,-8),P(-27,-21),color,5*unit);Line(P(-27,-21),P(-30,-32),color,5*unit);
            } else Line(P(-14,-10),P(7,-14),color,5*unit);
        }
    }
}
