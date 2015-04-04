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

namespace SFXUtility
{
    #region

    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Feature;
    using Features.Activators;
    using Features.Detectors;
    using Features.Drawings;
    using Features.Events;
    using Features.Others;
    using Features.Timers;
    using Features.Trackers;
    using SFXLibrary;
    using SFXLibrary.Logger;
    using Object = Features.Timers.Object;

    #endregion

    internal class Program
    {
        // ReSharper disable once UnusedParameter.Local
        private static void Main(string[] args)
        {
            Language.Parse(Utils.ReadResourceString("SFXUtility.Resources.languages.xml", Assembly.GetExecutingAssembly()));

            var langFile = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, @"sfxutility.language.*").FirstOrDefault();
            if (langFile != null && Language.Languages.Any(l => l.Equals(Path.GetExtension(langFile))))
                Language.Current = Path.GetExtension(langFile);
            else
                Language.Current = CultureInfo.InstalledUICulture.TwoLetterISOLanguageName;

            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs eventArgs)
            {
                var ex = sender as Exception ?? new NotSupportedException("Unhandled exception doesn't derive from System.Exception: " + sender);
                Global.Logger.AddItem(new LogItem(ex));
            };

            Global.IoC.Register(typeof (SFXUtility), () => new SFXUtility(), true, true);

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
            Global.IoC.Register(() => new Sidebar(), true, true);
            Global.IoC.Register(() => new WallJumpSpot(), true, true);
            Global.IoC.Register(() => new Waypoint(), true, true);

            Global.IoC.Register(() => new Events(), true, true);
            Global.IoC.Register(() => new AutoLeveler(), true, true);
            Global.IoC.Register(() => new Game(), true, true);
            Global.IoC.Register(() => new Trinket(), true, true);

            Global.IoC.Register(() => new Others(), true, true);
            Global.IoC.Register(() => new AntiFountain(), true, true);
            Global.IoC.Register(() => new AntiTrap(), true, true);
            Global.IoC.Register(() => new AutoLantern(), true, true);
            Global.IoC.Register(() => new ExtendFlash(), true, true);
            Global.IoC.Register(() => new Humanize(), true, true);
            Global.IoC.Register(() => new SummonerInfo(), true, true);
            Global.IoC.Register(() => new TurnAround(), true, true);

            Global.IoC.Register(() => new Timers(), true, true);
            Global.IoC.Register(() => new Ability(), true, true);
            Global.IoC.Register(() => new Cooldown(), true, true);
            Global.IoC.Register(() => new Jungle(), true, true);
            Global.IoC.Register(() => new Object(), true, true);

            Global.IoC.Register(() => new Trackers(), true, true);
            Global.IoC.Register(() => new Destination(), true, true);
            Global.IoC.Register(() => new LastPosition(), true, true);
            Global.IoC.Register(() => new Ward(), true, true);
        }
    }
}