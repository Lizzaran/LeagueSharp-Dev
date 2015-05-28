#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Program.cs is part of SFXUtility.

 SFXUtility is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXUtility is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXUtility. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using SFXLibrary.Logger;
using SFXUtility.Feature;
using SFXUtility.Features.Activators;
using SFXUtility.Features.Detectors;
using SFXUtility.Features.Drawings;
using SFXUtility.Features.Events;
using SFXUtility.Features.Others;
using SFXUtility.Features.Timers;
using SFXUtility.Features.Trackers;
using Object = SFXUtility.Features.Timers.Object;

#endregion

namespace SFXUtility
{
    internal class Program
    {
        // ReSharper disable once UnusedParameter.Local
        private static void Main(string[] args)
        {
            try
            {
                Global.Logger.LogLevel = LogLevel.High;

                AppDomain.CurrentDomain.UnhandledException +=
                    delegate(object sender, UnhandledExceptionEventArgs eventArgs)
                    {
                        var ex = sender as Exception;
                        if (ex != null)
                        {
                            Global.Logger.AddItem(new LogItem(ex));
                        }
                    };

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
                        AppDomain.CurrentDomain.BaseDirectory, @"sfxutility.language.*", SearchOption.TopDirectoryOnly)
                        .Select(Path.GetExtension)
                        .FirstOrDefault();
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
                Console.WriteLine(ex);
            }

            Global.IoC.Register(() => new SFXUtility(), true, true);

            Global.IoC.Register(() => new Activators(), true, true);
            Global.IoC.Register(() => new BushRevealer(), true, true);
            Global.IoC.Register(() => new InvisibilityRevealer(), true, true);
            Global.IoC.Register(() => new KillSteal(), true, true);
            Global.IoC.Register(() => new Potion(), true, true);
            Global.IoC.Register(() => new Smite(), true, true);

            Global.IoC.Register(() => new Detectors(), true, true);
            Global.IoC.Register(() => new Gank(), true, true);
            Global.IoC.Register(() => new Replay(), true, true);
            Global.IoC.Register(() => new SharedExperience(), true, true);
            Global.IoC.Register(() => new Teleport(), true, true);

            Global.IoC.Register(() => new Drawings(), true, true);
            Global.IoC.Register(() => new Clock(), true, true);
            Global.IoC.Register(() => new Clone(), true, true);
            Global.IoC.Register(() => new DamageIndicator(), true, true);
            Global.IoC.Register(() => new Health(), true, true);
            Global.IoC.Register(() => new LasthitMarker(), true, true);
            Global.IoC.Register(() => new PerfectWard(), true, true);
            Global.IoC.Register(() => new Range(), true, true);
            Global.IoC.Register(() => new SafeJungleSpot(), true, true);
            Global.IoC.Register(() => new WallJumpSpot(), true, true);
            Global.IoC.Register(() => new Waypoint(), true, true);

            Global.IoC.Register(() => new Events(), true, true);
            Global.IoC.Register(() => new AutoLeveler(), true, true);
            Global.IoC.Register(() => new Game(), true, true);
            Global.IoC.Register(() => new Trinket(), true, true);

            Global.IoC.Register(() => new Others(), true, true);
            Global.IoC.Register(() => new AntiTrap(), true, true);
            Global.IoC.Register(() => new AutoLantern(), true, true);
            Global.IoC.Register(() => new ExtendFlash(), true, true);
            Global.IoC.Register(() => new Humanize(), true, true);
            Global.IoC.Register(() => new Ping(), true, true);
            Global.IoC.Register(() => new SkinChanger(), true, true);
            Global.IoC.Register(() => new SummonerInfo(), true, true);
            Global.IoC.Register(() => new TurnAround(), true, true);

            Global.IoC.Register(() => new Timers(), true, true);
            Global.IoC.Register(() => new Ability(), true, true);
            Global.IoC.Register(() => new Cooldown(), true, true);
            Global.IoC.Register(() => new Jungle(), true, true);
            Global.IoC.Register(() => new Object(), true, true);

            Global.IoC.Register(() => new Trackers(), true, true);
            Global.IoC.Register(() => new GoldEfficiency(), true, true);
            Global.IoC.Register(() => new Destination(), true, true);
            Global.IoC.Register(() => new LastPosition(), true, true);
            Global.IoC.Register(() => new Sidebar(), true, true);
            Global.IoC.Register(() => new Ward(), true, true);
        }
    }
}