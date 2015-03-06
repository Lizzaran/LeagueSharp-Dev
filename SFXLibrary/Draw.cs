#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Draw.cs is part of SFXLibrary.

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

namespace SFXLibrary
{
    #region

    using LeagueSharp;
    using SharpDX;
    using Color = System.Drawing.Color;

    #endregion

    public class Draw
    {
        public static void Cross(Vector2 pos, float size, float thickness, Color color)
        {
            Drawing.DrawLine(pos.X - size, pos.Y - size, pos.X + size, pos.Y + size, thickness, color);
            Drawing.DrawLine(pos.X + size, pos.Y - size, pos.X - size, pos.Y + size, thickness, color);
        }

        public static void TextCentered(Vector2 pos, Color color, string content)
        {
            var rec = Drawing.GetTextExtent(content);
            Drawing.DrawText(pos.X - rec.Width/2f, pos.Y - rec.Height/2f, color, content);
        }
    }
}