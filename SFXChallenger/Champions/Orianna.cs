#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Orianna.cs is part of SFXChallenger.

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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXChallenger.Abstracts;
using SFXChallenger.Enumerations;
using SFXChallenger.Events;
using SFXChallenger.Helpers;
using SFXChallenger.Library;
using SFXChallenger.Library.Extensions.NET;
using SFXChallenger.Library.Logger;
using SFXChallenger.Managers;
using SharpDX;
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
    internal class Orianna : Champion
    {
        private readonly float _maxBallDistance = 1300f;
        private MenuItem _ballPositionCircle;
        private MenuItem _ballPositionRadius;
        private MenuItem _ballPositionThickness;

        protected override ItemFlags ItemFlags
        {
            get { return ItemFlags.Offensive | ItemFlags.Defensive | ItemFlags.Flee; }
        }

        protected override ItemUsageType ItemUsage
        {
            get { return ItemUsageType.Custom; }
        }

        protected override void OnLoad()
        {
            Interrupter2.OnInterruptableTarget += OnInterruptableTarget;
            InitiatorManager.OnAllyInitiator += OnAllyInitiator;
            Spellbook.OnCastSpell += OnSpellbookCastSpell;
            Ball.OnPositionChange += OnBallPositionChange;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            CustomEvents.Unit.OnDash += OnUnitDash;
            Drawing.OnDraw += OnDrawingDraw;
            Obj_AI_Base.OnProcessSpellCast += OnObjAiBaseProcessSpellCast;
        }

        protected override void AddToMenu()
        {
            var ultimateMenu = UltimateManager.AddToMenu(Menu, true, true, false, false, false, true, true, true, true);

            ultimateMenu.AddItem(
                new MenuItem(ultimateMenu.Name + ".width", "Width").SetValue(new Slider((int) R.Width, 250, 400)))
                .ValueChanged += delegate(object sender, OnValueChangeEventArgs args)
                {
                    R.Width = args.GetNewValue<Slider>().Value;
                    DrawingManager.Update("R Flash", args.GetNewValue<Slider>().Value + SummonerManager.Flash.Range);
                };

            var comboMenu = Menu.AddSubMenu(new Menu("Combo", Menu.Name + ".combo"));
            HitchanceManager.AddToMenu(
                comboMenu.AddSubMenu(new Menu("Hitchance", comboMenu.Name + ".hitchance")), "combo",
                new Dictionary<string, HitChance> { { "Q", HitChance.VeryHigh } });
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".q", "Use Q").SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".w", "Use W").SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".e", "Use E").SetValue(true));

            var harassMenu = Menu.AddSubMenu(new Menu("Harass", Menu.Name + ".harass"));
            HitchanceManager.AddToMenu(
                harassMenu.AddSubMenu(new Menu("Hitchance", harassMenu.Name + ".hitchance")), "harass",
                new Dictionary<string, HitChance> { { "Q", HitChance.VeryHigh } });
            ManaManager.AddToMenu(harassMenu, "harass-q", ManaCheckType.Minimum, ManaValueType.Percent, "Q");
            ManaManager.AddToMenu(harassMenu, "harass-w", ManaCheckType.Minimum, ManaValueType.Percent, "W", 50);
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".q", "Use Q").SetValue(true));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".w", "Use W").SetValue(true));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".e", "Use E").SetValue(false));

            var laneclearMenu = Menu.AddSubMenu(new Menu("Lane Clear", Menu.Name + ".lane-clear"));
            ManaManager.AddToMenu(laneclearMenu, "lane-clear", ManaCheckType.Minimum, ManaValueType.Percent, null, 25);
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".q", "Use Q").SetValue(true));
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".w", "Use W").SetValue(true));

            var fleeMenu = Menu.AddSubMenu(new Menu("Flee", Menu.Name + ".flee"));
            fleeMenu.AddItem(new MenuItem(fleeMenu.Name + ".w", "Use W").SetValue(true));
            fleeMenu.AddItem(new MenuItem(fleeMenu.Name + ".e", "Use E").SetValue(true));

            var initiatorMenu = Menu.AddSubMenu(new Menu("Initiator", Menu.Name + ".initiator"));
            var initiatorWhitelistMenu =
                initiatorMenu.AddSubMenu(new Menu("Whitelist", initiatorMenu.Name + ".whitelist"));
            initiatorWhitelistMenu.Color = HeroListManager.WhitelistColor;
            InitiatorManager.AddToMenu(initiatorWhitelistMenu, true, false);
            initiatorMenu.AddItem(new MenuItem(initiatorMenu.Name + ".use-e", "Use E").SetValue(true));

            var shieldMenu = Menu.AddSubMenu(new Menu("Shield", Menu.Name + ".shield"));
            ManaManager.AddToMenu(shieldMenu, "shield", ManaCheckType.Minimum, ManaValueType.Percent);
            shieldMenu.AddItem(
                new MenuItem(shieldMenu.Name + ".min-health", "Min. Health %").SetValue(new Slider(90, 1)));
            shieldMenu.AddItem(
                new MenuItem(shieldMenu.Name + ".min-damage", "Min. Damage % Incoming").SetValue(new Slider(10, 1)));
            shieldMenu.AddItem(new MenuItem(shieldMenu.Name + ".enabled", "Enabled").SetValue(true));

            var miscMenu = Menu.AddSubMenu(new Menu("Misc", Menu.Name + ".miscellaneous"));
            HeroListManager.AddToMenu(
                miscMenu.AddSubMenu(new Menu("Q Gapcloser", miscMenu.Name + "q-gapcloser")), "q-gapcloser", false, false,
                true, false);
            ManaManager.AddToMenu(miscMenu, "e-self", ManaCheckType.Minimum, ManaValueType.Percent, "E Self", 10);
            ManaManager.AddToMenu(miscMenu, "e-allies", ManaCheckType.Minimum, ManaValueType.Percent, "E Allies", 20);
            miscMenu.AddItem(new MenuItem(miscMenu.Name + ".e-allies", "E Allies").SetValue(true));
            miscMenu.AddItem(new MenuItem(miscMenu.Name + ".block-r", "Block Missing R").SetValue(true));

            DrawingManager.Add("R Flash", R.Width + SummonerManager.Flash.Range);

            IndicatorManager.AddToMenu(DrawingManager.Menu, true);
            IndicatorManager.Add(Q);
            IndicatorManager.Add(W);
            IndicatorManager.Add(E);
            IndicatorManager.Add(R);
            IndicatorManager.Finale();

            _ballPositionThickness = DrawingManager.Add("Ball Thickness", new Slider(7, 1, 10));
            _ballPositionRadius = DrawingManager.Add("Ball Radius", new Slider(95, 0, 300));
            _ballPositionCircle = DrawingManager.Add("Ball Position", new Circle(false, Color.Yellow));

            R.Width = Menu.Item(ultimateMenu.Name + ".width").GetValue<Slider>().Value;
        }

        private void CastE(Obj_AI_Hero target)
        {
            if (target != null &&
                (target.IsMe && ManaManager.Check("e-self") ||
                 !target.IsMe && Menu.Item(Menu.Name + ".miscellaneous.e-allies").GetValue<bool>() &&
                 ManaManager.Check("e-allies")))
            {
                E.CastOnUnit(target);
            }
        }

        private void OnObjAiBaseProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            try
            {
                if (!sender.IsEnemy || E.Level == 0 || !Menu.Item(Menu.Name + ".shield.enabled").GetValue<bool>())
                {
                    return;
                }
                var hero = sender as Obj_AI_Hero;
                if (hero != null)
                {
                    if (args.Target != null && args.Target.NetworkId == Player.NetworkId && args.SData.IsAutoAttack())
                    {
                        IncomingDamage.Add(
                            Player.ServerPosition.Distance(hero.ServerPosition) / args.SData.MissileSpeed + Game.Time,
                            (float) hero.GetAutoAttackDamage(Player));
                    }
                    else
                    {
                        var slot = hero.GetSpellSlot(args.SData.Name);
                        if (slot != SpellSlot.Unknown)
                        {
                            if (args.Target != null && args.Target.NetworkId == Player.NetworkId &&
                                slot == hero.GetSpellSlot("SummonerDot"))
                            {
                                if (E.IsReady() && ManaManager.Check("shield"))
                                {
                                    E.CastOnUnit(Player);
                                }
                                IncomingDamage.Add(
                                    Game.Time + 2,
                                    (float) hero.GetSummonerSpellDamage(Player, Damage.SummonerSpell.Ignite));
                            }
                            else if ((slot == SpellSlot.Q || slot == SpellSlot.W || slot == SpellSlot.E ||
                                      slot == SpellSlot.R) &&
                                     ((args.Target != null && args.Target.NetworkId == Player.NetworkId)))
                            {
                                var time = args.SData.CastFrame / 30f +
                                           Player.Distance(hero) / args.SData.MissileSpeed;
                                IncomingDamage.Add(
                                    Game.Time + (time > 2f ? (time < 5f ? time : 5f) : 2f),
                                    (float) hero.GetSpellDamage(Player, slot));
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

        private void OnDrawingDraw(EventArgs args)
        {
            try
            {
                var circle = _ballPositionCircle.GetValue<Circle>();
                if (circle.Active)
                {
                    Render.Circle.DrawCircle(
                        (Ball.Hero != null ? Ball.Hero.Position : Ball.Position),
                        _ballPositionRadius.GetValue<Slider>().Value, circle.Color,
                        _ballPositionThickness.GetValue<Slider>().Value, true);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnUnitDash(Obj_AI_Base sender, Dash.DashItem args)
        {
            try
            {
                var hero = sender as Obj_AI_Hero;
                if (!sender.IsEnemy || hero == null)
                {
                    return;
                }
                var endTick = Game.Time - Game.Ping / 2000f + (args.EndPos.Distance(args.StartPos) / args.Speed);
                if (HeroListManager.Check("q-gapcloser", hero) && Ball.Position.Distance(args.EndPos.To3D()) <= Q.Range &&
                    Q.IsReady())
                {
                    var target = TargetSelector.GetTarget(Q.Range * 0.85f, Q.DamageType);
                    if (target == null || sender.NetworkId.Equals(target.NetworkId))
                    {
                        var delay = (int) (endTick - Game.Time - Q.Delay - 0.1f);
                        if (delay > 0)
                        {
                            Utility.DelayAction.Add(delay * 1000, () => Q.Cast(args.EndPos));
                        }
                        else
                        {
                            Q.Cast(args.EndPos);
                        }
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
                if (HeroListManager.Check("q-gapcloser", args.Sender) && Ball.Position.Distance(args.End) <= Q.Range &&
                    Q.IsReady())
                {
                    var target = TargetSelector.GetTarget(Q.Range * 0.85f, Q.DamageType);
                    if (target == null || args.Sender.NetworkId.Equals(target.NetworkId))
                    {
                        Q.Cast(args.End);
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnSpellbookCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            try
            {
                if (sender.Owner.IsMe && args.Slot == SpellSlot.R)
                {
                    if (Ball.IsMoving || Menu.Item(Menu.Name + ".miscellaneous.block-r").GetValue<bool>())
                    {
                        args.Process = GetHits(R, 400f).Item1 > 0;
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
            Q = new Spell(SpellSlot.Q, 825f, DamageType.Magical);
            Q.SetSkillshot(0.15f, 110f, 1375f, false, SkillshotType.SkillshotCircle);

            W = new Spell(SpellSlot.W, float.MaxValue, DamageType.Magical);
            W.SetSkillshot(0f, 230f, float.MaxValue, false, SkillshotType.SkillshotCircle);

            E = new Spell(SpellSlot.E, 1095f, DamageType.Magical);
            E.SetSkillshot(0.25f, 125f, 1700f, false, SkillshotType.SkillshotLine);

            R = new Spell(SpellSlot.R, float.MaxValue, DamageType.Magical);
            R.SetSkillshot(0.75f, 375f, float.MaxValue, false, SkillshotType.SkillshotCircle);
        }

        protected override void OnPreUpdate()
        {
            if (Ball.IsMoving)
            {
                return;
            }
            if (Menu.Item(Menu.Name + ".shield.enabled").GetValue<bool>() && E.IsReady() && ManaManager.Check("shield") &&
                !Player.InFountain() && !Player.IsRecalling())
            {
                if (Player.HealthPercent <= Menu.Item(Menu.Name + ".shield.min-health").GetValue<Slider>().Value)
                {
                    IncomingDamage.Clean();
                    var totalDamage = IncomingDamage.TotalDamage * 1.1f;
                    if (totalDamage >= Player.Health ||
                        (totalDamage / Player.MaxHealth * 100) >=
                        Menu.Item(Menu.Name + ".shield.min-damage").GetValue<Slider>().Value)
                    {
                        E.CastOnUnit(Player);
                    }
                }
            }
        }

        protected override void OnPostUpdate()
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
                    (R.Width + SummonerManager.Flash.Range) * 1.5f, DamageType.Magical);
                if (target != null && !target.IsDashing() &&
                    (Prediction.GetPrediction(target, R.Delay + 0.3f).UnitPosition.Distance(Player.Position)) > R.Width)
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
                                Delay = R.Delay,
                                Range = R.Range,
                                Speed = R.Speed,
                                Radius = R.Width,
                                Type = R.Type,
                                Unit = target
                            });
                    if (pred.Hitchance >= HitChance.High)
                    {
                        R.UpdateSourcePosition(flashPos, flashPos);
                        var hits = GameObjects.EnemyHeroes.Where(x => R.WillHit(x, pred.CastPosition)).ToList();
                        if (UltimateManager.Check(
                            UltimateModeType.Combo, min, hits,
                            hero =>
                                CalcComboDamage(
                                    hero, Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady(),
                                    Menu.Item(Menu.Name + ".combo.w").GetValue<bool>() && W.IsReady(),
                                    Menu.Item(Menu.Name + ".combo.e").GetValue<bool>() && E.IsReady(), true)))
                        {
                            if (R.Cast(Ball.Position))
                            {
                                Utility.DelayAction.Add(300, () => SummonerManager.Flash.Cast(flashPos));
                            }
                        }
                        else if (Menu.Item(Menu.Name + ".ultimate.flash.single").GetValue<bool>())
                        {
                            if (UltimateManager.Check(
                                UltimateModeType.Combo, 1, hits,
                                hero =>
                                    CalcComboDamage(
                                        hero, Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady(),
                                        Menu.Item(Menu.Name + ".combo.w").GetValue<bool>() && W.IsReady(),
                                        Menu.Item(Menu.Name + ".combo.e").GetValue<bool>() && E.IsReady(), true)))
                            {
                                var cDmg = CalcComboDamage(
                                    target, Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady(),
                                    Menu.Item(Menu.Name + ".combo.w").GetValue<bool>() && W.IsReady(),
                                    Menu.Item(Menu.Name + ".combo.e").GetValue<bool>() && E.IsReady(), true);
                                if (cDmg - 20 >= target.Health)
                                {
                                    if (R.Cast(Ball.Position))
                                    {
                                        Utility.DelayAction.Add(300, () => SummonerManager.Flash.Cast(flashPos));
                                    }
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
                if (
                    !RLogic(
                        Menu.Item(Menu.Name + ".ultimate.assisted.min").GetValue<Slider>().Value,
                        Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady(),
                        Menu.Item(Menu.Name + ".combo.w").GetValue<bool>() && W.IsReady(),
                        Menu.Item(Menu.Name + ".combo.e").GetValue<bool>() && E.IsReady()))
                {
                    var casted = false;
                    if (Menu.Item(Menu.Name + ".ultimate.assisted.single").GetValue<bool>())
                    {
                        casted = RLogicSingle(
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
                if (
                    !RLogic(
                        Menu.Item(Menu.Name + ".ultimate.auto.min").GetValue<Slider>().Value,
                        Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady(),
                        Menu.Item(Menu.Name + ".combo.w").GetValue<bool>() && W.IsReady(),
                        Menu.Item(Menu.Name + ".combo.e").GetValue<bool>() && E.IsReady(), UltimateModeType.Auto))
                {
                    if (Menu.Item(Menu.Name + ".ultimate.auto.single").GetValue<bool>())
                    {
                        RLogicSingle(
                            Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady(),
                            Menu.Item(Menu.Name + ".combo.w").GetValue<bool>() && W.IsReady(),
                            Menu.Item(Menu.Name + ".combo.e").GetValue<bool>() && E.IsReady());
                    }
                }
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
            var target = TargetSelector.GetTarget(Q);
            if (w && W.IsReady())
            {
                WLogic(1);
            }
            if (r && R.IsReady())
            {
                if (
                    !RLogic(
                        Menu.Item(Menu.Name + ".ultimate.combo.min").GetValue<Slider>().Value, q && Q.IsReady(),
                        w && W.IsReady(), e && E.IsReady()))
                {
                    if (Menu.Item(Menu.Name + ".ultimate.combo.single").GetValue<bool>())
                    {
                        RLogicSingle(q, w, e);
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
            if (Ball.IsMoving)
            {
                return;
            }
            var q = Menu.Item(Menu.Name + ".harass.q").GetValue<bool>() && ManaManager.Check("harass-q");
            var w = Menu.Item(Menu.Name + ".harass.w").GetValue<bool>() && ManaManager.Check("harass-w");
            var e = Menu.Item(Menu.Name + ".harass.e").GetValue<bool>();
            if (w && W.IsReady())
            {
                WLogic(1);
            }
            if (q && Q.IsReady())
            {
                QLogic(TargetSelector.GetTarget(Q), Q.GetHitChance("combo"), e);
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
                if (e)
                {
                    damage += E.GetDamage(target) * 0.75f;
                }
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
                    if (eqTarget != null && bestEqTravelTime < directTravelTime * 1.3f &&
                        (Ball.Position.Distance(eqTarget.ServerPosition, true) > 10000))
                    {
                        CastE(eqTarget);
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
                    W.Cast(Ball.Position);
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

                if (!Q.IsReady() && W.Instance.CooldownExpires < Q.Instance.CooldownExpires)
                {
                    var e1 =
                        GameObjects.EnemyHeroes.Where(
                            e => e.IsValidTarget() && e.Distance(Player.Position) < W.Width * 1.5f).ToList();
                    var e2 =
                        GameObjects.EnemyHeroes.Where(
                            e => e.IsValidTarget() && e.Distance(Ball.Position) < W.Width * 1.5f).ToList();
                    if ((e2.Count > e1.Count || e2.Count == e1.Count))
                    {
                        return;
                    }
                }

                if (Utility.CountEnemiesInRange((int) (Q.Range + R.Width)) <= 1)
                {
                    foreach (var ally in GameObjects.AllyHeroes.Where(h => h.IsValidTarget(E.Range, false)))
                    {
                        if (ally.Position.CountEnemiesInRange(300) >= 1)
                        {
                            CastE(ally);
                            return;
                        }
                        target = ally;
                    }
                    if (target == null)
                    {
                        target = Player;
                    }
                    if (GetEHits(target.ServerPosition).Item1 >= minHits)
                    {
                        CastE(target);
                    }
                }
                else
                {
                    if (GetEHits(Player.ServerPosition).Item1 >= (Ball.Position.CountEnemiesInRange(800) <= 2 ? 1 : 2))
                    {
                        CastE(Player);
                        return;
                    }
                    foreach (var ally in
                        GameObjects.AllyHeroes.Where(h => h.IsValidTarget(E.Range, false))
                            .Where(ally => ally.Position.CountEnemiesInRange(300) >= 2))
                    {
                        CastE(ally);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private bool RLogic(int min, bool q, bool w, bool e, UltimateModeType mode = UltimateModeType.Combo)
        {
            try
            {
                var hits = GetHits(R);
                if (UltimateManager.Check(mode, min, hits.Item2, hero => CalcComboDamage(hero, q, w, e, true)))
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

        private bool RLogicSingle(bool q, bool w, bool e)
        {
            try
            {
                if (
                    GameObjects.EnemyHeroes.Where(
                        t => UltimateManager.CheckSingle(t, CalcComboDamage(t, q, w, e, true)))
                        .Any(t => RLogic(1, q, w, e)))
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

        private Tuple<int, List<Obj_AI_Hero>> GetHits(Spell spell, float overrideWidth = -1f)
        {
            try
            {
                var width = overrideWidth > 0 ? overrideWidth : spell.Width;
                var hits = new List<Obj_AI_Hero>();
                var positions = (from t in GameObjects.EnemyHeroes
                    where t.IsValidTarget(width * 4, true, spell.RangeCheckFrom)
                    let prediction = spell.GetPrediction(t)
                    where prediction.Hitchance >= HitChance.High
                    where
                        Utils.IsImmobile(t) || Utils.IsSlowed(t) || t.Distance(Ball.Position) < spell.Width * 0.75 ||
                        t.Distance(Ball.Position) < spell.Width && t.IsFacing(Ball.Position, 120f)
                    select new CPrediction.Position(t, prediction.UnitPosition)).ToList();
                if (positions.Any())
                {
                    var circle = new Geometry.Polygon.Circle(Ball.Position, width);
                    hits.AddRange(
                        from position in positions
                        where
                            !position.Hero.IsDashing() ||
                            (position.Hero.Distance(Ball.Position) >= 100f &&
                             position.Hero.Position.Distance(Ball.Position) >
                             position.Hero.GetDashInfo().EndPos.Distance(Ball.Position) - 50f)
                        where circle.IsInside(position.UnitPosition)
                        select position.Hero);
                    return new Tuple<int, List<Obj_AI_Hero>>(hits.Count, hits);
                }
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
                    CastE(hero);
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
                var hits = new List<Obj_AI_Hero>();
                var center = Vector3.Zero;
                var radius = float.MaxValue;
                var range = Q.Range + Q.Width + target.BoundingRadius * 0.85f;
                var positions = (from t in GameObjects.EnemyHeroes
                    where t.IsValidTarget(range, true, Q.RangeCheckFrom)
                    let prediction = Q.GetPrediction(t)
                    where prediction.Hitchance >= (hitChance - 1)
                    select new CPrediction.Position(t, prediction.UnitPosition)).ToList();
                if (positions.Any())
                {
                    var mainTarget = positions.FirstOrDefault(p => p.Hero.NetworkId == target.NetworkId);
                    var possibilities =
                        ListExtensions.ProduceEnumeration(
                            positions.Where(p => p.UnitPosition.Distance(mainTarget.UnitPosition) <= Q.Width * 0.85f)
                                .ToList())
                            .Where(p => p.Count > 0 && p.Any(t => t.Hero.NetworkId == mainTarget.Hero.NetworkId))
                            .ToList();
                    var rReady = R.IsReady();
                    var wReady = W.IsReady();
                    foreach (var possibility in possibilities)
                    {
                        var mec = MEC.GetMec(possibility.Select(p => p.UnitPosition.To2D()).ToList());
                        var distance = Q.From.Distance(mec.Center.To3D());
                        if (distance < range)
                        {
                            if (mec.Radius < R.Width * 0.85f && possibility.Count >= 3 && rReady ||
                                mec.Radius < W.Width * 0.9f && possibility.Count >= 2 && wReady ||
                                mec.Radius < Q.Width * 0.9f && possibility.Count >= 1)
                            {
                                var lHits = new List<Obj_AI_Hero>();
                                var circle =
                                    new Geometry.Polygon.Circle(
                                        Q.From.Extend(mec.Center.To3D(), Q.Range > distance ? distance : Q.Range),
                                        Q.Width);

                                lHits.AddRange(
                                    (from position in positions
                                        where
                                            new Geometry.Polygon.Circle(
                                                position.UnitPosition, (position.Hero.BoundingRadius * 0.85f)).Points
                                                .Any(p => circle.IsInside(p))
                                        select position.Hero));

                                if ((lHits.Count > hits.Count || lHits.Count == hits.Count && mec.Radius < radius ||
                                     lHits.Count == hits.Count &&
                                     Q.From.Distance(circle.Center.To3D()) < Q.From.Distance(center)) &&
                                    lHits.Any(p => p.NetworkId == target.NetworkId))
                                {
                                    center = circle.Center.To3D2();
                                    radius = mec.Radius;
                                    hits.Clear();
                                    hits.AddRange(lHits);
                                }
                            }
                        }
                    }
                    if (!center.Equals(Vector3.Zero))
                    {
                        return new Tuple<int, Vector3>(hits.Count, center);
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
                    var qLocation = Q.GetCircularFarmLocation(allMinions, W.Width);
                    var q2Location = Q.GetCircularFarmLocation(rangedMinions, W.Width);
                    var bestLocation = (qLocation.MinionsHit > q2Location.MinionsHit + 1) ? qLocation : q2Location;

                    if (bestLocation.MinionsHit > 0 && bestLocation.Position.Distance(Ball.Position) > 25)
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
                if (allMinions.Where(m => m.Distance(Ball.Position) <= W.Width).Count(m => W.GetDamage(m) > m.Health) >=
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
                CastE(Player);
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
                Status = BallStatus.Fixed;
                Pos = ObjectManager.Player.Position;
                GameObject.OnCreate += OnGameObjectCreate;
                Obj_AI_Base.OnProcessSpellCast += OnObjAiBaseProcessSpellCast;
                Game.OnUpdate += OnGameUpdate;

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
                        try
                        {
                            OnPositionChange.RaiseEvent(null, null);
                        }
                        catch
                        {
                            //ignored 
                        }
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

            private static void OnGameUpdate(EventArgs args)
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

        internal class IncomingDamage
        {
            private static readonly ConcurrentDictionary<float, float> Damages =
                new ConcurrentDictionary<float, float>();

            public static float TotalDamage
            {
                get
                {
                    try
                    {
                        return Damages.Where(e => e.Key >= Game.Time).Select(e => e.Value).DefaultIfEmpty(0).Sum();
                    }
                    catch (Exception ex)
                    {
                        Global.Logger.AddItem(new LogItem(ex));
                    }
                    return 0;
                }
            }

            public static void Clean()
            {
                try
                {
                    var damages = Damages.Where(entry => entry.Key < Game.Time).ToArray();
                    foreach (var entry in damages)
                    {
                        float old;
                        Damages.TryRemove(entry.Key, out old);
                    }
                }
                catch (Exception ex)
                {
                    Global.Logger.AddItem(new LogItem(ex));
                }
            }

            public static void Add(float time, float damage)
            {
                try
                {
                    float value;
                    if (Damages.TryGetValue(time, out value))
                    {
                        Damages[time] = value + damage;
                    }
                    else
                    {
                        Damages[time] = damage;
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