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
using SFXLibrary.Extensions.SharpDX;
using SFXLibrary.Logger;
using SharpDX;
using Color = System.Drawing.Color;
using Orbwalking = SFXChallenger.Wrappers.Orbwalking;

#endregion

namespace SFXChallenger.Champions
{
    internal class Viktor : Champion
    {
        private const float MaxERange = 1245f;
        private const float ELength = 710f;
        private const float RMoveInterval = 125f;
        private float _lastRMoveCommand = Environment.TickCount;
        private GameObject _rObject;
        private GameObject _wObject;
        private float _wObjectEndTime;

        protected override ItemFlags ItemFlags
        {
            get { return ItemFlags.Offensive | ItemFlags.Defensive | ItemFlags.Flee; }
        }

        protected override void OnLoad()
        {
            Core.OnPostUpdate += OnCorePostUpdate;
            Orbwalking.BeforeAttack += OnOrbwalkingBeforeAttack;
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
            Orbwalking.OnNonKillableMinion -= OnOrbwalkingNonKillableMinion;
            AntiGapcloser.OnEnemyGapcloser -= OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget -= OnInterruptableTarget;
            CustomEvents.Unit.OnDash -= OnUnitDash;
            GameObject.OnCreate -= OnGameObjectCreate;
        }

        protected override void AddToMenu()
        {
            var drawingMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Drawing"), Menu.Name + ".drawing"));
            drawingMenu.AddItem(
                new MenuItem(drawingMenu.Name + ".circle-thickness", Global.Lang.Get("G_CircleThickness")).SetValue(
                    new Slider(2, 0, 10)));
            drawingMenu.AddItem(new MenuItem(drawingMenu.Name + ".q", "Q").SetValue(new Circle(false, Color.White)));
            drawingMenu.AddItem(new MenuItem(drawingMenu.Name + ".w", "W").SetValue(new Circle(false, Color.White)));
            drawingMenu.AddItem(new MenuItem(drawingMenu.Name + ".e", "E").SetValue(new Circle(false, Color.White)));
            drawingMenu.AddItem(new MenuItem(drawingMenu.Name + ".r", "R").SetValue(new Circle(false, Color.White)));

            var comboMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Combo"), Menu.Name + ".combo"));
            HitchanceManager.AddToMenu(
                comboMenu.AddSubMenu(new Menu(Global.Lang.Get("F_MH"), comboMenu.Name + ".hitchance")), "combo",
                new Dictionary<string, int> { { "W", 2 }, { "E", 2 }, { "R", 2 } });
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
            ManaManager.AddToMenu(laneclearMenu, "lane-clear", ManaCheckType.Minimum, ManaValueType.Percent);
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));
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
            foreach (var enemy in HeroManager.Enemies)
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
                new MenuItem(miscMenu.Name + ".q-unkillable", "Q " + Global.Lang.Get("G_Unkillable")).SetValue(false));
            miscMenu.AddItem(
                new MenuItem(miscMenu.Name + ".w-stunned", "W " + Global.Lang.Get("G_Stunned")).SetValue(true));
            miscMenu.AddItem(new MenuItem(miscMenu.Name + ".w-dash", "W " + Global.Lang.Get("G_Dash")).SetValue(true));
            miscMenu.AddItem(
                new MenuItem(miscMenu.Name + ".w-gapcloser", "W " + Global.Lang.Get("G_Dash")).SetValue(true));
        }

        protected override void SetupSpells()
        {
            Q = new Spell(SpellSlot.Q, 600f);
            Q.SetTargetted(0.25f, 2000f);

            W = new Spell(SpellSlot.W, 700f);
            W.SetSkillshot(1.6f, 300f, float.MaxValue, false, SkillshotType.SkillshotCircle);

            E = new Spell(SpellSlot.E, 525f);
            E.SetSkillshot(0.05f, 90f, 1000f, false, SkillshotType.SkillshotLine);

            R = new Spell(SpellSlot.R, 700f);
            R.SetSkillshot(0.05f, 450f, float.MaxValue, false, SkillshotType.SkillshotCircle);
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

                    if (
                        !RLogic(
                            HitchanceManager.Get("combo", "r"),
                            Menu.Item(Menu.Name + ".ultimate.assisted.min").GetValue<Slider>().Value))
                    {
                        if (Menu.Item(Menu.Name + ".ultimate.assisted.1v1").GetValue<bool>())
                        {
                            RLogic1V1(
                                HitchanceManager.Get("combo", "r"),
                                Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady(),
                                Menu.Item(Menu.Name + ".combo.e").GetValue<bool>() && E.IsReady());
                        }
                    }
                }

                if (Menu.Item(Menu.Name + ".ultimate.auto.enabled").GetValue<bool>() && R.IsReady())
                {
                    if (
                        !RLogic(
                            HitchanceManager.Get("combo", "r"),
                            Menu.Item(Menu.Name + ".ultimate.auto.min").GetValue<Slider>().Value))
                    {
                        if (Menu.Item(Menu.Name + ".ultimate.auto.1v1").GetValue<bool>())
                        {
                            RLogic1V1(
                                HitchanceManager.Get("combo", "r"),
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
                        Casting.BasicSkillShot(target, W, HitchanceManager.Get("harass", "w"));
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
                if (sender.Type == GameObjectType.obj_GeneralParticleEmitter)
                {
                    if (sender.Name.Equals("Viktor_base_W_AUG_green.troy", StringComparison.OrdinalIgnoreCase))
                    {
                        _wObject = sender;
                        _wObjectEndTime = Game.Time + 4f;
                    }
                    else if (sender.Name.Equals("Viktor_ChaosStorm_green.troy", StringComparison.OrdinalIgnoreCase))
                    {
                        _rObject = sender;
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnOrbwalkingNonKillableMinion(AttackableUnit minion)
        {
            try
            {
                if (Menu.Item(Menu.Name + ".miscellaneous.q-unkillable").GetValue<bool>())
                {
                    var m = minion as Obj_AI_Minion;
                    if (m != null)
                    {
                        QLastHitLogic(m);
                    }
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
                    var target = args.Target as Obj_AI_Minion;
                    if (target != null)
                    {
                        if (target.Health * 2 > CalcPassiveDamage(target) || target.Health < CalcPassiveDamage(target))
                        {
                            args.Process = true;
                            return;
                        }
                        var minion =
                            MinionManager.GetMinions(
                                1000, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth)
                                .FirstOrDefault(
                                    m =>
                                        Orbwalker.InAutoAttackRange(m) && m.Health * 2 > CalcPassiveDamage(m) ||
                                        m.Health < CalcPassiveDamage(m));
                        if (minion != null)
                        {
                            Orbwalker.ForceTarget(minion);
                        }
                        args.Process = false;
                        return;
                    }
                    args.Process = true;
                    return;
                }
                Orbwalker.ForceTarget(null);
                if (args.Target.Type == GameObjectType.obj_AI_Hero &&
                    (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo ||
                     Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Harass))
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
                    Casting.BasicSkillShot(sender, R, HitchanceManager.Get("combo", "r"));
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

            if (q)
            {
                QLogic();
            }
            if (w)
            {
                WLogic(HitchanceManager.Get("combo", "w"));
            }
            if (e)
            {
                ELogic(HitchanceManager.Get("combo", "e"));
            }
            if (r)
            {
                if (
                    !RLogic(
                        HitchanceManager.Get("combo", "r"),
                        Menu.Item(Menu.Name + ".ultimate.combo.min").GetValue<Slider>().Value))
                {
                    if (Menu.Item(Menu.Name + ".ultimate.combo.1v1").GetValue<bool>())
                    {
                        RLogic1V1(HitchanceManager.Get("combo", "r"), q, e);
                    }
                }
            }
            var target = Targets.FirstOrDefault(t => t.IsValidTarget(R.Range));
            if (target != null && CalcComboDamage(target, q, e, r) * 1.3 > target.Health)
            {
                ItemManager.UseComboItems(target);
                SummonerManager.UseComboSummoners(target);
            }
        }

        private float CalcComboDamage(Obj_AI_Base target, bool q, bool e, bool r)
        {
            try
            {
                var damage = 0f;
                if (q && Q.IsReady())
                {
                    damage += Q.GetDamage(target) * 2;
                    if (Orbwalker.InAutoAttackRange(target))
                    {
                        damage += CalcPassiveDamage(target);
                    }
                }
                if (e && E.IsReady())
                {
                    damage += E.GetDamage(target);
                }
                if (r && R.IsReady())
                {
                    damage += R.GetDamage(target);
                    damage += R.GetDamage(target, 1) * 10;
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
                        target.CalcDamage(
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
                    var insideR = Targets.Where(t => t.Distance(_rObject.Position) < R.Width).ToList();
                    if (insideR.Any())
                    {
                        R.Cast(insideR.First().Position);
                    }
                    else
                    {
                        var near = Targets.OrderBy(t => t.Distance(_rObject.Position)).FirstOrDefault();
                        if (near != null)
                        {
                            R.Cast(near.Position);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void RLogic1V1(HitChance hitChance, bool q, bool e)
        {
            try
            {
                foreach (var target in Targets.Where(t => t.HealthPercent > 25 && !t.IsNearTurret() && R.CanCast(t)))
                {
                    var cDmg = CalcComboDamage(target, q, e, true);
                    if (cDmg - 20 >= target.Health)
                    {
                        if (HeroManager.Enemies.Count(em => !em.IsDead && em.IsVisible && em.Distance(Player) < 3000) ==
                            1)
                        {
                            Casting.BasicSkillShot(target, R, hitChance);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private bool RLogic(HitChance hitChance, int min)
        {
            try
            {
                foreach (var target in Targets)
                {
                    var pred = R.GetPrediction(target, true);
                    if (pred.Hitchance >= hitChance)
                    {
                        var hits = HeroManager.Enemies.Count(x => R.WillHit(x.Position, pred.CastPosition));
                        if (hits >= min)
                        {
                            R.Cast(pred.CastPosition);
                            return true;
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

        private void QLogic()
        {
            try
            {
                var ts = Targets.FirstOrDefault(t => Q.CanCast(t));
                if (ts != null)
                {
                    Casting.BasicTargetSkill(ts, Q);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void QLastHitLogic(Obj_AI_Minion minion)
        {
            try
            {
                if (minion.IsValid && Q.CanCast(minion))
                {
                    var health = HealthPrediction.GetHealthPrediction(minion, (int) (Q.GetSpellDelay(minion)), 0);
                    if (health > 0 && Q.IsKillable(minion))
                    {
                        Casting.BasicTargetSkill(minion, Q);
                    }
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
                                HeroManager.Enemies.Count(
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

        private void ELogic(HitChance hitChance)
        {
            try
            {
                foreach (var target in Targets)
                {
                    if (SingleELogic(target, hitChance))
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private bool SingleELogic(Obj_AI_Hero target, HitChance hitChance)
        {
            try
            {
                Vector3 startPos, endPos = Vector3.Zero;
                var lTarget = target;

                if (target.Distance(Player.Position) < E.Range)
                {
                    startPos = target.Position;
                    if (UseWObjectPosition(target, true))
                    {
                        endPos = startPos.Extend(_wObject.Position, ELength);
                    }
                    else
                    {
                        var hits = 0;
                        foreach (var t in Targets.Where(t => t.NetworkId != lTarget.NetworkId))
                        {
                            var input = new PredictionInput
                            {
                                Range = ELength,
                                Delay = E.Delay,
                                Radius = E.Width,
                                Speed = E.Speed,
                                Type = E.Type,
                                Unit = t,
                                From = startPos,
                                RangeCheckFrom = startPos
                            };
                            var pred = Prediction.GetPrediction(input);
                            if (pred.Hitchance >= hitChance)
                            {
                                var rect = new Geometry.Polygon.Rectangle(
                                    startPos.To2D(), startPos.Extend(pred.CastPosition, ELength).To2D(), E.Width);
                                var count = Targets.Count(c => rect.IsInside(c));
                                if (count > hits)
                                {
                                    hits = count;
                                    endPos = startPos.Extend(pred.CastPosition, ELength);
                                }
                            }
                        }
                    }
                    if (endPos.Equals(Vector3.Zero))
                    {
                        startPos = Player.Position.Extend(startPos, Player.Distance(startPos) * 0.9f);
                        endPos = Player.Position.Extend(startPos, Player.Distance(startPos) + ELength);
                    }
                    E.Cast(startPos, endPos);
                    return true;
                }
                var input2 = new PredictionInput
                {
                    Range = MaxERange,
                    Delay = E.Delay,
                    Radius = E.Width,
                    Speed = E.Speed,
                    Type = E.Type,
                    Unit = target
                };
                var castPos = Prediction.GetPrediction(input2).CastPosition;
                startPos = Player.Position.Extend(castPos, E.Range);
                input2.From = startPos;
                input2.RangeCheckFrom = startPos;
                input2.Range = ELength;

                var pred2 = Prediction.GetPrediction(input2);
                if (pred2.Hitchance >= hitChance)
                {
                    E.Cast(
                        startPos,
                        startPos.Extend(
                            UseWObjectPosition(target, false) ? _wObject.Position : pred2.CastPosition, ELength));
                    return true;
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return false;
        }

        private bool UseWObjectPosition(Obj_AI_Hero target, bool inRange)
        {
            try
            {
                if (_wObject != null && _wObject.IsValid && IsSpellUpgraded(W))
                {
                    var buff =
                        target.Buffs.FirstOrDefault(
                            b => b.Name.Equals("viktorgravitonfielddebuffslow", StringComparison.OrdinalIgnoreCase));
                    if (buff != null)
                    {
                        if (buff.Count == 2 && (!inRange || _wObjectEndTime - Game.Time > 0.5f))
                        {
                            return true;
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
                ELogic(HitchanceManager.Get("harass", "e"));
            }
        }

        protected override void LaneClear()
        {
            if (!ManaManager.Check("lane-clear"))
            {
                return;
            }

            if (Menu.Item(Menu.Name + ".lane-clear.q").GetValue<bool>() && Q.IsReady())
            {
                var minion =
                    MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth)
                        .FirstOrDefault(m => m.Health < Q.GetDamage(m) || m.Health * 2 > Q.GetDamage(m));
                if (minion != null)
                {
                    Casting.BasicTargetSkill(minion, Q);
                }
            }
            if (Menu.Item(Menu.Name + ".lane-clear.e").GetValue<bool>() && E.IsReady())
            {
                var minHits = Menu.Item(Menu.Name + ".lane-clear.e-min").GetValue<Slider>().Value;
                var endPos = Vector3.Zero;
                var minions = MinionManager.GetMinions(
                    MaxERange, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
                foreach (var minion in minions)
                {
                    Vector3 startPos;
                    if (minion.Distance(Player.Position) < E.Range)
                    {
                        startPos = minion.Position;
                        var hits = 0;
                        foreach (var t in minions)
                        {
                            var input = new PredictionInput
                            {
                                Range = ELength,
                                Delay = E.Delay,
                                Radius = E.Width,
                                Speed = E.Speed,
                                Type = E.Type,
                                Unit = t,
                                From = startPos,
                                RangeCheckFrom = startPos
                            };
                            var pred = Prediction.GetPrediction(input);
                            if (pred.Hitchance >= HitChance.High)
                            {
                                var rect = new Geometry.Polygon.Rectangle(
                                    startPos.To2D(), startPos.Extend(pred.CastPosition, ELength).To2D(), E.Width);
                                var count = minions.Count(m => rect.IsInside(m));
                                if (count > hits)
                                {
                                    hits = count;
                                    endPos = startPos.Extend(pred.CastPosition, ELength);
                                }
                            }
                        }
                        if (!endPos.Equals(Vector3.Zero) && hits >= minHits)
                        {
                            E.Cast(startPos, endPos);
                        }
                    }
                    else
                    {
                        var input2 = new PredictionInput
                        {
                            Range = MaxERange,
                            Delay = E.Delay,
                            Radius = E.Width,
                            Speed = E.Speed,
                            Type = E.Type,
                            Unit = minion
                        };
                        var castPos = Prediction.GetPrediction(input2).CastPosition;
                        startPos = Player.Position.Extend(castPos, E.Range);
                        var hits = 0;
                        foreach (var t in minions)
                        {
                            var input = new PredictionInput
                            {
                                Range = ELength,
                                Delay = E.Delay,
                                Radius = E.Width,
                                Speed = E.Speed,
                                Type = E.Type,
                                Unit = t,
                                From = startPos,
                                RangeCheckFrom = startPos
                            };
                            var pred = Prediction.GetPrediction(input);
                            if (pred.Hitchance >= HitChance.High)
                            {
                                var rect = new Geometry.Polygon.Rectangle(
                                    startPos.To2D(), startPos.Extend(pred.CastPosition, ELength).To2D(), E.Width);
                                var count = minions.Count(m => rect.IsInside(m));
                                if (count > hits)
                                {
                                    hits = count;
                                    endPos = startPos.Extend(pred.CastPosition, ELength);
                                }
                            }
                        }
                        if (!endPos.Equals(Vector3.Zero) && hits >= minHits)
                        {
                            E.Cast(startPos, endPos);
                        }
                    }
                }
            }
        }

        protected override void Flee()
        {
            if (Menu.Item(Menu.Name + ".flee.w").GetValue<bool>() && W.IsReady())
            {
                var near =
                    HeroManager.Enemies.Where(e => W.CanCast(e))
                        .OrderBy(e => e.Distance(Player.Position))
                        .FirstOrDefault();
                if (near != null)
                {
                    Casting.BasicSkillShot(near, W, HitchanceManager.Get("combo", "w"));
                }
            }
            if (Menu.Item(Menu.Name + ".flee.q-upgraded").GetValue<bool>() && Q.IsReady() && IsSpellUpgraded(Q))
            {
                var near =
                    HeroManager.Enemies.Where(e => Q.CanCast(e))
                        .OrderBy(e => e.Distance(Player.Position))
                        .FirstOrDefault();
                if (near != null)
                {
                    Casting.BasicTargetSkill(near, Q);
                }
            }
        }

        protected override void Killsteal()
        {
            if (Menu.Item(Menu.Name + ".killsteal.q-aa").GetValue<bool>() && Q.IsReady())
            {
                foreach (var target in Targets.Where(t => Orbwalker.InAutoAttackRange(t)))
                {
                    var damage = CalcPassiveDamage(target) + Q.GetDamage(target);
                    if (damage - 10 > target.Health)
                    {
                        Casting.BasicTargetSkill(target, Q);
                        Orbwalker.ForceTarget(target);
                    }
                }
            }
            if (Menu.Item(Menu.Name + ".killsteal.e").GetValue<bool>() && E.IsReady())
            {
                foreach (var target in Targets)
                {
                    var damage = E.GetDamage(target);
                    if (damage - 10 > target.Health)
                    {
                        if (SingleELogic(target, HitchanceManager.Get("combo", "e")))
                        {
                            break;
                        }
                    }
                }
            }
            if (Menu.Item(Menu.Name + ".killsteal.q-aa").GetValue<bool>() && Q.IsReady() &&
                Menu.Item(Menu.Name + ".killsteal.e").GetValue<bool>() && E.IsReady())
            {
                foreach (var target in Targets.Where(t => Orbwalker.InAutoAttackRange(t)))
                {
                    var damage = E.GetDamage(target) + CalcPassiveDamage(target) + Q.GetDamage(target);
                    if (damage - 10 > target.Health)
                    {
                        if (SingleELogic(target, HitchanceManager.Get("combo", "e")))
                        {
                            Casting.BasicTargetSkill(target, Q);
                            Orbwalker.ForceTarget(target);
                            break;
                        }
                    }
                }
            }
        }

        protected override void OnDraw()
        {
            var q = Menu.Item(Menu.Name + ".drawing.q").GetValue<Circle>();
            var w = Menu.Item(Menu.Name + ".drawing.w").GetValue<Circle>();
            var e = Menu.Item(Menu.Name + ".drawing.e").GetValue<Circle>();
            var r = Menu.Item(Menu.Name + ".drawing.r").GetValue<Circle>();
            var circleThickness = Menu.Item(Menu.Name + ".drawing.circle-thickness").GetValue<Slider>().Value;

            if (q.Active && Player.Position.IsOnScreen(Q.Range))
            {
                Render.Circle.DrawCircle(Player.Position, Q.Range, q.Color, circleThickness);
            }
            if (w.Active && Player.Position.IsOnScreen(W.Range))
            {
                Render.Circle.DrawCircle(Player.Position, W.Range, w.Color, circleThickness);
            }
            if (e.Active && Player.Position.IsOnScreen(MaxERange))
            {
                Render.Circle.DrawCircle(Player.Position, MaxERange, e.Color, circleThickness);
            }
            if (r.Active && Player.Position.IsOnScreen(R.Range))
            {
                Render.Circle.DrawCircle(Player.Position, R.Range, r.Color, circleThickness);
            }
        }
    }
}