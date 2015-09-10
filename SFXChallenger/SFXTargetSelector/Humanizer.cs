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

#endregion

namespace SFXChallenger.SFXTargetSelector
{
    internal class Humanizer
    {
        private static Menu _mainMenu;

        internal static void AddToMenu(Menu mainMenu)
        {
            try
            {
                _mainMenu = mainMenu;

                _mainMenu.AddItem(
                    new MenuItem(_mainMenu.Name + ".fow", "Target Acquire Delay").SetValue(new Slider(500, 0, 1500)));
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static IEnumerable<Targets.Item> FilterTargets(IEnumerable<Targets.Item> targets)
        {
            var fowDelay = (float) _mainMenu.Item(_mainMenu.Name + ".fow").GetValue<Slider>().Value;
            if (fowDelay > 0)
            {
                fowDelay = fowDelay / 1000f;
            }
            if (fowDelay > 0.0f)
            {
                return targets.Where(item => Game.Time - item.LastVisibleChange > fowDelay);
            }
            return targets;
        }
    }
}