using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using U = Neonmoves.NeonUI;

namespace Neonmoves
{
    public partial class NeonGame : MonoBehaviour
    {
        Screen page=Screen.Start;
        public Screen Page {
            get=>page;
            private set {if(page==value)return;if(transition)transition.Begin(page,value);page=value;}
        }
        Screen backPage=Screen.Menu,helpReturn=Screen.Menu;
        [NonSerialized] public DanceSession Session;
        public SaveData Save;
        public AIBridge AI;
        public NeonWorld World;
        SynthMusic music;
        GameDisplay display;
        ScreenTransition transition;
        bool cameraMode,recordsCamera,started,autoPaused,recovering,calibrationVoice=true;
        readonly CameraStartGate cameraStart=new CameraStartGate();
        readonly TrackingRecoveryGate trackingRecovery=new TrackingRecoveryGate();
        int character,menuSelection,helpCharacter;
        float countdown,missingTime,feedbackTime,liveAccuracy;
        const float TrackingPauseDelay=5f;
        string feedback="",notice="";
        RunResult finalResult;
        Vector2 recordsScroll;
        readonly System.Collections.Generic.HashSet<KeyCode> guiKeys=new System.Collections.Generic.HashSet<KeyCode>();
        static readonly string[] AchievementNames={"Первый сигнал","На одной волне","Идеальное движение","Весь город твой","Камера, танец!"};
        static readonly string[] AchievementHints={"Заверши первый этап","Собери комбо из 10 движений","Получи PERFECT","Пройди все три этапа","Заверши турнир с камерой"};
        void Awake()
        {
            Application.targetFrameRate=60; Application.runInBackground=true;
            Save=SaveStore.Load(); U.Init();
            World=gameObject.AddComponent<NeonWorld>();World.Create();
            AI=gameObject.AddComponent<AIBridge>();AI.OnCommand+=Voice;AI.Launch();
            // Each launch starts with devices off; the saved preference is presented in settings.
            Save.voice=false;
            music=gameObject.AddComponent<SynthMusic>();music.Create(Save.volume);
            display=gameObject.AddComponent<GameDisplay>();display.SetFullscreen(Save.fullscreen);
            transition=gameObject.AddComponent<ScreenTransition>();
        }
        void Update()=>Tick(Time.unscaledDeltaTime,Application.isFocused,Time.realtimeSinceStartup);
        void Tick(float dt,bool focused,float now)
        {
            if(feedbackTime>0)feedbackTime-=dt;
            World.Character=character;World.Menu=Page!=Screen.Playing && Page!=Screen.Paused && Page!=Screen.StageClear;
            World.Dancing=Page==Screen.Playing;World.Victory=Page==Screen.StageClear || Page==Screen.Results && finalResult!=null && finalResult.won;
            World.Damage=feedbackTime>.5f && feedback=="MISS";
            if(Session!=null)World.Pose=Session.Current;
            var kb=Keyboard.current;
            if(transition.Active)return;
            if(trackingRecovery.Observe(Page,autoPaused,cameraMode,focused,AI.Fresh,AI.State.frameId,now)){Resume();return;}
            if(Page==Screen.Calibration && cameraStart.Observe(AI.CameraReady,AI.State.startGesture,AI.State.frameId,now)){StartRun();return;}
            if(Page!=Screen.Playing)return;
            if(cameraMode && !AI.Fresh && (countdown<=0 || recovering)) {
                missingTime+=dt; liveAccuracy=0;
                if(missingTime>=TrackingPauseDelay)Pause(true);
                return; // Freeze immediately; tracking loss never consumes time or energy.
            }
            missingTime=0;
            if(countdown>0){countdown-=dt;if(countdown<=0){recovering=false;music.RestartBeat();}return;}
            liveAccuracy=cameraMode?(AI.State.scores!=null && Session.Current<AI.State.scores.Length?AI.State.scores[Session.Current]:0):KeyboardAccuracy(kb);
            if(Session.Tick(dt,liveAccuracy)) {
                feedback=Session.LastJudgement;feedbackTime=1.1f;music.Hit(feedback!="MISS");
                if(Session.MaxCombo>=10)Unlock(1);if(feedback=="PERFECT")Unlock(2);
                if(Session.Lost){Finish(false);return;}
                if(Session.StageDone){Unlock(0);Page=Screen.StageClear;World.Victory=true;music.SetPlaying(false,0);}
            }
        }
        float KeyboardAccuracy(Keyboard kb)
        {
            int held=0,index=-1;
            for(int i=0;i<6;i++) {
                bool down=guiKeys.Contains(KeyCode.Alpha1+i);
                if(kb!=null)down|=kb[(Key)((int)Key.Digit1+i)].isPressed;
                if(down){held++;index=i;}
            }
            return held==1 && index==GameRules.KeyForPose(Session.Current)?1:0;
        }
        void Devices(bool camera){AI.Configure(camera,Page==Screen.Calibration?calibrationVoice:Save.voice,Save.cameraIndex);}
        void GoMenu(){Page=Screen.Menu;Devices(false);music.Pause(false);World.Victory=false;}
        void BeginSetup(){Page=Screen.Setup;Devices(false);}
        void Begin()
        {
            if(cameraMode){Page=Screen.Calibration;cameraStart.Reset();calibrationVoice=true;Devices(true);}else StartRun();
        }
        void StartRun()
        {
            Session=new DanceSession(Environment.TickCount&0xffff,character);started=true;recovering=autoPaused=false;trackingRecovery.Reset();countdown=3;missingTime=0;feedbackTime=0;notice="";
            Page=Screen.Playing;Devices(cameraMode);music.Pause(true);music.SetPlaying(true,0);
        }
        void Pause(bool automatic){Page=Screen.Paused;autoPaused=automatic;recovering=false;trackingRecovery.Reset();music.Pause(true);}
        void Resume(){if(cameraMode&&!AI.Fresh)return;Page=Screen.Playing;autoPaused=false;recovering=cameraMode;trackingRecovery.Reset();countdown=2;missingTime=0;Session.Elapsed=0;Session.Hold=0;Session.Best=0;Session.BeatHold=0;}
        void NextStage()
        {
            if(Session.Stage==2){Finish(true);return;}
            Session.NextStage();Page=Screen.Playing;countdown=3;music.Pause(true);music.SetPlaying(true,Session.Stage,Session.Difficulty);
        }
        void Finish(bool won)
        {
            if(!started)return; started=false;
            if(won){Unlock(3);if(cameraMode)Unlock(4);}
            finalResult=new RunResult {date=DateTime.Now.ToString("dd.MM.yyyy HH:mm"),character=character==0?"NOVA":"PULSE",mode=cameraMode?"camera":"keyboard",score=Session.Score,maxCombo=Session.MaxCombo,accuracy=Session.Accuracy,stages=Session.Cleared,won=won,grade=GameRules.Grade(Session.Accuracy,won)};
            Save.results.Add(finalResult);if(Save.results.Count>100)Save.results.RemoveAt(0);
            if(!SaveStore.Save(Save))notice="Не удалось сохранить результат. Проверь доступ к папке профиля.";
            Page=Screen.Results;Devices(false);music.Pause(false);music.SetPlaying(false,0);
        }
        void Unlock(int index){if(!Save.achievements.Contains(AchievementNames[index])){Save.achievements.Add(AchievementNames[index]);SaveStore.Save(Save);}}
        void Back()
        {
            if(Page==Screen.Playing)Pause(false);
            else if(Page==Screen.Paused)Resume();
            else if(Page==Screen.Help){Page=helpReturn;}
            else if(Page==Screen.Settings){SaveStore.Save(Save);Page=backPage;}
            else if(Page!=Screen.Start)GoMenu();
        }
        void OpenSettings(){backPage=Page==Screen.Paused?Screen.Paused:Screen.Menu;autoPaused=false;trackingRecovery.Reset();Page=Screen.Settings;}
        void ToggleFullscreen()
        {
            if(display.Switching)return;
            Save.fullscreen=!Save.fullscreen;
            display.SetFullscreen(Save.fullscreen);
            SaveStore.Save(Save);
        }
        void Voice(string command)
        {
            if(transition && transition.Active)return;
            if(!(Page==Screen.Calibration?calibrationVoice:Save.voice))return;
            switch(command) {
                case "pause":if(Page==Screen.Playing)Pause(false);break;
                case "resume":if(Page==Screen.Paused)Resume();break;
                case "start":if(Page==Screen.Menu)BeginSetup();else if(Page==Screen.Setup)Begin();else if(calibrationVoice && CameraStartGate.CanStart(Page,AI.CameraReady))StartRun();break;
                case "next":if(Page==Screen.StageClear)NextStage();break;
                case "retry":if(Page==Screen.Results)Begin();break;
                case "menu":if(Page!=Screen.Playing)GoMenu();break;
            }
        }
        void OnApplicationFocus(bool focused){if(!focused){guiKeys.Clear();trackingRecovery.Reset();if(Page==Screen.Playing)Pause(false);}}
        void OnApplicationQuit(){SaveStore.Save(Save);}
        Rect R(float x,float y,float w,float h)=>new Rect(x,y,w,h);
        void Text(float x,float y,float w,float h,string value,int size,Color color,bool bold=false)=>U.Text(R(x,y,w,h),value,size,color,bold,TextAnchor.UpperLeft,true);
        void OnGUI()
        {
            if(Save==null)return;
            var e=Event.current;
            if(e.type==EventType.KeyUp)guiKeys.Remove(e.keyCode);
            if(e.type==EventType.KeyDown && guiKeys.Add(e.keyCode)) {
                if(e.keyCode==KeyCode.F12) {
                    string folder=System.IO.Path.Combine(System.IO.Directory.GetParent(Application.dataPath).FullName,"Captures");
                    System.IO.Directory.CreateDirectory(folder);
                    ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder,Page+"-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".png"));
                }
                else if(e.alt && e.keyCode==KeyCode.Return)ToggleFullscreen();
                else if(transition.Active){} // Do not carry one click/key through to the next screen.
                else if(Page==Screen.Start && (e.keyCode==KeyCode.Return||e.keyCode==KeyCode.Space))GoMenu();
                else if(Page==Screen.Menu && e.keyCode==KeyCode.DownArrow)menuSelection=(menuSelection+1)%5;
                else if(Page==Screen.Menu && e.keyCode==KeyCode.UpArrow)menuSelection=(menuSelection+4)%5;
                else if(Page==Screen.Menu && e.keyCode==KeyCode.Return)MenuAction(menuSelection);
                else if(e.keyCode==KeyCode.Escape)Back();
                else if(e.keyCode==KeyCode.Space) {
                    if(Page==Screen.Playing)Pause(false);else if(Page==Screen.Paused)Resume();else if(Page==Screen.StageClear)NextStage();
                }
            }
            Rect content=GameDisplay.ContentRect;
            float scale=content.width/1600f;
            float ox=content.x,oy=content.y;
            GUI.matrix=Matrix4x4.TRS(new Vector3(ox,oy,0),Quaternion.identity,new Vector3(scale,scale,1));
            if(transition.Active){transition.Draw();GUI.matrix=Matrix4x4.identity;return;}
            // Preserve the composition at 16:9, 16:10 and portrait window sizes.
            if(ox>0){U.Fill(R(-ox/scale,0,ox/scale,900),U.Ink);U.Fill(R(1600,0,ox/scale,900),U.Ink);}
            if(oy>0){U.Fill(R(0,-oy/scale,1600,oy/scale),U.Ink);U.Fill(R(0,900,1600,oy/scale),U.Ink);}
            U.Interactive=!transition.Active;
            if(Page==Screen.Start)DrawStart();
            else if(Page==Screen.Menu)DrawMenu();
            else if(Page==Screen.Setup)DrawSetup();
            else if(Page==Screen.Calibration)DrawCalibration();
            else if(Page==Screen.Playing)DrawGame();
            else if(Page==Screen.Paused){DrawGame();DrawPause();}
            else if(Page==Screen.StageClear)DrawStageClear();
            else if(Page==Screen.Results)DrawResults();
            else if(Page==Screen.Settings)DrawSettings();
            else if(Page==Screen.Records)DrawRecords();
            else if(Page==Screen.Help)DrawHelp();
            GUI.matrix=Matrix4x4.identity;
        }
    }
}
