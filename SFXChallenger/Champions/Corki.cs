#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Corki.cs is part of SFXChallenger.

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

///*
// Copyright 2014 - 2015 Nikita Bernthaler
// Corki.cs is part of SFXChallenger.

// SFXChallenger is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// SFXChallenger is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with SFXChallenger. If not, see <http://www.gnu.org/licenses/>.
//*/

//#endregion License

//#region

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using LeagueSharp;
//using LeagueSharp.Common;
//using SFXChallenger.Abstracts;
//using SFXChallenger.Enumerations;
//using SFXChallenger.Helpers;
//using SFXChallenger.Library;
//using SFXChallenger.Library.Logger;
//using SFXChallenger.Managers;
//using SharpDX;
//using DamageType = SFXChallenger.Enumerations.DamageType;
//using Orbwalking = SFXChallenger.Wrappers.Orbwalking;
//using Spell = SFXChallenger.Wrappers.Spell;
//using TargetSelector = SFXChallenger.SFXTargetSelector.TargetSelector;

//#endregion

//namespace SFXChallenger.Champions
//{
//    internal class Corki : Champion
//    {
//        protected override ItemFlags ItemFlags
//        {
//            get { return ItemFlags.Offensive | ItemFlags.Defensive | ItemFlags.Flee; }
//        }

//        protected override ItemUsageType ItemUsage
//        {
//            get { return ItemUsageType.AfterAttack; }
//        }

//        protected override void OnLoad() {}

//        protected override void OnUnload() {}

//        protected override void AddToMenu()
//        {
//            var comboMenu = Menu.AddSubMenu(new Menu("Combo", Menu.Name + ".combo"));
//            HitchanceManager.AddToMenu(
//                comboMenu.AddSubMenu(new Menu("Hitchance", comboMenu.Name + ".hitchance")), "combo",
//                new Dictionary<string, HitChance> { { "Q", HitChance.VeryHigh }, { "R", HitChance.VeryHigh } });
//            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".q", "Use Q").SetValue(true));
//            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".e", "Use E").SetValue(true));
//            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".r", "Use R").SetValue(true));

//            var harassMenu = Menu.AddSubMenu(new Menu("Harass", Menu.Name + ".harass"));
//            HitchanceManager.AddToMenu(
//                harassMenu.AddSubMenu(new Menu("Hitchance", harassMenu.Name + ".hitchance")), "harass",
//                new Dictionary<string, HitChance> { { "Q", HitChance.High }, { "R", HitChance.High } });
//            ManaManager.AddToMenu(harassMenu, "harass", ManaCheckType.Minimum, ManaValueType.Percent);
//            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".q", "Use Q").SetValue(true));
//            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".e", "Use E").SetValue(false));
//            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".r", "Use R").SetValue(true));

//            var laneclearMenu = Menu.AddSubMenu(new Menu("Lane Clear", Menu.Name + ".lane-clear"));
//            ManaManager.AddToMenu(laneclearMenu, "lane-clear", ManaCheckType.Minimum, ManaValueType.Percent);
//            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".q-min", "Q Min.").SetValue(new Slider(3, 1, 5)));
//            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".q", "Use Q").SetValue(true));
//            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".e", "Use E").SetValue(true));
//            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".r", "Use R").SetValue(true));

//            var fleeMenu = Menu.AddSubMenu(new Menu("Flee", Menu.Name + ".flee"));
//            fleeMenu.AddItem(new MenuItem(fleeMenu.Name + ".w", "Use E").SetValue(true));

//            IndicatorManager.AddToMenu(DrawingManager.Menu, true);
//            IndicatorManager.Add(Q);
//            IndicatorManager.Add(E);
//            IndicatorManager.Add(R);
//            IndicatorManager.Finale();
//        }

//        protected override void SetupSpells()
//        {
//            Q = new Spell(SpellSlot.Q, 825f, DamageType.Magical);
//            Q.SetSkillshot(0.40f, 250f, 1100f, false, SkillshotType.SkillshotCircle);

//            W = new Spell(SpellSlot.W, 800f, DamageType.Magical);

//            E = new Spell(SpellSlot.E, 600f);
//            E.SetSkillshot(0f, 35f * (float)Math.PI / 180, 2000f, false, SkillshotType.SkillshotCone);

//            R = new Spell(SpellSlot.R, 1300f, DamageType.Magical);
//            R.SetSkillshot(0.2f, 40f, 2000f, true, SkillshotType.SkillshotLine);
//        }

//        protected override void Combo()
//        {
//            var useQ = Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && Q.IsReady();
//            var useW = Menu.Item(Menu.Name + ".combo.w").GetValue<bool>() && W.IsReady();
//            var useE = Menu.Item(Menu.Name + ".combo.e").GetValue<bool>() && E.IsReady();
//            var useR = UltimateManager.Combo() && R.IsReady();

//            if (useQ)
//            {
//                Casting.SkillShot(Q, Q.GetHitChance("combo"));
//            }
//            if (useW)
//            {
//                var target = TargetSelector.GetTarget(W);
//                var best = CPrediction.Circle(W, target, W.GetHitChance("combo"));
//                if (best.TotalHits > 0 && !best.CastPosition.Equals(Vector3.Zero))
//                {
//                    W.Cast(best.CastPosition);
//                }
//            }
//            if (useE)
//            {
//                var target = TargetSelector.GetTarget((E.Range + Player.AttackRange) * 0.9f, E.DamageType);
//                if (target != null)
//                {
//                    var pos = Player.Position.Extend(Game.CursorPos, E.Range);
//                    if (!pos.UnderTurret(true))
//                    {
//                        E.Cast(pos);
//                    }
//                }
//            }
//            if (useR)
//            {
//                var target = TargetSelector.GetTarget(R);
//                if (target != null && Orbwalking.InAutoAttackRange(target))
//                {
//                    if (!RLogic(target, Menu.Item(Menu.Name + ".ultimate.combo.min").GetValue<Slider>().Value, useQ))
//                    {
//                        if (Menu.Item(Menu.Name + ".ultimate.combo.single").GetValue<bool>())
//                        {
//                            RLogicSingle(useQ);
//                        }
//                    }
//                }
//            }
//        }

//        private bool RLogic(Obj_AI_Hero target, int min, bool q, string mode = "combo")
//        {
//            try
//            {
//                var hits = GetRHits(target);
//                if (UltimateManager.Check(mode, min, hits.Item2, hero => CalcComboDamage(hero, q, true)))
//                {
//                    R.Cast(hits.Item3);
//                    return true;
//                }
//                return false;
//            }
//            catch (Exception ex)
//            {
//                Global.Logger.AddItem(new LogItem(ex));
//            }
//            return false;
//        }

//        private void RLogicSingle(bool q)
//        {
//            try
//            {
//                foreach (var t in GameObjects.EnemyHeroes)
//                {
//                    if (UltimateManager.CheckSingle(t, CalcComboDamage(t, q, true)))
//                    {
//                        if (RLogic(t, 1, q))
//                        {
//                            break;
//                        }
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Global.Logger.AddItem(new LogItem(ex));
//            }
//        }

//        private float CalcComboDamage(Obj_AI_Hero target, bool q, bool r)
//        {
//            try
//            {
//                if (target == null)
//                {
//                    return 0;
//                }
//                float damage = 0;
//                if (q && (Q.IsReady() || Q.Instance.CooldownExpires - Game.Time <= 1))
//                {
//                    damage += Q.GetDamage(target);
//                }
//                if (r && R.IsReady())
//                {
//                    damage += R.GetDamage(target);
//                }
//                damage += 3 * (float) Player.GetAutoAttackDamage(target, true);
//                damage += ItemManager.CalculateComboDamage(target);
//                damage += SummonerManager.CalculateComboDamage(target);
//                return damage;
//            }
//            catch (Exception ex)
//            {
//                Global.Logger.AddItem(new LogItem(ex));
//            }
//            return 0;
//        }

//        private Tuple<int, List<Obj_AI_Hero>, Vector3> GetRHits(Obj_AI_Hero target)
//        {
//            var hits = new List<Obj_AI_Hero>();
//            var castPos = Vector3.Zero;
//            try
//            {
//                var pred = R.GetPrediction(target);
//                if (pred.Hitchance >= R.GetHitChance("combo"))
//                {
//                    castPos = pred.CastPosition;
//                    hits.Add(target);
//                    var pos = Player.Position.Extend(pred.CastPosition, Player.Distance(pred.UnitPosition));
//                    var pos2 = Player.Position.Extend(pos, Player.Distance(pos) + R2.Range);
//                    R2.UpdateSourcePosition(pos, pos);
//                    R2.Delay = Player.Position.Distance(pred.UnitPosition) / R.Speed + 0.1f;
//                    hits.AddRange(
//                        GameObjects.EnemyHeroes.Where(
//                            h =>
//                                h.NetworkId != target.NetworkId && h.IsValidTarget() &&
//                                h.Distance(h.Position, true) < (R.Range + R2.Range) * (R.Range + R2.Range))
//                            .Where(enemy => R2.WillHit(enemy, pos2)));
//                    R2.UpdateSourcePosition();
//                }
//            }
//            catch (Exception ex)
//            {
//                Global.Logger.AddItem(new LogItem(ex));
//            }
//            return new Tuple<int, List<Obj_AI_Hero>, Vector3>(hits.Count, hits, castPos);
//        }

//        protected override void Harass()
//        {
//            if (!ResourceManager.Check("harass"))
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
//            if (!ResourceManager.Check("lane-clear"))
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

//        protected override void Killsteal() {}
//    }
//}
