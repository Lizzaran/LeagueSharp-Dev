#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Varus.cs is part of SFXChallenger.

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
using SFXChallenger.Abstracts;
using SFXChallenger.Enumerations;
using SFXChallenger.Helpers;
using SFXChallenger.Managers;
using SFXChallenger.Menus;
using SFXChallenger.Wrappers;
using SFXLibrary;
using SFXLibrary.Logger;
using Orbwalking = SFXChallenger.Wrappers.Orbwalking;
using TargetSelector = SFXChallenger.Wrappers.TargetSelector;

#endregion

namespace SFXChallenger.Champions
{
    internal class Varus : Champion
    {
        private float _rSpreadRadius = 450f;
        private MenuItem _wStacks;

        protected override ItemFlags ItemFlags
        {
            get { return ItemFlags.Offensive | ItemFlags.Defensive | ItemFlags.Flee; }
        }

        protected override void OnLoad()
        {
            Core.OnPostUpdate += OnCorePostUpdate;
            Orbwalking.AfterAttack += OnOrbwalkingAfterAttack;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            Drawing.OnDraw += OnDrawingDraw;
        }

        protected override void OnUnload()
        {
            Core.OnPostUpdate -= OnCorePostUpdate;
            Orbwalking.AfterAttack -= OnOrbwalkingAfterAttack;
            AntiGapcloser.OnEnemyGapcloser -= OnEnemyGapcloser;
            Drawing.OnDraw -= OnDrawingDraw;
        }

        protected override void AddToMenu()
        {
            var comboMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Combo"), Menu.Name + ".combo"));
            HitchanceManager.AddToMenu(
                comboMenu.AddSubMenu(new Menu(Global.Lang.Get("F_MH"), comboMenu.Name + ".hitchance")), "combo",
                new Dictionary<string, int> { { "Q", 1 }, { "E", 1 }, { "R", 2 } });
            comboMenu.AddItem(
                new MenuItem(comboMenu.Name + ".q-range", "Q " + Global.Lang.Get("G_OutOfRange")).SetValue(true));
            comboMenu.AddItem(
                new MenuItem(comboMenu.Name + ".q-always", "Q " + Global.Lang.Get("G_Always")).SetValue(false));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".q-stacks", "Q " + Global.Lang.Get("G_StacksIsOrMore")))
                .SetValue(new Slider(3, 1, 3));
            comboMenu.AddItem(
                new MenuItem(comboMenu.Name + ".q-min", "Q " + Global.Lang.Get("G_Min")).SetValue(new Slider(3, 1, 3)));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));
            comboMenu.AddItem(
                new MenuItem(comboMenu.Name + ".e-always", "E " + Global.Lang.Get("G_Always")).SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".e-stacks", "E " + Global.Lang.Get("G_StacksIsOrMore")))
                .SetValue(new Slider(3, 1, 3));
            comboMenu.AddItem(
                new MenuItem(comboMenu.Name + ".e-min", "E " + Global.Lang.Get("G_Min")).SetValue(new Slider(3, 1, 3)));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".r", Global.Lang.Get("G_UseR")).SetValue(true));

            var harassMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Harass"), Menu.Name + ".harass"));
            HitchanceManager.AddToMenu(
                harassMenu.AddSubMenu(new Menu(Global.Lang.Get("F_MH"), harassMenu.Name + ".hitchance")), "harass",
                new Dictionary<string, int> { { "Q", 1 }, { "E", 1 } });
            ManaManager.AddToMenu(harassMenu, "harass", ManaCheckType.Minimum, ManaValueType.Percent);
            harassMenu.AddItem(
                new MenuItem(harassMenu.Name + ".q-range", "Q " + Global.Lang.Get("G_OutOfRange")).SetValue(true));
            harassMenu.AddItem(
                new MenuItem(harassMenu.Name + ".q-always", "Q " + Global.Lang.Get("G_Always")).SetValue(false));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".q-stacks", "Q " + Global.Lang.Get("G_StacksIsOrMore")))
                .SetValue(new Slider(3, 1, 3));
            harassMenu.AddItem(
                new MenuItem(harassMenu.Name + ".q-min", "Q " + Global.Lang.Get("G_Min")).SetValue(new Slider(3, 1, 3)));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));
            harassMenu.AddItem(
                new MenuItem(harassMenu.Name + ".e-always", "E " + Global.Lang.Get("G_Always")).SetValue(false));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".e-stacks", "E " + Global.Lang.Get("G_StacksIsOrMore")))
                .SetValue(new Slider(3, 1, 3));
            harassMenu.AddItem(
                new MenuItem(harassMenu.Name + ".e-min", "E " + Global.Lang.Get("G_Min")).SetValue(new Slider(3, 1, 3)));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(true));

            var laneclearMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_LaneClear"), Menu.Name + ".lane-clear"));
            ManaManager.AddToMenu(laneclearMenu, "lane-clear", ManaCheckType.Minimum, ManaValueType.Percent);
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(true));
            laneclearMenu.AddItem(
                new MenuItem(laneclearMenu.Name + ".min", Global.Lang.Get("G_Min")).SetValue(new Slider(3, 1, 5)));

            var ultimateMenu = UltimateMenu.AddToMenu(Menu, true, false, true, false, true, true, true);

            ultimateMenu.AddItem(
                new MenuItem(ultimateMenu.Name + ".radius", Global.Lang.Get("G_Range")).SetValue(
                    new Slider(450, 100, 600))).ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs args)
                {
                    _rSpreadRadius = args.GetNewValue<Slider>().Value;
                };

            _rSpreadRadius = Menu.Item(Menu.Name + ".ultimate.radius").GetValue<Slider>().Value;

            var killstealMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Killsteal"), Menu.Name + ".killsteal"));
            killstealMenu.AddItem(new MenuItem(killstealMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));
            killstealMenu.AddItem(
                new MenuItem(killstealMenu.Name + ".range", Global.Lang.Get("G_OutOfRange")).SetValue(true));

            var fleeMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Flee"), Menu.Name + ".flee"));
            fleeMenu.AddItem(new MenuItem(fleeMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(true));

            var miscMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Miscellaneous"), Menu.Name + ".miscellaneous"));

            HeroListManager.AddToMenu(
                miscMenu.AddSubMenu(new Menu("E " + Global.Lang.Get("G_Gapcloser"), miscMenu.Name + "e-gapcloser")),
                "e-gapcloser", false, false, true, false);

            TargetSelector.AddWeightedItem(
                new WeightedItem("w-stacks", "W " + Global.Lang.Get("G_Stacks"), 13, true, 333, 500, t => GetWStacks(t)));

            IndicatorManager.AddToMenu(DrawingManager.GetMenu(), true);
            IndicatorManager.Add(Q);
            IndicatorManager.Add(E);
            IndicatorManager.Add(R);
            IndicatorManager.Finale();

            _wStacks = DrawingManager.Add("W " + Global.Lang.Get("G_Stacks"), true);
        }

        protected override void SetupSpells()
        {
            Q = new Spell(SpellSlot.Q, 925f);
            Q.SetSkillshot(0.25f, 70f, 1800f, false, SkillshotType.SkillshotLine);
            Q.SetCharged("VarusQ", "VarusQ", 925, 1600, 1.5f);

            W = new Spell(SpellSlot.W, 0f);

            E = new Spell(SpellSlot.E, 950f);
            E.SetSkillshot(0.50f, 250f, 1400f, false, SkillshotType.SkillshotCircle);

            R = new Spell(SpellSlot.R, 1075f);
            R.SetSkillshot(0.25f, 120f, 1950f, false, SkillshotType.SkillshotLine);
        }

        private void OnCorePostUpdate(EventArgs args)
        {
            try
            {
                if (Menu.Item(Menu.Name + ".ultimate.assisted.enabled").GetValue<bool>() &&
                    Menu.Item(Menu.Name + ".ultimate.assisted.hotkey").GetValue<KeyBind>().Active && R.IsReady())
                {
                    if (Menu.Item(Menu.Name + ".ultimate.assisted.move-cursor").GetValue<bool>())
                    {
                        Orbwalking.MoveTo(Game.CursorPos, Orbwalker.HoldAreaRadius);
                    }

                    if (
                        !RLogic(
                            TargetSelector.GetTarget(R.Range, LeagueSharp.Common.TargetSelector.DamageType.Physical),
                            R.GetHitChance("combo"),
                            Menu.Item(Menu.Name + ".ultimate.assisted.min").GetValue<Slider>().Value))
                    {
                        RLogic1V1(
                            Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady(),
                            Menu.Item(Menu.Name + ".combo.e").GetValue<bool>() && E.IsReady());
                    }
                }

                if (Menu.Item(Menu.Name + ".ultimate.auto.enabled").GetValue<bool>() && R.IsReady())
                {
                    if (
                        !RLogic(
                            TargetSelector.GetTarget(R.Range, LeagueSharp.Common.TargetSelector.DamageType.Physical),
                            R.GetHitChance("combo"),
                            Menu.Item(Menu.Name + ".ultimate.auto.min").GetValue<Slider>().Value))
                    {
                        RLogic1V1(
                            Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady(),
                            Menu.Item(Menu.Name + ".combo.e").GetValue<bool>() && E.IsReady());
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnEnemyGapcloser(ActiveGapcloser args)
        {
            try
            {
                if (!args.Sender.IsEnemy)
                {
                    return;
                }

                if (HeroListManager.Check("e-gapcloser", args.Sender) && args.End.Distance(Player.Position) < E.Range &&
                    E.IsReady())
                {
                    E.Cast(args.End);
                }
                if (Menu.Item(Menu.Name + ".ultimate.auto.enabled").GetValue<bool>() &&
                    HeroListManager.Check("ultimate-gapcloser", args.Sender))
                {
                    RLogic(args.Sender, HitChance.High, 1);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnOrbwalkingAfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            try
            {
                if (unit.IsMe)
                {
                    if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                    {
                        var enemy = target as Obj_AI_Hero;
                        if (enemy != null)
                        {
                            ItemManager.Muramana(true);
                            ItemManager.UseComboItems(enemy);
                            SummonerManager.UseComboSummoners(enemy);
                        }
                    }
                    else
                    {
                        ItemManager.Muramana(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected override void Combo()
        {
            var q = Menu.Item(Menu.Name + ".combo.q").GetValue<bool>();
            var e = Menu.Item(Menu.Name + ".combo.e").GetValue<bool>();
            var r = Menu.Item(Menu.Name + ".ultimate.combo.enabled").GetValue<bool>();

            if (q && Q.IsReady())
            {
                var target = TargetSelector.GetTarget(
                    Q.ChargedMaxRange, LeagueSharp.Common.TargetSelector.DamageType.Physical);
                if (target != null)
                {
                    var stacks = W.Level == 0 && Menu.Item(Menu.Name + ".combo.q-stacks").GetValue<Slider>().Value > 0;
                    if (Q.IsCharging || Menu.Item(Menu.Name + ".combo.q-always").GetValue<bool>() ||
                        Menu.Item(Menu.Name + ".combo.q-range").GetValue<bool>() &&
                        Player.CountEnemiesInRange(Player.AttackRange * 1.075f) == 0 || stacks ||
                        GetWStacks(target) >= Menu.Item(Menu.Name + ".combo.q-stacks").GetValue<Slider>().Value ||
                        Q.IsKillable(target) ||
                        GetQHits(target, Q.GetHitChance("combo")) >=
                        Menu.Item(Menu.Name + ".combo.q-min").GetValue<Slider>().Value)
                    {
                        QLogic(target, Q.GetHitChance("combo"));
                    }
                }
            }
            if (e && E.IsReady())
            {
                var target = TargetSelector.GetTarget(E.Range, LeagueSharp.Common.TargetSelector.DamageType.Physical);
                if (target != null)
                {
                    var stacks = W.Level == 0 && Menu.Item(Menu.Name + ".combo.e-stacks").GetValue<Slider>().Value > 0;
                    if (Menu.Item(Menu.Name + ".combo.e-always").GetValue<bool>() || stacks ||
                        GetWStacks(target) >= Menu.Item(Menu.Name + ".combo.e-stacks").GetValue<Slider>().Value ||
                        E.IsKillable(target) ||
                        GetEHits(target, E.GetHitChance("combo")) >=
                        Menu.Item(Menu.Name + ".combo.e-min").GetValue<Slider>().Value)
                    {
                        ELogic(target, E.GetHitChance("combo"));
                    }
                }
            }
            if (r && R.IsReady())
            {
                var target = TargetSelector.GetTarget(R.Range, LeagueSharp.Common.TargetSelector.DamageType.Physical);
                if (target != null)
                {
                    if (
                        !RLogic(
                            target, R.GetHitChance("combo"),
                            Menu.Item(Menu.Name + ".ultimate.combo.min").GetValue<Slider>().Value))
                    {
                        if (Menu.Item(Menu.Name + ".ultimate.combo.1v1").GetValue<bool>())
                        {
                            RLogic1V1(q, e);
                        }
                    }
                }
            }
        }

        protected override void Harass()
        {
            if (!ManaManager.Check("harass") && !Q.IsCharging)
            {
                return;
            }
            if (Menu.Item(Menu.Name + ".harass.q").GetValue<bool>() && Q.IsReady())
            {
                var target = TargetSelector.GetTarget(
                    Q.ChargedMaxRange, LeagueSharp.Common.TargetSelector.DamageType.Physical);
                if (target != null)
                {
                    var stacks = W.Level == 0 && Menu.Item(Menu.Name + ".harass.q-stacks").GetValue<Slider>().Value > 0;
                    if (Q.IsCharging || W.Level == 0 || Menu.Item(Menu.Name + ".harass.q-always").GetValue<bool>() ||
                        Menu.Item(Menu.Name + ".harass.q-range").GetValue<bool>() &&
                        Player.CountEnemiesInRange(Player.AttackRange * 1.075f) == 0 || stacks ||
                        GetWStacks(target) >= Menu.Item(Menu.Name + ".harass.q-stacks").GetValue<Slider>().Value ||
                        Q.IsKillable(target) ||
                        GetQHits(target, Q.GetHitChance("harass")) >=
                        Menu.Item(Menu.Name + ".harass.q-min").GetValue<Slider>().Value)
                    {
                        QLogic(target, Q.GetHitChance("harass"));
                    }
                }
            }
            if (Menu.Item(Menu.Name + ".harass.e").GetValue<bool>() && E.IsReady())
            {
                var target = TargetSelector.GetTarget(E.Range, LeagueSharp.Common.TargetSelector.DamageType.Physical);
                if (target != null)
                {
                    var stacks = W.Level == 0 && Menu.Item(Menu.Name + ".harass.e-stacks").GetValue<Slider>().Value > 0;
                    if (Menu.Item(Menu.Name + ".harass.e-always").GetValue<bool>() || stacks ||
                        GetWStacks(target) >= Menu.Item(Menu.Name + ".harass.e-stacks").GetValue<Slider>().Value ||
                        E.IsKillable(target) ||
                        GetEHits(target, E.GetHitChance("harass")) >=
                        Menu.Item(Menu.Name + ".harass.e-min").GetValue<Slider>().Value)
                    {
                        ELogic(target, E.GetHitChance("harass"));
                    }
                }
            }
        }

        private void QLogic(Obj_AI_Hero target, HitChance hitChance)
        {
            try
            {
                if (target == null)
                {
                    return;
                }
                if (!Q.IsCharging)
                {
                    var pred = Q.GetPrediction(target);
                    if (pred.Hitchance >= hitChance)
                    {
                        Q.Cast(target);
                    }
                    else
                    {
                        var input = new PredictionInput
                        {
                            Range = Q.ChargedMaxRange,
                            Collision = false,
                            Delay = 1f,
                            Radius = Q.Width,
                            Speed = Q.Speed,
                            Type = Q.Type,
                            Unit = target
                        };
                        if (Prediction.GetPrediction(input).Hitchance >= (hitChance - 1))
                        {
                            Q.StartCharging();
                        }
                    }
                }
                if (Q.IsCharging)
                {
                    var pred = Q.GetPrediction(target);
                    if (pred.Hitchance >= hitChance)
                    {
                        Q.Cast(pred.CastPosition);
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private int GetEHits(Obj_AI_Hero target, HitChance hitChance)
        {
            try
            {
                if (Q.IsCharging || target == null)
                {
                    return 0;
                }
                var pred = E.GetPrediction(target);
                if (pred.Hitchance >= hitChance)
                {
                    var points =
                        (from enemy in GameObjects.EnemyHeroes.Where(e => e.IsValidTarget((E.Range + E.Width) * 1.2f))
                            select E.GetPrediction(target)
                            into pred2
                            where pred2.Hitchance >= (hitChance - 1)
                            select pred2.UnitPosition).ToList();
                    return points.Count(p => p.Distance(pred.CastPosition) < E.Width * 0.9f);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return 0;
        }

        private int GetQHits(Obj_AI_Hero target, HitChance hitChance)
        {
            if (target == null)
            {
                return 0;
            }
            var totalHits = 0;
            try
            {
                var pred = Q.GetPrediction(target);
                if (pred.Hitchance >= hitChance)
                {
                    var enemies =
                        GameObjects.EnemyHeroes.Where(e => e.IsValidTarget((Q.Range + Q.Width) * 1.2f)).ToList();
                    var rect = new Geometry.Polygon.Rectangle(
                        ObjectManager.Player.Position.To2D(),
                        ObjectManager.Player.Position.Extend(pred.CastPosition, Q.ChargedMaxRange * 0.85f).To2D(),
                        Q.Width);
                    totalHits = 1 + (from enemy in enemies.Where(e => e.NetworkId != target.NetworkId)
                        let pred2 = Q.GetPrediction(enemy)
                        where pred2.Hitchance >= (hitChance - 1)
                        where
                            new Geometry.Polygon.Circle(pred2.UnitPosition, enemy.BoundingRadius * 0.9f).Points.Any(
                                p => rect.IsInside(p))
                        select enemy).Count();
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return totalHits;
        }

        private void ELogic(Obj_AI_Hero target, HitChance hitChance)
        {
            try
            {
                if (Q.IsCharging || target == null)
                {
                    return;
                }
                var pred = E.GetPrediction(target);
                if (pred.Hitchance >= hitChance)
                {
                    E.Cast(pred.CastPosition);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private bool RLogic(Obj_AI_Hero target, HitChance hitChance, int min)
        {
            try
            {
                if (Q.IsCharging || target == null)
                {
                    return false;
                }
                var pred = R.GetPrediction(target);
                if (pred.Hitchance >= hitChance)
                {
                    var hits = GameObjects.EnemyHeroes.Where(e => e.Distance(target) <= _rSpreadRadius).ToList();
                    if (hits.Count >= min && hits.Any(h => HeroListManager.Check("ultimate-whitelist", h)) ||
                        (hits.Any(h => HeroListManager.Check("ultimate-force", h)) &&
                         hits.Count >=
                         (Menu.Item(Menu.Name + ".ultimate.force.additional").GetValue<Slider>().Value + 1)))
                    {
                        R.Cast(pred.CastPosition);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return false;
        }

        private void RLogic1V1(bool q, bool e)
        {
            try
            {
                foreach (var t in GameObjects.EnemyHeroes)
                {
                    if (t.HealthPercent > 25)
                    {
                        var cDmg = CalcComboDamage(t, q, e, true);
                        if (cDmg - 10 >= t.Health)
                        {
                            if (
                                GameObjects.EnemyHeroes.Count(
                                    em => !em.IsDead && em.IsVisible && em.Distance(Player) < 3000) == 1)
                            {
                                if (RLogic(t, R.GetHitChance("combo"), 1))
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private float CalcComboDamage(Obj_AI_Hero target, bool q, bool e, bool r)
        {
            try
            {
                if (target == null)
                {
                    return 0;
                }
                float damage = 0;
                if (q)
                {
                    damage += Q.GetDamage(target);
                }
                if (e && E.IsReady())
                {
                    damage += E.GetDamage(target);
                }
                if (r && R.IsReady())
                {
                    damage += R.GetDamage(target);
                }
                damage += 5f * (float) Player.GetAutoAttackDamage(target);
                damage += ItemManager.CalculateComboDamage(target);
                damage += SummonerManager.CalculateComboDamage(target);
                return damage;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return 0;
        }

        protected override void LaneClear()
        {
            if (!ManaManager.Check("lane-clear") && !Q.IsCharging)
            {
                return;
            }
            var min = Menu.Item(Menu.Name + ".lane-clear.min").GetValue<Slider>().Value;
            if (Menu.Item(Menu.Name + ".lane-clear.q").GetValue<bool>() && Q.IsReady())
            {
                Casting.Farm(Q, min);
            }
            if (Menu.Item(Menu.Name + ".lane-clear.e").GetValue<bool>() && E.IsReady())
            {
                Casting.Farm(E, min);
            }
        }

        protected override void Flee()
        {
            if (Menu.Item(Menu.Name + ".flee.e").GetValue<bool>() && !Q.IsCharging && E.IsReady())
            {
                ELogic(
                    GameObjects.EnemyHeroes.Where(e => e.IsValidTarget(E.Range))
                        .OrderBy(e => e.Position.Distance(Player.Position))
                        .FirstOrDefault(), HitChance.High);
            }
        }

        protected override void Killsteal()
        {
            if (Menu.Item(Menu.Name + ".killsteal.q").GetValue<bool>() && Q.IsReady())
            {
                var range = Menu.Item(Menu.Name + ".killsteal.range").GetValue<bool>();
                var killable =
                    GameObjects.EnemyHeroes.FirstOrDefault(
                        e => Q.IsInRange(e) && (!range || !Orbwalking.InAutoAttackRange(e)) && Q.IsKillable(e));
                if (killable != null)
                {
                    QLogic(killable, HitChance.High);
                }
            }
        }

        private int GetWStacks(Obj_AI_Base target)
        {
            return target.GetBuffCount("varuswdebuff");
        }

        private void OnDrawingDraw(EventArgs args)
        {
            try
            {
                if (W.Level > 0 && _wStacks != null && _wStacks.GetValue<bool>() && !Player.IsDead)
                {
                    foreach (var enemy in
                        GameObjects.EnemyHeroes.Where(
                            e => e.IsHPBarRendered && e.Position.IsOnScreen() && e.IsValidTarget()))
                    {
                        var stacks = GetWStacks(enemy) - 1;
                        if (stacks > -1)
                        {
                            var x = enemy.HPBarPosition.X + 45;
                            var y = enemy.HPBarPosition.Y - 25;
                            for (var i = 0; 3 > i; i++)
                            {
                                Drawing.DrawLine(
                                    x + (i * 20), y, x + (i * 20) + 10, y, 10,
                                    (i > stacks ? Color.DarkGray : Color.Orange));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }
}