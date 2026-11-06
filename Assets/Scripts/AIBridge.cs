using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.Networking;

namespace Neonmoves
{
    [Serializable] public class AIState
    {
        public string service, cameraStatus = "off", voiceStatus = "off", cameraError = "", voiceError = "", cameraBackend, command, heard;
        public bool visible,lowLight,startGesture;
        public string leftGesture,rightGesture;
        public int version,frameId;
        public float confidence, poseAge = 999, fps;
        public int commandId;
        public float[] scores = new float[GameRules.PoseCount];
    }
    [Serializable] public class AIConfig { public bool camera, voice; public int cameraIndex,cameraRevision; }
    public class AIBridge : MonoBehaviour
    {
        public AIState State = new AIState();
        public Texture2D Preview;
        public bool Connected { get; private set; }
        public string Error { get; private set; } = "";
        public event Action<string> OnCommand;
        System.Diagnostics.Process process;
        string url, token;
        int commandId;
        float lastReply;
        bool camera, voice,configSending;
        int cameraIndex,cameraRevision,configVersion;
        public bool CameraReady => Connected && Time.realtimeSinceStartup-lastReply < 1.2f && State.cameraStatus=="ready" && State.poseAge < .7f;
        public bool Fresh => CameraReady && State.visible;
        public void Launch()
        {
            if (process != null) return;
            try {
                string root = Application.isEditor ? Directory.GetParent(Application.dataPath).FullName : Directory.GetParent(Application.dataPath).FullName;
                string folder = Path.Combine(root,"AI"), python = Path.Combine(folder,"runtime","python.exe");
                if (!File.Exists(python)) { Error = "AI не установлен. Доступна тренировка с клавиатурой."; return; }
                var listener = new TcpListener(IPAddress.Loopback,0); listener.Start(); int port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
                token = Guid.NewGuid().ToString("N"); url = "http://127.0.0.1:" + port;
                process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                    FileName=python, Arguments="\""+Path.Combine(folder,"bridge.py")+"\" --port "+port+" --token "+token,
                    WorkingDirectory=folder, UseShellExecute=false, CreateNoWindow=true
                });
                StartCoroutine(Poll()); StartCoroutine(Frames());
            } catch(Exception e) { Error="Не удалось запустить AI: "+e.Message; }
        }
        public void Configure(bool useCamera, bool useVoice, int index)
        {
            bool newCamera=useCamera && (!camera || index!=cameraIndex);
            if(newCamera)cameraRevision++;
            camera=useCamera; voice=useVoice; cameraIndex=index;
            if(!camera || newCamera)ClearCamera();
            State.cameraStatus=camera?(newCamera?"loading":State.cameraStatus):"off";
            QueueConfig();
        }
        void ClearCamera()
        {
            State.visible=false;State.startGesture=false;State.poseAge=999;State.cameraError="";
            if(Preview)Destroy(Preview);Preview=null;
        }
        public void RetryCamera()
        {
            cameraRevision++;ClearCamera();State.cameraStatus="loading";
            if(process!=null && process.HasExited) {
                StopAllCoroutines();configSending=false;Connected=false;
                process.Dispose();process=null;commandId=0;Launch();
            }
            QueueConfig();
        }
        void QueueConfig()
        {
            configVersion++;
            if(Connected && !configSending)StartCoroutine(SendConfig());
        }
        IEnumerator SendConfig()
        {
            configSending=true;
            while(Connected) {
                int sentVersion=configVersion;
                using (var req = new UnityWebRequest(url+"/configure","POST")) {
                req.uploadHandler=new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(new AIConfig {camera=camera,voice=voice,cameraIndex=cameraIndex,cameraRevision=cameraRevision})));
                req.downloadHandler=new DownloadHandlerBuffer(); req.SetRequestHeader("Content-Type","application/json"); req.SetRequestHeader("X-Neonmoves-Token",token); req.timeout=2;
                yield return req.SendWebRequest();
                if (req.result!=UnityWebRequest.Result.Success) Error="Не удалось включить устройство. Повтори попытку.";
                }
                if(sentVersion==configVersion)break;
            }
            configSending=false;
        }
        IEnumerator Poll()
        {
            bool wasConnected=false;
            while(true) {
                using(var req=UnityWebRequest.Get(url+"/state")) {
                    req.SetRequestHeader("X-Neonmoves-Token",token); req.timeout=2;
                    yield return req.SendWebRequest();
                    if(req.result==UnityWebRequest.Result.Success) {
                        try {
                            var next=JsonUtility.FromJson<AIState>(req.downloadHandler.text);
                            if(next!=null && next.service=="NeonmovesAI" && next.version==4 && next.scores!=null && next.scores.Length==GameRules.PoseCount) {
                                State=next; Connected=true; lastReply=Time.realtimeSinceStartup; Error="";
                                if(!wasConnected)QueueConfig();
                                if(State.commandId>commandId) { commandId=State.commandId; OnCommand?.Invoke(State.command); }
                            } else {Connected=false;Error="Набор поз AI не совпадает с игрой. Обнови папку AI рядом с EXE.";}
                        } catch(Exception e) { Error=e.Message; }
                    } else { Connected=false; State.visible=false;if(process!=null && process.HasExited)Error="Служба распознавания остановилась. Нажми «Подключить заново»."; }
                }
                wasConnected=Connected;
                yield return new WaitForSecondsRealtime(.06f);
            }
        }
        IEnumerator Frames()
        {
            while(true) {
                if(camera && Connected) {
                    int requestedCamera=cameraRevision;
                    using(var req=UnityWebRequest.Get(url+"/frame")) {
                        req.SetRequestHeader("X-Neonmoves-Token",token); req.timeout=2;
                        yield return req.SendWebRequest();
                        if(camera && requestedCamera==cameraRevision && req.result==UnityWebRequest.Result.Success && req.responseCode==200)
                            ApplyFrame(req.downloadHandler?.data);
                    }
                }
                yield return new WaitForSecondsRealtime(.08f);
            }
        }
        void ApplyFrame(byte[] data)
        {
            // HTTP 204 while the camera warms up is expected; DownloadHandler.data can be null.
            if(data==null || data.Length==0)return;
            try {
                bool first=!Preview;
                if(first)Preview=new Texture2D(2,2,TextureFormat.RGB24,false);
                if(Preview.LoadImage(data) && first)Debug.Log("AI camera preview ready: "+Preview.width+"x"+Preview.height);
            } catch(Exception e) {Debug.LogWarning("Camera frame ignored: "+e.Message);}
        }
        void OnDestroy()
        {
            if(Preview) Destroy(Preview);
            try { if(process!=null && !process.HasExited) process.Kill(); process?.Dispose(); } catch(Exception) { }
        }
    }
}
