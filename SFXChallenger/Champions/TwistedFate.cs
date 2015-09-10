#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 TwistedFate.cs is part of SFXChallenger.

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

#pragma warning disable 618

namespace SFXChallenger.Champions
{
    internal class TwistedFate : Champion
    {
        private const float QAngle = 28 * (float) Math.PI / 180;
        private const float WRedRadius = 200f;
        private MenuItem _eStacks;
        private MenuItem _rMinimap;
        private Obj_AI_Hero _wTarget;
        private float _wTargetEndTime;

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
            AntiGapcloser.OnEnemyGapcloser += OnAntiGapcloserEnemyGapcloser;
            CustomEvents.Unit.OnDash += OnUnitDash;
            Drawing.OnDraw += OnDrawingDraw;
            Drawing.OnEndScene += OnDrawingEndScene;
            Obj_AI_Base.OnProcessSpellCast += OnObjAiBaseProcessSpellCast;
            Orbwalking.BeforeAttack += OnOrbwalkingBeforeAttack;
        }

        protected override void AddToMenu()
        {
            var comboMenu = Menu.AddSubMenu(new Menu("Combo", Menu.Name + ".combo"));
            HitchanceManager.AddToMenu(
                comboMenu.AddSubMenu(new Menu("Hitchance", comboMenu.Name + ".hitchance")), "combo",
                new Dictionary<string, HitChance> { { "Q", HitChance.High } });
            ManaManager.AddToMenu(comboMenu, "combo-blue", ManaCheckType.Minimum, ManaValueType.Percent, "W " + "Blue");
            comboMenu.AddItem(
                new MenuItem(comboMenu.Name + ".gold-percent", "W " + "Gold Health Percent").SetValue(
                    new Slider(20, 5, 75)));
            comboMenu.AddItem(
                new MenuItem(comboMenu.Name + ".red-min", "W " + "Red Min.").SetValue(new Slider(3, 1, 5)));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".q", "Use Q").SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".w", "Use W").SetValue(true));

            var harassMenu = Menu.AddSubMenu(new Menu("Harass", Menu.Name + ".harass"));
            HitchanceManager.AddToMenu(
                harassMenu.AddSubMenu(new Menu("Hitchance", harassMenu.Name + ".hitchance")), "harass",
                new Dictionary<string, HitChance> { { "Q", HitChance.VeryHigh } });
            ManaManager.AddToMenu(harassMenu, "harass", ManaCheckType.Minimum, ManaValueType.Percent);
            ManaManager.AddToMenu(
                harassMenu, "harass-blue", ManaCheckType.Minimum, ManaValueType.Percent, "W " + "Blue", 50);
            harassMenu.AddItem(
                new MenuItem(harassMenu.Name + ".w-card", "W " + "Card").SetValue(
                    new StringList(new[] { "Gold", "Red", "Blue" })));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".w-auto", "Auto Select").SetValue(true));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".q", "Use Q").SetValue(true));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".w", "Use W").SetValue(true));

            var laneclearMenu = Menu.AddSubMenu(new Menu("Lane Clear", Menu.Name + ".lane-clear"));
            ManaManager.AddToMenu(laneclearMenu, "lane-clear", ManaCheckType.Minimum, ManaValueType.Percent);
            ManaManager.AddToMenu(
                laneclearMenu, "lane-clear-blue", ManaCheckType.Minimum, ManaValueType.Percent, "W " + "Blue", 50);
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".q-min", "Q Min.").SetValue(new Slider(3, 1, 5)));
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".q", "Use Q").SetValue(true));
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".w", "Use W").SetValue(true));

            var fleeMenu = Menu.AddSubMenu(new Menu("Flee", Menu.Name + ".flee"));
            fleeMenu.AddItem(new MenuItem(fleeMenu.Name + ".w", "Use W Gold").SetValue(true));

            var miscMenu = Menu.AddSubMenu(new Menu("Misc", Menu.Name + ".miscellaneous"));
            miscMenu.AddItem(
                new MenuItem(miscMenu.Name + ".w-range", "W " + "Range").SetValue(new Slider((int) W.Range, 500, 1000)))
                .ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs args) { W.Range = args.GetNewValue<Slider>().Value; };
            miscMenu.AddItem(
                new MenuItem(miscMenu.Name + ".w-delay", "W " + "Delay").SetValue(new Slider(300, 0, 1000)))
                .ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs args) { Cards.Delay = args.GetNewValue<Slider>().Value; };
            miscMenu.AddItem(
                new MenuItem(miscMenu.Name + ".mode", "Mode").SetValue(new StringList(new[] { "Burst", "Team" })));
            miscMenu.AddItem(new MenuItem(miscMenu.Name + ".r-card", "Pick Card on R").SetValue(true));

            HeroListManager.AddToMenu(
                miscMenu.AddSubMenu(new Menu("Q Gapcloser", miscMenu.Name + "q-gapcloser")), "q-gapcloser", false, false,
                true, false);

            var manualMenu = Menu.AddSubMenu(new Menu("Manual", Menu.Name + ".manual"));
            manualMenu.AddItem(
                new MenuItem(manualMenu.Name + ".blue", "Hotkey Blue").SetValue(new KeyBind('Z', KeyBindType.Press)));
            manualMenu.AddItem(
                new MenuItem(manualMenu.Name + ".red", "Hotkey Red").SetValue(new KeyBind('U', KeyBindType.Press)));
            manualMenu.AddItem(
                new MenuItem(manualMenu.Name + ".gold", "Hotkey Gold").SetValue(new KeyBind('I', KeyBindType.Press)));

            W.Range = Menu.Item(Menu.Name + ".miscellaneous.w-range").GetValue<Slider>().Value;
            Cards.Delay = Menu.Item(Menu.Name + ".miscellaneous.w-delay").GetValue<Slider>().Value;

            IndicatorManager.AddToMenu(DrawingManager.Menu, true);
            IndicatorManager.Add(Q);
            IndicatorManager.Add(
                "W",
                hero =>
                    (W.IsReady() || Cards.Status == SelectStatus.Selecting || Cards.Status == SelectStatus.Ready) &&
                    Cards.Status != SelectStatus.Selected
                        ? W.GetDamage(hero)
                        : (Cards.Status == SelectStatus.Selected
                            ? (Cards.Has(CardColor.Blue)
                                ? W.GetDamage(hero)
                                : (Cards.Has(CardColor.Red) ? W.GetDamage(hero, 1) : W.GetDamage(hero, 2)))
                            : 0));
            IndicatorManager.Add("E", hero => E.Level > 0 && GetEStacks() >= 2 ? E.GetDamage(hero) : 0);
            IndicatorManager.Finale();

            _eStacks = DrawingManager.Add("E " + "Stacks", true);
            _rMinimap = DrawingManager.Add("R " + "Minimap", true);
        }

        protected override void SetupSpells()
        {
            Q = new Spell(SpellSlot.Q, 1450f, DamageType.Magical);
            Q.SetSkillshot(0.25f, 40f, 1000f, false, SkillshotType.SkillshotLine);

            W = new Spell(SpellSlot.W, 785f, DamageType.Magical);
            W.SetSkillshot(0.5f, 100f, Player.BasicAttack.MissileSpeed, false, SkillshotType.SkillshotCircle);

            E = new Spell(SpellSlot.E);

            R = new Spell(SpellSlot.R, 5500f);
        }

        private void OnOrbwalkingBeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            try
            {
                var hero = args.Target as Obj_AI_Hero;
                if (hero != null)
                {
                    args.Process = Cards.Status != SelectStatus.Selecting && !Cards.ShouldWait;
                    if (args.Process)
                    {
                        if (Cards.Has(CardColor.Gold))
                        {
                            _wTarget = hero;
                            _wTargetEndTime = Game.Time + 5f;

                            var target = TargetSelector.GetTarget(W, false);
                            if (target != null && !target.NetworkId.Equals(hero.NetworkId))
                            {
                                Orbwalker.ForceTarget(target);
                                args.Process = false;
                            }
                        }
                    }
                }
                else
                {
                    if (Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Flee &&
                        Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.None)
                    {
                        if (Cards.Has(CardColor.Gold) || Cards.Has(CardColor.Blue))
                        {
                            var targets = TargetSelector.GetTargets(
                                Orbwalking.GetRealAutoAttackRange(null) * 1.25f, DamageType.Magical);
                            if (targets != null)
                            {
                                var target = targets.FirstOrDefault(Orbwalking.InAutoAttackRange);
                                if (target != null)
                                {
                                    Orbwalker.ForceTarget(target);
                                    args.Process = false;
                                }
                            }
                        }
                    }
                }
                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear ||
                    Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit)
                {
                    if (Cards.Has(CardColor.Red))
                    {
                        var target = Orbwalker.ForcedTarget();
                        if (target != null && target.NetworkId != args.Target.NetworkId)
                        {
                            args.Process = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private bool IsWKillable(Obj_AI_Base target, int stage = 0)
        {
            return target != null && W.GetDamage(target, stage) - 5 > target.Health + target.HPRegenRate;
        }

        protected override void OnPreUpdate() {}

        protected override void OnPostUpdate()
        {
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear ||
                Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit)
            {
                if (Cards.Has(CardColor.Red))
                {
                    var range = Player.AttackRange + Player.BoundingRadius * 1.5f;
                    var minions = MinionManager.GetMinions(range, MinionTypes.All, MinionTeam.NotAlly);
                    var pred = MinionManager.GetBestCircularFarmLocation(
                        minions.Select(m => m.Position.To2D()).ToList(), 500, range);
                    var target = minions.OrderBy(m => m.Distance(pred.Position)).FirstOrDefault();
                    if (target != null)
                    {
                        Orbwalker.ForceTarget(target);
                    }
                }
            }
            if (!Cards.ShouldWait && Cards.Status != SelectStatus.Selecting && Cards.Status != SelectStatus.Selected)
            {
                Orbwalker.ForceTarget(null);
            }
            if (Cards.Status != SelectStatus.Selected)
            {
                if (Menu.Item(Menu.Name + ".manual.blue").GetValue<KeyBind>().Active)
                {
                    Cards.Select(CardColor.Blue);
                }
                if (Menu.Item(Menu.Name + ".manual.red").GetValue<KeyBind>().Active)
                {
                    Cards.Select(CardColor.Red);
                }
                if (Menu.Item(Menu.Name + ".manual.gold").GetValue<KeyBind>().Active)
                {
                    Cards.Select(CardColor.Gold);
                }
            }
        }

        private Tuple<int, Vector3> BestQPosition(Obj_AI_Base target, List<Obj_AI_Base> targets, HitChance hitChance)
        {
            var castPos = Vector3.Zero;
            var totalHits = 0;
            try
            {
                var enemies = targets.Where(e => e.IsValidTarget(Q.Range * 1.5f)).ToList();
                var enemyPositions = new List<Tuple<Obj_AI_Base, Vector3>>();
                var circle = new Geometry.Polygon.Circle(Player.Position, Player.BoundingRadius, 30).Points;

                foreach (var h in enemies)
                {
                    var ePred = Q.GetPrediction(h);
                    if (ePred.Hitchance >= hitChance)
                    {
                        circle.Add(Player.Position.Extend(ePred.UnitPosition, Player.BoundingRadius).To2D());
                        enemyPositions.Add(new Tuple<Obj_AI_Base, Vector3>(h, ePred.UnitPosition));
                    }
                }
                var targetPos = target == null ? Vector3.Zero : target.Position;
                if (target == null)
                {
                    var possibilities =
                        ListExtensions.ProduceEnumeration(enemyPositions).Where(p => p.Count > 0).ToList();
                    var count = 0;
                    foreach (var possibility in possibilities)
                    {
                        var mec = MEC.GetMec(possibility.Select(p => p.Item2.To2D()).ToList());
                        if (mec.Radius < Q.Width && possibility.Count > count)
                        {
                            count = possibility.Count;
                            targetPos = mec.Center.To3D();
                        }
                    }
                }
                if (targetPos.Equals(Vector3.Zero))
                {
                    return new Tuple<int, Vector3>(totalHits, castPos);
                }
                circle = circle.OrderBy(c => c.Distance(targetPos)).ToList();
                if (!enemyPositions.Any())
                {
                    return new Tuple<int, Vector3>(totalHits, castPos);
                }

                foreach (var point in circle)
                {
                    var hits = 0;
                    var containsTarget = false;
                    var direction = Q.Range * (point.To3D() - ObjectManager.Player.Position).Normalized().To2D();
                    var rect1 = new Geometry.Polygon.Rectangle(
                        Player.Position, Player.Position.Extend(Player.Position + direction.To3D(), Q.Range), Q.Width);
                    var rect2 = new Geometry.Polygon.Rectangle(
                        Player.Position,
                        Player.Position.Extend(Player.Position + direction.Rotated(QAngle).To3D(), Q.Range), Q.Width);
                    var rect3 = new Geometry.Polygon.Rectangle(
                        Player.Position,
                        Player.Position.Extend(Player.Position + direction.Rotated(-QAngle).To3D(), Q.Range), Q.Width);
                    foreach (var enemy in enemyPositions)
                    {
                        var bounding = new Geometry.Polygon.Circle(enemy.Item2, enemy.Item1.BoundingRadius * 0.85f);
                        if (bounding.Points.Any(p => rect1.IsInside(p) || rect2.IsInside(p) || rect3.IsInside(p)))
                        {
                            hits++;
                            if (target != null && enemy.Item1.NetworkId.Equals(target.NetworkId))
                            {
                                containsTarget = true;
                            }
                        }
                    }
                    if ((containsTarget || target == null) && hits > totalHits)
                    {
                        totalHits = hits;
                        castPos = Player.Position.Extend(point.To3D(), Q.Range);
                        if (totalHits >= enemies.Count)
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
            return new Tuple<int, Vector3>(totalHits, castPos);
        }

        private int GetWHits(Obj_AI_Base target, List<Obj_AI_Base> targets = null, CardColor color = CardColor.Gold)
        {
            try
            {
                if (targets != null && color == CardColor.Red)
                {
                    targets = targets.Where(t => t.IsValidTarget((W.Range + W.Width) * 1.5f)).ToList();
                    var pred = W.GetPrediction(target);
                    if (pred.Hitchance >= HitChance.Medium)
                    {
                        var circle = new Geometry.Polygon.Circle(pred.UnitPosition, target.BoundingRadius + WRedRadius);
                        return 1 + (from t in targets.Where(x => x.NetworkId != target.NetworkId)
                            let pred2 = W.GetPrediction(t)
                            where pred2.Hitchance >= HitChance.Medium
                            select new Geometry.Polygon.Circle(pred2.UnitPosition, t.BoundingRadius * 0.9f)).Count(
                                circle2 => circle2.Points.Any(p => circle.IsInside(p)));
                    }
                }
                if (W.IsInRange(target))
                {
                    return 1;
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return 0;
        }

        private void OnObjAiBaseProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            try
            {
                if (!sender.IsMe || !Menu.Item(Menu.Name + ".miscellaneous.r-card").GetValue<bool>())
                {
                    return;
                }
                if (args.SData.Name.Equals("gate", StringComparison.OrdinalIgnoreCase) && W.IsReady())
                {
                    if (Cards.Status != SelectStatus.Selected)
                    {
                        Cards.Select(CardColor.Gold);
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
                if (sender != null && sender.IsEnemy && args.DangerLevel == Interrupter2.DangerLevel.High &&
                    Orbwalking.InAutoAttackRange(sender))
                {
                    if (Cards.Has(CardColor.Gold))
                    {
                        Orbwalker.ForceTarget(sender);
                        Player.IssueOrder(GameObjectOrder.AttackUnit, sender);
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
                var hero = sender as Obj_AI_Hero;
                if (!sender.IsEnemy || hero == null)
                {
                    return;
                }
                if (HeroListManager.Check("q-gapcloser", hero) && Player.Distance(args.EndPos) <= Q.Range && Q.IsReady())
                {
                    var target = TargetSelector.GetTarget(Q.Range * 0.85f, Q.DamageType);
                    if (target == null || sender.NetworkId.Equals(target.NetworkId))
                    {
                        Q.Cast(args.EndPos);
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnAntiGapcloserEnemyGapcloser(ActiveGapcloser args)
        {
            try
            {
                if (!args.Sender.IsEnemy)
                {
                    return;
                }
                if (HeroListManager.Check("q-gapcloser", args.Sender) && args.End.Distance(Player.Position) < Q.Range &&
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

        protected override void Combo()
        {
            var q = Menu.Item(Menu.Name + ".combo.q").GetValue<bool>();
            var w = Menu.Item(Menu.Name + ".combo.w").GetValue<bool>();

            if (w && W.IsReady())
            {
                var target = TargetSelector.GetTarget(W, false);
                if (target != null)
                {
                    var best = GetBestCard(target, "combo");
                    if (best.Any())
                    {
                        Cards.Select(best);
                    }
                }
            }
            if (q && Q.IsReady())
            {
                var target = TargetSelector.GetTarget(Q);
                var goldCardTarget = _wTarget != null && _wTarget.IsValidTarget(Q.Range) && _wTargetEndTime > Game.Time;
                if (goldCardTarget)
                {
                    target = _wTarget;
                }
                if (target == null || target.Distance(Player) < Player.BoundingRadius && !Utils.IsImmobile(target))
                {
                    return;
                }
                if (!goldCardTarget && (Cards.Has() || HasEBuff()) &&
                    GameObjects.EnemyHeroes.Any(e => Orbwalking.InAutoAttackRange(e) && e.IsValidTarget()) ||
                    Cards.Has(CardColor.Gold))
                {
                    return;
                }
                if (goldCardTarget)
                {
                    if (target.Distance(Player) > 250 && !Utils.IsImmobile(target))
                    {
                        return;
                    }
                    var best = BestQPosition(
                        target, GameObjects.EnemyHeroes.Select(e => e as Obj_AI_Base).ToList(), Q.GetHitChance("combo"));
                    if (!best.Item2.Equals(Vector3.Zero) && best.Item1 >= 1)
                    {
                        Q.Cast(best.Item2);
                        _wTarget = null;
                        _wTargetEndTime = 0;
                    }
                }
                else if (Utils.IsImmobile(target) || (W.Instance.CooldownExpires - Game.Time) >= 2 || W.Level == 0)
                {
                    var best = BestQPosition(
                        target, GameObjects.EnemyHeroes.Select(e => e as Obj_AI_Base).ToList(), Q.GetHitChance("combo"));
                    if (!best.Item2.Equals(Vector3.Zero) && best.Item1 >= 1)
                    {
                        Q.Cast(best.Item2);
                    }
                }
            }
        }

        protected override void Harass()
        {
            var q = Menu.Item(Menu.Name + ".harass.q").GetValue<bool>();
            var w = Menu.Item(Menu.Name + ".harass.w").GetValue<bool>();

            if (w && W.IsReady())
            {
                var target = TargetSelector.GetTarget(W, false);
                if (target != null)
                {
                    var best = GetBestCard(target, "harass");
                    if (best.Any())
                    {
                        Cards.Select(best);
                    }
                }
            }
            if (ManaManager.Check("harass") && q && Q.IsReady())
            {
                var target = TargetSelector.GetTarget(Q, false);
                if (target != null)
                {
                    {
                        var best = BestQPosition(
                            target, GameObjects.EnemyHeroes.Select(e => e as Obj_AI_Base).ToList(),
                            Q.GetHitChance("harass"));
                        if (!best.Item2.Equals(Vector3.Zero) && best.Item1 >= 1)
                        {
                            Q.Cast(best.Item2);
                        }
                    }
                }
            }
        }

        protected override void LaneClear()
        {
            var q = Menu.Item(Menu.Name + ".lane-clear.q").GetValue<bool>();
            var qMin = Menu.Item(Menu.Name + ".lane-clear.q-min").GetValue<Slider>().Value;
            var w = Menu.Item(Menu.Name + ".lane-clear.w").GetValue<bool>();

            if (ManaManager.Check("lane-clear") && q && Q.IsReady())
            {
                var minions = MinionManager.GetMinions(
                    Q.Range * 1.2f, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
                var m = minions.OrderBy(x => x.Distance(Player)).FirstOrDefault();
                if (m == null)
                {
                    return;
                }
                if (m.Team != GameObjectTeam.Neutral)
                {
                    minions.RemoveAll(x => x.Team == GameObjectTeam.Neutral);
                }
                else
                {
                    qMin = 1;
                }
                var best = BestQPosition(null, minions, HitChance.High);
                if (!best.Item2.Equals(Vector3.Zero) && best.Item1 >= qMin)
                {
                    Q.Cast(best.Item2);
                }
            }
            if (w && W.IsReady())
            {
                var minions = MinionManager.GetMinions(
                    W.Range * 1.2f, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
                if (minions.Any())
                {
                    Cards.Select(!ManaManager.Check("lane-clear-blue") ? CardColor.Blue : CardColor.Red);
                }
            }
        }

        private bool HasEBuff()
        {
            return Player.HasBuff("cardmasterstackparticle");
        }

        private int GetEStacks()
        {
            return HasEBuff() ? 3 : Player.GetBuffCount("cardmasterstackholder");
        }

        protected override void Flee()
        {
            if (Menu.Item(Menu.Name + ".flee.w").GetValue<bool>())
            {
                if (W.IsReady() || Cards.Status == SelectStatus.Ready)
                {
                    var target = TargetSelector.GetTarget(W, false);
                    if (target != null)
                    {
                        var best = GetBestCard(target, "flee");
                        if (best.Any())
                        {
                            Cards.Select(best);
                            Orbwalker.ForceTarget(target);
                        }
                    }
                }
                if (Player.CanAttack && (Cards.Has(CardColor.Red) || Cards.Has(CardColor.Gold)))
                {
                    var target =
                        GameObjects.EnemyHeroes.Where(e => Orbwalking.InAutoAttackRange(e) && e.IsValidTarget())
                            .OrderBy(e => e.Distance(Player))
                            .FirstOrDefault();
                    if (target != null)
                    {
                        Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                    }
                }
            }
        }

        protected override void Killsteal() {}

        private void OnDrawingDraw(EventArgs args)
        {
            try
            {
                if (Player.IsDead || !Player.Position.IsOnScreen())
                {
                    return;
                }

                var x = Player.HPBarPosition.X + 45;
                var y = Player.HPBarPosition.Y - 25;

                if (E.Level > 0 && _eStacks != null && _eStacks.GetValue<bool>())
                {
                    var stacks = HasEBuff() ? 3 : Player.GetBuffCount("cardmasterstackholder") - 1;
                    if (stacks > -1)
                    {
                        for (var i = 0; 3 > i; i++)
                        {
                            Drawing.DrawLine(
                                x + (i * 20), y, x + (i * 20) + 10, y, 10, (i > stacks ? Color.DarkGray : Color.Orange));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnDrawingEndScene(EventArgs args)
        {
            try
            {
                if (_rMinimap.GetValue<bool>() && R.Level > 0 && (R.Instance.CooldownExpires - Game.Time) < 3 &&
                    !Player.IsDead)
                {
                    Utility.DrawCircle(Player.Position, R.Range, Color.White, 1, 30, true);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private CardColor GetSelectedCardColor(int index)
        {
            switch (index)
            {
                case 0:
                    return CardColor.Gold;
                case 1:
                    return CardColor.Red;
                case 2:
                    return CardColor.Blue;
            }
            return CardColor.None;
        }

        private List<CardColor> GetBestCard(Obj_AI_Hero target, string mode)
        {
            var cards = new List<CardColor>();
            if (target == null || !target.IsValid || target.IsDead)
            {
                return cards;
            }
            try
            {
                if (IsWKillable(target, 2))
                {
                    cards.Add(CardColor.Gold);
                }
                if (IsWKillable(target))
                {
                    cards.Add(CardColor.Blue);
                }
                if (IsWKillable(target, 1))
                {
                    cards.Add(CardColor.Red);
                }
                if (cards.Any())
                {
                    return cards;
                }
                var burst = Menu.Item(Menu.Name + ".miscellaneous.mode").GetValue<StringList>().SelectedIndex == 0;
                var red = 0;
                var blue = 0;
                var gold = 0;
                if (!burst &&
                    (mode == "combo" || mode == "harass" && Menu.Item(Menu.Name + ".harass.w-auto").GetValue<bool>()))
                {
                    if (Q.Level == 0)
                    {
                        return new List<CardColor> { CardColor.Blue };
                    }
                    gold++;
                    if (target.Distance(Player) > W.Range * 0.8f)
                    {
                        gold++;
                    }
                    if (!ManaManager.Check(mode + "-blue"))
                    {
                        blue = 4;
                    }
                    var minRed = Menu.Item(Menu.Name + ".combo.red-min").GetValue<Slider>().Value;
                    var redHits = GetWHits(target, GameObjects.EnemyHeroes.Cast<Obj_AI_Base>().ToList(), CardColor.Red);
                    red += redHits;
                    if (red > blue && red > gold && redHits >= minRed)
                    {
                        cards.Add(CardColor.Red);
                        if (red == blue)
                        {
                            cards.Add(CardColor.Blue);
                        }
                        if (red == gold)
                        {
                            cards.Add(CardColor.Gold);
                        }
                    }
                    else if (gold > blue && gold > red)
                    {
                        cards.Add(CardColor.Gold);
                        if (gold == blue)
                        {
                            cards.Add(CardColor.Blue);
                        }
                        if (gold == red && redHits >= minRed)
                        {
                            cards.Add(CardColor.Red);
                        }
                    }
                    else if (blue > red && blue > gold)
                    {
                        cards.Add(CardColor.Blue);
                        if (blue == red && redHits >= minRed)
                        {
                            cards.Add(CardColor.Red);
                        }
                        if (blue == gold)
                        {
                            cards.Add(CardColor.Gold);
                        }
                    }
                }
                if (mode == "combo" && !cards.Any())
                {
                    if (Q.Level == 0)
                    {
                        return new List<CardColor> { CardColor.Blue };
                    }
                    var distance = target.Distance(Player);
                    var damage = ItemManager.CalculateComboDamage(target) - target.HPRegenRate * 2f - 10;
                    if (HasEBuff())
                    {
                        damage += E.GetDamage(target);
                    }
                    if (Q.IsReady() && (Utils.GetImmobileTime(target) > 0.5f || distance < Q.Range / 4f))
                    {
                        damage += Q.GetDamage(target);
                    }
                    if (W.GetDamage(target, 2) + damage > target.Health)
                    {
                        cards.Add(CardColor.Gold);
                    }
                    if (distance < Orbwalking.GetRealAutoAttackRange(target) * 0.85f)
                    {
                        if (W.GetDamage(target) + damage > target.Health)
                        {
                            cards.Add(CardColor.Blue);
                        }
                        if (W.GetDamage(target, 1) + damage > target.Health)
                        {
                            cards.Add(CardColor.Red);
                        }
                    }

                    var blueMana1 = (ObjectManager.Player.Mana < (W.Instance.ManaCost + Q.Instance.ManaCost) &&
                                     ObjectManager.Player.Mana > (Q.Instance.ManaCost - 10));
                    var blueMana2 = (ObjectManager.Player.Mana < (W.Instance.ManaCost + Q.Instance.ManaCost) &&
                                     ObjectManager.Player.Mana > (Q.Instance.ManaCost - 20));
                    var blueMana3 = (ObjectManager.Player.Mana < (W.Instance.ManaCost + Q.Instance.ManaCost));
                    if (!cards.Any())
                    {
                        if (ObjectManager.Player.HealthPercent <=
                            Menu.Item(Menu.Name + ".combo.gold-percent").GetValue<Slider>().Value)
                        {
                            cards.Add(CardColor.Gold);
                        }
                        else if ((!ManaManager.Check("combo-blue") || (W.Level == 1 && blueMana1) ||
                                  W.Level == 2 && blueMana2) || W.Level > 2 && blueMana3)
                        {
                            cards.Add(CardColor.Blue);
                        }
                        else
                        {
                            var redHits = GetWHits(
                                target, GameObjects.EnemyHeroes.Cast<Obj_AI_Base>().ToList(), CardColor.Red);
                            if (redHits >= Menu.Item(Menu.Name + ".combo.red-min").GetValue<Slider>().Value)
                            {
                                cards.Add(CardColor.Red);
                            }
                        }
                    }
                    if (!cards.Any())
                    {
                        cards.Add(CardColor.Gold);
                    }
                }
                else if (mode == "harass" && !cards.Any())
                {
                    if (Menu.Item(Menu.Name + ".harass.w-auto").GetValue<bool>() && burst)
                    {
                        cards.Add(target.Distance(Player) > W.Range * 0.8f ? CardColor.Gold : CardColor.Blue);
                    }
                    else
                    {
                        var card = !ManaManager.Check("harass-blue")
                            ? CardColor.Blue
                            : GetSelectedCardColor(
                                Menu.Item(Menu.Name + ".harass.w-card").GetValue<StringList>().SelectedIndex);
                        if (card != CardColor.None)
                        {
                            cards.Add(card);
                        }
                    }
                }
                else if (mode == "flee")
                {
                    cards.Add(
                        GetWHits(target, GameObjects.EnemyHeroes.Cast<Obj_AI_Base>().ToList(), CardColor.Red) >= 2
                            ? CardColor.Red
                            : CardColor.Gold);
                }
                if (!cards.Any())
                {
                    cards.Add(CardColor.Gold);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return cards;
        }

        internal enum CardColor
        {
            Red,

            Gold,

            Blue,

            None
        }

        internal enum SelectStatus
        {
            Selecting,

            Selected,

            Ready,

            Cooldown,

            None
        }

        public static class Cards
        {
            public static List<CardColor> ShouldSelect;
            public static CardColor LastCard;
            private static int _lastWSent;

            static Cards()
            {
                LastCard = CardColor.None;
                ShouldSelect = new List<CardColor>();
                Obj_AI_Base.OnProcessSpellCast += OnObjAiBaseProcessSpellCast;
                Game.OnUpdate += OnGameUpdate;
            }

            public static SelectStatus Status { get; set; }

            public static bool ShouldWait
            {
                get { return LeagueSharp.Common.Utils.TickCount - _lastWSent <= Delay; }
            }

            public static int Delay { get; set; }

            public static bool Has(CardColor color)
            {
                return color == CardColor.Gold && ObjectManager.Player.HasBuff("goldcardpreattack") ||
                       color == CardColor.Red && ObjectManager.Player.HasBuff("redcardpreattack") ||
                       color == CardColor.Blue && ObjectManager.Player.HasBuff("bluecardpreattack");
            }

            public static bool Has()
            {
                return ObjectManager.Player.HasBuff("goldcardpreattack") ||
                       ObjectManager.Player.HasBuff("redcardpreattack") ||
                       ObjectManager.Player.HasBuff("bluecardpreattack");
            }

            public static void Select(CardColor card)
            {
                try
                {
                    Select(new List<CardColor> { card });
                }
                catch (Exception ex)
                {
                    Global.Logger.AddItem(new LogItem(ex));
                }
            }

            public static void Select(List<CardColor> cards)
            {
                try
                {
                    if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Name == "PickACard" &&
                        Status == SelectStatus.Ready)
                    {
                        ShouldSelect = cards;
                        if (ShouldSelect.Any())
                        {
                            if (!ShouldWait)
                            {
                                if (ObjectManager.Player.Spellbook.CastSpell(SpellSlot.W, ObjectManager.Player))
                                {
                                    _lastWSent = LeagueSharp.Common.Utils.TickCount;
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

            private static void OnGameUpdate(EventArgs args)
            {
                try
                {
                    var wName = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Name;
                    var wState = ObjectManager.Player.Spellbook.CanUseSpell(SpellSlot.W);

                    if ((wState == SpellState.Ready && wName == "PickACard" &&
                         (Status != SelectStatus.Selecting || !ShouldWait)) || ObjectManager.Player.IsDead)
                    {
                        Status = SelectStatus.Ready;
                    }
                    else if (wState == SpellState.Cooldown && wName == "PickACard")
                    {
                        ShouldSelect.Clear();
                        Status = SelectStatus.Cooldown;
                    }
                    else if (wState == SpellState.Surpressed && !ObjectManager.Player.IsDead)
                    {
                        Status = SelectStatus.Selected;
                    }
                    if (
                        ShouldSelect.Any(
                            s =>
                                s == CardColor.Blue && wName == "bluecardlock" ||
                                s == CardColor.Gold && wName == "goldcardlock" ||
                                s == CardColor.Red && wName == "redcardlock"))
                    {
                        ObjectManager.Player.Spellbook.CastSpell(SpellSlot.W, false);
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

                    if (args.SData.Name == "PickACard")
                    {
                        Status = SelectStatus.Selecting;
                    }
                    if (args.SData.Name == "goldcardlock")
                    {
                        LastCard = CardColor.Gold;
                        Status = SelectStatus.Selected;
                    }
                    else if (args.SData.Name == "bluecardlock")
                    {
                        LastCard = CardColor.Blue;
                        Status = SelectStatus.Selected;
                    }
                    else if (args.SData.Name == "redcardlock")
                    {
                        LastCard = CardColor.Red;
                        Status = SelectStatus.Selected;
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