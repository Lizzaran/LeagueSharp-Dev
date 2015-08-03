#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Bootstrap.cs is part of SFXClone.

 SFXClone is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXClone is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXClone. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using LeagueSharp.Common;
using SFXLibrary;
using SFXLibrary.Logger;
using SFXClone.Interfaces;

using SFXClone.Features.Drawings;

#endregion

namespace SFXClone
{
    public class Bootstrap
    {
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

                SetupLanguage();

                #region GameObjects

                GameObjects.Initialize();

                #endregion GameObjects

                Global.SFX = new SFXClone();

                

                var app = new App();

                CustomEvents.Game.OnGameLoad += delegate
                {
                    Global.Features.AddRange(
                        new List<IChild>
                        {
                            new Clone(app)
                        });
                    foreach (var feature in Global.Features)
                    {
                        try
                        {
                            feature.HandleEvents();
                        }
                        catch (Exception ex)
                        {
                            Global.Logger.AddItem(new LogItem(ex));
                        }
                    }
                    try
                    {
                        Update.Check(
                            Global.Name, Assembly.GetExecutingAssembly().GetName().Version, Global.UpdatePath, 10000);
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