#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 SFXSnake.cs is part of SFXSnake.

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
using System.Timers;
using LeagueSharp;
using LeagueSharp.Common;

#endregion

namespace SFXSnake
{
    internal class SFXSnake
    {
        private Direction _direction = Direction.None;
        private Food _food;
        private Map _map;
        private Menu _menu;
        private Timer _onTickTimer;
        private bool _pauseWait;
        private Score _score;
        private Snake _snake;

        public SFXSnake()
        {
            CustomEvents.Game.OnGameLoad += OnLoad;
        }

        private void OnLoad(EventArgs args)
        {
            _map = new Map(20, 20);
            _snake = new Snake(_map);
            _food = new Food(_map, _snake);
            _score = new Score(_map, _food);

            _menu = new Menu("SFXSnake", "SFXSnake", true);
            _menu.AddItem(new MenuItem(_menu.Name + "Speed", "Speed").SetValue(new Slider(200, 25, 500)));
            _menu.AddItem(new MenuItem(_menu.Name + "Hotkey", "Hotkey").SetValue(new KeyBind('I', KeyBindType.Toggle)));

            _menu.Item(_menu.Name + "Speed").ValueChanged += delegate(object sender, OnValueChangeEventArgs eventArgs)
            {
                if (_onTickTimer != null)
                {
                    _onTickTimer.Interval = eventArgs.GetNewValue<Slider>().Value;
                }
            };

            _menu.AddToMainMenu();

            Game.OnWndProc += OnWndProc;
            Drawing.OnDraw += OnDrawingDraw;

            _onTickTimer = new Timer(_menu.Item(_menu.Name + "Speed").GetValue<Slider>().Value);
            _onTickTimer.Elapsed += OnTick;
            _onTickTimer.Start();
        }

        private void OnWndProc(WndEventArgs args)
        {
            try
            {
                switch (args.WParam)
                {
                    case (uint) Key.VK_KEY_W:
                    case (uint) Key.VK_UP:
                        if (args.Msg == (uint) Key.WM_KEYDOWN)
                        {
                            _direction = Direction.Up;
                            _pauseWait = false;
                        }
                        break;
                    case (uint) Key.VK_KEY_D:
                    case (uint) Key.VK_RIGHT:
                        if (args.Msg == (uint) Key.WM_KEYDOWN)
                        {
                            _direction = Direction.Right;
                            _pauseWait = false;
                        }
                        break;
                    case (uint) Key.VK_KEY_S:
                    case (uint) Key.VK_DOWN:
                        if (args.Msg == (uint) Key.WM_KEYDOWN)
                        {
                            _direction = Direction.Down;
                            _pauseWait = false;
                        }
                        break;
                    case (uint) Key.VK_KEY_A:
                    case (uint) Key.VK_LEFT:
                        if (args.Msg == (uint) Key.WM_KEYDOWN)
                        {
                            _direction = Direction.Left;
                            _pauseWait = false;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void OnTick(object state, ElapsedEventArgs elapsedEventArgs)
        {
            try
            {
                if (!_menu.Item(_menu.Name + "Hotkey").GetValue<KeyBind>().Active || _pauseWait)
                {
                    return;
                }

                if (!_snake.DoMove(_direction) || _snake.Win())
                {
                    _pauseWait = true;
                    _snake.Reset();
                    _food.Reset();
                    _score.Reset();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void OnDrawingDraw(EventArgs args)
        {
            try
            {
                if (!_menu.Item(_menu.Name + "Hotkey").GetValue<KeyBind>().Active)
                {
                    _pauseWait = true;
                    return;
                }

                _map.Draw();
                _food.Draw();
                _snake.Draw();
                _score.Draw();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}