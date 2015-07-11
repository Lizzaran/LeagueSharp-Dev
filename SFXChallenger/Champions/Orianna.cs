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
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXChallenger.Abstracts;
using SFXChallenger.Enumerations;
using SFXChallenger.Events;
using SFXChallenger.Helpers;
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
        private readonly float _maxBallDistance = 1200f;

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
        }

        protected override void AddToMenu()
        {
            var comboMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Combo"), Menu.Name + ".combo"));
            HitchanceManager.AddToMenu(
                comboMenu.AddSubMenu(new Menu(Global.Lang.Get("F_MH"), comboMenu.Name + ".hitchance")), "combo",
                new Dictionary<string, int> { { "Q", 2 }, { "W", 1 }, { "E", 2 }, { "R", 2 } });
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".w", Global.Lang.Get("G_UseW")).SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(true));

            var harassMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Harass"), Menu.Name + ".harass"));
            HitchanceManager.AddToMenu(
                harassMenu.AddSubMenu(new Menu(Global.Lang.Get("F_MH"), harassMenu.Name + ".hitchance")), "harass",
                new Dictionary<string, int> { { "Q", 2 }, { "W", 1 }, { "E", 2 } });
            ManaManager.AddToMenu(harassMenu, "harass", ManaCheckType.Minimum, ManaValueType.Percent);
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".w", Global.Lang.Get("G_UseW")).SetValue(true));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(true));

            var laneclearMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_LaneClear"), Menu.Name + ".lane-clear"));
            ManaManager.AddToMenu(laneclearMenu, "lane-clear", ManaCheckType.Minimum, ManaValueType.Percent);
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".w", Global.Lang.Get("G_UseW")).SetValue(true));
            laneclearMenu.AddItem(
                new MenuItem(laneclearMenu.Name + ".min", Global.Lang.Get("G_Min")).SetValue(new Slider(3, 1, 5)));

            var ultimateMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Ultimate"), Menu.Name + ".ultimate"));

            var uComboMenu = ultimateMenu.AddSubMenu(new Menu(Global.Lang.Get("G_Combo"), ultimateMenu.Name + ".combo"));
            uComboMenu.AddItem(
                new MenuItem(uComboMenu.Name + ".min", "R " + Global.Lang.Get("G_Min")).SetValue(new Slider(3, 1, 5)));
            uComboMenu.AddItem(new MenuItem(uComboMenu.Name + ".1v1", "R 1v1").SetValue(true));
            uComboMenu.AddItem(new MenuItem(uComboMenu.Name + ".enabled", Global.Lang.Get("G_Enabled")).SetValue(true));

            var uAutoMenu = ultimateMenu.AddSubMenu(new Menu(Global.Lang.Get("G_Auto"), ultimateMenu.Name + ".auto"));

            var autoInterruptMenu =
                uAutoMenu.AddSubMenu(new Menu(Global.Lang.Get("G_InterruptSpell"), uAutoMenu.Name + ".interrupt"));
            foreach (var enemy in GameObjects.EnemyHeroes)
            {
                autoInterruptMenu.AddItem(
                    new MenuItem(autoInterruptMenu.Name + "." + enemy.ChampionName, enemy.ChampionName).SetValue(true));
            }

            uAutoMenu.AddItem(
                new MenuItem(uAutoMenu.Name + ".min", "R " + Global.Lang.Get("G_Min")).SetValue(new Slider(3, 1, 5)));
            uAutoMenu.AddItem(new MenuItem(uAutoMenu.Name + ".1v1", "R 1v1").SetValue(true));
            uAutoMenu.AddItem(new MenuItem(uAutoMenu.Name + ".enabled", Global.Lang.Get("G_Enabled")).SetValue(true));

            var uFlashMenu = ultimateMenu.AddSubMenu(new Menu(Global.Lang.Get("G_Flash"), ultimateMenu.Name + ".flash"));
            uFlashMenu.AddItem(
                new MenuItem(uFlashMenu.Name + ".min", "R " + Global.Lang.Get("G_Min")).SetValue(new Slider(3, 1, 5)));
            uFlashMenu.AddItem(new MenuItem(uFlashMenu.Name + ".1v1", "R 1v1").SetValue(true));
            uFlashMenu.AddItem(
                new MenuItem(uFlashMenu.Name + ".hotkey", Global.Lang.Get("G_Hotkey")).SetValue(
                    new KeyBind('U', KeyBindType.Press)));
            uFlashMenu.AddItem(
                new MenuItem(uFlashMenu.Name + ".move-cursor", Global.Lang.Get("G_MoveCursor")).SetValue(true));
            uFlashMenu.AddItem(new MenuItem(uFlashMenu.Name + ".enabled", Global.Lang.Get("G_Enabled")).SetValue(true));

            var uAssistedMenu =
                ultimateMenu.AddSubMenu(new Menu(Global.Lang.Get("G_Assisted"), ultimateMenu.Name + ".assisted"));
            uAssistedMenu.AddItem(
                new MenuItem(uAssistedMenu.Name + ".min", "R " + Global.Lang.Get("G_Min")).SetValue(new Slider(3, 1, 5)));
            uAssistedMenu.AddItem(new MenuItem(uAssistedMenu.Name + ".1v1", "R 1v1").SetValue(true));
            uAssistedMenu.AddItem(
                new MenuItem(uAssistedMenu.Name + ".hotkey", Global.Lang.Get("G_Hotkey")).SetValue(
                    new KeyBind('R', KeyBindType.Press)));
            uAssistedMenu.AddItem(
                new MenuItem(uAssistedMenu.Name + ".move-cursor", Global.Lang.Get("G_MoveCursor")).SetValue(true));
            uAssistedMenu.AddItem(
                new MenuItem(uAssistedMenu.Name + ".enabled", Global.Lang.Get("G_Enabled")).SetValue(true));

            var fleeMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Flee"), Menu.Name + ".flee"));
            fleeMenu.AddItem(new MenuItem(fleeMenu.Name + ".w", Global.Lang.Get("G_UseW")).SetValue(true));
            fleeMenu.AddItem(new MenuItem(fleeMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(true));

            var initiatorMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("F_IM"), Menu.Name + ".initiator"));
            InitiatorManager.AddToMenu(
                initiatorMenu.AddSubMenu(new Menu(Global.Lang.Get("G_Whitelist"), initiatorMenu.Name + ".whitelist")));
            initiatorMenu.AddItem(new MenuItem(initiatorMenu.Name + ".use-e", Global.Lang.Get("G_UseE")).SetValue(true));

            var miscMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Miscellaneous"), Menu.Name + ".miscellaneous"));
            miscMenu.AddItem(
                new MenuItem(miscMenu.Name + ".block-r", Global.Lang.Get("G_BlockMissing") + " R").SetValue(true));

            DrawingManager.Add("R " + Global.Lang.Get("G_Flash"), R.Range + SummonerManager.Flash.Range);
            DrawingManager.Add("Combo Damage", new Circle(true, DamageIndicator.DrawingColor)).ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs args)
                {
                    DamageIndicator.Enabled = args.GetNewValue<Circle>().Active;
                    DamageIndicator.DrawingColor = args.GetNewValue<Circle>().Color;
                };
            DamageIndicator.Initialize(DamageIndicatorFunc);
            DamageIndicator.Enabled = DrawingManager.Get("Combo Damage").GetValue<Circle>().Active;
            DamageIndicator.DrawingColor = DrawingManager.Get("Combo Damage").GetValue<Circle>().Color;
        }

        private void OnSpellbookCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (sender.Owner.IsMe && args.Slot == SpellSlot.R)
            {
                if (Ball.IsMoving || Menu.Item(Menu.Name + ".miscellaneous.block-r").GetValue<bool>())
                {
                    args.Process =
                        GameObjects.EnemyHeroes.Where(e => e.Distance(Ball.Position) < R.Range * 3)
                            .Select(target => R.GetPrediction(target))
                            .Any(pred => pred.Hitchance >= HitChance.Medium);
                }
            }
        }

        private void OnAllyInitiator(object sender, InitiatorArgs args)
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

        private void OnBallPositionChange(object sender, EventArgs e)
        {
            foreach (var spell in Spells)
            {
                spell.UpdateSourcePosition(Ball.Position, Ball.Position);
            }
        }

        protected override void SetupSpells()
        {
            Q = new Spell(SpellSlot.Q, 825f);
            Q.SetSkillshot(0f, 130f, 1325f, false, SkillshotType.SkillshotCircle);

            W = new Spell(SpellSlot.W, 230f);
            W.SetSkillshot(0.25f, 230f, float.MaxValue, false, SkillshotType.SkillshotCircle);

            E = new Spell(SpellSlot.E, 1095f);
            E.SetSkillshot(0.25f, 80f, 1700f, false, SkillshotType.SkillshotLine);

            R = new Spell(SpellSlot.R, 375f);
            R.SetSkillshot(0.6f, 375f, float.MaxValue, false, SkillshotType.SkillshotCircle);
        }

        private void OnCorePostUpdate(EventArgs args)
        {
            try
            {
                if (Menu.Item(Menu.Name + ".ultimate.flash.enabled").GetValue<bool>() &&
                    Menu.Item(Menu.Name + ".ultimate.flash.hotkey").GetValue<KeyBind>().Active && R.IsReady() &&
                    SummonerManager.Flash.IsReady())
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
                            if (GameObjects.EnemyHeroes.Count(x => R.WillHit(x, pred.CastPosition)) >= min)
                            {
                                R.Cast(Player.Position);
                                Utility.DelayAction.Add(300, () => SummonerManager.Flash.Cast(flashPos));
                            }
                            else if (Menu.Item(Menu.Name + ".ultimate.flash.1v1").GetValue<bool>())
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
                            R.UpdateSourcePosition(Ball.Position, Ball.Position);
                        }
                    }
                }

                if (Menu.Item(Menu.Name + ".ultimate.assisted.enabled").GetValue<bool>() &&
                    Menu.Item(Menu.Name + ".ultimate.assisted.hotkey").GetValue<KeyBind>().Active && R.IsReady() &&
                    !Ball.IsMoving)
                {
                    if (Menu.Item(Menu.Name + ".ultimate.assisted.move-cursor").GetValue<bool>())
                    {
                        Orbwalking.MoveTo(Game.CursorPos, Orbwalker.HoldAreaRadius);
                    }
                    if (
                        !RLogic(
                            R.GetHitChance("combo"),
                            Menu.Item(Menu.Name + ".ultimate.assisted.min").GetValue<Slider>().Value))
                    {
                        var casted = false;
                        if (Menu.Item(Menu.Name + ".ultimate.assisted.1v1").GetValue<bool>())
                        {
                            casted = RLogic1V1(
                                R.GetHitChance("combo"),
                                Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady(),
                                Menu.Item(Menu.Name + ".combo.w").GetValue<bool>() && W.IsReady(),
                                Menu.Item(Menu.Name + ".combo.e").GetValue<bool>() && E.IsReady());
                        }
                        if (!casted)
                        {
                            if (E.IsReady())
                            {
                                int hits;
                                var hero = AssistedELogic(out hits);
                                if (hero != null && hits >= 1)
                                {
                                    E.CastOnUnit(hero);
                                    return;
                                }
                            }
                            else if (Q.IsReady())
                            {
                                int hits;
                                var pos = AssistedQLogic(R.GetHitChance("combo"), out hits);
                                if (!pos.Equals(Vector3.Zero) && hits >= 1)
                                {
                                    Q.Cast(pos);
                                    return;
                                }
                            }
                        }
                    }
                }

                if (Menu.Item(Menu.Name + ".ultimate.auto.enabled").GetValue<bool>() && R.IsReady() && !Ball.IsMoving)
                {
                    if (
                        !RLogic(
                            R.GetHitChance("combo"),
                            Menu.Item(Menu.Name + ".ultimate.auto.min").GetValue<Slider>().Value))
                    {
                        if (Menu.Item(Menu.Name + ".ultimate.auto.1v1").GetValue<bool>())
                        {
                            RLogic1V1(
                                R.GetHitChance("combo"),
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
                if (sender.IsEnemy && args.DangerLevel == Interrupter2.DangerLevel.High && args.MovementInterrupts &&
                    Menu.Item(Menu.Name + ".ultimate.auto.enabled").GetValue<bool>() &&
                    Menu.Item(Menu.Name + ".ultimate.auto.interrupt." + sender.ChampionName).GetValue<bool>() &&
                    R.IsReady())
                {
                    var pos = Ball.Position;
                    if (Ball.Status != BallStatus.Fixed)
                    {
                        var pred = Prediction.GetPrediction(Ball.Hero, R.Delay);
                        if (pred.Hitchance >= HitChance.Medium)
                        {
                            pos = pred.UnitPosition;
                        }
                    }
                    if (pos.Distance(sender.Position) <= R.Width)
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
            var r = Menu.Item(Menu.Name + ".ultimate.combo.enabled").GetValue<bool>() && R.IsReady();

            var target = TargetSelector.GetTarget(
                (q && e ? Math.Max(Q.Range, E.Range) : (e ? E.Range : Q.Range)), TargetSelector.DamageType.Magical);

            if (w && W.IsReady())
            {
                WLogic(W.GetHitChance("combo"));
            }
            if (w && e && W.IsReady() && E.IsReady())
            {
                EWLogic();
            }
            if (r && R.IsReady())
            {
                if (
                    !RLogic(
                        R.GetHitChance("combo"), Menu.Item(Menu.Name + ".ultimate.combo.min").GetValue<Slider>().Value))
                {
                    if (Menu.Item(Menu.Name + ".ultimate.combo.1v1").GetValue<bool>())
                    {
                        RLogic1V1(R.GetHitChance("combo"), q, w, e);
                    }
                }
                else
                {
                    if (e && E.IsReady())
                    {
                        ERLogic(Menu.Item(Menu.Name + ".ultimate.combo.min").GetValue<Slider>().Value);
                    }
                }
            }
            if (q && Q.IsReady())
            {
                QLogic(target, Q.GetHitChance("combo"));
            }
            if (e && E.IsReady())
            {
                ELogic(E.GetHitChance("combo"));
            }
            if (target != null && CalcComboDamage(target, q, w, e, r) > target.Health)
            {
                ItemManager.UseComboItems(target);
                SummonerManager.UseComboSummoners(target);
            }
        }

        private float DamageIndicatorFunc(Obj_AI_Hero target)
        {
            return CalcComboDamage(target, true, true, true, true);
        }

        private float CalcComboDamage(Obj_AI_Hero target, bool q, bool w, bool e, bool r)
        {
            try
            {
                float damage = 0;

                if (q)
                {
                    damage += Q.GetDamage(target) * 1.5f;
                }
                if (w && W.IsReady())
                {
                    damage += W.GetDamage(target);
                }
                if (e && E.IsReady())
                {
                    damage += E.GetDamage(target);
                }
                if (r && R.IsReady())
                {
                    damage += R.GetDamage(target);
                }
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

        private bool RLogic1V1(HitChance hitChance, bool q, bool w, bool e)
        {
            try
            {
                if (Ball.IsMoving)
                {
                    return false;
                }
                if ((from target in
                    GameObjects.EnemyHeroes.Where(t => t.HealthPercent > 25 && t.Distance(Ball.Position) < R.Width * 3)
                    let cDmg = CalcComboDamage(target, q, w, e, true)
                    where cDmg - 20 >= target.Health
                    where
                        GameObjects.EnemyHeroes.Count(em => !em.IsDead && em.IsVisible && em.Distance(Player) < 3000) ==
                        1
                    select target).Any(target => RLogic(hitChance, 1)))
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

        private bool RLogic(HitChance hitChance, int min)
        {
            try
            {
                if (Ball.IsMoving)
                {
                    return false;
                }
                var chance = HitChance.Low;
                var hits = 0;
                foreach (var target in GameObjects.EnemyHeroes.Where(e => e.Distance(Ball.Position) < R.Range * 3))
                {
                    var pred = R.GetPrediction(target);
                    if (pred.Hitchance >= HitChance.Low && pred.UnitPosition.Distance(Ball.Position) <= R.Width)
                    {
                        hits++;
                        if (pred.Hitchance > chance)
                        {
                            chance = pred.Hitchance;
                        }
                    }
                }
                if (hits >= min && chance >= hitChance)
                {
                    R.Cast(Player.Position);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return false;
        }

        private void QLogic(Obj_AI_Hero target, HitChance hitChance)
        {
            try
            {
                if (Ball.IsMoving)
                {
                    return;
                }
                var pos = GetBestQLocation(target, hitChance);
                if (!pos.Equals(Vector3.Zero))
                {
                    Q.Cast(pos);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void WLogic(HitChance hitChance)
        {
            try
            {
                if (Ball.IsMoving)
                {
                    return;
                }
                if (
                    GameObjects.EnemyHeroes.Where(e => e.Distance(Ball.Position) < W.Width * 3)
                        .Select(enemy => W.GetPrediction(enemy))
                        .Any(pred => pred.Hitchance >= hitChance))
                {
                    W.Cast(Player.Position);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private Obj_AI_Hero AssistedELogic(out int hits)
        {
            hits = 0;
            try
            {
                if (Ball.IsMoving)
                {
                    return null;
                }
                Obj_AI_Hero hero = null;
                var totalHits = 0;
                foreach (var ally in
                    GameObjects.AllyHeroes.Where(
                        a => (Ball.Hero == null || Ball.Hero.NetworkId != a.NetworkId) && E.IsInRange(a.Position)))
                {
                    var allyPred = E.GetPrediction(ally);
                    var cHits = 0;
                    if (allyPred.Hitchance >= HitChance.Low &&
                        allyPred.UnitPosition.Distance(Player.Position) < _maxBallDistance)
                    {
                        R.UpdateSourcePosition(allyPred.UnitPosition);
                        cHits =
                            GameObjects.EnemyHeroes.Where(e => e.Position.Distance(allyPred.UnitPosition) < R.Width * 3)
                                .Select(enemy => E.GetPrediction(enemy))
                                .Count(
                                    enemyPred =>
                                        enemyPred.Hitchance >= HitChance.Medium && R.IsInRange(enemyPred.UnitPosition));
                    }
                    if (cHits > totalHits || cHits == totalHits)
                    {
                        totalHits = cHits;
                        hero = ally;
                    }
                }
                R.UpdateSourcePosition(Ball.Position, Ball.Position);
                hits = totalHits;
                return hero;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return null;
        }

        private Vector3 AssistedQLogic(HitChance hitChance, out int hits)
        {
            hits = 0;
            try
            {
                if (Ball.IsMoving)
                {
                    return Vector3.Zero;
                }
                var input = new PredictionInput
                {
                    Collision = false,
                    From = Ball.Position,
                    RangeCheckFrom = Ball.Position,
                    Delay = Q.Delay + R.Delay,
                    Range = Q.Range,
                    Speed = Q.Speed,
                    Radius = R.Width,
                    Type = R.Type
                };

                var points = new List<Vector2>();
                var enemies = GameObjects.EnemyHeroes.Where(h => h.IsValidTarget((Q.Range + R.Range) * 1.5f)).ToList();
                foreach (var enemy in enemies)
                {
                    input.Unit = enemy;
                    var pred = Prediction.GetPrediction(input);
                    if (pred.Hitchance >= hitChance - 1)
                    {
                        points.Add(pred.UnitPosition.To2D());
                    }
                }
                var possiblities =
                    ListExtensions.ProduceEnumeration(points)
                        .Where(p => p.Count > 0)
                        .OrderByDescending(p => p.Count)
                        .ToList();
                var totalHits = 0;
                var center = Vector2.Zero;
                if (possiblities.Any())
                {
                    foreach (var possibility in possiblities)
                    {
                        var mec = MEC.GetMec(possibility);
                        if (mec.Radius < (R.Range - 50))
                        {
                            if (possibility.Count > totalHits)
                            {
                                totalHits = hits;
                                center = mec.Center;
                                if (hits >= enemies.Count)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
                hits = totalHits;
                return center.To3D();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return Vector3.Zero;
        }

        // ReSharper disable once InconsistentNaming
        private void EWLogic()
        {
            try
            {
                if (Ball.IsMoving)
                {
                    return;
                }
                Obj_AI_Hero hero = null;
                var totalHits = 0;
                foreach (var ally in
                    GameObjects.AllyHeroes.Where(
                        a => (Ball.Hero == null || Ball.Hero.NetworkId != a.NetworkId) && E.IsInRange(a.Position)))
                {
                    var allyPred = E.GetPrediction(ally);
                    var cHits = 0;
                    if (allyPred.Hitchance >= HitChance.Low &&
                        allyPred.UnitPosition.Distance(Player.Position) < _maxBallDistance)
                    {
                        W.UpdateSourcePosition(allyPred.UnitPosition);
                        cHits =
                            GameObjects.EnemyHeroes.Where(e => e.Position.Distance(allyPred.UnitPosition) < W.Width * 3)
                                .Select(enemy => E.GetPrediction(enemy))
                                .Count(
                                    enemyPred =>
                                        enemyPred.Hitchance >= HitChance.Medium && W.IsInRange(enemyPred.UnitPosition));
                    }
                    if (cHits > totalHits || cHits == totalHits)
                    {
                        totalHits = cHits;
                        hero = ally;
                    }
                }
                if (totalHits > 0)
                {
                    E.CastOnUnit(hero);
                }
                W.UpdateSourcePosition(Ball.Position, Ball.Position);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        // ReSharper disable once InconsistentNaming
        private void ERLogic(int minHits)
        {
            try
            {
                if (Ball.IsMoving)
                {
                    return;
                }
                Obj_AI_Hero hero = null;
                var totalHits = 0;
                foreach (var ally in
                    GameObjects.AllyHeroes.Where(
                        a => (Ball.Hero == null || Ball.Hero.NetworkId != a.NetworkId) && E.IsInRange(a.Position)))
                {
                    var allyPred = E.GetPrediction(ally);
                    var cHits = 0;
                    if (allyPred.Hitchance >= HitChance.Low &&
                        allyPred.UnitPosition.Distance(Player.Position) < _maxBallDistance)
                    {
                        R.UpdateSourcePosition(allyPred.UnitPosition);
                        cHits =
                            GameObjects.EnemyHeroes.Where(e => e.Position.Distance(allyPred.UnitPosition) < R.Width * 3)
                                .Select(enemy => E.GetPrediction(enemy))
                                .Count(
                                    enemyPred =>
                                        enemyPred.Hitchance >= HitChance.Medium && R.IsInRange(enemyPred.UnitPosition));
                    }
                    if (cHits > totalHits || cHits == totalHits)
                    {
                        totalHits = cHits;
                        hero = ally;
                    }
                }
                if (totalHits >= minHits)
                {
                    E.CastOnUnit(hero);
                }
                R.UpdateSourcePosition(Ball.Position, Ball.Position);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void ELogic(HitChance hitChance)
        {
            try
            {
                if (Ball.IsMoving || Ball.Status == BallStatus.Me)
                {
                    return;
                }
                var chance = HitChance.Low;
                Obj_AI_Hero hero = null;
                var totalHits = 0;
                foreach (var ally in
                    GameObjects.AllyHeroes.Where(
                        a => (Ball.Hero == null || Ball.Hero.NetworkId != a.NetworkId) && E.IsInRange(a.Position)))
                {
                    var cHits = 0;
                    var cChance = HitChance.Low;
                    var allyPred = E.GetPrediction(ally);
                    if (allyPred.Hitchance >= HitChance.Low)
                    {
                        foreach (var enemy in GameObjects.EnemyHeroes.Where(e => e.IsValidTarget(2000)))
                        {
                            var enemyPred = E.GetPrediction(enemy);
                            if (enemyPred.Hitchance >= HitChance.Low)
                            {
                                var circle = new Geometry.Polygon.Circle(enemyPred.CastPosition, enemy.BoundingRadius);
                                var rect = new Geometry.Polygon.Rectangle(
                                    allyPred.CastPosition, enemyPred.CastPosition, E.Width);
                                if (circle.Points.Any(c => rect.IsInside(c)))
                                {
                                    cHits++;
                                    if (enemyPred.Hitchance > cChance)
                                    {
                                        cChance = enemyPred.Hitchance;
                                    }
                                }
                            }
                        }
                        if (cHits > totalHits || cHits == totalHits && cChance > chance)
                        {
                            totalHits = cHits;
                            hero = ally;
                            chance = cChance;
                        }
                    }
                }
                if (totalHits > 0 && chance >= hitChance)
                {
                    E.CastOnUnit(hero);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
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

            if (q || w)
            {
                var target = TargetSelector.GetTarget(
                    (q && e ? Math.Max(Q.Range, E.Range) : (e ? E.Range : Q.Range)), TargetSelector.DamageType.Magical);
                if (w && W.IsReady())
                {
                    WLogic(W.GetHitChance("harass"));
                }
                if (w && e && W.IsReady() && E.IsReady())
                {
                    EWLogic();
                }
                if (q && Q.IsReady())
                {
                    QLogic(target, Q.GetHitChance("harass"));
                }
            }
            if (e && E.IsReady())
            {
                ELogic(E.GetHitChance("harass"));
            }
        }

        public Vector3 GetBestQLocation(Obj_AI_Hero target, HitChance hitChance)
        {
            if (Ball.IsMoving)
            {
                return Vector3.Zero;
            }
            var pred = Q.GetPrediction(target);
            if (pred.Hitchance >= hitChance)
            {
                return pred.CastPosition;
            }
            var points = (from enemy in GameObjects.EnemyHeroes.Where(h => h.IsValidTarget(Q.Range + R.Range))
                select Q.GetPrediction(enemy)
                into prediction
                where prediction.Hitchance >= (hitChance - 1)
                select prediction.UnitPosition.To2D()).ToList();
            var possiblities = ListExtensions.ProduceEnumeration(points).Where(p => p.Count > 1).ToList();
            if (possiblities.Any())
            {
                foreach (var possibility in possiblities)
                {
                    var mec = MEC.GetMec(possibility);
                    if (mec.Radius < (R.Range - 50) && points.Count >= 3 && R.IsReady())
                    {
                        return mec.Center.To3D();
                    }
                    if (mec.Radius < (W.Range - 50) && points.Count >= 2 && W.IsReady())
                    {
                        return mec.Center.To3D();
                    }
                    if (mec.Radius < Q.Width && points.Count == 2)
                    {
                        return mec.Center.To3D();
                    }
                }
            }
            return points.Count > 0 ? points.FirstOrDefault().To3D() : Vector3.Zero;
        }

        protected override void LaneClear()
        {
            if (!ManaManager.Check("lane-clear") || Ball.IsMoving)
            {
                return;
            }
            var q = Menu.Item(Menu.Name + ".lane-clear.q").GetValue<bool>() && Q.IsReady();
            var w = Menu.Item(Menu.Name + ".lane-clear.w").GetValue<bool>() && W.IsReady();

            if (!q && !w)
            {
                return;
            }
            var minions = MinionManager.GetMinions(
                Q.Range + W.Width, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
            var minHits = minions.Any(m => m.Team == GameObjectTeam.Neutral)
                ? 1
                : Menu.Item(Menu.Name + ".lane-clear.min").GetValue<Slider>().Value;
            if (q && w)
            {
                var wMinions = MinionManager.GetMinions(
                    Ball.Position, W.Width, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
                if (wMinions.Count < minHits)
                {
                    var prediction1 = W.GetCircularFarmLocation(minions);
                    var prediction2 = Q.GetCircularFarmLocation(minions);
                    var qCount = minions.Count(minion => Q.WillHit(minion, prediction1.Position.To3D()));

                    if (qCount > minHits && prediction1.MinionsHit >= minHits &&
                        qCount + prediction1.MinionsHit > prediction2.MinionsHit)
                    {
                        Q.Cast(prediction1.Position);
                    }
                    else
                    {
                        if (prediction2.MinionsHit >= minHits)
                        {
                            Q.Cast(prediction2.Position);
                        }
                    }
                }
                else
                {
                    W.Cast(Player.Position);
                }
            }
            else if (q)
            {
                var prediction = Q.GetCircularFarmLocation(minions);
                if (prediction.MinionsHit >= minHits)
                {
                    Q.Cast(prediction.Position);
                }
            }
            else
            {
                Console.WriteLine(minions.Count(m => W.IsInRange(m)));
                if (minions.Count(m => W.IsInRange(m)) >= minHits)
                {
                    W.Cast(Player.Position);
                }
            }
        }

        protected override void Flee()
        {
            if (Ball.IsMoving)
            {
                return;
            }
            if (Menu.Item(Menu.Name + ".flee.w").GetValue<bool>() && Ball.Status == BallStatus.Me && W.IsReady())
            {
                W.Cast(Player.Position);
            }
            if (Menu.Item(Menu.Name + ".flee.e").GetValue<bool>() && E.IsReady())
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
                    if (!_pos.Equals(tmp))
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
                    if (!_hero.Position.Equals(tmp))
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

            private static void OnObjAiBaseProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
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

            private static void OnGameObjectCreate(GameObject sender, EventArgs args)
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
        }
    }
}