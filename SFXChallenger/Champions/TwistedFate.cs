#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 twistedfate.cs is part of SFXChallenger.

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
using Utils = SFXChallenger.Helpers.Utils;

#endregion

#pragma warning disable 618

#endregion

namespace SFXChallenger.Champions
{
    internal class TwistedFate : Champion
    {
        private readonly float _qAngle = 28 * (float) Math.PI / 180;
        private readonly float _wRedRadius = 100f;
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
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".w", Global.Lang.Get("G_UseW")).SetValue(true));

            var harassMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Harass"), Menu.Name + ".harass"));
            HitchanceManager.AddToMenu(
                harassMenu.AddSubMenu(new Menu(Global.Lang.Get("F_MH"), harassMenu.Name + ".hitchance")), "harass",
                new Dictionary<string, int> { { "Q", 1 } });
            ManaManager.AddToMenu(harassMenu, "harass", ManaCheckType.Minimum, ManaValueType.Percent);
            ManaManager.AddToMenu(
                harassMenu, "harass-blue", ManaCheckType.Minimum, ManaValueType.Percent,
                "W " + Global.Lang.Get("TF_Blue"));
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
                "W " + Global.Lang.Get("TF_Blue"));
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));
            laneclearMenu.AddItem(
                new MenuItem(laneclearMenu.Name + ".q-min", "Q " + Global.Lang.Get("G_Min")).SetValue(
                    new Slider(3, 1, 5)));
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".w", Global.Lang.Get("G_UseW")).SetValue(true));

            var fleeMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Flee"), Menu.Name + ".flee"));
            fleeMenu.AddItem(
                new MenuItem(fleeMenu.Name + ".w", Global.Lang.Get("G_UseW") + " " + Global.Lang.Get("TF_Gold"))
                    .SetValue(true));

            var miscMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Miscellaneous"), Menu.Name + ".miscellaneous"));
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

            IndicatorManager.AddToMenu(DrawingManager.GetMenu(), true);
            IndicatorManager.Add(
                "Combo", delegate(Obj_AI_Hero target)
                {
                    float damage = 0;
                    if (Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady())
                    {
                        damage += Q.GetDamage(target);
                    }
                    if (Menu.Item(Menu.Name + ".combo.w").GetValue<bool>() && W.IsReady())
                    {
                        damage += W.GetDamage(target);
                    }
                    if (HasEBuff())
                    {
                        damage += E.GetDamage(target);
                    }
                    damage += ItemManager.CalculateComboDamage(target);
                    damage += SummonerManager.CalculateComboDamage(target);
                    return damage;
                });
            IndicatorManager.Finale();

            _eStacks = DrawingManager.Add("E " + Global.Lang.Get("G_Stacks"), true);
            _rMinimap = DrawingManager.Add("R " + Global.Lang.Get("G_Minimap"), true);
        }

        protected override void SetupSpells()
        {
            Q = new Spell(SpellSlot.Q, 1450f);
            Q.SetSkillshot(0.25f, 40f, 1000f, false, SkillshotType.SkillshotLine);

            W = new Spell(SpellSlot.W, (Player.AttackRange + Player.BoundingRadius) * 1.05f);
            W.SetSkillshot(0.5f, 100f, Player.BasicAttack.MissileSpeed, false, SkillshotType.SkillshotCircle);

            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R, 5500f);
        }

        private void OnCorePostUpdate(EventArgs args)
        {
            try
            {
                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
                {
                    if (Cards.Has() && Cards.Status != SelectStatus.Selecting)
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
            return 0;
        }

        private void OnObjAiBaseProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
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

            if (q && Q.IsReady())
            {
                QLogic(Q.GetHitChance("harass"));
            }
            if (w && W.IsReady() && Cards.Status == SelectStatus.None)
            {
                var target = TargetSelector.GetTarget(
                    W.Range * 1.2f, LeagueSharp.Common.TargetSelector.DamageType.Magical);
                var best = GetBestCard(target, "combo");
                if (best.Any())
                {
                    Cards.Select(best);
                }
            }
        }

        private void QLogic(HitChance hitChance)
        {
            var target = TargetSelector.GetTarget(Q.Range, LeagueSharp.Common.TargetSelector.DamageType.Magical);
            var cd = W.Instance.CooldownExpires - Game.Time;
            var dist = target.Distance(Player);
            var best = BestQPosition(target, GameObjects.EnemyHeroes.Cast<Obj_AI_Base>().ToList(), hitChance);
            if (!best.Item2.Equals(Vector3.Zero) &&
                (best.Item1 > 2 || dist <= 600 && cd <= 2 && Utils.IsStunned(target) || dist > 600 || cd > 2 ||
                 Q.IsKillable(target)))
            {
                Q.Cast(best.Item2);
            }
        }

        protected override void Harass()
        {
            if (!ManaManager.Check("harass"))
            {
                return;
            }
            var q = Menu.Item(Menu.Name + ".harass.q").GetValue<bool>();
            var w = Menu.Item(Menu.Name + ".harass.w").GetValue<bool>();

            if (q && Q.IsReady())
            {
                QLogic(Q.GetHitChance("harass"));
            }
            if (w && W.IsReady() && Cards.Status == SelectStatus.None)
            {
                var target = TargetSelector.GetTarget(
                    W.Range * 1.2f, LeagueSharp.Common.TargetSelector.DamageType.Magical);
                var best = GetBestCard(target, "harass");
                if (best.Any())
                {
                    Cards.Select(best);
                }
            }
        }

        protected override void LaneClear()
        {
            if (!ManaManager.Check("lane-clear"))
            {
                return;
            }

            var q = Menu.Item(Menu.Name + ".lane-clear.q").GetValue<bool>();
            var qMin = Menu.Item(Menu.Name + ".lane-clear.q-min").GetValue<Slider>().Value;
            var w = Menu.Item(Menu.Name + ".lane-clear.w").GetValue<bool>();

            if (q && Q.IsReady())
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

        protected override void Flee() {}
        protected override void Killsteal() {}

        private void OnDrawingDraw(EventArgs args)
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

        private void OnDrawingEndScene(EventArgs args)
        {
            if (_rMinimap.GetValue<bool>() && R.IsReady() && !Player.IsDead)
            {
                Utility.DrawCircle(Player.Position, R.Range, Color.White, 1, 30, true);
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
            foreach (var minion in minions)
            {
                var redHits = GetWHits(minion, minions, CardColor.Red);
                if (redHits > totalHits)
                {
                    totalHits = redHits;
                    target = minion;
                }
            }
            return new Tuple<int, Obj_AI_Base>(totalHits, target);
        }

        private Tuple<Obj_AI_Base, List<CardColor>> GetBestLaneClearTargetCard()
        {
            var cards = new List<CardColor>();
            Obj_AI_Base target = null;
            if (!ManaManager.Check("lane-clear-blue"))
            {
                var minions = MinionManager.GetMinions(
                    W.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
                if (minions.Any())
                {
                    var killable = minions.FirstOrDefault(m => W.IsKillable(m));
                    target = killable ?? minions.First();
                    cards.Add(CardColor.Blue);
                }
            }
            else
            {
                var minions = MinionManager.GetMinions(
                    W.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
                var minHits = minions.Any(m => m.Team == GameObjectTeam.Neutral) ? 3 : 2;
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
            return new Tuple<Obj_AI_Base, List<CardColor>>(target, cards);
        }

        private List<CardColor> GetBestCard(Obj_AI_Hero target, string mode)
        {
            var cards = new List<CardColor>();
            if (GetWHits(target) <= 0)
            {
                return cards;
            }

            if (W.IsKillable(target, 1))
            {
                cards.Add(CardColor.Red);
            }
            if (W.IsKillable(target, 2))
            {
                cards.Add(CardColor.Gold);
            }
            if (W.IsKillable(target))
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
                red += GetWHits(target, GameObjects.EnemyHeroes.Cast<Obj_AI_Base>().ToList(), CardColor.Red);
                if (red > blue && red > gold)
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
                    if (gold == red)
                    {
                        cards.Add(CardColor.Red);
                    }
                }
                else if (blue > red && blue > gold)
                {
                    cards.Add(CardColor.Blue);
                    if (blue == red)
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
                var damage = ItemManager.CalculateComboDamage(target) - target.HPRegenRate / 2f;
                if (HasEBuff())
                {
                    damage += E.GetDamage(target);
                }
                if (Q.IsReady() && Utils.IsStunned(target) && distance < Q.Range / 3f || distance < Q.Range / 5f)
                {
                    damage += Q.GetDamage(target) * 0.9f;
                }
                if (W.GetDamage(target, 2) + damage - 20 > target.Health)
                {
                    cards.Add(CardColor.Gold);
                }
                else if (W.GetDamage(target) + damage - 20 > target.Health)
                {
                    cards.Add(CardColor.Blue);
                }
                else if (ObjectManager.Player.HealthPercent <=
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
            None
        }

        internal static class Cards
        {
            private static readonly SpellDataInst Spell;
            private static readonly List<CardColor> ShouldSelect = new List<CardColor>();

            static Cards()
            {
                Spell = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W);
                Obj_AI_Base.OnProcessSpellCast += OnObjAiBaseProcessSpellCast;
                GameObject.OnCreate += OnGameObjectCreate;
                Game.OnUpdate += OnGameUpdate;
                Status = SelectStatus.None;
                LastCard = CardColor.None;
            }

            public static CardColor LastCard { get; set; }
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

            private static void OnGameUpdate(EventArgs args)
            {
                if (ObjectManager.Player.HasBuff("pickacard_tracker"))
                {
                    Status = SelectStatus.Selecting;
                }
                else if (ObjectManager.Player.HasBuff("goldcardpreattack") ||
                         ObjectManager.Player.HasBuff("redcardpreattack") ||
                         ObjectManager.Player.HasBuff("bluecardpreattack"))
                {
                    Status = SelectStatus.Selected;
                }
                else
                {
                    Status = SelectStatus.None;
                }
            }

            private static void OnObjAiBaseProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
            {
                if (sender.IsMe)
                {
                    if (args.SData.Name.Equals("goldcardlock", StringComparison.OrdinalIgnoreCase))
                    {
                        LastCard = CardColor.Gold;
                        Status = SelectStatus.Selected;
                        ShouldSelect.Clear();
                    }

                    if (args.SData.Name.Equals("redcardlock", StringComparison.OrdinalIgnoreCase))
                    {
                        LastCard = CardColor.Red;
                        Status = SelectStatus.Selected;
                        ShouldSelect.Clear();
                    }

                    if (args.SData.Name.Equals("bluecardlock", StringComparison.OrdinalIgnoreCase))
                    {
                        LastCard = CardColor.Blue;
                        Status = SelectStatus.Selected;
                        ShouldSelect.Clear();
                    }
                }
            }

            private static void OnGameObjectCreate(GameObject sender, EventArgs args)
            {
                try
                {
                    if (!sender.IsValid || sender.Type != GameObjectType.obj_GeneralParticleEmitter ||
                        Status != SelectStatus.Selecting)
                    {
                        return;
                    }
                    if (
                        ShouldSelect.Any(
                            card =>
                                card == CardColor.Blue &&
                                sender.Name.Equals(
                                    "twistedfate_base_w_bluecard.troy", StringComparison.OrdinalIgnoreCase) ||
                                card == CardColor.Red &&
                                sender.Name.Equals(
                                    "twistedfate_base_w_redcard.troy", StringComparison.OrdinalIgnoreCase) ||
                                card == CardColor.Gold &&
                                sender.Name.Equals(
                                    "twistedfate_base_w_goldcard.troy", StringComparison.OrdinalIgnoreCase)))
                    {
                        ObjectManager.Player.Spellbook.CastSpell(Spell.Slot);
                    }
                }
                catch (Exception ex)
                {
                    Global.Logger.AddItem(new LogItem(ex));
                }
            }

            public static void Select(CardColor card)
            {
                if (Spell.IsReady())
                {
                    ShouldSelect.Clear();
                    ShouldSelect.Add(card);
                    if (ObjectManager.Player.HasBuff("pickacard_tracker") ||
                        ObjectManager.Player.Spellbook.CastSpell(Spell.Slot, ObjectManager.Player.Position))
                    {
                        Status = SelectStatus.Selecting;
                    }
                }
            }

            public static void Select(List<CardColor> cards)
            {
                if (Spell.IsReady())
                {
                    ShouldSelect.Clear();
                    ShouldSelect.AddRange(cards);
                    if (ObjectManager.Player.HasBuff("pickacard_tracker") ||
                        ObjectManager.Player.Spellbook.CastSpell(Spell.Slot, ObjectManager.Player.Position))
                    {
                        Status = SelectStatus.Selecting;
                    }
                }
            }
        }
    }
}