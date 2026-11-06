using UnityEngine;
using U = Neonmoves.NeonUI;

namespace Neonmoves
{
    public sealed class TransitionTiming
    {
        float elapsed,duration;
        public bool Active=>elapsed<duration;
        public float Blend=>duration<=0?1:Mathf.SmoothStep(0,1,elapsed/duration);
        public void Begin(bool pause){elapsed=0;duration=pause?.28f:.65f;}
        public void Advance(float delta){elapsed=Mathf.Min(duration,elapsed+Mathf.Max(0,delta));}
        public void Cancel(){elapsed=duration;}
    }

    public sealed class ScreenTransition : MonoBehaviour
    {
        readonly TransitionTiming timing=new TransitionTiming();
        Screen destination;
        public bool Active=>timing.Active;

        public void Begin(Screen from,Screen to)
        {
            destination=to;
            timing.Begin(from==Screen.Paused || to==Screen.Paused);
        }
        void Update()=>timing.Advance(Time.unscaledDeltaTime);
        string Title {
            get {
                switch(destination) {
                    case Screen.Menu:return "Главное меню";
                    case Screen.Setup:return "Новая игра";
                    case Screen.Calibration:return "Проверка камеры";
                    case Screen.Playing:return "На сцену";
                    case Screen.Paused:return "Пауза";
                    case Screen.StageClear:return "Этап завершён";
                    case Screen.Results:return "Итоги игры";
                    case Screen.Settings:return "Настройки";
                    case Screen.Records:return "Результаты";
                    case Screen.Help:return "Управление";
                    default:return "Neonmoves";
                }
            }
        }
        public void Draw()
        {
            // A separate opaque screen; no frame capture or overlapping pages.
            var matrix=GUI.matrix;GUI.matrix=Matrix4x4.identity;
            U.Fill(new Rect(0,0,UnityEngine.Screen.width,UnityEngine.Screen.height),U.Ink);
            GUI.matrix=matrix;
            U.Logo(new Rect(520,333,560,64),true);
            U.Text(new Rect(400,410,800,64),Title,36,U.White,true,TextAnchor.MiddleCenter);
            U.Bar(new Rect(580,525,440,3),timing.Blend,U.Cyan);
        }
    }
}
