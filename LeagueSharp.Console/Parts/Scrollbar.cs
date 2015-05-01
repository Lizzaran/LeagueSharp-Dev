#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 scrollbar.cs is part of LeagueSharp.Console.

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

namespace LeagueSharp.Console.Parts
{
    #region

    using System;
    using Common;
    using SharpDX;
    using SharpDX.Direct3D9;
    using Console = Console;

    #endregion

    public class Scrollbar
    {
        private static readonly Line Line;
        private static Vector2 _dragPosition;
        private static Vector2 _dragOffset;
        private static bool _dragStart;
        private static Color _backgroundColor;
        private static int _dragTop;
        private static int _width;

        static Scrollbar()
        {
            Line = new Line(Drawing.Direct3DDevice);
            Game.OnWndProc += OnGameWndProc;
        }

        public static Color BackgroundColor
        {
            get { return new Color(_backgroundColor.R, _backgroundColor.G, _backgroundColor.B, Alpha); }
            set { _backgroundColor = value; }
        }

        public static int Width
        {
            get { return _width; }
            set
            {
                _width = value;
                RaiseEvent(OnChange);
            }
        }

        public static int Height
        {
            get
            {
                var percent = ((float) Content.Height/Content.RealHeight)*100f;
                return (int) ((Content.Height*(percent > 100 ? 100 : percent))/100);
            }
        }

        internal static float Multiplicator
        {
            get
            {
                var percent = ((float) Content.Height/Content.RealHeight)*100f;
                return (int) Math.Ceiling(100/(percent > 100 ? 100 : percent));
            }
        }

        public static int Interval { get; set; }
        public static int Alpha { get; set; }

        public static Vector2 Offset
        {
            get { return new Vector2(Content.Offset.X + (Content.Width - Width)/2f, Content.Offset.Y); }
        }

        public static int DragTop
        {
            get { return _dragTop; }
            set
            {
                var val = value;
                if (val < 0)
                {
                    val = 0;
                }
                else if (value > (Content.Height - Height))
                {
                    val = Content.Height - Height;
                }
                _dragTop = val;
                RaiseEvent(OnChange);
            }
        }

        internal static event EventHandler OnChange;

        private static void RaiseEvent(EventHandler evt)
        {
            try
            {
                if (evt != null)
                    evt(null, null);
            }
            catch
            {
            }
        }

        private static void OnGameWndProc(WndEventArgs args)
        {
            if (Console.Hidden || Console.Minimized)
                return;

            if (args.Msg == 0x020A)
            {
                DragTop -= Interval*(unchecked((short) ((long) args.WParam >> 16))/120)*2;
            }
            if (args.Msg == (ulong) WindowsMessages.WM_KEYDOWN)
            {
                if (args.WParam == 40)
                {
                    DragTop += Interval;
                }
                if (args.WParam == 38)
                {
                    DragTop -= Interval;
                }
            }
            if (args.Msg == (ulong) WindowsMessages.WM_LBUTTONDOWN)
            {
                var p = Drawing.WorldToScreen(Game.CursorPos);
                if (Utils.IsUnderRectangle(p, Offset.X - Width/2f, Offset.Y + DragTop, Width, Height))
                {
                    _dragPosition = p;
                    _dragOffset = new Vector2(Offset.X, DragTop);
                    _dragStart = true;
                }
            }
            if (args.Msg == (ulong) WindowsMessages.WM_LBUTTONUP)
            {
                _dragStart = false;
            }
            if (!_dragStart || args.Msg != (ulong) WindowsMessages.WM_MOUSEMOVE)
            {
                return;
            }

            var pos = Drawing.WorldToScreen(Game.CursorPos);

            DragTop = (int) (pos.Y + _dragOffset.Y - _dragPosition.Y);
        }

        public static void PostReset()
        {
            Line.OnResetDevice();
        }

        public static void PreReset()
        {
            Line.OnLostDevice();
        }

        public static void EndScene()
        {
            if (Line.IsDisposed)
                return;

            Line.Width = Width;

            Line.Begin();

            Line.Draw(new[] {new Vector2(Offset.X, Offset.Y + DragTop), new Vector2(Offset.X, Offset.Y + DragTop + Height)}, BackgroundColor);

            Line.End();
        }
    }
}