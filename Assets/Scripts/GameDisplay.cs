using System.Collections;
using UnityEngine;
using DeviceScreen = UnityEngine.Screen;

namespace Neonmoves
{
    public sealed class GameDisplay : MonoBehaviour
    {
        Vector2Int windowSize = new Vector2Int(1280,720);
        public bool Switching { get; private set; }

        public static Rect ContentRect
        {
            get {
                float width=Mathf.Max(1,DeviceScreen.width),height=Mathf.Max(1,DeviceScreen.height);
                float scale=Mathf.Min(width/1600f,height/900f);
                return new Rect((width-1600*scale)/2,(height-900*scale)/2,1600*scale,900*scale);
            }
        }

        public void SetFullscreen(bool enabled)
        {
            if(Switching)return;
            // The display containing this window can differ from the primary display.
            var monitor=DeviceScreen.mainWindowDisplayInfo;
            if(enabled) {
                if(!DeviceScreen.fullScreen && DeviceScreen.width>0 && DeviceScreen.height>0)
                    windowSize=new Vector2Int(DeviceScreen.width,DeviceScreen.height);
                var desktop=DeviceScreen.currentResolution;
                int width=monitor.width>0?monitor.width:desktop.width;
                int height=monitor.height>0?monitor.height:desktop.height;
                DeviceScreen.SetResolution(width,height,FullScreenMode.FullScreenWindow);
            } else {
                int availableWidth=monitor.workArea.width>0?monitor.workArea.width:monitor.width;
                int availableHeight=monitor.workArea.height>0?monitor.workArea.height:monitor.height;
                float fit=Mathf.Min(1,Mathf.Max(1,availableWidth-32)/(float)windowSize.x,
                    Mathf.Max(1,availableHeight-64)/(float)windowSize.y);
                DeviceScreen.SetResolution(Mathf.RoundToInt(windowSize.x*fit),Mathf.RoundToInt(windowSize.y*fit),FullScreenMode.Windowed);
            }
            Switching=true;
            StartCoroutine(FinishSwitch());
        }

        IEnumerator FinishSwitch()
        {
            // Unity applies resolution changes at the end of a frame.
            yield return null;
            yield return null;
            Switching=false;
            var monitor=DeviceScreen.mainWindowDisplayInfo;
            Debug.Log($"Display: {DeviceScreen.fullScreenMode}; render={DeviceScreen.width}x{DeviceScreen.height}; monitor={monitor.width}x{monitor.height}");
        }
    }
}
