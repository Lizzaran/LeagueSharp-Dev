#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Kalista.cs is part of SFXChallenger.

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
// Kalista.cs is part of SFXChallenger.

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

//namespace SFXChallenger.Champions
//{
//    #region

//    using System;
//    using System.Collections.Generic;
//    using System.Linq;
//    using Abstracts;
//    using Enumerations;
//    using LeagueSharp;
//    using LeagueSharp.Common;
//    using Managers;
//    using SFXLibrary.Logger;
//    using SharpDX;
//    using Wrappers;
//    using Color = System.Drawing.Color;
//    using Orbwalking = LeagueSharp.Common.Orbwalking;
//    using TargetSelector = LeagueSharp.Common.TargetSelector;

//    #endregion

//    internal class Kalista : Champion
//    {
//        protected override ItemFlags ItemFlags
//        {
//            get { return ItemFlags.Offensive | ItemFlags.Defensive | ItemFlags.Flee; }
//        }

//        protected override void OnLoad()
//        {
//            Obj_AI_Base.OnProcessSpellCast += OnObjAiBaseProcessSpellCast;
//            Spellbook.OnCastSpell += OnSpellbookCastSpell;
//            Orbwalking.OnNonKillableMinion += OnOrbwalkingNonKillableMinion;
//            Core.OnPreUpdate += OnCorePreUpdate;
//        }

//        protected override void OnUnload()
//        {
//            Obj_AI_Base.OnProcessSpellCast -= OnObjAiBaseProcessSpellCast;
//            Spellbook.OnCastSpell -= OnSpellbookCastSpell;
//            Orbwalking.OnNonKillableMinion -= OnOrbwalkingNonKillableMinion;
//            Core.OnPreUpdate -= OnCorePreUpdate;
//        }

//        protected override void AddToMenu()
//        {
//            var drawingMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Drawing"), Menu.Name + ".drawing"));
//            drawingMenu.AddItem(
//                new MenuItem(drawingMenu.Name + ".circle-thickness", Global.Lang.Get("G_CircleThickness")).SetValue(new Slider(2, 0, 10)));
//            drawingMenu.AddItem(new MenuItem(drawingMenu.Name + ".q", "Q").SetValue(new Circle(false, Color.White)));
//            drawingMenu.AddItem(new MenuItem(drawingMenu.Name + ".w", "W").SetValue(new Circle(false, Color.White)));
//            drawingMenu.AddItem(new MenuItem(drawingMenu.Name + ".e", "E").SetValue(new Circle(false, Color.White)));
//            drawingMenu.AddItem(new MenuItem(drawingMenu.Name + ".r", "R").SetValue(new Circle(false, Color.White)));

//            var comboMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("C_Combo"), Menu.Name + ".combo"));
//            ManaManager.AddToMenu(comboMenu, "combo", 0);
//            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".q", Global.Lang.Get("C_UseQ")).SetValue(true));
//            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".e", Global.Lang.Get("C_UseE")).SetValue(true));
//            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".min-e-stacks", Global.Lang.Get("Kalista_MinEStacks")).SetValue(new Slider(10, 1, 20)));

//            var harassMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("C_Harass"), Menu.Name + ".harass"));
//            ManaManager.AddToMenu(harassMenu, "harass");
//            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".q", Global.Lang.Get("C_UseQ")).SetValue(true));
//            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".e-reset", Global.Lang.Get("Kalista_EReset")).SetValue(true));

//            var laneclearMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("C_LaneClear"), Menu.Name + ".lane-clear"));
//            ManaManager.AddToMenu(laneclearMenu, "lane-clear");
//            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".q", Global.Lang.Get("C_UseQ")).SetValue(true));
//            laneclearMenu.AddItem(
//                new MenuItem(laneclearMenu.Name + ".min-q-minions", Global.Lang.Get("Kalista_MinQMinions")).SetValue(new Slider(3, 1, 10)));
//            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".e", Global.Lang.Get("C_UseE")).SetValue(false));
//            laneclearMenu.AddItem(
//                new MenuItem(laneclearMenu.Name + ".min-e-minions", Global.Lang.Get("Kalista_MinEMinions")).SetValue(new Slider(2, 1, 10)));


//            var fleeMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("C_Flee"), Menu.Name + ".flee"));
//            ManaManager.AddToMenu(fleeMenu, "flee", 15);
//            fleeMenu.AddItem(new MenuItem(fleeMenu.Name + ".autoattacks", Global.Lang.Get("C_UseAutoAttacks")).SetValue(true));

//            var miscMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Miscellaneous"), Menu.Name + ".miscellaneous"));
//            miscMenu.AddItem(new MenuItem(miscMenu.Name + ".e-big-minion", Global.Lang.Get("Kalista_EBig")).SetValue(true));
//            miscMenu.AddItem(new MenuItem(miscMenu.Name + ".e-unkillable-minion", Global.Lang.Get("Kalista_EUnkillable")).SetValue(true));
//            miscMenu.AddItem(new MenuItem(miscMenu.Name + ".e-killsteal", Global.Lang.Get("Kalista_EKillsteal")).SetValue(true));
//            miscMenu.AddItem(new MenuItem(miscMenu.Name + ".r-save-ally", Global.Lang.Get("Kalista_RSave")).SetValue(true));
//        }

//        protected override void SetupSpells()
//        {
//            Q = new Spell(SpellSlot.Q, 1200);
//            Q.SetSkillshot(0.35f, 40, 2350, true, SkillshotType.SkillshotLine);

//            W = new Spell(SpellSlot.W, 5000);

//            E = new Spell(SpellSlot.E, 1000);

//            R = new Spell(SpellSlot.R, 1500);
//        }

//        private void OnSpellbookCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
//        {
//            try
//            {
//                if (sender.Owner.IsMe && args.Slot == SpellSlot.Q && Player.IsDashing())
//                {
//                    args.Process = false;
//                }
//            }
//            catch (Exception ex)
//            {
//                Global.Logger.AddItem(new LogItem(ex));
//            }
//        }

//        private void OnObjAiBaseProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
//        {
//            try
//            {
//                if (sender.IsMe)
//                {
//                    if (args.SData.Name.Equals("KalistaExpungeWrapper", StringComparison.OrdinalIgnoreCase))
//                    {
//                        Utility.DelayAction.Add(250, Orbwalking.ResetAutoAttackTimer);
//                    }
//                }

//                if (!sender.IsEnemy || SoulBoundData.Unit == null || !Menu.Item(Menu.Name + ".miscellaneous.r-save-ally").GetValue<bool>())
//                    return;

//                if ((!(sender is Obj_AI_Hero) || args.SData.IsAutoAttack()) && args.Target != null &&
//                    args.Target.NetworkId == SoulBoundData.Unit.NetworkId)
//                {
//                    SoulBoundData.IncomingDamage.Add(
//                        SoulBoundData.Unit.ServerPosition.Distance(sender.ServerPosition)/args.SData.MissileSpeed + Game.Time,
//                        (float) sender.GetAutoAttackDamage(SoulBoundData.Unit));
//                }
//                else
//                {
//                    var hero = sender as Obj_AI_Hero;
//                    if (hero != null)
//                    {
//                        var attacker = hero;
//                        var slot = attacker.GetSpellSlot(args.SData.Name);

//                        if (slot != SpellSlot.Unknown)
//                        {
//                            if (slot == attacker.GetSpellSlot("SummonerDot") && args.Target != null &&
//                                args.Target.NetworkId == SoulBoundData.Unit.NetworkId)
//                            {
//                                SoulBoundData.InstantDamage.Add(Game.Time + 2,
//                                    (float) attacker.GetSummonerSpellDamage(SoulBoundData.Unit, Damage.SummonerSpell.Ignite));
//                            }
//                            else if ((slot == SpellSlot.Q || slot == SpellSlot.W || slot == SpellSlot.E || slot == SpellSlot.R) &&
//                                     ((args.Target != null && args.Target.NetworkId == SoulBoundData.Unit.NetworkId) ||
//                                      args.End.Distance(SoulBoundData.Unit.ServerPosition) < Math.Pow(args.SData.LineWidth, 2)))
//                            {
//                                SoulBoundData.InstantDamage.Add(Game.Time + 2, (float) attacker.GetSpellDamage(SoulBoundData.Unit, slot));
//                            }
//                        }
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Global.Logger.AddItem(new LogItem(ex));
//            }
//        }

//        private void OnOrbwalkingNonKillableMinion(AttackableUnit unit)
//        {
//            try
//            {
//                if (Menu.Item(Menu.Name + ".miscellaneous.e-unkillable-minion").GetValue<bool>() && E.IsReady())
//                {
//                    var target = unit as Obj_AI_Base;
//                    if (target != null && Utils.IsRendKillable(target))
//                    {
//                        E.Cast();
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Global.Logger.AddItem(new LogItem(ex));
//            }
//        }

//        private void OnCorePreUpdate(EventArgs args)
//        {
//            try
//            {
//                Orbwalker.ForceTarget(null);
//                if (Menu.Item(Menu.Name + ".miscellaneous.e-big-minion").GetValue<bool>() && E.IsReady() &&
//                    ObjectManager.Get<Obj_AI_Minion>()
//                        .Any(
//                            m =>
//                                m.IsValidTarget(E.Range) &&
//                                (m.BaseSkinName.Contains("MinionSiege") || m.BaseSkinName.Contains("Dragon") || m.BaseSkinName.Contains("Baron")) &&
//                                Utils.IsRendKillable(m)))
//                {
//                    E.Cast();
//                }
//                if (SoulBoundData.Unit == null)
//                {
//                    // ReSharper disable once StringLiteralTypo
//                    SoulBoundData.Unit = HeroManager.Allies.Find(h => h.Buffs.Any(b => b.Caster.IsMe && b.Name.Contains("kalistacoopstrikeally")));
//                }
//                else if (Menu.Item(Menu.Name + ".miscellaneous.r-save-ally").GetValue<bool>() && R.IsReady())
//                {
//                    if (SoulBoundData.Unit.HealthPercent <= 10 && SoulBoundData.Unit.CountEnemiesInRange(500) > 0 ||
//                        SoulBoundData.Damage > SoulBoundData.Unit.Health)
//                        R.Cast();
//                }
//                foreach (var entry in SoulBoundData.IncomingDamage)
//                {
//                    if (entry.Key < Game.Time)
//                        SoulBoundData.IncomingDamage.Remove(entry.Key);
//                }
//                foreach (var entry in SoulBoundData.InstantDamage)
//                {
//                    if (entry.Key < Game.Time)
//                        SoulBoundData.InstantDamage.Remove(entry.Key);
//                }
//            }
//            catch (Exception ex)
//            {
//                Global.Logger.AddItem(new LogItem(ex));
//            }
//        }

//        protected override void Combo()
//        {
//            var useQ = Menu.Item(Menu.Name + ".lane-clear.q").GetValue<bool>();
//            var useE = Menu.Item(Menu.Name + ".lane-clear.e").GetValue<bool>();

//            if (!(useQ && Q.IsReady()) && !(useE && E.IsReady()))
//            {
//                return;
//            }

//            var target = TargetSelector.GetTarget((useQ && Q.IsReady()) ? Q.Range : (E.Range*1.2f), TargetSelector.DamageType.Physical);
//            if (target != null)
//            {
//                if ((CalculateComboDamage(target, true, false, false, false, 2) + Utils.GetRendDamage(target)*1.5) > target.Health)
//                {
//                    ItemManager.UseComboItems(target);
//                    SummonerManager.UseComboSummoners(target);
//                }

//                if (useQ && !Player.IsDashing())
//                {
//                    Casting.BasicSkillShot(target, Q, HitchanceManager.Get("q"));
//                }

//                if (useE && E.IsReady() && Utils.HasRendBuff(target))
//                {
//                    if (Player.Distance(target, true) > Math.Pow(Orbwalking.GetRealAutoAttackRange(target), 2))
//                    {
//                        var minions = ObjectManager.Get<Obj_AI_Minion>().Where(m => m.IsValidTarget(Orbwalking.GetRealAutoAttackRange(m))).ToList();
//                        if (minions.Any(Utils.IsRendKillable))
//                        {
//                            E.Cast(true);
//                        }
//                        else
//                        {
//                            var minion =
//                                Utils.GetDashObjects(minions)
//                                    .Find(
//                                        m =>
//                                            m.Health > Player.GetAutoAttackDamage(m) &&
//                                            m.Health < Player.GetAutoAttackDamage(m) + Utils.GetRendDamage(m));
//                            if (minion != null)
//                            {
//                                Orbwalker.ForceTarget(minion);
//                            }
//                        }
//                    }
//                    else if (E.IsInRange(target))
//                    {
//                        if (Utils.IsRendKillable(target))
//                        {
//                            E.Cast();
//                        }
//                        else if (Utils.GetRendBuff(target).Count >= Menu.Item(Menu.Name + ".combo.min-e-stacks").GetValue<Slider>().Value)
//                        {
//                            if (target.ServerPosition.Distance(Player.ServerPosition, true) > Math.Pow(E.Range*0.8, 2) ||
//                                Utils.GetRendBuff(target).EndTime - Game.Time < 0.3)
//                            {
//                                E.Cast();
//                            }
//                        }
//                    }
//                }
//            }
//        }

//        protected override void Harass()
//        {
//            if (Menu.Item(Menu.Name + ".harass.q").GetValue<bool>() && Q.IsReady())
//            {
//                Casting.BasicSkillShot(Q, HitchanceManager.Get("q"));
//            }
//            if (Menu.Item(Menu.Name + ".harass.e-reset").GetValue<bool>() && E.IsReady())
//            {
//                var enemy = HeroManager.Enemies.Where(Utils.HasRendBuff).OrderBy(o => o.Distance(Player, true)).FirstOrDefault();
//                if (enemy != null)
//                {
//                    if (enemy.Distance(Player, true) < Math.Pow(E.Range + 200, 2))
//                    {
//                        if (ObjectManager.Get<Obj_AI_Minion>().Any(e => E.IsInRange(e) && Utils.IsRendKillable(e)))
//                        {
//                            E.Cast();
//                        }
//                    }
//                }
//            }
//        }

//        protected override void LaneClear()
//        {
//            var useQ = Menu.Item(Menu.Name + ".lane-clear.q").GetValue<bool>();
//            var useE = Menu.Item(Menu.Name + ".lane-clear.e").GetValue<bool>();

//            if (!(useQ && Q.IsReady()) && !(useE && E.IsReady()))
//            {
//                return;
//            }

//            var minions = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly);
//            if (minions.Count == 0)
//            {
//                return;
//            }

//            if (useQ && Q.IsReady() && !Player.IsDashing())
//            {
//                var minQMinions = Menu.Item(Menu.Name + ".lane-clear.min-q-minions").GetValue<Slider>().Value;
//                if (minions.Count >= minQMinions)
//                {
//                    var killable = minions.Where(m => m.Health < Q.GetDamage(m)).ToList();
//                    if (killable.Any())
//                    {
//                        var currentHitNumber = 0;
//                        var castPosition = Vector3.Zero;
//                        foreach (var target in killable)
//                        {
//                            var colliding = Q.GetPrediction(target).CollisionObjects;
//                            if (colliding.Count >= minQMinions)
//                            {
//                                var i = 0;
//                                foreach (var collide in colliding)
//                                {
//                                    if (Q.GetDamage(collide) < collide.Health)
//                                    {
//                                        if (currentHitNumber < i && i >= minQMinions)
//                                        {
//                                            currentHitNumber = i;
//                                            castPosition = Q.GetPrediction(collide).CastPosition;
//                                        }
//                                        break;
//                                    }
//                                    i++;
//                                }
//                            }
//                        }
//                        if (castPosition != Vector3.Zero)
//                        {
//                            Q.Cast(castPosition);
//                        }
//                    }
//                }
//            }
//            if (useE && E.IsReady())
//            {
//                var minEMinions = Menu.Item(Menu.Name + ".lane-clear.min-e-minions").GetValue<Slider>().Value;
//                var minionsInRange = minions.Where(m => E.IsInRange(m)).ToList();
//                if (minionsInRange.Count >= minEMinions)
//                {
//                    if (minionsInRange.Where(Utils.IsRendKillable).ToList().Count >= minEMinions)
//                    {
//                        E.Cast();
//                    }
//                }
//            }
//        }

//        protected override void Flee()
//        {
//            if (Menu.Item(Menu.Name + ".flee.autoattacks").GetValue<bool>())
//            {
//                var objs = Utils.GetDashObjects();
//                Orbwalking.Orbwalk(objs.Count > 0 ? objs.First() : null, Game.CursorPos);
//            }
//        }

//        protected override void Killsteal()
//        {
//            if (Menu.Item(Menu.Name + ".miscellaneous.e-killsteal").GetValue<bool>() && E.IsReady())
//            {
//                if (HeroManager.Enemies.Any(h => h.IsValidTarget(E.Range) && !Invulnerable.HasBuff(h) && Utils.IsRendKillable(h)))
//                {
//                    E.Cast();
//                }
//            }
//        }

//        protected override void OnDraw()
//        {
//            var q = Menu.Item(Menu.Name + ".drawing.q").GetValue<Circle>();
//            var w = Menu.Item(Menu.Name + ".drawing.w").GetValue<Circle>();
//            var e = Menu.Item(Menu.Name + ".drawing.e").GetValue<Circle>();
//            var r = Menu.Item(Menu.Name + ".drawing.r").GetValue<Circle>();
//            var circleThickness = Menu.Item(Menu.Name + ".drawing.circle-thickness").GetValue<Slider>().Value;

//            if (q.Active)
//            {
//                Render.Circle.DrawCircle(Player.Position, Q.Range, q.Color, circleThickness);
//            }
//            if (w.Active)
//            {
//                Render.Circle.DrawCircle(Player.Position, W.Range, w.Color, circleThickness);
//            }
//            if (e.Active)
//            {
//                Render.Circle.DrawCircle(Player.Position, E.Range*1.1f, e.Color, circleThickness);
//            }
//            if (r.Active)
//            {
//                Render.Circle.DrawCircle(Player.Position, R.Range, r.Color, circleThickness);
//            }
//        }

//        internal class SoulBoundData
//        {
//            public static Dictionary<float, float> IncomingDamage = new Dictionary<float, float>();
//            public static Dictionary<float, float> InstantDamage = new Dictionary<float, float>();
//            public static Obj_AI_Hero Unit { get; set; }

//            public static float Damage
//            {
//                get { return IncomingDamage.Sum(e => e.Value) + InstantDamage.Sum(e => e.Value); }
//            }
//        }

//        internal class Utils
//        {
//            private static readonly float[] RawRendDamage = {20, 30, 40, 50, 60};
//            private static readonly float[] RawRendDamageMultiplier = {0.6f, 0.6f, 0.6f, 0.6f, 0.6f};
//            private static readonly float[] RawRendDamagePerSpear = {10, 14, 19, 25, 32};
//            private static readonly float[] RawRendDamagePerSpearMultiplier = {0.2f, 0.225f, 0.25f, 0.275f, 0.3f};

//            public static bool IsRendKillable(Obj_AI_Base target)
//            {
//                return GetRendDamage(target) > target.Health;
//            }

//            public static float GetRendDamage(Obj_AI_Base target)
//            {
//                return ((float) ObjectManager.Player.CalcDamage(target, Damage.DamageType.Physical, GetRawRendDamage(target)) - 10)*0.98f;
//            }

//            public static float GetRawRendDamage(Obj_AI_Base target)
//            {
//                var buff = GetRendBuff(target);
//                var eLevel = ObjectManager.Player.GetSpell(SpellSlot.E).Level;
//                if (buff != null)
//                {
//                    return (RawRendDamage[eLevel - 1] + RawRendDamageMultiplier[eLevel - 1]*ObjectManager.Player.TotalAttackDamage()) +
//                           (buff.Count - 1)*
//                           (RawRendDamagePerSpear[eLevel - 1] + RawRendDamagePerSpearMultiplier[eLevel - 1]*ObjectManager.Player.TotalAttackDamage());
//                }
//                return 0;
//            }

//            public static bool HasRendBuff(Obj_AI_Base target)
//            {
//                return GetRendBuff(target) != null;
//            }

//            public static BuffInstance GetRendBuff(Obj_AI_Base target)
//            {
//                return target.Buffs.Find(b => b.Caster.IsMe && b.IsValidBuff() && b.DisplayName == "KalistaExpungeMarker");
//            }

//            public static List<Obj_AI_Base> GetDashObjects(IEnumerable<Obj_AI_Base> predefinedObjectList = null)
//            {
//                var objects = predefinedObjectList != null
//                    ? predefinedObjectList.ToList()
//                    : ObjectManager.Get<Obj_AI_Base>().Where(o => o.IsValidTarget(Orbwalking.GetRealAutoAttackRange(o))).ToList();
//                var apexPoint = ObjectManager.Player.ServerPosition.To2D() +
//                                (ObjectManager.Player.ServerPosition.To2D() - Game.CursorPos.To2D()).Normalized()*
//                                Orbwalking.GetRealAutoAttackRange(ObjectManager.Player);
//                return
//                    objects.Where(o => IsLyingInCone(o.ServerPosition.To2D(), apexPoint, ObjectManager.Player.ServerPosition.To2D(), Math.PI))
//                        .OrderBy(o => o.Distance(apexPoint, true))
//                        .ToList();
//            }

//            public static bool IsLyingInCone(Vector2 position, Vector2 apexPoint, Vector2 circleCenter, double aperture)
//            {
//                var halfAperture = aperture/2;
//                var apexToXVector = apexPoint - position;
//                var axisVector = apexPoint - circleCenter;
//                var isInInfiniteCone = DotProd(apexToXVector, axisVector)/Magn(apexToXVector)/Magn(axisVector) > Math.Cos(halfAperture);
//                return isInInfiniteCone && DotProd(apexToXVector, axisVector)/Magn(axisVector) < Magn(axisVector);
//            }

//            private static float DotProd(Vector2 a, Vector2 b)
//            {
//                return a.X*b.X + a.Y*b.Y;
//            }

//            private static float Magn(Vector2 a)
//            {
//                return (float) (Math.Sqrt(a.X*a.X + a.Y*a.Y));
//            }
//        }
//    }
//}
