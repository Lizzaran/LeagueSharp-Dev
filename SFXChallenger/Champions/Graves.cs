#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Graves.cs is part of SFXChallenger.

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
using SFXChallenger.Managers;
using SFXLibrary;
using SFXLibrary.Logger;
using SharpDX;
using DamageType = SFXChallenger.Enumerations.DamageType;
using Orbwalking = SFXChallenger.Wrappers.Orbwalking;
using Spell = SFXChallenger.Wrappers.Spell;
using TargetSelector = SFXChallenger.Wrappers.TargetSelector;

#endregion

namespace SFXChallenger.Champions
{
    internal class Graves : Champion
    {
        protected override ItemFlags ItemFlags
        {
            get { return ItemFlags.Offensive | ItemFlags.Defensive | ItemFlags.Flee; }
        }

        protected override ItemUsageType ItemUsage
        {
            get { return ItemUsageType.AfterAttack; }
        }

        public Spell R2 { get; private set; }

        protected override void OnLoad()
        {
            Core.OnPostUpdate += OnCorePostUpdate;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
        }

        protected override void OnUnload()
        {
            Core.OnPostUpdate -= OnCorePostUpdate;
            AntiGapcloser.OnEnemyGapcloser -= OnEnemyGapcloser;
        }

        protected override void AddToMenu()
        {
            var comboMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Combo"), Menu.Name + ".combo"));
            HitchanceManager.AddToMenu(
                comboMenu.AddSubMenu(new Menu(Global.Lang.Get("F_MH"), comboMenu.Name + ".hitchance")), "combo",
                new Dictionary<string, int> { { "Q", 2 }, { "W", 1 }, { "R", 2 } });
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".w", Global.Lang.Get("G_UseW")).SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(true));

            var harassMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Harass"), Menu.Name + ".harass"));
            HitchanceManager.AddToMenu(
                harassMenu.AddSubMenu(new Menu(Global.Lang.Get("F_MH"), harassMenu.Name + ".hitchance")), "harass",
                new Dictionary<string, int> { { "Q", 2 } });
            ManaManager.AddToMenu(harassMenu, "harass", ManaCheckType.Minimum, ManaValueType.Percent);
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));

            var laneclearMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_LaneClear"), Menu.Name + ".lane-clear"));
            ManaManager.AddToMenu(laneclearMenu, "lane-clear", ManaCheckType.Minimum, ManaValueType.Percent);
            laneclearMenu.AddItem(
                new MenuItem(laneclearMenu.Name + ".q-min", "Q " + Global.Lang.Get("G_Min")).SetValue(
                    new Slider(3, 1, 5)));
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));

            UltimateManager.AddToMenu(Menu, true, false, false, false, false, false, true, true, true);

            var killstealMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Killsteal"), Menu.Name + ".killsteal"));
            killstealMenu.AddItem(new MenuItem(killstealMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));

            var fleeMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Flee"), Menu.Name + ".flee"));
            fleeMenu.AddItem(new MenuItem(fleeMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(true));

            var miscMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Miscellaneous"), Menu.Name + ".miscellaneous"));

            HeroListManager.AddToMenu(
                miscMenu.AddSubMenu(new Menu("W " + Global.Lang.Get("G_Gapcloser"), miscMenu.Name + "w-gapcloser")),
                "w-gapcloser", false, false, true, false);
            HeroListManager.AddToMenu(
                miscMenu.AddSubMenu(new Menu("E " + Global.Lang.Get("G_Gapcloser"), miscMenu.Name + "e-gapcloser")),
                "e-gapcloser", false, false, true, false);

            IndicatorManager.AddToMenu(DrawingManager.GetMenu(), true);
            IndicatorManager.Add(Q);
            IndicatorManager.Add(W);
            IndicatorManager.Add(R);
            IndicatorManager.Finale();
        }

        protected override void SetupSpells()
        {
            Q = new Spell(SpellSlot.Q, 850f);
            Q.SetSkillshot(0.25f, 15f * (float) Math.PI / 180, 2000f, false, SkillshotType.SkillshotCone);

            W = new Spell(SpellSlot.W, 900f, DamageType.Magical);
            W.SetSkillshot(0.35f, 250f, 1650f, false, SkillshotType.SkillshotCircle);

            E = new Spell(SpellSlot.E, 425f);

            R = new Spell(SpellSlot.R, 1100f);
            R.SetSkillshot(0.25f, 110f, 2100f, false, SkillshotType.SkillshotLine);

            R2 = new Spell(SpellSlot.R, 750f);
            R2.SetSkillshot(0f, (float) (60 * Math.PI / 180), 1500f, false, SkillshotType.SkillshotCone);
        }

        private void OnCorePostUpdate(EventArgs args)
        {
            try
            {
                if (UltimateManager.Assisted() && R.IsReady())
                {
                    if (Menu.Item(Menu.Name + ".ultimate.assisted.move-cursor").GetValue<bool>())
                    {
                        Orbwalking.MoveTo(Game.CursorPos, Orbwalker.HoldAreaRadius);
                    }

                    if (
                        !RLogic(
                            TargetSelector.GetTarget(R),
                            Menu.Item(Menu.Name + ".ultimate.assisted.min").GetValue<Slider>().Value,
                            Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady()))
                    {
                        RLogicDuel(Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady());
                    }
                }

                if (UltimateManager.Auto() && R.IsReady())
                {
                    if (
                        !RLogic(
                            TargetSelector.GetTarget(R),
                            Menu.Item(Menu.Name + ".ultimate.auto.min").GetValue<Slider>().Value,
                            Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady()))
                    {
                        RLogicDuel(Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady());
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

                if (HeroListManager.Check("w-gapcloser", args.Sender))
                {
                    if (args.End.Distance(Player.Position) < W.Range)
                    {
                        W.Cast(args.End);
                    }
                }
                if (HeroListManager.Check("e-gapcloser", args.Sender))
                {
                    E.Cast(args.End.Extend(Player.Position, E.Range));
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected override void Combo()
        {
            var useQ = Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady();
            var useW = Menu.Item(Menu.Name + ".combo.w").GetValue<bool>() && W.IsReady();
            var useE = Menu.Item(Menu.Name + ".combo.e").GetValue<bool>() && E.IsReady();
            var useR = UltimateManager.Combo() && R.IsReady();

            if (useQ)
            {
                Casting.SkillShot(Q, Q.GetHitChance("combo"));
            }
            if (useW)
            {
                var target = TargetSelector.GetTarget(W);
                var best = CPrediction.Circle(W, target, W.GetHitChance("combo"));
                if (best.TotalHits > 0 && !best.CastPosition.Equals(Vector3.Zero))
                {
                    W.Cast(best.CastPosition);
                }
            }
            if (useE)
            {
                var target = TargetSelector.GetTarget((E.Range + Player.AttackRange) * 0.9f, E.DamageType);
                if (target != null)
                {
                    var pos = Player.Position.Extend(Game.CursorPos, E.Range);
                    if (!pos.UnderTurret(true))
                    {
                        E.Cast(pos);
                    }
                }
            }
            if (useR)
            {
                var target = TargetSelector.GetTarget(R);
                if (target != null && Orbwalking.InAutoAttackRange(target))
                {
                    if (!RLogic(target, Menu.Item(Menu.Name + ".ultimate.combo.min").GetValue<Slider>().Value, useQ))
                    {
                        if (Menu.Item(Menu.Name + ".ultimate.combo.duel").GetValue<bool>())
                        {
                            RLogicDuel(useQ);
                        }
                    }
                }
            }
        }

        private bool RLogic(Obj_AI_Hero target, int min, bool q)
        {
            try
            {
                var hits = GetRHits(target);
                if (UltimateManager.Check(min, hits.Item2, hero => CalcComboDamage(hero, q, true)))
                {
                    R.Cast(hits.Item3);
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

        private void RLogicDuel(bool q)
        {
            try
            {
                foreach (var t in GameObjects.EnemyHeroes)
                {
                    if (UltimateManager.CheckDuel(t, CalcComboDamage(t, q, true)))
                    {
                        if (RLogic(t, 1, q))
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

        private float CalcComboDamage(Obj_AI_Hero target, bool q, bool r)
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
                if (r && R.IsReady())
                {
                    damage += R.GetDamage(target);
                }
                damage += 3 * (float) Player.GetAutoAttackDamage(target, true);
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

        private Tuple<int, List<Obj_AI_Hero>, Vector3> GetRHits(Obj_AI_Hero target)
        {
            var hits = new List<Obj_AI_Hero>();
            var castPos = Vector3.Zero;
            try
            {
                var pred = R.GetPrediction(target);
                if (pred.Hitchance >= R.GetHitChance("combo"))
                {
                    castPos = pred.CastPosition;
                    hits.Add(target);
                    var pos = Player.Position.Extend(pred.CastPosition, Player.Distance(pred.UnitPosition));
                    var pos2 = Player.Position.Extend(pos, Player.Distance(pos) + R2.Range);
                    R2.UpdateSourcePosition(pos, pos);
                    R2.Delay = Player.Position.Distance(pred.UnitPosition) / R.Speed + 0.1f;
                    hits.AddRange(
                        GameObjects.EnemyHeroes.Where(
                            h =>
                                h.NetworkId != target.NetworkId && h.IsValidTarget() &&
                                h.Distance(h.Position, true) < (R.Range + R2.Range) * (R.Range + R2.Range))
                            .Where(enemy => R2.WillHit(enemy, pos2)));
                    R2.UpdateSourcePosition();
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return new Tuple<int, List<Obj_AI_Hero>, Vector3>(hits.Count, hits, castPos);
        }

        protected override void Harass()
        {
            if (!ManaManager.Check("harass"))
            {
                return;
            }

            if (Menu.Item(Menu.Name + ".harass.q").GetValue<bool>() && Q.IsReady())
            {
                Casting.SkillShot(Q, Q.GetHitChance("harass"));
            }
        }

        protected override void LaneClear()
        {
            if (!ManaManager.Check("lane-clear"))
            {
                return;
            }

            var useQ = Menu.Item(Menu.Name + ".lane-clear.q").GetValue<bool>() && Q.IsReady();
            var minQ = Menu.Item(Menu.Name + ".lane-clear.q-min").GetValue<Slider>().Value;

            if (useQ)
            {
                Casting.Farm(Q, minQ, 200f);
            }
        }

        protected override void Flee()
        {
            if (Menu.Item(Menu.Name + ".flee.e").GetValue<bool>() && E.IsReady())
            {
                E.Cast(Player.Position.Extend(Game.CursorPos, E.Range));
            }
        }

        protected override void Killsteal()
        {
            if (Menu.Item(Menu.Name + ".killsteal.q").GetValue<bool>() && Q.IsReady())
            {
                var fPredEnemy =
                    GameObjects.EnemyHeroes.Where(e => e.IsValidTarget(Q.Range * 1.2f) && Q.IsKillable(e))
                        .Select(enemy => Q.GetPrediction(enemy, true))
                        .FirstOrDefault(pred => pred.Hitchance >= Q.GetHitChance("harass"));
                if (fPredEnemy != null)
                {
                    Q.Cast(fPredEnemy.CastPosition);
                }
            }
        }
    }
}