#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 UltimateManager.cs is part of SFXChallenger.

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
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXLibrary;
using SFXLibrary.Logger;

#endregion

namespace SFXChallenger.Managers
{
    internal class UltimateManager
    {
        private static Menu _menu;
        private static bool _auto;
        private static bool _interrupt;
        private static bool _gapcloser;
        private static bool _flash;
        private static bool _assisted;

        public static Menu AddToMenu(Menu menu,
            bool auto,
            bool autoInterrupt,
            bool interruptDelay,
            bool autoGapcloser,
            bool gapcloserDelay,
            bool flash,
            bool assisted,
            bool required,
            bool force)
        {
            try
            {
                _auto = auto;
                _interrupt = autoInterrupt;
                _gapcloser = autoGapcloser;
                _flash = flash;
                _assisted = assisted;
                _menu = menu;

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
                        ultimateMenu.AddSubMenu(new Menu(Global.Lang.Get("UM_Auto"), ultimateMenu.Name + ".auto"));
                    if (autoInterrupt)
                    {
                        var autoInterruptMenu =
                            uAutoMenu.AddSubMenu(
                                new Menu(Global.Lang.Get("G_InterruptSpell"), uAutoMenu.Name + ".interrupt"));
                        if (interruptDelay)
                        {
                            DelayManager.AddToMenu(
                                autoInterruptMenu, "ultimate-interrupt-delay", string.Empty, 250, 0, 1000);
                        }
                        HeroListManager.AddToMenu(autoInterruptMenu, "ultimate-interrupt", false, false, true, false);
                    }
                    if (autoGapcloser)
                    {
                        var autoGapcloserMenu =
                            uAutoMenu.AddSubMenu(
                                new Menu(Global.Lang.Get("G_Gapcloser"), uAutoMenu.Name + ".gapcloser"));
                        if (gapcloserDelay)
                        {
                            DelayManager.AddToMenu(
                                autoGapcloserMenu, "ultimate-gapcloser-delay", string.Empty, 250, 0, 1000);
                        }
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
                        new MenuItem(uFlashMenu.Name + ".move-cursor", Global.Lang.Get("UM_MoveCursor")).SetValue(true));
                    uFlashMenu.AddItem(
                        new MenuItem(uFlashMenu.Name + ".enabled", Global.Lang.Get("G_Enabled")).SetValue(true));
                }

                if (assisted)
                {
                    var uAssistedMenu =
                        ultimateMenu.AddSubMenu(
                            new Menu(Global.Lang.Get("UM_Assisted"), ultimateMenu.Name + ".assisted"));
                    uAssistedMenu.AddItem(
                        new MenuItem(uAssistedMenu.Name + ".min", "R " + Global.Lang.Get("G_Min")).SetValue(
                            new Slider(3, 1, 5)));
                    uAssistedMenu.AddItem(new MenuItem(uAssistedMenu.Name + ".1v1", "R 1v1").SetValue(true));
                    uAssistedMenu.AddItem(
                        new MenuItem(uAssistedMenu.Name + ".hotkey", Global.Lang.Get("G_Hotkey")).SetValue(
                            new KeyBind('R', KeyBindType.Press)));
                    uAssistedMenu.AddItem(
                        new MenuItem(uAssistedMenu.Name + ".move-cursor", Global.Lang.Get("UM_MoveCursor")).SetValue(
                            true));
                    uAssistedMenu.AddItem(
                        new MenuItem(uAssistedMenu.Name + ".enabled", Global.Lang.Get("G_Enabled")).SetValue(true));
                }

                if (required)
                {
                    var uRequiredListMenu =
                        ultimateMenu.AddSubMenu(
                            new Menu(Global.Lang.Get("UM_RequiredTarget"), ultimateMenu.Name + ".required"));
                    uRequiredListMenu.AddItem(
                        new MenuItem(uRequiredListMenu.Name + ".range-check", Global.Lang.Get("UM_RangeCheck")).SetValue
                            (new Slider(2000, 1000, 3000)));
                    HeroListManager.AddToMenu(uRequiredListMenu, "ultimate-required", true, false, true, false);
                }

                if (force)
                {
                    var uForceMenu =
                        ultimateMenu.AddSubMenu(
                            new Menu(Global.Lang.Get("UM_ForceTarget"), ultimateMenu.Name + ".force"));
                    uForceMenu.AddItem(
                        new MenuItem(uForceMenu.Name + ".additional", Global.Lang.Get("UM_AdditionalTargets")).SetValue(
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

        public static bool Combo()
        {
            return _menu != null && _menu.Item(_menu.Name + ".ultimate.combo.enabled").GetValue<bool>();
        }

        public static bool Auto()
        {
            return _menu != null && _menu.Item(_menu.Name + ".ultimate.auto.enabled").GetValue<bool>();
        }

        public static bool Interrupt(Obj_AI_Hero hero)
        {
            return _menu != null && _menu.Item(_menu.Name + ".ultimate.auto.enabled").GetValue<bool>() &&
                   HeroListManager.Check("ultimate-interrupt", hero);
        }

        public static bool Gapcloser(Obj_AI_Hero hero)
        {
            return _menu != null && _menu.Item(_menu.Name + ".ultimate.auto.enabled").GetValue<bool>() &&
                   HeroListManager.Check("ultimate-gapcloser", hero);
        }

        public static bool Flash()
        {
            return _menu != null && _menu.Item(_menu.Name + ".ultimate.flash.enabled").GetValue<bool>() &&
                   _menu.Item(_menu.Name + ".ultimate.flash.hotkey").GetValue<KeyBind>().Active;
        }

        public static bool Assisted()
        {
            return _menu != null && _menu.Item(_menu.Name + ".ultimate.assisted.enabled").GetValue<bool>() &&
                   _menu.Item(_menu.Name + ".ultimate.assisted.hotkey").GetValue<KeyBind>().Active;
        }

        public static bool Check(int min, List<Obj_AI_Hero> hits)
        {
            try
            {
                if (_menu == null)
                {
                    return false;
                }

                if (HeroListManager.Enabled("ultimate-force"))
                {
                    if (hits.Any(hit => HeroListManager.Check("ultimate-force", hit)) &&
                        hits.Count >=
                        (_menu.Item(_menu.Name + ".ultimate.force.additional").GetValue<Slider>().Value + 1))
                    {
                        return true;
                    }
                }

                if (HeroListManager.Enabled("ultimate-required"))
                {
                    if (!hits.Any(hit => HeroListManager.Check("ultimate-required", hit)))
                    {
                        var range = _menu.Item(_menu.Name + ".ultimate.required.range-check").GetValue<Slider>().Value;
                        if (
                            GameObjects.EnemyHeroes.Where(
                                h => !h.IsDead && h.IsVisible && h.Distance(ObjectManager.Player) <= range)
                                .Any(h => HeroListManager.Check("ultimate-required", h)))
                        {
                            return false;
                        }
                    }
                }

                return hits.Count >= min;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return false;
        }
    }
}