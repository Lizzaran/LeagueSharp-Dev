#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 viktor.cs is part of SFXChallenger.

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
using SFXLibrary.Extensions.NET;
using SFXLibrary.Logger;
using SharpDX;
using MinionManager = SFXLibrary.MinionManager;
using MinionOrderTypes = SFXLibrary.MinionOrderTypes;
using MinionTeam = SFXLibrary.MinionTeam;
using MinionTypes = SFXLibrary.MinionTypes;
using Orbwalking = SFXChallenger.Wrappers.Orbwalking;
using TargetSelector = SFXChallenger.Wrappers.TargetSelector;
using Utils = SFXChallenger.Helpers.Utils;

#endregion

namespace SFXChallenger.Champions
{
    internal class Viktor : Champion
    {
        private const float MaxERange = 1225f;
        private const float ELength = 700f;
        private const float RMoveInterval = 325f;
        private float _lastRMoveCommand = Environment.TickCount;
        private GameObject _rObject;

        protected override ItemFlags ItemFlags
        {
            get { return ItemFlags.Offensive | ItemFlags.Defensive | ItemFlags.Flee; }
        }

        protected override void OnLoad()
        {
            Core.OnPostUpdate += OnCorePostUpdate;
            Orbwalking.BeforeAttack += OnOrbwalkingBeforeAttack;
            Orbwalking.AfterAttack += OnOrbwalkingAfterAttack;
            Orbwalking.OnNonKillableMinion += OnOrbwalkingNonKillableMinion;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget += OnInterruptableTarget;
            CustomEvents.Unit.OnDash += OnUnitDash;
            GameObject.OnCreate += OnGameObjectCreate;
        }

        protected override void OnUnload()
        {
            Core.OnPostUpdate -= OnCorePostUpdate;
            Orbwalking.BeforeAttack -= OnOrbwalkingBeforeAttack;
            Orbwalking.AfterAttack -= OnOrbwalkingAfterAttack;
            Orbwalking.OnNonKillableMinion -= OnOrbwalkingNonKillableMinion;
            AntiGapcloser.OnEnemyGapcloser -= OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget -= OnInterruptableTarget;
            CustomEvents.Unit.OnDash -= OnUnitDash;
            GameObject.OnCreate -= OnGameObjectCreate;
        }

        protected override void AddToMenu()
        {
            DrawingManager.Add("E " + Global.Lang.Get("G_Max"), MaxERange);
            DrawingManager.Add("R " + Global.Lang.Get("G_Max"), R.Range + (R.Width / 2f));

            var comboMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Combo"), Menu.Name + ".combo"));
            HitchanceManager.AddToMenu(
                comboMenu.AddSubMenu(new Menu(Global.Lang.Get("F_MH"), comboMenu.Name + ".hitchance")), "combo",
                new Dictionary<string, int> { { "W", 2 }, { "E", 2 } });
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".w", Global.Lang.Get("G_UseW")).SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(true));

            var harassMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Harass"), Menu.Name + ".harass"));
            HitchanceManager.AddToMenu(
                harassMenu.AddSubMenu(new Menu(Global.Lang.Get("F_MH"), harassMenu.Name + ".hitchance")), "harass",
                new Dictionary<string, int> { { "E", 2 } });
            ManaManager.AddToMenu(harassMenu, "harass", ManaCheckType.Minimum, ManaValueType.Percent);
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(true));

            var laneclearMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_LaneClear"), Menu.Name + ".lane-clear"));
            ManaManager.AddToMenu(
                laneclearMenu, "lane-clear-q", ManaCheckType.Minimum, ManaValueType.Total, string.Empty, 200, 0, 750);
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));
            ManaManager.AddToMenu(laneclearMenu, "lane-clear-e", ManaCheckType.Minimum, ManaValueType.Percent);
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(true));
            laneclearMenu.AddItem(
                new MenuItem(laneclearMenu.Name + ".e-min", "E " + Global.Lang.Get("G_Min")).SetValue(
                    new Slider(3, 1, 5)));

            var lasthitMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_LastHit"), Menu.Name + ".lasthit"));
            ManaManager.AddToMenu(lasthitMenu, "lasthit", ManaCheckType.Minimum, ManaValueType.Percent);
            lasthitMenu.AddItem(
                new MenuItem(lasthitMenu.Name + ".q-unkillable", "Q " + Global.Lang.Get("G_Unkillable")).SetValue(true));

            var ultimateMenu = UltimateManager.AddToMenu(Menu, true, true, true, false, false, false, true, true, true);

            ultimateMenu.AddItem(
                new MenuItem(ultimateMenu.Name + ".follow", Global.Lang.Get("Viktor_AutoFollow")).SetValue(true));

            var killstealMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Killsteal"), Menu.Name + ".killsteal"));
            killstealMenu.AddItem(new MenuItem(killstealMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(true));
            killstealMenu.AddItem(
                new MenuItem(killstealMenu.Name + ".q-aa", "Q + " + Global.Lang.Get("G_AutoAttack")).SetValue(true));

            var fleeMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Flee"), Menu.Name + ".flee"));
            fleeMenu.AddItem(new MenuItem(fleeMenu.Name + ".w", Global.Lang.Get("G_UseW")).SetValue(true));
            fleeMenu.AddItem(
                new MenuItem(fleeMenu.Name + ".q-upgraded", "Q " + Global.Lang.Get("G_Upgraded")).SetValue(true));

            var miscMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Miscellaneous"), Menu.Name + ".miscellaneous"));

            HeroListManager.AddToMenu(
                miscMenu.AddSubMenu(new Menu("W " + Global.Lang.Get("G_Slowed"), miscMenu.Name + "w-slowed")),
                "w-slowed", false, false, true, false);
            HeroListManager.AddToMenu(
                miscMenu.AddSubMenu(new Menu("W " + Global.Lang.Get("G_Stunned"), miscMenu.Name + "w-stunned")),
                "w-stunned", false, false, true, false);
            HeroListManager.AddToMenu(
                miscMenu.AddSubMenu(new Menu("W " + Global.Lang.Get("G_Gapcloser"), miscMenu.Name + "w-gapcloser")),
                "w-gapcloser", false, false, true, false);


            IndicatorManager.AddToMenu(DrawingManager.GetMenu(), true);
            IndicatorManager.Add(
                "Q", delegate(Obj_AI_Hero hero)
                {
                    var damage = 0f;
                    if (Q.IsReady())
                    {
                        damage += Q.GetDamage(hero);
                        damage += CalcPassiveDamage(hero);
                    }
                    else if (Player.HasBuff("viktorpowertransferreturn"))
                    {
                        damage += CalcPassiveDamage(hero);
                    }
                    return damage;
                });
            IndicatorManager.Add(E);
            IndicatorManager.Add(
                "R " + Global.Lang.Get("G_Burst"), delegate(Obj_AI_Hero hero)
                {
                    if (R.IsReady())
                    {
                        return R.GetDamage(hero) + R.GetDamage(hero, 1);
                    }
                    return 0;
                });
            IndicatorManager.Add(
                "R " + Global.Lang.Get("G_Full"), delegate(Obj_AI_Hero hero)
                {
                    if (R.IsReady())
                    {
                        return R.GetDamage(hero) + R.GetDamage(hero, 1) * 10;
                    }
                    return 0;
                });
            IndicatorManager.Finale();
        }

        protected override void SetupSpells()
        {
            Q = new Spell(SpellSlot.Q, Player.BoundingRadius + 600f);
            Q.Range += GameObjects.EnemyHeroes.Max(e => e.BoundingRadius);
            Q.SetTargetted(0.2f, 1800f);

            W = new Spell(SpellSlot.W, 700f);
            W.SetSkillshot(1.6f, 300f, float.MaxValue, false, SkillshotType.SkillshotCircle);

            E = new Spell(SpellSlot.E, 525f);
            E.SetSkillshot(0f, 90f, 800f, false, SkillshotType.SkillshotLine);

            R = new Spell(SpellSlot.R, 700f);
            R.SetSkillshot(0.3f, 300f, float.MaxValue, false, SkillshotType.SkillshotCircle);
        }

        private void OnCorePostUpdate(EventArgs args)
        {
            try
            {
                if (Menu.Item(Menu.Name + ".ultimate.follow").GetValue<bool>())
                {
                    RFollowLogic();
                }
                if (UltimateManager.Assisted() && R.IsReady())
                {
                    if (Menu.Item(Menu.Name + ".ultimate.assisted.move-cursor").GetValue<bool>())
                    {
                        Orbwalking.MoveTo(Game.CursorPos, Orbwalker.HoldAreaRadius);
                    }
                    var target = TargetSelector.GetTarget(
                        R.Range + R.Width, LeagueSharp.Common.TargetSelector.DamageType.Magical);
                    if (target != null &&
                        !RLogic(target, Menu.Item(Menu.Name + ".ultimate.assisted.min").GetValue<Slider>().Value))
                    {
                        if (Menu.Item(Menu.Name + ".ultimate.assisted.duel").GetValue<bool>())
                        {
                            RLogicDuel(
                                Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady(),
                                Menu.Item(Menu.Name + ".combo.e").GetValue<bool>() && E.IsReady());
                        }
                    }
                }

                if (UltimateManager.Auto() && R.IsReady())
                {
                    var target = TargetSelector.GetTarget(
                        R.Range + R.Width, LeagueSharp.Common.TargetSelector.DamageType.Magical);
                    if (target != null &&
                        !RLogic(target, Menu.Item(Menu.Name + ".ultimate.auto.min").GetValue<Slider>().Value))
                    {
                        if (Menu.Item(Menu.Name + ".ultimate.auto.duel").GetValue<bool>())
                        {
                            RLogicDuel(
                                Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady(),
                                Menu.Item(Menu.Name + ".combo.e").GetValue<bool>() && E.IsReady());
                        }
                    }
                }

                if (HeroListManager.Enabled("w-stunned") && W.IsReady())
                {
                    var target =
                        GameObjects.EnemyHeroes.FirstOrDefault(
                            t => t.IsValidTarget(W.Range) && HeroListManager.Check("w-stunned", t) && Utils.IsStunned(t));
                    if (target != null)
                    {
                        Casting.SkillShot(target, W, W.GetHitChance("combo"));
                    }
                }

                if (HeroListManager.Enabled("w-slowed") && W.IsReady())
                {
                    var target =
                        GameObjects.EnemyHeroes.FirstOrDefault(
                            t =>
                                t.IsValidTarget(W.Range) && HeroListManager.Check("w-slowed", t) &&
                                t.Buffs.Any(b => b.Type == BuffType.Slow && b.EndTime - Game.Time > 0.5f));
                    if (target != null)
                    {
                        Casting.SkillShot(target, W, W.GetHitChance("combo"));
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnOrbwalkingNonKillableMinion(AttackableUnit unit)
        {
            try
            {
                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit ||
                    Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
                {
                    if (Menu.Item(Menu.Name + ".lasthit.q-unkillable").GetValue<bool>() && Q.IsReady() &&
                        ManaManager.Check("lasthit"))
                    {
                        var target = unit as Obj_AI_Base;
                        if (target != null && Q.IsKillable(target) &&
                            HealthPrediction.GetHealthPrediction(target, (int) (Q.GetSpellDelay(target) * 1000)) > 0)
                        {
                            Casting.TargetSkill(target, Q);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private bool IsSpellUpgraded(Spell spell)
        {
            try
            {
                return
                    Player.Buffs.Select(b => b.Name.ToLower())
                        .Where(b => b.StartsWith("viktor") && b.EndsWith("aug"))
                        .Any(
                            b =>
                                spell.Slot == SpellSlot.R
                                    ? b.Contains("qwe")
                                    : b.Contains(spell.Slot.ToString().ToLower()));
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return false;
        }

        private void OnGameObjectCreate(GameObject sender, EventArgs args)
        {
            try
            {
                if (sender.Type == GameObjectType.obj_GeneralParticleEmitter &&
                    sender.Name.Equals("Viktor_ChaosStorm_green.troy", StringComparison.OrdinalIgnoreCase))
                {
                    _rObject = sender;
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnOrbwalkingBeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            try
            {
                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Flee)
                {
                    args.Process = false;
                    return;
                }
                if (Player.HasBuff("viktorpowertransferreturn"))
                {
                    if (args.Target.Type != GameObjectType.obj_AI_Hero)
                    {
                        var hero =
                            TargetSelector.GetTargets(Player.AttackRange + Player.BoundingRadius * 4f)
                                .FirstOrDefault(Orbwalking.InAutoAttackRange);
                        if (hero != null)
                        {
                            Orbwalker.ForceTarget(hero);
                            args.Process = false;
                            return;
                        }
                    }
                }
                if (args.Target.Type == GameObjectType.obj_AI_Hero &&
                    (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo ||
                     Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed))
                {
                    args.Process = (!Q.IsReady() || Player.Mana < Q.Instance.ManaCost) &&
                                   (!E.IsReady() || Player.Mana < E.Instance.ManaCost);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnOrbwalkingAfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            Orbwalker.ForceTarget(null);
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
                if (HeroListManager.Check("w-gapcloser", hero))
                {
                    if (args.EndPos.Distance(Player.Position) < W.Range)
                    {
                        W.Cast(args.EndPos);
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
                    UltimateManager.Interrupt(sender))
                {
                    Utility.DelayAction.Add(DelayManager.Get("ultimate-interrupt-delay"), () => R.Cast(sender.Position));
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
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected override void Combo()
        {
            var q = Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady();
            var w = Menu.Item(Menu.Name + ".combo.w").GetValue<bool>() && W.IsReady();
            var e = Menu.Item(Menu.Name + ".combo.e").GetValue<bool>() && E.IsReady();
            var r = UltimateManager.Combo() && R.IsReady();

            if (q)
            {
                Casting.TargetSkill(Q);
            }
            if (w)
            {
                var target = TargetSelector.GetTarget(W.Range * 1.2f);
                if (target != null)
                {
                    WLogic(target, W.GetHitChance("combo"));
                }
            }
            if (e)
            {
                var target = TargetSelector.GetTarget(
                    MaxERange * 1.2f, LeagueSharp.Common.TargetSelector.DamageType.Magical);
                if (target != null)
                {
                    ELogic(target, GameObjects.EnemyHeroes.ToList(), E.GetHitChance("combo"));
                }
            }
            if (r)
            {
                var target = TargetSelector.GetTarget(
                    R.Range + R.Width, LeagueSharp.Common.TargetSelector.DamageType.Magical);
                if (target != null &&
                    !RLogic(target, Menu.Item(Menu.Name + ".ultimate.combo.min").GetValue<Slider>().Value))
                {
                    if (Menu.Item(Menu.Name + ".ultimate.combo.duel").GetValue<bool>())
                    {
                        RLogicDuel(q, e);
                    }
                }
            }
            var rTarget = TargetSelector.GetTarget(
                R.Range + R.Width, LeagueSharp.Common.TargetSelector.DamageType.Magical);
            if (rTarget != null && CalcComboDamage(rTarget, q, e, r) > rTarget.Health)
            {
                ItemManager.UseComboItems(rTarget);
                SummonerManager.UseComboSummoners(rTarget);
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
                var damage = 0f;
                if (Player.HasBuff("viktorpowertransferreturn") && Orbwalking.InAutoAttackRange(target))
                {
                    damage += CalcPassiveDamage(target);
                }
                if (q && Q.IsReady() && target.IsValidTarget(Q.Range))
                {
                    damage += Q.GetDamage(target);
                    if (Orbwalking.InAutoAttackRange(target))
                    {
                        damage += CalcPassiveDamage(target);
                    }
                }
                if (e && E.IsReady() && target.IsValidTarget(MaxERange))
                {
                    damage += E.GetDamage(target);
                }
                if (r && R.IsReady() && target.IsValidTarget(R.Range + R.Width))
                {
                    damage += R.GetDamage(target);

                    int stacks;
                    if (!IsSpellUpgraded(R))
                    {
                        stacks = target.IsNearTurret(500f) ? 3 : 10;
                        var endTimes =
                            target.Buffs.Where(
                                t =>
                                    t.Type == BuffType.Charm || t.Type == BuffType.Snare || t.Type == BuffType.Knockup ||
                                    t.Type == BuffType.Polymorph || t.Type == BuffType.Fear || t.Type == BuffType.Taunt ||
                                    t.Type == BuffType.Stun).Select(t => t.EndTime).ToList();
                        if (endTimes.Any())
                        {
                            var max = endTimes.Max();
                            if (max - Game.Time > 0.5f)
                            {
                                stacks = 14;
                            }
                        }
                    }
                    else
                    {
                        stacks = 13;
                    }

                    damage += (R.GetDamage(target, 1) * stacks);
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

        private float CalcPassiveDamage(Obj_AI_Base target)
        {
            try
            {
                return
                    (float)
                        Player.CalcDamage(
                            target, Damage.DamageType.Magical,
                            (new[] { 20, 25, 30, 35, 40, 45, 50, 55, 60, 70, 80, 90, 110, 130, 150, 170, 190, 210 }[
                                Player.Level - 1] + Player.TotalMagicalDamage * 0.5f + Player.TotalAttackDamage)) *
                    0.98f;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return 0;
        }

        private void RFollowLogic()
        {
            try
            {
                if (_lastRMoveCommand + RMoveInterval > Environment.TickCount)
                {
                    return;
                }

                _lastRMoveCommand = Environment.TickCount;

                if (!R.Instance.Name.Equals("ViktorChaosStorm", StringComparison.OrdinalIgnoreCase) && _rObject != null &&
                    _rObject.IsValid)
                {
                    var pos = BestRFollowLocation(_rObject.Position);
                    if (!pos.Equals(Vector3.Zero))
                    {
                        R.Cast(pos);
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void RLogicDuel(bool q, bool e)
        {
            try
            {
                foreach (var t in GameObjects.EnemyHeroes)
                {
                    if (UltimateManager.CheckDuel(t, CalcComboDamage(t, q, e, true)))
                    {
                        if (RLogic(t, 1))
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

        private bool RLogic(Obj_AI_Hero target, int min)
        {
            try
            {
                var pred = CPrediction.Circle(R, target, HitChance.High);
                if (pred.TotalHits > 0 && UltimateManager.Check(min, pred.Hits))
                {
                    R.Cast(pred.CastPosition);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return false;
        }

        private void WLogic(Obj_AI_Hero target, HitChance hitChance)
        {
            try
            {
                var pred = W.GetPrediction(target, true);
                if (pred.Hitchance >= hitChance)
                {
                    W.Cast(pred.CastPosition);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private bool ELogic(Obj_AI_Hero target, List<Obj_AI_Hero> targets, HitChance hitChance, int minHits = 1)
        {
            return ELogic(
                target, targets.Select(t => t as Obj_AI_Base).Where(t => t != null).ToList(), hitChance, minHits);
        }

        private bool ELogic(Obj_AI_Hero mainTarget,
            List<Obj_AI_Base> targets,
            HitChance hitChance,
            int minHits,
            float overrideExtendedDistance = -1)
        {
            try
            {
                var input = new PredictionInput
                {
                    Range = ELength,
                    Delay = E.Delay,
                    Radius = E.Width,
                    Speed = E.Speed,
                    Type = E.Type
                };
                var input2 = new PredictionInput
                {
                    Range = MaxERange,
                    Delay = E.Delay,
                    Radius = E.Width,
                    Speed = E.Speed,
                    Type = E.Type
                };
                var startPos = Vector3.Zero;
                var endPos = Vector3.Zero;
                var hits = 0;
                targets = targets.Where(t => t.IsValidTarget(MaxERange * 1.2f)).ToList();
                var targetCount = targets.Count;

                foreach (var target in targets)
                {
                    bool containsTarget;
                    var lTarget = target;
                    if (target.Distance(Player.Position) < E.Range)
                    {
                        containsTarget = mainTarget == null || lTarget.NetworkId == mainTarget.NetworkId;
                        var cCastPos = target.Position;
                        foreach (var t in targets.Where(t => t.NetworkId != lTarget.NetworkId))
                        {
                            var count = 1;
                            var cTarget = t;
                            input.Unit = t;
                            input.From = cCastPos;
                            input.RangeCheckFrom = cCastPos;
                            var pred = Prediction.GetPrediction(input);
                            if (pred.Hitchance >= (hitChance - 1))
                            {
                                count++;
                                if (!containsTarget)
                                {
                                    containsTarget = t.NetworkId == mainTarget.NetworkId;
                                }
                                var rect = new Geometry.Polygon.Rectangle(
                                    cCastPos.To2D(), cCastPos.Extend(pred.CastPosition, ELength).To2D(), E.Width);
                                foreach (var c in
                                    targets.Where(
                                        c => c.NetworkId != cTarget.NetworkId && c.NetworkId != lTarget.NetworkId))
                                {
                                    input.Unit = c;
                                    var cPredPos = c.Type == GameObjectType.obj_AI_Minion
                                        ? c.Position
                                        : Prediction.GetPrediction(input).UnitPosition;
                                    if (
                                        new Geometry.Polygon.Circle(
                                            cPredPos,
                                            (c.Type == GameObjectType.obj_AI_Minion && c.IsMoving
                                                ? (c.BoundingRadius / 2f)
                                                : (c.BoundingRadius) * 0.9f)).Points.Any(p => rect.IsInside(p)))
                                    {
                                        count++;
                                        if (!containsTarget && c.NetworkId == mainTarget.NetworkId)
                                        {
                                            containsTarget = true;
                                        }
                                    }
                                }
                                if (count > hits && containsTarget)
                                {
                                    hits = count;
                                    startPos = cCastPos;
                                    endPos = cCastPos.Extend(pred.CastPosition, ELength);
                                    if (hits == targetCount)
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                        if (endPos.Equals(Vector3.Zero) && containsTarget)
                        {
                            startPos = target.IsFacing(Player) && IsSpellUpgraded(E)
                                ? Player.Position.Extend(cCastPos, Player.Distance(cCastPos) - (ELength / 10f))
                                : cCastPos;
                            endPos = Player.Position.Extend(cCastPos, ELength);
                            hits = 1;
                        }
                    }
                    else
                    {
                        input2.Unit = lTarget;
                        var castPos = Prediction.GetPrediction(input2).CastPosition;
                        var sCastPos = Player.Position.Extend(castPos, E.Range);

                        var extDist = overrideExtendedDistance > 0 ? overrideExtendedDistance : (ELength / 4f);
                        var circle =
                            new Geometry.Polygon.Circle(Player.Position, sCastPos.Distance(Player.Position), 45).Points
                                .Where(p => p.Distance(sCastPos) < extDist).OrderBy(p => p.Distance(lTarget));
                        foreach (var point in circle)
                        {
                            input2.From = point.To3D();
                            input2.RangeCheckFrom = point.To3D();
                            input2.Range = ELength;
                            var pred2 = Prediction.GetPrediction(input2);
                            if (pred2.Hitchance >= hitChance)
                            {
                                containsTarget = mainTarget == null || lTarget.NetworkId == mainTarget.NetworkId;
                                var count = 1;
                                var rect = new Geometry.Polygon.Rectangle(
                                    point, point.To3D().Extend(pred2.CastPosition, ELength).To2D(), E.Width);
                                foreach (var c in targets.Where(t => t.NetworkId != lTarget.NetworkId))
                                {
                                    input2.Unit = c;
                                    var cPredPos = c.Type == GameObjectType.obj_AI_Minion
                                        ? c.Position
                                        : Prediction.GetPrediction(input2).UnitPosition;
                                    if (
                                        new Geometry.Polygon.Circle(
                                            cPredPos,
                                            (c.Type == GameObjectType.obj_AI_Minion && c.IsMoving
                                                ? (c.BoundingRadius / 2f)
                                                : (c.BoundingRadius) * 0.9f)).Points.Any(p => rect.IsInside(p)))
                                    {
                                        count++;
                                        if (!containsTarget && c.NetworkId == mainTarget.NetworkId)
                                        {
                                            containsTarget = true;
                                        }
                                    }
                                }
                                if (count > hits && containsTarget)
                                {
                                    hits = count;
                                    startPos = point.To3D();
                                    endPos = startPos.Extend(pred2.CastPosition, ELength);
                                    if (hits == targetCount)
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    if (hits == targetCount)
                    {
                        break;
                    }
                }
                if (hits >= minHits && !startPos.Equals(Vector3.Zero) && !endPos.Equals(Vector3.Zero))
                {
                    E.Cast(startPos, endPos);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return false;
        }

        protected override void Harass()
        {
            if (!ManaManager.Check("harass"))
            {
                return;
            }

            if (Menu.Item(Menu.Name + ".harass.q").GetValue<bool>())
            {
                Casting.TargetSkill(Q);
            }
            if (Menu.Item(Menu.Name + ".harass.e").GetValue<bool>())
            {
                var target = TargetSelector.GetTarget(
                    MaxERange * 1.2f, LeagueSharp.Common.TargetSelector.DamageType.Magical);
                if (target != null)
                {
                    ELogic(target, GameObjects.EnemyHeroes.ToList(), E.GetHitChance("harass"));
                }
            }
        }

        protected override void LaneClear()
        {
            if (Menu.Item(Menu.Name + ".lane-clear.e").GetValue<bool>() && E.IsReady() &&
                ManaManager.Check("lane-clear-e"))
            {
                var minions = MinionManager.GetMinions(
                    MaxERange * 1.5f, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
                var minHits = minions.Any(m => m.Team == GameObjectTeam.Neutral)
                    ? 1
                    : Menu.Item(Menu.Name + ".lane-clear.e-min").GetValue<Slider>().Value;

                if (minions.Count >= minHits)
                {
                    ELogic(null, (minions.Concat(GameObjects.EnemyHeroes)).ToList(), HitChance.High, minHits);
                }
            }
            if (Menu.Item(Menu.Name + ".lane-clear.q").GetValue<bool>() && Q.IsReady() &&
                ManaManager.Check("lane-clear-q"))
            {
                var minion =
                    MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth)
                        .FirstOrDefault(m => m.Health < Q.GetDamage(m) || m.Health * 2 > Q.GetDamage(m));
                if (minion != null)
                {
                    Casting.TargetSkill(minion, Q);
                }
            }
        }

        protected override void Flee()
        {
            if (Menu.Item(Menu.Name + ".flee.w").GetValue<bool>() && W.IsReady())
            {
                var near =
                    GameObjects.EnemyHeroes.Where(e => W.CanCast(e))
                        .OrderBy(e => e.Distance(Player.Position))
                        .FirstOrDefault();
                if (near != null)
                {
                    Casting.SkillShot(near, W, W.GetHitChance("combo"));
                }
            }
            if (Menu.Item(Menu.Name + ".flee.q-upgraded").GetValue<bool>() && Q.IsReady() && IsSpellUpgraded(Q))
            {
                var near =
                    GameObjects.EnemyHeroes.Where(e => Q.CanCast(e))
                        .OrderBy(e => e.Distance(Player.Position))
                        .FirstOrDefault();
                if (near != null)
                {
                    Casting.TargetSkill(near, Q);
                }
                else
                {
                    var mobs = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly);
                    if (mobs.Any())
                    {
                        Casting.TargetSkill(mobs.First(), Q);
                    }
                }
            }
        }

        protected override void Killsteal()
        {
            if (Menu.Item(Menu.Name + ".killsteal.q-aa").GetValue<bool>() && Q.IsReady())
            {
                var target =
                    GameObjects.EnemyHeroes.FirstOrDefault(t => t.IsValidTarget() && Orbwalking.InAutoAttackRange(t));
                if (target != null)
                {
                    var damage = CalcPassiveDamage(target) + Q.GetDamage(target);
                    if (damage - 10 > target.Health)
                    {
                        Casting.TargetSkill(target, Q);
                        Orbwalker.ForceTarget(target);
                    }
                }
            }
            if (Menu.Item(Menu.Name + ".killsteal.e").GetValue<bool>() && E.IsReady())
            {
                foreach (var target in
                    GameObjects.EnemyHeroes.Where(t => t.IsValidTarget() && t.Distance(Player) < MaxERange))
                {
                    var damage = E.GetDamage(target);
                    if (damage - 10 > target.Health)
                    {
                        if (ELogic(target, GameObjects.EnemyHeroes.ToList(), E.GetHitChance("combo")))
                        {
                            break;
                        }
                    }
                }
            }
        }

        private Vector3 BestRFollowLocation(Vector3 position)
        {
            try
            {
                var center = Vector2.Zero;
                float radius = -1;
                var count = 0;
                var moveDistance = -1f;
                var maxRelocation = IsSpellUpgraded(R) ? R.Width * 1.2f : R.Width * 0.8f;
                var targets = GameObjects.EnemyHeroes.Where(t => t.IsValidTarget(1500f)).ToList();
                if (targets.Any())
                {
                    var possibilities =
                        ListExtensions.ProduceEnumeration(targets.Select(t => t.Position.To2D()).ToList())
                            .Where(p => p.Count > 1)
                            .ToList();
                    if (possibilities.Any())
                    {
                        foreach (var possibility in possibilities)
                        {
                            var mec = MEC.GetMec(possibility);
                            var distance = position.Distance(mec.Center.To3D());
                            if (mec.Radius < (R.Width / 2) && distance < maxRelocation)
                            {
                                if (possibility.Count > count ||
                                    possibility.Count == count && (mec.Radius < radius || distance < moveDistance))
                                {
                                    moveDistance = position.Distance(mec.Center.To3D());
                                    center = mec.Center;
                                    radius = mec.Radius;
                                    count = possibility.Count;
                                }
                            }
                        }
                        if (!center.Equals(Vector2.Zero))
                        {
                            return center.To3D();
                        }
                    }
                    var dTarget = targets.OrderBy(t => t.Distance(position)).FirstOrDefault();
                    if (dTarget != null)
                    {
                        return dTarget.Position;
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return Vector3.Zero;
        }
    }
}