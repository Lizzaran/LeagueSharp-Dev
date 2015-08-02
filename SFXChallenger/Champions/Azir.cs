#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Azir.cs is part of SFXChallenger.

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

//#region

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using LeagueSharp;
//using LeagueSharp.Common;
//using SFXChallenger.Abstracts;
//using SFXChallenger.Enumerations;
//using SFXChallenger.Helpers;
//using SFXChallenger.Managers;
//using SFXLibrary;
//using SFXLibrary.Logger;
//using Orbwalking = SFXChallenger.Wrappers.Orbwalking;
//using TargetSelector = SFXChallenger.Wrappers.TargetSelector;

//#endregion

//namespace SFXChallenger.Champions
//{
//    internal class Azir : Champion
//    {
//        protected override ItemFlags ItemFlags
//        {
//            get { return ItemFlags.Offensive | ItemFlags.Defensive | ItemFlags.Flee; }
//        }

//        protected override void OnLoad()
//        {

//        }

//        protected override void OnUnload()
//        {

//        }

//        protected override void AddToMenu()
//        {
//            var comboMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Combo"), Menu.Name + ".combo"));
//            HitchanceManager.AddToMenu(
//                comboMenu.AddSubMenu(new Menu(Global.Lang.Get("F_MH"), comboMenu.Name + ".hitchance")), "combo",
//                new Dictionary<string, int> { { "Q", 2 }, { "W", 1 }, { "R", 2 } });
//            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));
//            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".w", Global.Lang.Get("G_UseW")).SetValue(true));
//            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(true));
//            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".r", Global.Lang.Get("G_UseR")).SetValue(true));

//            var harassMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Harass"), Menu.Name + ".harass"));
//            HitchanceManager.AddToMenu(
//                harassMenu.AddSubMenu(new Menu(Global.Lang.Get("F_MH"), harassMenu.Name + ".hitchance")), "harass",
//                new Dictionary<string, int> { { "Q", 2 } });
//            ManaManager.AddToMenu(harassMenu, "harass", ManaCheckType.Minimum, ManaValueType.Percent);
//            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));

//            var laneclearMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_LaneClear"), Menu.Name + ".lane-clear"));
//            ManaManager.AddToMenu(laneclearMenu, "lane-clear", ManaCheckType.Minimum, ManaValueType.Percent);
//            laneclearMenu.AddItem(
//                new MenuItem(laneclearMenu.Name + ".q-min", "Q " + Global.Lang.Get("G_Min")).SetValue(
//                    new Slider(3, 1, 5)));
//            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));

//            var killstealMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Killsteal"), Menu.Name + ".killsteal"));
//            killstealMenu.AddItem(new MenuItem(killstealMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));

//            var fleeMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Flee"), Menu.Name + ".flee"));
//            fleeMenu.AddItem(new MenuItem(fleeMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(true));

//            var miscMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Miscellaneous"), Menu.Name + ".miscellaneous"));
//            miscMenu.AddItem(
//                new MenuItem(miscMenu.Name + ".w-gapcloser", "W " + Global.Lang.Get("G_Gapcloser")).SetValue(false));
//            miscMenu.AddItem(
//                new MenuItem(miscMenu.Name + ".e-gapcloser", "E " + Global.Lang.Get("G_Gapcloser")).SetValue(false));

//            IndicatorManager.AddToMenu(DrawingManager.GetMenu(), true);
//            IndicatorManager.Add(Q);
//            IndicatorManager.Add(W);
//            IndicatorManager.Add(E);
//            IndicatorManager.Add(R);
//            IndicatorManager.Finale();
//        }

//        protected override void SetupSpells()
//        {
//            Q = new Wrappers.Spell(SpellSlot.Q, 1075f);
//            Q.SetSkillshot(0f, 65f, 1500f, false, SkillshotType.SkillshotLine);

//            W = new Wrappers.Spell(SpellSlot.W, 450f);

//            E = new Wrappers.Spell(SpellSlot.E, 1150f);
//            E.SetSkillshot(0f, 65f, 1500f, false, SkillshotType.SkillshotLine);

//            R = new Wrappers.Spell(SpellSlot.R, 250f);
//            R.SetSkillshot(0.5f, 700f, 1400f, false, SkillshotType.SkillshotLine);
//        }


//        protected override void Combo()
//        {
//            var useQ = Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady();
//            var useW = Menu.Item(Menu.Name + ".combo.w").GetValue<bool>() && W.IsReady();
//            var useE = Menu.Item(Menu.Name + ".combo.e").GetValue<bool>() && E.IsReady();
//            var useR = Menu.Item(Menu.Name + ".combo.r").GetValue<bool>() && R.IsReady();

//            if (useQ)
//            {
//                Casting.SkillShot(Q, Q.GetHitChance("combo"));
//            }
//            if (useW)
//            {
//                Casting.SkillShot(W, W.GetHitChance("combo"));
//            }
//            if (useE)
//            {
//                var target = Wrappers.TargetSelector.GetTarget(
//                    (E.Range + Player.AttackRange) * 0.9f, Enumerations.DamageType.Physical);
//                if (target != null)
//                {
//                    var pos = Player.Position.Extend(target.Position, E.Range);
//                    if (!pos.UnderTurret(true))
//                    {
//                        E.Cast(pos);
//                    }
//                }
//            }
//            if (useR)
//            {
//                var target = Wrappers.TargetSelector.GetTarget(R.Range, Enumerations.DamageType.Physical);
//                if (target != null && R.GetDamage(target) * 0.9f > target.Health || Orbwalking.InAutoAttackRange(target))
//                {
//                    var pred = R.GetPrediction(target);
//                    if (pred.Hitchance >= R.GetHitChance("combo"))
//                    {
//                        R.Cast(pred.CastPosition);
//                    }
//                }
//            }
//        }

//        protected override void Harass()
//        {
//            if (!ManaManager.Check("harass"))
//            {
//                return;
//            }

//            if (Menu.Item(Menu.Name + ".harass.q").GetValue<bool>() && Q.IsReady())
//            {
//                Casting.SkillShot(Q, Q.GetHitChance("harass"));
//            }
//        }

//        protected override void LaneClear()
//        {
//            if (!ManaManager.Check("lane-clear"))
//            {
//                return;
//            }

//            var useQ = Menu.Item(Menu.Name + ".lane-clear.q").GetValue<bool>() && Q.IsReady();
//            var minQ = Menu.Item(Menu.Name + ".lane-clear.q-min").GetValue<Slider>().Value;

//            if (useQ)
//            {
//                Casting.Farm(Q, minQ, 200f);
//            }
//        }

//        protected override void Flee()
//        {
//            if (Menu.Item(Menu.Name + ".flee.e").GetValue<bool>() && E.IsReady())
//            {
//                E.Cast(Player.Position.Extend(Game.CursorPos, E.Range));
//            }
//        }

//        protected override void Killsteal()
//        {
//            if (Menu.Item(Menu.Name + ".killsteal.q").GetValue<bool>() && Q.IsReady())
//            {
//                var fPredEnemy =
//                    GameObjects.EnemyHeroes.Where(e => e.IsValidTarget(Q.Range * 1.2f) && Q.IsKillable(e))
//                        .Select(enemy => Q.GetPrediction(enemy, true))
//                        .FirstOrDefault(pred => pred.Hitchance >= Q.GetHitChance("harass"));
//                if (fPredEnemy != null)
//                {
//                    Q.Cast(fPredEnemy.CastPosition);
//                }
//            }
//        }

//        internal class Soldiers
//        {
//            private static readonly List<Obj_AI_Minion> _soldiers = new List<Obj_AI_Minion>();
//            private static readonly Dictionary<int, string> _animations = new Dictionary<int, string>();

//            static Soldiers()
//            {
//                GameObject.OnCreate += OnGameObjectCreate;
//                GameObject.OnDelete += OnGameObjectDelete;
//                Obj_AI_Base.OnPlayAnimation += OnObjAiBasePlayAnimation;
//            }

//            public static List<Obj_AI_Minion> Idle
//            {
//                get
//                {
//                    return
//                        _soldiers.Where(
//                            s =>
//                                s.IsValid && !s.IsDead && !s.IsMoving &&
//                                (!_animations.ContainsKey(s.NetworkId) || _animations[s.NetworkId] != "Inactive"))
//                            .ToList();
//                }
//            }

//            public static List<Obj_AI_Minion> All
//            {
//                get { return _soldiers.Where(s => s.IsValid && !s.IsDead).ToList(); }
//            }

//            private static void OnGameObjectCreate(GameObject sender, EventArgs args)
//            {
//                var minion = sender as Obj_AI_Minion;
//                if (minion != null && minion.IsAlly && minion.CharData.BaseSkinName == "AzirSoldier")
//                {
//                    _soldiers.Add(minion);
//                }
//            }

//            private static void OnGameObjectDelete(GameObject sender, EventArgs args)
//            {
//                _soldiers.RemoveAll(s => s.NetworkId == sender.NetworkId);
//                _animations.Remove(sender.NetworkId);
//            }

//            private static void OnObjAiBasePlayAnimation(Obj_AI_Base sender, GameObjectPlayAnimationEventArgs args)
//            {
//                var minion = sender as Obj_AI_Minion;
//                if (minion != null && minion.IsAlly && minion.CharData.BaseSkinName == "AzirSoldier")
//                {
//                    _animations[sender.NetworkId] = args.Animation;
//                }
//            }
//        }
//    }
//}
