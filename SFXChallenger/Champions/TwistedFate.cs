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
using SFXChallenger.Managers;
using SFXLibrary;
using SFXLibrary.Extensions.NET;
using SFXLibrary.Logger;
using SharpDX;
using Color = System.Drawing.Color;
using MinionManager = SFXLibrary.MinionManager;
using MinionOrderTypes = SFXLibrary.MinionOrderTypes;
using MinionTeam = SFXLibrary.MinionTeam;
using MinionTypes = SFXLibrary.MinionTypes;
using Orbwalking = SFXChallenger.Wrappers.Orbwalking;
using TargetSelector = SFXChallenger.Wrappers.TargetSelector;
using Utils = LeagueSharp.Common.Utils;

#endregion

#pragma warning disable 618

namespace SFXChallenger.Champions
{
    internal class TwistedFate : Champion
    {
        private readonly float _qAngle = 28 * (float) Math.PI / 180;
        private readonly float _wRedRadius = 110f;
        private MenuItem _eStacks;
        private MenuItem _rMinimap;

        protected override ItemFlags ItemFlags
        {
            get { return ItemFlags.Offensive | ItemFlags.Defensive | ItemFlags.Flee; }
        }

        protected override void OnLoad()
        {
            Core.OnPostUpdate += OnCorePostUpdate;
            Interrupter2.OnInterruptableTarget += OnInterruptableTarget;
            AntiGapcloser.OnEnemyGapcloser += OnAntiGapcloserEnemyGapcloser;
            CustomEvents.Unit.OnDash += OnUnitDash;
            Drawing.OnDraw += OnDrawingDraw;
            Drawing.OnEndScene += OnDrawingEndScene;
            Obj_AI_Base.OnProcessSpellCast += OnObjAiBaseProcessSpellCast;
            Orbwalking.BeforeAttack += OnOrbwalkingBeforeAttack;
        }

        protected override void OnUnload()
        {
            Core.OnPostUpdate -= OnCorePostUpdate;
            Interrupter2.OnInterruptableTarget -= OnInterruptableTarget;
            AntiGapcloser.OnEnemyGapcloser -= OnAntiGapcloserEnemyGapcloser;
            CustomEvents.Unit.OnDash -= OnUnitDash;
            Drawing.OnDraw -= OnDrawingDraw;
            Drawing.OnEndScene -= OnDrawingEndScene;
            Obj_AI_Base.OnProcessSpellCast -= OnObjAiBaseProcessSpellCast;
            Orbwalking.BeforeAttack -= OnOrbwalkingBeforeAttack;
        }

        protected override void AddToMenu()
        {
            var comboMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Combo"), Menu.Name + ".combo"));
            HitchanceManager.AddToMenu(
                comboMenu.AddSubMenu(new Menu(Global.Lang.Get("F_MH"), comboMenu.Name + ".hitchance")), "combo",
                new Dictionary<string, int> { { "Q", 1 } });
            ManaManager.AddToMenu(
                comboMenu, "combo-blue", ManaCheckType.Minimum, ManaValueType.Percent, "W " + Global.Lang.Get("TF_Blue"));
            comboMenu.AddItem(
                new MenuItem(
                    comboMenu.Name + ".gold-percent",
                    "W " + Global.Lang.Get("TF_Gold") + " " + Global.Lang.Get("G_HealthPercent")).SetValue(
                        new Slider(20, 5, 75)));
            comboMenu.AddItem(
                new MenuItem(
                    comboMenu.Name + ".red-min", "W " + Global.Lang.Get("TF_Red") + " " + Global.Lang.Get("G_Min"))
                    .SetValue(new Slider(3, 1, 5)));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".w", Global.Lang.Get("G_UseW")).SetValue(true));

            var harassMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Harass"), Menu.Name + ".harass"));
            HitchanceManager.AddToMenu(
                harassMenu.AddSubMenu(new Menu(Global.Lang.Get("F_MH"), harassMenu.Name + ".hitchance")), "harass",
                new Dictionary<string, int> { { "Q", 1 } });
            ManaManager.AddToMenu(harassMenu, "harass", ManaCheckType.Minimum, ManaValueType.Percent);
            ManaManager.AddToMenu(
                harassMenu, "harass-blue", ManaCheckType.Minimum, ManaValueType.Percent,
                "W " + Global.Lang.Get("TF_Blue"), 50);
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));
            harassMenu.AddItem(
                new MenuItem(harassMenu.Name + ".w-card", "W " + Global.Lang.Get("TF_Card")).SetValue(
                    new StringList(Global.Lang.GetList("TF_Cards"))));
            harassMenu.AddItem(
                new MenuItem(harassMenu.Name + ".w-auto", Global.Lang.Get("TF_AutoSelect")).SetValue(true));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".w", Global.Lang.Get("G_UseW")).SetValue(true));

            var laneclearMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_LaneClear"), Menu.Name + ".lane-clear"));
            ManaManager.AddToMenu(laneclearMenu, "lane-clear", ManaCheckType.Minimum, ManaValueType.Percent);
            ManaManager.AddToMenu(
                laneclearMenu, "lane-clear-blue", ManaCheckType.Minimum, ManaValueType.Percent,
                "W " + Global.Lang.Get("TF_Blue"), 50);
            laneclearMenu.AddItem(
                new MenuItem(laneclearMenu.Name + ".q-min", "Q " + Global.Lang.Get("G_Min")).SetValue(
                    new Slider(3, 1, 5)));
            laneclearMenu.AddItem(
                new MenuItem(
                    laneclearMenu.Name + ".red-min", "W " + Global.Lang.Get("TF_Red") + " " + Global.Lang.Get("G_Min"))
                    .SetValue(new Slider(3, 1, 5)));
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".w", Global.Lang.Get("G_UseW")).SetValue(true));

            var fleeMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Flee"), Menu.Name + ".flee"));
            fleeMenu.AddItem(
                new MenuItem(fleeMenu.Name + ".w", Global.Lang.Get("G_UseW") + " " + Global.Lang.Get("TF_Gold"))
                    .SetValue(true));

            var miscMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Miscellaneous"), Menu.Name + ".miscellaneous"));
            miscMenu.AddItem(
                new MenuItem(miscMenu.Name + ".q-range", "Q " + Global.Lang.Get("G_Range")).SetValue(
                    new Slider((int) Q.Range, 950, 1450))).ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs args) { Q.Range = args.GetNewValue<Slider>().Value; };
            miscMenu.AddItem(
                new MenuItem(miscMenu.Name + ".w-range", "W " + Global.Lang.Get("G_Range")).SetValue(
                    new Slider((int) W.Range, 500, 1000))).ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs args) { W.Range = args.GetNewValue<Slider>().Value; };
            miscMenu.AddItem(
                new MenuItem(miscMenu.Name + ".mode", Global.Lang.Get("G_Mode")).SetValue(
                    new StringList(Global.Lang.GetList("TF_Modes"))));
            miscMenu.AddItem(new MenuItem(miscMenu.Name + ".r-card", Global.Lang.Get("TF_RCard")).SetValue(true));
            miscMenu.AddItem(
                new MenuItem(miscMenu.Name + ".q-stunned", "Q " + Global.Lang.Get("G_Stunned")).SetValue(true));
            miscMenu.AddItem(new MenuItem(miscMenu.Name + ".q-dash", "Q " + Global.Lang.Get("G_Dash")).SetValue(true));
            miscMenu.AddItem(
                new MenuItem(miscMenu.Name + ".q-gap", "Q " + Global.Lang.Get("G_Gapcloser")).SetValue(true));
            miscMenu.AddItem(
                new MenuItem(miscMenu.Name + ".w-interrupt", "W " + Global.Lang.Get("G_InterruptSpell")).SetValue(true));

            var manualMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Manual"), Menu.Name + ".manual"));
            manualMenu.AddItem(
                new MenuItem(manualMenu.Name + ".blue", Global.Lang.Get("G_Hotkey") + " " + Global.Lang.Get("TF_Blue"))
                    .SetValue(new KeyBind('Z', KeyBindType.Press)));
            manualMenu.AddItem(
                new MenuItem(manualMenu.Name + ".red", Global.Lang.Get("G_Hotkey") + " " + Global.Lang.Get("TF_Red"))
                    .SetValue(new KeyBind('U', KeyBindType.Press)));
            manualMenu.AddItem(
                new MenuItem(manualMenu.Name + ".gold", Global.Lang.Get("G_Hotkey") + " " + Global.Lang.Get("TF_Gold"))
                    .SetValue(new KeyBind('I', KeyBindType.Press)));

            Q.Range = Menu.Item(Menu.Name + ".miscellaneous.q-range").GetValue<Slider>().Value;
            W.Range = Menu.Item(Menu.Name + ".miscellaneous.w-range").GetValue<Slider>().Value;

            IndicatorManager.AddToMenu(DrawingManager.GetMenu(), true);
            IndicatorManager.Add(Q);
            IndicatorManager.Add(W);
            IndicatorManager.Add(E, false);
            IndicatorManager.Finale();

            _eStacks = DrawingManager.Add("E " + Global.Lang.Get("G_Stacks"), true);
            _rMinimap = DrawingManager.Add("R " + Global.Lang.Get("G_Minimap"), true);
        }

        protected override void SetupSpells()
        {
            Q = new Spell(SpellSlot.Q, 1450f);
            Q.SetSkillshot(0.25f, 40f, 1000f, false, SkillshotType.SkillshotLine);

            W = new Spell(SpellSlot.W, 785f);
            W.SetSkillshot(0.5f, 100f, Player.BasicAttack.MissileSpeed, false, SkillshotType.SkillshotCircle);

            E = new Spell(SpellSlot.E);

            R = new Spell(SpellSlot.R, 5500f);
        }

        private void OnOrbwalkingBeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            try
            {
                if (args.Target is Obj_AI_Hero)
                {
                    args.Process = Cards.Status != SelectStatus.Selecting && Utils.TickCount - Cards.LastWSent > 300;
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private bool IsWKillable(Obj_AI_Base target, int stage = 0)
        {
            return W.GetDamage(target, stage) - 5 > target.Health + target.HPRegenRate;
        }

        private void OnCorePostUpdate(EventArgs args)
        {
            try
            {
                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear && Cards.Status == SelectStatus.Selected)
                {
                    if (Cards.Has(CardColor.Red) || Cards.Has(CardColor.Blue))
                    {
                        var best = GetBestLaneClearTargetCard();
                        if (best.Item1 != null && best.Item2.Any())
                        {
                            Orbwalker.ForceTarget(best.Item1);
                        }
                    }
                    else
                    {
                        Orbwalker.ForceTarget(null);
                    }
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
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
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
                    if (ePred.Hitchance >= (hitChance - 1))
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
                        Player.Position.Extend(Player.Position + direction.Rotated(_qAngle).To3D(), Q.Range), Q.Width);
                    var rect3 = new Geometry.Polygon.Rectangle(
                        Player.Position,
                        Player.Position.Extend(Player.Position + direction.Rotated(-_qAngle).To3D(), Q.Range), Q.Width);
                    foreach (var enemy in enemyPositions)
                    {
                        var bounding = new Geometry.Polygon.Circle(enemy.Item2, enemy.Item1.BoundingRadius);
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
                    targets = targets.Where(t => t.IsValidTarget(W.Range)).ToList();
                    var enemyPositions = (from h in targets
                        let wPred = W.GetPrediction(h)
                        where wPred.Hitchance >= HitChance.Medium
                        select new Tuple<Obj_AI_Base, Vector3>(h, wPred.UnitPosition)).ToList();
                    if (enemyPositions.Any())
                    {
                        var enemy = enemyPositions.FirstOrDefault(e => e.Item1.NetworkId.Equals(target.NetworkId));
                        if (enemy != null)
                        {
                            return enemyPositions.Count(e => e.Item2.Distance(enemy.Item2) < _wRedRadius);
                        }
                    }
                }
                var pred = W.GetPrediction(target);
                if (pred.Hitchance >= HitChance.Medium)
                {
                    if (Player.Distance(pred.UnitPosition) <
                        Player.AttackRange + Player.BoundingRadius + target.BoundingRadius)
                    {
                        return 1;
                    }
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
                if (sender.IsEnemy && args.DangerLevel == Interrupter2.DangerLevel.High &&
                    Menu.Item(Menu.Name + ".miscellaneous.w-interrupt").GetValue<bool>())
                {
                    if (Cards.Status != SelectStatus.Selected && W.IsReady())
                    {
                        Cards.Select(CardColor.Gold);
                        Orbwalker.ForceTarget(sender);
                    }
                    else
                    {
                        if (Cards.Has(CardColor.Gold))
                        {
                            Orbwalker.ForceTarget(sender);
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
                if (Menu.Item(Menu.Name + ".miscellaneous.q-dash").GetValue<bool>() &&
                    Player.Distance(endPos) <= Q.Range && Q.IsReady())
                {
                    Q.Cast(endPos);
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

                var endPos = args.End;
                if (args.Sender.ChampionName.Equals("Fizz", StringComparison.OrdinalIgnoreCase))
                {
                    endPos = args.Start.Extend(endPos, 550);
                }

                if (Menu.Item(Menu.Name + ".miscellaneous.q-gap").GetValue<bool>() && Q.IsReady())
                {
                    if (endPos.Distance(Player.Position) < Q.Range)
                    {
                        Q.Cast(endPos);
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
                var target = TargetSelector.GetTarget(
                    W.Range * 1.2f, LeagueSharp.Common.TargetSelector.DamageType.Magical);
                var best = GetBestCard(target, "combo");
                if (best.Any())
                {
                    Cards.Select(best);
                }
            }
            if (q && Q.IsReady())
            {
                QLogic(Q.GetHitChance("harass"));
            }
        }

        protected override void Harass()
        {
            var q = Menu.Item(Menu.Name + ".harass.q").GetValue<bool>();
            var w = Menu.Item(Menu.Name + ".harass.w").GetValue<bool>();

            if (w && W.IsReady())
            {
                var target = TargetSelector.GetTarget(
                    W.Range * 1.2f, LeagueSharp.Common.TargetSelector.DamageType.Magical);
                var best = GetBestCard(target, "harass");
                if (best.Any())
                {
                    Cards.Select(best);
                }
            }
            if (ManaManager.Check("harass") && q && Q.IsReady())
            {
                QLogic(Q.GetHitChance("harass"));
            }
        }

        private void QLogic(HitChance hitChance)
        {
            if ((Cards.Has() || HasEBuff()) &&
                GameObjects.EnemyHeroes.Any(e => Orbwalking.InAutoAttackRange(e) && e.IsValidTarget()))
            {
                return;
            }
            var target = TargetSelector.GetTarget(Q.Range, LeagueSharp.Common.TargetSelector.DamageType.Magical);
            var cd = W.Instance.CooldownExpires - Game.Time;
            var inRange = Orbwalking.InAutoAttackRange(target);
            var best = BestQPosition(target, GameObjects.EnemyHeroes.Cast<Obj_AI_Base>().ToList(), hitChance);
            if (!best.Item2.Equals(Vector3.Zero) &&
                (best.Item1 >= 2 || inRange && cd <= 2 || !inRange || cd > 2 || Helpers.Utils.IsStunned(target)))
            {
                Q.Cast(best.Item2);
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
                var best = BestQPosition(null, minions, HitChance.High);
                if (!best.Item2.Equals(Vector3.Zero) && best.Item1 >= qMin)
                {
                    Q.Cast(best.Item2);
                }
            }
            if (w && W.IsReady())
            {
                var best = GetBestLaneClearTargetCard();
                if (best.Item1 != null && best.Item2.Any())
                {
                    Cards.Select(best.Item2);
                    Orbwalker.ForceTarget(best.Item1);
                }
            }
        }

        private bool HasEBuff()
        {
            return Player.HasBuff("cardmasterstackparticle");
        }

        protected override void Flee()
        {
            if (Menu.Item(Menu.Name + ".flee.w").GetValue<bool>() && W.IsReady())
            {
                if (Cards.Has())
                {
                    var target =
                        GameObjects.EnemyHeroes.OrderBy(e => e.Distance(Player))
                            .FirstOrDefault(e => Orbwalking.InAutoAttackRange(e) && e.IsValidTarget());
                    if (target != null)
                    {
                        Orbwalking.Orbwalk(target, Game.CursorPos);
                    }
                }
                else
                {
                    var target = TargetSelector.GetTarget(
                        W.Range * 1.2f, LeagueSharp.Common.TargetSelector.DamageType.Magical);
                    var best = GetBestCard(target, "flee");
                    if (best.Any())
                    {
                        Cards.Select(best);
                        Orbwalker.ForceTarget(target);
                    }
                }
            }
        }

        protected override void Killsteal() {}

        private void OnDrawingDraw(EventArgs args)
        {
            try
            {
                if (E.Level > 0 && _eStacks != null && _eStacks.GetValue<bool>() && !Player.IsDead &&
                    Player.Position.IsOnScreen())
                {
                    var stacks = HasEBuff() ? 3 : Player.GetBuffCount("cardmasterstackholder") - 1;
                    if (stacks > -1)
                    {
                        var x = Player.HPBarPosition.X + 45;
                        var y = Player.HPBarPosition.Y - 25;
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

        private Tuple<int, Obj_AI_Base> GetBestRedMinion(List<Obj_AI_Base> minions)
        {
            var totalHits = 0;
            Obj_AI_Base target = null;
            try
            {
                foreach (var minion in minions)
                {
                    var redHits = GetWHits(minion, minions, CardColor.Red);
                    if (redHits > totalHits)
                    {
                        totalHits = redHits;
                        target = minion;
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return new Tuple<int, Obj_AI_Base>(totalHits, target);
        }

        private Tuple<Obj_AI_Base, List<CardColor>> GetBestLaneClearTargetCard()
        {
            var cards = new List<CardColor>();
            Obj_AI_Base target = null;
            try
            {
                if (!ManaManager.Check("lane-clear-blue"))
                {
                    var minions = MinionManager.GetMinions(
                        W.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
                    if (minions.Any())
                    {
                        var best = minions.FirstOrDefault(m => W.IsKillable(m) || m.Health > W.GetDamage(m) * 2);
                        target = best ?? minions.First();
                        cards.Add(CardColor.Blue);
                    }
                }
                else
                {
                    var minions = MinionManager.GetMinions(
                        W.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
                    var minHits = minions.Any(m => m.Team == GameObjectTeam.Neutral)
                        ? Menu.Item(Menu.Name + ".lane-clear.red-min").GetValue<Slider>().Value
                        : 2;
                    if (Cards.Has(CardColor.Red))
                    {
                        minHits = 1;
                    }
                    var best = GetBestRedMinion(minions);
                    if (best.Item2 != null && best.Item1 >= minHits)
                    {
                        cards.Add(CardColor.Red);
                        target = best.Item2;
                    }
                    else
                    {
                        cards.Add(CardColor.Blue);
                        target = best.Item2;
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return new Tuple<Obj_AI_Base, List<CardColor>>(target, cards);
        }

        private List<CardColor> GetBestCard(Obj_AI_Hero target, string mode)
        {
            var cards = new List<CardColor>();
            try
            {
                if (IsWKillable(target, 1))
                {
                    cards.Add(CardColor.Red);
                }
                if (IsWKillable(target, 2))
                {
                    cards.Add(CardColor.Gold);
                }
                if (IsWKillable(target))
                {
                    cards.Add(CardColor.Blue);
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
                    gold++;
                    if (target.Distance(Player) > W.Range * 0.8f)
                    {
                        gold++;
                    }
                    if (!ManaManager.Check(mode + "-blue"))
                    {
                        blue = 4;
                    }
                    var minRed = Menu.Item(Menu.Name + ".lane-clear.red-min").GetValue<Slider>().Value;
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
                    var distance = target.Distance(ObjectManager.Player);
                    var damage = ItemManager.CalculateComboDamage(target) - target.HPRegenRate * 2f - 10;
                    if (HasEBuff())
                    {
                        damage += E.GetDamage(target);
                    }
                    if (Q.IsReady() && Helpers.Utils.IsStunned(target) && distance < Q.Range / 3f ||
                        distance < Q.Range / 5f)
                    {
                        damage += Q.GetDamage(target) * 0.9f;
                    }
                    if (W.GetDamage(target, 2) + damage > target.Health)
                    {
                        cards.Add(CardColor.Gold);
                    }
                    if (W.GetDamage(target, 1) + damage > target.Health)
                    {
                        cards.Add(CardColor.Red);
                    }
                    if (W.GetDamage(target) + damage > target.Health)
                    {
                        cards.Add(CardColor.Blue);
                    }
                    if (!cards.Any())
                    {
                        if (ObjectManager.Player.HealthPercent <=
                            Menu.Item(Menu.Name + ".combo.gold-percent").GetValue<Slider>().Value)
                        {
                            cards.Add(CardColor.Gold);
                        }
                        else if (!ManaManager.Check("combo-blue"))
                        {
                            cards.Add(CardColor.Blue);
                        }
                        else
                        {
                            var redHits = GetWHits(
                                target, GameObjects.EnemyHeroes.Cast<Obj_AI_Base>().ToList(), CardColor.Red);
                            if (redHits >= 3)
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
                    red += GetWHits(target, GameObjects.EnemyHeroes.Cast<Obj_AI_Base>().ToList(), CardColor.Red);
                    if (red > gold || red == gold)
                    {
                        cards.Add(CardColor.Red);
                    }
                    else if (gold > red || red == gold)
                    {
                        cards.Add(CardColor.Gold);
                    }
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
            public static int LastWSent;
            public static int LastSendWSent;

            static Cards()
            {
                ShouldSelect = new List<CardColor>();
                Obj_AI_Base.OnProcessSpellCast += OnObjAiBaseProcessSpellCast;
                Game.OnUpdate += OnGameUpdate;
            }

            public static SelectStatus Status { get; set; }

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

            private static void SendWPacket()
            {
                LastSendWSent = Utils.TickCount;
                ObjectManager.Player.Spellbook.CastSpell(SpellSlot.W, false);
            }

            public static void Select(CardColor card)
            {
                try
                {
                    if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Name == "PickACard" &&
                        Status == SelectStatus.Ready)
                    {
                        ShouldSelect.Clear();
                        ShouldSelect = new List<CardColor> { card };
                        if (Utils.TickCount - LastWSent > 200)
                        {
                            if (ObjectManager.Player.Spellbook.CastSpell(SpellSlot.W, ObjectManager.Player))
                            {
                                LastWSent = Utils.TickCount;
                            }
                        }
                    }
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
                        ShouldSelect.Clear();
                        ShouldSelect = cards;
                        if (cards.Any())
                        {
                            if (Utils.TickCount - LastWSent > 200)
                            {
                                if (ObjectManager.Player.Spellbook.CastSpell(SpellSlot.W, ObjectManager.Player))
                                {
                                    LastWSent = Utils.TickCount;
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
                         (Status != SelectStatus.Selecting || Utils.TickCount - LastWSent > 500)) ||
                        ObjectManager.Player.IsDead)
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
                        SendWPacket();
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

                    if (args.SData.Name == "goldcardlock" || args.SData.Name == "bluecardlock" ||
                        args.SData.Name == "redcardlock")
                    {
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