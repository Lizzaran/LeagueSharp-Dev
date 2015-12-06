#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Map.cs is part of SFXSnake.

 SFXSnake is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXSnake is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXSnake. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using LeagueSharp;
using SharpDX;
using Color = System.Drawing.Color;

#endregion

namespace SFXSnake
{
    internal class Map
    {
        private const int Divider = 54;

        public Map(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; private set; }
        public int Y { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int TileSize { get; private set; }
        public Vector2 Center { get; private set; }

        public void Draw()
        {
            TileSize = Drawing.Height / Divider;
            Width = X * TileSize;
            Height = Y * TileSize;
            Center = GetCenter(Width, Height);
            Drawing.DrawLine(Center.X, Center.Y, Center.X + Width, Center.Y, Height, Color.Black);
            Drawing.DrawLine(Center.X - 2, Center.Y - 2, Center.X + Width + 2, Center.Y - 2, 2, Color.White);
            Drawing.DrawLine(Center.X - 2, Center.Y + Height, Center.X + Width + 2, Center.Y + Height, 2, Color.White);
            Drawing.DrawLine(Center.X - 1, Center.Y - 1, Center.X - 1, Center.Y + Height + 2, 2, Color.White);
            Drawing.DrawLine(
                Center.X + Width + 2, Center.Y - 1, Center.X + Height + 2, Center.Y + Height + 2, 2, Color.White);
        }

        private Vector2 GetCenter(int width, int height)
        {
            return new Vector2(Drawing.Width / 2 - width / 2, Drawing.Height / 2 - height / 2);
        }

        public Vector2 Tile2Positon(Tile tile)
        {
            return new Vector2(Center.X + tile.X * TileSize, Center.Y + tile.Y * TileSize);
        }
    }
}