//#define DEBUG
#define GameDEBUG
//#define TimeEchox
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
namespace GreedSnake
{
    partial class Program
    {
        static Param param;

        static void Main(string[] args)
        {
            //初始化參數
            Initialization();
            //產生空白遊戲場景
            ShowEmptyLayout();

            //生成100個障礙物
            Point Middle = new Point(param.LayoutSize.Width / 2, param.LayoutSize.Height / 2);
            Rectangle ClearArea = new Rectangle(Middle.X-2,Middle.Y-2, 5, 5);
            BoundSpawn(ClearArea, 100);
            //洞洞分析演算法
            FillHole();
            //初始化貪吃蛆的身體
            SnakeInitialization(Middle, Direct.Top, 30);
            param.Direct = Direct.Down;
            DrawSnake();
            //產生兩對黑洞
            SpawnBugHoleAction();
            SpawnBugHoleAction();
            //初始化遊戲畫面更新用執行序,每間隔一定時間會觸發GameClock事件
            Clock = new Thread(GameMainCheckProcedure);
            //GameTrigger用來產生新的食物(星星),管理貪吃蛆前進和咬到自己
            GameClock += GameTrigger;

            //Clock開始運作,遊戲開始
            Clock.Start();

            //主執行序進入一個無限迴圈,負責接受鍵盤的輸入
            while (true)
                KeyboardHandle();
        }
        /// <summary>
        /// 在遊戲剛開始時執行此函式,針對遊戲參數初始化
        /// </summary>
        static void Initialization()
        {
            Console.Title = "貪吃蛆";
            Console.CursorVisible = true;

            param = new Param(
                (Console.WindowHeight) - 1,
                (Console.WindowWidth / 2) - 1,
                3,
                300,
                10
                );
        }

        #region Main Menu

        #endregion
        #region GameMethod

        #region GameProcess
        private static readonly object PrintLock = new object();
        static Thread Clock;
        /// <summary>
        /// 遊戲FPS正在一秒中的第幾次運作,以1秒為1個循環,循環的操作由Thread經手
        /// </summary>
        private static int SecondPeriod;
        private static bool TimerShutDown = false;
        private static void GameMainCheckProcedure()
        {
#if TimeEcho
            DateTime Echo;
#endif
            while (!TimerShutDown)
            {
#if TimeEcho
                Echo = DateTime.Now;
#endif
                Thread.Sleep(Param.GameCheckSec / Param.Fixed_FPS);
#if TimeEcho
                TimeSpan ts = DateTime.Now - Echo;
                System.Diagnostics.Debug.WriteLine("Main Game Thread Sleep {0}ms", ts.TotalMilliseconds);
#endif
                SecondPeriod = (SecondPeriod + 1) % (Param.Fixed_FPS);
                //Run
                if (GameClock != null)
                    GameClock(null, new EventArgs());
                SetCursorPosition(0, param.GameHeight);
            }
        }
        private static EventHandler<EventArgs> GameClock;
        private static bool Pause;

        static void GameTrigger(object sender, EventArgs e)
        {
            if (SecondPeriod == 0 && !Pause)
            {
                SpawnPacAction();
                SnakeMoveAction();
                SnakeInjureAction();
            }
            /*
            if(RedrawProcedure == null)
                RedrawProcedure = new Thread(RedrawObjectAsync);
            else if (RedrawProcedure.ThreadState == ThreadState.Unstarted)
                RedrawProcedure.Start();
            else if(RedrawProcedure.ThreadState == ThreadState.Stopped)
            {
                RedrawProcedure = new Thread(RedrawObjectAsync);
                RedrawProcedure.Start();
            }*/
            RefreshUI();

        }

        #endregion
        //SYSTEM
        static DateTime lastCheck;
        static void RefreshUI()
        {
#if DEBUG
            lock (PrintLock)
            {
                SetCursorPosition(0, param.GameHeight);
                string Debug_Info =
                    String.Format("Dir={0} , SwDir={1}     ", param.Direct, param.SwitchDirect);
                Console.Write(Debug_Info);
            }
#endif
            lock (PrintLock)
            {
                SetCursorPosition(0, param.GameHeight);
                Console.Write(new string(' ', param.GameWidth));
                if (Pause)
                {
                    SetCursorPosition(0, param.GameHeight);
                    Console.Write("暫停....（按Ｐ繼續）");
                }
            }
            lock (PrintLock)
            {
                DateTime now = DateTime.Now;
                if (lastCheck != null)
                {
                    TimeSpan ts = now - lastCheck;
                    SetCursorPosition(22, param.GameHeight);
                    // Console.Write("LF {1}.{0,3}   ", ts.Milliseconds,ts.Seconds);
                }
                lastCheck = now;
            }
        }
        static void MapAnalyze(MapAnalyzeMode mode)
        {
            StringBuilder sb = new StringBuilder();

            //Build Map
            bool[,] map = new bool[param.LayoutSize.Width, param.LayoutSize.Height];
            for (int i = 0; i < param.LayoutSize.Height; i++)
            {
                for (int j = 0; j < param.LayoutSize.Width; j++)
                {
                    Point p = new Point(j, i);
                    map[j, i] = MovingAvailable(p, false, true);

                    //Analyze Dead Cornor
                    int UnimpededBlock = 0;
                    for (int b = 0; b < 4; b++)
                    {
                        Point np = Param.DirectionCond[b] + p;
                        if (MovingAvailable(np, false, true))
                            UnimpededBlock++;
                    }
                    if (UnimpededBlock <= 1 && !param.Bound[p.X, p.Y])
                    {
                        BlockSpawn(p);
                        j -= j <= 1 ? 0 : 2;
                        i -= i <= 1 ? 0 : 2;

                    }
                }
            }

            //Analyze Moving Available


        }
        static void FillHole()
        {
            //變數宣告
            int Width = param.LayoutSize.Width;
            int Height = param.LayoutSize.Height;
            int[,] InDegree = new int[Width, Height];
            Stack<Point> stack = new Stack<Point>();
            //InDegree陣列初始化
            for (int i = 0; i < Height; i++)
                for (int j = 0; j < Width; j++)
                {
                    //每一個座標可以從上下左右四個方向進來
                    InDegree[j, i] = 4;
                    //如果在遊戲邊界四周,InDegree要扣1
                    if (i == 0 || i == Height - 1)
                        InDegree[j, i] -= 1;
                    if (j == 0 || j == Width - 1)
                        InDegree[j, i] -= 1;
                }

            //枚舉每個座標點,如果該座標點有障礙物,其四周的格子InDegee扣1
            for (int i = 0; i < Height; i++)
                for (int j = 0; j < Width; j++)
                    if (!MovingAvailable(j, i, false, true))
                    {
                        //這個函式可以扣除Point座標四周的座標InDegree的值1
                        //如果有產生InDegree等於1的坑洞,也會順便塞入stack內
                        inDegreeDecrease(InDegree, new Point(j, i), 1, stack);
                        //這個座標點有障礙物,不可通行,InDegree = 0
                        InDegree[j, i] = 0;
                    }
            Stack<Point> temp = new Stack<Point>();
            while (stack.Count > 0)
            {
                temp.Push(stack.Peek());
                Point p = stack.Pop();
                //在p座標放上障礙物,設定背景顏色紅色
                BlockSpawn(p, ConsoleColor.Red);
                inDegreeDecrease(InDegree, p, 1, stack);
            }
        }
        static void show(int[,] ind)
        {
            for (int i = 0; i < param.LayoutSize.Height; i++)
            {
                for (int j = 0; j < param.LayoutSize.Width; j++)
                    System.Diagnostics.Debug.Write(ind[j, i] + " ");
                System.Diagnostics.Debug.WriteLine("");
            }
        }
        //MAC ACTION
        static Random r = new Random();
        static int NextPeriod = 10;
        static int NowPeriod = 0;
        static void SpawnPacAction()
        {
            if (param.Foods.Count == 0 && param.Max_Pac_Count > 0)
            {
                SpawnRandomPac();
                NowPeriod = 0;
            }

            if (param.Foods.Count < param.Max_Pac_Count && NowPeriod >= NextPeriod)
            {
                SpawnRandomPac();
                NowPeriod = 0;
            }
            else
                NowPeriod+=1;

            
        }
        //隨機生成食物星星
        static void SpawnRandomPac()
        {
            Point p;
            do
                p = new Point(r.Next(0, param.GameWidth), r.Next(0, param.GameHeight));
            while (!MovingAvailable(p, true,true));
            param.Foods.Add(p);
            //繪製星星
            lock (PrintLock)
            {
                SetLayoutPosition(p);
                Console.ResetColor();
                Console.Write(Param.PacStyle);
            }
        }

        //SNAKE ACTION
        private static Direct[] _conflictDirect = new Direct[]
            { Direct.Down, Direct.Left, Direct.Top, Direct.Right };
        static void SnakeMoveAction()
        {
            //方向參數
            Direct d = param.Direct;        //當前蛇面向
            Direct Choice = d;              //決定切換面向

            //玩家按鍵操控方向檢查
            Point p;
            if(param.SwitchDirect != Direct.Nono)
            {
                p = param.SnakeBody.GetTop + Param.DirectionCond[(int)param.SwitchDirect];
                if (MovingAvailable(p,false,true))
                {
                    Choice = param.SwitchDirect;
                    param.SwitchDirect = Direct.Nono;
                }
            }

            //能否移動
            p = param.SnakeBody.GetTop + Param.DirectionCond[(int)Choice];
            int tryDirect = r.Next(0, 2) == 0 ? -1 : 1;
            while(!MovingAvailable(p,false,true))
            {
                Choice = (Direct)(((int)Choice + tryDirect + 4) % 4);
                //換方向工作不反向180度移動
                if (
                    d == Direct.Down && Choice == Direct.Top ||
                    d == Direct.Top && Choice == Direct.Down ||
                    d == Direct.Right && Choice == Direct.Left ||
                    d == Direct.Left && Choice == Direct.Right
                    )
                {
                    continue;
                }

                //移動將會導致超出邊界,執行換方向工作
                p = param.SnakeBody.GetTop + Param.DirectionCond[(int)Choice];
            }
            param.Direct = Choice;

            //Pac Process or move
            int Eat = param.Foods.IndexOf(p);
            int Hole = param.BugHoles.IndexOf(p);
            if (Eat != -1)
                EatOne(param.Foods[Eat]);
            else if (Hole != -1)
                SnakeGoingBugHole(Hole % 2 == 0 ? param.BugHoles[Hole + 1] : param.BugHoles[Hole-1], Choice);
            else
                SnakeMoving(Choice);

            param.SwitchDirect = Direct.Nono;
        }
        static void SnakeMoving(Direct d)
        {
            param.Direct = d;
            Point LastHead = param.SnakeBody.GetTop;
            Point NewHead = new Point(
                LastHead.X + Param.DirectionCond[(int)param.Direct].X,
                LastHead.Y + Param.DirectionCond[(int)param.Direct].Y
                );
            Console.ForegroundColor = Param.BugColor[0];
            param.SnakeBody.Push(NewHead);
            Point Tail = param.SnakeBody.Pop();
            SetLayoutPosition(Tail);
            Console.Write(Param.EmptyStyle);
            SetLayoutPosition(LastHead);
            Console.Write(Param.SnakeBodyStyle);
            SetLayoutPosition(NewHead);
            Console.Write(Param.SnakeHeadStyle);
            Console.ResetColor();
        }
        static void SnakeGoingBugHole(Point OuterCond,Direct Dir)
        {
            SetLayoutPosition(param.SnakeBody.GetTop);
            Console.ForegroundColor = Param.BugColor[0];
            Console.Write(Param.SnakeBodyStyle);
            Point Out = OuterCond + Param.DirectionCond[(int)Dir];
            while(!MovingAvailable(Out,false,true))
            {
                Dir = (Direct)(((int)Dir + 1)%4);
                Out = OuterCond + Param.DirectionCond[(int)Dir];
                param.Direct = Dir;
            }
            param.SnakeBody.Push(Out);
            Point Tail = param.SnakeBody.Pop();
            SetLayoutPosition(Tail);
            Console.Write(Param.EmptyStyle);
            SetLayoutPosition(Out);
            Console.Write(Param.SnakeHeadStyle);
            Console.ResetColor();
        }
        static void EatOne(Point FeedPosition)
        {
            //新增蛇身在Pac的位置
            param.SnakeBody.Push(FeedPosition);

            //重繪頭
            Console.ForegroundColor = Param.BugColor[0];
            Point p = param.SnakeBody.GetTop;
            SetLayoutPosition(p);
            Console.Write(Param.SnakeHeadStyle);
            
            //重繪上一個頭的位置
            SetLayoutPosition(param.SnakeBody[1]);
            Console.Write(Param.SnakeBodyStyle);

            //Remove Food
            param.Foods.Remove(FeedPosition);

        }
        static bool MovingAvailable(int px,int py, bool CheckInstance = false, bool CheckBound = false)
        {
            return MovingAvailable(new Point(px, py), CheckInstance, CheckBound);
        }
        static bool MovingAvailable(Point p,bool CheckInstance = false,bool CheckBound = false)
        {
            if (CheckInstance &&
                (
                param.SnakeBody.IndexOf(p) != -1 ||
                param.Foods.IndexOf(p) != -1 ||
                param.BugHoles.IndexOf(p) != -1
                )
                )
                return false;
            return !(p.X < 0 || p.Y < 0 || p.X >= param.LayoutSize.Width || p.Y >= param.LayoutSize.Height || (CheckBound && param.Bound[p.X, p.Y]));
        }

        static void SnakeInitialization(Point HeadAt,Direct dir,int Length)
        {
            if (Length <= 0)
                throw new Exception("蛆身長度必須大於0");
            while(param.SnakeBody.Length>0)
                param.SnakeBody.Pop();

            Length -= 1;
            param.SnakeBody.PushBack(HeadAt);
            param.Direct = dir;

            for (int i = 0; i < Length; i++)
                SnakeBodyAdd(1, dir, false);
        }
        static void SnakeBodyAdd(int BodyNum,Direct d,bool Redraw = false)
        {
            Point Tail = param.SnakeBody.GetBottom;
            for (int i = 0; i < BodyNum; i++)
            {
                Tail = Tail + Param.DirectionCond[(int)d];
                if (MovingAvailable(Tail))
                    param.SnakeBody.PushBack(Tail);
                else
                    break;
            }
            if(Redraw)
                DrawSnake();
        }

        static void SnakeInjureAction()
        {
            Point head = param.SnakeBody.GetTop;
            int index = param.SnakeBody.LastIndexOf(head);
            if (index >= 1)
                SnakeInjure(param.SnakeBody[index]);
        }
        static void SnakeInjure(Point Position)
        {
            Point p;
            while (param.SnakeBody.GetBottom != Position)
            {
                p = param.SnakeBody.Pop();
                CreateEffect(p, BloodSnakeBody);
            }
            p = param.SnakeBody.Pop();
            //CreateEffect(p, BloodSnakeBody);
        }

        //BugHole
        static int colorIndex = 0;
        static void SpawnBugHoleAction()
        {
            Point begin, end;
            do
                begin = new Point(r.Next(0, param.GameWidth), r.Next(0, param.GameHeight));
            while (!MovingAvailable(begin, true, true) || GetInDegree(begin)==0);
            do
                end = new Point(r.Next(0, param.GameWidth), r.Next(0, param.GameHeight));
            while (!MovingAvailable(end, true, true) || GetInDegree(end)==0 || begin == end);

            param.BugHoles.Add(begin);
            param.BugHoles.Add(end);
            
            lock (PrintLock)
            {
                Console.ForegroundColor = Param.BugHoleColor[colorIndex];
                SetLayoutPosition(begin);
                Console.Write(Param.BugHoleStyle);
                SetLayoutPosition(end);
                Console.Write(Param.BugHoleStyle);
                Console.ResetColor();
                colorIndex = (colorIndex + 1) % 4;
            }
        }

        //Bound
        static void BoundSpawn(Rectangle ClearArea, int spawn = 1)
        {
            for (int i = 0; i < spawn; i++)
            {
                Point p;
                do
                    p = new Point(r.Next(0, param.GameWidth), r.Next(0, param.GameHeight));
                while (!MovingAvailable(p, true) || ClearArea.IntersectionWith(p));

                param.Bound[p.X, p.Y] = true;
                //繪製障礙物
                lock (PrintLock)
                {
                    SetLayoutPosition(p);
                    Console.BackgroundColor = Param.BoundColor;
                    Console.Write(Param.BoundStyle);
                    Console.ResetColor();
                }
            }
        }
        static void BoundSpawnNoDeadRoad(int spawn = 1)
        {
            //建立連通陣列
            int[,] InDegree = new int[param.LayoutSize.Width,param.LayoutSize.Height];
            for (int i = 0; i < param.LayoutSize.Height; i++)
                for (int j = 0; j < param.LayoutSize.Width; j++)
                {
                    InDegree[j, i] = 4;
                    if (i == 0 || i == param.LayoutSize.Height - 1)
                        InDegree[j, i] -= 1;
                    if (j == 0 || j == param.LayoutSize.Width - 1)
                        InDegree[j, i] -= 1;
                }
            for (int i = 0; i < spawn; i++)
            {
                Point p;
                do
                    p = new Point(r.Next(0, param.LayoutSize.Width), r.Next(0, param.LayoutSize.Height));
                while (!MovingAvailable(p, true) || inDegreeTest(InDegree, p));

                //inDegreeDecrease(InDegree, p);
                param.Bound[p.X, p.Y] = true;
                lock (PrintLock)
                {
                    SetLayoutPosition(p);
                    Console.BackgroundColor = Param.BoundColor;
                    Console.Write(Param.BoundStyle);
                    Console.ResetColor();
                }
            }
        }
        static bool inDegreeTest(int[,] inDegree,Point p,int value = 2)
        {
            foreach(Point cond in Param.DirectionCond)
            {
                Point np = new Point(cond.X + p.X, cond.Y + p.Y);
                if (np.X < 0 || np.Y < 0 || np.X >= param.LayoutSize.Width || np.Y >= param.LayoutSize.Height)
                    continue;
                if (inDegree[np.X, np.Y] == value)
                    return true;
            }
            return false;
        }
        static void inDegreeDecrease(int[,] inDegree,Point p,int value ,Stack<Point> collect)
        {
            foreach (Point cond in Param.DirectionCond)
            {
                Point np = new Point(cond.X + p.X, cond.Y + p.Y);
                
                if (np.X < 0 || np.Y < 0 || np.X >= param.LayoutSize.Width || np.Y >= param.LayoutSize.Height)
                    continue;
                if(inDegree[np.X,np.Y]>0)
                    inDegree[np.X, np.Y] -= 1;
                if (inDegree[np.X, np.Y] == value && !param.Bound[np.X,np.Y])
                {
                    collect.Push(np);
                }
            }
            
        }
        static void BlockSpawn(Point p)
        {
            param.Bound[p.X, p.Y] = true;
            lock (PrintLock)
            {
                SetLayoutPosition(p);
                Console.BackgroundColor = Param.BoundColor;
                Console.Write(Param.BoundStyle);
                Console.ResetColor();
            }
        }
        static void BlockSpawn(Point p,ConsoleColor cc)
        {
            param.Bound[p.X, p.Y] = true;
            lock (PrintLock)
            {
                SetLayoutPosition(p);
                Console.BackgroundColor = cc;
                Console.Write(Param.BoundStyle);
                Console.ResetColor();
            }
        }
        static int GetInDegree(Point p)
        {
            int InDegree = 0;
            for(int i = 0;i < 4; i++)
            {
                Point np = Param.DirectionCond[i] + p;
                if (MovingAvailable(np,true,true))
                    InDegree++;
            }
            return InDegree;
        }
#endregion
        #region Keyboard Control
        private static ConsoleKey[] _conflict = new ConsoleKey[]
            {ConsoleKey.DownArrow,ConsoleKey.LeftArrow,ConsoleKey.UpArrow,ConsoleKey.RightArrow};
        static void KeyboardHandle()
        {
            bool Handle = false;
            var key = Console.ReadKey();
            if (_conflict[(int)param.Direct] == key.Key)
            {
                param.SwitchDirect = Direct.Nono;
                return;
            }
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    param.SwitchDirect = Direct.Top;
                    Handle = true;
                    break;
                case ConsoleKey.DownArrow:
                    param.SwitchDirect = Direct.Down;
                    Handle = true;
                    break;
                case ConsoleKey.LeftArrow:
                    param.SwitchDirect = Direct.Left;
                    Handle = true;
                    break;
                case ConsoleKey.RightArrow:
                    param.SwitchDirect = Direct.Right;
                    Handle = true;
                    break;
                case ConsoleKey.P:
                    Pause = !Pause;
                    Handle = true;
                    break;
            }
        }
#endregion
        #region Print functions

        static void DrawSnake()
        {
            bool Head = true;
            foreach(Point p in param.SnakeBody)
            {
                lock (PrintLock)
                {
                    Console.ForegroundColor = Param.BugColor[0];
                    SetCursorPosition(p.X + param.BoundWidth, p.Y + param.BoundWidth);
                    if (Head)
                    {
                        Console.Write(Param.SnakeHeadStyle);
                        Head = false;
                    }
                    else
                        Console.Write(Param.SnakeBodyStyle);
                    Console.ResetColor();
                }
            }
        }
        static void BasicLayout()
        {
            lock (PrintLock)
            {
                //Bound Color
                Console.BackgroundColor = Param.BoundColor;

                //Top Bound
                SetCursorPosition(0, 0);
                for (int i = 0; i < param.BoundWidth; i++)
                    Console.WriteLine(new string(Param.BoundStyle, param.GameWidth));

                //Left and Right Bound
                for (int i = 0; i < param.GameHeight - 2 * param.BoundWidth; i++)
                {
                    Console.Write(new string(Param.BoundStyle, param.BoundWidth));
                    SetCursorPosition(param.GameWidth - param.BoundWidth, Console.CursorTop);
                    Console.WriteLine(new string(Param.BoundStyle, param.BoundWidth));
                }

                //Bottom Bound
                SetCursorPosition(0, param.GameHeight - param.BoundWidth);
                for (int i = 0; i < param.BoundWidth; i++)
                    Console.WriteLine(new string(Param.BoundStyle, param.GameWidth));

                //Reset Color
                Console.ResetColor();
            }
        }
        static void Clear()
        {
            Console.Clear();
            Console.SetCursorPosition(0, 0);
        }
        public static void SetLayoutPosition(int left,int top)
        {
            Console.SetCursorPosition( (param.BoundWidth+left) * Param.Trans, top+param.BoundWidth);
        }
        public static void SetLayoutPosition(Point P)
        {
            Console.SetCursorPosition((param.BoundWidth + P.X) * Param.Trans, P.Y+param.BoundWidth);
        }
        public static void SetCursorPosition(int left,int top)
        {
            Console.SetCursorPosition(left * Param.Trans, top);
        }

        private static Thread RedrawProcedure;
        public static void RedrawObjectAsync()
        {
            //Redraw Every Object
            foreach (var v in param.Foods.ToArray())
            {
                lock (PrintLock)
                {
                    SetLayoutPosition(v);
                    Console.ResetColor();
                    Console.Write(Param.PacStyle);
                }
                Thread.Sleep(50);
            }
            foreach (var v in param.BugHoles.ToArray())
            {
                lock (PrintLock)
                {
                    SetLayoutPosition(v);
                    Console.ForegroundColor = Param.BugHoleColor[0];
                    Console.Write(Param.BugHoleStyle);
                    Console.ResetColor();
                }
                Thread.Sleep(50);
            }
                //BasicLayout();
            
            Thread.Sleep(100);
        }
#endregion
        #region Special_Effect
        //一些特效
        public delegate void SpecialEffect(Point cond, object Event);
        public static void CreateEffect(Point ScreenCond,SpecialEffect se,object Event = null)
        {
            new Thread(() => se(ScreenCond, Event)).Start();
        }
        public static void BloodSnakeBody(Point ScreenCond,object Event)
        {
            lock (PrintLock)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                SetLayoutPosition(ScreenCond);
                Console.Write(Param.SnakeBodyStyle);
                Console.ResetColor();
            }
            int i = 0;
            while (i <= Param.OneSec/Param.Fixed_FPS*3)
            {
                Thread.Sleep(Param.OneSec/Param.Fixed_FPS);
                if (param.SnakeBody.GetTop == ScreenCond)
                    return;
                i++;
            }
            lock (PrintLock)
            {
                SetLayoutPosition(ScreenCond);
                Console.Write(Param.EmptyStyle);
            }
        }
#endregion


#if DEBUG || GameDEBUG
        static void ShowEmptyLayout()
        {
            Clear();
            BasicLayout();
        }
        static void HighLightLayout(Point p)
        {
            lock (PrintLock)
            {
                SetLayoutPosition(p);
                Console.BackgroundColor = ConsoleColor.Yellow;
                Console.Write(Param.EmptyStyle);
                Console.ResetColor();
            }
        }
        static void RedrawEveryThing(Point[] ps)
        {
            Console.ResetColor();
            ShowEmptyLayout();
            Console.ResetColor();
            for (int i = 0; i < param.LayoutSize.Width; i++)
                for(int j = 0;j < param.LayoutSize.Height;j++)
                    if(param.Bound[i,j])
                    {
                        SetLayoutPosition(i, j);
                        Console.BackgroundColor = Param.BoundColor;
                        Console.Write(Param.BoundStyle);
                    }
            Console.ResetColor();
            int k = 0;
            foreach(var item in param.BugHoles)
            {
                SetLayoutPosition(item);
                Console.ForegroundColor = Param.BugHoleColor[k++];
                Console.Write(Param.BugHoleStyle);
            }
            foreach(var p in ps)
            {
                SetLayoutPosition(p);
                Console.BackgroundColor = ConsoleColor.Red;
                Console.Write(Param.BoundStyle);
            }
           
            foreach(var item in param.Foods)
            {
                SetLayoutPosition(item);
                Console.Write(Param.PacStyle);
            }
        }
        static void NumUp(int[,] ind,Point[] ps)
        {
        
            for(int i = 0;i < param.LayoutSize.Width;i++)
                for(int j = 0;j < param.LayoutSize.Height;j++)
                {
                    Console.ResetColor();
                    SetLayoutPosition(i, j);
                    if (param.Bound[i, j])
                        Console.BackgroundColor = Param.BoundColor;
                    Console.Write(ind[i, j]);
                }
            foreach (var item in ps)
            {
                Console.ResetColor();
                SetLayoutPosition(item);
                Console.BackgroundColor = ConsoleColor.Red;
                Console.Write(ind[item.X, item.Y]);
            }
        }
#endif

    }

    enum Direct : int
    {
        Top = 0,
        Right = 1,
        Down = 2,
        Left = 3,
        Stand = 4,
        Nono = 5
    }
    enum MapAnalyzeMode
    {
        Fix,ThrowError,Ignore,ShowDialog
    }
    /// <summary>
    /// 提供Queue的Push和Pop,也提供List歷覽每一個元素的能力,用來當作Snake的身體結構
    /// </summary>
    class ListQueue<T> : IEnumerable<T>
    {
        public readonly int MAX_SIZE;
        private T[] QArray;

        int Next;
        int Bottom;
        public int Length
        {
            get { return (Next - Bottom + MAX_SIZE) % MAX_SIZE; }
        }

        public void Push(T data)
        {
            if ((Next + 1) % MAX_SIZE == Bottom)
                throw new Exception("ListQueue爆了");
            QArray[Next] = data;
            Next = (Next + 1) % MAX_SIZE;
        }
        public void PushBack(T data)
        {
            if ((Bottom - 1 + MAX_SIZE) % MAX_SIZE == Next)
                throw new Exception("ListQueue爆了");
            QArray[(Bottom - 1 + MAX_SIZE) % MAX_SIZE] = data;
            Bottom = (Bottom - 1 + MAX_SIZE) % MAX_SIZE;
        }
        public T Pop()
        {
            if (Next == Bottom)
                throw new Exception("沒有資料可以Pop");

            T Return = QArray[Bottom];
            QArray[Bottom] = default(T);
            Bottom = (Bottom + 1) % MAX_SIZE;

            return Return;
        }

        //Support Foreach
        public IEnumerator<T> GetEnumerator()
        {
            if (Next == Bottom)
                yield break;
            int i = (Next - 1 + MAX_SIZE) % MAX_SIZE;
            while (i != Bottom)
            {
                yield return QArray[i];
                i = (i - 1 + MAX_SIZE) % MAX_SIZE;
            }
            yield return QArray[Bottom];
        }    
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerable<T> GetEnumeratorReverse()
        {
            if (Next == Bottom)
                yield break;
            int i = Bottom;
            while (i != Next)
            {
                yield return QArray[i];
                i = (i + 1) % MAX_SIZE;
            }
        }

        public int IndexOf(T dat)
        {
            int i = 0;
            foreach (T obj in this)
                if (obj.Equals(dat))
                    return i;
                else
                    i++;
            return -1;
        }
        public int LastIndexOf(T dat)
        {
            int i = Length-1;
            foreach (T obj in this.GetEnumeratorReverse())
                if (obj.Equals(dat))
                    return i;
                else
                    i--;
            return -1;
        }

        public T GetTop
        {
            get
            {
                if (Next == Bottom)
                    throw new Exception("This is a empty queue");

                return QArray[(Next - 1+MAX_SIZE)%MAX_SIZE];
            }
            set { QArray[Next - 1] = value; }
        }
        public T GetBottom
        {
            set
            {
                if (Bottom == Next)
                    throw new Exception("This is a empty queue");
                QArray[Bottom] = value;
            }
            get { return QArray[Bottom]; }
        }

        public T this[int index]
        {
            get
            {
                if ((Next - 1 - index - Bottom + MAX_SIZE) % MAX_SIZE >= Length)
                    throw new IndexOutOfRangeException();
                return QArray[(Next - 1 - index+MAX_SIZE) % MAX_SIZE];
            }
        }

        public ListQueue(int SIZE)
        {
            MAX_SIZE = SIZE+1;
            QArray = new T[MAX_SIZE];
            Next = 0;
            Bottom = 0;
        }
    }
    /// <summary>
    /// 表示一個整數二維座標
    /// </summary>
    struct Point
    {
        public int X;
        public int Y;

        public static Point operator+(Point lsb,Point rsb)
        {
            return new GreedSnake.Point(lsb.X + rsb.X, lsb.Y + rsb.Y);
        }
        public static bool operator==(Point lsb,Point rsb)
        {
            return lsb.X == rsb.X && lsb.Y == rsb.Y;
        }
        public static bool operator!=(Point lsb,Point rsb)
        {
            return !(lsb == rsb);
        }

        public Point(int X,int Y) { this.X = X; this.Y = Y; }
    }
    /// <summary>
    /// 表示一個整數二維空間
    /// </summary>
    struct Rectangle
    {
        public Point Begin;
        public int w;
        public int h;
        public bool IntersectionWith(Point p)
        {
            return p.X >= Begin.X && p.Y >= Begin.Y && p.X < Begin.X + w && p.Y < Begin.Y + h;
        }

        public Rectangle(int x,int y,int w,int h)
        {
            this.Begin = new Point(x, y);
            this.w = w;
            this.h = h;
        }
        public Rectangle(Point p,int w,int h)
        {
            this.Begin = p;
            this.w = w;
            this.h = h;
        }
    }
    /// <summary>
    /// 表示一個大小
    /// </summary>
    struct Size
    {
        public int Height, Width;
        public Size(int Height,int Width) { this.Height = Height; this.Width = Width; }
    }
}
