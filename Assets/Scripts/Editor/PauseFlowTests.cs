using System;
using System.Reflection;
using UnityEngine;

namespace Neonmoves.Editor
{
    // Exercise the actual game controller without opening devices, creating audio or saving progress.
    static class PauseFlowTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static void Set(object instance,string field,object value)=>instance.GetType().GetField(field,Private).SetValue(instance,value);
        static T Get<T>(object instance,string field)=>(T)instance.GetType().GetField(field,Private).GetValue(instance);
        static void Call(object instance,string method,params object[] args)=>instance.GetType().GetMethod(method,Private).Invoke(instance,args);
        public static void Run(Action<string,bool> check)
        {
            var root=new GameObject("Pause flow test");root.SetActive(false);
            try {
                var game=root.AddComponent<NeonGame>();
                game.AI=root.AddComponent<AIBridge>();game.World=root.AddComponent<NeonWorld>();
                game.Session=new DanceSession(73,0);
                Set(game,"music",root.AddComponent<SynthMusic>());
                Set(game,"transition",root.AddComponent<ScreenTransition>());
                Set(game,"cameraMode",true);Set(game,"page",Screen.Playing);
                typeof(AIBridge).GetProperty("Connected").SetValue(game.AI,true);
                Set(game.AI,"lastReply",Time.realtimeSinceStartup);
                game.AI.State.cameraStatus="ready";game.AI.State.poseAge=0;
                float now=0;
                void Tick(bool fresh,bool focused=true,float delta=.1f) {
                    game.AI.State.visible=fresh;game.AI.State.frameId++;
                    Set(game.AI,"lastReply",Time.realtimeSinceStartup);
                    Get<TransitionTiming>(Get<ScreenTransition>(game,"transition"),"timing").Advance(delta);
                    now+=delta;Call(game,"Tick",delta,focused,now);
                }
                float energy=game.Session.Energy;int score=game.Session.Score;
                Tick(false,true,2.1f);
                check("Brief tracking loss does not open the pause screen",game.Page==Screen.Playing && game.Session.Elapsed==0 && game.Session.Energy==energy);
                Tick(false,true,3f);
                check("Game controller enters automatic pause after tracking loss",game.Page==Screen.Paused && Get<bool>(game,"autoPaused"));
                for(int i=0;i<12;i++)Tick(true);
                check("Game controller returns from tracking pause without input",game.Page==Screen.Playing && Get<float>(game,"countdown")>1.7f);
                check("Automatic recovery preserves score energy and move timer",game.Session.Score==score && game.Session.Energy==energy && game.Session.Elapsed==0);
                float countdown=Get<float>(game,"countdown");Tick(false);
                check("Brief tracking loss during recovery keeps the countdown",game.Page==Screen.Playing && Get<float>(game,"countdown")==countdown);
                Tick(false,true,5f);
                check("Losing hands during recovery countdown restores automatic pause",game.Page==Screen.Paused && Get<bool>(game,"autoPaused"));
                Call(game,"OnApplicationFocus",false);
                for(int i=0;i<8;i++)Tick(true,false);
                check("Tracking pause stays paused while the window is unfocused",game.Page==Screen.Paused && Get<bool>(game,"autoPaused"));
                for(int i=0;i<8;i++)Tick(true);
                check("Returning to the game window preserves automatic recovery",game.Page==Screen.Playing);
                Call(game,"Pause",false);
                for(int i=0;i<12;i++)Tick(true);
                check("Manual pause is not released by tracking recovery",game.Page==Screen.Paused && !Get<bool>(game,"autoPaused"));
                Call(game,"Pause",true);Call(game,"OpenSettings");
                for(int i=0;i<12;i++)Tick(true);
                check("Recovery never closes the settings screen",game.Page==Screen.Settings);
            }
            finally {UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
