#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 UltimateMenu.cs is part of SFXChallenger.

 SFXChallenger is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXChallenger is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXChallenger. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using LeagueSharp.Common;
using SFXChallenger.Managers;
using SFXLibrary.Logger;

#endregion

namespace SFXChallenger.Menus
{
    internal class UltimateMenu
    {
        public static Menu AddToMenu(Menu menu,
            bool auto,
            bool autoInterrupt,
            bool autoGapcloser,
            bool flash,
            bool assisted,
            bool whitelist,
            bool force)
        {
            try
            {
                var ultimateMenu = menu.AddSubMenu(new Menu(Global.Lang.Get("G_Ultimate"), menu.Name + ".ultimate"));

                var uComboMenu =
                    ultimateMenu.AddSubMenu(new Menu(Global.Lang.Get("G_Combo"), ultimateMenu.Name + ".combo"));
                uComboMenu.AddItem(
                    new MenuItem(uComboMenu.Name + ".min", "R " + Global.Lang.Get("G_Min")).SetValue(
                        new Slider(3, 1, 5)));
                uComboMenu.AddItem(new MenuItem(uComboMenu.Name + ".1v1", "R 1v1").SetValue(true));
                uComboMenu.AddItem(
                    new MenuItem(uComboMenu.Name + ".enabled", Global.Lang.Get("G_Enabled")).SetValue(true));

                if (auto)
                {
                    var uAutoMenu =
                        ultimateMenu.AddSubMenu(new Menu(Global.Lang.Get("G_Auto"), ultimateMenu.Name + ".auto"));
                    if (autoInterrupt)
                    {
                        var autoInterruptMenu =
                            uAutoMenu.AddSubMenu(
                                new Menu(Global.Lang.Get("G_InterruptSpell"), uAutoMenu.Name + ".interrupt"));
                        HeroListManager.AddToMenu(autoInterruptMenu, "ultimate-interrupt", false, false, true, false);
                    }
                    if (autoGapcloser)
                    {
                        var autoGapcloserMenu =
                            uAutoMenu.AddSubMenu(
                                new Menu(Global.Lang.Get("G_Gapcloser"), uAutoMenu.Name + ".gapcloser"));
                        HeroListManager.AddToMenu(autoGapcloserMenu, "ultimate-gapcloser", false, false, true, false);
                    }
                    uAutoMenu.AddItem(
                        new MenuItem(uAutoMenu.Name + ".min", "R " + Global.Lang.Get("G_Min")).SetValue(
                            new Slider(3, 1, 5)));
                    uAutoMenu.AddItem(new MenuItem(uAutoMenu.Name + ".1v1", "R 1v1").SetValue(false));
                    uAutoMenu.AddItem(
                        new MenuItem(uAutoMenu.Name + ".enabled", Global.Lang.Get("G_Enabled")).SetValue(true));
                }

                if (flash)
                {
                    var uFlashMenu =
                        ultimateMenu.AddSubMenu(new Menu(Global.Lang.Get("G_Flash"), ultimateMenu.Name + ".flash"));
                    uFlashMenu.AddItem(
                        new MenuItem(uFlashMenu.Name + ".min", "R " + Global.Lang.Get("G_Min")).SetValue(
                            new Slider(3, 1, 5)));
                    uFlashMenu.AddItem(new MenuItem(uFlashMenu.Name + ".1v1", "R 1v1").SetValue(true));
                    uFlashMenu.AddItem(
                        new MenuItem(uFlashMenu.Name + ".hotkey", Global.Lang.Get("G_Hotkey")).SetValue(
                            new KeyBind('U', KeyBindType.Press)));
                    uFlashMenu.AddItem(
                        new MenuItem(uFlashMenu.Name + ".move-cursor", Global.Lang.Get("G_MoveCursor")).SetValue(true));
                    uFlashMenu.AddItem(
                        new MenuItem(uFlashMenu.Name + ".enabled", Global.Lang.Get("G_Enabled")).SetValue(true));
                }

                if (assisted)
                {
                    var uAssistedMenu =
                        ultimateMenu.AddSubMenu(
                            new Menu(Global.Lang.Get("G_Assisted"), ultimateMenu.Name + ".assisted"));
                    uAssistedMenu.AddItem(
                        new MenuItem(uAssistedMenu.Name + ".min", "R " + Global.Lang.Get("G_Min")).SetValue(
                            new Slider(3, 1, 5)));
                    uAssistedMenu.AddItem(new MenuItem(uAssistedMenu.Name + ".1v1", "R 1v1").SetValue(true));
                    uAssistedMenu.AddItem(
                        new MenuItem(uAssistedMenu.Name + ".hotkey", Global.Lang.Get("G_Hotkey")).SetValue(
                            new KeyBind('R', KeyBindType.Press)));
                    uAssistedMenu.AddItem(
                        new MenuItem(uAssistedMenu.Name + ".move-cursor", Global.Lang.Get("G_MoveCursor")).SetValue(
                            true));
                    uAssistedMenu.AddItem(
                        new MenuItem(uAssistedMenu.Name + ".enabled", Global.Lang.Get("G_Enabled")).SetValue(true));
                }

                if (whitelist)
                {
                    var uWhitelistMenu =
                        ultimateMenu.AddSubMenu(
                            new Menu(Global.Lang.Get("G_Whitelist"), ultimateMenu.Name + ".whitelist"));
                    HeroListManager.AddToMenu(uWhitelistMenu, "ultimate-whitelist", true, false, true, true);
                }

                if (force)
                {
                    var uForceMenu =
                        ultimateMenu.AddSubMenu(new Menu(Global.Lang.Get("G_Force"), ultimateMenu.Name + ".force"));
                    uForceMenu.AddItem(
                        new MenuItem(uForceMenu.Name + ".additional", Global.Lang.Get("G_Additional")).SetValue(
                            new Slider(0, 0, 4)));
                    HeroListManager.AddToMenu(uForceMenu, "ultimate-force", true, false, true, false);
                }

                return ultimateMenu;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return null;
        }
    }
}