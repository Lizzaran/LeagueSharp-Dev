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
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXChallenger.Abstracts;
using SFXChallenger.Args;
using SFXChallenger.Enumerations;
using SFXChallenger.Helpers;
using SFXChallenger.Library;
using SFXChallenger.Library.Logger;
using SFXChallenger.Managers;
using MinionManager = SFXChallenger.Library.MinionManager;
using MinionOrderTypes = SFXChallenger.Library.MinionOrderTypes;
using MinionTeam = SFXChallenger.Library.MinionTeam;
using MinionTypes = SFXChallenger.Library.MinionTypes;
using Orbwalking = SFXChallenger.Wrappers.Orbwalking;
using Spell = SFXChallenger.Wrappers.Spell;
using Utils = SFXChallenger.Helpers.Utils;

#endregion

namespace SFXChallenger.Champions
{
    internal class Sivir : Champion
    {
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
            Obj_AI_Base.OnProcessSpellCast += OnObjAiBaseProcessSpellCast;
            Orbwalking.AfterAttack += OnOrbwalkingAfterAttack;
        }

        protected override void SetupSpells()
        {
            Q = new Spell(SpellSlot.Q, 850f);
            Q.SetSkillshot(0.25f, 90f, 1350f, false, SkillshotType.SkillshotLine);

            W = new Spell(SpellSlot.W, 800f);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R, 1100f);
        }

        protected override void AddToMenu()
        {
            var comboMenu = Menu.AddSubMenu(new Menu("Combo", Menu.Name + ".combo"));
            HitchanceManager.AddToMenu(
                comboMenu.AddSubMenu(new Menu("Hitchance", comboMenu.Name + ".hitchance")), "combo",
                new Dictionary<string, HitChance> { { "Q", HitChance.VeryHigh } });
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".q", "Use Q").SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".w", "Use W").SetValue(true));

            var harassMenu = Menu.AddSubMenu(new Menu("Harass", Menu.Name + ".harass"));
            HitchanceManager.AddToMenu(
                harassMenu.AddSubMenu(new Menu("Hitchance", harassMenu.Name + ".hitchance")), "harass",
                new Dictionary<string, HitChance> { { "Q", HitChance.High } });
            ResourceManager.AddToMenu(
                harassMenu,
                new ResourceManagerArgs(
                    "harass", ResourceType.Mana, ResourceValueType.Percent, ResourceCheckType.Minimum)
                {
                    DefaultValue = 30
                });
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".q", "Use Q").SetValue(true));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".w", "Use W").SetValue(true));

            var laneclearMenu = Menu.AddSubMenu(new Menu("Lane Clear", Menu.Name + ".lane-clear"));
            ResourceManager.AddToMenu(
                laneclearMenu,
                new ResourceManagerArgs(
                    "lane-clear-q", ResourceType.Mana, ResourceValueType.Percent, ResourceCheckType.Minimum)
                {
                    Prefix = "Q",
                    Advanced = true,
                    MaxValue = 101,
                    LevelRanges = new SortedList<int, int> { { 1, 6 }, { 6, 12 }, { 12, 18 } },
                    DefaultValues = new List<int> { 60, 50, 50 },
                    IgnoreJungleOption = true
                });
            ResourceManager.AddToMenu(
                laneclearMenu,
                new ResourceManagerArgs(
                    "lane-clear-w", ResourceType.Mana, ResourceValueType.Percent, ResourceCheckType.Minimum)
                {
                    Prefix = "W",
                    Advanced = true,
                    MaxValue = 101,
                    LevelRanges = new SortedList<int, int> { { 1, 6 }, { 6, 12 }, { 12, 18 } },
                    DefaultValues = new List<int> { 50, 40, 40 },
                    IgnoreJungleOption = true
                });
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".q", "Use Q").SetValue(true));
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".q-min", "Q Min.").SetValue(new Slider(4, 1, 5)));
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".w", "Use W").SetValue(true));
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".w-min", "W Min.").SetValue(new Slider(3, 1, 5)));

            var fleeMenu = Menu.AddSubMenu(new Menu("Flee", Menu.Name + ".flee"));
            fleeMenu.AddItem(new MenuItem(fleeMenu.Name + ".r", "Use R").SetValue(false));

            var shieldMenu = Menu.AddSubMenu(new Menu("Shield", Menu.Name + ".shield"));
            SpellBlockManager.AddToMenu(
                shieldMenu.AddSubMenu(new Menu("Whitelist", shieldMenu.Name + ".whitelist")), false, true, false);
            shieldMenu.AddItem(new MenuItem(shieldMenu.Name + ".enabled", "Enabled").SetValue(true));

            var miscMenu = Menu.AddSubMenu(new Menu("Misc", Menu.Name + ".miscellaneous"));

            HeroListManager.AddToMenu(
                miscMenu.AddSubMenu(new Menu("Q Immobile", miscMenu.Name + "q-immobile")),
                new HeroListManagerArgs("q-immobile")
                {
                    IsWhitelist = false,
                    Allies = false,
                    Enemies = true,
                    DefaultValue = false
                });

            IndicatorManager.AddToMenu(DrawingManager.Menu, true);
            IndicatorManager.Add(Q);
            IndicatorManager.Add(W);
            IndicatorManager.Finale();
        }

        // Credits: Trees
        private void OnObjAiBaseProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender == null || !sender.IsValid || !Menu.Item(Menu.Name + ".shield.enabled").GetValue<bool>())
            {
                return;
            }

            var unit = sender as Obj_AI_Hero;
            var type = args.SData.TargettingType;
            if (unit == null || !unit.IsEnemy)
            {
                return;
            }
            var blockableSpell = SpellBlockManager.Contains(unit, args);
            if (!blockableSpell)
            {
                return;
            }
            if ((type == SpellDataTargetType.Unit || type == SpellDataTargetType.SelfAndUnit) && args.Target != null &&
                args.Target.IsMe)
            {
                E.Cast();
            }
            else if (unit.ChampionName.Equals("Riven", StringComparison.OrdinalIgnoreCase) && unit.Distance(Player) < 400)
            {
                E.Cast();
            }
            else if (unit.ChampionName.Equals("Bard", StringComparison.OrdinalIgnoreCase) &&
                     type.Equals(SpellDataTargetType.Location) && args.End.Distance(Player.ServerPosition) < 300)
            {
                Utility.DelayAction.Add(400 + (int) (unit.Distance(Player) / 7f), () => E.Cast());
            }
            else if (args.SData.IsAutoAttack() && args.Target != null && args.Target.IsMe)
            {
                E.Cast();
            }
            else if (type.Equals(SpellDataTargetType.SelfAoe) &&
                     unit.Distance(Player.ServerPosition) < args.SData.CastRange + args.SData.CastRadius / 2)
            {
                E.Cast();
            }
            else if (type.Equals(SpellDataTargetType.Self))
            {
                if ((unit.ChampionName.Equals("Kalista", StringComparison.OrdinalIgnoreCase) &&
                     Player.Distance(unit) < 350))
                {
                    E.Cast();
                }
                if (unit.ChampionName.Equals("Zed", StringComparison.OrdinalIgnoreCase) &&
                    Player.Distance(unit) < 300)
                {
                    Utility.DelayAction.Add(200, () => E.Cast());
                }
            }
        }

        protected override void OnPreUpdate() {}

        protected override void OnPostUpdate()
        {
            if (Q.IsReady())
            {
                var target =
                    GameObjects.EnemyHeroes.OrderBy(e => e.Distance(Player))
                        .Where(e => Q.IsInRange(e))
                        .FirstOrDefault(t => HeroListManager.Check("q-immobile", t) && Utils.IsImmobile(t));
                if (target != null)
                {
                    Q.Cast(target.Position);
                }
            }
        }

        private void OnOrbwalkingAfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            try
            {
                if (unit.IsMe && W.IsReady())
                {
                    var useW = false;
                    var wMin = 0;
                    var laneclear = false;
                    var jungleClear = false;
                    switch (Orbwalker.ActiveMode)
                    {
                        case Orbwalking.OrbwalkingMode.Combo:
                            useW = Menu.Item(Menu.Name + ".combo.w").GetValue<bool>();
                            break;
                        case Orbwalking.OrbwalkingMode.Mixed:
                            useW = Menu.Item(Menu.Name + ".harass.w").GetValue<bool>();
                            break;
                        case Orbwalking.OrbwalkingMode.LaneClear:
                            useW = Menu.Item(Menu.Name + ".lane-clear.w").GetValue<bool>();
                            wMin = target.Team != GameObjectTeam.Neutral
                                ? Menu.Item(Menu.Name + ".lane-clear.w-min").GetValue<Slider>().Value
                                : 1;
                            laneclear = true;
                            jungleClear = target.Team == GameObjectTeam.Neutral;
                            break;
                    }
                    if (useW &&
                        (!laneclear ||
                         (jungleClear
                             ? (ResourceManager.Check("lane-clear-w") || ResourceManager.IgnoreJungle("lane-clear-w"))
                             : ResourceManager.Check("lane-clear-w"))))
                    {
                        var range = W.Range + Player.BoundingRadius * 2f;
                        var targets = laneclear
                            ? MinionManager.GetMinions(range + 450, MinionTypes.All, MinionTeam.NotAlly)
                            : GameObjects.EnemyHeroes.Where(e => e.IsValidTarget(range + 450))
                                .Cast<Obj_AI_Base>()
                                .ToList();
                        if (targets.Count >= wMin && targets.Any(Orbwalking.InAutoAttackRange) &&
                            (wMin == 0 ||
                             targets.Any(
                                 t =>
                                     Orbwalking.InAutoAttackRange(t) &&
                                     targets.Any(t2 => t2.NetworkId != t.NetworkId && t2.Distance(t) <= 450))))
                        {
                            W.Cast();
                            Orbwalking.ResetAutoAttackTimer();
                        }
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
            if (Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady() &&
                (!Menu.Item(Menu.Name + ".combo.w").GetValue<bool>() ||
                 (W.Level == 0 || !W.IsReady() || !GameObjects.EnemyHeroes.Any(Orbwalking.InAutoAttackRange))))
            {
                Casting.SkillShot(Q, Q.GetHitChance("combo"));
            }
        }

        protected override void Harass()
        {
            if (!ResourceManager.Check("harass"))
            {
                return;
            }

            if (Menu.Item(Menu.Name + ".harass.q").GetValue<bool>() && Q.IsReady() &&
                (!Menu.Item(Menu.Name + ".harass.w").GetValue<bool>() ||
                 (W.Level == 0 || !W.IsReady() || !GameObjects.EnemyHeroes.Any(Orbwalking.InAutoAttackRange))))
            {
                Casting.SkillShot(Q, Q.GetHitChance("combo"));
            }
        }

        protected override void LaneClear()
        {
            if (!ResourceManager.Check("lane-clear-q"))
            {
                return;
            }

            var useQ = Menu.Item(Menu.Name + ".lane-clear.q").GetValue<bool>() && Q.IsReady();
            if (useQ)
            {
                Casting.Farm(
                    Q, MinionManager.GetMinions(Q.Range + Q.Width),
                    Menu.Item(Menu.Name + ".lane-clear.q-min").GetValue<Slider>().Value);
            }
        }

        protected override void JungleClear()
        {
            if (!ResourceManager.Check("lane-clear-q") && !ResourceManager.IgnoreJungle("lane-clear-q"))
            {
                return;
            }

            var useQ = Menu.Item(Menu.Name + ".lane-clear.q").GetValue<bool>() && Q.IsReady();
            if (useQ)
            {
                Casting.Farm(
                    Q,
                    MinionManager.GetMinions(
                        Q.Range + Q.Width, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth), 1);
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