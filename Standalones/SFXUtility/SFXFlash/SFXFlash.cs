#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 SFXFlash.cs is part of SFXFlash.

 SFXFlash is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXFlash is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXFlash. If not, see <http://www.gnu.org/licenses/>.
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
using SFXFlash.Classes;
using Version = System.Version;

#endregion

namespace SFXFlash
{
    public class SFXFlash
    {
        private bool _unloadTriggered;

        public SFXFlash()
        {
            try
            {
                Menu = new Menu(Name, Name, true);

                var infoMenu = new Menu(Global.Lang.Get("SFX_Info"), Name + "Info");

                infoMenu.AddItem(
                    new MenuItem(
                        infoMenu.Name + "Version", string.Format("{0}: {1}", Global.Lang.Get("SFX_Version"), Version)));
                infoMenu.AddItem(new MenuItem(infoMenu.Name + "Forum", Global.Lang.Get("SFX_Forum") + ": Lizzaran"));
                infoMenu.AddItem(new MenuItem(infoMenu.Name + "Github", Global.Lang.Get("SFX_GitHub") + ": Lizzaran"));
                infoMenu.AddItem(new MenuItem(infoMenu.Name + "IRC", Global.Lang.Get("SFX_IRC") + ": Appril"));
                infoMenu.AddItem(
                    new MenuItem(
                        infoMenu.Name + "Exception", string.Format("{0}: {1}", Global.Lang.Get("SFX_Exception"), 0)));

                var globalMenu = new Menu(Global.Lang.Get("SFX_Settings"), Name + "Settings");

                

                AddLanguage(globalMenu);
                AddReport(globalMenu);

                Menu.AddSubMenu(infoMenu);
                Menu.AddSubMenu(globalMenu);

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
            get { return Global.Lang.Get("F_SFX"); }
        }

        public Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        private void AddLanguage(Menu menu)
        {
            try
            {
                menu.AddItem(
                    new MenuItem(menu.Name + "Language", Global.Lang.Get("SFX_Language")).SetValue(
                        new StringList(
                            new[] { Global.Lang.Get("Language_Auto") }.Concat(Global.Lang.Languages.ToArray()).ToArray())))
                    .ValueChanged += delegate(object sender, OnValueChangeEventArgs args)
                    {
                        try
                        {
                            var preName = string.Format("{0}.language.", Global.Name.ToLower());
                            var autoName = Global.Lang.Get("Language_Auto");
                            var files = Directory.GetFiles(
                                AppDomain.CurrentDomain.BaseDirectory, preName + "*", SearchOption.TopDirectoryOnly);
                            var selectedLanguage = args.GetNewValue<StringList>().SelectedValue;
                            foreach (var file in files)
                            {
                                try
                                {
                                    File.Delete(file);
                                }
                                catch
                                {
                                    // ignored
                                }
                            }
                            if (!selectedLanguage.Equals(autoName, StringComparison.OrdinalIgnoreCase))
                            {
                                File.Create(
                                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, preName + selectedLanguage));
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
            try
            {
                var file =
                    Directory.GetFiles(
                        AppDomain.CurrentDomain.BaseDirectory,
                        string.Format("{0}.language.", Global.Name.ToLower()) + "*", SearchOption.TopDirectoryOnly)
                        .FirstOrDefault();
                if (file != null && Global.Lang.Languages.Any(l => l.Equals(file.Substring(1))))
                {
                    string ext = null;
                    var splitted = file.Split('.');
                    if (splitted.Any())
                    {
                        ext = splitted.Last();
                    }
                    if (!string.IsNullOrEmpty(ext))
                    {
                        menu.Item(menu.Name + "Language")
                            .SetValue(
                                new StringList(
                                    new[] { ext }.Concat(
                                        menu.Item(menu.Name + "Language")
                                            .GetValue<StringList>()
                                            .SList.Where(val => !val.Equals(ext, StringComparison.OrdinalIgnoreCase))
                                            .ToArray()).ToArray()));
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public event EventHandler<UnloadEventArgs> OnUnload;

        private void OnExit(object sender, EventArgs e)
        {
            try
            {
                if (!_unloadTriggered)
                {
                    _unloadTriggered = true;

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

        private void AddReport(Menu menu)
        {
            try
            {
                menu.AddItem(new MenuItem(menu.Name + "Report", Global.Lang.Get("SFX_GenerateReport")).SetValue(false))
                    .ValueChanged += delegate(object sender, OnValueChangeEventArgs args)
                    {
                        try
                        {
                            if (!args.GetNewValue<bool>())
                            {
                                return;
                            }
                            Utility.DelayAction.Add(0, () => menu.Item(menu.Name + "Report").SetValue(false));
                            File.WriteAllText(
                                Path.Combine(Global.BaseDir, string.Format("{0}.report.txt", Global.Name.ToLower())),
                                GenerateReport.Generate());
                            Notifications.AddNotification(Global.Lang.Get("SFX_ReportGenerated"), 5000);
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