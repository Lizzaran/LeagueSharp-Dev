#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Weights.cs is part of SFXChallenger.

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
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using ItemData = LeagueSharp.Common.Data.ItemData;

#endregion

/*
 * Don't copy paste this without asking & giving credits fuckers :^) 
 */

namespace SFXChallenger.SFXTargetSelector
{
    public static partial class TargetSelector
    {
        public static partial class Weights
        {
            public const int MinWeight = 0;
            public const int MaxWeight = 20;
            private const string InvertedPrefix = "[i] ";
            private const float BestTargetSwitchDelay = 0.5f;
            private static float _range;
            private static bool _separated;
            private static List<Targets.Item> _drawingTargets;
            private static Targets.Item _bestTarget;
            private static float _lastBestTargetSwitch;

            static Weights()
            {
                _drawingTargets = new List<Targets.Item>();
                Items = new HashSet<Item>
                {
                    new Item(
                        "killable", "AA Killable", 20, false,
                        t => t.Health < ObjectManager.Player.GetAutoAttackDamage(t, true) ? 10 : 0),
                    new Item(
                        "attack-damage", "Attack Damage", 15, false, delegate(Obj_AI_Hero t)
                        {
                            var ad = t.FlatPhysicalDamageMod;
                            ad += ad / 100 * (t.Crit * 100) * (ItemData.Infinity_Edge.GetItem().IsOwned(t) ? 2.5f : 2f);
                            var averageArmor = HeroManager.Allies.Select(a => a.Armor).DefaultIfEmpty(0).Average() *
                                               t.PercentArmorPenetrationMod - t.FlatArmorPenetrationMod;
                            return (ad * (100 / (100 + (averageArmor > 0 ? averageArmor : 0)))) * t.AttackSpeedMod;
                        }),
                    new Item(
                        "ability-power", "Ability Power", 15, false, delegate(Obj_AI_Hero t)
                        {
                            var averageMr = HeroManager.Allies.Select(a => a.SpellBlock).DefaultIfEmpty(0).Average() *
                                            t.PercentMagicPenetrationMod - t.FlatMagicPenetrationMod;
                            return t.FlatMagicDamageMod * (100 / (100 + (averageMr > 0 ? averageMr : 0)));
                        }),
                    new Item(
                        "low-resists", "Resists", 0, true,
                        t =>
                            ObjectManager.Player.FlatPhysicalDamageMod >= ObjectManager.Player.FlatMagicDamageMod
                                ? t.Armor
                                : t.SpellBlock),
                    new Item("low-health", "Health", 20, true, t => t.Health),
                    new Item(
                        "short-distance-player", "Distance to Player", 5, true, t => t.Distance(ObjectManager.Player)),
                    new Item("short-distance-cursor", "Distance to Cursor", 0, true, t => t.Distance(Game.CursorPos)),
                    new Item(
                        "crowd-control", "Crowd Control", 0, false, delegate(Obj_AI_Hero t)
                        {
                            var buffs =
                                t.Buffs.Where(
                                    x =>
                                        x.Type == BuffType.Charm || x.Type == BuffType.Knockback ||
                                        x.Type == BuffType.Suppression || x.Type == BuffType.Fear ||
                                        x.Type == BuffType.Taunt || x.Type == BuffType.Stun || x.Type == BuffType.Slow ||
                                        x.Type == BuffType.Silence || x.Type == BuffType.Snare ||
                                        x.Type == BuffType.Polymorph).ToList();
                            return buffs.Any() ? buffs.Max(x => x.EndTime) + 1f : 0f;
                        }),
                    new Item(
                        "gold", "Acquired Gold", 0, false,
                        t =>
                            (t.MinionsKilled + t.NeutralMinionsKilled) * 27.35f + t.ChampionsKilled * 300f +
                            t.Assists * 85f),
                    new Item(
                        "team-focus", "Team Focus", 0, false,
                        t =>
                            Aggro.Entries.Where(a => a.Value.Target.Hero.NetworkId == t.NetworkId)
                                .Select(a => a.Value.Value)
                                .DefaultIfEmpty(0)
                                .Sum()),
                    new Item(
                        "focus-me", "Focus Me", 0, false, delegate(Obj_AI_Hero t)
                        {
                            var entry = Aggro.GetSenderTargetEntry(t, ObjectManager.Player);
                            return entry != null ? entry.Value + 1f : 0;
                        })
                };

                MaxRange = 2000f;
            }

            public static Menu WeightsMenu { get; private set; }
            public static HashSet<Item> Items { get; private set; }

            public static float Range
            {
                get { return _range; }
                set
                {
                    if (value <= MaxRange)
                    {
                        _range = value;
                    }
                }
            }

            public static float MaxRange { get; set; }

            internal static void AddToMainMenu()
            {
                WeightsMenu = MainMenu.AddSubMenu(new Menu("Weights", MainMenu.Name + ".weights"));

                var heroesMenu = WeightsMenu.AddSubMenu(new Menu("Hero Weight %", WeightsMenu.Name + ".heroes"));

                foreach (var enemy in Targets.Items)
                {
                    heroesMenu.AddItem(
                        new MenuItem(heroesMenu.Name + "." + enemy.Hero.ChampionName, enemy.Hero.ChampionName).SetValue(
                            new Slider(100, 0, 200)).DontSave());
                }

                foreach (var item in Items)
                {
                    var localItem = item;
                    WeightsMenu.AddItem(
                        new MenuItem(
                            WeightsMenu.Name + "." + item.UniqueName,
                            item.Inverted ? InvertedPrefix + item.DisplayName : item.DisplayName).SetShared()
                            .SetValue(new Slider(localItem.Weight, MinWeight, MaxWeight)));
                    WeightsMenu.Item(WeightsMenu.Name + "." + item.UniqueName).ValueChanged +=
                        delegate(object sender, OnValueChangeEventArgs args)
                        {
                            localItem.OnWeightChange(args.GetNewValue<Slider>().Value);
                        };
                    item.Weight = MainMenu.Item(WeightsMenu.Name + "." + item.UniqueName).GetValue<Slider>().Value;
                }

                var drawingWeightsMenu = DrawingMenu.AddSubMenu(new Menu("Weights", DrawingMenu.Name + ".weights"));

                var drawingWeightsGroupMenu =
                    drawingWeightsMenu.AddSubMenu(
                        new Menu("Highest Weight Target", drawingWeightsMenu.Name + ".highest-target"));
                drawingWeightsGroupMenu.AddItem(
                    new MenuItem(drawingWeightsGroupMenu.Name + ".color", "Color").SetShared()
                        .SetValue(Color.SpringGreen));
                drawingWeightsGroupMenu.AddItem(
                    new MenuItem(drawingWeightsGroupMenu.Name + ".radius", "Radius").SetShared()
                        .SetValue(new Slider(55)));
                drawingWeightsGroupMenu.AddItem(
                    new MenuItem(drawingWeightsGroupMenu.Name + ".enabled", "Enabled").SetShared().SetValue(true));

                drawingWeightsMenu.AddItem(
                    new MenuItem(drawingWeightsMenu.Name + ".simple", "Simple").SetShared().SetValue(false));

                Game.OnInput += OnGameInput;
                Drawing.OnDraw += OnDrawingDraw;
                Game.OnUpdate += OnGameUpdate;
            }

            private static void OnGameInput(GameInputEventArgs args)
            {
                if (args.Input == null)
                {
                    return;
                }
                var input = args.Input.ToLower();
                if (input.Equals("/weights reset", StringComparison.OrdinalIgnoreCase))
                {
                    args.Process = false;
                    RestoreDefaultWeights();
                }
            }

            public static void RestoreDefaultWeights()
            {
                foreach (var item in Items)
                {
                    if (WeightsMenu != null)
                    {
                        var menuItem = WeightsMenu.Item(WeightsMenu.Name + "." + item.UniqueName);
                        if (menuItem != null)
                        {
                            menuItem.SetValue(new Slider(item.DefaultWeight, MinWeight, MaxWeight));
                        }
                    }
                    item.Weight = item.DefaultWeight;
                }
            }

            private static void OnGameUpdate(EventArgs args)
            {
                if (MainMenu == null || Modes.Current.Mode != Mode.Weights)
                {
                    return;
                }

                var highestEnabled =
                    MainMenu.Item(MainMenu.Name + ".drawing.weights.highest-target.enabled").GetValue<bool>();
                var weightsSimple = MainMenu.Item(MainMenu.Name + ".drawing.weights.simple").GetValue<bool>();
                if (highestEnabled || weightsSimple)
                {
                    var enemies = Targets.Items.Where(h => h.Hero.IsValidTarget(Range)).ToList();
                    foreach (var weight in Items.Where(w => w.Weight > 0))
                    {
                        UpdateMaxMinValue(weight, enemies, true);
                    }
                    foreach (var target in enemies)
                    {
                        var totalWeight = Items.Where(w => w.Weight > 0).Sum(w => CalculatedWeight(w, target, true));

                        if (MainMenu != null)
                        {
                            var heroPercent =
                                MainMenu.Item(MainMenu.Name + ".weights.heroes." + target.Hero.ChampionName)
                                    .GetValue<Slider>()
                                    .Value;

                            totalWeight = heroPercent > 0 ? totalWeight / 100 * heroPercent : 0;
                        }

                        target.SimulatedWeight = totalWeight;
                    }
                    _drawingTargets = enemies.OrderByDescending(t => t.SimulatedWeight).ToList();
                    if (Game.Time - _lastBestTargetSwitch >= BestTargetSwitchDelay)
                    {
                        _bestTarget = _drawingTargets.FirstOrDefault();
                        _lastBestTargetSwitch = Game.Time;
                    }
                }
            }

            private static void OnDrawingDraw(EventArgs args)
            {
                if (MainMenu == null || Modes.Current.Mode != Mode.Weights)
                {
                    return;
                }

                var highestEnabled =
                    MainMenu.Item(MainMenu.Name + ".drawing.weights.highest-target.enabled").GetValue<bool>();
                var weightsSimple = MainMenu.Item(MainMenu.Name + ".drawing.weights.simple").GetValue<bool>();

                if (!highestEnabled && !weightsSimple)
                {
                    return;
                }

                var highestRadius =
                    MainMenu.Item(MainMenu.Name + ".drawing.weights.highest-target.radius").GetValue<Slider>().Value;
                var highestColor =
                    MainMenu.Item(MainMenu.Name + ".drawing.weights.highest-target.color").GetValue<Color>();
                var circleThickness =
                    MainMenu.Item(MainMenu.Name + ".drawing.circle-thickness").GetValue<Slider>().Value;

                if (weightsSimple)
                {
                    foreach (var target in
                        _drawingTargets.Where(
                            target => !target.Hero.IsDead && target.Hero.IsVisible && target.Hero.Position.IsOnScreen())
                        )
                    {
                        Drawing.DrawText(
                            target.Hero.HPBarPosition.X + 55f, target.Hero.HPBarPosition.Y - 20f, Color.White,
                            target.SimulatedWeight.ToString("0.0").Replace(",", "."));
                    }
                }
                if (highestEnabled && _bestTarget != null && !_bestTarget.Hero.IsDead && _bestTarget.Hero.IsVisible &&
                    _drawingTargets.Count(e => !e.Hero.IsDead && e.Hero.IsVisible && e.Hero.Position.IsOnScreen()) >= 2)
                {
                    Render.Circle.DrawCircle(
                        _bestTarget.Hero.Position, _bestTarget.Hero.BoundingRadius + highestRadius, highestColor,
                        circleThickness, true);
                }
            }

            /// <exception cref="ArgumentException">Unique Name does already exist.</exception>
            /// <exception cref="ArgumentException">Display Name is empty or null.</exception>
            /// <exception cref="ArgumentException">Value Function is null.</exception>
            public static void AddItem(Item item)
            {
                if (Items.Any(i => i.UniqueName.Equals(item.UniqueName)))
                {
                    throw new ArgumentException(
                        string.Format("Weights: Unique Name \"{0}\" already exist.", item.UniqueName));
                }
                if (string.IsNullOrEmpty(item.DisplayName))
                {
                    throw new ArgumentException(
                        string.Format("Weights: Display Name \"{0}\" can't be empty or null.", item.DisplayName));
                }
                if (item.ValueFunction == null)
                {
                    throw new ArgumentException("Modes: Value Function can't be null.");
                }

                Items.Add(item);

                if (WeightsMenu != null)
                {
                    if (!_separated)
                    {
                        WeightsMenu.AddItem(new MenuItem(WeightsMenu.Name + ".separator", string.Empty));
                        _separated = true;
                    }
                    var weightItem =
                        new MenuItem(
                            WeightsMenu.Name + "." + item.UniqueName,
                            item.Inverted ? InvertedPrefix + item.DisplayName : item.DisplayName).SetValue(
                                new Slider(item.Weight, MinWeight, MaxWeight)).SetShared();
                    if (!string.IsNullOrWhiteSpace(item.Tooltip))
                    {
                        weightItem.SetTooltip(item.Tooltip);
                    }
                    WeightsMenu.AddItem(weightItem);
                    WeightsMenu.Item(WeightsMenu.Name + "." + item.UniqueName).ValueChanged +=
                        delegate(object sender, OnValueChangeEventArgs args)
                        {
                            item.OnWeightChange(args.GetNewValue<Slider>().Value);
                        };
                    item.Weight = MainMenu.Item(WeightsMenu.Name + "." + item.UniqueName).GetValue<Slider>().Value;
                }
            }

            public static Item GetItem(string uniqueName, StringComparison comp = StringComparison.OrdinalIgnoreCase)
            {
                return Items.FirstOrDefault(w => w.UniqueName.Equals(uniqueName, comp));
            }

            public static float CalculatedWeight(Item item, Targets.Item target, bool simulation = false)
            {
                if (item.Weight == 0)
                {
                    return 0;
                }
                var weight = item.Weight *
                             (item.Inverted
                                 ? (simulation ? item.SimulationMinValue : item.MinValue)
                                 : GetValue(item, target)) / (item.Inverted ? GetValue(item, target) : item.MaxValue);
                return float.IsNaN(weight) || float.IsInfinity(weight) ? MinWeight : weight;
            }

            public static float GetValue(Item item, Targets.Item target)
            {
                try
                {
                    var value = item.ValueFunction(target.Hero);
                    return value >= 0 ? value : 0;
                }
                catch
                {
                    return item.Inverted ? item.MaxValue : item.MinValue;
                }
            }

            private static void UpdateMaxMinValue(Item item, IEnumerable<Targets.Item> targets, bool simulation = false)
            {
                var min = float.MaxValue;
                var max = float.MinValue;
                foreach (var target in targets)
                {
                    var value = GetValue(item, target);
                    if (value < min)
                    {
                        min = value;
                    }
                    if (value > max)
                    {
                        max = value;
                    }
                }
                if (!simulation)
                {
                    item.MinValue = min > 1 ? min : 1;
                    item.MaxValue = max > min ? max : min + 1;
                }
                else
                {
                    item.SimulationMinValue = min > 1 ? min : 1;
                    item.SimulationMaxValue = max > min ? max : min + 1;
                }
            }

            public static IEnumerable<Targets.Item> OrderChampions(IEnumerable<Targets.Item> targets)
            {
                if (targets == null)
                {
                    return new List<Targets.Item>();
                }
                var targetList = targets.ToList();
                foreach (var item in Items.Where(w => w.Weight > 0))
                {
                    UpdateMaxMinValue(item, targetList);
                }

                foreach (var target in targetList)
                {
                    var tmpWeight = Items.Where(w => w.Weight > 0).Sum(w => CalculatedWeight(w, target));

                    if (MainMenu != null)
                    {
                        var heroPercent =
                            MainMenu.Item(MainMenu.Name + ".weights.heroes." + target.Hero.ChampionName)
                                .GetValue<Slider>()
                                .Value;

                        tmpWeight = heroPercent > 0 ? tmpWeight / 100 * heroPercent : 0;
                    }

                    target.Weight = tmpWeight;
                }
                return Focus.Enabled && Focus.Force && targetList.Count > 1
                    ? new List<Targets.Item> { targetList.OrderByDescending(t => t.Weight).First() }
                    : targetList.OrderByDescending(t => t.Weight).ToList();
            }

            public class Item
            {
                private string _displayName;
                private string _tooltip;
                private int _weight;

                public Item(string uniqueName,
                    string displayName,
                    int weight,
                    bool inverted,
                    Func<Obj_AI_Hero, float> valueFunction)
                {
                    ValueFunction = valueFunction;
                    UniqueName = uniqueName;
                    DisplayName = displayName;
                    Weight = weight;
                    DefaultWeight = weight;
                    Inverted = inverted;
                }

                public Func<Obj_AI_Hero, float> ValueFunction { get; set; }
                public string UniqueName { get; private set; }

                public string DisplayName
                {
                    get { return _displayName; }
                    set
                    {
                        _displayName = value;
                        if (WeightsMenu != null)
                        {
                            var item = MainMenu.Item(WeightsMenu.Name + "." + UniqueName);
                            if (item != null)
                            {
                                item.DisplayName = DisplayName;
                            }
                        }
                    }
                }

                public string Tooltip
                {
                    get { return _tooltip; }
                    set
                    {
                        _tooltip = value;
                        if (WeightsMenu != null)
                        {
                            var item = MainMenu.Item(WeightsMenu.Name + "." + UniqueName);
                            if (item != null)
                            {
                                item.SetTooltip(Tooltip);
                            }
                        }
                    }
                }

                public int Weight
                {
                    get { return _weight; }
                    set
                    {
                        OnWeightChange(value);
                        if (WeightsMenu != null)
                        {
                            var item = MainMenu.Item(WeightsMenu.Name + "." + UniqueName);
                            if (item != null)
                            {
                                item.SetValue(new Slider(Weight, MinWeight, MaxWeight));
                            }
                        }
                    }
                }

                public int DefaultWeight { get; private set; }
                public bool Inverted { get; set; }
                public float MaxValue { get; internal set; }
                public float MinValue { get; internal set; }
                public float SimulationMaxValue { get; internal set; }
                public float SimulationMinValue { get; internal set; }

                internal void OnWeightChange(int newWeight)
                {
                    _weight = Math.Min(MaxWeight, Math.Max(MinWeight, newWeight));
                }
            }
        }
    }
}