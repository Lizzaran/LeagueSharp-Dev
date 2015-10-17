#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 TargetSelector.cs is part of SFXChallenger.

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
using SFXChallenger.Helpers;
using SFXChallenger.Library.Logger;
using SharpDX;
using DamageType = SFXChallenger.Enumerations.DamageType;
using Spell = SFXChallenger.Wrappers.Spell;

#endregion

/*
 * Don't copy paste this without asking & giving credits fuckers :^) 
 */

namespace SFXChallenger.SFXTargetSelector
{
    public static class TargetSelector
    {
        private static Menu _menu;

        static TargetSelector()
        {
            Mode = TargetSelectorModeType.Weights;
        }

        public static TargetSelectorModeType Mode { get; set; }

        public static bool ForceFocus
        {
            get { return _menu != null && _menu.Item(_menu.Name + ".force-focus").GetValue<bool>(); }
        }

        public static bool Focus
        {
            get { return _menu != null && _menu.Item(_menu.Name + ".focus").GetValue<bool>(); }
        }

        internal static bool IsValidTarget(Obj_AI_Hero target,
            float range,
            DamageType damageType,
            bool ignoreShields = true,
            Vector3 from = default(Vector3))
        {
            try
            {
                return target.IsValidTarget() &&
                       target.Distance(
                           (from.Equals(default(Vector3)) ? ObjectManager.Player.ServerPosition : from), true) <
                       Math.Pow((range <= 0 ? Orbwalking.GetRealAutoAttackRange(target) : range), 2) &&
                       !Invulnerable.Check(target, damageType, ignoreShields);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return false;
        }

        private static IEnumerable<Targets.Item> GetOrderedChampions(List<Targets.Item> items)
        {
            try
            {
                switch (Mode)
                {
                    case TargetSelectorModeType.Weights:
                        return Weights.OrderChampions(items);

                    case TargetSelectorModeType.Priorities:
                        return Priorities.OrderChampions(items);

                    case TargetSelectorModeType.LessAttacksToKill:
                        return items.OrderBy(x => x.Hero.Health / ObjectManager.Player.TotalAttackDamage);

                    case TargetSelectorModeType.MostAbilityPower:
                        return items.OrderByDescending(x => x.Hero.TotalMagicalDamage);

                    case TargetSelectorModeType.MostAttackDamage:
                        return items.OrderByDescending(x => x.Hero.TotalAttackDamage);

                    case TargetSelectorModeType.Closest:
                        return items.OrderBy(x => x.Hero.Distance(ObjectManager.Player));

                    case TargetSelectorModeType.NearMouse:
                        return items.OrderBy(x => x.Hero.Distance(Game.CursorPos));

                    case TargetSelectorModeType.LessCastPriority:
                        return items.OrderBy(x => x.Hero.Health / ObjectManager.Player.TotalMagicalDamage);

                    case TargetSelectorModeType.LeastHealth:
                        return items.OrderBy(x => x.Hero.Health);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return new List<Targets.Item>();
        }

        private static TargetSelectorModeType GetModeBySelectedIndex(int index)
        {
            try
            {
                switch (index)
                {
                    case 0:
                        return TargetSelectorModeType.Weights;
                    case 1:
                        return TargetSelectorModeType.Priorities;
                    case 2:
                        return TargetSelectorModeType.LessAttacksToKill;
                    case 3:
                        return TargetSelectorModeType.MostAbilityPower;
                    case 4:
                        return TargetSelectorModeType.MostAttackDamage;
                    case 5:
                        return TargetSelectorModeType.Closest;
                    case 6:
                        return TargetSelectorModeType.NearMouse;
                    case 7:
                        return TargetSelectorModeType.LessCastPriority;
                    case 8:
                        return TargetSelectorModeType.LeastHealth;
                    default:
                        return TargetSelectorModeType.Weights;
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return TargetSelectorModeType.Weights;
        }

        public static Obj_AI_Hero GetTargetNoCollision(Spell spell,
            bool ignoreShields = true,
            Vector3 from = default(Vector3),
            IEnumerable<Obj_AI_Hero> ignoredChampions = null)
        {
            try
            {
                return
                    GetTargets(spell.Range, spell.DamageType, ignoreShields, from, ignoredChampions)
                        .FirstOrDefault(t => spell.GetPrediction(t).Hitchance != HitChance.Collision);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return null;
        }

        public static Obj_AI_Hero GetTarget(Spell spell,
            bool ignoreShields = true,
            Vector3 from = default(Vector3),
            IEnumerable<Obj_AI_Hero> ignoredChampions = null)
        {
            try
            {
                return
                    GetTarget(
                        (spell.Range + spell.Width +
                         Targets.Items.Select(e => e.Hero.BoundingRadius).DefaultIfEmpty(50).Max()) * 1.1f,
                        spell.DamageType, ignoreShields, from, ignoredChampions);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return null;
        }

        public static Obj_AI_Hero GetTarget(float range,
            DamageType damageType = DamageType.True,
            bool ignoreShields = true,
            Vector3 from = default(Vector3),
            IEnumerable<Obj_AI_Hero> ignoredChampions = null)
        {
            try
            {
                var targets = GetTargets(range, damageType, ignoreShields, from, ignoredChampions);
                return targets != null ? targets.FirstOrDefault() : null;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return null;
        }

        public static IEnumerable<Obj_AI_Hero> GetTargets(float range,
            DamageType damageType = DamageType.True,
            bool ignoreShields = true,
            Vector3 from = default(Vector3),
            IEnumerable<Obj_AI_Hero> ignoredChampions = null)
        {
            try
            {
                Weights.Range = Math.Max(range, Weights.Range);

                var selectedTarget = Selected.GetTarget(range, damageType, ignoreShields, from);
                if (selectedTarget != null)
                {
                    return new List<Obj_AI_Hero> { selectedTarget };
                }

                range = Mode == TargetSelectorModeType.Weights && ForceFocus ? Weights.Range : range;

                var targets =
                    Humanizer.FilterTargets(Targets.Items)
                        .Where(
                            h => ignoredChampions == null || ignoredChampions.All(i => i.NetworkId != h.Hero.NetworkId))
                        .Where(h => IsValidTarget(h.Hero, range, damageType, ignoreShields, from))
                        .ToList();

                if (targets.Count > 0)
                {
                    var t = GetOrderedChampions(targets).ToList();
                    if (t.Count > 0)
                    {
                        if (Selected.Target != null && Focus && t.Count > 1)
                        {
                            t = t.OrderByDescending(x => x.Hero.NetworkId.Equals(Selected.Target.NetworkId)).ToList();
                        }
                        return t.Select(h => h.Hero).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return new List<Obj_AI_Hero>();
        }

        public static void AddToMenu(Menu menu)
        {
            try
            {
                _menu = menu;

                var drawingMenu = _menu.AddSubMenu(new Menu("Drawings", menu.Name + ".drawing"));

                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + ".circle-thickness", "Circle Thickness").SetShared()
                        .SetValue(new Slider(5, 1, 10)));

                Selected.AddToMenu(_menu, drawingMenu);
                Weights.AddToMenu(_menu, drawingMenu);
                Priorities.AddToMenu(_menu);

                _menu.AddItem(new MenuItem(_menu.Name + ".focus", "Focus Selected Target").SetShared().SetValue(true));
                _menu.AddItem(
                    new MenuItem(_menu.Name + ".force-focus", "Only Attack Selected Target").SetShared().SetValue(false));

                Humanizer.AddToMenu(_menu);

                _menu.AddItem(
                    new MenuItem(menu.Name + ".mode", "Mode").SetShared()
                        .SetValue(
                            new StringList(
                                new[]
                                {
                                    "Weigths", "Priorities", "Less Attacks To Kill", "Most Ability Power",
                                    "Most Attack Damage", "Closest", "Near Mouse", "Less Cast Priority", "Least Health"
                                }))).ValueChanged +=
                    delegate(object sender, OnValueChangeEventArgs args)
                    {
                        Mode = GetModeBySelectedIndex(args.GetNewValue<StringList>().SelectedIndex);
                    };

                Mode = GetModeBySelectedIndex(_menu.Item(menu.Name + ".mode").GetValue<StringList>().SelectedIndex);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }
}