#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Food.cs is part of SFXSnake.

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
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;

#endregion

namespace SFXSnake
{
    internal class Food
    {
        private readonly Map _map;
        private readonly Snake _snake;

        public Food(Map map, Snake snake)
        {
            _map = map;
            _snake = snake;
            _snake.OnMove += OnSnakeMove;
            Generate();
        }

        public Tile Position { get; private set; }

        private void OnSnakeMove(object sender, EventArgs eventArgs)
        {
            var head = _snake.Body.FirstOrDefault();
            if (head != null && head.X == Position.X && head.Y == Position.Y)
            {
                _snake.Add();
                if (OnEat != null)
                {
                    OnEat(null, null);
                }
                Generate();
            }
        }

        public void Reset()
        {
            Generate();
        }

        public void Generate()
        {
            Position = GetFreeTiles().OrderBy(x => Guid.NewGuid()).FirstOrDefault();
        }

        public void Draw()
        {
            var position = _map.Tile2Positon(Position);
            Drawing.DrawLine(
                position.X, position.Y, position.X + _map.TileSize, position.Y, _map.TileSize, Color.Yellow);
        }

        private IEnumerable<Tile> GetFreeTiles()
        {
            var freeTiles = new List<Tile>();
            for (var x = 0; x < _map.X; x++)
            {
                for (var y = 0; y < _map.Y; y++)
                {
                    if (!_snake.Body.Any(s => s.X == x && s.Y == y))
                    {
                        freeTiles.Add(new Tile(x, y));
                    }
                }
            }
            return freeTiles;
        }

        public event EventHandler<EventArgs> OnEat;
    }
}