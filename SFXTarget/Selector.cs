namespace SFXTarget
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using LeagueSharp;
    using LeagueSharp.Common;
    using SharpDX;
    using Color = System.Drawing.Color;

    #endregion

    public class Selector
    {
        internal const int MinWeight = 0;
        internal const int MaxWeight = 20;
        private const int AggroFadeTime = 10;
        private const int DamageFadeTime = 10;
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
        private static readonly Dictionary<Obj_AI_Hero, DamageItem> DamageItems = new Dictionary<Obj_AI_Hero, DamageItem>();

        static Selector()
        {
            WeightedItems = new List<WeightedItem>
            {
                new WeightedItem("Attack Damage", 5, false, t => t.BaseAttackDamage + t.FlatPhysicalDamageMod),
                new WeightedItem("Ability Power", 5, false, t => t.BaseAbilityDamage + t.FlatMagicDamageMod),
                new WeightedItem("Armor Penetration", 5, false,
                    t => t.FlatArmorPenetrationMod + (ObjectManager.Player.Armor*(100 - (1 - t.PercentArmorPenetrationMod)*100))),
                new WeightedItem("Magic Penetration", 5, false,
                    t => t.FlatMagicPenetrationMod + (ObjectManager.Player.SpellBlock*(100 - (1 - t.PercentMagicPenetrationMod)*100))),
                new WeightedItem("Life Steal", 5, false, t => t.PercentLifeStealMod),
                new WeightedItem("Spell Vamp", 5, false, t => t.PercentSpellVampMod),
                new WeightedItem("Crit Chance", 5, false, t => t.Crit),
                new WeightedItem("Attack Speed", 5, false, t => t.AttackSpeedMod),
                new WeightedItem("Low Armor", 5, true, t => t.Armor),
                new WeightedItem("Low Magic Resist", 5, true, t => t.SpellBlock),
                new WeightedItem("Low Health", 5, true, t => t.Health),
                new WeightedItem("Mana", 5, false, t => t.Mana),
                new WeightedItem("Gold", 5, false, t => t.MinionsKilled*MinionGold + t.ChampionsKilled*KillGold + t.Assists*AssistGold),
                new WeightedItem("Short Distance", 5, true, t => t.Distance(ObjectManager.Player)),
                new WeightedItem("Aggro Me", 5, true, delegate(Obj_AI_Hero t)
                {
                    TargetItem aggro;
                    if (AggroItems.TryGetValue(t, out aggro))
                    {
                        if (aggro.Target.IsMe && (Game.Time - aggro.Timestamp) <= AggroFadeTime)
                        {
                            return aggro.Timestamp;
                        }
                    }
                    return Game.Time;
                }),
                new WeightedItem("Team Aggro", 5, false,
                    delegate(Obj_AI_Hero t)
                    {
                        return
                            AggroItems.Where(a => a.Key.IsAlly && a.Value.Target.NetworkId == t.NetworkId)
                                .Count(aggro => (Game.Time - aggro.Value.Timestamp) <= AggroFadeTime);
                    }),
                new WeightedItem("Damage Me Count", 5, false, delegate(Obj_AI_Hero t)
                {
                    DamageItem damage;
                    if (DamageItems.TryGetValue(t, out damage))
                    {
                        damage.Update();
                        return damage.Targets.Count(d => d.Target.IsMe);
                    }
                    return Game.Time;
                }),
                new WeightedItem("Team Damage Count", 5, false, delegate(Obj_AI_Hero t)
                {
                    var count = 0;
                    foreach (var damage in DamageItems.Where(d => d.Key.IsAlly))
                    {
                        damage.Value.Update();
                        count += damage.Value.Targets.Count(d => d.Target.NetworkId == t.NetworkId);
                    }
                    return count;
                }),
                new WeightedItem("Damage Me Total", 5, false, delegate(Obj_AI_Hero t)
                {
                    DamageItem damage;
                    if (DamageItems.TryGetValue(t, out damage))
                    {
                        damage.Update();
                        return damage.Targets.Where(d => d.Target.IsMe).Sum(d => d.Damage);
                    }
                    return Game.Time;
                }),
                new WeightedItem("Team Damage Total", 5, false, delegate(Obj_AI_Hero t)
                {
                    float dmg = 0;
                    foreach (var damage in DamageItems.Where(d => d.Key.IsAlly))
                    {
                        damage.Value.Update();
                        dmg += damage.Value.Targets.Where(d => d.Target.NetworkId == t.NetworkId).Sum(d => d.Damage);
                    }
                    return dmg;
                })
            };

            Game.OnWndProc += OnGameWndProc;
            Drawing.OnDraw += OnDrawingDraw;
            Obj_AI_Base.OnAggro += OnObjAiBaseAggro;
            AttackableUnit.OnDamage += OnAttackableUnitDamage;
        }

        ~Selector()
        {
            Game.OnWndProc -= OnGameWndProc;
            Drawing.OnDraw -= OnDrawingDraw;
            Obj_AI_Base.OnAggro -= OnObjAiBaseAggro;
            AttackableUnit.OnDamage -= OnAttackableUnitDamage;
        }

        private static void OnAttackableUnitDamage(AttackableUnit sender, AttackableUnitDamageEventArgs args)
        {
            var hero = HeroManager.AllHeroes.FirstOrDefault(h => h.NetworkId.Equals(args.SourceNetworkId));
            if (hero != null)
            {
                var target = HeroManager.AllHeroes.FirstOrDefault(h => h.NetworkId.Equals(args.TargetNetworkId));
                if (target != null)
                {
                    DamageItem damage;
                    if (DamageItems.TryGetValue(hero, out damage))
                    {
                        damage.Add(target, args.Damage);
                    }
                    else
                    {
                        DamageItems[target] = new DamageItem(target, args.Damage, DamageFadeTime);
                    }
                }
            }
        }

        private static void OnObjAiBaseAggro(Obj_AI_Base sender, GameObjectAggroEventArgs args)
        {
            var hero = sender as Obj_AI_Hero;
            if (hero != null)
            {
                var target = HeroManager.AllHeroes.FirstOrDefault(h => h.NetworkId.Equals(args.NetworkId));
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

        private static void OnDrawingDraw(EventArgs args)
        {
            if (_selectedTarget != null && _selectedTarget.IsValidTarget() && _menu != null && _menu.Item("FocusSelected").GetValue<bool>() &&
                _menu.Item("DrawingSelectedColor").GetValue<Circle>().Active)
            {
                Render.Circle.DrawCircle(_selectedTarget.Position, _selectedTarget.BoundingRadius + SelectClickBuffer,
                    _menu.Item("DrawingSelectedColor").GetValue<Circle>().Color, _menu.Item("DrawingSelectedThickness").GetValue<Slider>().Value, true);
            }
        }

        private static void OnGameWndProc(WndEventArgs args)
        {
            if (args.Msg != (ulong) WindowsMessages.WM_LBUTTONDOWN)
                return;

            _selectedTarget =
                HeroManager.Enemies.Where(h => h.IsValidTarget() && h.Distance(Game.CursorPos, true) < h.BoundingRadius + SelectClickBuffer)
                    .OrderBy(h => h.Distance(Game.CursorPos, true))
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

        public static Obj_AI_Hero GetTargetNoCollision(Spell spell, bool ignoreShields = true, Vector3 from = new Vector3(),
            IEnumerable<Obj_AI_Hero> ignoredChampions = null)
        {
            var target = GetTarget(spell.Range, spell.DamageType, ignoreShields, spell.From, ignoredChampions);
            return target != null && spell.Collision && spell.GetPrediction(target).Hitchance != HitChance.Collision ? target : null;
        }

        public static Obj_AI_Hero GetTarget(float range, TargetSelector.DamageType damageType = TargetSelector.DamageType.True,
            bool ignoreShields = true, Vector3 from = new Vector3(), IEnumerable<Obj_AI_Hero> ignoredChampions = null)
        {
            try
            {
                if (_menu != null && _selectedTarget != null &&
                    _selectedTarget.IsValidTarget(_menu.Item("ForceFocusSelected").GetValue<bool>() ? float.MaxValue : range, true, from))
                    return _selectedTarget;

                var targets =
                    HeroManager.Enemies.Where(h => h.IsValidTarget(range, true, from))
                        .Where(h => ignoredChampions == null || ignoredChampions.All(i => i.NetworkId != h.NetworkId))
                        .Where(h => Invulnerable.HasBuff(h, damageType, ignoreShields))
                        .ToList();

                foreach (var item in WeightedItems.Where(w => w.Weight > 0))
                {
                    item.CurrentMin = targets.Select(item.GetValue).Min();
                    item.CurrentMax = targets.Select(item.GetValue).Max();
                }

                return ChampionHighestWeight(targets);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return null;
        }

        private static Obj_AI_Hero ChampionHighestWeight(List<Obj_AI_Hero> targets)
        {
            var first = true;
            var maxHero = default(Obj_AI_Hero);
            var maxWeight = default(float);
            foreach (var target in targets)
            {
                var tmpWeight = WeightedItems.Where(w => w.Weight > 0).Sum(weight => weight.CalculatedWeight(target));
                if (_menu != null)
                {
                    var champWeight = (((_menu.Item("Heroes").GetValue<Slider>().Value*(_averageWeight - MinWeight))/5) + MinWeight) + 1;
                    tmpWeight += champWeight*_menu.Item("HeroesWeightMultiplicator").GetValue<Slider>().Value;
                }
                if (first)
                {
                    maxHero = target;
                    maxWeight = tmpWeight;
                    first = false;
                }
                else
                {
                    if (tmpWeight > maxWeight)
                    {
                        maxHero = target;
                        maxWeight = tmpWeight;
                    }
                }
            }
            return maxHero;
        }

        public static void AddToMenu(Menu menu)
        {
            _menu = menu;
            var drawingMenu = _menu.AddSubMenu(new Menu("Drawing", "Drawing"));
            drawingMenu.AddItem(
                new MenuItem(drawingMenu.Name + "SelectedColor", "Selected Target Color").SetShared().SetValue(new Circle(true, Color.Red)));
            drawingMenu.AddItem(new MenuItem(drawingMenu.Name + "SelectedThickness", "Selected Target Thickness").SetShared().SetValue(new Slider(5)));

            var weightsMenu = _menu.AddSubMenu(new Menu("Weights", "Weights"));

            foreach (var item in WeightedItems)
            {
                var localItem = item;
                weightsMenu.AddItem(
                    new MenuItem(weightsMenu.Name + item.Name, item.Name).SetShared().SetValue(new Slider(localItem.Weight, MinWeight, MaxWeight)));
                weightsMenu.Item(weightsMenu.Name + item.Name).ValueChanged += delegate(object sender, OnValueChangeEventArgs args)
                {
                    localItem.Weight = args.GetNewValue<Slider>().Value;
                    _averageWeight = (float) WeightedItems.Average(w => w.Weight);
                };
            }

            var heroesMenu = _menu.AddSubMenu(new Menu("Heroes", "Heroes"));

            heroesMenu.AddItem(
                new MenuItem(heroesMenu.Name + "WeightMultiplicator", "Weight Multiplicator").SetShared()
                    .SetValue(new Slider(1, MinMultiplicator, MaxMultiplicator)));

            foreach (var enemy in HeroManager.Enemies)
            {
                heroesMenu.AddItem(
                    new MenuItem(heroesMenu.Name + enemy.ChampionName, "Weight: " + enemy.ChampionName).SetShared().SetValue(new Slider(1, 1, 5)));
            }

            _menu.AddItem(new MenuItem("FocusSelected", "Focus Selected Target").SetShared().SetValue(true));
            _menu.AddItem(new MenuItem("ForceFocusSelected", "Only Attack Selected Target").SetShared().SetValue(false));
        }
    }
}