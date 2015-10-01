#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 TargetSelector.cs is part of SFXTargetSelector.

 SFXTargetSelector is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXTargetSelector is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXTargetSelector. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Version = System.Version;

#endregion

/*
 * Don't copy paste this without asking & giving credits fuckers :^) 
 */

namespace SFXTargetSelector
{
    public static class TargetSelector
    {
        private static Menu _menu;

        static TargetSelector()
        {
            Mode = ModeType.Weights;

            CustomEvents.Game.OnGameLoad += delegate
            {
                Notifications.AddNotification(string.Format("{0} loaded.", Name), 10000);
                Game.PrintChat(string.Format("<font color='#259FF8'>{0} v{1} loaded.</font>", Name, Version));
                Update.Check(Name, Version, "Lizzaran/LeagueSharp-Dev/master/SFXTargetSelector", 10000);
            };
        }

        public static string Name
        {
            get { return "SFXTargetSelector"; }
        }

        public static Version Version
        {
            get { return Assembly.GetEntryAssembly().GetName().Version; }
        }

        public static ModeType Mode { get; set; }

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
            return target.IsValidTarget() &&
                   target.Distance((from.Equals(default(Vector3)) ? ObjectManager.Player.ServerPosition : from), true) <
                   Math.Pow((range <= 0 ? Orbwalking.GetRealAutoAttackRange(target) : range), 2) &&
                   !Invulnerable.Check(target, damageType, ignoreShields);
        }

        private static IEnumerable<Targets.Item> GetOrderedChampions(List<Targets.Item> items)
        {
            switch (Mode)
            {
                case ModeType.Weights:
                    return Weights.OrderChampions(items);

                case ModeType.Priorities:
                    return Priorities.OrderChampions(items);

                case ModeType.LessAttacksToKill:
                    return items.OrderBy(x => x.Hero.Health / ObjectManager.Player.TotalAttackDamage);

                case ModeType.MostAbilityPower:
                    return items.OrderByDescending(x => x.Hero.TotalMagicalDamage);

                case ModeType.MostAttackDamage:
                    return items.OrderByDescending(x => x.Hero.TotalAttackDamage);

                case ModeType.Closest:
                    return items.OrderBy(x => x.Hero.Distance(ObjectManager.Player));

                case ModeType.NearMouse:
                    return items.OrderBy(x => x.Hero.Distance(Game.CursorPos));

                case ModeType.LessCastPriority:
                    return items.OrderBy(x => x.Hero.Health / ObjectManager.Player.TotalMagicalDamage);

                case ModeType.LeastHealth:
                    return items.OrderBy(x => x.Hero.Health);
            }
            return new List<Targets.Item>();
        }

        private static ModeType GetModeBySelectedIndex(int index)
        {
            switch (index)
            {
                case 0:
                    return ModeType.Weights;
                case 1:
                    return ModeType.Priorities;
                case 2:
                    return ModeType.LessAttacksToKill;
                case 3:
                    return ModeType.MostAbilityPower;
                case 4:
                    return ModeType.MostAttackDamage;
                case 5:
                    return ModeType.Closest;
                case 6:
                    return ModeType.NearMouse;
                case 7:
                    return ModeType.LessCastPriority;
                case 8:
                    return ModeType.LeastHealth;
                default:
                    return ModeType.Weights;
            }
        }

        public static Obj_AI_Hero GetTarget(this Spell spell,
            bool ignoreShields = true,
            Vector3 from = new Vector3(),
            IEnumerable<Obj_AI_Hero> ignoredChampions = null)
        {
            return
                GetTarget(
                    (spell.Range + spell.Width +
                     Targets.Items.Select(e => e.Hero.BoundingRadius).DefaultIfEmpty(50).Max()),
                    (spell.DamageType == LeagueSharp.Common.TargetSelector.DamageType.True
                        ? DamageType.True
                        : (spell.DamageType == LeagueSharp.Common.TargetSelector.DamageType.Physical
                            ? DamageType.Physical
                            : DamageType.Magical)), ignoreShields, from, ignoredChampions);
        }

        public static Obj_AI_Hero GetTarget(float range,
            DamageType damageType = DamageType.True,
            bool ignoreShields = true,
            Vector3 from = default(Vector3),
            IEnumerable<Obj_AI_Hero> ignoredChampions = null)
        {
            var targets = GetTargets(range, damageType, ignoreShields, from, ignoredChampions);
            return targets != null ? targets.FirstOrDefault() : null;
        }

        public static IEnumerable<Obj_AI_Hero> GetTargets(float range,
            DamageType damageType = DamageType.True,
            bool ignoreShields = true,
            Vector3 from = default(Vector3),
            IEnumerable<Obj_AI_Hero> ignoredChampions = null)
        {
            Weights.Range = Math.Max(range, Weights.Range);

            var selectedTarget = Selected.GetTarget(range, damageType, ignoreShields, from);
            if (selectedTarget != null)
            {
                return new List<Obj_AI_Hero> { selectedTarget };
            }

            range = Mode == ModeType.Weights && ForceFocus ? Weights.Range : range;

            var targets =
                Humanizer.FilterTargets(Targets.Items, from, range)
                    .Where(h => ignoredChampions == null || ignoredChampions.All(i => i.NetworkId != h.Hero.NetworkId))
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

            return new List<Obj_AI_Hero>();
        }

        public static void AddToMenu(Menu menu)
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
                            })))
                .ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs args)
                {
                    Mode = GetModeBySelectedIndex(args.GetNewValue<StringList>().SelectedIndex);
                };

            Mode = GetModeBySelectedIndex(_menu.Item(menu.Name + ".mode").GetValue<StringList>().SelectedIndex);
        }
    }
}