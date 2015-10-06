#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Bootstrap.cs is part of SFXChallenger.

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
using System.Linq;
using System.Reflection;
using LeagueSharp;
using LeagueSharp.Common;
using SFXChallenger.Helpers;
using SFXChallenger.Interfaces;
using SFXChallenger.Library;
using SFXChallenger.Library.Logger;
using SFXChallenger.SFXTargetSelector;

#endregion

namespace SFXChallenger
{
    public class Bootstrap
    {
        private static IChampion _champion;

        public static void Init()
        {
            try
            {
                AppDomain.CurrentDomain.UnhandledException +=
                    delegate(object sender, UnhandledExceptionEventArgs eventArgs)
                    {
                        try
                        {
                            var ex = sender as Exception;
                            if (ex != null)
                            {
                                Global.Logger.AddItem(new LogItem(ex));
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    };

                GameObjects.Initialize();

                CustomEvents.Game.OnGameLoad += delegate
                {
                    try
                    {
                        _champion = LoadChampion();

                        if (_champion != null)
                        {
                            if (Global.Reset.Enabled)
                            {
                                Reset.Force(Global.Name, Global.Reset.MaxAge, Weights.RestoreDefaultWeights);
                            }
                            Update.Check(
                                Global.Name, Assembly.GetExecutingAssembly().GetName().Version, Global.UpdatePath, 10000);
                            Core.Init(_champion, 50);
                            Core.Boot();
                        }
                    }
                    catch (Exception ex)
                    {
                        Global.Logger.AddItem(new LogItem(ex));
                    }
                };
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private static IChampion LoadChampion()
        {
            try
            {
                var type =
                    Assembly.GetAssembly(typeof(IChampion))
                        .GetTypes()
                        .Where(t => t.IsClass && !t.IsAbstract && typeof(IChampion).IsAssignableFrom(t))
                        .FirstOrDefault(
                            t => t.Name.Equals(ObjectManager.Player.ChampionName, StringComparison.OrdinalIgnoreCase));

                return type != null ? (IChampion) DynamicInitializer.NewInstance(type) : null;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return null;
        }
    }
}