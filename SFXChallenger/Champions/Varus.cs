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
using SFXLibrary;
using SFXLibrary.Logger;
using Orbwalking = SFXChallenger.Wrappers.Orbwalking;
using TargetSelector = SFXChallenger.Wrappers.TargetSelector;

#endregion

namespace SFXChallenger.Champions
{
    internal class Varus : Champion
    {
        protected override ItemFlags ItemFlags
        {
            get { return ItemFlags.Offensive | ItemFlags.Defensive | ItemFlags.Flee; }
        }

        protected override void OnLoad()
        {
            Orbwalking.AfterAttack += OnOrbwalkingAfterAttack;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
        }

        protected override void OnUnload()
        {
            Orbwalking.AfterAttack -= OnOrbwalkingAfterAttack;
            AntiGapcloser.OnEnemyGapcloser -= OnEnemyGapcloser;
        }

        protected override void AddToMenu()
        {
            var comboMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Combo"), Menu.Name + ".combo"));
            HitchanceManager.AddToMenu(
                comboMenu.AddSubMenu(new Menu(Global.Lang.Get("F_MH"), comboMenu.Name + ".hitchance")), "combo",
                new Dictionary<string, int> { { "E", 1 }, { "R", 2 } });
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".r", Global.Lang.Get("G_UseR")).SetValue(true));

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
                new MenuItem(laneclearMenu.Name + ".min", Global.Lang.Get("G_Min")).SetValue(
                    new Slider(3, 1, 5)));

            var killstealMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Killsteal"), Menu.Name + ".killsteal"));
            killstealMenu.AddItem(new MenuItem(killstealMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));

            var fleeMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Flee"), Menu.Name + ".flee"));
            fleeMenu.AddItem(new MenuItem(fleeMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(true));

            var miscMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Miscellaneous"), Menu.Name + ".miscellaneous"));
            miscMenu.AddItem(
                new MenuItem(miscMenu.Name + ".e-gapcloser", "E " + Global.Lang.Get("G_Gapcloser")).SetValue(false));
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

                if (Menu.Item(Menu.Name + ".miscellaneous.w-gapcloser").GetValue<bool>() && endPos.Distance(Player.Position) < W.Range)
                {
                    W.Cast(endPos);
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
            if (Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady())
            {
                if (!Q.IsCharging)
                {
                    Q.StartCharging();
                }
                if (Q.IsCharging)
                {
                    var target = TargetSelector.GetTarget(Q.ChargedMaxRange, LeagueSharp.Common.TargetSelector.DamageType.Physical);
                    var pred = Q.GetPrediction(target);
                    var distance = Player.ServerPosition.Distance(pred.UnitPosition + 200 * (pred.UnitPosition - Player.ServerPosition).Normalized(), true);
                    if (distance < Q.RangeSqr)
                    {
                        Q.Cast(pred.CastPosition);
                    }
                }
            }
            if (!Q.IsCharging && Menu.Item(Menu.Name + ".combo.e").GetValue<bool>() && E.IsReady())
            {
                var target = TargetSelector.GetTarget(E.Range, LeagueSharp.Common.TargetSelector.DamageType.Physical);
                var pred = E.GetPrediction(target);
                if (pred.Hitchance >= E.GetHitChance("combo"))
                {
                    E.Cast(pred.CastPosition);
                }
            }
        }

        protected override void Harass()
        {
            if (!ManaManager.Check("harass"))
            {
                return;
            }
            if (Menu.Item(Menu.Name + ".harass.q").GetValue<bool>() && Q.IsReady())
            {
                if (!Q.IsCharging)
                {
                    Q.StartCharging();
                }
                if (Q.IsCharging)
                {
                    var target = TargetSelector.GetTarget(Q.ChargedMaxRange, LeagueSharp.Common.TargetSelector.DamageType.Physical);
                    var pred = Q.GetPrediction(target);
                    var distance = Player.ServerPosition.Distance(pred.UnitPosition + 200 * (pred.UnitPosition - Player.ServerPosition).Normalized(), true);
                    if (distance < Q.RangeSqr)
                    {
                        Q.Cast(pred.CastPosition);
                    }
                }
            }
            if (!Q.IsCharging && Menu.Item(Menu.Name + ".harass.e").GetValue<bool>() && E.IsReady())
            {
                var target = TargetSelector.GetTarget(E.Range, LeagueSharp.Common.TargetSelector.DamageType.Physical);
                var pred = E.GetPrediction(target);
                if (pred.Hitchance >= E.GetHitChance("harass"))
                {
                    E.Cast(pred.CastPosition);
                }
            }
        }

        protected override void LaneClear()
        {
            if (!ManaManager.Check("lane-clear"))
            {
                return;
            }

            var useQ = Menu.Item(Menu.Name + ".lane-clear.q").GetValue<bool>() && Q.IsReady();
            var minQ = Menu.Item(Menu.Name + ".lane-clear.q-min").GetValue<Slider>().Value;

            if (useQ)
            {
                Casting.Farm(Q, minQ, 200f);
            }
        }

        protected override void Flee()
        {
            if (Menu.Item(Menu.Name + ".flee.e").GetValue<bool>() && E.IsReady())
            {
                E.Cast(Player.Position.Extend(Game.CursorPos, E.Range));
            }
        }

        protected override void Killsteal()
        {
            if (Menu.Item(Menu.Name + ".killsteal.q").GetValue<bool>() && Q.IsReady())
            {
                var fPredEnemy =
                    GameObjects.EnemyHeroes.Where(e => e.IsValidTarget(Q.Range * 1.2f) && Q.IsKillable(e))
                        .Select(enemy => Q.GetPrediction(enemy, true))
                        .FirstOrDefault(pred => pred.Hitchance >= Q.GetHitChance("harass"));
                if (fPredEnemy != null)
                {
                    Q.Cast(fPredEnemy.CastPosition);
                }
            }
        }
    }
}