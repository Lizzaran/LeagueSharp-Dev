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
using LeagueSharp.Common;
using SFXLibrary;
using SFXLibrary.Logger;
using SFXUtility.Features.Activators;
using SFXUtility.Features.Detectors;
using SFXUtility.Features.Drawings;
using SFXUtility.Features.Events;
using SFXUtility.Features.Others;
using SFXUtility.Features.Timers;
using SFXUtility.Features.Trackers;

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
                Upvote.Initialize(Global.Name, 7);

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

                Update.Check(Global.Name, Assembly.GetExecutingAssembly().GetName().Version, Global.UpdatePath, 10000);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            GameObjects.Initialize();

            CustomEvents.Game.OnGameLoad += delegate
            {
                var sfx = new SFXUtility();
                Global.IoC.Register(() => sfx, true, true);

                Global.IoC.Register(() => new Activators(sfx), true, true);
                Global.IoC.Register(() => new KillSteal(sfx), true, true);
                Global.IoC.Register(() => new Potion(sfx), true, true);
                //Global.IoC.Register(() => new Revealer(sfx), true, true);
                Global.IoC.Register(() => new Smite(sfx), true, true);

                Global.IoC.Register(() => new Detectors(sfx), true, true);
                Global.IoC.Register(() => new Gank(sfx), true, true);
                Global.IoC.Register(() => new Replay(sfx), true, true);
                //Global.IoC.Register(() => new SharedExperience(sfx), true, true);
                Global.IoC.Register(() => new Teleport(sfx), true, true);

                Global.IoC.Register(() => new Drawings(sfx), true, true);
                Global.IoC.Register(() => new Clock(sfx), true, true);
                Global.IoC.Register(() => new Clone(sfx), true, true);
                Global.IoC.Register(() => new DamageIndicator(sfx), true, true);
                Global.IoC.Register(() => new Health(sfx), true, true);
                Global.IoC.Register(() => new LasthitMarker(sfx), true, true);
                Global.IoC.Register(() => new PerfectWard(sfx), true, true);
                Global.IoC.Register(() => new Range(sfx), true, true);
                Global.IoC.Register(() => new SafeJungleSpot(sfx), true, true);
                Global.IoC.Register(() => new WallJumpSpot(sfx), true, true);
                Global.IoC.Register(() => new Waypoint(sfx), true, true);

                Global.IoC.Register(() => new Events(sfx), true, true);
                Global.IoC.Register(() => new AutoLeveler(sfx), true, true);
                Global.IoC.Register(() => new Game(sfx), true, true);
                Global.IoC.Register(() => new Trinket(sfx), true, true);

                Global.IoC.Register(() => new Others(sfx), true, true);
                Global.IoC.Register(() => new AutoLantern(sfx), true, true);
                Global.IoC.Register(() => new ExtendFlash(sfx), true, true);
                Global.IoC.Register(() => new Humanize(sfx), true, true);
                Global.IoC.Register(() => new MoveTo(sfx), true, true);
                Global.IoC.Register(() => new Ping(sfx), true, true);
                Global.IoC.Register(() => new SkinChanger(sfx), true, true);
                Global.IoC.Register(() => new TurnAround(sfx), true, true);

                Global.IoC.Register(() => new Timers(sfx), true, true);
                Global.IoC.Register(() => new Ability(sfx), true, true);
                Global.IoC.Register(() => new Altar(sfx), true, true);
                Global.IoC.Register(() => new Cooldown(sfx), true, true);
                Global.IoC.Register(() => new Relic(sfx), true, true);
                Global.IoC.Register(() => new Inhibitor(sfx), true, true);
                Global.IoC.Register(() => new Jungle(sfx), true, true);

                Global.IoC.Register(() => new Trackers(sfx), true, true);
                Global.IoC.Register(() => new GoldEfficiency(sfx), true, true);
                Global.IoC.Register(() => new Destination(sfx), true, true);
                Global.IoC.Register(() => new LastPosition(sfx), true, true);
                Global.IoC.Register(() => new Sidebar(sfx), true, true);
                Global.IoC.Register(() => new Ward(sfx), true, true);
            };
        }
    }
}