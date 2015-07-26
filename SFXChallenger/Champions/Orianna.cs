#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 orianna.cs is part of SFXChallenger.

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
using SFXChallenger.Events;
using SFXChallenger.Managers;
using SFXLibrary;
using SFXLibrary.Extensions.NET;
using SFXLibrary.Logger;
using SharpDX;
using MinionManager = SFXLibrary.MinionManager;
using MinionOrderTypes = SFXLibrary.MinionOrderTypes;
using MinionTeam = SFXLibrary.MinionTeam;
using MinionTypes = SFXLibrary.MinionTypes;
using Orbwalking = SFXChallenger.Wrappers.Orbwalking;

#endregion

namespace SFXChallenger.Champions
{
    internal class Orianna : Champion
    {
        private readonly float _maxBallDistance = 1300f;

        protected override ItemFlags ItemFlags
        {
            get { return ItemFlags.Offensive | ItemFlags.Defensive | ItemFlags.Flee; }
        }

        protected override void OnLoad()
        {
            Core.OnPostUpdate += OnCorePostUpdate;
            Interrupter2.OnInterruptableTarget += OnInterruptableTarget;
            InitiatorManager.OnAllyInitiator += OnAllyInitiator;
            Spellbook.OnCastSpell += OnSpellbookCastSpell;
            Ball.OnPositionChange += OnBallPositionChange;
        }

        protected override void OnUnload()
        {
            Core.OnPostUpdate -= OnCorePostUpdate;
            Interrupter2.OnInterruptableTarget -= OnInterruptableTarget;
            InitiatorManager.OnAllyInitiator -= OnAllyInitiator;
            Spellbook.OnCastSpell -= OnSpellbookCastSpell;
            Ball.OnPositionChange -= OnBallPositionChange;
        }

        protected override void AddToMenu()
        {
            var comboMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Combo"), Menu.Name + ".combo"));
            HitchanceManager.AddToMenu(
                comboMenu.AddSubMenu(new Menu(Global.Lang.Get("F_MH"), comboMenu.Name + ".hitchance")), "combo",
                new Dictionary<string, int> { { "Q", 2 } });
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".w", Global.Lang.Get("G_UseW")).SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(true));

            var harassMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Harass"), Menu.Name + ".harass"));
            HitchanceManager.AddToMenu(
                harassMenu.AddSubMenu(new Menu(Global.Lang.Get("F_MH"), harassMenu.Name + ".hitchance")), "harass",
                new Dictionary<string, int> { { "Q", 2 } });
            ManaManager.AddToMenu(harassMenu, "harass", ManaCheckType.Minimum, ManaValueType.Percent);
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".w", Global.Lang.Get("G_UseW")).SetValue(true));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(true));

            var laneclearMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_LaneClear"), Menu.Name + ".lane-clear"));
            ManaManager.AddToMenu(laneclearMenu, "lane-clear", ManaCheckType.Minimum, ManaValueType.Percent);
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".w", Global.Lang.Get("G_UseW")).SetValue(true));

            var ultimateMenu = UltimateManager.AddToMenu(Menu, true, true, false, false, false, true, true, true, true);

            ultimateMenu.AddItem(
                new MenuItem(ultimateMenu.Name + ".range", Global.Lang.Get("G_Range")).SetValue(
                    new Slider(350, 250, 400))).ValueChanged += delegate(object sender, OnValueChangeEventArgs args)
                    {
                        R.Range = args.GetNewValue<Slider>().Value;
                        DrawingManager.Update(
                            "R " + Global.Lang.Get("G_Flash"),
                            args.GetNewValue<Slider>().Value + SummonerManager.Flash.Range);
                    };

            var fleeMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Flee"), Menu.Name + ".flee"));
            fleeMenu.AddItem(new MenuItem(fleeMenu.Name + ".w", Global.Lang.Get("G_UseW")).SetValue(true));
            fleeMenu.AddItem(new MenuItem(fleeMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(true));

            var initiatorMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("F_IM"), Menu.Name + ".initiator"));
            InitiatorManager.AddToMenu(
                initiatorMenu.AddSubMenu(new Menu(Global.Lang.Get("G_Whitelist"), initiatorMenu.Name + ".whitelist")),
                true, false);
            initiatorMenu.AddItem(new MenuItem(initiatorMenu.Name + ".use-e", Global.Lang.Get("G_UseE")).SetValue(true));

            var miscMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Miscellaneous"), Menu.Name + ".miscellaneous"));
            ManaManager.AddToMenu(
                miscMenu, "e-self", ManaCheckType.Minimum, ManaValueType.Percent, "E " + Global.Lang.Get("G_Self"));
            ManaManager.AddToMenu(
                miscMenu, "e-allies", ManaCheckType.Minimum, ManaValueType.Percent, "E " + Global.Lang.Get("G_Allies"));
            miscMenu.AddItem(
                new MenuItem(miscMenu.Name + ".e-allies", "E " + Global.Lang.Get("G_Allies")).SetValue(true));
            miscMenu.AddItem(
                new MenuItem(miscMenu.Name + ".block-r", Global.Lang.Get("G_BlockMissing") + " R").SetValue(true));

            DrawingManager.Add("R " + Global.Lang.Get("G_Flash"), R.Range + SummonerManager.Flash.Range);

            IndicatorManager.AddToMenu(DrawingManager.GetMenu(), true);
            IndicatorManager.Add(Q);
            IndicatorManager.Add(W);
            IndicatorManager.Add(E);
            IndicatorManager.Add(R);
            IndicatorManager.Finale();
        }

        private void OnSpellbookCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            try
            {
                if (sender.Owner.IsMe && args.Slot == SpellSlot.R)
                {
                    if (Ball.IsMoving || Menu.Item(Menu.Name + ".miscellaneous.block-r").GetValue<bool>())
                    {
                        args.Process = GameObjects.EnemyHeroes.Any(e => e.Distance(Ball.Position) < R.Width * 2);
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnAllyInitiator(object sender, InitiatorArgs args)
        {
            try
            {
                if (!Menu.Item(Menu.Name + ".initiator.use-e").GetValue<bool>() || Ball.IsMoving || !E.IsReady() ||
                    (Ball.Hero != null && Ball.Hero.NetworkId.Equals(args.Hero.NetworkId)))
                {
                    return;
                }
                if (args.Start.Distance(Player.Position) <= E.Range &&
                    args.End.Distance(Player.Position) <= _maxBallDistance &&
                    GameObjects.EnemyHeroes.Any(
                        e =>
                            !e.IsDead &&
                            (e.Position.Distance(args.End) < 600 || e.Position.Distance(args.Start) < args.Range + 300)))
                {
                    E.CastOnUnit(args.Hero);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnBallPositionChange(object sender, EventArgs e)
        {
            try
            {
                Q.UpdateSourcePosition(Ball.Position, ObjectManager.Player.Position);
                E.UpdateSourcePosition(Ball.Position, ObjectManager.Player.Position);

                W.UpdateSourcePosition(Ball.Position, Ball.Position);
                R.UpdateSourcePosition(Ball.Position, Ball.Position);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected override void SetupSpells()
        {
            Q = new Spell(SpellSlot.Q, 825f);
            Q.SetSkillshot(0.25f, 110f, 1350f, false, SkillshotType.SkillshotCircle);

            W = new Spell(SpellSlot.W, 220f);
            W.SetSkillshot(0.1f, 240f, float.MaxValue, false, SkillshotType.SkillshotCircle);

            E = new Spell(SpellSlot.E, 1095f);
            E.SetSkillshot(0.25f, 125f, 1700f, false, SkillshotType.SkillshotLine);

            R = new Spell(SpellSlot.R, 400f);
            R.SetSkillshot(0.60f, 350f, float.MaxValue, false, SkillshotType.SkillshotCircle);
        }

        private void OnCorePostUpdate(EventArgs args)
        {
            try
            {
                Q.UpdateSourcePosition(Ball.Position, ObjectManager.Player.Position);
                E.UpdateSourcePosition(Ball.Position, ObjectManager.Player.Position);

                if (UltimateManager.Flash() && R.IsReady() && SummonerManager.Flash.IsReady())
                {
                    if (Menu.Item(Menu.Name + ".ultimate.flash.move-cursor").GetValue<bool>())
                    {
                        Orbwalking.MoveTo(Game.CursorPos, Orbwalker.HoldAreaRadius);
                    }
                    if (Ball.Status != BallStatus.Me)
                    {
                        if (E.IsReady())
                        {
                            E.CastOnUnit(Player);
                        }
                        return;
                    }
                    if (Ball.IsMoving)
                    {
                        return;
                    }
                    var target = TargetSelector.GetTarget(
                        (R.Range + SummonerManager.Flash.Range) * 1.2f, TargetSelector.DamageType.Magical);
                    if (target != null && !target.IsDashing() &&
                        (Prediction.GetPrediction(target, R.Delay + 0.3f).UnitPosition.Distance(Player.Position)) >
                        R.Range * 1.025f)
                    {
                        var min = Menu.Item(Menu.Name + ".ultimate.flash.min").GetValue<Slider>().Value;
                        var flashPos = Player.Position.Extend(target.Position, SummonerManager.Flash.Range);
                        var pred =
                            Prediction.GetPrediction(
                                new PredictionInput
                                {
                                    Aoe = true,
                                    Collision = false,
                                    CollisionObjects = new[] { CollisionableObjects.YasuoWall },
                                    From = flashPos,
                                    RangeCheckFrom = flashPos,
                                    Delay = R.Delay + 0.3f,
                                    Range = R.Range,
                                    Speed = R.Speed,
                                    Radius = R.Width,
                                    Type = R.Type,
                                    Unit = target
                                });
                        if (pred.Hitchance >= R.GetHitChance("combo"))
                        {
                            R.UpdateSourcePosition(flashPos, flashPos);
                            var hits = GameObjects.EnemyHeroes.Where(x => R.WillHit(x, pred.CastPosition)).ToList();
                            if (UltimateManager.Check(min, hits))
                            {
                                R.Cast(Player.Position);
                                Utility.DelayAction.Add(300, () => SummonerManager.Flash.Cast(flashPos));
                            }
                            else if (Menu.Item(Menu.Name + ".ultimate.flash.duel").GetValue<bool>())
                            {
                                if (UltimateManager.Check(1, hits))
                                {
                                    var cDmg = CalcComboDamage(
                                        target, Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady(),
                                        Menu.Item(Menu.Name + ".combo.w").GetValue<bool>() && W.IsReady(),
                                        Menu.Item(Menu.Name + ".combo.e").GetValue<bool>() && E.IsReady(), true);
                                    if (cDmg - 20 >= target.Health)
                                    {
                                        R.Cast(Player.Position);
                                        Utility.DelayAction.Add(300, () => SummonerManager.Flash.Cast(flashPos));
                                    }
                                }
                            }
                            R.UpdateSourcePosition(Ball.Position, Ball.Position);
                        }
                    }
                }

                if (UltimateManager.Assisted() && R.IsReady() && !Ball.IsMoving)
                {
                    if (Menu.Item(Menu.Name + ".ultimate.assisted.move-cursor").GetValue<bool>())
                    {
                        Orbwalking.MoveTo(Game.CursorPos, Orbwalker.HoldAreaRadius);
                    }
                    if (!RLogic(Menu.Item(Menu.Name + ".ultimate.assisted.min").GetValue<Slider>().Value))
                    {
                        var casted = false;
                        if (Menu.Item(Menu.Name + ".ultimate.assisted.duel").GetValue<bool>())
                        {
                            casted = RLogicDuel(
                                Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady(),
                                Menu.Item(Menu.Name + ".combo.w").GetValue<bool>() && W.IsReady(),
                                Menu.Item(Menu.Name + ".combo.e").GetValue<bool>() && E.IsReady());
                        }
                        if (!casted)
                        {
                            if (E.IsReady())
                            {
                                EComboLogic(R);
                            }
                            if (Q.IsReady())
                            {
                                int hits;
                                var pos = AssistedQLogic(out hits);
                                if (!pos.Equals(Vector3.Zero) && hits >= 1)
                                {
                                    Q.Cast(pos);
                                }
                            }
                        }
                    }
                }

                if (UltimateManager.Auto() && R.IsReady() && !Ball.IsMoving)
                {
                    if (!RLogic(Menu.Item(Menu.Name + ".ultimate.auto.min").GetValue<Slider>().Value))
                    {
                        if (Menu.Item(Menu.Name + ".ultimate.auto.duel").GetValue<bool>())
                        {
                            RLogicDuel(
                                Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady(),
                                Menu.Item(Menu.Name + ".combo.w").GetValue<bool>() && W.IsReady(),
                                Menu.Item(Menu.Name + ".combo.e").GetValue<bool>() && E.IsReady());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnInterruptableTarget(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            try
            {
                if (sender.IsEnemy && args.DangerLevel == Interrupter2.DangerLevel.High &&
                    UltimateManager.Interrupt(sender) && R.IsReady())
                {
                    var hits = GetHits(R);
                    if (hits.Item2.Any(i => i.NetworkId.Equals(sender.NetworkId)))
                    {
                        R.Cast(Player.Position);
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
            if (Ball.IsMoving)
            {
                return;
            }
            var q = Menu.Item(Menu.Name + ".combo.q").GetValue<bool>();
            var w = Menu.Item(Menu.Name + ".combo.w").GetValue<bool>();
            var e = Menu.Item(Menu.Name + ".combo.e").GetValue<bool>();
            var r = UltimateManager.Combo();
            var target = TargetSelector.GetTarget(Q.Range + Q.Width, TargetSelector.DamageType.Magical);
            if (w && W.IsReady())
            {
                WLogic(1);
            }
            if (r && R.IsReady())
            {
                if (!RLogic(Menu.Item(Menu.Name + ".ultimate.combo.min").GetValue<Slider>().Value))
                {
                    if (Menu.Item(Menu.Name + ".ultimate.combo.duel").GetValue<bool>())
                    {
                        RLogicDuel(q, w, e);
                    }
                }
            }
            if (q && Q.IsReady())
            {
                QLogic(target, Q.GetHitChance("combo"), e);
            }
            if (e && E.IsReady())
            {
                ELogic();
            }
            if (target != null && CalcComboDamage(target, q, w, e, r) > target.Health)
            {
                ItemManager.UseComboItems(target);
                SummonerManager.UseComboSummoners(target);
            }
        }

        protected override void Harass()
        {
            if (!ManaManager.Check("harass") || Ball.IsMoving)
            {
                return;
            }
            var q = Menu.Item(Menu.Name + ".harass.q").GetValue<bool>();
            var w = Menu.Item(Menu.Name + ".harass.w").GetValue<bool>();
            var e = Menu.Item(Menu.Name + ".harass.e").GetValue<bool>();
            if (w && W.IsReady())
            {
                WLogic(1);
            }
            if (q && Q.IsReady())
            {
                QLogic(
                    TargetSelector.GetTarget(Q.Range + Q.Width, TargetSelector.DamageType.Magical),
                    Q.GetHitChance("combo"), e);
            }
            if (e && E.IsReady())
            {
                ELogic();
            }
        }

        private float CalcComboDamage(Obj_AI_Hero target, bool q, bool w, bool e, bool r)
        {
            try
            {
                if (target == null)
                {
                    return 0;
                }
                float damage = 0;
                if (q && (Q.IsReady() || Q.Instance.CooldownExpires - Game.Time <= 1))
                {
                    damage += Q.GetDamage(target);
                }
                if (w && (W.IsReady() || W.Instance.CooldownExpires - Game.Time <= 1))
                {
                    damage += W.GetDamage(target);
                }
                if (e) {}
                if (r && R.IsReady())
                {
                    damage += R.GetDamage(target);
                }
                damage += 2 * (float) Player.GetAutoAttackDamage(target, true);
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

        private void QLogic(Obj_AI_Hero target, HitChance hitChance, bool useE)
        {
            try
            {
                if (target == null)
                {
                    return;
                }
                if (Utility.CountEnemiesInRange((int) (Q.Range + R.Width)) > 1)
                {
                    var qLoc = GetBestQLocation(target, hitChance);
                    if (qLoc.Item1 > 1)
                    {
                        Q.Cast(qLoc.Item2);
                        return;
                    }
                }
                var pred = Q.GetPrediction(target);
                if (pred.Hitchance < hitChance)
                {
                    return;
                }
                if (useE && E.IsReady())
                {
                    var directTravelTime = Ball.Position.Distance(pred.CastPosition) / Q.Speed;
                    var bestEqTravelTime = float.MaxValue;
                    Obj_AI_Hero eqTarget = null;
                    foreach (var ally in GameObjects.AllyHeroes.Where(h => h.IsValidTarget(E.Range, false)))
                    {
                        var t = Ball.Position.Distance(ally.ServerPosition) / E.Speed +
                                ally.Distance(pred.CastPosition) / Q.Speed;
                        if (t < bestEqTravelTime)
                        {
                            eqTarget = ally;
                            bestEqTravelTime = t;
                        }
                    }
                    if (eqTarget != null &&
                        (eqTarget.IsMe || Menu.Item(Menu.Name + ".miscellaneous.e-allies").GetValue<bool>()) &&
                        (eqTarget.IsMe && ManaManager.Check("e-self") || !eqTarget.IsMe && ManaManager.Check("e-allies")) &&
                        bestEqTravelTime < directTravelTime * 1.3f &&
                        (Ball.Position.Distance(eqTarget.ServerPosition, true) > 10000))
                    {
                        E.CastOnUnit(eqTarget);
                        return;
                    }
                }
                Q.Cast(pred.CastPosition);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void WLogic(int minHits)
        {
            try
            {
                var hits = GetHits(W);
                if (hits.Item1 >= minHits)
                {
                    W.Cast(Player.Position);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void ELogic()
        {
            try
            {
                if (Ball.IsMoving)
                {
                    return;
                }
                Obj_AI_Hero target = null;
                var minHits = 1;
                if (Utility.CountEnemiesInRange((int) (Q.Range + R.Width)) <= 1)
                {
                    foreach (var ally in GameObjects.AllyHeroes.Where(h => h.IsValidTarget(E.Range, false)))
                    {
                        if (ally.Position.CountEnemiesInRange(300) >= 1)
                        {
                            E.CastOnUnit(ally);
                            return;
                        }
                        target = ally;
                    }
                    if (target == null)
                    {
                        target = Player;
                    }
                    if ((target.IsMe || Menu.Item(Menu.Name + ".miscellaneous.e-allies").GetValue<bool>()) &&
                        (target.IsMe && ManaManager.Check("e-self") || !target.IsMe && ManaManager.Check("e-allies")) &&
                        GetEHits(target.ServerPosition).Item1 >= minHits)
                    {
                        E.CastOnUnit(target);
                    }
                }
                else
                {
                    if (GetEHits(Player.ServerPosition).Item1 >= (Ball.Position.CountEnemiesInRange(800) <= 2 ? 1 : 2))
                    {
                        E.CastOnUnit(Player);
                        return;
                    }
                    foreach (var ally in
                        GameObjects.AllyHeroes.Where(h => h.IsValidTarget(E.Range, false))
                            .Where(ally => ally.Position.CountEnemiesInRange(300) >= 2))
                    {
                        if ((ally.IsMe || Menu.Item(Menu.Name + ".miscellaneous.e-allies").GetValue<bool>()) &&
                            (ally.IsMe && ManaManager.Check("e-self") || !ally.IsMe && ManaManager.Check("e-allies")))
                        {
                            E.CastOnUnit(ally);
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private bool RLogic(int min)
        {
            try
            {
                var hits = GetHits(R);
                if (UltimateManager.Check(min, hits.Item2))
                {
                    R.Cast(Player.Position);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return false;
        }

        private bool RLogicDuel(bool q, bool w, bool e)
        {
            try
            {
                if (
                    GameObjects.EnemyHeroes.Where(t => UltimateManager.CheckDuel(t, CalcComboDamage(t, q, w, e, true)))
                        .Any(t => RLogic(1)))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return false;
        }

        private Tuple<int, List<Obj_AI_Hero>> GetHits(Spell spell)
        {
            try
            {
                var hits = new List<Obj_AI_Hero>();
                foreach (var enemy in
                    GameObjects.EnemyHeroes.Where(
                        h => h.IsValidTarget() && Ball.Position.Distance(h.Position, true) < spell.Range * spell.Range)
                        .Where(
                            enemy =>
                                spell.WillHit(enemy, Ball.Position) &&
                                Ball.Position.Distance(enemy.ServerPosition, true) < spell.Width * spell.Width))
                {
                    if (enemy.IsDashing())
                    {
                        if (enemy.Position.Distance(Ball.Position) >
                            enemy.GetDashInfo().EndPos.Distance(Ball.Position) - 50f)
                        {
                            hits.Add(enemy);
                        }
                    }
                    else
                    {
                        hits.Add(enemy);
                    }
                }
                return new Tuple<int, List<Obj_AI_Hero>>(hits.Count, hits);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return new Tuple<int, List<Obj_AI_Hero>>(0, null);
        }

        private Tuple<int, List<Obj_AI_Hero>> GetEHits(Vector3 to)
        {
            try
            {
                var hits = new List<Obj_AI_Hero>();
                foreach (var enemy in GameObjects.EnemyHeroes.Where(h => h.IsValidTarget(2000)))
                {
                    var pos = Ball.Position.Extend(enemy.Position, E.Width * 0.3f);
                    E.UpdateSourcePosition(pos, pos);
                    if (E.WillHit(enemy, to))
                    {
                        hits.Add(enemy);
                    }
                    E.UpdateSourcePosition(Ball.Position, Ball.Position);
                }
                return new Tuple<int, List<Obj_AI_Hero>>(hits.Count, hits);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return new Tuple<int, List<Obj_AI_Hero>>(0, null);
        }

        private Vector3 AssistedQLogic(out int hits)
        {
            try
            {
                if (Ball.IsMoving)
                {
                    hits = 0;
                    return Vector3.Zero;
                }
                var center = Vector2.Zero;
                float radius = -1;
                var count = 0;
                var range = (Q.Range + R.Width) * 1.5f;
                var input = new PredictionInput
                {
                    Collision = false,
                    From = Ball.Position,
                    RangeCheckFrom = Ball.Position,
                    Delay = (Q.Delay + R.Delay) - 0.1f,
                    Range = Q.Range + R.Width / 2f,
                    Speed = Q.Speed,
                    Radius = R.Width,
                    Type = R.Type
                };
                var points = new List<Vector2>();
                foreach (var enemy in GameObjects.EnemyHeroes.Where(t => t.IsValidTarget(range)))
                {
                    input.Unit = enemy;
                    var pred = Prediction.GetPrediction(input);
                    if (pred.Hitchance >= HitChance.Low)
                    {
                        points.Add(pred.UnitPosition.To2D());
                    }
                }
                if (points.Any())
                {
                    var possibilities = ListExtensions.ProduceEnumeration(points).Where(p => p.Count > 1).ToList();
                    if (possibilities.Any())
                    {
                        foreach (var possibility in possibilities)
                        {
                            var mec = MEC.GetMec(possibility);
                            if (mec.Radius < R.Width && Player.Distance(mec.Center) < range)
                            {
                                if (possibility.Count > count || possibility.Count == count && mec.Radius < radius)
                                {
                                    center = mec.Center;
                                    radius = mec.Radius;
                                    count = possibility.Count;
                                }
                            }
                        }
                        if (!center.Equals(Vector2.Zero))
                        {
                            hits = count;
                            return center.To3D();
                        }
                    }
                    var dTarget = GameObjects.EnemyHeroes.FirstOrDefault(t => t.IsValidTarget(range));
                    if (dTarget != null)
                    {
                        hits = 1;
                        return dTarget.Position;
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            hits = 0;
            return Vector3.Zero;
        }

        private void EComboLogic(Spell spell)
        {
            try
            {
                Obj_AI_Hero hero = null;
                var totalHits = 0;
                foreach (var ally in
                    GameObjects.AllyHeroes.Where(
                        a =>
                            (Ball.Hero == null || Ball.Hero.NetworkId != a.NetworkId) && a.Distance(Player) <= E.Range &&
                            (a.IsMe || Menu.Item(Menu.Name + ".miscellaneous.e-allies").GetValue<bool>()) &&
                            (a.IsMe && ManaManager.Check("e-self") || !a.IsMe && ManaManager.Check("e-allies"))))
                {
                    var hits = GameObjects.EnemyHeroes.Count(e => e.Distance(ally) < spell.Range);
                    if (hits > totalHits)
                    {
                        totalHits = hits;
                        hero = ally;
                    }
                }
                if (totalHits > 0)
                {
                    E.CastOnUnit(hero);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private Tuple<int, Vector3> GetBestQLocation(Obj_AI_Hero target, HitChance hitChance)
        {
            try
            {
                if (target == null)
                {
                    return new Tuple<int, Vector3>(0, Vector3.Zero);
                }
                var pred = Q.GetPrediction(target);
                if (pred.Hitchance < hitChance)
                {
                    return new Tuple<int, Vector3>(0, Vector3.Zero);
                }
                var points =
                    (from enemy in GameObjects.EnemyHeroes.Where(h => h.IsValidTarget((Q.Range + R.Range * 1.2f)))
                        select Q.GetPrediction(enemy)
                        into ePred
                        where ePred.Hitchance >= (hitChance - 1)
                        select ePred.UnitPosition.To2D()).ToList();
                if (points.Any())
                {
                    var possibilities = ListExtensions.ProduceEnumeration(points).Where(p => p.Count > 0).ToList();
                    if (possibilities.Any())
                    {
                        var hits = 0;
                        var radius = float.MaxValue;
                        var pos = Vector3.Zero;
                        var rReady = R.IsReady();
                        var wReady = R.IsReady();
                        foreach (var possibility in possibilities)
                        {
                            var check = false;
                            var mec = MEC.GetMec(possibility);
                            if (mec.Radius < R.Range * 0.85f && possibility.Count >= 3 && rReady)
                            {
                                check = true;
                            }
                            if (mec.Radius < W.Range * 0.9f && points.Count >= 2 && wReady)
                            {
                                check = true;
                            }
                            if (mec.Radius < Q.Width * 0.9f && points.Count >= 1)
                            {
                                check = true;
                            }
                            if (check && possibility.Count > hits || possibility.Count == hits && radius > mec.Radius)
                            {
                                hits = possibility.Count;
                                radius = mec.Radius;
                                pos = mec.Center.To3D();
                            }
                        }
                        return new Tuple<int, Vector3>(hits, pos);
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return new Tuple<int, Vector3>(0, Vector3.Zero);
        }

        protected override void LaneClear()
        {
            if (!ManaManager.Check("lane-clear") || Ball.IsMoving)
            {
                return;
            }
            var q = Menu.Item(Menu.Name + ".lane-clear.q").GetValue<bool>();
            var w = Menu.Item(Menu.Name + ".lane-clear.w").GetValue<bool>();

            if (!q && !w)
            {
                return;
            }

            var mobs = MinionManager.GetMinions(
                Player.Position, Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            if (mobs.Count > 0)
            {
                var mob = mobs.First();
                if (w && W.IsReady() && W.WillHit(mob.ServerPosition, Ball.Position))
                {
                    W.Cast(Player.Position);
                }
                else if (q && Q.IsReady())
                {
                    Q.Cast(mob.Position);
                }
                return;
            }
            var allMinions = MinionManager.GetMinions(Player.Position, Q.Range + W.Width);
            var rangedMinions = MinionManager.GetMinions(Player.Position, Q.Range + W.Width, MinionTypes.Ranged);

            if (q && Q.IsReady())
            {
                if (w)
                {
                    var qLocation = Q.GetCircularFarmLocation(allMinions, W.Range);
                    var q2Location = Q.GetCircularFarmLocation(rangedMinions, W.Range);
                    var bestLocation = (qLocation.MinionsHit > q2Location.MinionsHit + 1) ? qLocation : q2Location;

                    if (bestLocation.MinionsHit > 0)
                    {
                        Q.Cast(bestLocation.Position);
                        return;
                    }
                }
                else
                {
                    foreach (var minion in allMinions.FindAll(m => !Orbwalking.InAutoAttackRange(m)))
                    {
                        if (
                            HealthPrediction.GetHealthPrediction(
                                minion,
                                Math.Max((int) (minion.Position.Distance(Ball.Position) / Q.Speed * 1000) - 100, 0)) <
                            50)
                        {
                            Q.Cast(minion.Position);
                            return;
                        }
                    }
                }
            }
            if (w && W.IsReady())
            {
                if (allMinions.Where(m => m.Distance(Ball.Position) <= W.Range).Count(m => W.GetDamage(m) > m.Health) >=
                    3)
                {
                    W.Cast(Player.Position);
                }
            }
        }

        protected override void Flee()
        {
            if (Menu.Item(Menu.Name + ".flee.w").GetValue<bool>() &&
                (Ball.Status == BallStatus.Me || Ball.Position.Distance(Player.Position) <= W.Width * 0.8f) &&
                W.IsReady())
            {
                W.Cast(Player.Position);
            }
            if (Menu.Item(Menu.Name + ".flee.e").GetValue<bool>() && E.IsReady() &&
                (Ball.Status != BallStatus.Me || Player.CountEnemiesInRange(500) > 0))
            {
                E.CastOnUnit(Player);
            }
        }

        protected override void Killsteal() {}

        internal enum BallStatus
        {
            Me,
            Ally,
            Fixed
        }

        internal class Ball
        {
            private static Vector3 _pos;
            private static Obj_AI_Hero _hero;

            static Ball()
            {
                Pos = ObjectManager.Player.Position;
                GameObject.OnCreate += OnGameObjectCreate;
                Obj_AI_Base.OnProcessSpellCast += OnObjAiBaseProcessSpellCast;
                Core.OnPreUpdate += OnCorePreUpdate;

                foreach (var obj in GameObjects.AllGameObjects)
                {
                    OnGameObjectCreate(obj, null);
                }
            }

            private static Vector3 Pos
            {
                get { return _pos; }
                set
                {
                    var tmp = Position;
                    _pos = value;
                    if (_pos != null && !_pos.Equals(tmp))
                    {
                        OnPositionChange.RaiseEvent(null, null);
                    }
                }
            }

            public static Obj_AI_Hero Hero
            {
                get { return _hero; }
                private set
                {
                    var tmp = Position;
                    _hero = value;
                    if (_hero != null && !_hero.Position.Equals(tmp))
                    {
                        OnPositionChange.RaiseEvent(null, null);
                    }
                }
            }

            public static BallStatus Status { get; private set; }

            public static Vector3 Position
            {
                get { return Hero != null ? Hero.ServerPosition : Pos; }
            }

            public static bool IsMoving { get; private set; }
            public static event EventHandler OnPositionChange;

            private static void OnCorePreUpdate(EventArgs args)
            {
                try
                {
                    if (ObjectManager.Player.HasBuff("OrianaGhostSelf"))
                    {
                        Status = BallStatus.Me;
                        Hero = ObjectManager.Player;
                        IsMoving = false;
                        return;
                    }
                    foreach (var hero in
                        GameObjects.AllyHeroes.Where(x => x.IsAlly && !x.IsDead && !x.IsMe)
                            .Where(hero => hero.HasBuff("OrianaGhost")))
                    {
                        Status = BallStatus.Ally;
                        Hero = hero;
                        IsMoving = false;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Global.Logger.AddItem(new LogItem(ex));
                }
            }

            private static void OnObjAiBaseProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
            {
                try
                {
                    if (!sender.IsMe)
                    {
                        return;
                    }
                    if (args.SData.Name.Equals("OrianaIzunaCommand", StringComparison.OrdinalIgnoreCase) ||
                        args.SData.Name.Equals("OrianaRedactCommand", StringComparison.OrdinalIgnoreCase))
                    {
                        IsMoving = true;
                    }
                }
                catch (Exception ex)
                {
                    Global.Logger.AddItem(new LogItem(ex));
                }
            }

            private static void OnGameObjectCreate(GameObject sender, EventArgs args)
            {
                try
                {
                    if (sender.IsValid && !string.IsNullOrEmpty(sender.Name) &&
                        sender.Name.Equals("Orianna_Base_Q_yomu_ring_green.troy", StringComparison.OrdinalIgnoreCase))
                    {
                        Hero = null;
                        Pos = sender.Position;
                        Status = BallStatus.Fixed;
                        IsMoving = false;
                    }
                }
                catch (Exception ex)
                {
                    Global.Logger.AddItem(new LogItem(ex));
                }
            }
        }
    }
}