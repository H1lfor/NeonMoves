using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neonmoves
{
    public enum Screen { Start, Menu, Setup, Calibration, Playing, Paused, StageClear, Results, Settings, Records, Help }
    [Serializable] public class PoseDefinition { public string name,hint; public float[] arms; public string[] hands; }
    [Serializable] public class GestureDefinition { public string id,name; public int[] fingers; }
    [Serializable] public class PoseCatalog { public PoseDefinition[] poses; public GestureDefinition[] gestures; }
    public static class GameRules
    {
        public const int PosesPerCharacter=6;
        static readonly PoseCatalog catalog=JsonUtility.FromJson<PoseCatalog>(Resources.Load<TextAsset>("PoseCatalog").text);
        public static readonly PoseDefinition[] Poses=catalog.poses;
        public static readonly GestureDefinition[] Gestures=catalog.gestures;
        public static GestureDefinition Gesture(string id)=>Array.Find(Gestures,g=>g.id==id);
        public static string HandHint(int pose)=>Gesture(Poses[pose].hands[0]).name+" / "+Gesture(Poses[pose].hands[1]).name;
        public static readonly string[] PoseNames=Array.ConvertAll(Poses,p=>p.name);
        public static readonly string[] PoseHints=Array.ConvertAll(Poses,p=>p.hint);
        public static int PoseCount=>Poses.Length;
        public static int PoseId(int character,int key)=>Mathf.Clamp(character,0,1)*PosesPerCharacter+key;
        public static int KeyForPose(int pose)=>pose%PosesPerCharacter;
        public static readonly string[] StageNames = { "SIGNAL", "SKYLINE", "OVERDRIVE" };
        public static readonly string[] StageDescriptions = { "Поймай свой первый сигнал", "Поднимись над городом", "Забери ночную сцену" };
        // Screen-left upper arm, screen-right upper arm, then forearms. Degrees above outward horizontal.
        public static readonly float[][] Angles=Array.ConvertAll(Poses,p=>p.arms);
        public static float Adapt(float difficulty, float accuracy)
        { return Mathf.Clamp(difficulty + (accuracy >= .87f ? .08f : accuracy < .62f ? -.10f : 0), .8f, 1.35f); }
        public static int Points(float accuracy, int combo, bool beat)
        { return Mathf.RoundToInt((100 + 200 * Mathf.Clamp01(accuracy)) * (1 + Mathf.Min(combo / 5, 4) * .25f) * (beat ? 1.2f : 1)); }
        public static int[] Sequence(int stage, int seed, int character=0)
        {
            var rng = new System.Random(seed + stage * 137 + character * 71);
            int[] moves = new int[12];
            for (int i = 0; i < moves.Length; i++) {
                int count=stage==0 && i<4?4:PosesPerCharacter;
                int n=rng.Next(count);
                if(i>0 && PoseId(character,n)==moves[i-1])n=(n+1)%count;
                moves[i]=PoseId(character,n);
            }
            return moves;
        }
        public static string Grade(float accuracy, bool won)
        { return !won ? "RETRY" : accuracy >= .92f ? "S" : accuracy >= .82f ? "A" : accuracy >= .68f ? "B" : "C"; }
    }
    [Serializable] public class RunResult
    {
        public string date, character, mode, grade;
        public int score, maxCombo, stages;
        public float accuracy;
        public bool won;
    }
    [Serializable] public class SaveData
    {
        public int version = 1;
        public float volume = .55f;
        public bool voice, fullscreen;
        public int cameraIndex;
        public List<RunResult> results = new List<RunResult>();
        public List<string> achievements = new List<string>();
    }
    public static class SaveStore
    {
        public static string FilePath => System.IO.Path.Combine(Application.persistentDataPath, "neonmoves-save.json");
        public static SaveData Load()
        {
            try {
                if (!System.IO.File.Exists(FilePath)) return new SaveData();
                var data = JsonUtility.FromJson<SaveData>(System.IO.File.ReadAllText(FilePath));
                if (data == null) return new SaveData();
                data.results ??= new List<RunResult>(); data.achievements ??= new List<string>();
                data.volume = Mathf.Clamp01(data.volume); data.cameraIndex = Mathf.Clamp(data.cameraIndex, 0, 4);
                return data;
            } catch (Exception e) { Debug.LogWarning("Save load: " + e.Message); return new SaveData(); }
        }
        public static bool Save(SaveData data)
        {
            try {
                System.IO.Directory.CreateDirectory(Application.persistentDataPath);
                var temp = FilePath + ".tmp";
                System.IO.File.WriteAllText(temp, JsonUtility.ToJson(data, true));
                if (System.IO.File.Exists(FilePath)) System.IO.File.Replace(temp, FilePath, FilePath + ".bak");
                else System.IO.File.Move(temp, FilePath);
                return true;
            } catch (Exception e) { Debug.LogError("Save write: " + e.Message); return false; }
        }
    }
}
