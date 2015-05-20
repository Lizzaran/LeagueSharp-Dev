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

namespace SFXChallenger.Champions
{
    #region

    using System;
    using System.Drawing;
    using System.Linq;
    using Abstracts;
    using Enumerations;
    using LeagueSharp;
    using LeagueSharp.Common;
    using Managers;
    using SFXLibrary.Logger;
    using Wrappers;
    using Orbwalking = Wrappers.Orbwalking;
    using TargetSelector = Wrappers.TargetSelector;

    #endregion

    internal class Cassiopeia : Champion
    {
        private Obj_AI_Base _lastPoison;
        private float _nextPoison;

        protected override ItemFlags ItemFlags
        {
            get { return ItemFlags.Offensive | ItemFlags.Defensive | ItemFlags.Flee; }
        }

        protected override void OnLoad()
        {
            Core.OnPostUpdate += OnCorePostUpdate;
            Orbwalking.BeforeAttack += OnOrbwalkingBeforeAttack;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget += OnInterruptableTarget;
        }

        protected override void OnUnload()
        {
            Core.OnPostUpdate -= OnCorePostUpdate;
            Orbwalking.BeforeAttack -= OnOrbwalkingBeforeAttack;
            AntiGapcloser.OnEnemyGapcloser -= OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget -= OnInterruptableTarget;
        }

        protected override void AddToMenu()
        {
            var drawingMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Drawing"), Menu.Name + ".drawing"));
            drawingMenu.AddItem(
                new MenuItem(drawingMenu.Name + ".circle-thickness", Global.Lang.Get("G_CircleThickness")).SetValue(new Slider(2, 0, 10)));
            drawingMenu.AddItem(new MenuItem(drawingMenu.Name + ".q", "Q").SetValue(new Circle(false, Color.White)));
            drawingMenu.AddItem(new MenuItem(drawingMenu.Name + ".w", "W").SetValue(new Circle(false, Color.White)));
            drawingMenu.AddItem(new MenuItem(drawingMenu.Name + ".e", "E").SetValue(new Circle(false, Color.White)));
            drawingMenu.AddItem(new MenuItem(drawingMenu.Name + ".r", "R").SetValue(new Circle(false, Color.White)));

            var comboMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("C_Combo"), Menu.Name + ".combo"));
            ManaManager.AddToMenu(comboMenu, "combo", 0);
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".q", Global.Lang.Get("C_UseQ")).SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".w", Global.Lang.Get("C_UseW")).SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".e", Global.Lang.Get("C_UseE")).SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".r", Global.Lang.Get("C_UseR")).SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".r-min-facing", Global.Lang.Get("Cassio_RMinFacing")).SetValue(new Slider(1, 1, 5)));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".r-min", Global.Lang.Get("Cassio_RMin")).SetValue(new Slider(3, 1, 5)));

            var harassMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("C_Harass"), Menu.Name + ".harass"));
            ManaManager.AddToMenu(harassMenu, "harass");
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".q", Global.Lang.Get("C_UseQ")).SetValue(true));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".w", Global.Lang.Get("C_UseW")).SetValue(true));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".e", Global.Lang.Get("C_UseE")).SetValue(true));

            var laneclearMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("C_LaneClear"), Menu.Name + ".lane-clear"));
            ManaManager.AddToMenu(laneclearMenu, "lane-clear");
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".q", Global.Lang.Get("C_UseQ")).SetValue(true));
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".w", Global.Lang.Get("C_UseW")).SetValue(true));
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".e", Global.Lang.Get("C_UseE")).SetValue(true));

            var autoMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("C_Auto"), Menu.Name + ".auto"));
            autoMenu.AddItem(new MenuItem(autoMenu.Name + ".r-min-facing", Global.Lang.Get("Cassio_RMinFacing")).SetValue(new Slider(3, 1, 5)));
            autoMenu.AddItem(new MenuItem(autoMenu.Name + ".r-min", Global.Lang.Get("Cassio_RMin")).SetValue(new Slider(4, 1, 5)));
            autoMenu.AddItem(new MenuItem(autoMenu.Name + ".enabled", Global.Lang.Get("G_Enabled")).SetValue(true));

            var flashMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("C_Flash"), Menu.Name + ".flash"));
            flashMenu.AddItem(new MenuItem(flashMenu.Name + ".r-min-facing", Global.Lang.Get("Cassio_RMinFacing")).SetValue(new Slider(3, 1, 5)));
            flashMenu.AddItem(new MenuItem(flashMenu.Name + ".r-min-killable", Global.Lang.Get("Cassio_RMinKillable")).SetValue(new Slider(2, 1, 5)));
            flashMenu.AddItem(new MenuItem(flashMenu.Name + ".r-min", Global.Lang.Get("Cassio_RMin")).SetValue(new Slider(4, 1, 5)));
            flashMenu.AddItem(
                new MenuItem(flashMenu.Name + ".r-hotkey", "R " + Global.Lang.Get("G_Hotkey")).SetValue(new KeyBind('U', KeyBindType.Press)));

            var fleeMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("C_Flee"), Menu.Name + ".flee"));
            ManaManager.AddToMenu(fleeMenu, "flee", 15);
            fleeMenu.AddItem(new MenuItem(fleeMenu.Name + ".w", Global.Lang.Get("C_UseW")).SetValue(true));

            var miscMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Miscellaneous"), Menu.Name + ".miscellaneous"));
            ManaManager.AddToMenu(miscMenu, "misc", 15);
            miscMenu.AddItem(new MenuItem(miscMenu.Name + ".r-gapcloser", "R " + Global.Lang.Get("G_Gapcloser")).SetValue(false));
            miscMenu.AddItem(new MenuItem(miscMenu.Name + ".r-important", "R " + Global.Lang.Get("G_ImportantSpell")).SetValue(false));
            miscMenu.AddItem(new MenuItem(miscMenu.Name + ".e-lasthit", "E " + Global.Lang.Get("G_Lasthit")).SetValue(false));
            miscMenu.AddItem(new MenuItem(miscMenu.Name + ".w-stunned", "W " + Global.Lang.Get("G_Stunned")).SetValue(false));
        }

        protected override void SetupSpells()
        {
            Q = new Spell(SpellSlot.Q, 850f);
            Q.SetSkillshot(0.6f, 40f, float.MaxValue, false, SkillshotType.SkillshotCircle);

            W = new Spell(SpellSlot.W, 850f);
            W.SetSkillshot(0.5f, 90f, 2500f, false, SkillshotType.SkillshotCircle);

            E = new Spell(SpellSlot.E, 700f);
            E.SetTargetted(0.2f, 1900f);

            R = new Spell(SpellSlot.R, 800f);
            R.SetSkillshot(0.7f, (float) (80*Math.PI/180), float.MaxValue, false, SkillshotType.SkillshotCone);
        }

        private void OnCorePostUpdate(EventArgs args)
        {
            if (Menu.Item(Menu.Name + ".auto.enabled").GetValue<bool>())
            {
                RLogic(Menu.Item(Menu.Name + ".auto.r-min").GetValue<Slider>().Value,
                    Menu.Item(Menu.Name + ".auto.r-min-facing").GetValue<Slider>().Value);
            }
            if (Menu.Item(Menu.Name + ".flash.r-hotkey").GetValue<KeyBind>().Active && R.IsReady() && SummonerManager.Flash.IsReady())
            {
                Orbwalking.MoveTo(Game.CursorPos, Orbwalker.HoldAreaRadius);
                var target =
                    HeroManager.Enemies.FirstOrDefault(
                        x =>
                            x.IsValidTarget(R.Range + 400) &&
                            Prediction.GetPrediction(x, R.Delay + 0.3f).UnitPosition.Distance(Player.Position) <= (R.Range*0.9) + 400 &&
                            Prediction.GetPrediction(x, R.Delay + 0.3f).UnitPosition.Distance(Player.Position) > R.Range);
                if (target != null)
                {
                    var minFacing = Menu.Item(Menu.Name + ".flash.r-min-facing").GetValue<Slider>().Value;
                    var pred = R.GetPrediction(target, true);
                    var rHits = HeroManager.Enemies.Where(x => R.WillHit(x.Position, pred.CastPosition)).ToList();
                    var inRange = rHits.Count(enemy => enemy.Position.Distance(Player.Position) < R.Range + 400);
                    var isFacing = rHits.Count(enemy => IsFacing(enemy, Player));
                    var killable = rHits.Count(enemy => enemy.Health < R.GetDamage(enemy) - 20);
                    if (isFacing >= minFacing || inRange >= Menu.Item(Menu.Name + ".flash.r-min").GetValue<Slider>().Value ||
                        killable >= Menu.Item(Menu.Name + ".flash.r-min-killable").GetValue<Slider>().Value)
                    {
                        if (minFacing == 1 && (!target.CanMove || target.IsImmovable || target.IsWindingUp) || minFacing > 1)
                        {
                            var pos = Player.Position.Extend(pred.CastPosition, -(Player.Position.Distance(pred.CastPosition)*2));
                            R.Cast(pos, true);
                            Utility.DelayAction.Add(300, () => SummonerManager.Flash.Cast(pred.CastPosition));
                        }
                    }
                }
            }
            if (ManaManager.Check("misc"))
            {
                if (Menu.Item(Menu.Name + ".miscellaneous.w-stunned").GetValue<bool>() && W.IsReady())
                {
                    var target =
                        TargetSelector.GetTargets(W.Range)
                            .FirstOrDefault(t => t.Hero.IsValidTarget(W.Range) && t.Hero.IsStunned || t.Hero.IsCharmed || t.Hero.IsRooted);
                    if (target != null)
                    {
                        Casting.BasicSkillShot(target.Hero, W, HitchanceManager.Get("w"));
                    }
                }
                if (Menu.Item(Menu.Name + ".miscellaneous.e-lasthit").GetValue<bool>() &&
                    (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit || Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear) &&
                    E.IsReady())
                {
                    var minion =
                        MinionManager.GetMinions(E.Range, MinionTypes.All, MinionTeam.NotAlly)
                            .OrderByDescending(GetPoisonBuffEndTime)
                            .FirstOrDefault(m => m.Health < E.GetDamage(m) - 5);
                    if (minion != null)
                    {
                        E.Cast(minion, true);
                    }
                }
            }
        }

        private void OnOrbwalkingBeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                var t = args.Target as Obj_AI_Hero;
                if (t != null && (Q.CanCast(t) || W.CanCast(t) || E.CanCast(t)) && GetPoisonBuffEndTime(t) > 0.4f)
                {
                    args.Process = false;
                }
            }
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Harass || Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                if (Menu.Item(Menu.Name + ".harass.q").GetValue<bool>())
                {
                    QLogic();
                }
                if (Menu.Item(Menu.Name + ".harass.w").GetValue<bool>())
                {
                    WLogic();
                }
                if (Menu.Item(Menu.Name + ".harass.e").GetValue<bool>())
                {
                    ELogic();
                }
                var t = args.Target as Obj_AI_Hero;
                if (t != null &&
                    (Q.CanCast(t) && Menu.Item(Menu.Name + ".harass.q").GetValue<bool>() ||
                     W.CanCast(t) && Menu.Item(Menu.Name + ".harass.w").GetValue<bool>() ||
                     E.CanCast(t) && Menu.Item(Menu.Name + ".harass.e").GetValue<bool>()) && GetPoisonBuffEndTime(t) > 0.4f)
                {
                    args.Process = false;
                }
            }
        }

        private bool IsFacing(Obj_AI_Base source, Obj_AI_Base target)
        {
            if (source == null || target == null)
                return false;
            return source.Direction.To2D().Perpendicular().AngleBetween((target.Position - source.Position).To2D()) < 80.0;
        }

        private void OnInterruptableTarget(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (sender.IsAlly || args.DangerLevel != Interrupter2.DangerLevel.High || !ManaManager.Check("misc"))
                return;

            if (Menu.Item(Menu.Name + ".miscellaneous.r-important").GetValue<bool>())
            {
                Casting.BasicSkillShot(sender, R, HitchanceManager.Get("r"));
            }
        }

        private void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Menu.Item(Menu.Name + ".miscellaneous.r-gapcloser").GetValue<bool>() || !ManaManager.Check("misc"))
            {
                Casting.BasicSkillShot(gapcloser.Sender, R, HitchanceManager.Get("r"));
            }
        }

        protected override void Combo()
        {
            var q = Menu.Item(Menu.Name + ".combo.q").GetValue<bool>();
            var w = Menu.Item(Menu.Name + ".combo.w").GetValue<bool>();
            var e = Menu.Item(Menu.Name + ".combo.e").GetValue<bool>();
            var r = Menu.Item(Menu.Name + ".combo.r").GetValue<bool>();
            var target = TargetSelector.GetTarget(Q.Range);

            if (target == null)
                return;

            if (q)
            {
                QLogic();
            }
            if (w)
            {
                WLogic();
            }
            if (e)
            {
                ELogic();
            }

            var cDmg = CalculateComboDamage(target, q, w, e, r);
            if (cDmg >= target.Health - 20)
            {
                if (r)
                {
                    RLogic(Menu.Item(Menu.Name + ".combo.r-min").GetValue<Slider>().Value,
                        Menu.Item(Menu.Name + ".combo.r-min-facing").GetValue<Slider>().Value);
                }
            }
            if (cDmg*1.5 > target.Health)
            {
                ItemManager.UseComboItems(target);
                SummonerManager.UseComboSummoners(target);
            }
        }

        private void RLogic(int min, int minFacing)
        {
            var target = TargetSelector.GetTarget(R);
            var pred = R.GetPrediction(target, true);
            var rHits = HeroManager.Enemies.Where(x => R.WillHit(x.Position, pred.CastPosition)).ToList();
            var inRange = rHits.Count(enemy => enemy.Position.Distance(Player.Position) < R.Range);
            var isFacing = rHits.Count(enemy => IsFacing(enemy, Player));
            if (isFacing >= minFacing || inRange >= min)
            {
                if (minFacing == 1 && (!target.CanMove || target.IsImmovable || target.IsWindingUp) || minFacing > 1)
                {
                    R.Cast(pred.CastPosition, true);
                }
            }
        }

        private void QLogic()
        {
            var ts = TargetSelector.GetTargets(Q.Range, Q.DamageType).FirstOrDefault(t => GetPoisonBuffEndTime(t.Hero) < Q.Delay*1.2);
            if (ts != null && ts.Hero != null)
            {
                if (_nextPoison < Game.Time || _lastPoison.NetworkId != ts.Hero.NetworkId)
                {
                    _nextPoison = Game.Time + Q.Delay*1.2f;
                    _lastPoison = ts.Hero;
                    Casting.BasicSkillShot(ts.Hero, Q, HitchanceManager.Get("q"));
                }
            }
        }

        private void WLogic()
        {
            var ts =
                TargetSelector.GetTargets(W.Range, W.DamageType)
                    .FirstOrDefault(
                        t =>
                            GetPoisonBuffEndTime(t.Hero) < W.Delay*1.2 ||
                            !IsFacing(t.Hero, Player) && t.Hero.Position.Distance(Player.Position) > W.Range*0.7);
            if (ts != null && ts.Hero != null)
            {
                if (_nextPoison < Game.Time || _lastPoison.NetworkId != ts.Hero.NetworkId)
                {
                    _nextPoison = Game.Time + Q.Delay*1.2f;
                    _lastPoison = ts.Hero;
                    Casting.BasicSkillShot(ts.Hero, W, HitchanceManager.Get("w"));
                }
            }
        }

        private void ELogic()
        {
            var ts =
                TargetSelector.GetTargets(W.Range, W.DamageType)
                    .FirstOrDefault(t => GetPoisonBuffEndTime(t.Hero) > GetEDelay(t.Hero) || E.GetDamage(t.Hero) - 10 > t.Hero.Health);
            if (ts != null && ts.Hero != null)
            {
                Casting.BasicTargetSkill(E, HitchanceManager.Get("e"), true);
            }
        }

        protected override void Harass()
        {
            if (Menu.Item(Menu.Name + ".harass.q").GetValue<bool>())
            {
                QLogic();
            }
            if (Menu.Item(Menu.Name + ".harass.w").GetValue<bool>())
            {
                WLogic();
            }
            if (Menu.Item(Menu.Name + ".harass.e").GetValue<bool>())
            {
                ELogic();
            }
        }

        private float GetPoisonBuffEndTime(Obj_AI_Base target)
        {
            try
            {
                var buffEndTime = target == null || !target.IsValid
                    ? 0
                    : (target.Buffs.Where(buff => buff.Type == BuffType.Poison)
                        .OrderByDescending(buff => buff.EndTime - Game.Time)
                        .Select(buff => buff.EndTime)
                        .FirstOrDefault());
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
            return (E.Delay + (E.Delay > 0 ? ((ObjectManager.Player.ServerPosition.Distance(target.ServerPosition)/(E.Speed/1000))) : 0) +
                    Game.Ping/2f) + 0.1f;
        }

        protected override void LaneClear()
        {
            var q = Menu.Item(Menu.Name + ".lane-clear.q").GetValue<bool>();
            var w = Menu.Item(Menu.Name + ".lane-clear.w").GetValue<bool>();
            if (q || w)
            {
                var minion =
                    MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.NotAlly)
                        .Where(e => GetPoisonBuffEndTime(e) < Q.Delay*1.1 && !e.IsMoving)
                        .ToList();
                if (q)
                {
                    var prediction = Q.GetCircularFarmLocation(minion, Q.Width + 20);
                    if (prediction.MinionsHit >= 2)
                        Q.Cast(prediction.Position);
                }
                if (w)
                {
                    var prediction = W.GetCircularFarmLocation(minion, W.Width + 30);
                    if (prediction.MinionsHit >= 2)
                        W.Cast(prediction.Position);
                }
            }
            if (Menu.Item(Menu.Name + ".lane-clear.e").GetValue<bool>())
            {
                var minion =
                    MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.NotAlly)
                        .Where(e => GetPoisonBuffEndTime(e) > GetEDelay(e) && e.Health > E.GetDamage(e)*2 || e.Health < E.GetDamage(e) - 5)
                        .ToList();
                if (minion.Any())
                {
                    Casting.BasicTargetSkill(minion.First(), E, HitchanceManager.Get("e"), true);
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
                    if (pred.Hitchance >= HitchanceManager.Get("w"))
                    {
                        W.Cast(pred.CastPosition);
                    }
                }
            }
            ItemManager.UseFleeItems();
        }

        protected override void Killsteal()
        {
            KillstealManager.Killsteal();
        }

        protected override void OnDraw()
        {
            var q = Menu.Item(Menu.Name + ".drawing.q").GetValue<Circle>();
            var w = Menu.Item(Menu.Name + ".drawing.w").GetValue<Circle>();
            var e = Menu.Item(Menu.Name + ".drawing.e").GetValue<Circle>();
            var r = Menu.Item(Menu.Name + ".drawing.r").GetValue<Circle>();
            var circleThickness = Menu.Item(Menu.Name + ".drawing.circle-thickness").GetValue<Slider>().Value;

            if (q.Active)
            {
                Render.Circle.DrawCircle(Player.Position, Q.Range, q.Color, circleThickness);
            }
            if (w.Active)
            {
                Render.Circle.DrawCircle(Player.Position, W.Range, w.Color, circleThickness);
            }
            if (e.Active)
            {
                Render.Circle.DrawCircle(Player.Position, E.Range, e.Color, circleThickness);
            }
            if (r.Active)
            {
                Render.Circle.DrawCircle(Player.Position, R.Range, r.Color, circleThickness);
            }
        }
    }
}