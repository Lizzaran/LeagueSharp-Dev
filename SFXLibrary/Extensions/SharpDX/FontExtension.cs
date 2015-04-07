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

using Color = SharpDX.Color;
using Font = SharpDX.Direct3D9.Font;
using FontDrawFlags = SharpDX.Direct3D9.FontDrawFlags;
using Vector2 = SharpDX.Vector2;

#endregion

namespace SFXLibrary.Extensions.SharpDX
{
    public static class FontExtension
    {
        public static void DrawTextCentered(this Font font, string text, Vector2 position, Color color)
        {
            font.DrawText(null, text, (int) (position.X - font.MeasureText(null, text, FontDrawFlags.Center).Width/2f),
                (int) (position.Y - font.MeasureText(null, text, FontDrawFlags.Center).Height/2f), color);
        }

        public static void DrawTextCentered(this Font font, string text, int x, int y, Color color)
        {
            DrawTextCentered(font, text, new Vector2(x, y), color);
        }
    }
}