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
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXChallenger.Abstracts;
using SFXChallenger.Enumerations;
using SFXChallenger.Helpers;
using SFXChallenger.Library;
using SFXChallenger.Library.Logger;
using SFXChallenger.Managers;
using SFXChallenger.SFXTargetSelector;
using SharpDX;
using Collision = LeagueSharp.Common.Collision;
using Color = System.Drawing.Color;
using DamageType = SFXChallenger.Enumerations.DamageType;
using MinionManager = SFXChallenger.Library.MinionManager;
using MinionOrderTypes = SFXChallenger.Library.MinionOrderTypes;
using MinionTeam = SFXChallenger.Library.MinionTeam;
using MinionTypes = SFXChallenger.Library.MinionTypes;
using Orbwalking = SFXChallenger.Wrappers.Orbwalking;
using Spell = SFXChallenger.Wrappers.Spell;
using TargetSelector = SFXChallenger.SFXTargetSelector.TargetSelector;
using Utils = SFXChallenger.Helpers.Utils;

#endregion

namespace SFXChallenger.Champions
{
    internal class Varus : Champion
    {
        private float _lastLaneClearQStart;
        private float _rSpreadRadius = 450f;
        private MenuItem _wStacks;

        protected override ItemFlags ItemFlags
        {
            get { return ItemFlags.Offensive | ItemFlags.Defensive | ItemFlags.Flee; }
        }

        protected override ItemUsageType ItemUsage
        {
            get { return ItemUsageType.AfterAttack; }
        }

        protected override void OnLoad()
        {
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            Drawing.OnDraw += OnDrawingDraw;
        }

        protected override void AddToMenu()
        {
            var ultimateMenu = UltimateManager.AddToMenu(Menu, true, false, false, true, false, false, true, true, true);

            ultimateMenu.AddItem(
                new MenuItem(ultimateMenu.Name + ".radius", "Range").SetValue(new Slider(450, 100, 600))).ValueChanged
                +=
                delegate(object sender, OnValueChangeEventArgs args)
                {
                    _rSpreadRadius = args.GetNewValue<Slider>().Value;
                };

            _rSpreadRadius = Menu.Item(Menu.Name + ".ultimate.radius").GetValue<Slider>().Value;

            var comboMenu = Menu.AddSubMenu(new Menu("Combo", Menu.Name + ".combo"));
            HitchanceManager.AddToMenu(
                comboMenu.AddSubMenu(new Menu("Hitchance", comboMenu.Name + ".hitchance")), "combo",
                new Dictionary<string, HitChance>
                {
                    { "Q", HitChance.High },
                    { "E", HitChance.High },
                    { "R", HitChance.VeryHigh }
                });
            comboMenu.AddItem(
                new MenuItem(comboMenu.Name + ".q-fast-cast-min", "Fast Cast @ Health %").SetValue(new Slider(25)));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".q-range", "Q " + "Out of Range").SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".q-always", "Q " + "Always").SetValue(false));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".q-stacks", "Q " + "Stacks >="))
                .SetValue(new Slider(3, 1, 3));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".q-min", "Q Min.").SetValue(new Slider(3, 1, 3)));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".q", "Use Q").SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".e-always", "E " + "Always").SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".e-stacks", "E " + "Stacks >="))
                .SetValue(new Slider(3, 1, 3));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".e-min", "E Min.").SetValue(new Slider(3, 1, 3)));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".e", "Use E").SetValue(true));

            var harassMenu = Menu.AddSubMenu(new Menu("Harass", Menu.Name + ".harass"));
            HitchanceManager.AddToMenu(
                harassMenu.AddSubMenu(new Menu("Hitchance", harassMenu.Name + ".hitchance")), "harass",
                new Dictionary<string, HitChance> { { "Q", HitChance.High }, { "E", HitChance.High } });
            ManaManager.AddToMenu(harassMenu, "harass", ManaCheckType.Minimum, ManaValueType.Percent);
            harassMenu.AddItem(
                new MenuItem(harassMenu.Name + ".q-fast-cast-min", "Fast Cast @ Health %").SetValue(new Slider(25)));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".q-range", "Q " + "Out of Range").SetValue(true));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".q-always", "Q " + "Always").SetValue(false));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".q-stacks", "Q " + "Stacks >="))
                .SetValue(new Slider(3, 1, 3));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".q-min", "Q Min.").SetValue(new Slider(3, 1, 3)));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".q", "Use Q").SetValue(true));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".e-always", "E " + "Always").SetValue(false));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".e-stacks", "E " + "Stacks >="))
                .SetValue(new Slider(3, 1, 3));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".e-min", "E Min.").SetValue(new Slider(3, 1, 3)));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".e", "Use E").SetValue(true));

            var laneclearMenu = Menu.AddSubMenu(new Menu("Lane Clear", Menu.Name + ".lane-clear"));
            ManaManager.AddToMenu(laneclearMenu, "lane-clear", ManaCheckType.Minimum, ManaValueType.Percent);
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".q", "Use Q").SetValue(true));
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".e", "Use E").SetValue(true));
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".min", "Min.").SetValue(new Slider(3, 1, 5)));

            var fleeMenu = Menu.AddSubMenu(new Menu("Flee", Menu.Name + ".flee"));
            fleeMenu.AddItem(new MenuItem(fleeMenu.Name + ".e", "Use E").SetValue(true));

            var killstealMenu = Menu.AddSubMenu(new Menu("Killsteal", Menu.Name + ".killsteal"));
            killstealMenu.AddItem(new MenuItem(killstealMenu.Name + ".q", "Use Q").SetValue(true));
            killstealMenu.AddItem(new MenuItem(killstealMenu.Name + ".range", "Out of Range").SetValue(true));

            var miscMenu = Menu.AddSubMenu(new Menu("Misc", Menu.Name + ".miscellaneous"));
            HeroListManager.AddToMenu(
                miscMenu.AddSubMenu(new Menu("E Gapcloser", miscMenu.Name + "e-gapcloser")), "e-gapcloser", false, false,
                true, false);

            Weights.AddItem(new Weights.Item("w-stacks", "W " + "Stacks", 13, true, t => GetWStacks(t) + 1));

            IndicatorManager.AddToMenu(DrawingManager.Menu, true);
            IndicatorManager.Add("Q", hero => Q.IsReady() ? Q.GetDamage(hero, 1) : 0);
            IndicatorManager.Add(E);
            IndicatorManager.Add(R);
            IndicatorManager.Finale();

            _wStacks = DrawingManager.Add("W " + "Stacks", true);
        }

        protected override void SetupSpells()
        {
            Q = new Spell(SpellSlot.Q, 925f);
            Q.SetSkillshot(0.25f, 70f, 1800f, false, SkillshotType.SkillshotLine);
            Q.SetCharged("VarusQ", "VarusQ", 925, 1700, 1.5f);

            W = new Spell(SpellSlot.W, 0f, DamageType.Magical);

            E = new Spell(SpellSlot.E, 950f);
            E.SetSkillshot(0.25f, 250f, 1500f, false, SkillshotType.SkillshotCircle);

            R = new Spell(SpellSlot.R, 1075f);
            R.SetSkillshot(0.25f, 120f, 1950f, false, SkillshotType.SkillshotLine);
        }

        protected override void OnPreUpdate() {}

        protected override void OnPostUpdate()
        {
            Orbwalker.SetAttack(!Q.IsCharging);
            if (UltimateManager.Assisted() && R.IsReady())
            {
                if (Menu.Item(Menu.Name + ".ultimate.assisted.move-cursor").GetValue<bool>())
                {
                    Orbwalking.MoveTo(Game.CursorPos, Orbwalker.HoldAreaRadius);
                }

                if (
                    !RLogic(
                        TargetSelector.GetTarget(R), R.GetHitChance("combo"),
                        Menu.Item(Menu.Name + ".ultimate.assisted.min").GetValue<Slider>().Value,
                        Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady(),
                        Menu.Item(Menu.Name + ".combo.e").GetValue<bool>() && E.IsReady()))
                {
                    RLogicSingle(
                        Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady(),
                        Menu.Item(Menu.Name + ".combo.e").GetValue<bool>() && E.IsReady());
                }
            }

            if (UltimateManager.Auto() && R.IsReady())
            {
                if (
                    !RLogic(
                        TargetSelector.GetTarget(R), R.GetHitChance("combo"),
                        Menu.Item(Menu.Name + ".ultimate.auto.min").GetValue<Slider>().Value,
                        Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady(),
                        Menu.Item(Menu.Name + ".combo.e").GetValue<bool>() && E.IsReady(), UltimateModeType.Auto))
                {
                    RLogicSingle(
                        Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady(),
                        Menu.Item(Menu.Name + ".combo.e").GetValue<bool>() && E.IsReady());
                }
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
                    var target = TargetSelector.GetTarget(E.Range * 0.85f, E.DamageType);
                    if (target == null || args.Sender.NetworkId.Equals(target.NetworkId))
                    {
                        E.Cast(args.End);
                    }
                }
                if (UltimateManager.Gapcloser(args.Sender))
                {
                    RLogic(
                        args.Sender, HitChance.High, 1,
                        Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady(),
                        Menu.Item(Menu.Name + ".combo.e").GetValue<bool>() && E.IsReady());
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
            var r = UltimateManager.Combo();

            if (e && !Q.IsCharging && E.IsReady())
            {
                var target = TargetSelector.GetTarget(E);
                if (target != null)
                {
                    var stacks = W.Level == 0 && Menu.Item(Menu.Name + ".combo.e-stacks").GetValue<Slider>().Value > 0;
                    if (Menu.Item(Menu.Name + ".combo.e-always").GetValue<bool>() || stacks ||
                        GetWStacks(target) >= Menu.Item(Menu.Name + ".combo.e-stacks").GetValue<Slider>().Value ||
                        E.IsKillable(target) ||
                        CPrediction.Circle(E, target, E.GetHitChance("combo")).TotalHits >=
                        Menu.Item(Menu.Name + ".combo.e-min").GetValue<Slider>().Value)
                    {
                        ELogic(target, E.GetHitChance("combo"));
                    }
                }
            }
            if (q && Q.IsReady())
            {
                var target = TargetSelector.GetTarget((Q.ChargedMaxRange + Q.Width) * 1.1f, Q.DamageType);
                if (target != null)
                {
                    var stacks = W.Level == 0 && Menu.Item(Menu.Name + ".combo.q-stacks").GetValue<Slider>().Value > 0;
                    if (Q.IsCharging || Menu.Item(Menu.Name + ".combo.q-always").GetValue<bool>() ||
                        Menu.Item(Menu.Name + ".combo.q-range").GetValue<bool>() &&
                        !Orbwalking.InAutoAttackRange(target) || stacks ||
                        GetWStacks(target) >= Menu.Item(Menu.Name + ".combo.q-stacks").GetValue<Slider>().Value ||
                        CPrediction.Line(Q, target, Q.GetHitChance("combo")).TotalHits >=
                        Menu.Item(Menu.Name + ".combo.q-min").GetValue<Slider>().Value || Q.IsKillable(target))
                    {
                        QLogic(
                            target, Q.GetHitChance("combo"),
                            Menu.Item(Menu.Name + ".combo.q-fast-cast-min").GetValue<Slider>().Value);
                    }
                }
            }
            if (r && R.IsReady())
            {
                var target = TargetSelector.GetTarget(R);
                if (target != null)
                {
                    if (
                        !RLogic(
                            target, R.GetHitChance("combo"),
                            Menu.Item(Menu.Name + ".ultimate.combo.min").GetValue<Slider>().Value,
                            Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady(),
                            Menu.Item(Menu.Name + ".combo.e").GetValue<bool>() && E.IsReady()))
                    {
                        if (Menu.Item(Menu.Name + ".ultimate.combo.single").GetValue<bool>())
                        {
                            RLogicSingle(q, e);
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
            if (Menu.Item(Menu.Name + ".harass.e").GetValue<bool>() && !Q.IsCharging && E.IsReady())
            {
                var target = TargetSelector.GetTarget(E);
                if (target != null)
                {
                    var stacks = W.Level == 0 && Menu.Item(Menu.Name + ".harass.e-stacks").GetValue<Slider>().Value > 0;
                    if (Menu.Item(Menu.Name + ".harass.e-always").GetValue<bool>() || stacks ||
                        GetWStacks(target) >= Menu.Item(Menu.Name + ".harass.e-stacks").GetValue<Slider>().Value ||
                        E.IsKillable(target) ||
                        CPrediction.Circle(E, target, E.GetHitChance("harass")).TotalHits >=
                        Menu.Item(Menu.Name + ".combo.e-min").GetValue<Slider>().Value)
                    {
                        ELogic(target, E.GetHitChance("harass"));
                    }
                }
            }
            if (Menu.Item(Menu.Name + ".harass.q").GetValue<bool>() && Q.IsReady())
            {
                var target = TargetSelector.GetTarget((Q.ChargedMaxRange + Q.Width) * 1.1f, Q.DamageType);
                if (target != null)
                {
                    var stacks = W.Level == 0 && Menu.Item(Menu.Name + ".harass.q-stacks").GetValue<Slider>().Value > 0;
                    if (Q.IsCharging || Menu.Item(Menu.Name + ".harass.q-always").GetValue<bool>() ||
                        Menu.Item(Menu.Name + ".harass.q-range").GetValue<bool>() &&
                        !Orbwalking.InAutoAttackRange(target) || stacks ||
                        GetWStacks(target) >= Menu.Item(Menu.Name + ".harass.q-stacks").GetValue<Slider>().Value ||
                        Q.IsKillable(target) ||
                        CPrediction.Line(Q, target, Q.GetHitChance("harass")).TotalHits >=
                        Menu.Item(Menu.Name + ".harass.q-min").GetValue<Slider>().Value)
                    {
                        QLogic(
                            target, Q.GetHitChance("harass"),
                            Menu.Item(Menu.Name + ".harass.q-fast-cast-min").GetValue<Slider>().Value);
                    }
                }
            }
        }

        private bool QMaxRangeHit(Obj_AI_Hero target)
        {
            var delay = (Q.ChargeDuration / 1000f) *
                        ((Q.Range - Q.ChargedMinRange) / (Q.ChargedMaxRange - Q.ChargedMinRange));
            return
                Utils.PositionAfter(
                    target,
                    delay + (Player.Distance(target) - Q.Width - target.BoundingRadius * 0.75f) / Q.Speed +
                    Game.Ping / 2000f, target.MoveSpeed).Distance(Player) < Q.ChargedMaxRange;
        }

        private bool QIsKillable(Obj_AI_Hero target, int collisions)
        {
            return target.Health + target.HPRegenRate / 2f < GetQDamage(target, collisions);
        }

        private bool IsFullyCharged()
        {
            return Q.ChargedMaxRange - Q.Range < 200;
        }

        private float GetQDamage(Obj_AI_Hero target, int collisions)
        {
            if (Q.Level == 0)
            {
                return 0;
            }
            var chargePercentage = Q.Range / Q.ChargedMaxRange;
            var damage =
                (float)
                    ((new float[] { 10, 47, 83, 120, 157 }[Q.Level - 1] +
                      new float[] { 5, 23, 42, 60, 78 }[Q.Level - 1] * chargePercentage) +
                     (chargePercentage * (Player.TotalAttackDamage() + Player.TotalAttackDamage * .6)));
            var minimum = damage / 100f * 33f;
            for (var i = 0; i < collisions; i++)
            {
                var reduce = (damage / 100f * 15f);
                if (damage - reduce < minimum)
                {
                    damage = minimum;
                    break;
                }
                damage -= reduce;
            }
            return (float) Player.CalcDamage(target, Damage.DamageType.Physical, damage);
        }

        private int GetQCollisionsCount(Obj_AI_Hero target, Vector3 castPos)
        {
            try
            {
                var input = new PredictionInput
                {
                    Unit = target,
                    Radius = Q.Width,
                    Delay = Q.Delay,
                    Speed = Q.Speed,
                    CollisionObjects = new[] { CollisionableObjects.Heroes, CollisionableObjects.Minions }
                };
                return
                    Collision.GetCollision(
                        new List<Vector3> { Player.Position.Extend(castPos, Q.Range + Q.Width) }, input).Count;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return 0;
        }

        private void QLogic(Obj_AI_Hero target, HitChance hitChance, int minHealthPercent)
        {
            try
            {
                if (target == null)
                {
                    return;
                }

                var pred = CPrediction.Line(Q, target, hitChance);
                if (pred.TotalHits > 0 &&
                    (QIsKillable(target, GetQCollisionsCount(target, pred.CastPosition)) ||
                     Player.HealthPercent <= minHealthPercent))
                {
                    Q.Cast(pred.CastPosition);
                }

                if (!Q.IsCharging)
                {
                    if (QMaxRangeHit(target))
                    {
                        Q.StartCharging();
                    }
                }
                if (Q.IsCharging && (!QMaxRangeHit(target) || IsFullyCharged()))
                {
                    var pred2 = CPrediction.Line(Q, target, hitChance);
                    if (pred2.TotalHits > 0)
                    {
                        Q.Cast(pred2.CastPosition);
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void ELogic(Obj_AI_Hero target, HitChance hitChance)
        {
            try
            {
                if (Q.IsCharging || target == null)
                {
                    return;
                }

                var best = CPrediction.Circle(E, target, hitChance);
                if (best.TotalHits > 0 && !best.CastPosition.Equals(Vector3.Zero))
                {
                    E.Cast(best.CastPosition);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private bool RLogic(Obj_AI_Hero target,
            HitChance hitChance,
            int min,
            bool q,
            bool e,
            UltimateModeType mode = UltimateModeType.Combo)
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
                    var hits = GameObjects.EnemyHeroes.Where(x => x.Distance(target) <= _rSpreadRadius).ToList();
                    if (UltimateManager.Check(mode, min, hits, hero => CalcComboDamage(hero, q, e, true)))
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

        private void RLogicSingle(bool q, bool e)
        {
            try
            {
                foreach (var t in GameObjects.EnemyHeroes)
                {
                    if (UltimateManager.CheckSingle(t, CalcComboDamage(t, q, e, true)))
                    {
                        if (RLogic(t, R.GetHitChance("combo"), 1, q, e))
                        {
                            break;
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
                    damage += GetQDamage(target, 1);
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
            var min = Menu.Item(Menu.Name + ".lane-clear.min").GetValue<Slider>().Value;
            if (Menu.Item(Menu.Name + ".lane-clear.q").GetValue<bool>() && Q.IsReady() &&
                (ManaManager.Check("lane-clear") || Q.IsCharging))
            {
                var minions = MinionManager.GetMinions(
                    Q.ChargedMaxRange, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
                if (Q.IsCharging || minions.Count >= min || minions.Any(m => m.Team == GameObjectTeam.Neutral))
                {
                    if (!Q.IsCharging)
                    {
                        Q.StartCharging();
                        _lastLaneClearQStart = Game.Time;
                    }
                    if (Q.IsCharging && IsFullyCharged())
                    {
                        Casting.Farm(
                            Q, Game.Time - _lastLaneClearQStart > 3 ? 1 : (minions.Count < min ? minions.Count : min),
                            -1f, false, minions);
                    }
                }
            }

            if (Menu.Item(Menu.Name + ".lane-clear.e").GetValue<bool>() && E.IsReady() &&
                ManaManager.Check("lane-clear"))
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
                        e =>
                            e.IsValidTarget(Q.Range) && (!range || !Orbwalking.InAutoAttackRange(e)) &&
                            (QIsKillable(e, 1) || QMaxRangeHit(e) && QIsKillable(e, 2)));
                if (killable != null)
                {
                    QLogic(killable, HitChance.High, 100);
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