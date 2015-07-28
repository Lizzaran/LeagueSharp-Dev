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
using SFXLibrary;
using SFXLibrary.Extensions.LeagueSharp;
using SFXLibrary.Logger;
using SharpDX;
using Color = System.Drawing.Color;
using ItemData = LeagueSharp.Common.Data.ItemData;

#endregion

namespace SFXChallenger.Wrappers
{
    internal class TargetSelector
    {
        internal const int MinWeight = 0;
        internal const int MaxWeight = 20;
        private const int AggroFadeTime = 10;
        private const int MinMultiplicator = 1;
        private const int MaxMultiplicator = 5;
        private const float SelectClickBuffer = 100f;
        private const float MinionGold = 22.34f;
        private const float KillGold = 300.00f;
        private const float AssistGold = 95.00f;
        private static float _averageWeight;
        private static float _debugRange;
        private static Menu _menu;
        private static Obj_AI_Hero _lastTarget;
        private static Obj_AI_Hero _selectedTarget;
        private static readonly HashSet<Priority> Priorities;
        private static readonly HashSet<WeightedItem> WeightedItems;
        private static readonly Dictionary<int, AggroItem> AggroItems = new Dictionary<int, AggroItem>();
        private static TargetSelectorModeType _tsMode = TargetSelectorModeType.Weights;
        private static Menu _weightsMenu;

        static TargetSelector()
        {
            #region Settings

            Priorities = new HashSet<Priority>
            {
                new Priority
                {
                    Champions =
                        new[]
                        {
                            "Ahri", "Anivia", "Annie", "Ashe", "Azir", "Brand", "Caitlyn", "Cassiopeia", "Corki", "Draven",
                            "Ezreal", "Graves", "Jinx", "Kalista", "Karma", "Karthus", "Katarina", "Kennen", "KogMaw",
                            "Leblanc", "Lucian", "Lux", "Malzahar", "MasterYi", "MissFortune", "Orianna", "Quinn",
                            "Sivir", "Syndra", "Talon", "Teemo", "Tristana", "TwistedFate", "Twitch", "Varus", "Vayne",
                            "Veigar", "VelKoz", "Viktor", "Xerath", "Zed", "Ziggs"
                        },
                    Type = TargetSelectorPriorityType.Highest
                },
                new Priority
                {
                    Champions =
                        new[]
                        {
                            "Akali", "Diana", "Ekko", "Fiddlesticks", "Fiora", "Fizz", "Heimerdinger", "Jayce", "Kassadin",
                            "Kayle", "Kha'Zix", "Lissandra", "Mordekaiser", "Nidalee", "Riven", "Shaco", "Vladimir",
                            "Yasuo", "Zilean"
                        },
                    Type = TargetSelectorPriorityType.High
                },
                new Priority
                {
                    Champions =
                        new[]
                        {
                            "Aatrox", "Darius", "Elise", "Evelynn", "Galio", "Gangplank", "Gragas", "Irelia", "Jax",
                            "Lee Sin", "Maokai", "Morgana", "Nocturne", "Pantheon", "Poppy", "Rengar", "Rumble", "Ryze",
                            "Swain", "Trundle", "Tryndamere", "Udyr", "Urgot", "Vi", "XinZhao", "RekSai"
                        },
                    Type = TargetSelectorPriorityType.Medium
                },
                new Priority
                {
                    Champions =
                        new[]
                        {
                            "Alistar", "Amumu", "Bard", "Blitzcrank", "Braum", "Cho'Gath", "Dr. Mundo", "Garen", "Gnar",
                            "Hecarim", "Janna", "Jarvan IV", "Leona", "Lulu", "Malphite", "Nami", "Nasus", "Nautilus",
                            "Nunu", "Olaf", "Rammus", "Renekton", "Sejuani", "Shen", "Shyvana", "Singed", "Sion",
                            "Skarner", "Sona", "Soraka", "Taric", "Thresh", "Volibear", "Warwick", "MonkeyKing",
                            "Yorick", "Zac", "Zyra"
                        },
                    Type = TargetSelectorPriorityType.Low
                }
            };
            WeightedItems = new HashSet<WeightedItem>
            {
                new WeightedItem(
                    "killable", Global.Lang.Get("TS_AAKillable"), 20, false, 333, 333,
                    t => t.Health < ObjectManager.Player.GetAutoAttackDamage(t, true) ? 1 : 0),
                new WeightedItem(
                    "attack-damage", Global.Lang.Get("TS_AttackDamage"), 10, false, 333, 6500, delegate(Obj_AI_Hero t)
                    {
                        var ad = (t.BaseAttackDamage + t.FlatPhysicalDamageMod);
                        ad += ad / 100 * (t.Crit * 100) * (t.HasItem(ItemData.Infinity_Edge.Id) ? 2.5f : 2f);
                        var averageArmor = GameObjects.AllyHeroes.Average(a => a.Armor) *
                                           ObjectManager.Player.PercentArmorPenetrationMod - t.FlatArmorPenetrationMod;
                        return (ad * (100 / (100 + (averageArmor > 0 ? averageArmor : 0)))) * t.AttackSpeedMod;
                    }),
                new WeightedItem(
                    "ability-power", Global.Lang.Get("TS_AbilityPower"), 10, false, 333, 7000, delegate(Obj_AI_Hero t)
                    {
                        var averageMr = GameObjects.AllyHeroes.Average(a => a.SpellBlock) *
                                        ObjectManager.Player.PercentMagicPenetrationMod - t.FlatMagicPenetrationMod;
                        return (t.BaseAbilityDamage + t.FlatMagicDamageMod) *
                               (100 / (100 + (averageMr > 0 ? averageMr : 0)));
                    }),
                new WeightedItem(
                    "low-resists", Global.Lang.Get("TS_LowResists"), 6, true, 333, 7500,
                    t =>
                        ObjectManager.Player.FlatPhysicalDamageMod >= ObjectManager.Player.FlatMagicDamageMod
                            ? t.Armor
                            : t.SpellBlock),
                new WeightedItem("low-health", Global.Lang.Get("TS_LowHealth"), 8, true, 333, 333, t => t.Health),
                new WeightedItem(
                    "short-distance", Global.Lang.Get("TS_ShortDistance"), 7, true, 333, 333,
                    t => t.Distance(ObjectManager.Player)),
                new WeightedItem(
                    "team-focus", Global.Lang.Get("TS_TeamFocus"), 3, false, 333, 1250,
                    t =>
                        AggroItems.Count(
                            a =>
                                a.Value.Target.NetworkId == t.NetworkId &&
                                AggroFadeTime + a.Value.Timestamp >= Game.Time)),
                new WeightedItem(
                    "hard-cc", Global.Lang.Get("TS_HardCC"), 5, false, 333, 333, delegate(Obj_AI_Hero t)
                    {
                        var buffs =
                            t.Buffs.Where(
                                x =>
                                    x.Type == BuffType.Charm || x.Type == BuffType.Knockback ||
                                    x.Type == BuffType.Suppression || x.Type == BuffType.Fear ||
                                    x.Type == BuffType.Taunt || x.Type == BuffType.Stun).ToList();
                        return buffs.Any() ? buffs.Max(x => x.EndTime) : 0f;
                    }),
                new WeightedItem(
                    "soft-cc", Global.Lang.Get("TS_SoftCC"), 5, false, 333, 333, delegate(Obj_AI_Hero t)
                    {
                        var buffs =
                            t.Buffs.Where(
                                x =>
                                    x.Type == BuffType.Slow || x.Type == BuffType.Silence || x.Type == BuffType.Snare ||
                                    x.Type == BuffType.Polymorph).ToList();
                        return buffs.Any() ? buffs.Max(x => x.EndTime) : 0f;
                    }),
                new WeightedItem(
                    "gold", Global.Lang.Get("TS_Gold"), 7, false, 333, 8000,
                    t =>
                        (t.MinionsKilled + t.NeutralMinionsKilled) * MinionGold + t.ChampionsKilled * KillGold +
                        t.Assists * AssistGold)
            };

            #endregion

            Game.OnWndProc += OnGameWndProc;
            Drawing.OnDraw += OnDrawingDraw;
            Obj_AI_Base.OnAggro += OnObjAiBaseAggro;
        }

        public static Obj_AI_Hero SelectedTarget
        {
            get
            {
                return (_menu != null && _menu.Item(_menu.Name + ".focus-selected").GetValue<bool>()
                    ? _selectedTarget
                    : null);
            }
        }

        public static void AddWeightedItem(WeightedItem item)
        {
            try
            {
                WeightedItems.Add(item);

                if (_weightsMenu != null)
                {
                    _weightsMenu.AddItem(
                        new MenuItem(_weightsMenu.Name + "." + item.Name, item.DisplayName).SetValue(
                            new Slider(item.Weight, MinWeight, MaxWeight)));
                    _weightsMenu.Item(_weightsMenu.Name + "." + item.Name).ValueChanged +=
                        delegate(object sender, OnValueChangeEventArgs args)
                        {
                            item.Weight = args.GetNewValue<Slider>().Value;
                            _averageWeight = (float) WeightedItems.Average(w => w.Weight);
                        };
                    item.Weight = _menu.Item(_weightsMenu.Name + "." + item.Name).GetValue<Slider>().Value;
                }

                _averageWeight = (float) WeightedItems.Average(w => w.Weight);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static void OverwriteWeightFunction(string name, Func<Obj_AI_Hero, float> func)
        {
            try
            {
                var item = WeightedItems.FirstOrDefault(w => w.Name.Equals(name));
                if (item != null)
                {
                    item.GetValueFunc = func;
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private static void OnObjAiBaseAggro(Obj_AI_Base sender, GameObjectAggroEventArgs args)
        {
            try
            {
                if (!sender.IsEnemy || sender.Type != GameObjectType.obj_AI_Hero)
                {
                    return;
                }
                var hero = sender as Obj_AI_Hero;
                var target = GameObjects.EnemyHeroes.FirstOrDefault(h => h.NetworkId == args.NetworkId);
                if (hero != null && target != null)
                {
                    AggroItem aggro;
                    if (AggroItems.TryGetValue(hero.NetworkId, out aggro))
                    {
                        aggro.Target = target;
                    }
                    else
                    {
                        AggroItems[target.NetworkId] = new AggroItem(target);
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
                if (_menu == null || ObjectManager.Player.IsDead)
                {
                    return;
                }
                var weightsSimple = _menu.Item(_menu.Name + ".drawing.weights-simple").GetValue<bool>();
                var weightsAdvanced = _menu.Item(_menu.Name + ".drawing.weights-advanced").GetValue<bool>();
                var weightMultiplicator =
                    _menu.Item(_menu.Name + ".weights.heroes.weight-multiplicator").GetValue<Slider>().Value;
                var lastTarget = _menu.Item(_menu.Name + ".drawing.last-target").GetValue<Circle>();
                var assassin = _menu.Item(_menu.Name + ".assassin-mode.enabled").GetValue<bool>();
                var assassinColor = _menu.Item(_menu.Name + ".drawing.assassin-color").GetValue<Circle>();
                var assassinRange = _menu.Item(_menu.Name + ".assassin-mode.range").GetValue<Slider>().Value;
                var circleThickness = _menu.Item(_menu.Name + ".drawing.circle-thickness").GetValue<Slider>().Value;
                var focusSelected = _menu.Item(_menu.Name + ".focus-selected").GetValue<bool>();
                var selected = _menu.Item(_menu.Name + ".drawing.selected-color").GetValue<Circle>();

                if (_selectedTarget != null && _selectedTarget.IsValidTarget() && focusSelected && selected.Active)
                {
                    Render.Circle.DrawCircle(
                        _selectedTarget.Position, _selectedTarget.BoundingRadius + SelectClickBuffer, selected.Color,
                        circleThickness);
                }

                if (assassin && assassinColor.Active)
                {
                    foreach (var target in
                        GameObjects.EnemyHeroes.Where(
                            h =>
                                _menu.Item(_menu.Name + ".assassin-mode.heroes." + h.ChampionName).GetValue<bool>() &&
                                h.IsValidTarget(assassinRange) && h.Position.IsOnScreen()))
                    {
                        Render.Circle.DrawCircle(
                            target.Position, target.BoundingRadius + SelectClickBuffer, assassinColor.Color,
                            circleThickness);
                    }
                    Render.Circle.DrawCircle(
                        ObjectManager.Player.Position, assassinRange, assassinColor.Color, circleThickness);
                }
                if (lastTarget.Active)
                {
                    if (_lastTarget != null && !_lastTarget.IsDead && _lastTarget.IsVisible &&
                        _lastTarget.Position.IsOnScreen())
                    {
                        Render.Circle.DrawCircle(
                            _lastTarget.Position, _lastTarget.BoundingRadius + SelectClickBuffer, lastTarget.Color,
                            circleThickness);
                    }
                }
                if ((weightsSimple || weightsAdvanced) && _tsMode == TargetSelectorModeType.Weights)
                {
                    var enemies =
                        GameObjects.EnemyHeroes.Where(h => h.IsValidTarget(_debugRange) && h.Position.IsOnScreen());
                    foreach (var target in enemies)
                    {
                        var position = Drawing.WorldToScreen(target.Position);
                        var totalWeight = 0f;
                        var offset = 0f;
                        foreach (var weight in WeightedItems.Where(w => w.Weight > 0))
                        {
                            var lastWeight = weight.LastWeight(target);
                            if (lastWeight > 0)
                            {
                                if (_menu != null)
                                {
                                    var heroMultiplicator =
                                        _menu.Item(_menu.Name + ".weights.heroes." + target.ChampionName)
                                            .GetValue<Slider>()
                                            .Value;
                                    if (heroMultiplicator > 1)
                                    {
                                        lastWeight += _averageWeight * heroMultiplicator;
                                    }
                                    if (weightMultiplicator > 1)
                                    {
                                        lastWeight *= weightMultiplicator;
                                    }
                                }
                                if (weightsAdvanced)
                                {
                                    Drawing.DrawText(
                                        position.X + target.BoundingRadius, position.Y - 100 + offset, Color.White,
                                        lastWeight.ToString("0.0").Replace(",", ".") + " - " + weight.DisplayName);
                                    offset += 17f;
                                }
                                totalWeight += lastWeight;
                            }
                        }
                        if (weightsSimple)
                        {
                            Drawing.DrawText(
                                target.HPBarPosition.X + 55f, target.HPBarPosition.Y - 20f, Color.White,
                                totalWeight.ToString("0.0").Replace(",", "."));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private static void OnGameWndProc(WndEventArgs args)
        {
            try
            {
                if (args.Msg != (ulong) WindowsMessages.WM_LBUTTONDOWN)
                {
                    return;
                }

                _selectedTarget =
                    GameObjects.EnemyHeroes.Where(
                        h => h.IsValidTarget() && h.Distance(Game.CursorPos) < h.BoundingRadius + SelectClickBuffer)
                        .OrderBy(h => h.Distance(Game.CursorPos))
                        .FirstOrDefault();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static void SetTarget(Obj_AI_Hero hero)
        {
            if (hero.IsValidTarget())
            {
                _selectedTarget = hero;
            }
        }

        public static Obj_AI_Hero GetTarget()
        {
            return _selectedTarget;
        }

        public static Obj_AI_Hero GetTarget(Spell spell,
            bool ignoreShields = true,
            Vector3 from = new Vector3(),
            IEnumerable<Obj_AI_Hero> ignoredChampions = null)
        {
            return GetTarget(spell.Range, spell.DamageType, ignoreShields, from, ignoredChampions);
        }

        private static bool IsValidTarget(Obj_AI_Hero target,
            float range,
            LeagueSharp.Common.TargetSelector.DamageType damageType,
            bool ignoreShields = true,
            Vector3 from = default(Vector3))
        {
            return target.IsValidTarget() &&
                   target.Distance((from.Equals(default(Vector3)) ? ObjectManager.Player.ServerPosition : from), true) <
                   Math.Pow((range <= 0 ? Orbwalking.GetRealAutoAttackRange(target) : range), 2) &&
                   !Invulnerable.HasBuff(target, damageType, ignoreShields);
        }

        public static Obj_AI_Hero GetTarget(float range,
            LeagueSharp.Common.TargetSelector.DamageType damageType = LeagueSharp.Common.TargetSelector.DamageType.True,
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

        public static List<Obj_AI_Hero> GetTargets(float range,
            LeagueSharp.Common.TargetSelector.DamageType damageType = LeagueSharp.Common.TargetSelector.DamageType.True,
            bool ignoreShields = true,
            Vector3 from = default(Vector3),
            IEnumerable<Obj_AI_Hero> ignoredChampions = null)
        {
            try
            {
                var assassin = _menu != null && _menu.Item(_menu.Name + ".assassin-mode.enabled").GetValue<bool>();
                var aRange = assassin ? _menu.Item(_menu.Name + ".assassin-mode.range").GetValue<Slider>().Value : range;
                _debugRange = aRange > _debugRange ? aRange : _debugRange;

                if (_menu != null && SelectedTarget != null &&
                    IsValidTarget(
                        SelectedTarget,
                        _menu.Item(_menu.Name + ".force-focus-selected").GetValue<bool>() ? float.MaxValue : aRange,
                        LeagueSharp.Common.TargetSelector.DamageType.True, ignoreShields, from))
                {
                    return new List<Obj_AI_Hero> { SelectedTarget };
                }

                var targets =
                    GameObjects.EnemyHeroes.Where(
                        h => ignoredChampions == null || ignoredChampions.All(i => i.NetworkId != h.NetworkId))
                        .Where(h => IsValidTarget(h, aRange, damageType, ignoreShields, from))
                        .ToList();

                if (targets.Count > 0)
                {
                    if (assassin)
                    {
                        var assassinTargets =
                            targets.Where(h => _menu.Item(_menu.Name + ".assassin-mode").GetValue<bool>()).ToList();
                        if (assassinTargets.Any())
                        {
                            targets = assassinTargets;
                        }
                    }
                    var t = GetChampionsByMode(targets).ToList();
                    if (t.Any())
                    {
                        _lastTarget = t[0];
                    }
                    return t;
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return null;
        }

        public static void SetMode(TargetSelectorModeType mode)
        {
            _tsMode = mode;
        }

        private static IEnumerable<Obj_AI_Hero> GetChampionsByMode(List<Obj_AI_Hero> heroes)
        {
            try
            {
                switch (_tsMode)
                {
                    case TargetSelectorModeType.Weights:
                        return TargetWeights(heroes);

                    case TargetSelectorModeType.LessAttacksToKill:
                        return heroes.OrderBy(x => x.Health / ObjectManager.Player.TotalAttackDamage);

                    case TargetSelectorModeType.MostAbilityPower:
                        return heroes.OrderByDescending(x => x.TotalMagicalDamage);

                    case TargetSelectorModeType.MostAttackDamage:
                        return heroes.OrderByDescending(x => x.TotalAttackDamage);

                    case TargetSelectorModeType.Closest:
                        return heroes.OrderBy(x => x.Distance(ObjectManager.Player));

                    case TargetSelectorModeType.NearMouse:
                        return heroes.OrderBy(x => x.Distance(Game.CursorPos));

                    case TargetSelectorModeType.LessCastPriority:
                        return heroes.OrderBy(x => x.Health / ObjectManager.Player.TotalMagicalDamage);

                    case TargetSelectorModeType.Priorities:
                        return heroes.OrderByDescending(x => GetPriorityByName(x.ChampionName));

                    case TargetSelectorModeType.LeastHealth:
                        return heroes.OrderBy(x => x.Health);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return new List<Obj_AI_Hero>();
        }

        private static IEnumerable<Obj_AI_Hero> TargetWeights(List<Obj_AI_Hero> targets)
        {
            try
            {
                foreach (var weight in WeightedItems.Where(w => w.Weight > 0))
                {
                    weight.UpdateMaxValue(targets);
                }

                var targetsList = new List<Target>();
                var multiplicator =
                    _menu.Item(_menu.Name + ".weights.heroes.weight-multiplicator").GetValue<Slider>().Value;
                foreach (var target in targets)
                {
                    var tmpWeight = WeightedItems.Where(w => w.Weight > 0).Sum(w => w.CalculatedWeight(target));

                    if (_menu != null)
                    {
                        var heroMultiplicator =
                            _menu.Item(_menu.Name + ".weights.heroes." + target.ChampionName).GetValue<Slider>().Value;
                        if (heroMultiplicator > 1)
                        {
                            tmpWeight += _averageWeight * heroMultiplicator;
                        }
                        if (multiplicator > 1)
                        {
                            tmpWeight *= multiplicator;
                        }
                    }

                    targetsList.Add(new Target(target, tmpWeight));
                }
                return targetsList.Count > 0 ? targetsList.OrderByDescending(t => t.Weight).Select(t => t.Hero) : null;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return null;
        }

        private static int GetPriorityByName(string name)
        {
            try
            {
                if (_menu != null)
                {
                    var item = _menu.Item(_menu.Name + ".priorities." + name);
                    if (item != null)
                    {
                        return item.GetValue<Slider>().Value;
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return 1;
        }

        private static TargetSelectorPriorityType GetDefaultPriorityByName(string name)
        {
            try
            {
                var priority = Priorities.FirstOrDefault(m => m.Champions.Contains(name));
                if (priority != null)
                {
                    return priority.Type;
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return TargetSelectorPriorityType.Low;
        }

        public static void AddToMenu(Menu menu)
        {
            try
            {
                _menu = menu;

                var drawingMenu = _menu.AddSubMenu(new Menu(Global.Lang.Get("G_Drawing"), menu.Name + ".drawing"));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + ".selected-color", Global.Lang.Get("TS_SelectedTarget")).SetValue(
                        new Circle(true, Color.Red)));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + ".assassin-color", Global.Lang.Get("TS_AssassinTarget")).SetValue(
                        new Circle(true, Color.GreenYellow)));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + ".last-target", Global.Lang.Get("TS_LastTarget")).SetValue(
                        new Circle(false, Color.Orange)));
                drawingMenu.AddItem(
                    new MenuItem(
                        drawingMenu.Name + ".weights-simple",
                        Global.Lang.Get("TS_Weights") + " " + Global.Lang.Get("G_Simple")).SetValue(false));
                drawingMenu.AddItem(
                    new MenuItem(
                        drawingMenu.Name + ".weights-advanced",
                        Global.Lang.Get("TS_Weights") + " " + Global.Lang.Get("G_Advanced")).SetValue(false));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + ".circle-thickness", Global.Lang.Get("G_CircleThickness")).SetValue(
                        new Slider(2, 1, 10)));

                var assassinManager =
                    _menu.AddSubMenu(new Menu(Global.Lang.Get("TS_AssassinMode"), menu.Name + ".assassin-mode"));
                var enemyListMenu =
                    assassinManager.AddSubMenu(new Menu(Global.Lang.Get("G_Heroes"), assassinManager.Name + ".heroes"));
                foreach (var enemy in GameObjects.EnemyHeroes)
                {
                    enemyListMenu.AddItem(
                        new MenuItem(enemyListMenu.Name + "." + enemy.ChampionName, enemy.ChampionName).SetValue(false));
                }
                assassinManager.AddItem(
                    new MenuItem(assassinManager.Name + ".range", Global.Lang.Get("G_Range")).SetValue(
                        new Slider(1000, 500, 2000)));
                assassinManager.AddItem(
                    new MenuItem(assassinManager.Name + ".enabled", Global.Lang.Get("G_Enabled")).SetValue(false));

                _weightsMenu = _menu.AddSubMenu(new Menu(Global.Lang.Get("TS_Weights"), menu.Name + ".weights"));

                var heroesMenu =
                    _weightsMenu.AddSubMenu(new Menu(Global.Lang.Get("G_Heroes"), _weightsMenu.Name + ".heroes"));

                heroesMenu.AddItem(
                    new MenuItem(heroesMenu.Name + ".weight-multiplicator", Global.Lang.Get("TS_WeightMultiplicator"))
                        .SetValue(new Slider(1, MinMultiplicator, MaxMultiplicator)));

                foreach (var enemy in GameObjects.EnemyHeroes)
                {
                    heroesMenu.AddItem(
                        new MenuItem(heroesMenu.Name + "." + enemy.ChampionName, enemy.ChampionName).SetValue(
                            new Slider(1, MinMultiplicator, MaxMultiplicator)));
                }

                foreach (var item in WeightedItems)
                {
                    var localItem = item;
                    _weightsMenu.AddItem(
                        new MenuItem(_weightsMenu.Name + "." + item.Name, item.DisplayName).SetValue(
                            new Slider(localItem.Weight, MinWeight, MaxWeight)));
                    _weightsMenu.Item(_weightsMenu.Name + "." + item.Name).ValueChanged +=
                        delegate(object sender, OnValueChangeEventArgs args)
                        {
                            localItem.Weight = args.GetNewValue<Slider>().Value;
                            _averageWeight = (float) WeightedItems.Average(w => w.Weight);
                        };
                    item.Weight = _menu.Item(_weightsMenu.Name + "." + item.Name).GetValue<Slider>().Value;
                }


                var prioritiesMenu =
                    _menu.AddSubMenu(new Menu(Global.Lang.Get("TS_Priorities"), menu.Name + ".priorities"));
                prioritiesMenu.AddItem(
                    new MenuItem(prioritiesMenu.Name + ".auto", Global.Lang.Get("TS_AutoPriority")).SetValue(false))
                    .ValueChanged += delegate(object sender, OnValueChangeEventArgs args)
                    {
                        if (args.GetNewValue<bool>())
                        {
                            foreach (var enemy in GameObjects.EnemyHeroes)
                            {
                                _menu.Item(prioritiesMenu.Name + "." + enemy.ChampionName)
                                    .SetValue(
                                        new Slider(Convert.ToInt32(GetDefaultPriorityByName(enemy.ChampionName)), 1, 5));
                            }
                        }
                    };
                foreach (var enemy in GameObjects.EnemyHeroes)
                {
                    var item =
                        new MenuItem(prioritiesMenu.Name + "." + enemy.ChampionName, enemy.ChampionName).SetValue(
                            new Slider(1, 1, 5));
                    prioritiesMenu.AddItem(item);
                    if (_menu.Item(menu.Name + ".priorities.auto").GetValue<bool>())
                    {
                        item.SetValue(new Slider(Convert.ToInt32(GetDefaultPriorityByName(enemy.ChampionName)), 1, 5));
                    }
                }

                _menu.AddItem(
                    new MenuItem(menu.Name + ".focus-selected", Global.Lang.Get("TS_FocusSelectedTarget")).SetValue(
                        true));
                _menu.AddItem(
                    new MenuItem(menu.Name + ".force-focus-selected", Global.Lang.Get("TS_OnlyAttackSelectedTarget"))
                        .SetValue(false));

                _menu.AddItem(
                    new MenuItem(menu.Name + ".mode", Global.Lang.Get("TS_Mode")).SetValue(
                        new StringList(
                            new[]
                            {
                                Global.Lang.Get("TS_Weights"), Global.Lang.Get("TS_Priorities"),
                                Global.Lang.Get("TS_LessAttacksToKill"), Global.Lang.Get("TS_MostAbilityPower"),
                                Global.Lang.Get("TS_MostAttackDamage"), Global.Lang.Get("TS_Closest"),
                                Global.Lang.Get("TS_NearMouse"), Global.Lang.Get("TS_LessCastPriority"),
                                Global.Lang.Get("TS_LeastHealth")
                            }))).ValueChanged +=
                    delegate(object sender, OnValueChangeEventArgs args)
                    {
                        _tsMode = GetPriorityByMenuValue(args.GetNewValue<StringList>().SelectedValue);
                    };

                _tsMode = GetPriorityByMenuValue(_menu.Item(menu.Name + ".mode").GetValue<StringList>().SelectedValue);
                _averageWeight = (float) WeightedItems.Average(w => w.Weight);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private static TargetSelectorModeType GetPriorityByMenuValue(string value)
        {
            var tsMode = TargetSelectorModeType.Priorities;
            try
            {
                if (value.Equals(Global.Lang.Get("TS_Weights")))
                {
                    tsMode = TargetSelectorModeType.Weights;
                }
                else if (value.Equals(Global.Lang.Get("TS_Priorities")))
                {
                    tsMode = TargetSelectorModeType.Priorities;
                }
                else if (value.Equals(Global.Lang.Get("TS_LessAttacksToKill")))
                {
                    tsMode = TargetSelectorModeType.LessAttacksToKill;
                }
                else if (value.Equals(Global.Lang.Get("TS_MostAbilityPower")))
                {
                    tsMode = TargetSelectorModeType.MostAbilityPower;
                }
                else if (value.Equals(Global.Lang.Get("TS_MostAttackDamage")))
                {
                    tsMode = TargetSelectorModeType.MostAttackDamage;
                }
                else if (value.Equals(Global.Lang.Get("TS_Closest")))
                {
                    tsMode = TargetSelectorModeType.Closest;
                }
                else if (value.Equals(Global.Lang.Get("TS_NearMouse")))
                {
                    tsMode = TargetSelectorModeType.NearMouse;
                }
                else if (value.Equals(Global.Lang.Get("TS_LessCastPriority")))
                {
                    tsMode = TargetSelectorModeType.LessCastPriority;
                }
                else if (value.Equals(Global.Lang.Get("TS_LeastHealth")))
                {
                    tsMode = TargetSelectorModeType.LeastHealth;
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return tsMode;
        }
    }

    public class Target
    {
        public Target(Obj_AI_Hero hero, float weight)
        {
            Hero = hero;
            Weight = weight;
        }

        public Obj_AI_Hero Hero { get; private set; }
        public float Weight { get; private set; }
    }

    internal class AggroItem
    {
        public AggroItem(Obj_AI_Hero target)
        {
            Target = target;
            Timestamp = Game.Time;
        }

        public Obj_AI_Hero Target { get; set; }
        public float Timestamp { get; private set; }
    }

    internal class Cache
    {
        private float _value;

        public Cache(float value)
        {
            Value = value;
            Time = Environment.TickCount;
        }

        public float Value
        {
            get { return _value; }
            set
            {
                _value = value;
                Time = Environment.TickCount;
            }
        }

        public int Time { get; private set; }
    }

    internal class WeightedItem
    {
        private readonly Dictionary<int, Cache> _valueCache = new Dictionary<int, Cache>();
        private readonly Dictionary<int, Cache> _weightCache = new Dictionary<int, Cache>();

        public WeightedItem(string name,
            string displayName,
            int weight,
            bool inverted,
            int cacheWeightTime,
            int cacheValueTime,
            Func<Obj_AI_Hero, float> getValue)
        {
            GetValueFunc = getValue;
            Name = name;
            DisplayName = displayName;
            Weight = weight;
            Inverted = inverted;
            CacheWeightTime = cacheWeightTime;
            CacheValueTime = cacheValueTime;
        }

        public Func<Obj_AI_Hero, float> GetValueFunc { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public int Weight { get; set; }
        public bool Inverted { get; set; }
        public int CacheWeightTime { get; set; }
        public int CacheValueTime { get; set; }
        public float LastMax { get; set; }
        public float LastMin { get; set; }

        public float LastWeight(Obj_AI_Hero target)
        {
            try
            {
                Cache cache;
                if (_weightCache.TryGetValue(target.NetworkId, out cache))
                {
                    return cache.Value;
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return 0f;
        }

        public float LastValue(Obj_AI_Hero target)
        {
            try
            {
                Cache cache;
                if (_valueCache.TryGetValue(target.NetworkId, out cache))
                {
                    return cache.Value;
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return 0f;
        }

        public float CalculatedWeight(Obj_AI_Hero target)
        {
            try
            {
                if (Weight == 0)
                {
                    return 0;
                }

                Cache cache;
                if (_weightCache.TryGetValue(target.NetworkId, out cache) &&
                    cache.Time + CacheWeightTime > Environment.TickCount)
                {
                    return cache.Value;
                }

                var weight = Inverted
                    ? CalculatedWeight(LastMin, GetValue(target), 0, Weight, TargetSelector.MinWeight)
                    : CalculatedWeight(GetValue(target), 0, LastMax, TargetSelector.MinWeight, Weight);

                if (cache == null)
                {
                    _weightCache[target.NetworkId] = new Cache(weight);
                }
                else
                {
                    _weightCache[target.NetworkId].Value = weight;
                }
                return weight;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return 0;
        }

        private float CalculatedWeight(float currentValue,
            float currentMin,
            float currentMax,
            float newMin,
            float newMax)
        {
            try
            {
                var weight = (currentValue - currentMin) * (newMax - newMin) / (currentMax - currentMin) + newMin;
                return !float.IsNaN(weight) && !float.IsInfinity(weight) ? weight : 0;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return 0;
        }

        public float GetValue(Obj_AI_Hero target)
        {
            try
            {
                Cache cache;
                if (_valueCache.TryGetValue(target.NetworkId, out cache) &&
                    cache.Time + CacheValueTime - 15 > Environment.TickCount)
                {
                    return cache.Value;
                }
                var value = GetValueFunc(target);
                if (cache == null)
                {
                    _valueCache[target.NetworkId] = new Cache(value);
                }
                else
                {
                    _valueCache[target.NetworkId].Value = value;
                }
                return value;
            }
            catch
            {
                return Inverted ? float.MaxValue : float.MinValue;
            }
        }

        public void UpdateMaxValue(List<Obj_AI_Hero> targets)
        {
            try
            {
                var min = float.MaxValue;
                var max = float.MinValue;
                foreach (var target in targets)
                {
                    var value = GetValue(target);
                    if (value < min)
                    {
                        min = value;
                    }
                    if (value > max)
                    {
                        max = value;
                    }
                }
                LastMin = min;
                LastMax = max;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }

    internal class Priority
    {
        public TargetSelectorPriorityType Type { get; set; }
        public string[] Champions { get; set; }
    }
}