#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Tile.cs is part of SFXSnake.

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

namespace SFXSnake
{
    internal class Tile
    {
        public Tile(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; set; }
        public int Y { get; set; }
    }
}