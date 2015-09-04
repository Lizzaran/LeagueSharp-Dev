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
using SFXChallenger.Enumerations;
using SFXChallenger.Library;
using SFXChallenger.Library.Extensions.LeagueSharp;
using SFXChallenger.Library.Logger;
using ItemData = LeagueSharp.Common.Data.ItemData;

#endregion

/*
 * Don't copy paste this without asking & giving credits fuckers :^) 
 */

namespace SFXChallenger.SFXTargetSelector
{
    public static class Weights
    {
        public const int MinWeight = 0;
        public const int MaxWeight = 20;
        public const int MinMultiplicator = 1;
        public const int MaxMultiplicator = 5;
        private static Menu _mainMenu;
        private static Menu _weightsMenu;
        private static float _range;

        static Weights()
        {
            try
            {
                Items = new HashSet<Item>
                {
                    new Item(
                        "killable", Global.Lang.Get("TS_AAKillable"), 20, false,
                        t => t.Health < ObjectManager.Player.GetAutoAttackDamage(t, true) ? 10 : 0),
                    new Item(
                        "attack-damage", Global.Lang.Get("TS_AttackDamage"), 10, false, delegate(Obj_AI_Hero t)
                        {
                            var ad = t.FlatPhysicalDamageMod;
                            ad += ad / 100 * (t.Crit * 100) * (t.HasItem(ItemData.Infinity_Edge.Id) ? 2.5f : 2f);
                            var averageArmor = GameObjects.AllyHeroes.Average(a => a.Armor) *
                                               t.PercentArmorPenetrationMod - t.FlatArmorPenetrationMod;
                            return (ad * (100 / (100 + (averageArmor > 0 ? averageArmor : 0)))) * t.AttackSpeedMod;
                        }),
                    new Item(
                        "ability-power", Global.Lang.Get("TS_AbilityPower"), 10, false, delegate(Obj_AI_Hero t)
                        {
                            var averageMr = GameObjects.AllyHeroes.Average(a => a.SpellBlock) *
                                            t.PercentMagicPenetrationMod - t.FlatMagicPenetrationMod;
                            return t.FlatMagicDamageMod * (100 / (100 + (averageMr > 0 ? averageMr : 0)));
                        }),
                    new Item(
                        "low-resists", Global.Lang.Get("TS_LowResists"), 6, true,
                        t =>
                            ObjectManager.Player.FlatPhysicalDamageMod >= ObjectManager.Player.FlatMagicDamageMod
                                ? t.Armor
                                : t.SpellBlock),
                    new Item("low-health", Global.Lang.Get("TS_LowHealth"), 8, true, t => t.Health),
                    new Item(
                        "short-distance", Global.Lang.Get("TS_ShortDistance"), 7, true,
                        t => t.Distance(ObjectManager.Player)),
                    new Item(
                        "team-focus", Global.Lang.Get("TS_TeamFocus"), 3, false,
                        t =>
                            Aggro.Items.Where(a => a.Value.Target.Hero.NetworkId == t.NetworkId)
                                .Select(a => a.Value.Value)
                                .DefaultIfEmpty(0)
                                .Sum()),
                    new Item(
                        "focus-me", Global.Lang.Get("TS_FocusMe"), 3, false, delegate(Obj_AI_Hero t)
                        {
                            var entry = Aggro.GetSenderTargetEntry(t, ObjectManager.Player);
                            return entry != null ? entry.Value + 1f : 0;
                        }),
                    new Item(
                        "hard-cc", Global.Lang.Get("TS_HardCC"), 5, false, delegate(Obj_AI_Hero t)
                        {
                            var buffs =
                                t.Buffs.Where(
                                    x =>
                                        x.Type == BuffType.Charm || x.Type == BuffType.Knockback ||
                                        x.Type == BuffType.Suppression || x.Type == BuffType.Fear ||
                                        x.Type == BuffType.Taunt || x.Type == BuffType.Stun).ToList();
                            return buffs.Any() ? buffs.Max(x => x.EndTime) + 1f : 0f;
                        }),
                    new Item(
                        "soft-cc", Global.Lang.Get("TS_SoftCC"), 5, false, delegate(Obj_AI_Hero t)
                        {
                            var buffs =
                                t.Buffs.Where(
                                    x =>
                                        x.Type == BuffType.Slow || x.Type == BuffType.Silence ||
                                        x.Type == BuffType.Snare || x.Type == BuffType.Polymorph).ToList();
                            return buffs.Any() ? buffs.Max(x => x.EndTime) + 1f : 0f;
                        })/*,
                    new Item(
                        "gold", Global.Lang.Get("TS_Gold"), 7, false,
                        t =>
                            (t.MinionsKilled + t.NeutralMinionsKilled) * 22.35f + t.ChampionsKilled * 300f +
                            t.Assists * 95f)*/ //Bug: Bugsplatting currently 
                };

                Average = (float) Items.Average(w => w.Weight);
                MaxRange = 2000f;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static float Average { get; private set; }
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

        internal static void AddToMenu(Menu mainMenu, Menu drawingMenu)
        {
            try
            {
                _mainMenu = mainMenu;

                _weightsMenu = mainMenu.AddSubMenu(new Menu(Global.Lang.Get("TS_Weights"), mainMenu.Name + ".weights"));

                var heroesMenu =
                    _weightsMenu.AddSubMenu(new Menu(Global.Lang.Get("G_Heroes"), _weightsMenu.Name + ".heroes"));

                heroesMenu.AddItem(
                    new MenuItem(heroesMenu.Name + ".weight-multiplicator", Global.Lang.Get("TS_WeightMultiplicator"))
                        .SetValue(new Slider(1, MinMultiplicator, MaxMultiplicator)));

                foreach (var enemy in Targets.Items)
                {
                    heroesMenu.AddItem(
                        new MenuItem(heroesMenu.Name + "." + enemy.Hero.ChampionName, enemy.Hero.ChampionName).SetValue(
                            new Slider(1, MinMultiplicator, MaxMultiplicator)));
                }

                foreach (var item in Items)
                {
                    var localItem = item;
                    _weightsMenu.AddItem(
                        new MenuItem(_weightsMenu.Name + "." + item.Name, item.DisplayName).SetValue(
                            new Slider(localItem.Weight, MinWeight, MaxWeight)));
                    _weightsMenu.Item(_weightsMenu.Name + "." + item.Name).ValueChanged +=
                        delegate(object sender, OnValueChangeEventArgs args)
                        {
                            localItem.Weight = args.GetNewValue<Slider>().Value;
                            Average = (float) Items.Average(w => w.Weight);
                        };
                    item.Weight = mainMenu.Item(_weightsMenu.Name + "." + item.Name).GetValue<Slider>().Value;
                }

                var drawingWeightsMenu =
                    drawingMenu.AddSubMenu(new Menu(Global.Lang.Get("TS_Weights"), drawingMenu.Name + ".weights"));

                var drawingWeightsGroupMenu =
                    drawingWeightsMenu.AddSubMenu(
                        new Menu(Global.Lang.Get("TS_BestGroupTarget"), drawingWeightsMenu.Name + ".group-target"));
                drawingWeightsGroupMenu.AddItem(
                    new MenuItem(drawingWeightsGroupMenu.Name + ".color", Global.Lang.Get("G_Color")).SetValue(
                        Color.HotPink));
                drawingWeightsGroupMenu.AddItem(
                    new MenuItem(drawingWeightsGroupMenu.Name + ".radius", Global.Lang.Get("G_Radius")).SetValue(
                        new Slider(25)));
                drawingWeightsGroupMenu.AddItem(
                    new MenuItem(drawingWeightsGroupMenu.Name + ".enabled", Global.Lang.Get("G_Enabled")).SetValue(
                        false));

                drawingWeightsMenu.AddItem(
                    new MenuItem(
                        drawingWeightsMenu.Name + ".simple",
                        Global.Lang.Get("TS_Weights") + " " + Global.Lang.Get("G_Simple")).SetValue(false));
                drawingWeightsMenu.AddItem(
                    new MenuItem(
                        drawingWeightsMenu.Name + ".advanced",
                        Global.Lang.Get("TS_Weights") + " " + Global.Lang.Get("G_Advanced")).SetValue(false));
                drawingWeightsMenu.AddItem(
                    new MenuItem(
                        drawingWeightsMenu.Name + ".range-check",
                        Global.Lang.Get("TS_Weights") + " " + Global.Lang.Get("TS_RangeCheck")).SetValue(false));

                Drawing.OnDraw += OnDrawingDraw;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private static void OnDrawingDraw(EventArgs args)
        {
            try
            {
                if (_mainMenu == null)
                {
                    return;
                }

                var groupEnabled =
                    _mainMenu.Item(_mainMenu.Name + ".drawing.weights.group-target.enabled").GetValue<bool>();
                var groupRadius =
                    _mainMenu.Item(_mainMenu.Name + ".drawing.weights.group-target.radius").GetValue<Slider>().Value;
                var groupColor =
                    _mainMenu.Item(_mainMenu.Name + ".drawing.weights.group-target.color").GetValue<Color>();

                var weightsRangeCheck = _mainMenu.Item(_mainMenu.Name + ".drawing.weights.range-check").GetValue<bool>();
                var weightsSimple = _mainMenu.Item(_mainMenu.Name + ".drawing.weights.simple").GetValue<bool>();
                var weightsAdvanced = _mainMenu.Item(_mainMenu.Name + ".drawing.weights.advanced").GetValue<bool>();

                var weightMultiplicator =
                    _mainMenu.Item(_mainMenu.Name + ".weights.heroes.weight-multiplicator").GetValue<Slider>().Value;
                var circleThickness =
                    _mainMenu.Item(_mainMenu.Name + ".drawing.circle-thickness").GetValue<Slider>().Value;

                if ((groupEnabled || weightsSimple || weightsAdvanced) &&
                    TargetSelector.Mode == TargetSelectorModeType.Weights)
                {
                    var enemies =
                        Targets.Items.Where(
                            h =>
                                h.Hero.IsValidTarget(
                                    groupEnabled ? Math.Max(1750f, Range) : (weightsRangeCheck ? Range : float.MaxValue)))
                            .ToList();
                    foreach (var weight in Items.Where(w => w.Weight > 0))
                    {
                        UpdateMaxMinValue(weight, enemies, true);
                    }
                    Targets.Item bestTarget = null;
                    var bestTargetWeight = float.MinValue;
                    foreach (var target in enemies.Where(e => e.Hero.Position.IsOnScreen()))
                    {
                        var position = Drawing.WorldToScreen(target.Hero.Position);
                        var totalWeight = 0f;
                        var offset = 0f;
                        foreach (var weight in Items.Where(w => w.Weight > 0))
                        {
                            var lastWeight = CalculatedWeight(weight, target, true);
                            if (lastWeight > 0)
                            {
                                if (_mainMenu != null)
                                {
                                    var heroMultiplicator =
                                        _mainMenu.Item(_mainMenu.Name + ".weights.heroes." + target.Hero.ChampionName)
                                            .GetValue<Slider>()
                                            .Value;
                                    if (heroMultiplicator > 1)
                                    {
                                        lastWeight += Average * heroMultiplicator;
                                    }
                                    if (weightMultiplicator > 1)
                                    {
                                        lastWeight *= weightMultiplicator;
                                    }
                                }
                                if (weightsAdvanced)
                                {
                                    Drawing.DrawText(
                                        position.X + target.Hero.BoundingRadius, position.Y - 100 + offset, Color.White,
                                        lastWeight.ToString("0.0").Replace(",", ".") + " - " + weight.DisplayName);
                                    offset += 17f;
                                }
                                totalWeight += lastWeight;
                            }
                        }
                        if (weightsSimple)
                        {
                            Drawing.DrawText(
                                target.Hero.HPBarPosition.X + 55f, target.Hero.HPBarPosition.Y - 20f, Color.White,
                                totalWeight.ToString("0.0").Replace(",", "."));
                        }
                        if (groupEnabled)
                        {
                            if (totalWeight > bestTargetWeight)
                            {
                                bestTargetWeight = totalWeight;
                                bestTarget = target;
                            }
                        }
                    }
                    if (groupEnabled && bestTarget != null && enemies.Count >= 2)
                    {
                        Render.Circle.DrawCircle(
                            bestTarget.Hero.Position, bestTarget.Hero.BoundingRadius + groupRadius, groupColor,
                            circleThickness, true);
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static void AddItem(Item item)
        {
            try
            {
                if (GetItem(item.Name) != null || Items.Contains(item))
                {
                    return;
                }

                Items.Add(item);

                if (_weightsMenu != null)
                {
                    _weightsMenu.AddItem(
                        new MenuItem(_weightsMenu.Name + "." + item.Name, item.DisplayName).SetValue(
                            new Slider(item.Weight, MinWeight, MaxWeight)));
                    _weightsMenu.Item(_weightsMenu.Name + "." + item.Name).ValueChanged +=
                        delegate(object sender, OnValueChangeEventArgs args)
                        {
                            item.Weight = args.GetNewValue<Slider>().Value;
                            Average = (float) Items.Average(w => w.Weight);
                        };
                    item.Weight = _mainMenu.Item(_weightsMenu.Name + "." + item.Name).GetValue<Slider>().Value;
                }

                Average = (float) Items.Average(w => w.Weight);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static Item GetItem(string name, StringComparison comp = StringComparison.OrdinalIgnoreCase)
        {
            return Items.FirstOrDefault(w => w.Name.Equals(name, comp));
        }

        public static float CalculatedWeight(Item item, Targets.Item target, bool simulation = false)
        {
            try
            {
                if (item.Weight == 0)
                {
                    return 0;
                }
                return item.Inverted
                    ? item.Weight -
                      (item.Weight * (GetValue(item, target) - (simulation ? item.SimulationMinValue : item.MinValue)) /
                       (simulation ? item.SimulationMaxValue : item.MaxValue))
                    : item.Weight * GetValue(item, target) / (simulation ? item.SimulationMaxValue : item.MaxValue);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return 0;
        }

        public static float GetValue(Item item, Targets.Item target)
        {
            try
            {
                var value = item.GetValueFunc(target.Hero);
                return value > 1 ? value : 1;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
                return item.Inverted ? item.MaxValue : item.MinValue;
            }
        }

        public static void UpdateMaxMinValue(Item item, IEnumerable<Targets.Item> targets, bool simulation = false)
        {
            try
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
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static IEnumerable<Targets.Item> OrderChampions(List<Targets.Item> targets)
        {
            try
            {
                foreach (var item in Items.Where(w => w.Weight > 0))
                {
                    UpdateMaxMinValue(item, targets);
                }

                var multiplicator = _mainMenu != null
                    ? _mainMenu.Item(_mainMenu.Name + ".weights.heroes.weight-multiplicator").GetValue<Slider>().Value
                    : 1;
                foreach (var target in targets)
                {
                    var tmpWeight = Items.Where(w => w.Weight > 0).Sum(w => CalculatedWeight(w, target));

                    if (_mainMenu != null)
                    {
                        var heroMultiplicator =
                            _mainMenu.Item(_mainMenu.Name + ".weights.heroes." + target.Hero.ChampionName)
                                .GetValue<Slider>()
                                .Value;
                        if (heroMultiplicator > 1)
                        {
                            tmpWeight += Average * heroMultiplicator;
                        }
                        if (multiplicator > 1)
                        {
                            tmpWeight *= multiplicator;
                        }
                    }

                    target.Weight = tmpWeight;
                }
                return targets.OrderByDescending(t => t.Weight);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return new List<Targets.Item>();
        }

        public class Item
        {
            public Item(string name, string displayName, int weight, bool inverted, Func<Obj_AI_Hero, float> getValue)
            {
                GetValueFunc = getValue;
                Name = name;
                DisplayName = displayName;
                Weight = weight;
                Inverted = inverted;
            }

            public Func<Obj_AI_Hero, float> GetValueFunc { get; set; }
            public string Name { get; set; }
            public string DisplayName { get; set; }
            public int Weight { get; set; }
            public bool Inverted { get; set; }
            public float MaxValue { get; set; }
            public float MinValue { get; set; }
            public float SimulationMaxValue { get; set; }
            public float SimulationMinValue { get; set; }
        }
    }
}