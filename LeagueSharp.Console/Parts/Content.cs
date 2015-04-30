#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Content.cs is part of LeagueSharp.Console.

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
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using System.Windows;
    using System.Windows.Media;
    using SharpDX;
    using SharpDX.Direct3D9;
    using Color = SharpDX.Color;
    using Console = Console;
    using Drawing = Drawing;

    #endregion

    public class Content
    {
        private static readonly Line Line;
        private static Font _font;
        private static Color _foregroundColor;
        private static Color _backgroundColor;

        static Content()
        {
            Line = new Line(Drawing.Direct3DDevice);
            _font = new Font(Drawing.Direct3DDevice,
                new FontDescription {FaceName = "Calibri", Height = 18, OutputPrecision = FontPrecision.Default, Quality = FontQuality.Default});
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

        public static int Padding { get; set; }

        public static int FontHeight
        {
            get { return _font.Description.Height; }
            set
            {
                _font = new Font(Drawing.Direct3DDevice,
                    new FontDescription {FaceName = FontName, Height = value, OutputPrecision = FontPrecision.Default, Quality = FontQuality.Default});
            }
        }

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

        public static int Alpha { get; set; }

        public static Vector2 Offset
        {
            get { return new Vector2(Console.Offset.X, Console.Offset.Y + Header.Height); }
        }

        public static int Width
        {
            get { return Console.Width; }
        }

        public static int Height
        {
            get { return Console.Height - Header.Height; }
        }

        public static int RealHeight
        {
            get
            {
                return
                    _font.MeasureText(null, Console.Output,
                        new Rectangle((int) (Offset.X - Width/2f) + Padding, (int) Offset.Y + Padding, Width - Scrollbar.Width - Padding*2,
                            int.MaxValue), FontDrawFlags.Left | FontDrawFlags.WordBreak).Height;
            }
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

            if (!Console.IsMoving)
            {
                _font.DrawText(null, GetText(),
                    new Rectangle((int) (Offset.X - Width/2f) + Padding, (int) Offset.Y + Padding, Width - Scrollbar.Width - Padding*2,
                        Height - Padding*2), FontDrawFlags.Left, ForegroundColor);
            }
        }

        private static string GetText()
        {
            var wrapped = WrapText(Console.Output, Width - Scrollbar.Width - Padding*2, FontName, FontHeight);
            var lines = (int) ((Height - Padding*2)/Scrollbar.FontHeight);
            var offset = (int)((Scrollbar.DragTop / Scrollbar.FontHeight) * Scrollbar.Multiplicator);
            return string.Join(Environment.NewLine,
                wrapped.GetRange(offset, offset + lines > wrapped.Count ? wrapped.Count - offset : lines).ToArray());
        }

        private static List<string> WrapText(string text, int width, string fontName, float fontHeight)
        {
            var originalLines = text.Split(new[] {" "}, StringSplitOptions.None);

            var wrappedLines = new List<string>();

            var actualLine = new StringBuilder();
            double actualWidth = 0;

            foreach (var item in originalLines)
            {
                var formatted = new FormattedText(item, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(fontName), fontHeight,
                    Brushes.Black);

                actualLine.Append(item + " ");
                actualWidth += formatted.Width;

                if (actualWidth > width)
                {
                    wrappedLines.Add(actualLine.ToString());
                    actualLine.Clear();
                    actualWidth = 0;
                }
            }

            if (actualLine.Length > 0)
                wrappedLines.Add(actualLine.ToString());

            return wrappedLines;
        }
    }
}