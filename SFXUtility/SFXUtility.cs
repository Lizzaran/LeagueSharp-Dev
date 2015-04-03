#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 SFXUtility.cs is part of SFXUtility.

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
    using System.Reflection;
    using LeagueSharp;
    using LeagueSharp.Common;
    using SFXLibrary;
    using SFXLibrary.Data;
    using SFXLibrary.Extensions.NET;
    using SFXLibrary.Logger;
    using Version = System.Version;

    #endregion

    internal class SFXUtility
    {
        private bool _unloadFired;

        public SFXUtility()
        {
            try
            {
                Menu = new Menu(Name, Name, true);

                var infoMenu = new Menu("Info", Name + "Info");
                infoMenu.AddItem(new MenuItem(infoMenu.Name + "Version", string.Format("Version: {0}", Version)));
                infoMenu.AddItem(new MenuItem(infoMenu.Name + "Forum", "Forum: Lizzaran"));
                infoMenu.AddItem(new MenuItem(infoMenu.Name + "Github", "GitHub: Lizzaran"));
                infoMenu.AddItem(new MenuItem(infoMenu.Name + "IRC", "IRC: Appril"));

                infoMenu.AddSubMenu(infoMenu);

                AppDomain.CurrentDomain.DomainUnload += OnExit;
                AppDomain.CurrentDomain.ProcessExit += OnExit;
                CustomEvents.Game.OnGameEnd += OnGameEnd;
                Game.OnEnd += OnGameEnd;
                Game.OnNotify += OnGameNotify;
                CustomEvents.Game.OnGameLoad += OnGameLoad;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public Menu Menu { get; private set; }

        public string Name
        {
            get { return "SFXUtility"; }
        }

        public Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        private void OnGameNotify(GameNotifyEventArgs args)
        {
            if (args.EventId == GameEventId.OnLeave || args.EventId == GameEventId.OnEndGame || args.EventId == GameEventId.OnQuit)
            {
                try
                {
                    OnExit(null, null);
                }
                catch (Exception ex)
                {
                    Global.Logger.AddItem(new LogItem(ex));
                }
            }
        }

        public event EventHandler OnUnload;

        private void OnExit(object sender, EventArgs e)
        {
            try
            {
                if (!_unloadFired)
                {
                    OnUnload.RaiseEvent(null, null);
                    _unloadFired = true;
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnGameEnd(EventArgs args)
        {
            try
            {
                OnExit(null, null);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnGameLoad(EventArgs args)
        {
            try
            {
                var logger = Global.Logger as FileLogger;

                if (logger != null)
                {
                    logger.SensitiveData = Sensitive.Data;
                    logger.AdditionalData = Additional.Data;
                }

                Chat.Local(string.Format("{0} v{1}.{2}.{3} loaded.", Name, Version.Major, Version.Minor, Version.Build));

                Menu.AddToMainMenu();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex) {Object = this});
            }
        }
    }
}