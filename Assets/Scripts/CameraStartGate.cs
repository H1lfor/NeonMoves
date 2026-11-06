using UnityEngine;

namespace Neonmoves
{
    // Start only from fresh camera observations; repeated HTTP snapshots do not extend a hold.
    public sealed class CameraStartGate
    {
        public const float HoldSeconds=1f;
        readonly float holdSeconds;
        const float MaxFrameGap=.45f;
        int lastFrameId=-1;
        float since=-1,lastFrameTime=-1;
        bool fired;
        public CameraStartGate(float seconds=HoldSeconds){holdSeconds=seconds;}
        public float Progress=>since<0?0:Mathf.Clamp01((lastFrameTime-since)/holdSeconds);
        public static bool CanStart(Screen page,bool cameraReady)=>page==Screen.Calibration && cameraReady;
        public void Reset(){lastFrameId=-1;since=lastFrameTime=-1;fired=false;}
        public bool Observe(bool ready,bool thumbUp,int frameId,float now)
        {
            if(!ready){Reset();return false;}
            if(frameId==lastFrameId) {
                if(now-lastFrameTime>MaxFrameGap){since=-1;fired=false;}
                return false;
            }
            float gap=lastFrameTime<0?0:now-lastFrameTime;
            lastFrameId=frameId;lastFrameTime=now;
            if(!thumbUp){since=-1;fired=false;return false;}
            if(since<0 || gap>MaxFrameGap){since=now;fired=false;}
            if(fired || now-since<holdSeconds)return false;
            fired=true;return true;
        }
    }
}
