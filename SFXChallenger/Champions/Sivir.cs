#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Sivir.cs is part of SFXChallenger.

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
using LeagueSharp;
using LeagueSharp.Common;
using SFXChallenger.Abstracts;
using SFXChallenger.Enumerations;
using SFXChallenger.Events;
using SFXChallenger.Helpers;
using SFXChallenger.Managers;
using SFXLibrary.Logger;
using Orbwalking = SFXChallenger.Wrappers.Orbwalking;
using TargetSelector = SFXChallenger.Wrappers.TargetSelector;
using Utils = SFXChallenger.Helpers.Utils;

#endregion

namespace SFXChallenger.Champions
{
    internal class Sivir : Champion
    {
        private const float ShieldTime = 1.5f;

        protected override ItemFlags ItemFlags
        {
            get { return ItemFlags.Offensive | ItemFlags.Defensive | ItemFlags.Flee; }
        }

        protected override void OnLoad()
        {
            TargetSpellManager.OnEnemyTargetCast += OnEnemyTargetCast;
            Orbwalking.AfterAttack += OnOrbwalkingAfterAttack;
        }

        protected override void OnUnload()
        {
            TargetSpellManager.OnEnemyTargetCast -= OnEnemyTargetCast;
            Orbwalking.AfterAttack -= OnOrbwalkingAfterAttack;
        }

        protected override void AddToMenu()
        {
            var comboMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Combo"), Menu.Name + ".combo"));
            HitchanceManager.AddToMenu(
                comboMenu.AddSubMenu(new Menu(Global.Lang.Get("F_MH"), comboMenu.Name + ".hitchance")), "combo",
                new Dictionary<string, int> { { "Q", 2 } });
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".w", Global.Lang.Get("G_UseW")).SetValue(true));

            var harassMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Harass"), Menu.Name + ".harass"));
            HitchanceManager.AddToMenu(
                harassMenu.AddSubMenu(new Menu(Global.Lang.Get("F_MH"), harassMenu.Name + ".hitchance")), "harass",
                new Dictionary<string, int> { { "Q", 2 } });
            ManaManager.AddToMenu(harassMenu, "harass", ManaCheckType.Minimum, ManaValueType.Percent);
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(true));

            var laneclearMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_LaneClear"), Menu.Name + ".lane-clear"));
            ManaManager.AddToMenu(laneclearMenu, "lane-clear-q", ManaCheckType.Minimum, ManaValueType.Percent, "Q");
            ManaManager.AddToMenu(laneclearMenu, "lane-clear-e", ManaCheckType.Minimum, ManaValueType.Percent, "E");
            laneclearMenu.AddItem(
                new MenuItem(laneclearMenu.Name + ".q-min", "Q " + Global.Lang.Get("G_Min")).SetValue(
                    new Slider(3, 1, 5)));
            laneclearMenu.AddItem(
                new MenuItem(laneclearMenu.Name + ".e-min", "E " + Global.Lang.Get("G_Min")).SetValue(
                    new Slider(3, 1, 5)));
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(true));

            var fleeMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Flee"), Menu.Name + ".flee"));
            fleeMenu.AddItem(new MenuItem(fleeMenu.Name + ".r", Global.Lang.Get("G_UseR")).SetValue(false));

            var shieldMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("Sivir_Shield"), Menu.Name + ".shield"));
            TargetSpellManager.AddToMenu(
                shieldMenu.AddSubMenu(new Menu(Global.Lang.Get("G_Whitelist"), shieldMenu.Name + ".whitelist")), false,
                true);
            shieldMenu.AddItem(new MenuItem(shieldMenu.Name + ".enabled", Global.Lang.Get("G_Enabled")).SetValue(true));

            var miscMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Miscellaneous"), Menu.Name + ".miscellaneous"));
            miscMenu.AddItem(
                new MenuItem(miscMenu.Name + ".q-stun", "Q " + Global.Lang.Get("G_Stunned")).SetValue(false));

            IndicatorManager.AddToMenu(DrawingManager.GetMenu(), true);
            IndicatorManager.Add(Q);
            IndicatorManager.Add(W);
            IndicatorManager.Add(R);
            IndicatorManager.Finale();
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

        private void OnEnemyTargetCast(object sender, TargetCastArgs args)
        {
            try
            {
                if (Menu.Item(Menu.Name + ".shield.enabled").GetValue<bool>() &&
                    (args.Target == null || args.Target.IsMe))
                {
                    if (args.Type == SpellDataTargetType.SelfAoe &&
                        args.Sender.Distance(Player.Position) <=
                        args.SData.CastRadius + args.Sender.BoundingRadius + Player.BoundingRadius && E.IsReady())
                    {
                        E.Cast();
                    }
                    if (args.Type == SpellDataTargetType.Unit && args.Target != null && args.Target.IsMe)
                    {
                        var delay = (int) (Utils.GetSpellDelay(args.Sender, Player, args.Delay, args.Speed, true)) *
                                    1000;
                        var ping = Game.Ping / 2000;
                        if (delay - 200 - ping > 0)
                        {
                            Utility.DelayAction.Add(
                                delay - 100 - ping, delegate
                                {
                                    if (E.IsReady())
                                    {
                                        E.Cast();
                                    }
                                });
                        }
                        else if (E.IsReady())
                        {
                            E.Cast();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnOrbwalkingAfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            try
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
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
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
                Casting.Farm(Q, minQ);
            }
        }

        protected override void Flee()
        {
            if (Menu.Item(Menu.Name + ".flee.r").GetValue<bool>() && R.IsReady())
            {
                R.Cast();
            }
        }

        protected override void Killsteal() {}
    }
}