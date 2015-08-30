#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 KogMaw.cs is part of SFXKogMaw.

 SFXKogMaw is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXKogMaw is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXKogMaw. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXKogMaw.Abstracts;
using SFXKogMaw.Enumerations;
using SFXKogMaw.Helpers;
using SFXKogMaw.Managers;
using SFXLibrary;
using SFXLibrary.Logger;
using DamageType = SFXKogMaw.Enumerations.DamageType;
using Spell = SFXKogMaw.Wrappers.Spell;
using TargetSelector = SFXKogMaw.Wrappers.TargetSelector;
using Utils = SFXKogMaw.Helpers.Utils;

#endregion

namespace SFXKogMaw.Champions
{
    internal class KogMaw : Champion
    {
        private int _rLevel;
        private int _wLevel;

        protected override ItemFlags ItemFlags
        {
            get { return ItemFlags.Offensive | ItemFlags.Defensive | ItemFlags.Flee; }
        }

        protected override ItemUsageType ItemUsage
        {
            get { return ItemUsageType.AfterAttack; }
        }

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
                new Dictionary<string, HitChance>
                {
                    { "Q", HitChance.VeryHigh },
                    { "E", HitChance.VeryHigh },
                    { "R", HitChance.VeryHigh }
                });
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".w", Global.Lang.Get("G_UseW")).SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".r", Global.Lang.Get("G_UseR")).SetValue(true));

            var harassMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Harass"), Menu.Name + ".harass"));
            HitchanceManager.AddToMenu(
                harassMenu.AddSubMenu(new Menu(Global.Lang.Get("F_MH"), harassMenu.Name + ".hitchance")), "harass",
                new Dictionary<string, HitChance> { { "Q", HitChance.VeryHigh }, { "R", HitChance.VeryHigh } });
            ManaManager.AddToMenu(harassMenu, "harass", ManaCheckType.Minimum, ManaValueType.Percent);
            ManaManager.AddToMenu(harassMenu, "harass-r", ManaCheckType.Minimum, ManaValueType.Percent, "R", 50);
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".w", Global.Lang.Get("G_UseW")).SetValue(true));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".r", Global.Lang.Get("G_UseR")).SetValue(true));

            var laneclearMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_LaneClear"), Menu.Name + ".lane-clear"));
            ManaManager.AddToMenu(laneclearMenu, "lane-clear", ManaCheckType.Minimum, ManaValueType.Percent);
            laneclearMenu.AddItem(
                new MenuItem(laneclearMenu.Name + ".w-min", "W " + Global.Lang.Get("G_Min")).SetValue(
                    new Slider(3, 1, 5)));
            laneclearMenu.AddItem(
                new MenuItem(laneclearMenu.Name + ".e-min", "E " + Global.Lang.Get("G_Min")).SetValue(
                    new Slider(3, 1, 5)));
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".w", Global.Lang.Get("G_UseW")).SetValue(true));
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(false));

            var killstealMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Killsteal"), Menu.Name + ".killsteal"));
            killstealMenu.AddItem(new MenuItem(killstealMenu.Name + ".R", Global.Lang.Get("G_UseR")).SetValue(true));

            var fleeMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Flee"), Menu.Name + ".flee"));
            fleeMenu.AddItem(new MenuItem(fleeMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(true));

            var miscMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Miscellaneous"), Menu.Name + ".miscellaneous"));
            HeroListManager.AddToMenu(
                miscMenu.AddSubMenu(new Menu("E " + Global.Lang.Get("G_Gapcloser"), miscMenu.Name + "e-gapcloser")),
                "e-gapcloser", false, false, true, false);
            HeroListManager.AddToMenu(
                miscMenu.AddSubMenu(new Menu("R " + Global.Lang.Get("G_Immobile"), miscMenu.Name + "r-immobile")),
                "r-immobile", false, false, true, false);
            miscMenu.AddItem(
                new MenuItem(
                    miscMenu.Name + ".r-max", "R " + Global.Lang.Get("G_Max") + " " + Global.Lang.Get("G_Stacks"))
                    .SetValue(new Slider(5, 1, 10)));

            IndicatorManager.AddToMenu(DrawingManager.Menu, true);
            IndicatorManager.Add(Q);
            IndicatorManager.Add(W);
            IndicatorManager.Add(E);
            IndicatorManager.Add(R);
            IndicatorManager.Finale();
        }

        protected override void SetupSpells()
        {
            Q = new Spell(SpellSlot.Q, 980f, DamageType.Magical);
            Q.SetSkillshot(0.25f, 50f, 2000f, true, SkillshotType.SkillshotLine);

            W = new Spell(
                SpellSlot.W,
                Player.AttackRange + Player.BoundingRadius +
                GameObjects.EnemyHeroes.Select(e => e.BoundingRadius).DefaultIfEmpty(50).Average(), DamageType.Magical);

            E = new Spell(SpellSlot.E, 1200f, DamageType.Magical);
            E.SetSkillshot(0.25f, 120f, 1400f, false, SkillshotType.SkillshotLine);

            R = new Spell(SpellSlot.R, 1200f, DamageType.Magical);
            R.SetSkillshot(1.5f, 200f, float.MaxValue, false, SkillshotType.SkillshotCircle);
        }

        private void OnCorePostUpdate(EventArgs args)
        {
            try
            {
                if (HeroListManager.Enabled("r-immobile") && R.IsReady())
                {
                    var target =
                        GameObjects.EnemyHeroes.FirstOrDefault(
                            t =>
                                t.IsValidTarget(R.Range) && HeroListManager.Check("r-immobile", t) &&
                                Utils.IsImmobile(t));
                    if (target != null)
                    {
                        Casting.SkillShot(target, R, HitChance.VeryHigh);
                    }
                }

                if (W.Level > _wLevel)
                {
                    _wLevel = W.Level;
                    W.Range = Player.AttackRange + Player.BoundingRadius +
                              GameObjects.EnemyHeroes.Select(e => e.BoundingRadius).DefaultIfEmpty(50).Average() +
                              20f * _wLevel;
                }
                if (R.Level > _rLevel)
                {
                    _rLevel = R.Level;
                    R.Range = 900f + 300f * _rLevel;
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
                if (HeroListManager.Check("e-gapcloser", args.Sender) && E.IsInRange(args.End))
                {
                    E.Cast(args.End);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private int GetRBuffCount()
        {
            try
            {
                return
                    Player.Buffs.Count(x => x.Name.Equals("kogmawlivingartillery", StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return 0;
        }

        protected override void Combo()
        {
            var useQ = Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady();
            var useW = Menu.Item(Menu.Name + ".combo.w").GetValue<bool>() && W.IsReady();
            var useE = Menu.Item(Menu.Name + ".combo.e").GetValue<bool>() && E.IsReady();
            var useR = Menu.Item(Menu.Name + ".combo.r").GetValue<bool>() && R.IsReady();

            if (useQ)
            {
                Casting.SkillShot(Q, Q.GetHitChance("combo"));
            }
            if (useW)
            {
                WLogic();
            }
            if (useE)
            {
                Casting.SkillShot(E, E.GetHitChance("combo"));
            }
            if (useR && Menu.Item(Menu.Name + ".miscellaneous.r-max").GetValue<Slider>().Value > GetRBuffCount())
            {
                var target = TargetSelector.GetTarget(R);
                if (target != null &&
                    (Player.FlatMagicDamageMod > 50 ||
                     !GameObjects.Enemy.Any(e => e.IsValidTarget() && Orbwalking.InAutoAttackRange(e))))
                {
                    Casting.SkillShot(R, R.GetHitChance("combo"));
                }
            }
        }

        private void WLogic()
        {
            try
            {
                var wRange = Player.AttackRange + Player.BoundingRadius + 20 * W.Level;
                if (GameObjects.EnemyHeroes.Any(e => e.Distance(Player) < wRange + e.BoundingRadius))
                {
                    W.Cast();
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected override void Harass()
        {
            if (ManaManager.Check("harass"))
            {
                var useQ = Menu.Item(Menu.Name + ".harass.q").GetValue<bool>() && Q.IsReady();
                var useW = Menu.Item(Menu.Name + ".harass.w").GetValue<bool>() && W.IsReady();
                if (useQ)
                {
                    Casting.SkillShot(Q, Q.GetHitChance("harass"));
                }
                if (useW)
                {
                    WLogic();
                }
            }
            if (ManaManager.Check("harass-r"))
            {
                var useR = Menu.Item(Menu.Name + ".harass.r").GetValue<bool>() && R.IsReady();
                if (useR && Menu.Item(Menu.Name + ".miscellaneous.r-max").GetValue<Slider>().Value > GetRBuffCount())
                {
                    var target = TargetSelector.GetTarget(R);
                    if (target != null &&
                        (Player.FlatMagicDamageMod > 50 ||
                         !GameObjects.Enemy.Any(e => e.IsValidTarget() && Orbwalking.InAutoAttackRange(e))))
                    {
                        Casting.SkillShot(R, R.GetHitChance("harass"));
                    }
                }
            }
        }

        protected override void LaneClear()
        {
            if (!ManaManager.Check("lane-clear"))
            {
                return;
            }

            var useW = Menu.Item(Menu.Name + ".lane-clear.w").GetValue<bool>() && W.IsReady();
            var minW = Menu.Item(Menu.Name + ".lane-clear.w-min").GetValue<Slider>().Value;
            var useE = Menu.Item(Menu.Name + ".lane-clear.e").GetValue<bool>() && E.IsReady();
            var minE = Menu.Item(Menu.Name + ".lane-clear.e-min").GetValue<Slider>().Value;

            if (useW)
            {
                Casting.FarmSelfAoe(W, minW, Player.AttackRange + Player.BoundingRadius * 1.25f + 20 * W.Level);
            }
            if (useE)
            {
                Casting.Farm(E, minE);
            }
        }

        protected override void Flee()
        {
            if (Menu.Item(Menu.Name + ".flee.e").GetValue<bool>() && E.IsReady())
            {
                var enemy =
                    GameObjects.EnemyHeroes.Where(e => e.IsValidTarget() && !Utils.IsSlowed(e) && !Utils.IsImmobile(e))
                        .OrderBy(e => e.Distance(Player))
                        .FirstOrDefault();
                if (enemy != null)
                {
                    Casting.SkillShot(enemy, E, HitChance.High);
                }
            }
        }

        protected override void Killsteal()
        {
            if (Menu.Item(Menu.Name + ".killsteal.r").GetValue<bool>() && R.IsReady())
            {
                var fPredEnemy =
                    GameObjects.EnemyHeroes.Where(e => e.IsValidTarget(R.Range) && R.IsKillable(e))
                        .Select(enemy => R.GetPrediction(enemy, true))
                        .FirstOrDefault(pred => pred.Hitchance >= HitChance.High);
                if (fPredEnemy != null)
                {
                    R.Cast(fPredEnemy.CastPosition);
                }
            }
        }
    }
}