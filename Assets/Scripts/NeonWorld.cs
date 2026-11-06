using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Neonmoves
{
    public static class MeshArt
    {
        public static Material Material(Color color, float glow=0)
        {
            var shader=Resources.Load<Shader>("NeonSurface");
            var m=new Material(shader); m.SetColor("_BaseColor",color); m.SetColor("_Emission",color*glow); return m;
        }
        // Authored ring profiles form beveled clothing, armor, boots and helmet meshes.
        public static Transform Loft(string name,Transform parent,Vector3 position,Vector3[] rings,Material material,int sides=10)
        {
            var vertices=new List<Vector3>(); var triangles=new List<int>();
            foreach(var r in rings) for(int j=0;j<sides;j++) {
                float a=(j+.5f)*Mathf.PI*2/sides;
                vertices.Add(new Vector3(Mathf.Cos(a)*r.x,r.y,Mathf.Sin(a)*r.z));
            }
            for(int k=0;k<rings.Length-1;k++) for(int j=0;j<sides;j++) {
                int a=k*sides+j,b=k*sides+(j+1)%sides,c=(k+1)*sides+j,d=(k+1)*sides+(j+1)%sides;
                triangles.AddRange(new[]{a,c,b,b,c,d});
            }
            int bottomRing=vertices.Count;for(int j=0;j<sides;j++)vertices.Add(vertices[j]);
            int topRing=vertices.Count;for(int j=0;j<sides;j++)vertices.Add(vertices[(rings.Length-1)*sides+j]);
            vertices.Add(new Vector3(0,rings[0].y,0)); int bottom=vertices.Count-1;
            vertices.Add(new Vector3(0,rings[rings.Length-1].y,0)); int top=vertices.Count-1;
            for(int j=0;j<sides;j++) { triangles.AddRange(new[]{bottom,bottomRing+j,bottomRing+(j+1)%sides}); triangles.AddRange(new[]{top,topRing+(j+1)%sides,topRing+j}); }
            var mesh=new Mesh {name=name}; mesh.SetVertices(vertices); mesh.SetTriangles(triangles,0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)); go.transform.SetParent(parent,false); go.transform.localPosition=position;
            go.GetComponent<MeshFilter>().sharedMesh=mesh; go.GetComponent<MeshRenderer>().sharedMaterial=material; return go.transform;
        }
        public static Transform Shape(string name,Transform parent,Vector3 pos,Vector3 size,Material mat,int sides=8)
        {
            float x=size.x/2,y=size.y/2,z=size.z/2;
            return Loft(name,parent,pos,new[]{new Vector3(x*.75f,-y,z*.75f),new Vector3(x,-y*.7f,z),new Vector3(x,y*.7f,z),new Vector3(x*.75f,y,z*.75f)},mat,sides);
        }
        public static void Line(string name,Transform parent,Vector3 a,Vector3 b,float width,Material mat)
        {
            var go=new GameObject(name,typeof(LineRenderer)); go.transform.SetParent(parent,false);
            var line=go.GetComponent<LineRenderer>(); line.useWorldSpace=false; line.positionCount=2; line.SetPositions(new[]{a,b}); line.startWidth=line.endWidth=width; line.sharedMaterial=mat; line.numCapVertices=2;
        }
        public static void Ring(Transform parent,Vector3 pos,float radius,float width,Material mat)
        {
            var go=new GameObject("Light circuit",typeof(LineRenderer)); go.transform.SetParent(parent,false);
            var line=go.GetComponent<LineRenderer>(); line.useWorldSpace=false; line.loop=true; line.positionCount=96; line.startWidth=line.endWidth=width; line.sharedMaterial=mat;
            for(int i=0;i<96;i++){float a=i*Mathf.PI*2/96;line.SetPosition(i,pos+new Vector3(Mathf.Sin(a)*radius,0,Mathf.Cos(a)*radius));}
        }
    }
    public class Dancer
    {
        public Transform Root, Body;
        Transform head;
        readonly Transform[] upper=new Transform[2],lower=new Transform[2],thigh=new Transform[2],shin=new Transform[2];
        readonly HandRig[] hands=new HandRig[2];
        readonly float phase;
        readonly bool isPulse;
        public Dancer(Transform parent,bool pulse)
        {
            isPulse=pulse;phase=pulse?1.6f:0;
            Root=new GameObject(pulse?"PULSE / prism DJ":"NOVA / signal runner").transform; Root.SetParent(parent,false);
            Body=new GameObject("Articulated costume").transform; Body.SetParent(Root,false);
            var dark=MeshArt.Material(new Color(.035f,.045f,.10f)); var cloth=MeshArt.Material(pulse?new Color(.28f,.055f,.28f):new Color(.055f,.17f,.24f));
            var accent=MeshArt.Material(pulse?new Color(1,.1f,.56f):new Color(.1f,.95f,.86f),1.1f);
            var white=MeshArt.Material(new Color(.78f,.85f,.92f)); var metal=MeshArt.Material(new Color(.12f,.16f,.25f));
            var violet=MeshArt.Material(new Color(.43f,.22f,1),.5f);
            MeshArt.Loft("Tailored angular jacket",Body,new Vector3(0,1.23f,0),new[]{new Vector3(.25f,0,.17f),new Vector3(.28f,.14f,.2f),new Vector3(.40f,.58f,.22f),new Vector3(.34f,.72f,.18f)},cloth);
            MeshArt.Loft("High collar",Body,new Vector3(0,1.92f,0),new[]{new Vector3(.19f,0,.16f),new Vector3(.20f,.16f,.17f),new Vector3(.15f,.2f,.13f)},dark);
            MeshArt.Shape("Belt shell",Body,new Vector3(0,1.24f,-.025f),new Vector3(.57f,.16f,.39f),dark);
            MeshArt.Shape("Luminous buckle",Body,new Vector3(0,1.24f,-.225f),new Vector3(.14f,.09f,.035f),accent,4);
            // Chest panels and asymmetric zipper distinguish the two costume silhouettes.
            var panel=MeshArt.Shape("Chest insignia",Body,new Vector3(pulse?-.1f:.13f,1.73f,-.22f),new Vector3(.19f,.22f,.045f),white,4); panel.localRotation=Quaternion.Euler(0,0,22);
            MeshArt.Line("Jacket illuminated seam",Body,new Vector3(-.18f,1.36f,-.215f),new Vector3(-.25f,1.85f,-.225f),.024f,accent);
            MeshArt.Line("Chest signal",Body,new Vector3(-.12f,1.61f,-.24f),new Vector3(.25f,1.61f,-.24f),.015f,accent);
            if(pulse) {
                for(int i=0;i<3;i++) MeshArt.Shape("Equalizer chest bar",Body,new Vector3(.12f+i*.055f,1.49f,-.23f),new Vector3(.026f,.08f+i*.025f,.025f),accent,4);
            } else {
                var tail=MeshArt.Shape("Asymmetric jacket tail",Body,new Vector3(.28f,1.11f,.04f),new Vector3(.24f,.4f,.28f),cloth,5); tail.localRotation=Quaternion.Euler(0,0,12);
            }
            head=new GameObject("Helmet pivot").transform; head.SetParent(Body,false); head.localPosition=new Vector3(0,2.16f,0);
            MeshArt.Loft("Sculpted helmet",head,Vector3.zero,new[]{new Vector3(.16f,-.16f,.16f),new Vector3(.26f,-.08f,.23f),new Vector3(.27f,.18f,.23f),new Vector3(.19f,.30f,.17f),new Vector3(.06f,.34f,.055f)},pulse?white:metal,12);
            MeshArt.Shape("Black glass visor",head,new Vector3(0,.09f,-.215f),new Vector3(.46f,.20f,.10f),dark,8);
            MeshArt.Line("Visor light",head,new Vector3(-.19f,.10f,-.27f),new Vector3(.19f,.10f,-.27f),.027f,accent);
            MeshArt.Shape("Chin vent",head,new Vector3(0,-.095f,-.207f),new Vector3(.14f,.055f,.045f),metal,4);
            if(pulse) {
                for(int s=-1;s<=1;s+=2) {
                    var ear=MeshArt.Shape("Prism headphones",head,new Vector3(s*.28f,.08f,0),new Vector3(.14f,.32f,.31f),dark,10);
                    MeshArt.Shape("Headphone light",ear,new Vector3(s*.065f,0,0),new Vector3(.035f,.22f,.22f),accent,8);
                }
                MeshArt.Shape("Headband",head,new Vector3(0,.31f,.025f),new Vector3(.52f,.075f,.12f),violet,6);
            } else {
                var crest=MeshArt.Shape("Swept signal crest",head,new Vector3(.025f,.36f,.04f),new Vector3(.09f,.25f,.30f),accent,5); crest.localRotation=Quaternion.Euler(-20,0,-12);
                MeshArt.Shape("Side antenna",head,new Vector3(-.26f,.2f,.02f),new Vector3(.04f,.25f,.05f),accent,5);
            }
            for(int j=0;j<2;j++) {
                int s=j==0?-1:1;
                upper[j]=new GameObject("Shoulder "+s).transform; upper[j].SetParent(Body,false); upper[j].localPosition=new Vector3(s*.39f,1.85f,0);
                MeshArt.Shape("Shoulder shield",upper[j],new Vector3(0,-.05f,0),new Vector3(.32f,.27f,.34f),pulse?white:cloth,6);
                MeshArt.Loft("Sleeve",upper[j],Vector3.zero,new[]{new Vector3(.10f,-.49f,.10f),new Vector3(.14f,-.22f,.14f),new Vector3(.14f,-.02f,.15f)},cloth);
                MeshArt.Shape("Shoulder neon piping",upper[j],new Vector3(0,-.10f,-.165f),new Vector3(.23f,.035f,.035f),accent,4);
                lower[j]=new GameObject("Elbow "+s).transform; lower[j].SetParent(upper[j],false); lower[j].localPosition=new Vector3(0,-.46f,0);
                MeshArt.Loft("Forearm gauntlet",lower[j],Vector3.zero,new[]{new Vector3(.09f,-.43f,.09f),new Vector3(.135f,-.31f,.13f),new Vector3(.11f,-.02f,.10f)},dark);
                MeshArt.Shape("Wrist light",lower[j],new Vector3(0,-.35f,-.01f),new Vector3(.23f,.07f,.23f),accent);
                hands[j]=new HandRig(lower[j],s,pulse?white:metal,white);
                thigh[j]=new GameObject("Hip "+s).transform; thigh[j].SetParent(Root,false); thigh[j].localPosition=new Vector3(s*.17f,1.20f,0);
                MeshArt.Loft("Tapered trouser leg",thigh[j],Vector3.zero,new[]{new Vector3(.105f,-.53f,.12f),new Vector3(.15f,-.2f,.17f),new Vector3(.155f,0,.16f)},dark);
                MeshArt.Line("Trouser stripe",thigh[j],new Vector3(s*.10f,-.12f,-.12f),new Vector3(s*.08f,-.43f,-.10f),.023f,accent);
                shin[j]=new GameObject("Knee "+s).transform; shin[j].SetParent(thigh[j],false); shin[j].localPosition=new Vector3(0,-.52f,0);
                MeshArt.Shape("Knee pad",shin[j],new Vector3(0,0,-.10f),new Vector3(.23f,.21f,.12f),metal,6);
                MeshArt.Loft("Boot shaft",shin[j],Vector3.zero,new[]{new Vector3(.12f,-.48f,.13f),new Vector3(.095f,-.22f,.11f),new Vector3(.11f,-.03f,.12f)},cloth);
                MeshArt.Shape("Platform sneaker",shin[j],new Vector3(0,-.50f,-.09f),new Vector3(.29f,.20f,.48f),white,8);
                MeshArt.Shape("Luminous sole",shin[j],new Vector3(0,-.58f,-.095f),new Vector3(.30f,.055f,.49f),accent,8);
                thigh[j].localRotation=Quaternion.Euler(0,0,s*7);
                shin[j].localRotation=Quaternion.Euler(0,0,-s*7);
            }
        }
        public void Animate(int pose,float time,bool dance,bool victory=false,bool damage=false)
        {
            float blend=1-Mathf.Exp(-Time.unscaledDeltaTime*(isPulse?22:9));
            float beat=time*(112f/60)*Mathf.PI*2;
            float bob=isPulse?Mathf.Abs(Mathf.Sin(beat))*(dance?.04f:.025f):Mathf.Sin(time*(dance?3.35f:2)+phase)*.018f;
            Body.localPosition=new Vector3(isPulse?Mathf.Sin(beat*.5f)*.025f:0,bob,0);
            Body.localRotation=Quaternion.Euler(0,Mathf.Sin(time*1.5f+phase)*(isPulse?1:3),damage?Mathf.Sin(time*35)*5:isPulse?Mathf.Sin(beat*.5f)*2.5f:0);
            head.localRotation=isPulse?Quaternion.Euler(Mathf.Sin(beat)*5,0,Mathf.Sin(beat*.5f)*5):Quaternion.Euler(0,Mathf.Sin(time+phase)*8,Mathf.Sin(time*1.5f)*2);
            for(int j=0;j<2;j++) {
                float s=j==0?-1:1;
                float a=dance?GameRules.Angles[pose][j]:isPulse?-25+Mathf.Sin(beat+j*Mathf.PI)*9:-66+Mathf.Sin(time*2+phase+j)*6;
                float b=dance?GameRules.Angles[pose][j+2]:isPulse?60+Mathf.Sin(beat+j*Mathf.PI)*15:-76;
                if(victory) { a=isPulse?Mathf.Sin(beat+j*Mathf.PI)*55:65+Mathf.Sin(time*4)*10;b=isPulse?85:80; }
                float theta=s*(90+a), fore=s*(90+b)-theta;
                var shoulderPosition=upper[j].localPosition;
                shoulderPosition.z=Mathf.Lerp(shoulderPosition.z,dance&&pose==4?-.30f:0,blend);
                upper[j].localPosition=shoulderPosition;
                upper[j].localRotation=Quaternion.Slerp(upper[j].localRotation,Quaternion.Euler(0,0,theta),blend);
                lower[j].localRotation=Quaternion.Slerp(lower[j].localRotation,Quaternion.Euler(0,0,fore),blend);
                hands[j].Animate(dance?GameRules.Poses[pose].hands[j]:victory?"Victory":isPulse?"Closed_Fist":"Open_Palm",blend);
            }
        }
    }
    public class NeonWorld : MonoBehaviour
    {
        [System.NonSerialized] public Dancer Nova,Pulse;
        public Camera View;
        public Transform Podium;
        public int Character;
        public int Pose;
        public bool Dancing,Victory,Damage,Menu=true;
        Material cyan,pink;
        public void Create()
        {
            View=new GameObject("Neonmoves camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();
            View.transform.position=new Vector3(0,2.3f,-7.2f); View.transform.LookAt(new Vector3(0,1.35f,0)); View.fieldOfView=37; View.nearClipPlane=.1f; View.farClipPlane=80;
            View.clearFlags=CameraClearFlags.SolidColor; View.backgroundColor=new Color(.019f,.025f,.033f); View.allowHDR=true;
            cyan=MeshArt.Material(new Color(.12f,.42f,.39f),.65f); pink=MeshArt.Material(new Color(.30f,.20f,.25f),.4f);
            var navy=MeshArt.Material(new Color(.018f,.025f,.035f)); var grid=MeshArt.Material(new Color(.025f,.05f,.065f),.1f);
            var city=new GameObject("Night district").transform;
            MeshArt.Shape("Stage floor",city,new Vector3(0,-.19f,4),new Vector3(40,.15f,38),navy,4);
            for(int i=-12;i<=12;i+=2) {
                MeshArt.Line("Floor circuit",city,new Vector3(i,-.1f,-3),new Vector3(i,-.1f,22),.012f,grid);
                MeshArt.Line("Floor circuit",city,new Vector3(-15,-.1f,i+8),new Vector3(15,-.1f,i+8),.012f,grid);
            }
            var rng=new System.Random(61);
            for(int i=0;i<38;i++) {
                float x=-22+i*1.2f,y=1.1f+(float)rng.NextDouble()*5,z=12+(float)rng.NextDouble()*8;
                MeshArt.Shape("District tower "+i,city,new Vector3(x,y/2-.1f,z),new Vector3(.65f,y,.7f),navy,4);
                for(int k=0;k<5;k++) MeshArt.Line("Tower window",city,new Vector3(x-.22f,y*.1f+k*.55f,z-.36f),new Vector3(x+.22f,y*.1f+k*.55f,z-.36f),.025f,i%3==0?pink:grid);
            }
            // Restrained side lights leave the dancer silhouette and prompts readable.
            for(int i=-1;i<=1;i+=2)
                MeshArt.Line("Stage side light",city,new Vector3(i*4.4f,0,5),new Vector3(i*4.4f,4.5f,5),.025f,cyan);
            Podium=new GameObject("Dance platform").transform;
            MeshArt.Shape("Platform",Podium,new Vector3(0,-.12f,0),new Vector3(3.3f,.22f,2.6f),navy,64);
            MeshArt.Ring(Podium,new Vector3(0,.004f,0),1.35f,.022f,cyan); MeshArt.Ring(Podium,new Vector3(0,-.07f,0),1.55f,.035f,pink);
            Nova=new Dancer(transform,false); Pulse=new Dancer(transform,true);
            var volume=new GameObject("Neon glow",typeof(Volume)).GetComponent<Volume>(); volume.isGlobal=true; volume.profile=ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom=volume.profile.Add<Bloom>(); bloom.intensity.Override(.16f); bloom.threshold.Override(1.2f); bloom.scatter.Override(.4f);
            View.GetUniversalAdditionalCameraData().renderPostProcessing=true;
        }
        void Update()
        {
            var main=Character==0?Nova:Pulse; var other=Character==0?Pulse:Nova;
            if(main==null)return;
            Rect content=GameDisplay.ContentRect;
            View.rect=new Rect(content.x/UnityEngine.Screen.width,content.y/UnityEngine.Screen.height,
                content.width/UnityEngine.Screen.width,content.height/UnityEngine.Screen.height);
            main.Root.gameObject.SetActive(true);
            other.Root.gameObject.SetActive(false);
            float blend=1-Mathf.Exp(-Time.unscaledDeltaTime*7);
            View.fieldOfView=Mathf.Lerp(View.fieldOfView,Menu?37:40,blend);
            View.transform.position=Vector3.Lerp(View.transform.position,new Vector3(0,2.3f,Menu?-7.2f:-5.6f),blend);
            var viewRotation=Quaternion.LookRotation(new Vector3(0,Menu?1.35f:1.75f,0)-View.transform.position);
            View.transform.rotation=Quaternion.Slerp(View.transform.rotation,viewRotation,blend);
            Vector3 pos=new Vector3(Menu?1.45f:.25f,0,0);
            main.Root.localPosition=Vector3.Lerp(main.Root.localPosition,pos,blend);
            main.Root.localScale=Vector3.one; main.Root.localRotation=Quaternion.Slerp(main.Root.localRotation,Quaternion.Euler(0,Menu?-12:0,0),blend);
            other.Root.localPosition=new Vector3(Menu?2.8f:-2.7f,.02f,2.5f); other.Root.localScale=Vector3.one*.8f; other.Root.localRotation=Quaternion.Euler(0,12,0);
            Podium.position=Vector3.Lerp(Podium.position,pos,blend);
            main.Animate(Pose,Time.unscaledTime,Dancing,Victory,Damage);
            other.Animate((Pose+2)%6,Time.unscaledTime,Menu||Dancing,false,false);
        }
    }
}
