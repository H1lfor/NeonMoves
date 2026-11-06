using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Neonmoves.Editor
{
    public static class RuleTests
    {
        static readonly List<string> results=new List<string>();
        static void Check(string name,bool condition){results.Add((condition?"PASS ":"FAIL ")+name);if(!condition)throw new Exception("Test failed: "+name);}
        static void CompleteMove(DanceSession session,float accuracy){int move=session.Move;for(int i=0;i<1000&&session.Move==move&&!session.Lost;i++)session.Tick(.02f,accuracy);}
        [UnityEditor.MenuItem("Neonmoves/Run rule tests")]
        public static void Run()
        {
            results.Clear();
            Check("Camera connection is sufficient to start calibration",CameraStartGate.CanStart(Screen.Calibration,true));
            Check("Disconnected camera and other pages cannot start a run",!CameraStartGate.CanStart(Screen.Calibration,false) && !CameraStartGate.CanStart(Screen.Playing,true) && !CameraStartGate.CanStart(Screen.Menu,true));
            var gate=new CameraStartGate();bool triggered=false;
            for(int i=0;i<=12;i++)triggered |= gate.Observe(true,true,i,i*.1f);
            Check("One sustained thumb starts after one second of fresh frames",triggered);
            Check("Sustained thumb cannot start twice",!gate.Observe(true,true,13,1.3f));
            gate.Reset();gate.Observe(true,true,1,0);
            for(int i=1;i<15;i++)gate.Observe(true,true,1,i*.1f);
            Check("Repeated stale camera frame never completes thumb hold",gate.Progress==0 && !gate.Observe(true,true,2,1.5f));
            gate.Reset();gate.Observe(true,true,1,0);gate.Observe(true,true,2,.2f);gate.Observe(true,false,3,.3f);
            Check("Releasing thumb resets hold",gate.Progress==0 && !gate.Observe(true,true,4,.4f));
            gate.Reset();gate.Observe(true,true,1,0);gate.Observe(true,true,2,.2f);
            Check("Large gap between frames resets hold",!gate.Observe(true,true,3,1.2f) && gate.Progress==0);
            gate.Observe(false,true,4,1.3f);
            Check("Camera disconnect resets hold",gate.Progress==0 && !gate.Observe(false,true,5,4));
            var recovery=new TrackingRecoveryGate();triggered=false;
            for(int i=0;i<=8;i++)triggered |= recovery.Observe(Screen.Paused,true,true,true,true,i,i*.1f);
            Check("Stable tracked hands release an automatic pause",triggered);
            Check("A recovered pose triggers continuation only once",!recovery.Observe(Screen.Paused,true,true,true,true,9,.9f));
            foreach(string reason in new[]{"manual","keyboard","unfocused","settings","missing"}) {
                recovery.Reset();triggered=false;
                for(int i=0;i<20;i++)triggered |= recovery.Observe(reason=="settings"?Screen.Settings:Screen.Paused,reason!="manual",reason!="keyboard",reason!="unfocused",reason!="missing",i,i*.1f);
                Check("Recovery does not resume when "+reason,!triggered && recovery.Progress==0);
            }
            recovery.Reset();
            for(int i=0;i<5;i++)recovery.Observe(Screen.Paused,true,true,true,true,i,i*.1f);
            recovery.Observe(Screen.Paused,true,true,true,false,5,.5f);
            Check("Pose disappearing during recovery resets progress",recovery.Progress==0 && !recovery.Observe(Screen.Paused,true,true,true,true,6,.6f));
            recovery.Reset();recovery.Observe(Screen.Paused,true,true,true,true,1,0);triggered=false;
            for(int i=1;i<20;i++)triggered |= recovery.Observe(Screen.Paused,true,true,true,true,1,i*.1f);
            Check("Stale repeated frames cannot automatically resume",!triggered && recovery.Progress==0);
            recovery.Reset();recovery.Observe(Screen.Paused,true,true,true,true,1,0);
            Check("Tracking recovery restarts after a long frame gap",!recovery.Observe(Screen.Paused,true,true,true,true,2,.8f) && recovery.Progress==0);
            var transition=new TransitionTiming();
            Check("Idle transition leaves input available",!transition.Active && transition.Blend==1);
            transition.Begin(false);transition.Advance(.325f);
            Check("Navigation transition blends smoothly at halfway",transition.Active && Mathf.Approximately(transition.Blend,.5f));
            transition.Advance(1);
            Check("Finished transition restores input and clears overlay",!transition.Active && transition.Blend==1);
            transition.Begin(true);transition.Advance(.29f);
            Check("Pause transition finishes faster than navigation",!transition.Active);
            transition.Begin(false);transition.Cancel();
            Check("Resize can cancel a transition cleanly",!transition.Active && transition.Blend==1);
            PauseFlowTests.Run(Check);
            Check("Adaptive difficulty rises after high accuracy",GameRules.Adapt(1,.95f)>1);
            Check("Adaptive difficulty falls after low accuracy",GameRules.Adapt(1,.4f)<1);
            Check("Adaptive difficulty clamps at upper bound",Mathf.Approximately(GameRules.Adapt(1.35f,1),1.35f));
            Check("Adaptive difficulty clamps at lower bound",Mathf.Approximately(GameRules.Adapt(.8f,0),.8f));
            Check("Combo increases awarded points",GameRules.Points(1,10,false)>GameRules.Points(1,0,false));
            Check("Beat challenge awards bonus",GameRules.Points(1,0,true)>GameRules.Points(1,0,false));
            var sequence=GameRules.Sequence(2,3);bool valid=sequence.Length==12;
            for(int i=0;i<sequence.Length;i++)valid &= sequence[i]>=0&&sequence[i]<6&&(i==0||sequence[i]!=sequence[i-1]);
            Check("Sequence has twelve valid nonrepeating moves",valid);
            Check("Shared catalog contains two complete six-pose sets",GameRules.PoseCount==12);
            bool gestures=GameRules.Gestures.Length==6;
            foreach(var pose in GameRules.Poses) {
                gestures &= pose.hands.Length==2;
                foreach(string hand in pose.hands)gestures &= GameRules.Gesture(hand)!=null && GameRules.Gesture(hand).fingers.Length==5;
            }
            Check("All poses have two supported five-finger gestures",gestures);
            Check("Former low poses are replaced by distinct hand gestures",GameRules.Poses[5].hands[0]=="Victory" && GameRules.Poses[11].hands[0]=="ILoveYou");
            bool keys=true;for(int i=0;i<6;i++)keys &= GameRules.KeyForPose(GameRules.PoseId(1,i))==i;
            Check("Pulse uses keyboard keys one through six",keys);
            var pulse=new DanceSession(41,1);bool pulseOnly=true;
            for(int s=0;s<3;s++) {
                foreach(int pose in pulse.Moves)pulseOnly &= pose>=6 && pose<12;
                for(int m=0;m<12;m++)CompleteMove(pulse,1);
                if(s<2)pulse.NextStage();
            }
            Check("Pulse keeps its own choreography through all three stages",pulseOnly && pulse.Cleared==3 && pulse.Hits==36);
            var failed=new DanceSession();for(int i=0;i<7;i++)CompleteMove(failed,0);
            Check("Seven consecutive misses exhaust energy",failed.Lost&&failed.Energy==0);
            int attempts=failed.Attempts;failed.Tick(.1f,1);Check("Loss freezes session",failed.Attempts==attempts);
            var hold=new DanceSession();hold.Tick(.1f,1);CompleteMove(hold,0);Check("Brief pose cannot pass hold challenge",hold.Hits==0);
            var success=new DanceSession();CompleteMove(success,1);Check("Sustained pose earns perfect",success.Hits==1&&success.LastJudgement=="PERFECT");
            CompleteMove(success,0);Check("Miss resets combo",success.Combo==0);
            var run=new DanceSession();int beatCount=0;
            for(int s=0;s<3;s++) {
                for(int m=0;m<12;m++){if(run.IsBeat)beatCount++;CompleteMove(run,1);}
                Check("Stage "+s+" completes after twelve hits",run.StageDone&&!run.Lost&&run.StageHits==12);
                if(s<2)run.NextStage();
            }
            Check("Full tournament completes all three stages",run.Cleared==3&&run.Attempts==36&&run.Hits==36);
            Check("Both challenge types exercised",beatCount==8);
            Check("Full accuracy is one",Mathf.Approximately(run.Accuracy,1));
            Check("Three-stage progression increases difficulty",run.Difficulty>1);
            var beat=new DanceSession();for(int m=0;m<12;m++)CompleteMove(beat,1);beat.NextStage();CompleteMove(beat,1);CompleteMove(beat,1);
            int before=beat.Hits;while(beat.Elapsed<beat.Duration-.65f)beat.Tick(.02f,1);CompleteMove(beat,0);
            Check("Early pose misses beat target",beat.Hits==before);
            var pause=new DanceSession();pause.Tick(0,1);Check("Zero time cannot advance session",pause.Elapsed==0&&pause.Score==0);
            Check("Loss cannot receive winning grade",GameRules.Grade(1,false)=="RETRY");
            Check("Perfect win receives S",GameRules.Grade(1,true)=="S");
            Directory.CreateDirectory("Docs/Testing");File.WriteAllLines("Docs/Testing/unity-rule-tests.txt",results);
            Debug.Log("NEONMOVES rule tests: "+results.Count+" passed");
        }
    }
}
