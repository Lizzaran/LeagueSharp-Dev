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
                uComboMenu.AddItem(new MenuItem(uComboMenu.Name + ".min", "R " + "Min").SetValue(new Slider(2, 1, 5)));
                uComboMenu.AddItem(new MenuItem(uComboMenu.Name + ".duel", "Duel").SetValue(true));
                uComboMenu.AddItem(new MenuItem(uComboMenu.Name + ".enabled", "Enabled").SetValue(true));

                if (auto)
                {
                    var uAutoMenu = ultimateMenu.AddSubMenu(new Menu("Auto", ultimateMenu.Name + ".auto"));
                    if (autoInterrupt)
                    {
                        var autoInterruptMenu =
                            uAutoMenu.AddSubMenu(new Menu("Interrupt Spell", uAutoMenu.Name + ".interrupt"));
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
                            uAutoMenu.AddSubMenu(new Menu("Gapcloser", uAutoMenu.Name + ".gapcloser"));
                        if (gapcloserDelay)
                        {
                            DelayManager.AddToMenu(
                                autoGapcloserMenu, "ultimate-gapcloser-delay", string.Empty, 250, 0, 1000);
                        }
                        HeroListManager.AddToMenu(autoGapcloserMenu, "ultimate-gapcloser", false, false, true, false);
                    }
                    uAutoMenu.AddItem(new MenuItem(uAutoMenu.Name + ".min", "R " + "Min").SetValue(new Slider(3, 1, 5)));
                    uAutoMenu.AddItem(new MenuItem(uAutoMenu.Name + ".duel", "Duel").SetValue(false));
                    uAutoMenu.AddItem(new MenuItem(uAutoMenu.Name + ".enabled", "Enabled").SetValue(true));
                }

                if (flash)
                {
                    var uFlashMenu = ultimateMenu.AddSubMenu(new Menu("Flash", ultimateMenu.Name + ".flash"));
                    uFlashMenu.AddItem(
                        new MenuItem(uFlashMenu.Name + ".min", "R " + "Min").SetValue(new Slider(3, 1, 5)));
                    uFlashMenu.AddItem(new MenuItem(uFlashMenu.Name + ".duel", "Duel").SetValue(true));
                    uFlashMenu.AddItem(
                        new MenuItem(uFlashMenu.Name + ".hotkey", "Hotkey").SetValue(
                            new KeyBind('U', KeyBindType.Press)));
                    uFlashMenu.AddItem(new MenuItem(uFlashMenu.Name + ".move-cursor", "Move to Cursor").SetValue(true));
                    uFlashMenu.AddItem(new MenuItem(uFlashMenu.Name + ".enabled", "Enabled").SetValue(true));
                }

                if (assisted)
                {
                    var uAssistedMenu = ultimateMenu.AddSubMenu(new Menu("Assisted", ultimateMenu.Name + ".assisted"));
                    uAssistedMenu.AddItem(
                        new MenuItem(uAssistedMenu.Name + ".min", "R " + "Min").SetValue(new Slider(3, 1, 5)));
                    uAssistedMenu.AddItem(new MenuItem(uAssistedMenu.Name + ".duel", "Duel").SetValue(true));
                    uAssistedMenu.AddItem(
                        new MenuItem(uAssistedMenu.Name + ".hotkey", "Hotkey").SetValue(
                            new KeyBind('R', KeyBindType.Press)));
                    uAssistedMenu.AddItem(
                        new MenuItem(uAssistedMenu.Name + ".move-cursor", "Move to Cursor").SetValue(true));
                    uAssistedMenu.AddItem(new MenuItem(uAssistedMenu.Name + ".enabled", "Enabled").SetValue(true));
                }

                var uDuelMenu = ultimateMenu.AddSubMenu(new Menu("Duel Settings", ultimateMenu.Name + ".duel"));

                var uDuelAlliesMenu = uDuelMenu.AddSubMenu(new Menu("Allies", uDuelMenu.Name + ".allies"));

                uDuelAlliesMenu.AddItem(
                    new MenuItem(uDuelAlliesMenu.Name + ".range", "Max Allies Range").SetValue(
                        new Slider(1500, 500, 3000)));
                uDuelAlliesMenu.AddItem(
                    new MenuItem(uDuelAlliesMenu.Name + ".min", "Min Allies").SetValue(new Slider(0, 0, 4)));
                uDuelAlliesMenu.AddItem(
                    new MenuItem(uDuelAlliesMenu.Name + ".max", "Max Allies").SetValue(new Slider(3, 0, 4)));

                var uDuelEnemiesMenu = uDuelMenu.AddSubMenu(new Menu("Enemies", uDuelMenu.Name + ".enemies"));

                uDuelEnemiesMenu.AddItem(
                    new MenuItem(uDuelEnemiesMenu.Name + ".range", "Max Enemies Range").SetValue(
                        new Slider(2500, 500, 3000)));
                uDuelEnemiesMenu.AddItem(
                    new MenuItem(uDuelEnemiesMenu.Name + ".min", "Min Enemies").SetValue(new Slider(1, 1, 5)));
                uDuelEnemiesMenu.AddItem(
                    new MenuItem(uDuelEnemiesMenu.Name + ".max", "Max Enemies").SetValue(new Slider(1, 1, 5)));

                var uDuelTargetMenu = uDuelMenu.AddSubMenu(new Menu("Target", uDuelMenu.Name + ".target"));

                uDuelTargetMenu.AddItem(
                    new MenuItem(uDuelTargetMenu.Name + ".min-health", "Min Target Health %").SetValue(
                        new Slider(15, 10)));
                uDuelTargetMenu.AddItem(
                    new MenuItem(uDuelTargetMenu.Name + ".max-health", "Max Target Health %").SetValue(
                        new Slider(100, 10)));

                var uDuelDamageMenu = uDuelMenu.AddSubMenu(new Menu("Damage", uDuelMenu.Name + ".damage"));

                uDuelDamageMenu.AddItem(
                    new MenuItem(uDuelDamageMenu.Name + ".percent", "Combo Damage %").SetValue(new Slider(100, 1, 200)));

                if (required)
                {
                    var uRequiredMenu =
                        ultimateMenu.AddSubMenu(new Menu("Required Target", ultimateMenu.Name + ".required"));

                    var requiredComboMenu = uRequiredMenu.AddSubMenu(new Menu("Combo", uRequiredMenu.Name + ".combo"));
                    requiredComboMenu.AddItem(
                        new MenuItem(requiredComboMenu.Name + ".min", "Min").SetValue(new Slider(0, 0, 5)).DontSave());
                    HeroListManager.AddToMenu(
                        requiredComboMenu, "ultimate-required-combo", true, false, true, false, true);

                    var requiredAutoMenu = uRequiredMenu.AddSubMenu(new Menu("Auto", uRequiredMenu.Name + ".auto"));
                    requiredAutoMenu.AddItem(
                        new MenuItem(requiredAutoMenu.Name + ".min", "Min").SetValue(new Slider(0, 0, 5)).DontSave());
                    HeroListManager.AddToMenu(
                        requiredAutoMenu, "ultimate-required-auto", true, false, true, false, true);


                    uRequiredMenu.AddItem(
                        new MenuItem(uRequiredMenu.Name + ".range-check", "Range Check").SetValue(
                            new Slider(2000, 1000, 3000)));
                }

                if (force)
                {
                    var uForceMenu = ultimateMenu.AddSubMenu(new Menu("Force Target", ultimateMenu.Name + ".force"));
                    uForceMenu.AddItem(
                        new MenuItem(uForceMenu.Name + ".additional", "Additional Targets").SetValue(
                            new Slider(0, 0, 4)).DontSave());
                    uForceMenu.AddItem(
                        new MenuItem(uForceMenu.Name + ".combo-killable", "Combo Killable").SetValue(false).DontSave());
                    HeroListManager.AddToMenu(uForceMenu, "ultimate-force", true, false, true, false, true);
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

        public static bool CheckDuel(Obj_AI_Hero target, float damage)
        {
            try
            {
                if (_menu == null || target == null || !target.IsValidTarget())
                {
                    return false;
                }

                var alliesRange = _menu.Item(_menu.Name + ".ultimate.duel.allies.range").GetValue<Slider>().Value;
                var alliesMin = _menu.Item(_menu.Name + ".ultimate.duel.allies.min").GetValue<Slider>().Value;
                var alliesMax = _menu.Item(_menu.Name + ".ultimate.duel.allies.max").GetValue<Slider>().Value;

                var enemiesRange = _menu.Item(_menu.Name + ".ultimate.duel.enemies.range").GetValue<Slider>().Value;
                var enemiesMin = _menu.Item(_menu.Name + ".ultimate.duel.enemies.min").GetValue<Slider>().Value;
                var enemiesMax = _menu.Item(_menu.Name + ".ultimate.duel.enemies.max").GetValue<Slider>().Value;

                var targetMinHealth =
                    _menu.Item(_menu.Name + ".ultimate.duel.target.min-health").GetValue<Slider>().Value;
                var targetMaxHealth =
                    _menu.Item(_menu.Name + ".ultimate.duel.target.max-health").GetValue<Slider>().Value;

                if (target.HealthPercent >= targetMinHealth && target.HealthPercent <= targetMaxHealth)
                {
                    var pos = ObjectManager.Player.Position.Extend(
                        target.Position, ObjectManager.Player.Distance(target) / 2f);

                    var aCount =
                        GameObjects.AllyHeroes.Count(
                            h => h.IsValid && !h.IsMe && !h.IsDead && h.Distance(pos) <= alliesRange);
                    var eCount =
                        GameObjects.EnemyHeroes.Count(
                            h => h.IsValid && !h.IsDead && h.IsVisible && h.Distance(pos) <= enemiesRange);

                    if (aCount >= alliesMin && aCount <= alliesMax && eCount >= enemiesMin && eCount <= enemiesMax)
                    {
                        return damage *
                               (_menu.Item(_menu.Name + ".ultimate.duel.damage.percent").GetValue<Slider>().Value / 100f) >
                               target.Health;
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return false;
        }

        public static bool Check(string mode,
            int min,
            List<Obj_AI_Hero> hits,
            Func<Obj_AI_Hero, float> calcDamage = null)
        {
            try
            {
                if (_menu == null || hits == null || !hits.Any())
                {
                    return false;
                }

                if (_force && HeroListManager.Enabled("ultimate-force"))
                {
                    var killable = _menu.Item(_menu.Name + ".ultimate.force.combo-killable").GetValue<bool>();
                    var additional = _menu.Item(_menu.Name + ".ultimate.force.additional").GetValue<Slider>().Value;
                    if (
                        hits.Any(
                            hit =>
                                HeroListManager.Check("ultimate-force", hit) &&
                                (!killable || calcDamage == null || calcDamage(hit) > hit.Health)) &&
                        hits.Count >= additional + 1)
                    {
                        return true;
                    }
                }

                if (_required && HeroListManager.Enabled("ultimate-required-" + mode))
                {
                    var minReq = _menu.Item(_menu.Name + ".ultimate.required." + mode + ".min").GetValue<Slider>().Value;
                    var range = _menu.Item(_menu.Name + ".ultimate.required.range-check").GetValue<Slider>().Value;
                    var enabledHeroes = HeroListManager.GetEnabledHeroes("ultimate-required-" + mode);
                    var count =
                        enabledHeroes.Where(e => !e.IsDead && e.IsVisible && e.Distance(ObjectManager.Player) <= range)
                            .Count(e => hits.Any(h => h.NetworkId.Equals(e.NetworkId)));
                    if (count < minReq)
                    {
                        return false;
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