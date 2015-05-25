#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Cassiopeia.cs is part of SFXChallenger.

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
using SFXChallenger.Managers;
using SFXChallenger.Wrappers;
using SFXLibrary.Extensions.NET;
using SFXLibrary.Extensions.SharpDX;
using SFXLibrary.Logger;
using Orbwalking = SFXChallenger.Wrappers.Orbwalking;
using TargetSelector = SFXChallenger.Wrappers.TargetSelector;

#endregion

namespace SFXChallenger.Champions
{

    #region

    #endregion

    internal class Cassiopeia : Champion
    {
        private float _lastEEndTime;
        private float _lastPoisonClearDelay;
        private float _lastQPoisonDelay;
        private Obj_AI_Base _lastQPoisonT;
        private List<Obj_AI_Hero> _targets = new List<Obj_AI_Hero>();

        protected override ItemFlags ItemFlags
        {
            get { return ItemFlags.Offensive | ItemFlags.Defensive | ItemFlags.Flee; }
        }

        protected override void OnLoad()
        {
            Core.OnPreUpdate += OnCorePreUpdate;
            Core.OnPostUpdate += OnCorePostUpdate;
            Orbwalking.BeforeAttack += OnOrbwalkingBeforeAttack;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget += OnInterruptableTarget;
            CustomEvents.Unit.OnDash += OnUnitDash;
        }

        protected override void OnUnload()
        {
            Core.OnPreUpdate -= OnCorePreUpdate;
            Core.OnPostUpdate -= OnCorePostUpdate;
            Orbwalking.BeforeAttack -= OnOrbwalkingBeforeAttack;
            AntiGapcloser.OnEnemyGapcloser -= OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget -= OnInterruptableTarget;
            CustomEvents.Unit.OnDash -= OnUnitDash;
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
            drawingMenu.AddItem(
                new MenuItem(drawingMenu.Name + ".r-flash", "R " + Global.Lang.Get("G_Flash")).SetValue(
                    new Circle(false, Color.White)));

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
                new Dictionary<string, int> { { "Q", 2 }, { "W", 1 } });
            ManaManager.AddToMenu(harassMenu, "harass", ManaCheckType.Minimum, ManaValueType.Total, 70, 0, 750);
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".w", Global.Lang.Get("G_UseW")).SetValue(true));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(true));

            var laneclearMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_LaneClear"), Menu.Name + ".lane-clear"));
            ManaManager.AddToMenu(laneclearMenu, "lane-clear", ManaCheckType.Minimum, ManaValueType.Total, 90, 0, 750);
            laneclearMenu.AddItem(
                new MenuItem(laneclearMenu.Name + ".aa", Global.Lang.Get("G_UseAutoAttacks")).SetValue(true));
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".w", Global.Lang.Get("G_UseW")).SetValue(true));
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(true));

            var lasthitMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_LastHit"), Menu.Name + ".lasthit"));
            ManaManager.AddToMenu(lasthitMenu, "lasthit", ManaCheckType.Maximum, ManaValueType.Percent, 70);
            lasthitMenu.AddItem(new MenuItem(lasthitMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(true));
            lasthitMenu.AddItem(
                new MenuItem(lasthitMenu.Name + ".e-poison", Global.Lang.Get("Cassio_UseEPoison")).SetValue(true));

            var ultimateMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Ultimate"), Menu.Name + ".ultimate"));

            var uComboMenu = ultimateMenu.AddSubMenu(new Menu(Global.Lang.Get("G_Combo"), ultimateMenu.Name + ".combo"));
            uComboMenu.AddItem(
                new MenuItem(uComboMenu.Name + ".min", Global.Lang.Get("Cassio_RMin")).SetValue(new Slider(3, 1, 5)));
            uComboMenu.AddItem(new MenuItem(uComboMenu.Name + ".1v1", "R 1v1").SetValue(true));
            uComboMenu.AddItem(new MenuItem(uComboMenu.Name + ".enabled", Global.Lang.Get("G_Enabled")).SetValue(true));

            var uAutoMenu = ultimateMenu.AddSubMenu(new Menu(Global.Lang.Get("G_Auto"), ultimateMenu.Name + ".auto"));

            var autoGapMenu =
                uAutoMenu.AddSubMenu(new Menu(Global.Lang.Get("G_Gapcloser"), uAutoMenu.Name + ".gapcloser"));
            foreach (var enemy in
                HeroManager.Enemies.Where(
                    e =>
                        AntiGapcloser.Spells.Any(
                            s => s.ChampionName.Equals(e.ChampionName, StringComparison.OrdinalIgnoreCase))))
            {
                autoGapMenu.AddItem(
                    new MenuItem(autoGapMenu.Name + "." + enemy.ChampionName, enemy.ChampionName).SetValue(true));
            }

            var autoInterruptMenu =
                uAutoMenu.AddSubMenu(new Menu(Global.Lang.Get("G_InterruptSpell"), uAutoMenu.Name + ".interrupt"));
            foreach (var enemy in HeroManager.Enemies)
            {
                autoInterruptMenu.AddItem(
                    new MenuItem(autoInterruptMenu.Name + "." + enemy.ChampionName, enemy.ChampionName).SetValue(true));
            }

            uAutoMenu.AddItem(
                new MenuItem(uAutoMenu.Name + ".min", Global.Lang.Get("Cassio_RMin")).SetValue(new Slider(3, 1, 5)));
            uAutoMenu.AddItem(new MenuItem(uAutoMenu.Name + ".1v1", "R 1v1").SetValue(true));
            uAutoMenu.AddItem(new MenuItem(uAutoMenu.Name + ".enabled", Global.Lang.Get("G_Enabled")).SetValue(true));

            var uFlashMenu = ultimateMenu.AddSubMenu(new Menu(Global.Lang.Get("G_Flash"), ultimateMenu.Name + ".flash"));
            uFlashMenu.AddItem(
                new MenuItem(uFlashMenu.Name + ".min", Global.Lang.Get("Cassio_RMin")).SetValue(new Slider(3, 1, 5)));
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
                new MenuItem(uAssistedMenu.Name + ".min", Global.Lang.Get("Cassio_RMin")).SetValue(new Slider(3, 1, 5)));
            uAssistedMenu.AddItem(new MenuItem(uAssistedMenu.Name + ".1v1", "R 1v1").SetValue(true));
            uAssistedMenu.AddItem(
                new MenuItem(uAssistedMenu.Name + ".hotkey", Global.Lang.Get("G_Hotkey")).SetValue(
                    new KeyBind('R', KeyBindType.Press)));
            uAssistedMenu.AddItem(
                new MenuItem(uAssistedMenu.Name + ".move-cursor", Global.Lang.Get("G_MoveCursor")).SetValue(true));
            uAssistedMenu.AddItem(
                new MenuItem(uAssistedMenu.Name + ".enabled", Global.Lang.Get("G_Enabled")).SetValue(true));

            var killstealMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Killsteal"), Menu.Name + ".killsteal"));
            killstealMenu.AddItem(new MenuItem(killstealMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(true));
            killstealMenu.AddItem(
                new MenuItem(killstealMenu.Name + ".e-poison", Global.Lang.Get("Cassio_UseEPoison")).SetValue(true));

            var fleeMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Flee"), Menu.Name + ".flee"));
            fleeMenu.AddItem(new MenuItem(fleeMenu.Name + ".w", Global.Lang.Get("G_UseW")).SetValue(true));

            var miscMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Miscellaneous"), Menu.Name + ".miscellaneous"));
            miscMenu.AddItem(new MenuItem(miscMenu.Name + ".q-dash", "Q " + Global.Lang.Get("G_Dash")).SetValue(false));
            miscMenu.AddItem(
                new MenuItem(miscMenu.Name + ".q-fleeing", "Q " + Global.Lang.Get("G_Fleeing")).SetValue(false));
            miscMenu.AddItem(
                new MenuItem(miscMenu.Name + ".w-stunned", "W " + Global.Lang.Get("G_Stunned")).SetValue(false));
            miscMenu.AddItem(new MenuItem(miscMenu.Name + ".w-dash", "W " + Global.Lang.Get("G_Dash")).SetValue(false));
            miscMenu.AddItem(
                new MenuItem(miscMenu.Name + ".w-fleeing", "W " + Global.Lang.Get("G_Fleeing")).SetValue(false));
        }

        protected override void SetupSpells()
        {
            Q = new Spell(SpellSlot.Q, 850f);
            Q.SetSkillshot(0.6f, 60f, float.MaxValue, false, SkillshotType.SkillshotCircle);

            W = new Spell(SpellSlot.W, 850f);
            W.SetSkillshot(0.7f, 125f, 2500f, false, SkillshotType.SkillshotCircle);

            E = new Spell(SpellSlot.E, 700f);
            E.SetTargetted(0.2f, 1900f);

            R = new Spell(SpellSlot.R, 750f);
            R.SetSkillshot(0.7f, (float) (80 * Math.PI / 180), float.MaxValue, false, SkillshotType.SkillshotCone);
        }

        private void OnCorePreUpdate(EventArgs args)
        {
            try
            {
                _targets =
                    TargetSelector.GetTargets(850f, LeagueSharp.Common.TargetSelector.DamageType.Magical)
                        .Select(t => t.Hero)
                        .ToList();
                if ((Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit && ManaManager.Check("lasthit")) &&
                    E.IsReady())
                {
                    var ePoison = Menu.Item(Menu.Name + ".lasthit.e-poison").GetValue<bool>();
                    var eHit = Menu.Item(Menu.Name + ".lasthit.e").GetValue<bool>();
                    if (eHit || ePoison)
                    {
                        var m =
                            MinionManager.GetMinions(
                                ObjectManager.Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.NotAlly)
                                .FirstOrDefault(
                                    e =>
                                        e.Health < E.GetDamage(e) - 5 &&
                                        (ePoison && GetPoisonBuffEndTime(e) > GetEDelay(e) || eHit));
                        if (m != null)
                        {
                            Casting.BasicTargetSkill(m, E);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
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
                    var targets =
                        TargetSelector.GetTargets(R.Range + SummonerManager.Flash.Range)
                            .Where(
                                t =>
                                    t.Hero != null &&
                                    Prediction.GetPrediction(t.Hero, R.Delay + 0.3f)
                                        .UnitPosition.Distance(Player.Position) > R.Range * 1.05);
                    foreach (var target in targets)
                    {
                        var min = Menu.Item(Menu.Name + ".ultimate.flash.min").GetValue<Slider>().Value;
                        var flashPos = Player.Position.Extend(target.Hero.Position, SummonerManager.Flash.Range);
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
                                    Range = R.Range * 1.1f,
                                    Speed = R.Speed,
                                    Radius = R.Width,
                                    Type = SkillshotType.SkillshotCone,
                                    Unit = target.Hero
                                });
                        if (pred.Hitchance >= HitchanceManager.Get("combo", "r"))
                        {
                            if (Menu.Item(Menu.Name + ".ultimate.flash.1v1").GetValue<bool>() &&
                                target.Hero.IsFacing(Player))
                            {
                                var cDmg = CalcComboDamage(
                                    target.Hero, Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady(),
                                    Menu.Item(Menu.Name + ".combo.w").GetValue<bool>() && W.IsReady(),
                                    Menu.Item(Menu.Name + ".combo.e").GetValue<bool>() && E.IsReady(), true);
                                if (cDmg - 20 >= target.Hero.Health)
                                {
                                    R.Cast(
                                        Player.Position.Extend(
                                            pred.CastPosition, -(Player.Position.Distance(pred.CastPosition) * 2)), true);
                                    Utility.DelayAction.Add(300, () => SummonerManager.Flash.Cast(pred.CastPosition));
                                    return;
                                }
                            }
                            if (HeroManager.Enemies.Count(x => R.WillHit(x, pred.CastPosition)) >= min)
                            {
                                R.Cast(
                                    Player.Position.Extend(
                                        pred.CastPosition, -(Player.Position.Distance(pred.CastPosition) * 2)), true);
                                Utility.DelayAction.Add(300, () => SummonerManager.Flash.Cast(pred.CastPosition));
                            }
                        }
                    }
                }

                if (Menu.Item(Menu.Name + ".ultimate.assisted.enabled").GetValue<bool>() &&
                    Menu.Item(Menu.Name + ".ultimate.assisted.hotkey").GetValue<KeyBind>().Active && R.IsReady())
                {
                    if (Menu.Item(Menu.Name + ".ultimate.assisted.move-cursor").GetValue<bool>())
                    {
                        Orbwalking.MoveTo(Game.CursorPos, Orbwalker.HoldAreaRadius);
                    }
                    if (Menu.Item(Menu.Name + ".ultimate.assisted.1v1").GetValue<bool>())
                    {
                        RLogic1V1(
                            HitchanceManager.Get("combo", "r"),
                            Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady(),
                            Menu.Item(Menu.Name + ".combo.w").GetValue<bool>() && W.IsReady(),
                            Menu.Item(Menu.Name + ".combo.e").GetValue<bool>() && E.IsReady());
                    }
                    RLogic(
                        HitchanceManager.Get("combo", "r"),
                        Menu.Item(Menu.Name + ".ultimate.assisted.min").GetValue<Slider>().Value);
                }

                if (Menu.Item(Menu.Name + ".ultimate.auto.enabled").GetValue<bool>() && R.IsReady())
                {
                    if (Menu.Item(Menu.Name + ".ultimate.auto.1v1").GetValue<bool>())
                    {
                        RLogic1V1(
                            HitchanceManager.Get("combo", "r"),
                            Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady(),
                            Menu.Item(Menu.Name + ".combo.w").GetValue<bool>() && W.IsReady(),
                            Menu.Item(Menu.Name + ".combo.e").GetValue<bool>() && E.IsReady());
                    }
                    RLogic(
                        HitchanceManager.Get("combo", "r"),
                        Menu.Item(Menu.Name + ".ultimate.auto.min").GetValue<Slider>().Value);
                }

                if (Menu.Item(Menu.Name + ".miscellaneous.w-stunned").GetValue<bool>() && W.IsReady())
                {
                    var target =
                        _targets.FirstOrDefault(
                            t =>
                                t.IsValidTarget(W.Range) &&
                                (t.HasBuffOfType(BuffType.Charm) || t.HasBuffOfType(BuffType.Knockup) ||
                                 t.HasBuffOfType(BuffType.Polymorph) || t.HasBuffOfType(BuffType.Fear) ||
                                 t.HasBuffOfType(BuffType.Stun)));
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

        private void OnOrbwalkingBeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            try
            {
                var t = args.Target as Obj_AI_Hero;
                if (t != null &&
                    (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo ||
                     Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Harass))
                {
                    args.Process = false;
                }
                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
                {
                    args.Process = Menu.Item(Menu.Name + ".lane-clear.aa").GetValue<bool>();
                    if (args.Process == false)
                    {
                        var m = args.Target as Obj_AI_Minion;
                        if (m != null && (_lastEEndTime < Game.Time || E.IsReady()) ||
                            (GetPoisonBuffEndTime(m) < GetEDelay(m) || Player.GetSpell(E.Slot).ManaCost > Player.Mana) ||
                            !ManaManager.Check("lane-clear"))
                        {
                            args.Process = true;
                        }
                    }
                }

                if ((Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit))
                {
                    var m = args.Target as Obj_AI_Minion;
                    if (m != null && E.CanCast(m))
                    {
                        if (Player.GetSpell(E.Slot).ManaCost < Player.Mana)
                        {
                            args.Process = Menu.Item(Menu.Name + ".lasthit.e").GetValue<bool>() ||
                                           (Menu.Item(Menu.Name + ".lasthit.e-poison").GetValue<bool>() &&
                                            GetPoisonBuffEndTime(m) > GetEDelay(m)) && ManaManager.Check("lasthit");
                        }
                    }
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
                if (!sender.IsEnemy)
                {
                    return;
                }

                if (Menu.Item(Menu.Name + ".miscellaneous.q-dash").GetValue<bool>() &&
                    Player.Distance(args.EndPos) <= Q.Range)
                {
                    var delay = (int) (args.EndTick - Game.Time - Q.Delay - 0.1f);
                    if (delay > 0)
                    {
                        Utility.DelayAction.Add(delay * 1000, () => Q.Cast(args.EndPos));
                    }
                    else
                    {
                        Q.Cast(args.EndPos);
                    }
                }
                if (Menu.Item(Menu.Name + ".miscellaneous.w-dash").GetValue<bool>() &&
                    Player.Distance(args.EndPos) <= W.Range)
                {
                    if (sender.BaseSkinName.Equals("LeBlanc", StringComparison.CurrentCultureIgnoreCase))
                    {
                        W.Cast(args.StartPos.Distance(Player) < W.Range ? args.StartPos : args.EndPos);
                        return;
                    }
                    var delay = (int) (args.EndTick - Game.Time - W.Delay - 0.1f);
                    if (delay > 0)
                    {
                        Utility.DelayAction.Add(delay * 1000, () => W.Cast(args.EndPos));
                    }
                    else
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
                    args.EndTime > Game.Time + R.Delay + 0.1f &&
                    Menu.Item(Menu.Name + ".ultimate.auto.enabled").GetValue<bool>() &&
                    Menu.Item(Menu.Name + ".ultimate.auto.interrupt." + sender.ChampionName).GetValue<bool>())
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
                if (Menu.Item(Menu.Name + ".ultimate.auto.enabled").GetValue<bool>() &&
                    Menu.Item(Menu.Name + ".ultimate.auto.gapcloser." + args.Sender.ChampionName).GetValue<bool>())
                {
                    if (args.End.Distance(Player.Position) < R.Range)
                    {
                        R.Cast(args.End);
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
                QLogic(HitchanceManager.Get("combo", "q"));
            }
            if (w)
            {
                WLogic(HitchanceManager.Get("combo", "w"));
            }
            if (e)
            {
                ELogic();
            }
            if (r)
            {
                if (Menu.Item(Menu.Name + ".ultimate.combo.1v1").GetValue<bool>())
                {
                    RLogic1V1(HitchanceManager.Get("combo", "r"), q, w, e);
                }
                RLogic(
                    HitchanceManager.Get("combo", "r"),
                    Menu.Item(Menu.Name + ".ultimate.combo.min").GetValue<Slider>().Value);
            }
            var target = _targets.FirstOrDefault(t => t.IsValidTarget(R.Range));
            if (target != null && CalcComboDamage(target, q, w, e, r) * 1.3 > target.Health)
            {
                ItemManager.UseComboItems(target);
                SummonerManager.UseComboSummoners(target);
            }
        }

        private bool IsNearTurret(Obj_AI_Base target)
        {
            try
            {
                return
                    ObjectManager.Get<Obj_AI_Turret>().Any(turret => turret.IsValidTarget(1200f, true, target.Position));
            }

            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return false;
        }

        private float CalcComboDamage(Obj_AI_Base target, bool q, bool w, bool e, bool r)
        {
            try
            {
                var manaCost = (w && W.IsReady()
                    ? Player.GetSpell(W.Slot).ManaCost
                    : (q ? Player.GetSpell(Q.Slot).ManaCost : 0)) * 2;
                var damage = (w && W.IsReady() ? W.GetDamage(target) : (q ? Q.GetDamage(target) : 0)) * 2;

                if (e)
                {
                    var eMana = Player.GetSpell(E.Slot).ManaCost;
                    var eDamage = E.GetDamage(target);
                    var count = IsNearTurret(target) && !target.IsFacing(Player) ||
                                IsNearTurret(target) && Player.HealthPercent <= 35 || !R.IsReady()
                        ? 5
                        : 10;
                    for (var i = 0; i < count; i++)
                    {
                        if (manaCost + eMana > Player.Mana)
                        {
                            break;
                        }
                        manaCost += eMana;
                        damage += eDamage;
                    }
                }
                if (r)
                {
                    if (manaCost + Player.GetSpell(R.Slot).ManaCost - 10 > Player.Mana)
                    {
                        return damage;
                    }
                    return damage + (R.IsReady() ? R.GetDamage(target) : 0);
                }
                return damage;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return 0;
        }

        private void RLogic1V1(HitChance hitChance, bool q, bool w, bool e)
        {
            foreach (var target in _targets.Where(t => t.IsFacing(Player) && t.HealthPercent > 25 && R.CanCast(t)))
            {
                var cDmg = CalcComboDamage(target, q, w, e, true);
                if (cDmg - 20 >= target.Health)
                {
                    if (HeroManager.Enemies.Count(em => !em.IsDead && em.IsVisible && em.Distance(Player) < 3000) == 1)
                    {
                        Casting.BasicSkillShot(target, R, hitChance);
                    }
                }
            }
        }

        private void RLogic(HitChance hitChance, int min)
        {
            foreach (var target in _targets.Where(t => R.CanCast(t)))
            {
                var pred = R.GetPrediction(target, true);
                if (pred.Hitchance >= hitChance)
                {
                    var hits = HeroManager.Enemies.Count(x => R.WillHit(x, pred.CastPosition));
                    if (hits >= min)
                    {
                        R.Cast(pred.CastPosition, true);
                    }
                }
            }
        }

        private void QLogic(HitChance hitChance)
        {
            var ts =
                _targets.FirstOrDefault(
                    t =>
                        Q.CanCast(t) &&
                        (GetPoisonBuffEndTime(t) < Q.Delay * 1.2f ||
                         (Menu.Item(Menu.Name + ".miscellaneous.q-fleeing").GetValue<bool>() && !t.IsFacing(Player) &&
                          t.IsMoving && t.Distance(Player) > 150)));
            if (ts != null)
            {
                _lastQPoisonDelay = Game.Time + Q.Delay;
                _lastQPoisonT = ts;
                Casting.BasicSkillShot(ts, Q, hitChance);
            }
        }

        private void WLogic(HitChance hitChance)
        {
            var ts =
                _targets.FirstOrDefault(
                    t =>
                        W.CanCast(t) &&
                        ((_lastQPoisonDelay < Game.Time && GetPoisonBuffEndTime(t) < W.Delay * 1.2 ||
                          _lastQPoisonT.NetworkId != t.NetworkId) ||
                         (Menu.Item(Menu.Name + ".miscellaneous.w-fleeing").GetValue<bool>() && !t.IsFacing(Player) &&
                          t.IsMoving && t.Distance(Player) > 150)));
            if (ts != null)
            {
                Casting.BasicSkillShot(ts, W, hitChance);
            }
        }

        private void ELogic()
        {
            var ts = _targets.FirstOrDefault(t => E.CanCast(t) && GetPoisonBuffEndTime(t) > GetEDelay(t));
            if (ts != null)
            {
                E.Cast(ts);
            }
        }

        protected override void Harass()
        {
            if (Menu.Item(Menu.Name + ".harass.q").GetValue<bool>())
            {
                QLogic(HitchanceManager.Get("harass", "q"));
            }
            if (Menu.Item(Menu.Name + ".harass.w").GetValue<bool>())
            {
                WLogic(HitchanceManager.Get("harass", "w"));
            }
            if (Menu.Item(Menu.Name + ".harass.e").GetValue<bool>() && ManaManager.Check("harass"))
            {
                ELogic();
            }
        }

        private float GetPoisonBuffEndTime(Obj_AI_Base target)
        {
            try
            {
                var buffEndTime =
                    target.Buffs.OrderByDescending(buff => buff.EndTime - Game.Time)
                        .Where(buff => buff.Type == BuffType.Poison)
                        .Select(buff => buff.EndTime)
                        .FirstOrDefault();
                return buffEndTime;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return 0;
        }

        private float GetEDelay(Obj_AI_Base target)
        {
            try
            {
                return E.Delay + (ObjectManager.Player.ServerPosition.Distance(target.ServerPosition) / E.Speed) +
                       (Game.Ping / 2000f) + 0.1f;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return 0;
        }

        protected override void LaneClear()
        {
            var q = Menu.Item(Menu.Name + ".lane-clear.q").GetValue<bool>() && Q.IsReady();
            var w = Menu.Item(Menu.Name + ".lane-clear.w").GetValue<bool>() && W.IsReady();

            if (Menu.Item(Menu.Name + ".lane-clear.e").GetValue<bool>() && E.IsReady() &&
                ManaManager.Check("lane-clear"))
            {
                var minion =
                    MinionManager.GetMinions(
                        ObjectManager.Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.NotAlly)
                        .Where(
                            e =>
                                GetPoisonBuffEndTime(e) > GetEDelay(e) &&
                                (e.Team == GameObjectTeam.Neutral ||
                                 (e.Health > E.GetDamage(e) * 2 || e.Health < E.GetDamage(e) - 5)))
                        .OrderByDescending(
                            m => m.BaseSkinName.Contains("MinionSiege", StringComparison.OrdinalIgnoreCase))
                        .FirstOrDefault();
                if (minion != null)
                {
                    _lastEEndTime = Game.Time + GetEDelay(minion) + 0.1f;
                    Casting.BasicTargetSkill(minion, E);
                }
            }

            if (q || w)
            {
                var minions =
                    MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range + Q.Width)
                        .Where(e => GetPoisonBuffEndTime(e) < Q.Delay * 1.1)
                        .OrderByDescending(
                            m => m.BaseSkinName.Contains("MinionSiege", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                if (minions.Any())
                {
                    if (q)
                    {
                        var prediction = Q.GetCircularFarmLocation(minions, Q.Width + 30);
                        if (prediction.MinionsHit > 1 && _lastPoisonClearDelay < Game.Time)
                        {
                            _lastPoisonClearDelay = Game.Time + Q.Delay;
                            Q.Cast(prediction.Position);
                        }
                    }
                    if (w)
                    {
                        var prediction = W.GetCircularFarmLocation(minions, W.Width + 50);
                        if (prediction.MinionsHit > 2 && _lastPoisonClearDelay < Game.Time)
                        {
                            _lastPoisonClearDelay = Game.Time + W.Delay;
                            W.Cast(prediction.Position);
                        }
                    }
                }
                else
                {
                    var creep =
                        MinionManager.GetMinions(
                            ObjectManager.Player.ServerPosition, Q.Range + Q.Width, MinionTypes.All, MinionTeam.Neutral,
                            MinionOrderTypes.MaxHealth).FirstOrDefault(e => GetPoisonBuffEndTime(e) < Q.Delay * 1.1);
                    if (creep != null)
                    {
                        if (q)
                        {
                            Q.Cast(creep);
                        }
                        if (w)
                        {
                            W.Cast(creep);
                        }
                    }
                }
            }
        }

        protected override void Flee()
        {
            if (Menu.Item(Menu.Name + ".flee.w").GetValue<bool>() && E.IsReady())
            {
                var near = HeroManager.Enemies.OrderBy(e => e.Distance(Player.Position)).FirstOrDefault();
                if (near != null)
                {
                    var pred = W.GetPrediction(near, true);
                    if (pred.Hitchance >= HitchanceManager.Get("harass", "w"))
                    {
                        W.Cast(
                            Player.Position.Extend(
                                pred.CastPosition, Player.Position.Distance(pred.CastPosition) * 0.8f));
                    }
                }
            }
        }

        protected override void Killsteal()
        {
            var ePoison = Menu.Item(Menu.Name + ".killsteal.e-poison").GetValue<bool>();
            var eHit = Menu.Item(Menu.Name + ".killsteal.e").GetValue<bool>();
            if (ePoison || eHit && E.IsReady())
            {
                var m =
                    HeroManager.Enemies.FirstOrDefault(
                        e =>
                            E.CanCast(e) && e.Health < E.GetDamage(e) - 5 &&
                            (ePoison && GetPoisonBuffEndTime(e) > GetEDelay(e) || eHit));
                if (m != null)
                {
                    Casting.BasicTargetSkill(m, E);
                }
            }
        }

        protected override void OnDraw()
        {
            var q = Menu.Item(Menu.Name + ".drawing.q").GetValue<Circle>();
            var w = Menu.Item(Menu.Name + ".drawing.w").GetValue<Circle>();
            var e = Menu.Item(Menu.Name + ".drawing.e").GetValue<Circle>();
            var r = Menu.Item(Menu.Name + ".drawing.r").GetValue<Circle>();
            var rFlash = Menu.Item(Menu.Name + ".drawing.r-flash").GetValue<Circle>();
            var circleThickness = Menu.Item(Menu.Name + ".drawing.circle-thickness").GetValue<Slider>().Value;

            if (q.Active && Player.Position.IsOnScreen(Q.Range))
            {
                Render.Circle.DrawCircle(Player.Position, Q.Range, q.Color, circleThickness);
            }
            if (w.Active && Player.Position.IsOnScreen(W.Range))
            {
                Render.Circle.DrawCircle(Player.Position, W.Range, w.Color, circleThickness);
            }
            if (e.Active && Player.Position.IsOnScreen(E.Range))
            {
                Render.Circle.DrawCircle(Player.Position, E.Range, e.Color, circleThickness);
            }
            if (r.Active && Player.Position.IsOnScreen(R.Range))
            {
                Render.Circle.DrawCircle(Player.Position, R.Range, r.Color, circleThickness);
            }
            if (rFlash.Active && Player.Position.IsOnScreen(R.Range + SummonerManager.Flash.Range))
            {
                Render.Circle.DrawCircle(
                    Player.Position, R.Range + SummonerManager.Flash.Range, rFlash.Color, circleThickness);
            }
        }
    }
}