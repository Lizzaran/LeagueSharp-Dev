#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Bootstrap.cs is part of SFXUtility.

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
using System.Collections.Generic;
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
using SFXUtility.Interfaces;

#endregion

namespace SFXUtility
{
    public class Bootstrap
    {
        public static void Init()
        {
            try
            {
                #region Upvote

                var upvoteItem = Upvote.Initialize(Global.Name, 7);

                #endregion Upvote

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

                Global.SFX = new SFXUtility();

                #region Upvote

                Global.SFX.Menu.SubMenu(Global.SFX.Name + "Settings").AddItem(upvoteItem);

                #endregion Upvote

                #region Parents

                var activators = new Activators();
                var detectors = new Detectors();
                var drawings = new Drawings();
                var events = new Events();
                var others = new Others();
                var timers = new Timers();
                var trackers = new Trackers();

                #endregion Parents

                var features = new List<IChild>
                {
                    #region Features
                    new KillSteal(activators),
                    new Potion(activators),
                    new Revealer(activators),
                    new Smite(activators),
                    new Gank(detectors),
                    new Replay(detectors),
                    new Teleport(detectors),
                    new Clock(drawings),
                    new Clone(drawings),
                    new DamageIndicator(drawings),
                    new Health(drawings),
                    new LasthitMarker(drawings),
                    new PerfectWard(drawings),
                    new Range(drawings),
                    new SafeJungleSpot(drawings),
                    new WallJumpSpot(drawings),
                    new Waypoint(drawings),
                    new AutoLeveler(events),
                    new Game(events),
                    new Trinket(events),
                    new AntiFountain(others),
                    new AutoLantern(others),
                    new Flash(others),
                    new Humanize(others),
                    new MoveTo(others),
                    new Ping(others),
                    new SkinChanger(others),
                    new TurnAround(others),
                    new Ability(timers),
                    new Altar(timers),
                    new Cooldown(timers),
                    new Relic(timers),
                    new Inhibitor(timers),
                    new Jungle(timers),
                    new GoldEfficiency(trackers),
                    new Destination(trackers),
                    new LastPosition(trackers),
                    new Sidebar(trackers),
                    new Ward(trackers)
                    #endregion Features
                };

                CustomEvents.Game.OnGameLoad += delegate
                {
                    foreach (var feature in features)
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
                    Update.Check(
                        Global.Name, Assembly.GetExecutingAssembly().GetName().Version, Global.UpdatePath, 10000);
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