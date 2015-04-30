#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Console.cs is part of LeagueSharp.Console.

 LeagueSharp.Console is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 LeagueSharp.Console is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with LeagueSharp.Console. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License
namespace LeagueSharp.Console
{
    #region

    using System;
    using System.Globalization;
    using Parts;
    using SharpDX;

    #endregion

    public class Console
    {
        private static Vector2 _offset;
        private static string _output = string.Empty;

        static Console()
        {
            Width = (int) (Drawing.Width/2.5);
            Height = (int) (Drawing.Height/2.5);

            Offset = new Vector2(Drawing.Width/2f, (Drawing.Height - Height)/2f);

            Alpha = 200;

            Header.BackgroundColor = Color.DarkSlateGray;
            Header.ForegroundColor = Color.White;
            Header.Height = 25;
            Header.FontHeight = 18;
            Header.Content = "LeagueSharp.Console";

            Content.BackgroundColor = Color.Black;
            Content.ForegroundColor = Color.White;
            Content.Padding = 10;
            Content.FontHeight = 18;

            Scrollbar.BackgroundColor = Color.DarkGray;
            Scrollbar.FontHeight = (int)(Content.FontHeight * 1.3);
            Scrollbar.Width = 15;
            Scrollbar.Interval = 5;

            Parts.Minimize.BackgroundColor = Color.DarkSlateGray;
            Parts.Minimize.ForegroundColor = Color.White;

            Drawing.OnPreReset += OnDrawingPreReset;
            Drawing.OnPostReset += OnDrawingPostReset;
            Drawing.OnEndScene += OnDrawingEndScene;

            WriteLine("LeagueSharp.Console successfully loaded." + Environment.NewLine);
        }

        internal static bool Minimized { get; set; }
        internal static bool Hidden { get; set; }

        public static Vector2 Offset
        {
            get { return _offset; }
            set
            {
                var x = value.X;
                var y = value.Y;

                if (x + Width/2f > Drawing.Width)
                {
                    x = Drawing.Width - Width/2f;
                }
                else if (x - Width/2f < 0)
                {
                    x = 0 + Width/2f;
                }

                if (y + Height > Drawing.Height)
                {
                    y = Drawing.Height - Height;
                }
                else if (y < 0)
                {
                    y = 0;
                }
                _offset = new Vector2(x, y);
            }
        }

        public static int Width { get; set; }
        public static int Height { get; set; }

        internal static string Output
        {
            get { return _output; }
            set { _output = value; }
        }

        public static int Alpha
        {
            set
            {
                Header.Alpha = value;
                Content.Alpha = value;
                Scrollbar.Alpha = value;
                Parts.Minimize.Alpha = value;
            }
        }

        internal static bool IsMoving { get; set; }

        ~Console()
        {
            Drawing.OnPreReset -= OnDrawingPreReset;
            Drawing.OnPostReset -= OnDrawingPostReset;
            Drawing.OnEndScene -= OnDrawingEndScene;
        }

        private static void OnDrawingEndScene(EventArgs args)
        {
            if (Hidden)
            {
                return;
            }

            if (Minimized)
            {
                Parts.Minimize.EndScene();
            }
            else
            {
                Header.EndScene();
                Content.EndScene();
                Scrollbar.EndScene();
                Parts.Minimize.EndScene();
            }
        }

        private static void OnDrawingPostReset(EventArgs args)
        {
            Header.PostReset();
            Content.PostReset();
            Scrollbar.PostReset();
            Parts.Minimize.PostReset();
        }

        private static void OnDrawingPreReset(EventArgs args)
        {
            Header.PreReset();
            Content.PreReset();
            Scrollbar.PreReset();
            Parts.Minimize.PreReset();
        }

        public static void Write(string value)
        {
            var scrollDown = Scrollbar.DragTop == Content.Height - Scrollbar.Height;
            Output += value;
            Scrollbar.DragTop = scrollDown ? Content.Height - Scrollbar.Height : Scrollbar.DragTop;
        }

        public static void WriteLine(string value)
        {
            Write(value + Environment.NewLine);
        }

        public static void Clear()
        {
            Output = string.Empty;
        }

        public static void Show()
        {
            Hidden = false;
        }

        public static void Hide()
        {
            Hidden = true;
        }

        public static void Minimize()
        {
            Minimized = true;
        }

        public static void Maximize()
        {
            Minimized = false;
        }

        #region Write

        public static void WriteLine()
        {
            WriteLine(string.Empty);
        }

        public static void WriteLine(bool value)
        {
            WriteLine(value.ToString());
        }

        public static void WriteLine(char value)
        {
            WriteLine(value.ToString());
        }

        public static void WriteLine(decimal value)
        {
            WriteLine(value.ToString(CultureInfo.InvariantCulture));
        }

        public static void WriteLine(double value)
        {
            WriteLine(value.ToString(CultureInfo.InvariantCulture));
        }

        public static void WriteLine(float value)
        {
            WriteLine(value.ToString(CultureInfo.InvariantCulture));
        }

        public static void WriteLine(int value)
        {
            WriteLine(value.ToString());
        }

        public static void WriteLine(uint value)
        {
            WriteLine(value.ToString());
        }

        public static void WriteLine(long value)
        {
            WriteLine(value.ToString());
        }

        public static void WriteLine(ulong value)
        {
            WriteLine(value.ToString());
        }

        public static void Write(bool value)
        {
            Write(value.ToString());
        }

        public static void Write(char value)
        {
            Write(value.ToString());
        }

        public static void Write(double value)
        {
            Write(value.ToString(CultureInfo.InvariantCulture));
        }

        public static void Write(decimal value)
        {
            Write(value.ToString(CultureInfo.InvariantCulture));
        }

        public static void Write(float value)
        {
            Write(value.ToString(CultureInfo.InvariantCulture));
        }

        public static void Write(int value)
        {
            Write(value.ToString());
        }

        public static void Write(uint value)
        {
            Write(value.ToString());
        }

        public static void Write(long value)
        {
            Write(value.ToString());
        }

        public static void Write(ulong value)
        {
            Write(value.ToString());
        }

        #endregion
    }
}