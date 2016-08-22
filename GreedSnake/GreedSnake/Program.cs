﻿using System;
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
            Initialization();
#if DEBUG
            Debug_ShowEmptyLayout();
            
            SnakeBodyAdd(3, Direct.Right);
            BoundSpawn(30);

            Clock = new Thread(GameMainCheckProcedure);
            GameClock += GameTrigger;
            param.Direct = Direct.Down;

            DrawSnake();
            Clock.Start();
#endif
            while (true)
                KeyboardHandle();

            Console.ReadKey();
        }
        /// <summary>
        /// 在遊戲剛開始時執行此函式,針對遊戲參數初始化
        /// </summary>
        static void Initialization()
        {
            Console.Title = "貪吃蛆";
            Console.CursorVisible = true;
            
            param = new Param(
                (Console.WindowHeight)-1,
                (Console.WindowWidth/2)-1,
                3,
                300,
                7
                );
            Clock = new Thread(GameMainCheckProcedure);
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
            while (!TimerShutDown)
            {
                Thread.Sleep(Param.GameCheckSec / Param.Fixed_FPS);
                SecondPeriod = (SecondPeriod + 1) % (Param.GameCheckSec / Param.Fixed_FPS);
                //Run
                if (GameClock != null)
                    GameClock(null,new EventArgs());
                SetCursorPosition(0,param.GameHeight);
            }   
        }
        private static EventHandler<EventArgs> GameClock;
        
        static void GameTrigger(object sender,EventArgs e)
        {
            if (SecondPeriod == 0)
            {
                SpawnPacAction();
                SnakeMoveAction();
                SnakeInjureAction();
            }
            RefreshUI();
            
        }

        #endregion
        //SYSTEM
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
        static void SpawnRandomPac()
        {
            Point p;
            do
                p = new Point(r.Next(0, param.GameWidth / 2), r.Next(0, param.GameHeight / 2));
            while (!MovingAvailable(p, true,true));
            param.Foods.Add(p);

            SetLayoutPosition(p);
            Console.Write(Param.PacStyle);
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
            while(!MovingAvailable(p,false,true))
            {
                Choice = (Direct)(((int)Choice + 1) % 4);
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
            if (Eat != -1)
                EatOne(param.Foods[Eat]);
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
            param.SnakeBody.Push(NewHead);
            Point Tail = param.SnakeBody.Pop();
            SetLayoutPosition(Tail);
            Console.Write(Param.EmptyStyle);
            SetLayoutPosition(LastHead);
            Console.Write(Param.SnakeBodyStyle);
            SetLayoutPosition(NewHead);
            Console.Write(Param.SnakeHeadStyle);
        }
        static void EatOne(Point FeedPosition)
        {
            //新增蛇身在Pac的位置
            param.SnakeBody.Push(FeedPosition);

            //重繪頭
            Point p = param.SnakeBody.GetTop;
            SetLayoutPosition(p);
            Console.Write(Param.SnakeHeadStyle);
            //重繪上一個頭的位置
            SetLayoutPosition(param.SnakeBody[1]);
            Console.Write(Param.SnakeBodyStyle);

            //Remove Food
            param.Foods.Remove(FeedPosition);

        }
        static bool MovingAvailable(Point p,bool CheckInstance = false,bool CheckBound = false)
        {
            if (CheckInstance &&
                (
                param.SnakeBody.IndexOf(p) != -1 ||
                param.Foods.IndexOf(p) != -1
                )
                )
                return false;
            return !(p.X < 0 || p.Y < 0 || p.X >= param.LayoutSize.Width || p.Y >= param.LayoutSize.Height || (CheckBound && param.Bound[p.X, p.Y]));
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
            CreateEffect(p, BloodSnakeBody);
        }

        //Bound
        static void BoundSpawn(int spawn = 1)
        {
            for (int i = 0; i < spawn; i++)
            {
                Point p;
                do
                    p = new Point(r.Next(0, param.GameWidth), r.Next(0, param.GameHeight));
                while (!MovingAvailable(p, true));

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
                    SetCursorPosition(p.X + param.BoundWidth, p.Y + param.BoundWidth);
                    if (Head)
                    {
                        Console.Write(Param.SnakeHeadStyle);
                        Head = false;
                    }
                    else
                        Console.Write(Param.SnakeBodyStyle);
                }
            }
        }
        static void BasicLayout()
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

        static bool Async_Working = false;
        public async static void RedrawObjectAsync()
        {
            //Redraw Every Object
            
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
            while (i <= Param.OneSec / Param.Fixed_FPS*3)
            {
                Thread.Sleep(Param.GameCheckSec/Param.Fixed_FPS);
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


#if DEBUG
        static void Debug_ShowEmptyLayout()
        {
            Clear();
            BasicLayout();
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
    /// 表示一個大小
    /// </summary>
    struct Size
    {
        public int Height, Width;
        public Size(int Height,int Width) { this.Height = Height; this.Width = Width; }
    }
}