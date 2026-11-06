using System.Linq;
using UnityEngine;
using U = Neonmoves.NeonUI;

namespace Neonmoves
{
    public partial class NeonGame
    {
        void Header(string section="")
        {
            U.Logo(R(80,42,224,28));
            if(!string.IsNullOrEmpty(section))Text(330,44,720,29,"/   "+section,16,U.Muted);
            if(Page!=Screen.Menu && Page!=Screen.Start && U.Button(R(1370,34,150,44),"Esc   Назад"))Back();
            U.Rule(80,104,1440);
        }
        void Footer()
        {
            Text(80,858,800,26,Page==Screen.Menu?"↑ ↓  Выбор     Enter  Подтвердить":"Esc  Назад",12,U.Muted);
            U.Text(R(1250,858,270,26),"NEONMOVES   /   "+Application.version,11,U.Muted,false,TextAnchor.UpperRight);
        }
        void PageBackground()
        {U.Fill(R(0,0,1600,900),new Color(.025f,.03f,.04f,.97f));}
        void LeftShade()
        {U.Fill(R(0,0,480,900),new Color(.025f,.03f,.04f,.97f));U.Shade(R(480,0,550,900));}
        void MenuAction(int option)
        {
            menuSelection=option;
            switch(option) {
                case 0:BeginSetup();break;
                case 1:Page=Screen.Records;recordsScroll=Vector2.zero;break;
                case 2:helpReturn=Screen.Menu;helpCharacter=character;Page=Screen.Help;break;
                case 3:OpenSettings();break;
                case 4:Application.Quit();break;
            }
        }
        void DrawStart()
        {
            LeftShade();
            U.Logo(R(80,220,900,112));
            U.Fill(R(84,340,60,4),U.Cyan);
            if(U.Button(R(80,476,360,64),"Начать",true))GoMenu();
            Text(80,562,600,30,"Enter / пробел",15,U.Muted);
            Text(80,858,500,26,Application.version+"   ·   Windows",12,U.Muted);
        }
        void DrawMenu()
        {
            LeftShade();
            U.Logo(R(80,87,800,100));
            U.Fill(R(84,193,52,4),U.Cyan);
            string[] entries={"Играть","Результаты","Управление","Настройки","Выход"};
            for(int i=0;i<entries.Length;i++)if(U.Nav(R(80,284+i*76,430,66),entries[i],menuSelection==i))MenuAction(i);
            U.Rule(80,729,430);
            Text(80,752,430,30,"SIGNAL / SKYLINE / OVERDRIVE",14,U.White);
            Text(80,789,430,28,"3 этапа  ·  36 движений",14,U.Muted);
            Text(1190,754,310,36,character==0?"NOVA":"PULSE",22,U.White,true);
            Footer();
        }
        void DrawSetup()
        {
            LeftShade();Header("Новая игра");
            Text(80,151,720,66,"Подготовка",42,U.White,true);
            Text(80,255,620,28,"ПЕРСОНАЖ",13,U.Muted,true);
            if(U.Button(R(80,300,264,60),"Nova",false,character==0))character=0;
            if(U.Button(R(358,300,264,60),"Pulse",false,character==1))character=1;
            Text(80,373,590,28,character==0?"Широкие позы · плавные переходы":"Угловатые позы · резкие акценты",14,U.Muted);
            Text(80,407,620,28,"УПРАВЛЕНИЕ",13,U.Muted,true);
            if(U.Button(R(80,452,264,60),"Камера",false,cameraMode))cameraMode=true;
            if(U.Button(R(358,452,264,60),"Клавиатура",false,!cameraMode))cameraMode=false;
            Text(80,538,550,92,cameraMode?"Повторяй положения рук и жесты пальцами.\nМожно играть сидя. Ноги не участвуют.":"Тренировочный режим.\nУдерживай указанную клавишу от 1 до 6.",18,U.White);
            U.Rule(80,658,542);
            Text(80,680,550,58,"3 этапа по 12 движений. Для перехода нужны\n7 попаданий и энергия выше нуля.",15,U.Muted);
            if(U.Button(R(80,762,542,62),cameraMode?"Проверить камеру":"Начать",true))Begin();
            Text(1090,739,420,56,character==0?"NOVA":"PULSE",38,U.White,true);
            Footer();
        }
        void DrawCalibration()
        {
            PageBackground();Header("Новая игра");
            Text(80,146,1000,66,"Проверка камеры",42,U.White,true);
            U.Fill(R(80,249,820,555),Color.black);
            if(AI.Preview)GUI.DrawTexture(R(120,249,740,555),AI.Preview,ScaleMode.ScaleToFit);
            else U.Text(R(165,400,650,180),CameraMessage(),22,U.Muted,false,TextAnchor.MiddleCenter,true);
            Text(960,250,560,40,AI.CameraReady?"Камера готова":AI.State.cameraStatus=="error"?"Камера недоступна":"Подключение камеры",26,AI.CameraReady?U.Cyan:U.White,true);
            U.Bar(R(960,313,560,4),cameraStart.Progress,U.Cyan);
            Text(960,350,550,100,"Скажи «начать» или «старт»\nили удержи лайк 1 секунду.",23,U.White,true);
            Text(960,468,550,80,"После запуска будет отсчёт 3 секунды.\nДля игры покажи плечи, локти и обе кисти.\nМожно играть сидя.",17,U.White);
            if(U.Button(R(960,566,192,44),calibrationVoice?"Голос: вкл":"Голос: выкл",false,calibrationVoice)){calibrationVoice=!calibrationVoice;Devices(true);}
            Text(1172,568,348,44,!calibrationVoice?"Запуск кнопкой или лайком":AI.State.voiceStatus=="ready"?"Слушаю команды запуска":AI.State.voiceStatus=="error"?"Микрофон недоступен. Используй лайк.":"Подключение микрофона…",14,U.Muted);
            Text(960,620,550,40,CameraMessage(),13,U.Muted);
            if(U.Button(R(960,668,268,52),"Камера № "+Save.cameraIndex+"   ›")){Save.cameraIndex=(Save.cameraIndex+1)%5;cameraStart.Reset();Devices(true);}
            if(U.Button(R(1244,668,276,52),"Подключить заново")){cameraStart.Reset();AI.RetryCamera();}
            GUI.enabled=CameraStartGate.CanStart(Page,AI.CameraReady);
            if(U.Button(R(960,744,560,60),"Начать",true))StartRun();GUI.enabled=true;
            Footer();
        }
        string CameraMessage()
        {
            if(!string.IsNullOrEmpty(AI.Error))return AI.Error;
            if(!AI.Connected)return "Подключение к службе распознавания…";
            if(AI.State.cameraStatus=="error")return AI.State.cameraError;
            if(AI.State.cameraStatus=="loading")return "Камера загружается…";
            if(AI.State.cameraStatus!="ready")return "Подключение камеры… При необходимости нажми «Подключить заново».";
            if(AI.State.lowLight)return "Кадр слишком тёмный. Проверь шторку камеры и освещение рук.";
            if(!AI.Fresh)return "Покажи плечи, локти и обе кисти. Не перекрывай пальцы и держи руки ближе к камере.";
            return "Слева: "+(GameRules.Gesture(AI.State.leftGesture)?.name??"жест неясен")+" · Справа: "+(GameRules.Gesture(AI.State.rightGesture)?.name??"жест неясен");
        }
        void DrawGame()
        {
            if(Session==null)return;
            U.Fill(R(0,0,1600,139),new Color(.025f,.03f,.04f,.9f));
            Text(64,29,500,24,"ЭТАП "+(Session.Stage+1)+" / 3",12,U.Muted,true);
            Text(64,59,540,45,GameRules.StageNames[Session.Stage],29,U.White,true);
            Text(64,112,400,20,cameraMode?"Камера":"Клавиатура · тренировка",12,U.Muted);
            U.Text(R(600,24,400,48),Session.Score.ToString("N0"),36,U.White,true,TextAnchor.MiddleCenter);
            U.Text(R(600,79,400,28),"КОМБО  "+Session.Combo,15,Session.Combo>0?U.Cyan:U.Muted,true,TextAnchor.MiddleCenter);
            Text(1150,39,224,24,"ЭНЕРГИЯ  "+Mathf.CeilToInt(Session.Energy),12,U.Muted,true);
            U.Bar(R(1150,79,220,5),Session.Energy/100,Session.Energy<35?U.Pink:U.Cyan);
            if(U.Button(R(1433,40,88,56),"Ⅱ"))Pause(false);
            if(cameraMode) {
                U.Fill(R(1252,177,268,211),new Color(.025f,.03f,.04f,.86f));
                if(AI.Preview)GUI.DrawTexture(R(1260,185,252,189),AI.Preview,ScaleMode.ScaleToFit);
                Text(1252,399,280,28,AI.Fresh?"Поза в кадре":"Поза не видна",13,AI.Fresh?U.Muted:U.Pink);
            }
            // A single cue strip keeps the dancer unobstructed and puts timing beside the move.
            U.Fill(R(0,656,1600,244),new Color(.025f,.03f,.04f,.95f));
            U.Fill(R(64,688,3,104),Session.IsBeat?U.Pink:U.Cyan);
            U.Pose(R(85,673,145,126),Session.Current,U.White);
            Text(256,677,570,25,Session.IsBeat?"ПОПАДИ В БИТ":"УДЕРЖИ ПОЗУ",12,Session.IsBeat?U.Pink:U.Cyan,true);
            Text(256,708,470,43,GameRules.PoseNames[Session.Current],26,U.White,true);
            Text(256,758,470,50,cameraMode?GameRules.PoseHints[Session.Current]:"Удерживай клавишу "+(GameRules.KeyForPose(Session.Current)+1),15,U.Muted);
            Text(740,677,150,25,"КИСТИ",11,U.Muted,true);
            U.Hand(R(740,711,62,87),GameRules.Poses[Session.Current].hands[0],U.White,true);
            U.Hand(R(821,711,62,87),GameRules.Poses[Session.Current].hands[1],U.White);
            Text(918,682,235,24,"ТОЧНОСТЬ",11,U.Muted,true);
            Text(918,716,235,42,Mathf.RoundToInt(liveAccuracy*100)+"%",28,U.White,true);
            U.Bar(R(918,772,164,4),liveAccuracy,U.Cyan);
            if(Session.Move<11) {
                U.Pose(R(1190,696,91,90),Session.Next,U.Muted);
                Text(1304,682,250,25,"ДАЛЬШЕ",11,U.Muted,true);
                Text(1304,720,235,55,GameRules.PoseNames[Session.Next],16,U.White,true);
            } else Text(1232,720,290,50,"Последнее движение",16,U.Muted);
            float remain=Mathf.Max(0,Session.Duration-Session.Elapsed);
            U.Bar(R(64,815,1364,7),Session.Elapsed/Session.Duration,U.Cyan);
            if(Session.IsBeat)U.Fill(R(64+1364*(1-.55f/Session.Duration),810,1364*.55f/Session.Duration,17),new Color(.91f,.46f,.41f,.65f));
            Text(1450,800,100,34,remain.ToString("0.0")+" с",17,U.White);
            Text(64,853,420,26,"Движение "+Mathf.Min(Session.Move+1,12)+" / 12",13,U.Muted);
            for(int i=0;i<12;i++)U.Fill(R(595+i*34,864,24,3),i<Session.Move?U.Cyan:i==Session.Move?U.White:U.LineColor);
            U.Text(R(1130,851,390,28),Save.voice?(AI.State.voiceStatus=="ready"?"Голос включён  ·  Esc Пауза":"Голос недоступен  ·  Esc Пауза"):"Пробел / Esc  Пауза",12,U.Muted,false,TextAnchor.MiddleRight);
            if(countdown>0)U.Text(R(490,213,620,170),Mathf.CeilToInt(countdown).ToString(),118,U.White,true,TextAnchor.MiddleCenter);
            else if(feedbackTime>0)U.Text(R(490,175,620,83),feedback,45,feedback=="MISS"?U.Pink:U.Cyan,true,TextAnchor.MiddleCenter);
            if(missingTime>.8f) {
                U.Fill(R(440,465,720,66),U.Ink);
                U.Text(R(440,465,720,66),"Покажи обе кисти · пауза через "+Mathf.CeilToInt(Mathf.Max(0,TrackingPauseDelay-missingTime))+" с",19,U.White,true,TextAnchor.MiddleCenter);
            }
        }
        void DrawPause()
        {
            U.Fill(R(0,0,1600,900),new Color(.025f,.03f,.04f,.94f));
            Text(520,188,600,70,"Пауза",46,U.White,true);
            Text(520,290,560,95,autoPaused?(AI.Fresh?"Вижу тебя. Готовлю продолжение…":"Верни плечи, локти и обе кисти в кадр.\nИгра продолжится автоматически через отсчёт."):GameRules.StageNames[Session.Stage]+"  /  Этап "+(Session.Stage+1)+" из 3",19,U.Muted);
            if(autoPaused)U.Bar(R(520,383,560,3),trackingRecovery.Progress,U.Cyan);
            GUI.enabled=!cameraMode||AI.Fresh;
            if(U.Button(R(520,410,560,64),"Продолжить",true))Resume();GUI.enabled=true;
            if(U.Button(R(520,494,560,58),"Настройки"))OpenSettings();
            if(U.Button(R(520,572,560,58),"Главное меню")){started=false;GoMenu();}
            Text(520,684,560,28,"Пробел / Esc  Продолжить",13,U.Muted);
        }
        void DrawStageClear()
        {
            LeftShade();Header("Результат этапа");
            Text(80,186,900,32,"ЭТАП "+(Session.Stage+1)+" ПРОЙДЕН",14,U.Cyan,true);
            Text(80,242,1000,82,GameRules.StageNames[Session.Stage],58,U.White,true);
            U.Rule(80,376,695);
            Stat(80,411,"ПОПАДАНИЯ",Session.StageHits+" / 12",210);
            Stat(330,411,"ТОЧНОСТЬ",Mathf.RoundToInt(Session.StageAccuracy/12*100)+"%",210);
            Stat(580,411,"ОЧКИ",Session.Score.ToString("N0"),210);
            float adapted=GameRules.Adapt(Session.Difficulty,Session.StageAccuracy/12);
            Text(80,570,700,85,Session.Stage==2?"Все три этапа пройдены.":adapted>Session.Difficulty?"На следующем этапе темп немного увеличится.":adapted<Session.Difficulty?"На следующем этапе будет больше времени на позу.":"Темп следующего этапа остаётся прежним.",19,U.Muted);
            if(U.Button(R(80,726,480,64),Session.Stage==2?"Итоговый результат":"Следующий этап",true))NextStage();Footer();
        }
        void Stat(float x,float y,string label,string value,float width)
        {Text(x,y,width,24,label,11,U.Muted,true);Text(x,y+35,width,50,value,32,U.White,true);}
        void DrawResults()
        {
            PageBackground();Header("Результат игры");if(finalResult==null)return;
            Text(80,174,1150,76,finalResult.won?"Турнир пройден":"Попытка завершена",48,U.White,true);
            Text(80,275,1110,60,finalResult.won?"Все три этапа завершены.":"Для перехода нужны 7 попаданий на этапе и энергия выше нуля.",20,U.Muted);
            U.Rule(80,381,1440);
            Stat(80,421,"ОЧКИ",finalResult.score.ToString("N0"),240);
            Stat(368,421,"ТОЧНОСТЬ",Mathf.RoundToInt(finalResult.accuracy*100)+"%",220);
            Stat(656,421,"ЛУЧШЕЕ КОМБО",finalResult.maxCombo.ToString(),230);
            Stat(944,421,"ЭТАПЫ",finalResult.stages+" / 3",200);
            Text(1280,421,240,24,"РАНГ",11,U.Muted,true);
            Text(1280,448,240,86,finalResult.won?finalResult.grade:"—",65,finalResult.won?U.Cyan:U.Muted,true);
            U.Rule(80,568,1440);
            Text(80,600,1400,70,(finalResult.mode=="camera"?"Камера":"Клавиатура")+"   /   "+finalResult.character+"\n"+(string.IsNullOrEmpty(notice)?"Результат сохранён.":notice),17,U.Muted);
            if(U.Button(R(80,737,360,64),"Повторить",true))Begin();
            if(U.Button(R(460,737,280,64),"Результаты"))Page=Screen.Records;
            if(U.Button(R(760,737,280,64),"Главное меню"))GoMenu();Footer();
        }
        void DrawSettings()
        {
            PageBackground();Header("Настройки");Text(80,151,1200,65,"Настройки",42,U.White,true);
            Text(80,265,620,28,"ЗВУК И ЭКРАН",13,U.Muted,true);U.Rule(80,310,650);
            Text(80,341,650,32,"Громкость",19,U.White);
            if(U.Button(R(80,397,52,48),"−")){Save.volume=Mathf.Max(0,Save.volume-.1f);music.SetVolume(Save.volume);}
            U.Bar(R(157,418,390,5),Save.volume,U.Cyan);
            Text(568,406,110,35,Mathf.RoundToInt(Save.volume*100)+"%",19,U.White);
            if(U.Button(R(678,397,52,48),"+")){Save.volume=Mathf.Min(1,Save.volume+.1f);music.SetVolume(Save.volume);}
            U.Rule(80,489,650);Text(80,524,390,35,"Полный экран",19,U.White);
            GUI.enabled=!display.Switching;
            if(U.Button(R(510,514,220,52),Save.fullscreen?"Включён":"Выключен"))ToggleFullscreen();
            GUI.enabled=true;
            Text(80,589,650,30,UnityEngine.Screen.width+" × "+UnityEngine.Screen.height+(Save.fullscreen?"  ·  По размеру монитора":"  ·  Оконный режим"),15,U.Muted);
            Text(80,646,650,70,"Alt + Enter — полный экран.\nПробел / Esc — пауза.",17,U.Muted);
            if(U.Button(R(80,744,650,60),"Сохранить и вернуться",true)){SaveStore.Save(Save);Page=backPage;}
            Text(850,265,670,28,"КАМЕРА И МИКРОФОН",13,U.Muted,true);U.Rule(850,310,670);
            Text(850,341,365,35,"Устройство камеры",19,U.White);
            if(U.Button(R(1230,331,290,52),"Камера № "+Save.cameraIndex+"   ›")){Save.cameraIndex=(Save.cameraIndex+1)%5;Devices(backPage==Screen.Paused&&cameraMode);}
            Text(850,429,365,35,"Голосовые команды",19,U.White);
            if(U.Button(R(1230,419,290,52),Save.voice?"Включены":"Выключены",false,Save.voice)){Save.voice=!Save.voice;Devices(backPage==Screen.Paused&&cameraMode);}
            U.Rule(850,511,670);
            Text(850,543,670,75,"«пауза»  «продолжить»  «старт»\n«дальше»  «повтор»  «меню»",19,U.White);
            string status=!Save.voice?"Микрофон выключен.":AI.State.voiceStatus=="ready"?"Микрофон готов.":AI.State.voiceStatus=="error"?"Микрофон недоступен: "+AI.State.voiceError:"Загрузка распознавания речи…";
            Text(850,649,670,68,status,16,Save.voice?U.Cyan:U.Muted);
            Text(850,747,670,67,"Обработка на этом компьютере. Видео и аудио\nне записываются. Для голоса используй наушники.",15,U.Muted);Footer();
        }
        void DrawRecords()
        {
            PageBackground();Header("Результаты");Text(80,151,1000,65,"Результаты",42,U.White,true);
            if(U.Button(R(80,251,245,48),"Камера",false,recordsCamera))recordsCamera=true;
            if(U.Button(R(339,251,245,48),"Клавиатура",false,!recordsCamera))recordsCamera=false;
            Text(80,345,390,25,"ДАТА / ПЕРСОНАЖ",12,U.Muted,true);Text(474,345,150,25,"ОЧКИ",12,U.Muted,true);Text(660,345,160,25,"ТОЧНОСТЬ",12,U.Muted,true);Text(854,345,130,25,"РАНГ",12,U.Muted,true);
            U.Rule(80,386,925);
            var results=Save.results.Where(r=>r.mode==(recordsCamera?"camera":"keyboard")).OrderByDescending(r=>r.score).ToArray();
            if(results.Length==0)Text(80,456,900,90,"В этом режиме пока нет результатов.",22,U.Muted);
            recordsScroll=GUI.BeginScrollView(R(72,407,943,402),recordsScroll,R(0,0,918,Mathf.Max(402,results.Length*76)),false,false);
            for(int i=0;i<results.Length;i++) {
                var r=results[i];float y=i*76;
                Text(8,y+8,370,25,r.date+"  /  "+r.character,15,U.White);
                Text(8,y+38,370,24,r.won?"Турнир пройден":"Этапов: "+r.stages+" / 3",12,U.Muted);
                Text(402,y+17,170,40,r.score.ToString("N0"),22,U.White,true);Text(588,y+17,170,40,Mathf.RoundToInt(r.accuracy*100)+"%",22,U.White);Text(782,y+17,100,40,r.won?r.grade:"—",22,U.Cyan,true);U.Rule(8,y+73,905);
            }GUI.EndScrollView();
            Text(1080,261,440,30,"ДОСТИЖЕНИЯ",13,U.Muted,true);U.Rule(1080,311,440);
            for(int i=0;i<AchievementNames.Length;i++) {
                bool unlocked=Save.achievements.Contains(AchievementNames[i]);float y=345+i*91;
                U.Fill(R(1080,y+9,6,6),unlocked?U.Cyan:U.LineColor);
                Text(1105,y,415,30,AchievementNames[i],18,unlocked?U.White:U.Muted,true);
                Text(1105,y+34,415,42,AchievementHints[i],13,U.Muted);
            }Footer();
        }
        void DrawHelp()
        {
            PageBackground();Header("Управление");Text(80,151,1200,65,"Управление",42,U.White,true);
            if(U.Button(R(1080,159,212,48),"Nova",false,helpCharacter==0))helpCharacter=0;
            if(U.Button(R(1308,159,212,48),"Pulse",false,helpCharacter==1))helpCharacter=1;
            for(int i=0;i<6;i++) {
                float x=80+i*244;int pose=GameRules.PoseId(helpCharacter,i);
                Text(x,265,220,26,"КЛАВИША "+(i+1),12,U.Muted,true);
                U.Pose(R(x+22,322,176,130),pose,U.White);
                U.Hand(R(x+43,423,54,65),GameRules.Poses[pose].hands[0],U.Cyan,true);
                U.Hand(R(x+125,423,54,65),GameRules.Poses[pose].hands[1],U.Cyan);
                Text(x,497,222,28,GameRules.HandHint(pose),11,U.Muted);
                Text(x,534,222,43,GameRules.PoseNames[pose],15,U.White,true);
                U.Rule(x,584,220);
            }
            Text(80,601,690,30,"ПРАВИЛА",13,U.Cyan,true);
            Text(80,650,690,162,"Удерживай позу минимум 0,55 секунды.\nВ движениях «Бит» держи позу в выделенной зоне.\nПромах отнимает 15 энергии. Для перехода нужны\n7 попаданий. Пройди три этапа, чтобы победить.",18,U.White);
            Text(850,601,670,30,"КАМЕРА И КЛАВИАТУРА",13,U.Cyan,true);
            Text(850,650,670,162,"Камера: видны плечи, локти и обе кисти с пальцами.\nМожно играть сидя; ноги не участвуют.\nКлавиатура: удерживай одну клавишу от 1 до 6.\nПробел / Esc — пауза. Голос включается в настройках.",18,U.White);Footer();
        }
    }
}
