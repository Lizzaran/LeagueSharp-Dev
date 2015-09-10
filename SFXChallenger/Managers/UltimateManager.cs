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
using SFXChallenger.Enumerations;
using SFXChallenger.Library;
using SFXChallenger.Library.Logger;

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
        private static bool _required;
        private static bool _force;

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
                _required = required;
                _force = force;
                _menu = menu;

                var ultimateMenu = menu.AddSubMenu(new Menu("Ultimate", menu.Name + ".ultimate"));

                var uComboMenu = ultimateMenu.AddSubMenu(new Menu("Combo", ultimateMenu.Name + ".combo"));
                if (required)
                {
                    var requiredComboMenu =
                        uComboMenu.AddSubMenu(new Menu("Required Targets", uComboMenu.Name + ".required"));
                    requiredComboMenu.AddItem(
                        new MenuItem(requiredComboMenu.Name + ".min", "Min. Required").SetValue(new Slider(1, 1, 5))
                            .DontSave());
                    HeroListManager.AddToMenu(requiredComboMenu, "ultimate-required-combo", true, false, true, false);
                }
                uComboMenu.AddItem(new MenuItem(uComboMenu.Name + ".min", "Min. Hits").SetValue(new Slider(2, 1, 5)));
                uComboMenu.AddItem(new MenuItem(uComboMenu.Name + ".single", "Single").SetValue(true));
                uComboMenu.AddItem(new MenuItem(uComboMenu.Name + ".enabled", "Enabled").SetValue(true));

                if (auto)
                {
                    var uAutoMenu = ultimateMenu.AddSubMenu(new Menu("Auto", ultimateMenu.Name + ".auto"));
                    if (required)
                    {
                        var requiredAutoMenu =
                            uAutoMenu.AddSubMenu(new Menu("Required Targets", uAutoMenu.Name + ".required"));
                        requiredAutoMenu.AddItem(
                            new MenuItem(requiredAutoMenu.Name + ".min", "Min. Required").SetValue(new Slider(1, 1, 5))
                                .DontSave());
                        HeroListManager.AddToMenu(requiredAutoMenu, "ultimate-required-auto", true, false, true, false);
                    }
                    if (autoInterrupt)
                    {
                        var autoInterruptMenu =
                            uAutoMenu.AddSubMenu(new Menu("Interrupt", uAutoMenu.Name + ".interrupt"));
                        if (interruptDelay)
                        {
                            DelayManager.AddToMenu(
                                autoInterruptMenu, "ultimate-interrupt-delay", string.Empty, 0, 0, 500);
                        }
                        HeroListManager.AddToMenu(autoInterruptMenu, "ultimate-interrupt", false, false, true, false);
                    }
                    if (autoGapcloser)
                    {
                        var autoGapcloserMenu =
                            uAutoMenu.AddSubMenu(new Menu("Gapcloser", uAutoMenu.Name + ".gapcloser"));
                        if (gapcloserDelay)
                        {
                            DelayManager.AddToMenu(
                                autoGapcloserMenu, "ultimate-gapcloser-delay", string.Empty, 0, 0, 500);
                        }
                        HeroListManager.AddToMenu(autoGapcloserMenu, "ultimate-gapcloser", false, false, true, false);
                    }
                    uAutoMenu.AddItem(new MenuItem(uAutoMenu.Name + ".min", "Min. Hits").SetValue(new Slider(3, 1, 5)));
                    uAutoMenu.AddItem(new MenuItem(uAutoMenu.Name + ".single", "Single").SetValue(false));
                    uAutoMenu.AddItem(new MenuItem(uAutoMenu.Name + ".enabled", "Enabled").SetValue(true));
                }

                if (flash)
                {
                    var uFlashMenu = ultimateMenu.AddSubMenu(new Menu("Flash", ultimateMenu.Name + ".flash"));
                    uFlashMenu.AddItem(
                        new MenuItem(uFlashMenu.Name + ".min", "Min. Hits").SetValue(new Slider(1, 1, 5)));
                    uFlashMenu.AddItem(new MenuItem(uFlashMenu.Name + ".single", "Single").SetValue(false));
                    uFlashMenu.AddItem(
                        new MenuItem(uFlashMenu.Name + ".hotkey", "Hotkey").SetValue(
                            new KeyBind('Y', KeyBindType.Press)));
                    uFlashMenu.AddItem(new MenuItem(uFlashMenu.Name + ".move-cursor", "Move to Cursor").SetValue(true));
                    uFlashMenu.AddItem(new MenuItem(uFlashMenu.Name + ".enabled", "Enabled").SetValue(true));
                }

                if (assisted)
                {
                    var uAssistedMenu = ultimateMenu.AddSubMenu(new Menu("Assisted", ultimateMenu.Name + ".assisted"));
                    uAssistedMenu.AddItem(
                        new MenuItem(uAssistedMenu.Name + ".min", "Min. Hits").SetValue(new Slider(1, 1, 5)));
                    uAssistedMenu.AddItem(new MenuItem(uAssistedMenu.Name + ".single", "Single").SetValue(false));
                    uAssistedMenu.AddItem(
                        new MenuItem(uAssistedMenu.Name + ".hotkey", "Hotkey").SetValue(
                            new KeyBind('T', KeyBindType.Press)));
                    uAssistedMenu.AddItem(
                        new MenuItem(uAssistedMenu.Name + ".move-cursor", "Move to Cursor").SetValue(true));
                    uAssistedMenu.AddItem(new MenuItem(uAssistedMenu.Name + ".enabled", "Enabled").SetValue(true));
                }

                var uSingleMenu = ultimateMenu.AddSubMenu(new Menu("Single", ultimateMenu.Name + ".single"));

                var uSingleAlliesMenu = uSingleMenu.AddSubMenu(new Menu("Allies", uSingleMenu.Name + ".allies"));
                uSingleAlliesMenu.AddItem(
                    new MenuItem(uSingleAlliesMenu.Name + ".min", "Min. Allies").SetValue(new Slider(0, 0, 4)));
                uSingleAlliesMenu.AddItem(
                    new MenuItem(uSingleAlliesMenu.Name + ".max", "Max. Allies").SetValue(new Slider(3, 0, 4)));

                var uSingleEnemiesMenu = uSingleMenu.AddSubMenu(new Menu("Enemies", uSingleMenu.Name + ".enemies"));
                uSingleEnemiesMenu.AddItem(
                    new MenuItem(uSingleEnemiesMenu.Name + ".min", "Min. Enemies").SetValue(new Slider(1, 1, 5)));
                uSingleEnemiesMenu.AddItem(
                    new MenuItem(uSingleEnemiesMenu.Name + ".max", "Max. Enemies").SetValue(new Slider(1, 1, 5)));

                var uSingleTargetMenu = uSingleMenu.AddSubMenu(new Menu("Target", uSingleMenu.Name + ".target"));

                uSingleTargetMenu.AddItem(
                    new MenuItem(uSingleTargetMenu.Name + ".min-health", "Min. Target Health %").SetValue(
                        new Slider(20, 1)));
                uSingleTargetMenu.AddItem(
                    new MenuItem(uSingleTargetMenu.Name + ".max-health", "Max. Target Health %").SetValue(
                        new Slider(100, 1)));

                var uSingleDamageMenu = uSingleMenu.AddSubMenu(new Menu("Damage", uSingleMenu.Name + ".damage"));

                uSingleDamageMenu.AddItem(
                    new MenuItem(uSingleDamageMenu.Name + ".percent", "Combo Damage %").SetValue(
                        new Slider(100, 1, 200)));

                if (force)
                {
                    var uForceMenu = ultimateMenu.AddSubMenu(new Menu("Force On", ultimateMenu.Name + ".force"));
                    uForceMenu.AddItem(new MenuItem(uForceMenu.Name + ".combo-killable", "Killable").SetValue(false));
                    uForceMenu.AddItem(
                        new MenuItem(uForceMenu.Name + ".additional", "Additional Targets").SetValue(
                            new Slider(0, 0, 4)).DontSave());
                    HeroListManager.AddToMenu(uForceMenu, "ultimate-force", true, false, true, false, true, false);
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
            return _menu != null && _auto && _menu.Item(_menu.Name + ".ultimate.auto.enabled").GetValue<bool>();
        }

        public static bool Interrupt(Obj_AI_Hero hero)
        {
            return _menu != null && _interrupt && _menu.Item(_menu.Name + ".ultimate.auto.enabled").GetValue<bool>() &&
                   HeroListManager.Check("ultimate-interrupt", hero);
        }

        public static bool Gapcloser(Obj_AI_Hero hero)
        {
            return _menu != null && _gapcloser && _menu.Item(_menu.Name + ".ultimate.auto.enabled").GetValue<bool>() &&
                   HeroListManager.Check("ultimate-gapcloser", hero);
        }

        public static bool Flash()
        {
            return _menu != null && _flash && _menu.Item(_menu.Name + ".ultimate.flash.enabled").GetValue<bool>() &&
                   _menu.Item(_menu.Name + ".ultimate.flash.hotkey").GetValue<KeyBind>().Active;
        }

        public static bool Assisted()
        {
            return _menu != null && _assisted && _menu.Item(_menu.Name + ".ultimate.assisted.enabled").GetValue<bool>() &&
                   _menu.Item(_menu.Name + ".ultimate.assisted.hotkey").GetValue<KeyBind>().Active;
        }

        public static bool CheckSingle(Obj_AI_Hero target, float damage)
        {
            try
            {
                if (_menu == null || target == null || !target.IsValidTarget())
                {
                    return false;
                }

                var alliesMin = _menu.Item(_menu.Name + ".ultimate.single.allies.min").GetValue<Slider>().Value;
                var alliesMax = _menu.Item(_menu.Name + ".ultimate.single.allies.max").GetValue<Slider>().Value;

                var enemiesMin = _menu.Item(_menu.Name + ".ultimate.single.enemies.min").GetValue<Slider>().Value;
                var enemiesMax = _menu.Item(_menu.Name + ".ultimate.single.enemies.max").GetValue<Slider>().Value;

                var targetMinHealth =
                    _menu.Item(_menu.Name + ".ultimate.single.target.min-health").GetValue<Slider>().Value;
                var targetMaxHealth =
                    _menu.Item(_menu.Name + ".ultimate.single.target.max-health").GetValue<Slider>().Value;

                if (target.HealthPercent >= targetMinHealth && target.HealthPercent <= targetMaxHealth)
                {
                    var pos = ObjectManager.Player.Position.Extend(
                        target.Position, ObjectManager.Player.Distance(target) / 2f);

                    var aCount =
                        GameObjects.AllyHeroes.Count(h => h.IsValid && !h.IsMe && !h.IsDead && h.Distance(pos) <= 1750);
                    var eCount =
                        GameObjects.EnemyHeroes.Count(
                            h => h.IsValid && !h.IsDead && h.IsVisible && h.Distance(pos) <= 1750);

                    if (aCount >= alliesMin && aCount <= alliesMax && eCount >= enemiesMin && eCount <= enemiesMax)
                    {
                        return damage *
                               (_menu.Item(_menu.Name + ".ultimate.single.damage.percent").GetValue<Slider>().Value /
                                100f) > target.Health;
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return false;
        }

        public static bool Check(UltimateModeType mode,
            int min,
            List<Obj_AI_Hero> hits,
            Func<Obj_AI_Hero, float> calcDamage = null)
        {
            try
            {
                var modeString = mode.ToString().ToLower();
                if (_menu == null || hits == null || !hits.Any())
                {
                    return false;
                }

                if (_force && HeroListManager.Enabled("ultimate-force"))
                {
                    var killable = _menu.Item(_menu.Name + ".ultimate.force.combo-killable").GetValue<bool>();
                    var additional = _menu.Item(_menu.Name + ".ultimate.force.additional").GetValue<Slider>().Value;
                    var damageMulti =
                        (_menu.Item(_menu.Name + ".ultimate.single.damage.percent").GetValue<Slider>().Value / 100f);
                    if (
                        hits.Any(
                            hit =>
                                HeroListManager.Check("ultimate-force", hit) &&
                                (!killable || calcDamage == null || calcDamage(hit) * damageMulti > hit.Health)) &&
                        hits.Count >= additional + 1)
                    {
                        return true;
                    }
                }

                if (_required && HeroListManager.Enabled("ultimate-required-" + modeString))
                {
                    var minReq =
                        _menu.Item(_menu.Name + ".ultimate." + modeString + ".required.min").GetValue<Slider>().Value;
                    var enabledHeroes = HeroListManager.GetEnabledHeroes("ultimate-required-" + modeString);
                    if (minReq > 0 && enabledHeroes.Count > 0)
                    {
                        var count =
                            enabledHeroes.Where(
                                e => !e.IsDead && e.IsVisible && e.Distance(ObjectManager.Player) <= 2000)
                                .Count(e => hits.Any(h => h.NetworkId.Equals(e.NetworkId)));
                        if (count < minReq)
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