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
    using LeagueSharp;
    using LeagueSharp.Common;
    using LeagueSharp.CommonEx.Core.Events;
    using SFXLibrary;
    using SFXLibrary.Data;
    using SFXLibrary.Extensions.NET;
    using SFXLibrary.IoCContainer;
    using SFXLibrary.Logger;
    using Version = System.Version;

    #endregion

    internal class SFXUtility
    {
        private readonly ILogger _logger;
        private bool _unloadFired;

        public SFXUtility(IContainer container)
        {
            try
            {
                _logger = container.Resolve<ILogger>();

                Menu = new Menu(Name, Name, true);

                var infoMenu = new Menu("Info", Name + "Info");
                infoMenu.AddItem(new MenuItem(Name + "InfoVersion", string.Format("Version: {0}", Version)));
                infoMenu.AddItem(new MenuItem(Name + "InfoForum", "Forum: Lizzaran"));
                infoMenu.AddItem(new MenuItem(Name + "InfoGithub", "GitHub: Lizzaran"));
                infoMenu.AddItem(new MenuItem(Name + "InfoIRC", "IRC: Appril"));

                infoMenu.AddSubMenu(infoMenu);

                AppDomain.CurrentDomain.DomainUnload += OnExit;
                AppDomain.CurrentDomain.ProcessExit += OnExit;
                CustomEvents.Game.OnGameEnd += OnGameEnd;
                Game.OnEnd += OnGameEnd;
                Game.OnNotify += OnGameNotify;
                Load.OnLoad += OnLoad;
            }
            catch (Exception ex)
            {
                _logger.AddItem(new LogItem(ex) {Object = this});
            }
        }

        public Menu Menu { get; private set; }

        public string Name
        {
            get { return "SFXUtility"; }
        }

        public Version Version
        {
            get { return new Version(0, 6, 5, 0); }
        }

        private void OnGameNotify(GameNotifyEventArgs args)
        {
            if (args.EventId == GameEventId.OnLeave || args.EventId == GameEventId.OnEndGame ||
                args.EventId == GameEventId.OnQuit)
            {
                try
                {
                    OnExit(null, null);
                }
                catch (Exception ex)
                {
                    _logger.AddItem(new LogItem(ex) {Object = this});
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
                _logger.AddItem(new LogItem(ex) {Object = this});
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
                _logger.AddItem(new LogItem(ex) {Object = this});
            }
        }

        private void OnLoad(EventArgs args)
        {
            try
            {
                var logger = _logger as ExceptionLogger;

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
                _logger.AddItem(new LogItem(ex) {Object = this});
            }
        }
    }
}