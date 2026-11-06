using UnityEngine;

namespace Neonmoves
{
    public class DanceSession
    {
        public int Stage, Move, Score, Combo, MaxCombo, Hits, Attempts, StageHits, Cleared;
        public float Energy=100, Difficulty=1, Elapsed, AccuracySum, StageAccuracy, Hold, Best;
        public bool StageDone, Lost;
        public int[] Moves;
        public float BeatHold;
        public int Current => Moves[Mathf.Min(Move,Moves.Length-1)];
        public int Next => Moves[Mathf.Min(Move+1,Moves.Length-1)];
        public bool IsBeat => Stage>0 && Move%3==2;
        public float Duration => (60f/112*(Stage==0?8:6))/((1+Stage*.06f)*Difficulty);
        public float Accuracy => Attempts>0?AccuracySum/Attempts:0;
        public float RequiredHold => .55f;
        public string LastJudgement="";
        public readonly int Character;
        public DanceSession(int seed=19,int character=0){ Character=Mathf.Clamp(character,0,1);Moves=GameRules.Sequence(0,seed,Character);Seed=seed; }
        readonly int Seed;
        public bool Tick(float dt,float accuracy)
        {
            if(StageDone||Lost||dt<=0)return false;
            dt=Mathf.Min(dt,.1f); Elapsed+=dt; accuracy=Mathf.Clamp01(accuracy);
            if(IsBeat) {
                if(Elapsed>=Duration-.55f && accuracy>=.72f){BeatHold+=dt; if(BeatHold>=.12f)Best=Mathf.Max(Best,accuracy);} else BeatHold=0;
            } else {
                Hold=accuracy>=.72f?Hold+dt:0;
                if(Hold>=RequiredHold)Best=Mathf.Max(Best,accuracy);
            }
            if(Elapsed<Duration)return false;
            Attempts++; StageAccuracy+=Best; AccuracySum+=Best;
            if(Best>=.72f) {
                Hits++; StageHits++; Combo++; MaxCombo=Mathf.Max(MaxCombo,Combo); Energy=Mathf.Min(100,Energy+3);
                Score+=GameRules.Points(Best,Combo,IsBeat); LastJudgement=Best>=.90f?"PERFECT":"GOOD";
            } else { Combo=0; Energy=Mathf.Max(0,Energy-15); LastJudgement="MISS"; }
            if(Energy<=0)Lost=true;
            Move++; Elapsed=0; Hold=0; Best=0; BeatHold=0;
            if(Move>=Moves.Length) {
                StageDone=true;
                if(StageHits<7)Lost=true;
                if(!Lost)Cleared=Stage+1;
            }
            return true;
        }
        public void NextStage()
        {
            if(!StageDone||Lost||Stage>=2)return;
            Difficulty=GameRules.Adapt(Difficulty,StageAccuracy/12);
            Stage++; Move=0; StageHits=0; StageAccuracy=0; StageDone=false; Elapsed=0; Hold=0; Best=0; BeatHold=0;
            Energy=Mathf.Min(100,Energy+20); Moves=GameRules.Sequence(Stage,Seed,Character);
        }
    }
}
