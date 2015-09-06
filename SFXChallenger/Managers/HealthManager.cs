#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 HealthManager.cs is part of SFXChallenger.

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
    internal class HealthManager
    {
        private static readonly Dictionary<string, Tuple<Menu, HealthCheckType, HealthValueType>> Menues =
            new Dictionary<string, Tuple<Menu, HealthCheckType, HealthValueType>>();

        public static void AddToMenu(Menu menu,
            string uniqueId,
            HealthCheckType checkType,
            HealthValueType valueType,
            string prefix = null,
            int value = 30,
            int minValue = 0,
            int maxValue = 100)
        {
            try
            {
                if (Menues.ContainsKey(uniqueId))
                {
                    throw new ArgumentException(
                        string.Format("HealthHealthger: UniqueID \"{0}\" already exist.", uniqueId));
                }

                menu.AddItem(
                    new MenuItem(
                        menu.Name + ".health-" + uniqueId,
                        (!string.IsNullOrEmpty(prefix) ? prefix + " " : string.Empty) +
                        (checkType == HealthCheckType.Minimum ? "Min. Health" : "Max. Health") +
                        (valueType == HealthValueType.Percent ? " %" : string.Empty)).SetValue(
                            new Slider(value, minValue, maxValue)));

                Menues[uniqueId] = new Tuple<Menu, HealthCheckType, HealthValueType>(menu, checkType, valueType);
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
                Tuple<Menu, HealthCheckType, HealthValueType> tuple;
                if (Menues.TryGetValue(uniqueId, out tuple))
                {
                    var value = tuple.Item1.Item(tuple.Item1.Name + ".health-" + uniqueId).GetValue<Slider>().Value;
                    if (tuple.Item2 == HealthCheckType.Maximum)
                    {
                        return (tuple.Item3 == HealthValueType.Percent
                            ? ObjectManager.Player.HealthPercent <= value
                            : ObjectManager.Player.Health <= value);
                    }
                    return (tuple.Item3 == HealthValueType.Percent
                        ? ObjectManager.Player.HealthPercent >= value
                        : ObjectManager.Player.Health >= value);
                }
                throw new KeyNotFoundException(string.Format("HealthHealthger: UniqueID \"{0}\" not found.", uniqueId));
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return true;
        }
    }
}