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

#region

using System;
using System.Collections.Generic;
using LeagueSharp;
using LeagueSharp.Common;
using SFXChallenger.Enumerations;
using SFXChallenger.Library.Logger;

#endregion

namespace SFXChallenger.Managers
{
    internal class ManaManager
    {
        private static readonly Dictionary<string, Tuple<Menu, ManaCheckType, ManaValueType>> Menues =
            new Dictionary<string, Tuple<Menu, ManaCheckType, ManaValueType>>();

        public static void AddToMenu(Menu menu,
            string uniqueId,
            ManaCheckType checkType,
            ManaValueType valueType,
            string prefix = null,
            int value = 30,
            int minValue = 0,
            int maxValue = 100)
        {
            try
            {
                if (Menues.ContainsKey(uniqueId))
                {
                    throw new ArgumentException(string.Format("ManaManager: UniqueID \"{0}\" already exist.", uniqueId));
                }

                menu.AddItem(
                    new MenuItem(
                        menu.Name + ".mana-" + uniqueId,
                        (!string.IsNullOrEmpty(prefix) ? prefix + " " : string.Empty) +
                        (checkType == ManaCheckType.Minimum ? "Min. Mana" : "Max. Mana") +
                        (valueType == ManaValueType.Percent ? " %" : string.Empty)).SetValue(
                            new Slider(value, minValue, maxValue)));

                Menues[uniqueId] = new Tuple<Menu, ManaCheckType, ManaValueType>(menu, checkType, valueType);
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
                Tuple<Menu, ManaCheckType, ManaValueType> tuple;
                if (Menues.TryGetValue(uniqueId, out tuple))
                {
                    var value = tuple.Item1.Item(tuple.Item1.Name + ".mana-" + uniqueId).GetValue<Slider>().Value;
                    if (tuple.Item2 == ManaCheckType.Maximum)
                    {
                        return (tuple.Item3 == ManaValueType.Percent
                            ? ObjectManager.Player.ManaPercent <= value
                            : ObjectManager.Player.Mana <= value);
                    }
                    return (tuple.Item3 == ManaValueType.Percent
                        ? ObjectManager.Player.ManaPercent >= value
                        : ObjectManager.Player.Mana >= value);
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