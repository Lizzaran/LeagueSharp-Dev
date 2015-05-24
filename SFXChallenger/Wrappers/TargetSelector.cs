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

namespace SFXChallenger.Wrappers
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using LeagueSharp;
    using LeagueSharp.Common;
    using LeagueSharp.Common.Data;
    using SFXLibrary.Extensions.LeagueSharp;
    using SFXLibrary.Logger;
    using SharpDX;
    using Color = System.Drawing.Color;

    #endregion

    internal class TargetSelector
    {
        internal const int MinWeight = 0;
        internal const int MaxWeight = 20;
        private const int AggroFadeTime = 10;
        private const int MinMultiplicator = 0;
        private const int MaxMultiplicator = 10;
        private const float SelectClickBuffer = 100f;
        private const float MinionGold = 22.34f;
        private const float KillGold = 300.00f;
        private const float AssistGold = 95.00f;
        private static float _averageWeight;
        private static Menu _menu;
        private static Obj_AI_Hero _selectedTarget;
        private static readonly List<WeightedItem> WeightedItems;
        private static readonly Dictionary<Obj_AI_Hero, TargetItem> AggroItems = new Dictionary<Obj_AI_Hero, TargetItem>();

        static TargetSelector()
        {
            WeightedItems = new List<WeightedItem>
            {
                new WeightedItem("killable", Global.Lang.Get("TS_AAKillable"), 20, false, delegate(Obj_AI_Hero t)
                {
                    var time = (int) (ObjectManager.Player.AttackCastDelay*1000) - 100 + Game.Ping/2 +
                               1000*(int) ObjectManager.Player.Distance(t)/(int) Orbwalking.GetMyProjectileSpeed();
                    return HealthPrediction.GetHealthPrediction(t, time, 0) + t.HPRegenRate*time <= ObjectManager.Player.GetAutoAttackDamage(t, true)
                        ? 1
                        : 0;
                }),
                new WeightedItem("attack-damage", Global.Lang.Get("TS_AttackDamage"), 10, false, delegate(Obj_AI_Hero t)
                {
                    var ad = (t.BaseAttackDamage + t.FlatPhysicalDamageMod);
                    ad += ad/100*(t.Crit*100)*(t.HasItem(ItemData.Infinity_Edge.Id) ? 2.5f : 2f);
                    var averageArmor = HeroManager.Allies.Average(a => a.Armor)*ObjectManager.Player.PercentArmorPenetrationMod -
                                       t.FlatArmorPenetrationMod;
                    return (ad*(100/(100 + (averageArmor > 0 ? averageArmor : 0))))*t.AttackSpeedMod;
                }),
                new WeightedItem("ability-power", Global.Lang.Get("TS_AbilityPower"), 10, false, delegate(Obj_AI_Hero t)
                {
                    var averageMr = HeroManager.Allies.Average(a => a.SpellBlock)*ObjectManager.Player.PercentMagicPenetrationMod -
                                    t.FlatMagicPenetrationMod;
                    return (t.BaseAbilityDamage + t.FlatMagicDamageMod)*(100/(100 + (averageMr > 0 ? averageMr : 0)));
                }),
                new WeightedItem("low-resists", Global.Lang.Get("TS_LowResists"), 6, true,
                    t => ObjectManager.Player.FlatPhysicalDamageMod >= ObjectManager.Player.FlatMagicDamageMod ? t.Armor : t.SpellBlock),
                new WeightedItem("low-health", Global.Lang.Get("TS_LowHealth"), 8, true, t => t.Health),
                new WeightedItem("short-distance", Global.Lang.Get("TS_ShortDistance"), 7, true, t => t.Distance(ObjectManager.Player)),
                new WeightedItem("team-focus", Global.Lang.Get("TS_TeamFocus"), 5, false,
                    t =>
                        AggroItems.Where(a => a.Key.IsAlly && a.Value.Target.NetworkId == t.NetworkId)
                            .Count(aggro => (Game.Time - aggro.Value.Timestamp) <= AggroFadeTime)),
                new WeightedItem("gold", Global.Lang.Get("TS_Gold"), 7, false,
                    t => t.MinionsKilled*MinionGold + t.ChampionsKilled*KillGold + t.Assists*AssistGold)
            };

            Game.OnWndProc += OnGameWndProc;
            Drawing.OnDraw += OnDrawingDraw;
            Obj_AI_Base.OnAggro += OnObjAiBaseAggro;
        }

        public static Obj_AI_Hero SelectedTarget
        {
            get { return (_menu != null && _menu.Item(_menu.Name + ".focus-selected").GetValue<bool>() ? _selectedTarget : null); }
        }

        ~TargetSelector()
        {
            Game.OnWndProc -= OnGameWndProc;
            Drawing.OnDraw -= OnDrawingDraw;
            Obj_AI_Base.OnAggro -= OnObjAiBaseAggro;
        }

        private static void OnObjAiBaseAggro(Obj_AI_Base sender, GameObjectAggroEventArgs args)
        {
            try
            {
                var hero = sender as Obj_AI_Hero;
                if (hero != null)
                {
                    var target = HeroManager.Enemies.FirstOrDefault(h => h.NetworkId.Equals(args.NetworkId));
                    if (target != null)
                    {
                        TargetItem aggro;
                        if (AggroItems.TryGetValue(hero, out aggro))
                        {
                            aggro.Target = target;
                        }
                        else
                        {
                            AggroItems[target] = new TargetItem(target);
                        }
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
                    return;

                if (_selectedTarget != null && _selectedTarget.IsValidTarget() && _menu.Item(_menu.Name + ".focus-selected").GetValue<bool>() &&
                    _menu.Item(_menu.Name + ".drawing.selected-color").GetValue<Circle>().Active)
                {
                    Render.Circle.DrawCircle(_selectedTarget.Position, _selectedTarget.BoundingRadius + SelectClickBuffer,
                        _menu.Item(_menu.Name + ".drawing.selected-color").GetValue<Circle>().Color,
                        _menu.Item(_menu.Name + ".drawing.circle-thickness").GetValue<Slider>().Value, true);
                }

                if (_menu.Item(_menu.Name + ".assassin-mode.enabled-" + ObjectManager.Player.ChampionName).GetValue<bool>() &&
                    _menu.Item(_menu.Name + ".drawing.assassin-color").GetValue<Circle>().Active)
                {
                    foreach (var target in
                        HeroManager.Enemies.Where(
                            h =>
                                _menu.Item(_menu.Name + ".assassin-mode.enemy-list." + h.ChampionName).GetValue<bool>() &&
                                h.Position.Distance(ObjectManager.Player.Position) <=
                                _menu.Item(_menu.Name + ".assassin-mode.range-" + ObjectManager.Player.ChampionName).GetValue<Slider>().Value))
                    {
                        Render.Circle.DrawCircle(target.Position, target.BoundingRadius + SelectClickBuffer,
                            _menu.Item(_menu.Name + ".drawing.assassin-color").GetValue<Circle>().Color,
                            _menu.Item(_menu.Name + ".drawing.circle-thickness").GetValue<Slider>().Value, true);
                    }
                }
                if (_menu.Item(_menu.Name + ".drawing.debug").GetValue<bool>())
                {
                    if (_menu.Item(_menu.Name + ".weights.enabled").GetValue<bool>())
                    {
                        foreach (var target in HeroManager.Enemies.Where(h => h.IsVisible && !h.IsDead && h.Position.IsOnScreen()))
                        {
                            var champWeight = (((_menu.Item(_menu.Name + ".heroes." + target.ChampionName).GetValue<Slider>().Value*
                                                 (_averageWeight - MinWeight))/5) + MinWeight) + 1;
                            var position = Drawing.WorldToScreen(target.Position);
                            var offset = 0;
                            var totalWeight = 0f;
                            foreach (var weight in WeightedItems)
                            {
                                weight.CurrentMin = HeroManager.Enemies.Select(weight.GetValue).DefaultIfEmpty().Min();
                                weight.CurrentMax = HeroManager.Enemies.Select(weight.GetValue).DefaultIfEmpty().Max();
                                var tmpWeight = weight.CalculatedWeight(target);
                                tmpWeight += champWeight*_menu.Item(_menu.Name + ".heroes.weight-multiplicator").GetValue<Slider>().Value;
                                totalWeight += tmpWeight;
                                Drawing.DrawText(position.X + target.BoundingRadius, position.Y - 100 + offset, Color.White,
                                    string.Format("{0} - {1}", tmpWeight.ToString("00.00"), weight.DisplayName));
                                offset += 17;
                            }
                            Drawing.DrawText(target.HPBarPosition.X + 51, target.HPBarPosition.Y - 20, Color.White, totalWeight.ToString("0.00"));
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
            if (args.Msg != (ulong) WindowsMessages.WM_LBUTTONDOWN)
                return;

            _selectedTarget =
                HeroManager.Enemies.Where(h => h.IsValidTarget() && h.Distance(Game.CursorPos) < h.BoundingRadius + SelectClickBuffer)
                    .OrderBy(h => h.Distance(Game.CursorPos))
                    .FirstOrDefault();
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

        public static Obj_AI_Hero GetTarget(Spell spell, bool ignoreShields = true, Vector3 from = new Vector3(),
            IEnumerable<Obj_AI_Hero> ignoredChampions = null)
        {
            return GetTarget(spell.Range, spell.DamageType, ignoreShields, from, ignoredChampions);
        }

        private static bool IsValidTarget(Obj_AI_Base target, float range, LeagueSharp.Common.TargetSelector.DamageType damageType,
            bool ignoreShields = true, Vector3 from = default(Vector3))
        {
            return target.IsValidTarget() &&
                   target.Distance((from.Equals(default(Vector3)) ? ObjectManager.Player.ServerPosition : from), true) <
                   Math.Pow((range <= 0 ? Orbwalking.GetRealAutoAttackRange(target) : range), 2) &&
                   !Invulnerable.HasBuff(target, damageType, ignoreShields);
        }

        public static Obj_AI_Hero GetTarget(float range,
            LeagueSharp.Common.TargetSelector.DamageType damageType = LeagueSharp.Common.TargetSelector.DamageType.True, bool ignoreShields = true,
            Vector3 from = default(Vector3), IEnumerable<Obj_AI_Hero> ignoredChampions = null)
        {
            try
            {
                var assassin = _menu != null &&
                               _menu.Item(_menu.Name + ".assassin-mode.enabled-" + ObjectManager.Player.ChampionName).GetValue<bool>();

                var aRange = assassin
                    ? _menu.Item(_menu.Name + ".assassin-mode.range-" + ObjectManager.Player.ChampionName).GetValue<Slider>().Value
                    : range;

                if (_menu != null && SelectedTarget != null &&
                    SelectedTarget.IsValidTarget(_menu.Item(_menu.Name + ".force-focus-selected").GetValue<bool>() ? float.MaxValue : aRange, true,
                        from))
                {
                    return SelectedTarget;
                }

                var targets =
                    HeroManager.Enemies.Where(h => ignoredChampions == null || ignoredChampions.All(i => i.NetworkId != h.NetworkId))
                        .Where(h => IsValidTarget(h, aRange, damageType, ignoreShields, from))
                        .ToList();

                if (assassin)
                {
                    var assassinTargets = targets.Where(h => _menu.Item(_menu.Name + ".assassin-mode." + h.ChampionName).GetValue<bool>()).ToList();
                    if (assassinTargets.Any())
                    {
                        targets = assassinTargets;
                    }
                }

                foreach (var item in WeightedItems.Where(w => w.Weight > 0))
                {
                    item.CurrentMin = targets.Select(item.GetValue).DefaultIfEmpty().Min();
                    item.CurrentMax = targets.Select(item.GetValue).DefaultIfEmpty().Max();
                }

                var target = TargetWeights(targets).FirstOrDefault();
                return target != null ? target.Hero : null;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return null;
        }

        public static List<Target> GetTargets(float range,
            LeagueSharp.Common.TargetSelector.DamageType damageType = LeagueSharp.Common.TargetSelector.DamageType.True, bool ignoreShields = true,
            Vector3 from = default(Vector3), IEnumerable<Obj_AI_Hero> ignoredChampions = null)
        {
            try
            {
                var assassin = _menu != null &&
                               _menu.Item(_menu.Name + ".assassin-mode.enabled-" + ObjectManager.Player.ChampionName).GetValue<bool>();

                var aRange = assassin
                    ? _menu.Item(_menu.Name + ".assassin-mode.range-" + ObjectManager.Player.ChampionName).GetValue<Slider>().Value
                    : range;

                var targets =
                    HeroManager.Enemies.Where(h => ignoredChampions == null || ignoredChampions.All(i => i.NetworkId != h.NetworkId))
                        .Where(h => IsValidTarget(h, aRange, damageType, ignoreShields, from))
                        .ToList();

                if (assassin)
                {
                    var assassinTargets = targets.Where(h => _menu.Item(_menu.Name + ".assassin-mode." + h.ChampionName).GetValue<bool>()).ToList();
                    if (assassinTargets.Any())
                    {
                        targets = assassinTargets;
                    }
                }

                foreach (var item in WeightedItems.Where(w => w.Weight > 0))
                {
                    item.CurrentMin = targets.Select(item.GetValue).DefaultIfEmpty().Min();
                    item.CurrentMax = targets.Select(item.GetValue).DefaultIfEmpty().Max();
                }

                var targetsWeight = TargetWeights(targets);

                if (_menu != null && SelectedTarget != null &&
                    SelectedTarget.IsValidTarget(_menu.Item(_menu.Name + ".force-focus-selected").GetValue<bool>() ? float.MaxValue : aRange, true,
                        from))
                {
                    var id = targetsWeight.FindIndex(x => x.Hero.NetworkId == SelectedTarget.NetworkId);
                    var item = targetsWeight[id];
                    targetsWeight.RemoveAt(id);
                    targetsWeight.Insert(0, item);
                }

                return targetsWeight;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return null;
        }

        private static List<Target> TargetWeights(List<Obj_AI_Hero> targets)
        {
            try
            {
                var targetsList = new List<Target>();

                foreach (var target in targets)
                {
                    var tmpWeight = _menu != null && _menu.Item(_menu.Name + ".weights.enabled").GetValue<bool>()
                        ? WeightedItems.Where(w => w.Weight > 0).Sum(weight => weight.CalculatedWeight(target))
                        : 0;
                    if (_menu != null)
                    {
                        var champWeight = (((_menu.Item(_menu.Name + ".heroes." + target.ChampionName).GetValue<Slider>().Value*
                                             (_averageWeight - MinWeight))/5) + MinWeight) + 1;
                        tmpWeight += champWeight*_menu.Item(_menu.Name + ".heroes.weight-multiplicator").GetValue<Slider>().Value;
                    }

                    targetsList.Add(new Target(target, tmpWeight));
                }
                return targetsList.OrderByDescending(t => t.Weight).ToList();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return null;
        }

        public static void AddToMenu(Menu menu)
        {
            try
            {
                _menu = menu;

                var drawingMenu = _menu.AddSubMenu(new Menu(Global.Lang.Get("G_Drawing"), menu.Name + ".drawing"));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + ".selected-color", Global.Lang.Get("TS_SelectedTargetColor")).SetValue(new Circle(true, Color.Red)));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + ".assassin-color", Global.Lang.Get("TS_AssassinTargetColor")).SetValue(new Circle(true,
                        Color.GreenYellow)));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + ".circle-thickness", Global.Lang.Get("G_CircleThickness")).SetValue(new Slider(2, 1, 10)));
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + ".debug", Global.Lang.Get("G_Debug")).SetValue(false));

                var assassinManager = _menu.AddSubMenu(new Menu(Global.Lang.Get("TS_AssassinMode"), menu.Name + ".assassin-mode"));
                var enemyListMenu = assassinManager.AddSubMenu(new Menu(Global.Lang.Get("TS_EnemyList"), assassinManager.Name + ".enemy-list"));
                foreach (var enemy in HeroManager.Enemies)
                {
                    enemyListMenu.AddItem(new MenuItem(enemyListMenu.Name + "." + enemy.ChampionName, enemy.ChampionName).SetValue(false));
                }
                assassinManager.AddItem(
                    new MenuItem(assassinManager.Name + ".range-" + ObjectManager.Player.ChampionName, Global.Lang.Get("G_Range")).SetValue(
                        new Slider(1000, 500, 2000)));
                assassinManager.AddItem(
                    new MenuItem(assassinManager.Name + ".enabled-" + ObjectManager.Player.ChampionName, Global.Lang.Get("G_Enabled")).SetValue(false));

                var weightsMenu = _menu.AddSubMenu(new Menu(Global.Lang.Get("TS_Weights"), menu.Name + ".weights"));

                foreach (var item in WeightedItems)
                {
                    var localItem = item;
                    weightsMenu.AddItem(
                        new MenuItem(weightsMenu.Name + "." + item.Name, item.DisplayName).SetValue(new Slider(localItem.Weight, MinWeight, MaxWeight)));
                    weightsMenu.Item(weightsMenu.Name + "." + item.Name).ValueChanged += delegate(object sender, OnValueChangeEventArgs args)
                    {
                        localItem.Weight = args.GetNewValue<Slider>().Value;
                        _averageWeight = (float) WeightedItems.Average(w => w.Weight);
                    };
                }

                weightsMenu.AddItem(new MenuItem(weightsMenu.Name + ".enabled", Global.Lang.Get("G_Enabled")).SetValue(true));

                var heroesMenu = _menu.AddSubMenu(new Menu(Global.Lang.Get("G_Heroes"), menu.Name + ".heroes"));

                heroesMenu.AddItem(
                    new MenuItem(heroesMenu.Name + ".weight-multiplicator", Global.Lang.Get("TS_WeightMultiplicator")).SetValue(new Slider(1,
                        MinMultiplicator, MaxMultiplicator)));

                foreach (var enemy in HeroManager.Enemies)
                {
                    heroesMenu.AddItem(
                        new MenuItem(heroesMenu.Name + "." + enemy.ChampionName, Global.Lang.Get("TS_Weights") + ": " + enemy.ChampionName).SetValue(
                            new Slider(1, 1, 5)));
                }

                _menu.AddItem(new MenuItem(menu.Name + ".focus-selected", Global.Lang.Get("TS_FocusSelectedTarget")).SetValue(true));
                _menu.AddItem(new MenuItem(menu.Name + ".force-focus-selected", Global.Lang.Get("TS_OnlyAttackSelectedTarget")).SetValue(false));
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
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

    internal class TargetItem
    {
        private Obj_AI_Hero _target;

        public TargetItem(Obj_AI_Hero target, float damage = 0)
        {
            Target = target;
            Damage = damage;
        }

        public Obj_AI_Hero Target
        {
            get { return _target; }
            set
            {
                _target = value;
                Timestamp = Game.Time;
            }
        }

        public float Damage { get; set; }
        public float Timestamp { get; private set; }
    }

    internal class WeightedItem
    {
        private readonly Func<Obj_AI_Hero, float> _getValue;
        private float _currentMax;

        public WeightedItem(string name, string displayName, int weight, bool inverted, Func<Obj_AI_Hero, float> getValue)
        {
            _getValue = getValue;
            Name = name;
            DisplayName = displayName;
            Weight = weight;
            Inverted = inverted;
        }

        public string Name { get; set; }
        public string DisplayName { get; set; }
        public int Weight { get; set; }
        public bool Inverted { get; set; }
        public float CurrentMin { get; set; }

        public float CurrentMax
        {
            get { return _currentMax; }
            set
            {
                if (value > CurrentMin)
                {
                    _currentMax = value;
                }
                else
                {
                    _currentMax = CurrentMin + 1;
                }
            }
        }

        public float CalculatedWeight(Obj_AI_Hero target)
        {
            return CalculatedWeight(GetValue(target), CurrentMin, CurrentMax, Inverted ? Weight : TargetSelector.MinWeight,
                Inverted ? TargetSelector.MinWeight : Weight);
        }

        public float CalculatedWeight(float currentValue, float currentMin, float currentMax, float newMin, float newMax)
        {
            var weight = (((currentValue - currentMin)*(newMax - newMin))/(currentMax - currentMin)) + newMin;
            return !float.IsNaN(weight) && !float.IsInfinity(weight) ? weight : 0;
        }

        public float GetValue(Obj_AI_Hero target)
        {
            try
            {
                return _getValue(target);
            }
            catch
            {
                return Inverted ? float.MaxValue : float.MinValue;
            }
        }
    }
}