#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Viktor.cs is part of SFXChallenger.

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
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXChallenger.Abstracts;
using SFXChallenger.Enumerations;
using SFXChallenger.Managers;
using SFXLibrary.Logger;
using Orbwalking = SFXChallenger.Wrappers.Orbwalking;

#endregion

namespace SFXChallenger.Champions
{

    #region

    #endregion

    internal class Viktor : Champion
    {
        protected override ItemFlags ItemFlags
        {
            get { return ItemFlags.Offensive | ItemFlags.Defensive | ItemFlags.Flee; }
        }

        protected override void OnLoad()
        {
            Core.OnPreUpdate += OnCorePreUpdate;
            Core.OnPostUpdate += OnCorePostUpdate;
            Orbwalking.BeforeAttack += OnOrbwalkingBeforeAttack;
            Orbwalking.OnNonKillableMinion += OnOrbwalkingNonKillableMinion;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget += OnInterruptableTarget;
            CustomEvents.Unit.OnDash += OnUnitDash;
        }

        protected override void OnUnload()
        {
            Core.OnPreUpdate -= OnCorePreUpdate;
            Core.OnPostUpdate -= OnCorePostUpdate;
            Orbwalking.BeforeAttack -= OnOrbwalkingBeforeAttack;
            Orbwalking.OnNonKillableMinion -= OnOrbwalkingNonKillableMinion;
            AntiGapcloser.OnEnemyGapcloser -= OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget -= OnInterruptableTarget;
            CustomEvents.Unit.OnDash -= OnUnitDash;
        }

        protected override void AddToMenu()
        {
            var drawingMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Drawing"), Menu.Name + ".drawing"));
            drawingMenu.AddItem(
                new MenuItem(drawingMenu.Name + ".circle-thickness", Global.Lang.Get("G_CircleThickness")).SetValue(
                    new Slider(2, 0, 10)));
            drawingMenu.AddItem(new MenuItem(drawingMenu.Name + ".q", "Q").SetValue(new Circle(false, Color.White)));
            drawingMenu.AddItem(new MenuItem(drawingMenu.Name + ".w", "W").SetValue(new Circle(false, Color.White)));
            drawingMenu.AddItem(new MenuItem(drawingMenu.Name + ".e", "E").SetValue(new Circle(false, Color.White)));
            drawingMenu.AddItem(new MenuItem(drawingMenu.Name + ".r", "R").SetValue(new Circle(false, Color.White)));

            var comboMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Combo"), Menu.Name + ".combo"));
            HitchanceManager.AddToMenu(
                comboMenu.AddSubMenu(new Menu(Global.Lang.Get("F_MH"), comboMenu.Name + ".hitchance")), "combo",
                new Dictionary<string, int> { { "W", 2 }, { "E", 2 }, { "R", 2 } });
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".w", Global.Lang.Get("G_UseW")).SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(true));

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

            var ultimateMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Ultimate"), Menu.Name + ".ultimate"));

            var uComboMenu = ultimateMenu.AddSubMenu(new Menu(Global.Lang.Get("G_Combo"), ultimateMenu.Name + ".combo"));
            uComboMenu.AddItem(
                new MenuItem(uComboMenu.Name + ".min", "R " + Global.Lang.Get("G_Min")).SetValue(new Slider(3, 1, 5)));
            uComboMenu.AddItem(new MenuItem(uComboMenu.Name + ".1v1", "R 1v1").SetValue(true));
            uComboMenu.AddItem(new MenuItem(uComboMenu.Name + ".enabled", Global.Lang.Get("G_Enabled")).SetValue(true));

            var uAutoMenu = ultimateMenu.AddSubMenu(new Menu(Global.Lang.Get("G_Auto"), ultimateMenu.Name + ".auto"));

            var autoInterruptMenu =
                uAutoMenu.AddSubMenu(new Menu(Global.Lang.Get("G_InterruptSpell"), uAutoMenu.Name + ".interrupt"));
            foreach (var enemy in HeroManager.Enemies)
            {
                autoInterruptMenu.AddItem(
                    new MenuItem(autoInterruptMenu.Name + "." + enemy.ChampionName, enemy.ChampionName).SetValue(true));
            }

            uAutoMenu.AddItem(
                new MenuItem(uAutoMenu.Name + ".min", Global.Lang.Get("G_Min")).SetValue(new Slider(3, 1, 5)));
            uAutoMenu.AddItem(new MenuItem(uAutoMenu.Name + ".1v1", "R 1v1").SetValue(true));
            uAutoMenu.AddItem(new MenuItem(uAutoMenu.Name + ".enabled", Global.Lang.Get("G_Enabled")).SetValue(true));

            var uAssistedMenu =
                ultimateMenu.AddSubMenu(new Menu(Global.Lang.Get("G_Assisted"), ultimateMenu.Name + ".assisted"));
            uAssistedMenu.AddItem(
                new MenuItem(uAssistedMenu.Name + ".min", "R " + Global.Lang.Get("G_Min")).SetValue(new Slider(3, 1, 5)));
            uAssistedMenu.AddItem(new MenuItem(uAssistedMenu.Name + ".1v1", "R 1v1").SetValue(true));
            uAssistedMenu.AddItem(
                new MenuItem(uAssistedMenu.Name + ".hotkey", Global.Lang.Get("G_Hotkey")).SetValue(
                    new KeyBind('R', KeyBindType.Press)));
            uAssistedMenu.AddItem(
                new MenuItem(uAssistedMenu.Name + ".move-cursor", Global.Lang.Get("G_MoveCursor")).SetValue(true));
            uAssistedMenu.AddItem(
                new MenuItem(uAssistedMenu.Name + ".enabled", Global.Lang.Get("G_Enabled")).SetValue(true));

            var killstealMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Killsteal"), Menu.Name + ".killsteal"));
            killstealMenu.AddItem(new MenuItem(killstealMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(true));
            killstealMenu.AddItem(
                new MenuItem(killstealMenu.Name + ".q-aa", Global.Lang.Get("Viktor_QAA")).SetValue(true));

            var fleeMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Flee"), Menu.Name + ".flee"));
            fleeMenu.AddItem(new MenuItem(fleeMenu.Name + ".w", Global.Lang.Get("G_UseW")).SetValue(true));
            fleeMenu.AddItem(
                new MenuItem(fleeMenu.Name + ".q-upgraded", Global.Lang.Get("Viktor_QUpgraded")).SetValue(true));

            var miscMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Miscellaneous"), Menu.Name + ".miscellaneous"));
            miscMenu.AddItem(
                new MenuItem(miscMenu.Name + ".q-unkillable", "Q " + Global.Lang.Get("G_Unkillable")).SetValue(false));
            miscMenu.AddItem(
                new MenuItem(miscMenu.Name + ".q-aa-range", Global.Lang.Get("Viktor_QAARange")).SetValue(false));
            miscMenu.AddItem(
                new MenuItem(miscMenu.Name + ".w-stunned", "W " + Global.Lang.Get("G_Stunned")).SetValue(true));
            miscMenu.AddItem(new MenuItem(miscMenu.Name + ".w-dash", "W " + Global.Lang.Get("G_Dash")).SetValue(true));
            miscMenu.AddItem(
                new MenuItem(miscMenu.Name + ".w-gapcloser", "W " + Global.Lang.Get("G_Dash")).SetValue(true));
        }

        protected override void SetupSpells()
        {
            Q = new Spell(SpellSlot.Q, 600f);
            Q.SetTargetted(0.25f, 2000f);

            W = new Spell(SpellSlot.W, 700f);
            W.SetSkillshot(0.25f, 300f, float.MaxValue, false, SkillshotType.SkillshotCircle);

            E = new Spell(SpellSlot.E, 525f);
            E.SetSkillshot(0.0f, 90f, 1200f, false, SkillshotType.SkillshotLine);

            R = new Spell(SpellSlot.R, 700f);
            R.SetSkillshot(0.5f, 450f, float.MaxValue, false, SkillshotType.SkillshotCircle);
        }

        private void OnCorePreUpdate(EventArgs args)
        {
            try {}
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnCorePostUpdate(EventArgs args)
        {
            try {}
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnOrbwalkingNonKillableMinion(AttackableUnit minion) {}

        private void OnOrbwalkingBeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            try
            {
                if (args.Target.Type == GameObjectType.obj_AI_Hero &&
                    (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo ||
                     Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Harass))
                {
                    args.Process = (!Q.IsReady() || Player.Mana < Q.Instance.ManaCost) &&
                                   (!E.IsReady() || Player.Mana < E.Instance.ManaCost);
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
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnInterruptableTarget(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            try {}
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
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected override void Combo()
        {
            var q = Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady();
            var w = Menu.Item(Menu.Name + ".combo.w").GetValue<bool>() && W.IsReady();
            var e = Menu.Item(Menu.Name + ".combo.e").GetValue<bool>() && E.IsReady();
        }

        private bool IsNearTurret(Obj_AI_Base target)
        {
            try
            {
                return
                    ObjectManager.Get<Obj_AI_Turret>().Any(turret => turret.IsValidTarget(1200f, true, target.Position));
            }

            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return false;
        }

        private float CalcComboDamage(Obj_AI_Base target, bool q, bool w, bool e, bool r)
        {
            try {}
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return 0;
        }

        private void RLogic1V1(HitChance hitChance, bool q, bool w, bool e, bool face = true) {}

        private bool RLogic(HitChance hitChance, int min)
        {
            return true;
        }

        private void QLogic(HitChance hitChance) {}

        protected override void Harass() {}

        protected override void LaneClear() {}

        protected override void Flee() {}

        protected override void Killsteal() {}

        protected override void OnDraw() {}
    }
}