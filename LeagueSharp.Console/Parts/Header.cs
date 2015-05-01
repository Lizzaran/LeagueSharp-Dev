#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 header.cs is part of LeagueSharp.Console.

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

    using Common;
    using SharpDX;
    using SharpDX.Direct3D9;

    #endregion

    public class Header
    {
        private static readonly Line Line;
        private static Font _font;
        private static Vector2 _dragPosition;
        private static Vector2 _dragOffset;
        private static bool _dragStart;
        private static Color _backgroundColor;
        private static Color _foregroundColor;

        static Header()
        {
            Line = new Line(Drawing.Direct3DDevice);
            _font = new Font(Drawing.Direct3DDevice,
                new FontDescription {FaceName = "Calibri", Height = 18, OutputPrecision = FontPrecision.Default, Quality = FontQuality.Default});

            Game.OnWndProc += OnGameWndProc;
        }

        public static Color BackgroundColor
        {
            get { return new Color(_backgroundColor.R, _backgroundColor.G, _backgroundColor.B, Alpha); }
            set { _backgroundColor = value; }
        }

        public static Color ForegroundColor
        {
            get { return new Color(_foregroundColor.R, _foregroundColor.G, _foregroundColor.B, Alpha); }
            set { _foregroundColor = value; }
        }

        public static int Height { get; set; }

        public static int Width
        {
            get { return Console.Width; }
        }

        public static string Content { get; set; }

        public static Vector2 Offset
        {
            get { return new Vector2(Console.Offset.X, Console.Offset.Y); }
        }

        public static int FontHeight
        {
            get { return _font.Description.Height; }
            set
            {
                _font = new Font(Drawing.Direct3DDevice,
                    new FontDescription {FaceName = FontName, Height = value, OutputPrecision = FontPrecision.Default, Quality = FontQuality.Default});
            }
        }

        public static int Alpha { get; set; }

        public static string FontName
        {
            get { return _font.Description.FaceName; }
            set
            {
                _font = new Font(Drawing.Direct3DDevice,
                    new FontDescription
                    {
                        FaceName = value,
                        Height = FontHeight,
                        OutputPrecision = FontPrecision.Default,
                        Quality = FontQuality.Default
                    });
            }
        }

        private static void OnGameWndProc(WndEventArgs args)
        {
            if (Console.Hidden || Console.Minimized)
                return;

            if (args.Msg == (ulong) WindowsMessages.WM_LBUTTONDOWN)
            {
                var p = Drawing.WorldToScreen(Game.CursorPos);
                if (Utils.IsUnderRectangle(p, Offset.X - Width/2f, Offset.Y, Width, Height))
                {
                    _dragPosition = p;
                    _dragOffset = Console.Offset;
                    _dragStart = true;
                }
            }
            if (args.Msg == (ulong) WindowsMessages.WM_LBUTTONUP)
            {
                _dragStart = false;
            }
            if (!_dragStart || args.Msg != (ulong) WindowsMessages.WM_MOUSEMOVE)
            {
                Console.IsMoving = false;
                return;
            }

            Console.IsMoving = true;
            var pos = Drawing.WorldToScreen(Game.CursorPos);

            Console.Menu.Item(Console.Menu.Name + "OffsetLeft")
                .SetValue(new Slider((int) (pos.X + _dragOffset.X - _dragPosition.X), 0, Drawing.Width));
            Console.Menu.Item(Console.Menu.Name + "OffsetTop")
                .SetValue(new Slider((int) (pos.Y + _dragOffset.Y - _dragPosition.Y), 0, Drawing.Height));
        }

        public static void PostReset()
        {
            _font.OnResetDevice();
            Line.OnResetDevice();
        }

        public static void PreReset()
        {
            _font.OnLostDevice();
            Line.OnLostDevice();
        }

        public static void EndScene()
        {
            if (Line.IsDisposed || _font.IsDisposed)
                return;

            Line.Width = Width;

            Line.Begin();

            Line.Draw(new[] {new Vector2(Offset.X, Offset.Y), new Vector2(Offset.X, Offset.Y + Height)}, BackgroundColor);

            Line.End();

            _font.DrawText(null, Content, (int) Offset.X - Width/2 + 10,
                (int) Offset.Y + Height/2 - (_font.MeasureText(null, Content, FontDrawFlags.Center).Height/2), ForegroundColor);
        }
    }
}