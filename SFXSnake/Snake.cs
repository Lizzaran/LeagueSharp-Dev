#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Snake.cs is part of SFXSnake.

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
    internal class Snake
    {
        private readonly Map _map;
        private Direction _lastDirection = Direction.None;
        private Tile _lastTail;

        public Snake(Map map)
        {
            _map = map;
            Body = new List<Tile> { new Tile(1, Convert.ToInt32(_map.Y / 2)) };
        }

        public List<Tile> Body { get; private set; }

        public void Draw()
        {
            var first = true;
            foreach (var tile in Body)
            {
                var position = _map.Tile2Positon(tile);
                Drawing.DrawLine(
                    position.X, position.Y, position.X + _map.TileSize, position.Y, _map.TileSize,
                    first ? Color.Gray : Color.White);
                first = false;
            }
        }

        public bool Win()
        {
            return Body.Count == _map.X * _map.Y;
        }

        public bool DoMove(Direction direction)
        {
            var tail = Body.LastOrDefault();
            var head = Body.FirstOrDefault();
            if (tail != null && head != null)
            {
                switch (direction)
                {
                    case Direction.Up:
                        if (_lastDirection == Direction.Down)
                        {
                            direction = Direction.Down;
                        }
                        break;
                    case Direction.Right:
                        if (_lastDirection == Direction.Left)
                        {
                            direction = Direction.Left;
                        }
                        break;
                    case Direction.Down:
                        if (_lastDirection == Direction.Up)
                        {
                            direction = Direction.Up;
                        }
                        break;
                    case Direction.Left:
                        if (_lastDirection == Direction.Right)
                        {
                            direction = Direction.Right;
                        }
                        break;
                }
                var x = direction == Direction.Right ? 1 : (direction == Direction.Left ? -1 : 0);
                var y = direction == Direction.Down ? 1 : (direction == Direction.Up ? -1 : 0);
                var newPos = new Tile(head.X + x, head.Y + y);
                if (newPos.X < _map.X && newPos.X >= 0 && newPos.Y < _map.Y && newPos.Y >= 0 &&
                    !Body.Any(s => s.X == newPos.X && s.Y == newPos.Y))
                {
                    _lastTail = new Tile(tail.X, tail.Y);
                    Body.RemoveAt(Body.Count - 1);
                    Body.Insert(0, newPos);
                    _lastDirection = direction;
                    if (OnMove != null)
                    {
                        OnMove(null, null);
                    }
                    return true;
                }
            }
            return false;
        }

        public void Add()
        {
            Body.Add(_lastTail);
        }

        public void Reset()
        {
            _lastDirection = Direction.None;
            Body = new List<Tile> { new Tile(1, Convert.ToInt32(_map.Y / 2)) };
        }

        public event EventHandler<EventArgs> OnMove;
    }
}