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
    using System.IO;
    using System.Linq;
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

                Menu.AddItem(
                    new MenuItem(Name + "Language", Language.Get("SFXUtility_Language")).SetValue(
                        new StringList(new[] {"auto"}.Concat(Language.Languages.ToArray()).ToArray())));

                var infoMenu = new Menu(Language.Get("SFXUtility_Info"), Name + "Info");

                infoMenu.AddItem(new MenuItem(infoMenu.Name + "Version", string.Format("{0}: {1}", Language.Get("SFXUtility_Version"), Version)));
                infoMenu.AddItem(new MenuItem(infoMenu.Name + "Forum", Language.Get("SFXUtility_Forum") + ": Lizzaran"));
                infoMenu.AddItem(new MenuItem(infoMenu.Name + "Github", Language.Get("SFXUtility_GitHub") + ": Lizzaran"));
                infoMenu.AddItem(new MenuItem(infoMenu.Name + "IRC", Language.Get("SFXUtility_IRC") + ": Appril"));

                infoMenu.AddSubMenu(infoMenu);

                Menu.Item(Name + "Language").ValueChanged += delegate(object sender, OnValueChangeEventArgs args)
                {
                    const string preName = "sfxutility.language.";
                    var files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, preName + "*", SearchOption.TopDirectoryOnly);
                    foreach (var file in files)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch
                        {
                        }
                    }
                    if (!args.GetNewValue<StringList>().SelectedValue.Equals("auto"))
                        File.Create(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, preName + args.GetNewValue<StringList>().SelectedValue));
                };

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
            get { return Language.Get("F_SFXUtility"); }
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

        public event EventHandler<UnloadEventArgs> OnUnload;

        private void OnExit(object sender, EventArgs e)
        {
            try
            {
                if (!_unloadFired)
                {
                    OnUnload.RaiseEvent(null, new UnloadEventArgs(true));
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

                Chat.Local(string.Format("{0} v{1}.{2}.{3} {4}.", Name, Version.Major, Version.Minor, Version.Build, Language.Get("SFXUtility_Loaded")));

                Menu.AddToMainMenu();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex) {Object = this});
            }
        }
    }

    public class UnloadEventArgs : EventArgs
    {
        public bool Real;

        public UnloadEventArgs(bool real = false)
        {
            Real = real;
        }
    }
}