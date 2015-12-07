#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Bootstrap.cs is part of ChampionTemplate.

 ChampionTemplate is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 ChampionTemplate is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with ChampionTemplate. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Linq;
using System.Reflection;
using ChampionTemplate.Helpers;
using ChampionTemplate.Interfaces;
using LeagueSharp;
using LeagueSharp.Common;

#endregion

namespace ChampionTemplate
{
    public class Bootstrap
    {
        private static IChampion _champion;

        public static void Init()
        {
            CustomEvents.Game.OnGameLoad += delegate
            {
                _champion = LoadChampion();
                if (_champion != null)
                {
                    // Start and initialize the core which is responsible for calling the different modes such as Combo, Harass, LaneClear etc.
                    Core.Init(_champion, 50);
                    Core.Boot();
                }
            };
        }

        private static IChampion LoadChampion()
        {
            /*
             * Search in every file of the assembly and filter it by the following statements:
             * - Has to be a class
             * - Can't be abstact
             * - Must implement the "IChampion" interface
             * - Class name must equal Champion Name, not case sensitive
             */

            var type =
                Assembly.GetAssembly(typeof(IChampion))
                    .GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && typeof(IChampion).IsAssignableFrom(t))
                    .FirstOrDefault(
                        t => t.Name.Equals(ObjectManager.Player.ChampionName, StringComparison.OrdinalIgnoreCase));

            // If champion class found, load / initialize it.
            return type != null ? (IChampion) DynamicInitializer.NewInstance(type) : null;
        }
    }
}