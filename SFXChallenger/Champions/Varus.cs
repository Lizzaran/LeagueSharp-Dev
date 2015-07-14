#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Varus.cs is part of SFXChallenger.

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
using SFXChallenger.Wrappers;
using SFXLibrary;
using SFXLibrary.Extensions.NET;
using SFXLibrary.Logger;
using SharpDX;
using Color = System.Drawing.Color;
using Orbwalking = SFXChallenger.Wrappers.Orbwalking;
using TargetSelector = SFXChallenger.Wrappers.TargetSelector;

#endregion

namespace SFXChallenger.Champions
{
    internal class Varus : Champion
    {
        private float _rSpreadRadius = 450f;
        private MenuItem _wStacks;

        protected override ItemFlags ItemFlags
        {
            get { return ItemFlags.Offensive | ItemFlags.Defensive | ItemFlags.Flee; }
        }

        protected override void OnLoad()
        {
            Core.OnPostUpdate += OnCorePostUpdate;
            Orbwalking.AfterAttack += OnOrbwalkingAfterAttack;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget += OnInterruptableTarget;
            Drawing.OnDraw += OnDrawingDraw;
        }

        protected override void OnUnload()
        {
            Core.OnPostUpdate -= OnCorePostUpdate;
            Orbwalking.AfterAttack -= OnOrbwalkingAfterAttack;
            AntiGapcloser.OnEnemyGapcloser -= OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget -= OnInterruptableTarget;
            Drawing.OnDraw -= OnDrawingDraw;
        }

        protected override void AddToMenu()
        {
            var comboMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Combo"), Menu.Name + ".combo"));
            HitchanceManager.AddToMenu(
                comboMenu.AddSubMenu(new Menu(Global.Lang.Get("F_MH"), comboMenu.Name + ".hitchance")), "combo",
                new Dictionary<string, int> { { "Q", 1 }, { "E", 1 }, { "R", 2 } });
            comboMenu.AddItem(
                new MenuItem(comboMenu.Name + ".q-range", "Q " + Global.Lang.Get("G_OutOfRange")).SetValue(true));
            comboMenu.AddItem(
                new MenuItem(comboMenu.Name + ".q-always", "Q " + Global.Lang.Get("G_Always")).SetValue(false));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".q-stacks", "Q " + Global.Lang.Get("G_StacksIsOrMore")))
                .SetValue(new Slider(3, 1, 3));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));
            comboMenu.AddItem(
                new MenuItem(comboMenu.Name + ".e-always", "E " + Global.Lang.Get("G_Always")).SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".e-stacks", "E " + Global.Lang.Get("G_StacksIsOrMore")))
                .SetValue(new Slider(3, 1, 3));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".r", Global.Lang.Get("G_UseR")).SetValue(true));

            var harassMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Harass"), Menu.Name + ".harass"));
            HitchanceManager.AddToMenu(
                harassMenu.AddSubMenu(new Menu(Global.Lang.Get("F_MH"), harassMenu.Name + ".hitchance")), "harass",
                new Dictionary<string, int> { { "Q", 1 }, { "E", 2 } });
            ManaManager.AddToMenu(harassMenu, "harass", ManaCheckType.Minimum, ManaValueType.Percent);
            harassMenu.AddItem(
                new MenuItem(harassMenu.Name + ".q-range", "Q " + Global.Lang.Get("G_OutOfRange")).SetValue(true));
            harassMenu.AddItem(
                new MenuItem(harassMenu.Name + ".q-always", "Q " + Global.Lang.Get("G_Always")).SetValue(false));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".q-stacks", "Q " + Global.Lang.Get("G_StacksIsOrMore")))
                .SetValue(new Slider(3, 1, 3));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));
            harassMenu.AddItem(
                new MenuItem(harassMenu.Name + ".e-always", "E " + Global.Lang.Get("G_Always")).SetValue(false));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".e-stacks", "E " + Global.Lang.Get("G_StacksIsOrMore")))
                .SetValue(new Slider(3, 1, 3));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(true));

            var laneclearMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_LaneClear"), Menu.Name + ".lane-clear"));
            ManaManager.AddToMenu(laneclearMenu, "lane-clear", ManaCheckType.Minimum, ManaValueType.Percent);
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(true));
            laneclearMenu.AddItem(
                new MenuItem(laneclearMenu.Name + ".min", Global.Lang.Get("G_Min")).SetValue(new Slider(3, 1, 5)));

            var ultimateMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Ultimate"), Menu.Name + ".ultimate"));

            var uComboMenu = ultimateMenu.AddSubMenu(new Menu(Global.Lang.Get("G_Combo"), ultimateMenu.Name + ".combo"));
            uComboMenu.AddItem(
                new MenuItem(uComboMenu.Name + ".min", "R " + Global.Lang.Get("G_Min")).SetValue(new Slider(2, 1, 5)));
            uComboMenu.AddItem(new MenuItem(uComboMenu.Name + ".1v1", "R 1v1").SetValue(true));
            uComboMenu.AddItem(new MenuItem(uComboMenu.Name + ".enabled", Global.Lang.Get("G_Enabled")).SetValue(true));

            var uAutoMenu = ultimateMenu.AddSubMenu(new Menu(Global.Lang.Get("G_Auto"), ultimateMenu.Name + ".auto"));

            var autoGapMenu =
                uAutoMenu.AddSubMenu(new Menu(Global.Lang.Get("G_Gapcloser"), uAutoMenu.Name + ".gapcloser"));
            foreach (var enemy in
                GameObjects.EnemyHeroes.Where(
                    e =>
                        AntiGapcloser.Spells.Any(
                            s => s.ChampionName.Equals(e.ChampionName, StringComparison.OrdinalIgnoreCase))))
            {
                autoGapMenu.AddItem(
                    new MenuItem(autoGapMenu.Name + "." + enemy.ChampionName, enemy.ChampionName).SetValue(false));
            }

            var autoInterruptMenu =
                uAutoMenu.AddSubMenu(new Menu(Global.Lang.Get("G_InterruptSpell"), uAutoMenu.Name + ".interrupt"));
            foreach (var enemy in GameObjects.EnemyHeroes)
            {
                autoInterruptMenu.AddItem(
                    new MenuItem(autoInterruptMenu.Name + "." + enemy.ChampionName, enemy.ChampionName).SetValue(false));
            }

            uAutoMenu.AddItem(
                new MenuItem(uAutoMenu.Name + ".min", "R " + Global.Lang.Get("G_Min")).SetValue(new Slider(3, 1, 5)));
            uAutoMenu.AddItem(new MenuItem(uAutoMenu.Name + ".1v1", "R 1v1").SetValue(false));
            uAutoMenu.AddItem(new MenuItem(uAutoMenu.Name + ".enabled", Global.Lang.Get("G_Enabled")).SetValue(true));

            var uAssistedMenu =
                ultimateMenu.AddSubMenu(new Menu(Global.Lang.Get("G_Assisted"), ultimateMenu.Name + ".assisted"));
            uAssistedMenu.AddItem(
                new MenuItem(uAssistedMenu.Name + ".min", "R " + Global.Lang.Get("G_Min")).SetValue(new Slider(2, 1, 5)));
            uAssistedMenu.AddItem(new MenuItem(uAssistedMenu.Name + ".1v1", "R 1v1").SetValue(true));
            uAssistedMenu.AddItem(
                new MenuItem(uAssistedMenu.Name + ".hotkey", Global.Lang.Get("G_Hotkey")).SetValue(
                    new KeyBind('R', KeyBindType.Press)));
            uAssistedMenu.AddItem(
                new MenuItem(uAssistedMenu.Name + ".move-cursor", Global.Lang.Get("G_MoveCursor")).SetValue(true));
            uAssistedMenu.AddItem(
                new MenuItem(uAssistedMenu.Name + ".enabled", Global.Lang.Get("G_Enabled")).SetValue(true));

            ultimateMenu.AddItem(
                new MenuItem(ultimateMenu.Name + ".radius", Global.Lang.Get("G_Range")).SetValue(
                    new Slider(450, 100, 600))).ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs args)
                {
                    _rSpreadRadius = args.GetNewValue<Slider>().Value;
                };
            _rSpreadRadius = Menu.Item(Menu.Name + ".ultimate.radius").GetValue<Slider>().Value;

            var killstealMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Killsteal"), Menu.Name + ".killsteal"));
            killstealMenu.AddItem(new MenuItem(killstealMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));
            killstealMenu.AddItem(
                new MenuItem(killstealMenu.Name + ".range", Global.Lang.Get("G_OutOfRange")).SetValue(true));

            var fleeMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Flee"), Menu.Name + ".flee"));
            fleeMenu.AddItem(new MenuItem(fleeMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(true));

            var miscMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Miscellaneous"), Menu.Name + ".miscellaneous"));
            miscMenu.AddItem(
                new MenuItem(miscMenu.Name + ".e-gapcloser", "E " + Global.Lang.Get("G_Gapcloser")).SetValue(false));

            TargetSelector.AddWeightedItem(
                new WeightedItem("w-stacks", "W " + Global.Lang.Get("G_Stacks"), 13, true, 500, t => GetWStacks(t)));

            IndicatorManager.AddToMenu(DrawingManager.GetMenu(), true);
            IndicatorManager.Add(Q);
            IndicatorManager.Add(E);
            IndicatorManager.Add(R);
            IndicatorManager.Finale();

            _wStacks = DrawingManager.Add("W " + Global.Lang.Get("Stacks"), true);
        }

        protected override void SetupSpells()
        {
            Q = new Spell(SpellSlot.Q, 925f);
            Q.SetSkillshot(0.25f, 70f, 1650f, false, SkillshotType.SkillshotLine);
            Q.SetCharged("VarusQ", "VarusQ", 250, 1600, 1.2f);

            W = new Spell(SpellSlot.W, 0f);

            E = new Spell(SpellSlot.E, 925f);
            E.SetSkillshot(0.50f, 250f, 1400f, false, SkillshotType.SkillshotCircle);

            R = new Spell(SpellSlot.R, 1075f);
            R.SetSkillshot(0.25f, 120f, 1950f, false, SkillshotType.SkillshotLine);
        }

        private void OnCorePostUpdate(EventArgs args)
        {
            try
            {
                if (Menu.Item(Menu.Name + ".ultimate.assisted.enabled").GetValue<bool>() &&
                    Menu.Item(Menu.Name + ".ultimate.assisted.hotkey").GetValue<KeyBind>().Active && R.IsReady())
                {
                    if (Menu.Item(Menu.Name + ".ultimate.assisted.move-cursor").GetValue<bool>())
                    {
                        Orbwalking.MoveTo(Game.CursorPos, Orbwalker.HoldAreaRadius);
                    }

                    if (
                        !RLogic(
                            TargetSelector.GetTarget(R.Range, LeagueSharp.Common.TargetSelector.DamageType.Physical),
                            R.GetHitChance("combo"),
                            Menu.Item(Menu.Name + ".ultimate.assisted.min").GetValue<Slider>().Value))
                    {
                        RLogic1V1(
                            Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady(),
                            Menu.Item(Menu.Name + ".combo.e").GetValue<bool>() && E.IsReady());
                    }
                }

                if (Menu.Item(Menu.Name + ".ultimate.auto.enabled").GetValue<bool>() && R.IsReady())
                {
                    if (
                        !RLogic(
                            TargetSelector.GetTarget(R.Range, LeagueSharp.Common.TargetSelector.DamageType.Physical),
                            R.GetHitChance("combo"),
                            Menu.Item(Menu.Name + ".ultimate.auto.min").GetValue<Slider>().Value))
                    {
                        RLogic1V1(
                            Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady(),
                            Menu.Item(Menu.Name + ".combo.e").GetValue<bool>() && E.IsReady());
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

                var endPos = args.End;
                if (args.Sender.ChampionName.Equals("Fizz", StringComparison.OrdinalIgnoreCase))
                {
                    endPos = args.Start.Extend(endPos, 550);
                }

                if (Menu.Item(Menu.Name + ".miscellaneous.e-gapcloser").GetValue<bool>() &&
                    endPos.Distance(Player.Position) < E.Range)
                {
                    E.Cast(endPos);
                }
                if (Menu.Item(Menu.Name + ".ultimate.auto.enabled").GetValue<bool>() &&
                    Menu.Item(Menu.Name + ".ultimate.auto.gapcloser." + args.Sender.ChampionName).GetValue<bool>())
                {
                    RLogic(args.Sender, HitChance.High, 1);
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
                if (sender.IsEnemy && args.DangerLevel >= Interrupter2.DangerLevel.High &&
                    Menu.Item(Menu.Name + ".ultimate.auto.enabled").GetValue<bool>() &&
                    Menu.Item(Menu.Name + ".ultimate.auto.interrupt." + sender.ChampionName).GetValue<bool>())
                {
                    RLogic(sender, HitChance.High, 1);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnOrbwalkingAfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (unit.IsMe)
            {
                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                {
                    var enemy = target as Obj_AI_Hero;
                    if (enemy != null)
                    {
                        ItemManager.Muramana(true);
                        ItemManager.UseComboItems(enemy);
                        SummonerManager.UseComboSummoners(enemy);
                    }
                }
                else
                {
                    ItemManager.Muramana(false);
                }
            }
        }

        protected override void Combo()
        {
            var q = Menu.Item(Menu.Name + ".combo.q").GetValue<bool>();
            var e = Menu.Item(Menu.Name + ".combo.e").GetValue<bool>();
            var r = Menu.Item(Menu.Name + ".ultimate.combo.enabled").GetValue<bool>();

            if (q && Q.IsReady())
            {
                var target = TargetSelector.GetTarget(
                    Q.ChargedMaxRange, LeagueSharp.Common.TargetSelector.DamageType.Physical);
                if (Q.IsCharging || W.Level == 0 || Menu.Item(Menu.Name + ".combo.q-always").GetValue<bool>() ||
                    Menu.Item(Menu.Name + ".combo.q-range").GetValue<bool>() &&
                    Player.CountEnemiesInRange(Player.AttackRange * 1.075f) == 0 ||
                    GetWStacks(target) >= Menu.Item(Menu.Name + ".combo.q-stacks").GetValue<Slider>().Value)
                {
                    QLogic(target, Q.GetHitChance("combo"));
                }
            }
            if (e && E.IsReady())
            {
                var target = TargetSelector.GetTarget(E.Range, LeagueSharp.Common.TargetSelector.DamageType.Physical);
                if (Menu.Item(Menu.Name + ".combo.e-always").GetValue<bool>() ||
                    GetWStacks(target) >= Menu.Item(Menu.Name + ".combo.e-stacks").GetValue<Slider>().Value)
                {
                    ELogic(target, E.GetHitChance("combo"));
                }
            }
            if (r && R.IsReady())
            {
                var target = TargetSelector.GetTarget(R.Range, LeagueSharp.Common.TargetSelector.DamageType.Physical);
                if (
                    !RLogic(
                        target, R.GetHitChance("combo"),
                        Menu.Item(Menu.Name + ".ultimate.combo.min").GetValue<Slider>().Value))
                {
                    if (Menu.Item(Menu.Name + ".ultimate.combo.1v1").GetValue<bool>())
                    {
                        RLogic1V1(q, e);
                    }
                }
            }
        }

        protected override void Harass()
        {
            if (!ManaManager.Check("harass") && !Q.IsCharging)
            {
                return;
            }
            if (Menu.Item(Menu.Name + ".harass.q").GetValue<bool>() && Q.IsReady())
            {
                var target = TargetSelector.GetTarget(
                    Q.ChargedMaxRange, LeagueSharp.Common.TargetSelector.DamageType.Physical);
                if (Q.IsCharging || W.Level == 0 || Menu.Item(Menu.Name + ".harass.q-always").GetValue<bool>() ||
                    Menu.Item(Menu.Name + ".harass.q-range").GetValue<bool>() &&
                    Player.CountEnemiesInRange(Player.AttackRange * 1.075f) == 0 ||
                    GetWStacks(target) >= Menu.Item(Menu.Name + ".harass.q-stacks").GetValue<Slider>().Value)
                {
                    QLogic(target, Q.GetHitChance("harass"));
                }
            }
            if (Menu.Item(Menu.Name + ".harass.e").GetValue<bool>() && E.IsReady())
            {
                var target = TargetSelector.GetTarget(E.Range, LeagueSharp.Common.TargetSelector.DamageType.Physical);
                if (Menu.Item(Menu.Name + ".harass.e-always").GetValue<bool>() ||
                    GetWStacks(target) >= Menu.Item(Menu.Name + ".harass.e-stacks").GetValue<Slider>().Value)
                {
                    ELogic(target, E.GetHitChance("harass"));
                }
            }
        }

        private void QLogic(Obj_AI_Hero target, HitChance hitChance)
        {
            if (target == null)
            {
                return;
            }
            if (!Q.IsCharging)
            {
                var pred = Q.GetPrediction(target);
                if (pred.Hitchance >= hitChance)
                {
                    Q.Cast(target);
                }
                else
                {
                    var input = new PredictionInput
                    {
                        Range = Q.ChargedMaxRange,
                        Collision = false,
                        Delay = 1f,
                        Radius = Q.Width,
                        Speed = Q.Speed,
                        Type = Q.Type,
                        Unit = target
                    };
                    if (Prediction.GetPrediction(input).Hitchance >= (hitChance - 1))
                    {
                        Q.StartCharging();
                    }
                }
            }
            if (Q.IsCharging)
            {
                var pred = Q.GetPrediction(target);
                if (pred.Hitchance >= hitChance)
                {
                    Q.Cast(pred.CastPosition);
                }
            }
        }

        private void ELogic(Obj_AI_Hero target, HitChance hitChance)
        {
            if (Q.IsCharging)
            {
                return;
            }
            var pos = GetBestELocation(target, hitChance);
            if (!pos.Equals(Vector3.Zero))
            {
                E.Cast(pos);
            }
        }

        private bool RLogic(Obj_AI_Hero target, HitChance hitChance, int min)
        {
            if (Q.IsCharging)
            {
                return false;
            }
            var pred = R.GetPrediction(target);
            if (pred.Hitchance >= hitChance && target.CountEnemiesInRange(_rSpreadRadius) >= min)
            {
                R.Cast(pred.CastPosition);
                return true;
            }
            return false;
        }

        private void RLogic1V1(bool q, bool e)
        {
            try
            {
                foreach (var t in GameObjects.EnemyHeroes)
                {
                    if (t.HealthPercent > 25)
                    {
                        var cDmg = CalcComboDamage(t, q, e, true);
                        if (cDmg - 10 >= t.Health)
                        {
                            if (
                                GameObjects.EnemyHeroes.Count(
                                    em => !em.IsDead && em.IsVisible && em.Distance(Player) < 3000) == 1)
                            {
                                if (RLogic(t, R.GetHitChance("combo"), 1))
                                {
                                    break;
                                }
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

        private float CalcComboDamage(Obj_AI_Hero target, bool q, bool e, bool r)
        {
            try
            {
                float damage = 0;
                if (q)
                {
                    damage += Q.GetDamage(target);
                }
                if (e && E.IsReady())
                {
                    damage += E.GetDamage(target);
                }
                if (r && R.IsReady())
                {
                    damage += R.GetDamage(target);
                }
                damage += 5f * (float) Player.GetAutoAttackDamage(target);
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

        protected override void LaneClear()
        {
            if (!ManaManager.Check("lane-clear") && !Q.IsCharging)
            {
                return;
            }
            var min = Menu.Item(Menu.Name + ".lane-clear.min").GetValue<Slider>().Value;
            if (Menu.Item(Menu.Name + ".lane-clear.q").GetValue<bool>() && Q.IsReady())
            {
                Casting.Farm(Q, min);
            }
            if (Menu.Item(Menu.Name + ".lane-clear.e").GetValue<bool>() && E.IsReady())
            {
                Casting.Farm(E, min);
            }
        }

        protected override void Flee()
        {
            if (Menu.Item(Menu.Name + ".flee.e").GetValue<bool>() && !Q.IsCharging && E.IsReady())
            {
                ELogic(
                    GameObjects.EnemyHeroes.Where(e => e.IsValidTarget(E.Range))
                        .OrderBy(e => e.Position.Distance(Player.Position))
                        .FirstOrDefault(), HitChance.High);
            }
        }

        protected override void Killsteal()
        {
            if (Menu.Item(Menu.Name + ".killsteal.q").GetValue<bool>() && Q.IsReady())
            {
                var range = Menu.Item(Menu.Name + ".killsteal.range").GetValue<bool>();
                var killable =
                    GameObjects.EnemyHeroes.FirstOrDefault(
                        e => Q.IsInRange(e) && (!range || !Orbwalking.InAutoAttackRange(e)) && Q.IsKillable(e));
                if (killable != null)
                {
                    QLogic(killable, HitChance.High);
                }
            }
        }

        private Vector3 GetBestELocation(Obj_AI_Hero target, HitChance hitChance)
        {
            var pred = E.GetPrediction(target);
            if (pred.Hitchance < hitChance)
            {
                return Vector3.Zero;
            }
            var points = (from enemy in GameObjects.EnemyHeroes.Where(h => h.IsValidTarget((E.Range + E.Range * 1.3f)))
                select E.GetPrediction(enemy)
                into ePred
                where ePred.Hitchance >= (hitChance - 1)
                select ePred.UnitPosition.To2D()).ToList();
            if (points.Any())
            {
                var possibilities = ListExtensions.ProduceEnumeration(points).Where(p => p.Count > 0).ToList();
                if (possibilities.Any())
                {
                    var hits = 0;
                    var radius = float.MaxValue;
                    var pos = Vector3.Zero;
                    foreach (var possibility in possibilities)
                    {
                        var mec = MEC.GetMec(possibility);
                        if (mec.Radius < E.Width * 0.95f)
                        {
                            if (possibility.Count > hits || possibility.Count == hits && radius > mec.Radius)
                            {
                                hits = possibility.Count;
                                radius = mec.Radius;
                                pos = mec.Center.To3D();
                            }
                        }
                    }
                    return pos;
                }
            }
            return Vector3.Zero;
        }

        private int GetWStacks(Obj_AI_Base target)
        {
            return target.GetBuffCount("varuswdebuff");
        }

        private void OnDrawingDraw(EventArgs args)
        {
            if (W.Level > 0 && _wStacks != null && _wStacks.GetValue<bool>())
            {
                foreach (var enemy in
                    GameObjects.EnemyHeroes.Where(
                        e => e.IsHPBarRendered && e.Position.IsOnScreen() && e.IsValidTarget()))
                {
                    var stacks = GetWStacks(enemy) - 1;
                    var x = enemy.HPBarPosition.X + 45;
                    var y = enemy.HPBarPosition.Y - 20;
                    for (var i = 0; 3 > i; i++)
                    {
                        Drawing.DrawLine(
                            x + (i * 20), y, x + (i * 20) + 10, y, 10, (i > stacks ? Color.DarkGray : Color.Orange));
                    }
                }
            }
        }
    }
}