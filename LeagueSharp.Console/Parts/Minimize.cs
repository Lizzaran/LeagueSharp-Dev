#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Minimize.cs is part of LeagueSharp.Console.

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

    public class Minimize
    {
        private static readonly Line Line;
        private static Color _backgroundColor;
        private static Color _foregroundColor;

        static Minimize()
        {
            Line = new Line(Drawing.Direct3DDevice);

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

        public static int Height
        {
            get { return Header.Height; }
        }

        public static int Width
        {
            get { return Header.Height; }
        }

        public static string Content { get; set; }

        public static Vector2 Offset
        {
            get { return new Vector2(Header.Offset.X + Header.Width/2f - Width/2f, Header.Offset.Y); }
        }

        public static Vector2 MinimizedOffset
        {
            get { return new Vector2((int) ((Drawing.Width - Width/2f)*0.98), (int) (Drawing.Height*0.08)); }
        }

        public static int Alpha { get; set; }

        private static void OnGameWndProc(WndEventArgs args)
        {
            if (Console.Hidden)
                return;

            if (args.Msg == (ulong) WindowsMessages.WM_LBUTTONDOWN)
            {
                var p = Drawing.WorldToScreen(Game.CursorPos);
                var offset = Console.Minimized ? MinimizedOffset : Offset;
                if (Utils.IsUnderRectangle(p, offset.X - Width/2f, offset.Y, Width, Height))
                {
                    Console.Menu.Item(Console.Menu.Name + "Minimized").SetValue(!Console.Menu.Item(Console.Menu.Name + "Minimized").GetValue<bool>());
                }
            }
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

            var offset = Console.Minimized ? MinimizedOffset : Offset;

            Line.Width = Width;

            Line.Begin();

            Line.Draw(new[] {new Vector2(offset.X, offset.Y), new Vector2(offset.X, offset.Y + Height)}, BackgroundColor);

            Line.End();

            Line.Width = Height/4f;

            Line.Begin();

            Line.Draw(new[] {new Vector2(offset.X - Width/4f, offset.Y + Height/2f), new Vector2(offset.X + Width/4f, offset.Y + Height/2f)},
                ForegroundColor);

            Line.End();
        }
    }
}