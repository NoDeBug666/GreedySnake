using System;
using System.Collections.Generic;

namespace GreedSnake
{
    partial class Program
    {
        /// <summary>
        /// 放置著整個遊戲的系統參數
        /// </summary>
        class Param
        {
            public readonly int GameHeight;
            public readonly int GameWidth;
            public readonly Size LayoutSize;
            public readonly int BoundWidth;
            /// <summary>
            /// 遊戲障壁分布 Bound[寬,高]
            /// </summary>
            public bool[,] Bound;
            public ListQueue<Point> SnakeBody;
            public List<Point> Foods;
            //奇數偶數兩個一對洞
            public List<Point> BugHoles;
            /// <summary>
            /// 幾毫秒對此遊戲來說是1秒
            /// </summary>
            public static int OneSec = 1000;
            /// <summary>
            /// 遊戲檢查時間,通常會影響到整個遊戲的運作,但不包含畫面的更新
            /// </summary>
            public static int GameCheckSec = 250;

            private object _directLock = new object();
            private Direct _Direct;
            public Direct Direct
            {
                get
                {
                    lock (_directLock)
                    {
                        return _Direct;
                    }
                }
                set
                {
                    lock (_directLock)
                    {
                        _Direct = value;
                    }
                }
            }
            public Direct SwitchDirect;

            public int Max_Pac_Count;

            public static Point[] DirectionCond = new Point[]
            {
            new Point(0,-1),
            new Point(1,0),
            new Point(0,1),
            new Point(-1,0),
            new Point(0,0)
            };

            public const int Fixed_FPS = 60;
            public static char SnakeBodyStyle = 'ｍ';
            public static char SnakeHeadStyle = '囧';
            public static char BoundStyle = '■';
            public static char EmptyStyle = '　';
            public static char PacStyle = '★';
            public static char BugHoleStyle = 'Ｏ';
            public static ConsoleColor BoundColor = ConsoleColor.DarkYellow;
            public static ConsoleColor[] BugHoleColor = new ConsoleColor[] { ConsoleColor.Blue , ConsoleColor.Cyan,ConsoleColor.DarkBlue,ConsoleColor.DarkCyan };
            public static ConsoleColor[] BugColor = new ConsoleColor[] { ConsoleColor.Green };
            public static int Trans = 2;

            public Param(
                int ConsoleHeight,
                int ConsoleWidth,
                int BoundWidth,
                int Max_Snake_Length,
                int Max_Pac_Count
                )
            {
                this.GameHeight = ConsoleHeight;
                this.GameWidth = ConsoleWidth;
                this.LayoutSize = new Size(GameHeight - BoundWidth * 2, GameWidth - BoundWidth * 2);
                this.BoundWidth = BoundWidth;
                Bound = new bool[GameWidth, GameHeight];

                this.Max_Pac_Count = Max_Pac_Count;

                SnakeBody = new ListQueue<Point>(Max_Snake_Length);
                Foods = new List<Point>();
                BugHoles = new List<Point>();
                SwitchDirect = Direct.Nono;
            }
        }
    }
}
