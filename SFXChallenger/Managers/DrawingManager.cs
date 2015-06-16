#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 DrawingManager.cs is part of SFXChallenger.

 SFXChallenger is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXChallenger is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXChallenger. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXChallenger.Interfaces;
using SFXLibrary.Extensions.SharpDX;
using SFXLibrary.Logger;

#endregion

namespace SFXChallenger.Managers
{
    internal class DrawingManager
    {
        private static Menu _menu;
        private static IChampion _champion;
        private static bool _seperator;
        private static readonly Dictionary<string, float> Customs = new Dictionary<string, float>();
        private static readonly Dictionary<string, MenuItem> Others = new Dictionary<string, MenuItem>();

        public static void AddToMenu(Menu menu, IChampion champion)
        {
            try
            {
                _champion = champion;
                _menu = menu;

                _menu.AddItem(
                    new MenuItem(_menu.Name + ".circle-thickness", Global.Lang.Get("G_CircleThickness")).SetValue(
                        new Slider(2, 0, 10)));

                foreach (var spell in _champion.Spells.Where(s => s != null))
                {
                    _menu.AddItem(
                        new MenuItem(
                            _menu.Name + "." + spell.Slot.ToString().ToLower(), spell.Slot.ToString().ToUpper())
                            .SetValue(new Circle(false, Color.White)));
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static void Add(string name, float range)
        {
            try
            {
                if (!_seperator)
                {
                    _menu.AddItem(new MenuItem(_menu.Name + ".seperator", string.Empty));
                    _seperator = true;
                }
                var key = name.Trim().ToLower();
                if (Customs.ContainsKey(key))
                {
                    throw new ArgumentException(string.Format("DrawingManager: Name \"{0}\" already exist.", name));
                }

                _menu.AddItem(new MenuItem(_menu.Name + "." + key, name).SetValue(new Circle(false, Color.White)));

                Customs[key] = range;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static MenuItem Add<T>(string name, T value)
        {
            try
            {
                if (!_seperator)
                {
                    _menu.AddItem(new MenuItem(_menu.Name + ".seperator", string.Empty));
                    _seperator = true;
                }
                var key = name.Trim().ToLower();
                if (Others.ContainsKey(key))
                {
                    throw new ArgumentException(string.Format("DrawingManager: Name \"{0}\" already exist.", name));
                }
                var item = new MenuItem(_menu.Name + "." + key, name).SetValue(value);
                _menu.AddItem(item);

                Others[key] = item;

                return item;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return null;
        }

        public static MenuItem Get(string name)
        {
            var key = name.Trim().ToLower();
            MenuItem value;
            if (!Others.TryGetValue(key, out value))
            {
                throw new ArgumentException(string.Format("DrawingManager: Name \"{0}\" not found.", name));
            }
            return value;
        }

        public static void Update(string name, float range)
        {
            try
            {
                var key = name.Trim().ToLower();
                if (!Customs.ContainsKey(key))
                {
                    throw new ArgumentException(string.Format("DrawingManager: Name \"{0}\" not found.", name));
                }
                Customs[key] = range;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static void Draw()
        {
            try
            {
                if (_menu == null || _champion.Spells == null || ObjectManager.Player.IsDead)
                {
                    return;
                }

                var circleThickness = _menu.Item(_menu.Name + ".circle-thickness").GetValue<Slider>().Value;
                foreach (var spell in _champion.Spells)
                {
                    var item = _menu.Item(_menu.Name + "." + spell.Slot.ToString().ToLower()).GetValue<Circle>();
                    if (item.Active && ObjectManager.Player.Position.IsOnScreen(spell.Range))
                    {
                        Render.Circle.DrawCircle(
                            ObjectManager.Player.Position, spell.Range, item.Color, circleThickness);
                    }
                }

                foreach (var custom in Customs)
                {
                    var item = _menu.Item(_menu.Name + "." + custom.Key).GetValue<Circle>();
                    if (item.Active && ObjectManager.Player.Position.IsOnScreen(custom.Value))
                    {
                        Render.Circle.DrawCircle(
                            ObjectManager.Player.Position, custom.Value, item.Color, circleThickness);
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }
}