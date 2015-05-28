#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Score.cs is part of SFXSnake.

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

using System;
using System.Drawing;
using LeagueSharp;

#endregion

namespace SFXSnake
{
    internal class Score
    {
        private readonly Map _map;

        public Score(Map map, Food food)
        {
            Points = 0;
            _map = map;
            food.OnEat += FoodOnEat;
        }

        public int Points { get; private set; }

        private void FoodOnEat(object sender, EventArgs eventArgs)
        {
            Points++;
        }

        public void Reset()
        {
            Points = 0;
        }

        public void Draw()
        {
            var position = _map.Tile2Positon(new Tile(0, -2));
            Drawing.DrawText(position.X, position.Y, Color.GreenYellow, string.Format("Score: {0}", Points));
        }
    }
}