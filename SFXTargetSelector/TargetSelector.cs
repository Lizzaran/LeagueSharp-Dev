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
using SFXTargetSelector.Others;
using SharpDX;
using Orbwalking = SFXTargetSelector.Others.Orbwalking;
using Version = System.Version;

#endregion

/*
 * Don't copy paste this without asking & giving credits fuckers :^) 
 */

namespace SFXTargetSelector
{
    public static class TargetSelector
    {
        public enum TargetingMode
        {
            Weights,
            Priorities,
            LessAttacksToKill,
            MostAbilityPower,
            MostAttackDamage,
            Closest,
            NearMouse,
            LessCastPriority,
            LeastHealth
        }

        private static Menu _menu;

        static TargetSelector()
        {
            Mode = TargetingMode.Weights;

            CustomEvents.Game.OnGameLoad += delegate
            {
                Notifications.AddNotification(string.Format("{0} loaded.", Name), 7500);
                Game.PrintChat(string.Format("<font color='#259FF8'>{0} v{1} loaded.</font>", Name, Version));
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

        public static TargetingMode Mode { get; set; }

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
                case TargetingMode.Weights:
                    return Weights.OrderChampions(items);

                case TargetingMode.Priorities:
                    return Priorities.OrderChampions(items);

                case TargetingMode.LessAttacksToKill:
                    return items.OrderBy(x => x.Hero.Health / ObjectManager.Player.TotalAttackDamage);

                case TargetingMode.MostAbilityPower:
                    return items.OrderByDescending(x => x.Hero.TotalMagicalDamage);

                case TargetingMode.MostAttackDamage:
                    return items.OrderByDescending(x => x.Hero.TotalAttackDamage);

                case TargetingMode.Closest:
                    return items.OrderBy(x => x.Hero.Distance(ObjectManager.Player));

                case TargetingMode.NearMouse:
                    return items.OrderBy(x => x.Hero.Distance(Game.CursorPos));

                case TargetingMode.LessCastPriority:
                    return items.OrderBy(x => x.Hero.Health / ObjectManager.Player.TotalMagicalDamage);

                case TargetingMode.LeastHealth:
                    return items.OrderBy(x => x.Hero.Health);
            }
            return new List<Targets.Item>();
        }

        private static TargetingMode GetModeBySelectedIndex(int index)
        {
            try
            {
                var modes =
                    Enum.GetNames(typeof(TargetingMode))
                        .Select(m => (TargetingMode) Enum.Parse(typeof(TargetingMode), m))
                        .ToArray();
                if (index < modes.Length && index >= 0)
                {
                    return modes[index];
                }
            }
            catch
            {
                // Ignored
            }
            return TargetingMode.Weights;
        }

        public static Obj_AI_Hero GetTargetNoCollision(Spell spell,
            bool ignoreShields = true,
            Vector3 from = default(Vector3),
            IEnumerable<Obj_AI_Hero> ignoredChampions = null)
        {
            return
                GetTargets(
                    spell.Range,
                    (spell.DamageType == LeagueSharp.Common.TargetSelector.DamageType.True
                        ? DamageType.True
                        : (spell.DamageType == LeagueSharp.Common.TargetSelector.DamageType.Physical
                            ? DamageType.Physical
                            : DamageType.Magical)), ignoreShields, from, ignoredChampions)
                    .FirstOrDefault(t => spell.GetPrediction(t).Hitchance != HitChance.Collision);
        }

        public static Obj_AI_Hero GetTarget(Spell spell,
            bool ignoreShields = true,
            Vector3 from = default(Vector3),
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

            range = Mode == TargetingMode.Weights && ForceFocus ? Weights.Range : range;

            var targets =
                Humanizer.FilterTargets(Targets.Items)
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
            menu.Name = "sfx.ts";
            _menu = menu;

            var drawingMenu = _menu.AddSubMenu(new Menu("Drawings", _menu.Name + ".drawing"));

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
                new MenuItem(_menu.Name + ".mode", "Mode").SetShared()
                    .SetValue(
                        new StringList(
                            Enum.GetNames(typeof(TargetingMode))
                                .Select(
                                    e =>
                                        string.Concat(e.Select(x => char.IsUpper(x) ? " " + x : x.ToString()))
                                            .TrimStart(' '))
                                .ToArray()))).ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs args)
                {
                    Mode = GetModeBySelectedIndex(args.GetNewValue<StringList>().SelectedIndex);
                };

            var infoMenu = _menu.AddSubMenu(new Menu("Info", _menu.Name + ".info"));
            infoMenu.AddItem(new MenuItem(menu.Name + ".version", string.Format("{0}: {1}", "Version", Version)));
            infoMenu.AddItem(new MenuItem(menu.Name + ".forum", "Forum: Lizzaran"));
            infoMenu.AddItem(new MenuItem(menu.Name + ".github", "GitHub: Lizzaran"));
            infoMenu.AddItem(new MenuItem(menu.Name + ".irc", "IRC: Appril"));

            Mode = GetModeBySelectedIndex(_menu.Item(_menu.Name + ".mode").GetValue<StringList>().SelectedIndex);
            LeagueSharp.Common.TargetSelector.CustomTS = true;
        }

        // For easy switching

        #region Compatibility

        [Obsolete("TargetSelector.SelectedTarget is deprecated, use Selected.Target instead.")]
        public static Obj_AI_Hero SelectedTarget
        {
            get { return Focus ? Selected.Target : null; }
        }

        [Obsolete("TargetSelector.SetPriority is deprecated, use Priorities.SetPriority instead.")]
        public static void SetPriority(Obj_AI_Hero hero, int newPriority)
        {
            Priorities.SetPriority(hero, newPriority);
        }

        [Obsolete("TargetSelector.GetPriority is deprecated, use Priorities.GetPriority instead.")]
        public static float GetPriority(Obj_AI_Hero hero)
        {
            switch (Priorities.GetPriority(hero))
            {
                case 2:
                    return 1.5f;
                case 3:
                    return 1.75f;
                case 4:
                    return 2f;
                case 5:
                    return 2.5f;
                default:
                    return 1f;
            }
        }

        [Obsolete("TargetSelector.IsInvulnerable is deprecated, use Invulnerable.Check instead.")]
        public static bool IsInvulnerable(Obj_AI_Base target, DamageType damageType, bool ignoreShields = true)
        {
            var hero = target as Obj_AI_Hero;
            return hero != null && Invulnerable.Check(hero, damageType, ignoreShields);
        }

        [Obsolete("TargetSelector.SetTarget is deprecated, use Selected.Target instead.")]
        public static void SetTarget(Obj_AI_Hero hero)
        {
            if (hero.IsValidTarget())
            {
                Selected.Target = hero;
            }
        }

        [Obsolete("TargetSelector.GetSelectedTarget is deprecated, use Selected.Target instead.")]
        public static Obj_AI_Hero GetSelectedTarget()
        {
            return SelectedTarget;
        }

        #endregion Compatibility
    }
}