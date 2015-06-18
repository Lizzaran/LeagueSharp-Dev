#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Graves.cs is part of SFXChallenger.

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
    internal class Graves : Champion
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
                new Dictionary<string, int> { { "Q", 2 }, { "W", 1 }, { "R", 2 } });
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".w", Global.Lang.Get("G_UseE")).SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".r", Global.Lang.Get("G_UseR")).SetValue(true));

            var harassMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Harass"), Menu.Name + ".harass"));
            HitchanceManager.AddToMenu(
                harassMenu.AddSubMenu(new Menu(Global.Lang.Get("F_MH"), harassMenu.Name + ".hitchance")), "harass",
                new Dictionary<string, int> { { "Q", 2 } });
            ManaManager.AddToMenu(harassMenu, "harass", ManaCheckType.Minimum, ManaValueType.Percent);
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));

            var laneclearMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_LaneClear"), Menu.Name + ".lane-clear"));
            ManaManager.AddToMenu(laneclearMenu, "lane-clear", ManaCheckType.Minimum, ManaValueType.Percent);
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));
            laneclearMenu.AddItem(
                new MenuItem(laneclearMenu.Name + ".q-min", "Q " + Global.Lang.Get("G_Min")).SetValue(
                    new Slider(3, 1, 5)));

            var killstealMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Killsteal"), Menu.Name + ".killsteal"));
            killstealMenu.AddItem(new MenuItem(killstealMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));

            var fleeMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Flee"), Menu.Name + ".flee"));
            fleeMenu.AddItem(new MenuItem(fleeMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(true));

            var miscMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Miscellaneous"), Menu.Name + ".miscellaneous"));
            miscMenu.AddItem(
                new MenuItem(miscMenu.Name + ".w-gapcloser", "W " + Global.Lang.Get("G_Gapcloser")).SetValue(false));
            miscMenu.AddItem(
                new MenuItem(miscMenu.Name + ".e-gapcloser", "E " + Global.Lang.Get("G_Gapcloser")).SetValue(false));
        }

        protected override void SetupSpells()
        {
            Q = new Spell(SpellSlot.Q, 850f);
            Q.SetSkillshot(0.25f, 15f * (float) Math.PI / 180, 2000f, false, SkillshotType.SkillshotCone);

            W = new Spell(SpellSlot.W, 900f);
            W.SetSkillshot(0.35f, 250f, 1650f, false, SkillshotType.SkillshotCircle);

            E = new Spell(SpellSlot.E, 425f);

            R = new Spell(SpellSlot.R, 1100f);
            R.SetSkillshot(0.25f, 110f, 2100f, false, SkillshotType.SkillshotLine);
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
                if (Menu.Item(Menu.Name + ".miscellaneous.e-gapcloser").GetValue<bool>())
                {
                    E.Cast(endPos.Extend(Player.Position, E.Range));
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
                        ItemManager.UseComboItems(enemy);
                        SummonerManager.UseComboSummoners(enemy);
                    }
                }
            }
        }

        protected override void Combo()
        {
            var useQ = Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady();
            var useW = Menu.Item(Menu.Name + ".combo.w").GetValue<bool>() && W.IsReady();
            var useE = Menu.Item(Menu.Name + ".combo.e").GetValue<bool>() && E.IsReady();
            var useR = Menu.Item(Menu.Name + ".combo.e").GetValue<bool>() && R.IsReady();

            if (useQ)
            {
                Casting.SkillShot(Q, Q.GetHitChance("combo"));
            }
            if (useW)
            {
                Casting.SkillShot(W, W.GetHitChance("combo"));
            }
            if (useE)
            {
                var target = TargetSelector.GetTarget(
                    (E.Range + Player.AttackRange) * 0.9f, LeagueSharp.Common.TargetSelector.DamageType.Physical);
                if (target != null)
                {
                    var pos = Player.Position.Extend(target.Position, E.Range);
                    if (!pos.UnderTurret(true))
                    {
                        E.Cast(pos);
                    }
                }
            }
            if (useR)
            {
                var target = TargetSelector.GetTarget(R.Range, LeagueSharp.Common.TargetSelector.DamageType.Physical);
                if (R.GetDamage(target) * 0.9f > target.Health || Orbwalking.InAutoAttackRange(target))
                {
                    var pred = R.GetPrediction(target);
                    if (pred.Hitchance >= R.GetHitChance("combo"))
                    {
                        R.Cast(pred.CastPosition);
                    }
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
                Casting.SkillShot(Q, Q.GetHitChance("harass"));
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