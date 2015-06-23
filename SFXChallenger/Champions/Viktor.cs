#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Viktor.cs is part of SFXChallenger.

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

#endregion

namespace SFXChallenger.Champions
{
    internal class Viktor : TChampion
    {
        private const float MaxERange = 1225f;
        private const float ELength = 700f;
        private const float RMoveInterval = 325f;
        private float _lastRMoveCommand = Environment.TickCount;
        private GameObject _rObject;
        public Viktor() : base(1500f) {}

        protected override ItemFlags ItemFlags
        {
            get { return ItemFlags.Offensive | ItemFlags.Defensive | ItemFlags.Flee; }
        }

        protected override void OnLoad()
        {
            Core.OnPostUpdate += OnCorePostUpdate;
            Orbwalking.BeforeAttack += OnOrbwalkingBeforeAttack;
            Orbwalking.AfterAttack += OnOrbwalkingAfterAttack;
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
                laneclearMenu, "lane-clear-q", ManaCheckType.Minimum, ManaValueType.Total, 200, 0, 750);
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));
            ManaManager.AddToMenu(laneclearMenu, "lane-clear-e", ManaCheckType.Minimum, ManaValueType.Percent);
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(true));
            laneclearMenu.AddItem(
                new MenuItem(laneclearMenu.Name + ".e-min", "E " + Global.Lang.Get("G_Min")).SetValue(
                    new Slider(3, 1, 5)));

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
                new MenuItem(uAutoMenu.Name + ".min", Global.Lang.Get("G_Min")).SetValue(new Slider(3, 1, 5)));
            uAutoMenu.AddItem(new MenuItem(uAutoMenu.Name + ".1v1", "R 1v1").SetValue(true));
            uAutoMenu.AddItem(new MenuItem(uAutoMenu.Name + ".enabled", Global.Lang.Get("G_Enabled")).SetValue(true));

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
            miscMenu.AddItem(
                new MenuItem(miscMenu.Name + ".w-slowed", "W " + Global.Lang.Get("G_Slowed")).SetValue(true));
            miscMenu.AddItem(
                new MenuItem(miscMenu.Name + ".w-stunned", "W " + Global.Lang.Get("G_Stunned")).SetValue(true));
            miscMenu.AddItem(new MenuItem(miscMenu.Name + ".w-dash", "W " + Global.Lang.Get("G_Dash")).SetValue(true));
            miscMenu.AddItem(
                new MenuItem(miscMenu.Name + ".w-gapcloser", "W " + Global.Lang.Get("G_Gapcloser")).SetValue(true));

            DrawingManager.Add("Combo Damage", new Circle(true, DamageIndicator.DrawingColor)).ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs args)
                {
                    DamageIndicator.Enabled = args.GetNewValue<Circle>().Active;
                    DamageIndicator.DrawingColor = args.GetNewValue<Circle>().Color;
                };

            DamageIndicator.Initialize(CalcComboDamage);
            DamageIndicator.Enabled = DrawingManager.Get("Combo Damage").GetValue<Circle>().Active;
            DamageIndicator.DrawingColor = DrawingManager.Get("Combo Damage").GetValue<Circle>().Color;
        }

        protected override void SetupSpells()
        {
            Q = new Spell(SpellSlot.Q, 600f);
            Q.SetTargetted(0.3f, 2000f);

            W = new Spell(SpellSlot.W, 700f);
            W.SetSkillshot(1.6f, 300f, float.MaxValue, false, SkillshotType.SkillshotCircle);

            E = new Spell(SpellSlot.E, 525f);
            E.SetSkillshot(0f, 90f, 800f, false, SkillshotType.SkillshotLine);

            R = new Spell(SpellSlot.R, 700f);
            R.SetSkillshot(0.75f, 575f, float.MaxValue, false, SkillshotType.SkillshotCircle);
        }

        private void OnCorePostUpdate(EventArgs args)
        {
            try
            {
                if (Menu.Item(Menu.Name + ".ultimate.follow").GetValue<bool>())
                {
                    RFollowLogic();
                }
                if (Menu.Item(Menu.Name + ".ultimate.assisted.enabled").GetValue<bool>() &&
                    Menu.Item(Menu.Name + ".ultimate.assisted.hotkey").GetValue<KeyBind>().Active && R.IsReady())
                {
                    if (Menu.Item(Menu.Name + ".ultimate.assisted.move-cursor").GetValue<bool>())
                    {
                        Orbwalking.MoveTo(Game.CursorPos, Orbwalker.HoldAreaRadius);
                    }

                    if (!RLogic(Menu.Item(Menu.Name + ".ultimate.assisted.min").GetValue<Slider>().Value))
                    {
                        if (Menu.Item(Menu.Name + ".ultimate.assisted.1v1").GetValue<bool>())
                        {
                            RLogic1V1(
                                Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady(),
                                Menu.Item(Menu.Name + ".combo.e").GetValue<bool>() && E.IsReady());
                        }
                    }
                }

                if (Menu.Item(Menu.Name + ".ultimate.auto.enabled").GetValue<bool>() && R.IsReady())
                {
                    if (!RLogic(Menu.Item(Menu.Name + ".ultimate.auto.min").GetValue<Slider>().Value))
                    {
                        if (Menu.Item(Menu.Name + ".ultimate.auto.1v1").GetValue<bool>())
                        {
                            RLogic1V1(
                                Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady(),
                                Menu.Item(Menu.Name + ".combo.e").GetValue<bool>() && E.IsReady());
                        }
                    }
                }

                if (Menu.Item(Menu.Name + ".miscellaneous.w-stunned").GetValue<bool>() && W.IsReady())
                {
                    var target =
                        Targets.FirstOrDefault(
                            t =>
                                t.IsValidTarget(W.Range) &&
                                (t.HasBuffOfType(BuffType.Charm) || t.HasBuffOfType(BuffType.Snare) ||
                                 t.HasBuffOfType(BuffType.Knockup) || t.HasBuffOfType(BuffType.Polymorph) ||
                                 t.HasBuffOfType(BuffType.Fear) || t.HasBuffOfType(BuffType.Taunt) ||
                                 t.HasBuffOfType(BuffType.Stun) || t.IsStunned ||
                                 W.GetPrediction(t).Hitchance == HitChance.Immobile));
                    if (target != null)
                    {
                        Casting.SkillShot(target, W, W.GetHitChance("combo"));
                    }
                }

                if (Menu.Item(Menu.Name + ".miscellaneous.w-slowed").GetValue<bool>() && W.IsReady())
                {
                    var target =
                        Targets.FirstOrDefault(
                            t =>
                                t.IsValidTarget(W.Range) &&
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

        private bool IsSpellUpgraded(Spell spell)
        {
            try
            {
                return
                    Player.Buffs.Select(b => b.Name.ToLower())
                        .Where(b => b.Contains("viktor") && b.Contains("aug"))
                        .Select(b => b.Replace("viktor", string.Empty).Replace("aug", string.Empty))
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
                if (Player.HasBuff("viktorpowertransferreturn"))
                {
                    if (args.Target.Type != GameObjectType.obj_AI_Hero)
                    {
                        var hero = Targets.FirstOrDefault(Orbwalking.InAutoAttackRange);
                        if (hero != null)
                        {
                            args.Process = false;
                            Orbwalker.ForceTarget(hero);
                            return;
                        }
                    }
                    var target = args.Target as Obj_AI_Minion;
                    if (target != null)
                    {
                        var health = HealthPrediction.GetHealthPrediction(target, (int) ((Player.AttackDelay * 1000)));
                        if (health * 2 > CalcPassiveDamage(target) || health < CalcPassiveDamage(target))
                        {
                            args.Process = true;
                            return;
                        }

                        var minions =
                            MinionManager.GetMinions(
                                1000, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth)
                                .Where(Orbwalking.InAutoAttackRange)
                                .ToList();

                        var killable =
                            minions.FirstOrDefault(
                                m =>
                                    HealthPrediction.GetHealthPrediction(m, (int) ((Player.AttackDelay * 1000))) <
                                    CalcPassiveDamage(m));
                        if (killable != null)
                        {
                            Orbwalker.ForceTarget(killable);
                        }
                        else
                        {
                            var other =
                                minions.FirstOrDefault(
                                    m =>
                                        HealthPrediction.GetHealthPrediction(m, (int) ((Player.AttackDelay * 1000))) * 2 >
                                        CalcPassiveDamage(m));
                            if (other != null)
                            {
                                Orbwalker.ForceTarget(other);
                            }
                        }
                        args.Process = false;
                        return;
                    }
                    args.Process = true;
                    return;
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

                var endPos = args.EndPos;
                if (hero.ChampionName.Equals("Fizz", StringComparison.OrdinalIgnoreCase))
                {
                    endPos = args.StartPos.Extend(endPos, 550);
                }
                if (hero.ChampionName.Equals("LeBlanc", StringComparison.CurrentCultureIgnoreCase))
                {
                    endPos = args.StartPos.Distance(Player) < W.Range ? args.StartPos : endPos;
                }
                if (Menu.Item(Menu.Name + ".miscellaneous.w-dash").GetValue<bool>())
                {
                    if (endPos.Distance(Player.Position) < W.Range)
                    {
                        W.Cast(endPos);
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
                    sender.IsFacing(Player))
                {
                    R.Cast(sender);
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

                var endPos = args.End;
                if (args.Sender.ChampionName.Equals("Fizz", StringComparison.OrdinalIgnoreCase))
                {
                    endPos = args.Start.Extend(endPos, 550);
                }

                if (Menu.Item(Menu.Name + ".miscellaneous.w-gapcloser").GetValue<bool>())
                {
                    if (endPos.Distance(Player.Position) < W.Range)
                    {
                        W.Cast(endPos);
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
            var r = Menu.Item(Menu.Name + ".ultimate.combo.enabled").GetValue<bool>() && R.IsReady();
            var extended = false;

            if (q)
            {
                QLogic();
            }
            if (w)
            {
                WLogic(W.GetHitChance("combo"));
            }
            if (e)
            {
                ELogic(Targets, E.GetHitChance("combo"));
            }
            if (r)
            {
                if (!RLogic(Menu.Item(Menu.Name + ".ultimate.combo.min").GetValue<Slider>().Value))
                {
                    if (Menu.Item(Menu.Name + ".ultimate.combo.1v1").GetValue<bool>())
                    {
                        extended = RLogic1V1(q, e);
                    }
                }
            }
            var target = Targets.FirstOrDefault(t => t.IsValidTarget(R.Range));
            if (target != null && CalcComboDamage(target, q, e, r, extended) * 1.3 > target.Health)
            {
                ItemManager.UseComboItems(target);
                SummonerManager.UseComboSummoners(target);
            }
        }

        private float CalcComboDamage(Obj_AI_Base target)
        {
            return CalcComboDamage(target, true, true, true, false);
        }

        private float CalcComboDamage(Obj_AI_Base target, bool q, bool e, bool r, bool extended)
        {
            try
            {
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
                if (r && R.IsReady() && target.IsValidTarget(extended ? R.Range + (R.Width * 0.45f) : R.Range))
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
                        stacks = extended ? 12 : 14;
                    }

                    damage += (R.GetDamage(target, 1) * stacks);
                }
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

        private bool RLogic1V1(bool q, bool e)
        {
            try
            {
                if (!R.IsReady())
                {
                    return false;
                }
                if (GameObjects.EnemyHeroes.Count(em => !em.IsDead && em.IsVisible && em.Distance(Player) < 3000) == 1)
                {
                    var extended = false;
                    var extendedRange = R.Range + (R.Width * 0.4f);
                    var targets = Targets.Where(t => t.HealthPercent > 20 && R.CanCast(t)).ToList();
                    if (!targets.Any())
                    {
                        targets =
                            Targets.Where(
                                t =>
                                    t.HealthPercent > 20 && t.IsFacing(Player)
                                        ? t.IsValidTarget(extendedRange)
                                        : R.GetPrediction(t, true, extendedRange).Hitchance >= HitChance.High).ToList();
                        extended = targets.Any();
                    }

                    foreach (var target in targets)
                    {
                        var cDmg = CalcComboDamage(target, q, e, false, extended);
                        if (cDmg - 20 >= target.Health)
                        {
                            return extended;
                        }
                        cDmg = CalcComboDamage(target, q, e, true, extended);
                        if (cDmg - 20 >= target.Health)
                        {
                            Vector3 center;
                            int hits;
                            BestRCastLocation(out center, out hits, extended ? extendedRange : -1);
                            if (hits >= 1 && !center.Equals(Vector3.Zero))
                            {
                                R.Cast(extended ? (Player.Position.Extend(center, R.Range)) : center);
                                return extended;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return false;
        }

        private bool RLogic(int min)
        {
            try
            {
                Vector3 center;
                int hits;
                BestRCastLocation(out center, out hits);
                if (hits >= min && !center.Equals(Vector3.Zero))
                {
                    R.Cast(center);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return false;
        }

        private void QLogic()
        {
            try
            {
                var ts = Targets.FirstOrDefault(t => Q.CanCast(t));
                if (ts != null)
                {
                    Casting.TargetSkill(ts, Q);
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
                var ts =
                    Targets.Where(t => W.CanCast(t))
                        .OrderByDescending(
                            t =>
                                GameObjects.EnemyHeroes.Count(
                                    e => W.WillHit(e.Position, W.GetPrediction(t, true).CastPosition)))
                        .FirstOrDefault();
                if (ts != null)
                {
                    var pred = W.GetPrediction(ts, true);
                    if (pred.Hitchance >= hitChance)
                    {
                        W.Cast(pred.CastPosition);
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private bool ELogic(List<Obj_AI_Hero> targets, HitChance hitChance, int minHits = 1)
        {
            return ELogic(targets.Select(t => t as Obj_AI_Base).Where(t => t != null).ToList(), hitChance, minHits);
        }

        private bool ELogic(List<Obj_AI_Base> targets,
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
                targets = targets.Where(t => t.Distance(Player) < MaxERange * 1.5f).ToList();
                var targetCount = targets.Count;

                foreach (var target in targets)
                {
                    var lTarget = target;
                    if (target.Distance(Player.Position) < E.Range)
                    {
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
                                    }
                                }
                                if (count > hits)
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
                        if (endPos.Equals(Vector3.Zero))
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
                        input2.Unit = target;
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
                                    }
                                }
                                if (count > hits)
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
                QLogic();
            }
            if (Menu.Item(Menu.Name + ".harass.e").GetValue<bool>())
            {
                ELogic(Targets, E.GetHitChance("harass"));
            }
        }

        protected override void LaneClear()
        {
            if (Menu.Item(Menu.Name + ".lane-clear.e").GetValue<bool>() && E.IsReady() &&
                ManaManager.Check("lane-clear-e"))
            {
                var minions = MinionManager.GetMinions(
                    MaxERange * 1.3f, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
                var minHits = minions.Any(m => m.Team == GameObjectTeam.Neutral)
                    ? 1
                    : Menu.Item(Menu.Name + ".lane-clear.e-min").GetValue<Slider>().Value;

                if (minions.Count >= minHits)
                {
                    ELogic((minions.Concat(Targets)).ToList(), HitChance.High, minHits);
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
            }
        }

        protected override void Killsteal()
        {
            if (Menu.Item(Menu.Name + ".killsteal.q-aa").GetValue<bool>() && Q.IsReady())
            {
                foreach (var target in Targets.Where(Orbwalking.InAutoAttackRange))
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
                foreach (var target in Targets.Where(t => t.Distance(Player) < MaxERange))
                {
                    var damage = E.GetDamage(target);
                    if (damage - 10 > target.Health)
                    {
                        if (ELogic(new List<Obj_AI_Hero> { target }, E.GetHitChance("combo")))
                        {
                            break;
                        }
                    }
                }
            }
        }

        private void BestRCastLocation(out Vector3 pos, out int hits, float overrideRange = -1)
        {
            var center = Vector2.Zero;
            float radius = -1;
            var count = 0;
            var range = (overrideRange > 0 ? overrideRange : R.Range);
            var targets = Targets.Where(t => t.IsValidTarget(range)).ToList();
            if (targets.Any())
            {
                var possiblities =
                    ListExtensions.ProduceEnumeration(targets.Select(t => t.Position.To2D()).ToList())
                        .Where(p => p.Count > 1)
                        .ToList();
                if (possiblities.Any())
                {
                    foreach (var possibility in possiblities)
                    {
                        Vector2 lCenter;
                        float lRadius;
                        ConvexHull.FindMinimalBoundingCircle(possibility, out lCenter, out lRadius);
                        if (lRadius < (R.Width / 2) && Player.Distance(lCenter) < range)
                        {
                            if (possibility.Count > count || possibility.Count == count && lRadius < radius)
                            {
                                center = lCenter;
                                radius = lRadius;
                                count = possibility.Count;
                            }
                        }
                    }
                    if (!center.Equals(Vector2.Zero))
                    {
                        hits = count;
                        pos = center.To3D();
                        return;
                    }
                }
                var dTarget = targets.FirstOrDefault();
                if (dTarget != null)
                {
                    hits = 1;
                    pos = dTarget.Position;
                    return;
                }
            }
            hits = 0;
            pos = Vector3.Zero;
        }

        private Vector3 BestRFollowLocation(Vector3 position)
        {
            var center = Vector2.Zero;
            float radius = -1;
            var count = 0;
            var moveDistance = -1f;
            var maxRelocation = IsSpellUpgraded(R) ? R.Width * 1.2f : R.Width * 0.8f;
            var targets = Targets.Where(t => t.IsValidTarget()).ToList();
            if (targets.Any())
            {
                var possiblities =
                    ListExtensions.ProduceEnumeration(targets.Select(t => t.Position.To2D()).ToList())
                        .Where(p => p.Count > 1)
                        .ToList();
                if (possiblities.Any())
                {
                    foreach (var possibility in possiblities)
                    {
                        Vector2 lCenter;
                        float lRadius;
                        ConvexHull.FindMinimalBoundingCircle(possibility, out lCenter, out lRadius);
                        var distance = position.Distance(lCenter.To3D());
                        if (lRadius < (R.Width / 2) && distance < maxRelocation)
                        {
                            if (possibility.Count > count ||
                                possibility.Count == count && (lRadius < radius || distance < moveDistance))
                            {
                                moveDistance = position.Distance(lCenter.To3D());
                                center = lCenter;
                                radius = lRadius;
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
            return Vector3.Zero;
        }
    }
}