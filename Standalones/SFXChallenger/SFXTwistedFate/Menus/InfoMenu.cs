#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 InfoMenu.cs is part of SFXTwistedFate.

 SFXTwistedFate is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXTwistedFate is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXTwistedFate. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Reflection;
using LeagueSharp.Common;
using SFXLibrary.Logger;

#endregion

namespace SFXTwistedFate.Menus
{
    internal class InfoMenu
    {
        public static void AddToMenu(Menu menu)
        {
            try
            {
                menu.AddItem(
                    new MenuItem(
                        menu.Name + ".version",
                        string.Format(
                            "{0}: {1}", Global.Lang.Get("Info_Version"), Assembly.GetEntryAssembly().GetName().Version)));
                menu.AddItem(new MenuItem(menu.Name + ".forum", Global.Lang.Get("Info_Forum") + ": Lizzaran"));
                menu.AddItem(new MenuItem(menu.Name + ".github", Global.Lang.Get("Info_GitHub") + ": Lizzaran"));
                menu.AddItem(new MenuItem(menu.Name + ".irc", Global.Lang.Get("Info_IRC") + ": Appril"));
                menu.AddItem(
                    new MenuItem(
                        menu.Name + ".exception", string.Format("{0}: {1}", Global.Lang.Get("Info_Exception"), 0)));

                var errorText = Global.Lang.Get("Info_Exception");
                Global.Logger.OnItemAdded += delegate
                {
                    try
                    {
                        var text = menu.Item(menu.Name + ".exception")
                            .DisplayName.Replace(errorText + ": ", string.Empty);
                        int count;
                        if (int.TryParse(text, out count))
                        {
                            menu.Item(menu.Name + ".exception").DisplayName = string.Format(
                                "{0}: {1}", errorText, count + 1);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                };

                Core.OnShutdown +=
                    delegate
                    {
                        Notifications.AddNotification(new Notification(menu.Item(menu + ".exception").DisplayName));
                    };
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }
}