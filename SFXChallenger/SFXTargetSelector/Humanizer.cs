#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Humanizer.cs is part of SFXChallenger.

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
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXChallenger.Library.Logger;
using SharpDX;

#endregion

namespace SFXChallenger.SFXTargetSelector
{
    internal class Humanizer
    {
        private static Menu _mainMenu;
        private static float _lastRange;
        private static float _lastRangeChange;

        internal static void AddToMenu(Menu mainMenu)
        {
            try
            {
                _mainMenu = mainMenu;

                _mainMenu.AddItem(
                    new MenuItem(_mainMenu.Name + ".fow", "Target Acquire Delay").SetShared()
                        .SetValue(new Slider(400, 0, 1500)));
                _mainMenu.AddItem(
                    new MenuItem(_mainMenu.Name + ".range", "Range Change Delay").SetShared()
                        .SetValue(new Slider(400, 0, 1500)));
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static IEnumerable<Targets.Item> FilterTargets(IEnumerable<Targets.Item> targets,
            Vector3 from,
            float range)
        {
            var rangeDelay = _mainMenu.Item(_mainMenu.Name + ".range").GetValue<Slider>().Value;
            var fowDelay = _mainMenu.Item(_mainMenu.Name + ".fow").GetValue<Slider>().Value;
            if (rangeDelay > 0 && range > 0)
            {
                if (_lastRange > 0 && Game.Time - _lastRangeChange <= rangeDelay / 1000f)
                {
                    targets =
                        targets.Where(
                            t =>
                                t.Hero.Distance(
                                    from.Equals(default(Vector3)) ? ObjectManager.Player.ServerPosition : from) <=
                                _lastRange);
                }
                else
                {
                    _lastRange = range;
                    _lastRangeChange = Game.Time;
                }
            }
            if (fowDelay > 0)
            {
                targets = targets.Where(item => Game.Time - item.LastVisibleChange > fowDelay / 1000f);
            }
            return targets;
        }
    }
}