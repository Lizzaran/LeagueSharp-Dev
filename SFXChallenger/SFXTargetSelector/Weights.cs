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
        private const string InvertedPrefix = "[i] ";
        private const float BestTargetSwitchDelay = 0.5f;
        private static Menu _mainMenu;
        private static Menu _weightsMenu;
        private static float _range;
        private static bool _separated;
        private static List<Targets.Item> _drawingTargets;
        private static Targets.Item _bestTarget;
        private static float _lastBestTargetSwitch;

        static Weights()
        {
            try
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
                            ad += ad / 100 * (t.Crit * 100) * (t.HasItem(ItemData.Infinity_Edge.Id) ? 2.5f : 2f);
                            var averageArmor = GameObjects.AllyHeroes.Select(a => a.Armor).DefaultIfEmpty(0).Average() *
                                               t.PercentArmorPenetrationMod - t.FlatArmorPenetrationMod;
                            return (ad * (100 / (100 + (averageArmor > 0 ? averageArmor : 0)))) * t.AttackSpeedMod;
                        }),
                    new Item(
                        "ability-power", "Ability Power", 15, false, delegate(Obj_AI_Hero t)
                        {
                            var averageMr =
                                GameObjects.AllyHeroes.Select(a => a.SpellBlock).DefaultIfEmpty(0).Average() *
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
                            (t.MinionsKilled + t.NeutralMinionsKilled) * 22.35f + t.ChampionsKilled * 300f +
                            t.Assists * 95f),
                    new Item(
                        "team-focus", "Team Focus", 0, false,
                        t =>
                            Aggro.Items.Where(a => a.Value.Target.Hero.NetworkId == t.NetworkId)
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

                Average = (float) Items.Select(w => w.Weight).DefaultIfEmpty(0).Average();
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

                _weightsMenu = mainMenu.AddSubMenu(new Menu("Weights", mainMenu.Name + ".weights"));

                var heroesMenu = _weightsMenu.AddSubMenu(new Menu("Hero Weight %", _weightsMenu.Name + ".heroes"));

                foreach (var enemy in Targets.Items)
                {
                    heroesMenu.AddItem(
                        new MenuItem(heroesMenu.Name + "." + enemy.Hero.ChampionName, enemy.Hero.ChampionName).SetValue(
                            new Slider(100, 0, 200)).DontSave());
                }

                foreach (var item in Items)
                {
                    var localItem = item;
                    _weightsMenu.AddItem(
                        new MenuItem(
                            _weightsMenu.Name + "." + item.Name,
                            item.Inverted ? InvertedPrefix + item.DisplayName : item.DisplayName).SetShared()
                            .SetValue(new Slider(localItem.Weight, MinWeight, MaxWeight)));
                    _weightsMenu.Item(_weightsMenu.Name + "." + item.Name).ValueChanged +=
                        delegate(object sender, OnValueChangeEventArgs args)
                        {
                            localItem.Weight = args.GetNewValue<Slider>().Value;
                            Average = (float) Items.Select(w => w.Weight).DefaultIfEmpty(0).Average();
                        };
                    item.Weight = mainMenu.Item(_weightsMenu.Name + "." + item.Name).GetValue<Slider>().Value;
                }

                var drawingWeightsMenu = drawingMenu.AddSubMenu(new Menu("Weights", drawingMenu.Name + ".weights"));

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
                Core.OnPreUpdate += OnCorePreUpdate;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private static void OnGameInput(GameInputEventArgs args)
        {
            try
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
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static void RestoreDefaultWeights()
        {
            try
            {
                foreach (var item in Items)
                {
                    if (_weightsMenu != null)
                    {
                        var menuItem = _weightsMenu.Item(_weightsMenu.Name + "." + item.Name);
                        if (menuItem != null)
                        {
                            menuItem.SetValue(new Slider(item.DefaultWeight, MinWeight, MaxWeight));
                        }
                    }
                    item.Weight = item.DefaultWeight;
                }
                Average = (float) Items.Select(w => w.Weight).DefaultIfEmpty(0).Average();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private static void OnCorePreUpdate(EventArgs args)
        {
            try
            {
                if (_mainMenu == null || TargetSelector.Mode != TargetSelectorModeType.Weights)
                {
                    return;
                }

                var highestEnabled =
                    _mainMenu.Item(_mainMenu.Name + ".drawing.weights.highest-target.enabled").GetValue<bool>();
                var weightsSimple = _mainMenu.Item(_mainMenu.Name + ".drawing.weights.simple").GetValue<bool>();
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

                        if (_mainMenu != null)
                        {
                            var heroPercent =
                                _mainMenu.Item(_mainMenu.Name + ".weights.heroes." + target.Hero.ChampionName)
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
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private static void OnDrawingDraw(EventArgs args)
        {
            try
            {
                if (_mainMenu == null || TargetSelector.Mode != TargetSelectorModeType.Weights)
                {
                    return;
                }

                var highestEnabled =
                    _mainMenu.Item(_mainMenu.Name + ".drawing.weights.highest-target.enabled").GetValue<bool>();
                var weightsSimple = _mainMenu.Item(_mainMenu.Name + ".drawing.weights.simple").GetValue<bool>();

                if (!highestEnabled && !weightsSimple)
                {
                    return;
                }

                var highestRadius =
                    _mainMenu.Item(_mainMenu.Name + ".drawing.weights.highest-target.radius").GetValue<Slider>().Value;
                var highestColor =
                    _mainMenu.Item(_mainMenu.Name + ".drawing.weights.highest-target.color").GetValue<Color>();
                var circleThickness =
                    _mainMenu.Item(_mainMenu.Name + ".drawing.circle-thickness").GetValue<Slider>().Value;

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
                    if (!_separated)
                    {
                        _weightsMenu.AddItem(new MenuItem(_weightsMenu.Name + ".separator", string.Empty));
                        _separated = true;
                    }
                    _weightsMenu.AddItem(
                        new MenuItem(
                            _weightsMenu.Name + "." + item.Name,
                            item.Inverted ? InvertedPrefix + item.DisplayName : item.DisplayName).SetValue(
                                new Slider(item.Weight, MinWeight, MaxWeight)));
                    _weightsMenu.Item(_weightsMenu.Name + "." + item.Name).ValueChanged +=
                        delegate(object sender, OnValueChangeEventArgs args)
                        {
                            item.Weight = args.GetNewValue<Slider>().Value;
                            Average = (float) Items.Select(w => w.Weight).DefaultIfEmpty(0).Average();
                        };
                    item.Weight = _mainMenu.Item(_weightsMenu.Name + "." + item.Name).GetValue<Slider>().Value;
                }

                Average = (float) Items.Select(w => w.Weight).DefaultIfEmpty(0).Average();
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
                var weight = item.Weight *
                             (item.Inverted
                                 ? (simulation ? item.SimulationMinValue : item.MinValue)
                                 : GetValue(item, target)) / (item.Inverted ? GetValue(item, target) : item.MaxValue);
                return float.IsNaN(weight) || float.IsInfinity(weight) ? MinWeight : weight;
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

                foreach (var target in targets)
                {
                    var tmpWeight = Items.Where(w => w.Weight > 0).Sum(w => CalculatedWeight(w, target));

                    if (_mainMenu != null)
                    {
                        var heroPercent =
                            _mainMenu.Item(_mainMenu.Name + ".weights.heroes." + target.Hero.ChampionName)
                                .GetValue<Slider>()
                                .Value;

                        tmpWeight = heroPercent > 0 ? tmpWeight / 100 * heroPercent : 0;
                    }

                    target.Weight = tmpWeight;
                }
                return TargetSelector.ForceFocus && targets.Count > 1
                    ? new List<Targets.Item> { targets.OrderByDescending(t => t.Weight).First() }
                    : targets.OrderByDescending(t => t.Weight).ToList();
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
                DefaultWeight = weight;
                Inverted = inverted;
            }

            public Func<Obj_AI_Hero, float> GetValueFunc { get; set; }
            public string Name { get; set; }
            public string DisplayName { get; set; }
            public int Weight { get; set; }
            public int DefaultWeight { get; private set; }
            public bool Inverted { get; set; }
            public float MaxValue { get; set; }
            public float MinValue { get; set; }
            public float SimulationMaxValue { get; set; }
            public float SimulationMinValue { get; set; }
        }
    }
}