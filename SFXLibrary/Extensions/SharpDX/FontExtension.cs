#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 FontExtension.cs is part of SFXLibrary.

 SFXLibrary is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXLibrary is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXLibrary. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using SharpDX;
using SharpDX.Direct3D9;

#endregion

namespace SFXLibrary.Extensions.SharpDX
{
    public static class FontExtension
    {
        public static void DrawTextCentered(this Font font,
            string text,
            Vector2 position,
            Color color,
            bool outline = false)
        {
            var measure = font.MeasureText(null, text, FontDrawFlags.Center);
            if (outline)
            {
                font.DrawText(
                    null, text, (int) (position.X + 1 - measure.Width / 2f),
                    (int)(position.Y + 1 - measure.Height / 2f), Color.Black);
                font.DrawText(
                    null, text, (int) (position.X - 1 - measure.Width / 2f),
                    (int)(position.Y - 1 - measure.Height / 2f), Color.Black);
                font.DrawText(
                    null, text, (int) (position.X + 1 - measure.Width / 2f), (int) (position.Y - measure.Height / 2f),
                    Color.Black);
                font.DrawText(
                    null, text, (int) (position.X - 1 - measure.Width / 2f), (int) (position.Y - measure.Height / 2f),
                    Color.Black);
            }
            font.DrawText(
                null, text, (int)(position.X - measure.Width / 2f), (int)(position.Y - measure.Height / 2f), color);
        }

        public static void DrawTextCentered(this Font font, string text, int x, int y, Color color)
        {
            DrawTextCentered(font, text, new Vector2(x, y), color);
        }

        public static void DrawTextLeft(this Font font, string text, Vector2 position, Color color)
        {
            font.DrawText(
                null, text, (int) (position.X - font.MeasureText(null, text, FontDrawFlags.Center).Width),
                (int) (position.Y - font.MeasureText(null, text, FontDrawFlags.Center).Height / 2f), color);
        }

        public static void DrawTextLeft(this Font font, string text, int x, int y, Color color)
        {
            DrawTextLeft(font, text, new Vector2(x, y), color);
        }

        public static void DrawTextRight(this Font font, string text, Vector2 position, Color color)
        {
            font.DrawText(
                null, text, (int) (position.X + font.MeasureText(null, text, FontDrawFlags.Center).Width),
                (int) (position.Y - font.MeasureText(null, text, FontDrawFlags.Center).Height / 2f), color);
        }

        public static void DrawTextRight(this Font font, string text, int x, int y, Color color)
        {
            DrawTextRight(font, text, new Vector2(x, y), color);
        }
    }
}