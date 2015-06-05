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

#region

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using LeagueSharp.Common;
using SFXLibrary.Extensions.NET;
using SFXLibrary.Logger;
using Version = System.Version;

#endregion

namespace SFXUtility
{
    internal class SFXUtility
    {
        private bool _unloadTriggered;

        public SFXUtility()
        {
            try
            {
                Menu = new Menu(Name, Name, true);

                Menu.AddItem(
                    new MenuItem(Name + "Font", Global.Lang.Get("SFXUtility_Font")).SetValue(
                        new StringList(
                            new[]
                            {
                                "Calibri", "Arial", "Tahoma", "Verdana", "Times New Roman", "Lucida Console",
                                "Comic Sans MS"
                            })));
                Menu.AddItem(
                    new MenuItem(Name + "Language", Global.Lang.Get("SFXUtility_Language")).SetValue(
                        new StringList(
                            new[] { Global.Lang.Get("Language_Auto") }.Concat(Global.Lang.Languages.ToArray()).ToArray())));

                Global.DefaultFont = Menu.Item(Name + "Font").GetValue<StringList>().SelectedValue;

                var infoMenu = new Menu(Global.Lang.Get("SFXUtility_Info"), Name + "Info");

                infoMenu.AddItem(
                    new MenuItem(
                        infoMenu.Name + "Version",
                        string.Format("{0}: {1}", Global.Lang.Get("SFXUtility_Version"), Version)));
                infoMenu.AddItem(
                    new MenuItem(infoMenu.Name + "Forum", Global.Lang.Get("SFXUtility_Forum") + ": Lizzaran"));
                infoMenu.AddItem(
                    new MenuItem(infoMenu.Name + "Github", Global.Lang.Get("SFXUtility_GitHub") + ": Lizzaran"));
                infoMenu.AddItem(new MenuItem(infoMenu.Name + "IRC", Global.Lang.Get("SFXUtility_IRC") + ": Appril"));
                infoMenu.AddItem(
                    new MenuItem(
                        infoMenu.Name + "Exception", string.Format("{0}: {1}", Global.Lang.Get("SFX_Exception"), 0)));

                Menu.AddSubMenu(infoMenu);

                try
                {
                    var file =
                        Directory.GetFiles(
                            AppDomain.CurrentDomain.BaseDirectory, "sfxutility.language.*",
                            SearchOption.TopDirectoryOnly).FirstOrDefault();
                    if (!string.IsNullOrEmpty(file))
                    {
                        var ext = Path.GetExtension(file);
                        if (!string.IsNullOrEmpty(ext))
                        {
                            ext = ext.RightSubstring(ext.Length - 1);
                            Menu.Item(Menu.Name + "Language")
                                .SetValue(
                                    new StringList(
                                        new[] { ext }.Concat(
                                            Menu.Item(Menu.Name + "Language")
                                                .GetValue<StringList>()
                                                .SList.Where(val => val != ext)
                                                .ToArray()).ToArray()));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Global.Logger.AddItem(new LogItem(ex));
                }

                AppDomain.CurrentDomain.DomainUnload += OnExit;
                AppDomain.CurrentDomain.ProcessExit += OnExit;
                CustomEvents.Game.OnGameEnd += OnGameEnd;
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
            get { return Global.Lang.Get("F_SFXUtility"); }
        }

        public Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        public event EventHandler<UnloadEventArgs> OnUnload;

        private void OnExit(object sender, EventArgs e)
        {
            try
            {
                if (!_unloadTriggered)
                {
                    _unloadTriggered = true;

                    try
                    {
                        var preName = "sfxutility.language.";
                        var autoName = Global.Lang.Get("Language_Auto");
                        var files = Directory.GetFiles(
                            AppDomain.CurrentDomain.BaseDirectory, preName + "*", SearchOption.TopDirectoryOnly);
                        var selectedLanguage = Menu.Item(Menu.Name + "Language").GetValue<StringList>().SelectedValue;
                        foreach (var file in files)
                        {
                            try
                            {
                                File.Delete(file);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex);
                            }
                        }
                        if (!selectedLanguage.Equals(autoName, StringComparison.OrdinalIgnoreCase))
                        {
                            File.Create(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, preName + selectedLanguage));
                        }
                    }
                    catch (Exception ex)
                    {
                        Global.Logger.AddItem(new LogItem(ex));
                    }

                    OnUnload.RaiseEvent(null, new UnloadEventArgs(true));
                    Notifications.AddNotification(new Notification(Menu.Item(Name + "InfoException").DisplayName));
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnGameEnd(EventArgs args)
        {
            OnExit(null, args);
        }

        private void OnGameLoad(EventArgs args)
        {
            try
            {
                Menu.AddToMainMenu();

                var errorText = Global.Lang.Get("SFX_Exception");
                Global.Logger.OnItemAdded += delegate
                {
                    try
                    {
                        var text = Menu.Item(Name + "InfoException").DisplayName.Replace(errorText + ": ", string.Empty);
                        int count;
                        if (int.TryParse(text, out count))
                        {
                            Menu.Item(Name + "InfoException").DisplayName = string.Format(
                                "{0}: {1}", errorText, count + 1);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                };
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }

    public class UnloadEventArgs : EventArgs
    {
        public bool Final;

        public UnloadEventArgs(bool final = false)
        {
            Final = final;
        }
    }
}