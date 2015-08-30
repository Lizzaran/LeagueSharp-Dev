#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Bootstrap.cs is part of SFXKogMaw.

 SFXKogMaw is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXKogMaw is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXKogMaw. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using LeagueSharp;
using LeagueSharp.Common;
using SFXKogMaw.Helpers;
using SFXKogMaw.Interfaces;
using SFXLibrary;
using SFXLibrary.Logger;

#endregion

namespace SFXKogMaw
{
    public class Bootstrap
    {
        private static IChampion _champion;

        public static void Init()
        {
            try
            {
                var upvoteItem = Upvote.Initialize(Global.Name, 7);

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

                SetupLanguage();

                GameObjects.Initialize();

                CustomEvents.Game.OnGameLoad += delegate
                {
                    try
                    {
                        _champion = LoadChampion();

                        if (_champion != null)
                        {
                            try
                            {
                                Update.Check(
                                    Global.Name, Assembly.GetExecutingAssembly().GetName().Version, Global.UpdatePath,
                                    10000);
                            }
                            catch (Exception ex)
                            {
                                Global.Logger.AddItem(new LogItem(ex));
                            }
                            Core.Init(_champion, 50);
                            Core.Boot();

                            if (_champion.SFXMenu != null && upvoteItem != null)
                            {
                                _champion.SFXMenu.SubMenu(_champion.SFXMenu.Name + ".settings").AddItem(upvoteItem);
                            }
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

        private static void SetupLanguage()
        {
            try
            {
                Global.Lang.Default = "en";

                var currentAsm = Assembly.GetExecutingAssembly();
                foreach (var resName in currentAsm.GetManifestResourceNames())
                {
                    ResourceReader resReader = null;
                    using (var stream = currentAsm.GetManifestResourceStream(resName))
                    {
                        if (stream != null)
                        {
                            resReader = new ResourceReader(stream);
                        }

                        if (resReader != null)
                        {
                            var en = resReader.GetEnumerator();

                            while (en.MoveNext())
                            {
                                if (en.Key.ToString().StartsWith("language_"))
                                {
                                    Global.Lang.Parse(en.Value.ToString());
                                }
                            }
                        }
                    }
                }

                var lang =
                    Directory.GetFiles(
                        AppDomain.CurrentDomain.BaseDirectory, string.Format(@"{0}.language.*", Global.Name.ToLower()),
                        SearchOption.TopDirectoryOnly).Select(Path.GetExtension).FirstOrDefault();
                if (lang != null && Global.Lang.Languages.Any(l => l.Equals(lang.Substring(1))))
                {
                    Global.Lang.Current = lang.Substring(1);
                }
                else
                {
                    Global.Lang.Current = CultureInfo.InstalledUICulture.TwoLetterISOLanguageName;
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }
}