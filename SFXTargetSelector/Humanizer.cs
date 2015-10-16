#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Humanizer.cs is part of SFXTargetSelector.

 SFXTargetSelector is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXTargetSelector is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXTargetSelector. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

#endregion

namespace SFXTargetSelector
{
    public class Humanizer
    {
        private static Menu _mainMenu;

        internal static void AddToMenu(Menu mainMenu)
        {
            _mainMenu = mainMenu;

            _mainMenu.AddItem(
                new MenuItem(_mainMenu.Name + ".fow", "Target Acquire Delay").SetShared()
                    .SetValue(new Slider(350, 0, 1500)));
        }

        public static IEnumerable<Targets.Item> FilterTargets(IEnumerable<Targets.Item> targets)
        {
            var finalTargets = targets.ToList();
            var fowDelay = _mainMenu.Item(_mainMenu.Name + ".fow").GetValue<Slider>().Value;
            if (fowDelay > 0)
            {
                finalTargets =
                    finalTargets.Where(item => Game.Time - item.LastVisibleChange > fowDelay / 1000f).ToList();
            }
            return finalTargets;
        }
    }
}