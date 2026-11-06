namespace Neonmoves
{
    public sealed class TrackingRecoveryGate
    {
        // A single good detection should not release a pause while tracking is flickering.
        readonly CameraStartGate hold=new CameraStartGate(.6f);
        public float Progress=>hold.Progress;
        public void Reset()=>hold.Reset();
        public bool Observe(Screen page,bool automatic,bool camera,bool focused,bool fresh,int frameId,float now)
        {
            bool allowed=page==Screen.Paused && automatic && camera && focused && fresh;
            return hold.Observe(allowed,true,frameId,now);
        }
    }
}
