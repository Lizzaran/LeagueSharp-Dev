#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 KogMaw.cs is part of SFXChallenger.

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

namespace SFXChallenger.Champions
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
            GapcloserManager.OnGapcloser += OnEnemyGapcloser;
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

        protected override void AddToMenu()
        {
            var comboMenu = Menu.AddSubMenu(new Menu("Combo", Menu.Name + ".combo"));
            HitchanceManager.AddToMenu(
                comboMenu.AddSubMenu(new Menu("Hitchance", comboMenu.Name + ".hitchance")), "combo",
                new Dictionary<string, HitChance>
                {
                    { "Q", HitChance.VeryHigh },
                    { "E", HitChance.VeryHigh },
                    { "R", HitChance.VeryHigh }
                });
            ResourceManager.AddToMenu(
                comboMenu,
                new ResourceManagerArgs(
                    "combo-r", ResourceType.Mana, ResourceValueType.Percent, ResourceCheckType.Minimum)
                {
                    Prefix = "R",
                    DefaultValue = 30
                });
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".q", "Use Q").SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".w", "Use W").SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".e", "Use E").SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".r", "Use R").SetValue(true));

            var harassMenu = Menu.AddSubMenu(new Menu("Harass", Menu.Name + ".harass"));
            HitchanceManager.AddToMenu(
                harassMenu.AddSubMenu(new Menu("Hitchance", harassMenu.Name + ".hitchance")), "harass",
                new Dictionary<string, HitChance> { { "Q", HitChance.High }, { "R", HitChance.VeryHigh } });
            ResourceManager.AddToMenu(
                harassMenu,
                new ResourceManagerArgs(
                    "harass", ResourceType.Mana, ResourceValueType.Percent, ResourceCheckType.Minimum)
                {
                    DefaultValue = 30
                });
            ResourceManager.AddToMenu(
                harassMenu,
                new ResourceManagerArgs(
                    "harass-r", ResourceType.Mana, ResourceValueType.Percent, ResourceCheckType.Minimum)
                {
                    Prefix = "R",
                    DefaultValue = 30
                });
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".q", "Use Q").SetValue(false));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".w", "Use W").SetValue(true));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".r", "Use R").SetValue(true));

            var laneclearMenu = Menu.AddSubMenu(new Menu("Lane Clear", Menu.Name + ".lane-clear"));
            ResourceManager.AddToMenu(
                laneclearMenu,
                new ResourceManagerArgs(
                    "lane-clear", ResourceType.Mana, ResourceValueType.Percent, ResourceCheckType.Minimum)
                {
                    Advanced = true,
                    MaxValue = 101,
                    LevelRanges = new SortedList<int, int> { { 1, 6 }, { 6, 12 }, { 12, 18 } },
                    DefaultValues = new List<int> { 50, 30, 30 },
                    IgnoreJungleOption = true
                });
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".w", "Use W").SetValue(true));
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".e", "Use E").SetValue(true));
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".e-min", "E Min.").SetValue(new Slider(3, 1, 5)));
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".r", "Use R").SetValue(false));
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".r-min", "R Min.").SetValue(new Slider(3, 1, 5)));

            var fleeMenu = Menu.AddSubMenu(new Menu("Flee", Menu.Name + ".flee"));
            fleeMenu.AddItem(new MenuItem(fleeMenu.Name + ".e", "Use E").SetValue(true));

            var killstealMenu = Menu.AddSubMenu(new Menu("Killsteal", Menu.Name + ".killsteal"));
            killstealMenu.AddItem(new MenuItem(killstealMenu.Name + ".r", "Use R").SetValue(true));

            var miscMenu = Menu.AddSubMenu(new Menu("Misc", Menu.Name + ".miscellaneous"));

            var eGapcloserMenu = miscMenu.AddSubMenu(new Menu("E Gapcloser", miscMenu.Name + "e-gapcloser"));
            GapcloserManager.AddToMenu(
                eGapcloserMenu,
                new HeroListManagerArgs("e-gapcloser")
                {
                    IsWhitelist = false,
                    Allies = false,
                    Enemies = true,
                    DefaultValue = false
                }, true);
            BestTargetOnlyManager.AddToMenu(eGapcloserMenu, "e-gapcloser");

            var rImmobileMenu = miscMenu.AddSubMenu(new Menu("R Immobile", miscMenu.Name + "r-immobile"));
            HeroListManager.AddToMenu(
                rImmobileMenu,
                new HeroListManagerArgs("r-immobile")
                {
                    IsWhitelist = false,
                    Allies = false,
                    Enemies = true,
                    DefaultValue = false
                });
            BestTargetOnlyManager.AddToMenu(rImmobileMenu, "r-immobile", true);

            var rGapcloserMenu = miscMenu.AddSubMenu(new Menu("R Gapcloser", miscMenu.Name + "r-gapcloser"));
            GapcloserManager.AddToMenu(
                rGapcloserMenu,
                new HeroListManagerArgs("r-gapcloser")
                {
                    IsWhitelist = false,
                    Allies = false,
                    Enemies = true,
                    DefaultValue = false
                });
            BestTargetOnlyManager.AddToMenu(rGapcloserMenu, "r-gapcloser", true);

            miscMenu.AddItem(new MenuItem(miscMenu.Name + ".r-max", "R Max. Stacks").SetValue(new Slider(5, 1, 10)));

            IndicatorManager.AddToMenu(DrawingManager.Menu, true);
            IndicatorManager.Add(Q);
            IndicatorManager.Add(W);
            IndicatorManager.Add(E);
            IndicatorManager.Add(R);
            IndicatorManager.Finale();
        }

        protected override void OnPreUpdate() {}

        protected override void OnPostUpdate()
        {
            if (HeroListManager.Enabled("r-immobile") && R.IsReady())
            {
                var target =
                    GameObjects.EnemyHeroes.FirstOrDefault(
                        t =>
                            t.IsValidTarget(R.Range) && HeroListManager.Check("r-immobile", t) &&
                            BestTargetOnlyManager.Check("r-immobile", R, t) && Utils.IsImmobile(t));
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

        private void OnEnemyGapcloser(object sender, GapcloserManagerArgs args)
        {
            try
            {
                if (args.UniqueId.Equals("e-gapcloser") && E.IsReady() &&
                    BestTargetOnlyManager.Check("e-gapcloser", E, args.Hero))
                {
                    if (args.End.Distance(Player.Position) <= E.Range)
                    {
                        E.Cast(args.End);
                    }
                }
                if (args.UniqueId.Equals("r-gapcloser") && R.IsReady() &&
                    BestTargetOnlyManager.Check("r-gapcloser", R, args.Hero) &&
                    Menu.Item(Menu.Name + ".miscellaneous.r-max").GetValue<Slider>().Value > GetRBuffCount())
                {
                    if (args.End.Distance(Player.Position) <= R.Range)
                    {
                        R.Cast(args.End);
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private bool ShouldUseR()
        {
            var attackSpeed =
                (~(int) ((1 / ObjectManager.Player.AttackSpeedMod * 100) - (1 / ObjectManager.Player.AttackDelay * 100)) +
                 1);

            if (attackSpeed > 70 && Player.HasBuff("KogMawBioArcaneBarrage"))
            {
                return false;
            }

            return true;
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
            if (useR && ResourceManager.Check("combo-r") &&
                Menu.Item(Menu.Name + ".miscellaneous.r-max").GetValue<Slider>().Value > GetRBuffCount())
            {
                var target = TargetSelector.GetTarget(R);
                if (target != null &&
                    Menu.Item(Menu.Name + ".miscellaneous.r-max").GetValue<Slider>().Value > GetRBuffCount() &&
                    (Player.FlatMagicDamageMod > 50 || ShouldUseR()))
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
            if (ResourceManager.Check("harass"))
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
            if (ResourceManager.Check("harass-r"))
            {
                var useR = Menu.Item(Menu.Name + ".harass.r").GetValue<bool>() && R.IsReady();
                if (useR && Menu.Item(Menu.Name + ".miscellaneous.r-max").GetValue<Slider>().Value > GetRBuffCount())
                {
                    var target = TargetSelector.GetTarget(R);
                    if (target != null && (Player.FlatMagicDamageMod > 50 || ShouldUseR()))
                    {
                        Casting.SkillShot(R, R.GetHitChance("harass"));
                    }
                }
            }
        }

        protected override void LaneClear()
        {
            if (!ResourceManager.Check("lane-clear"))
            {
                return;
            }

            var useW = Menu.Item(Menu.Name + ".lane-clear.w").GetValue<bool>() && W.IsReady();
            var useE = Menu.Item(Menu.Name + ".lane-clear.e").GetValue<bool>() && E.IsReady();
            var useR = Menu.Item(Menu.Name + ".lane-clear.r").GetValue<bool>() && R.IsReady() &&
                       Menu.Item(Menu.Name + ".miscellaneous.r-max").GetValue<Slider>().Value > GetRBuffCount();

            if (useW)
            {
                Casting.FarmSelfAoe(
                    W, MinionManager.GetMinions(W.Range), 1,
                    Player.AttackRange + Player.BoundingRadius * 1.25f + 20 * W.Level);
            }
            if (useE)
            {
                Casting.Farm(
                    E, MinionManager.GetMinions(E.Range),
                    Menu.Item(Menu.Name + ".lane-clear.e-min").GetValue<Slider>().Value);
            }
            if (useR)
            {
                Casting.Farm(
                    R, MinionManager.GetMinions(R.Range),
                    Menu.Item(Menu.Name + ".lane-clear.r-min").GetValue<Slider>().Value);
            }
        }

        protected override void JungleClear()
        {
            if (!ResourceManager.Check("lane-clear") && !ResourceManager.IgnoreJungle("lane-clear"))
            {
                return;
            }

            var useW = Menu.Item(Menu.Name + ".lane-clear.w").GetValue<bool>() && W.IsReady();
            var useE = Menu.Item(Menu.Name + ".lane-clear.e").GetValue<bool>() && E.IsReady();
            var useR = Menu.Item(Menu.Name + ".lane-clear.r").GetValue<bool>() && R.IsReady() &&
                       Menu.Item(Menu.Name + ".miscellaneous.r-max").GetValue<Slider>().Value > GetRBuffCount();

            if (useW)
            {
                Casting.FarmSelfAoe(
                    W,
                    MinionManager.GetMinions(W.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth),
                    1, Player.AttackRange + Player.BoundingRadius * 1.25f + 20 * W.Level);
            }
            if (useE)
            {
                Casting.Farm(
                    E,
                    MinionManager.GetMinions(E.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth),
                    1);
            }
            if (useR)
            {
                Casting.Farm(
                    R,
                    MinionManager.GetMinions(R.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth),
                    1);
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
                    GameObjects.EnemyHeroes.Where(
                        e => e.IsValidTarget(R.Range) && !Orbwalking.InAutoAttackRange(e) && R.IsKillable(e))
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