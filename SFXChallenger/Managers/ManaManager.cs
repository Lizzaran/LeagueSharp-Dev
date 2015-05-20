#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 ManaManager.cs is part of SFXChallenger.

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

namespace SFXChallenger.Managers
{
    #region

    using System;
    using System.Collections.Generic;
    using LeagueSharp;
    using LeagueSharp.Common;
    using SFXLibrary.Logger;

    #endregion

    internal class ManaManager
    {
        private static readonly Dictionary<string, Menu> Menues = new Dictionary<string, Menu>();

        public static void AddToMenu(Menu menu, string uniqueId, int value = 30, int minValue = 0, int maxValue = 100)
        {
            try
            {
                if (Menues.ContainsKey(uniqueId))
                    throw new ArgumentException(string.Format("ManaManager: UniqueID \"{0}\" already exist.", uniqueId));

                menu.AddItem(
                    new MenuItem(menu.Name + ".min-mana-" + uniqueId, Global.Lang.Get("MM_MinMana")).SetValue(new Slider(value, minValue, maxValue)));

                Menues[uniqueId] = menu;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static bool Check(string uniqueId)
        {
            try
            {
                Menu menu;
                if (Menues.TryGetValue(uniqueId, out menu))
                {
                    return !(ObjectManager.Player.ManaPercent < menu.Item(menu.Name + ".min-mana-" + uniqueId).GetValue<Slider>().Value);
                }
                throw new KeyNotFoundException(string.Format("ManaManager: UniqueID \"{0}\" not found.", uniqueId));
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return true;
        }
    }
}