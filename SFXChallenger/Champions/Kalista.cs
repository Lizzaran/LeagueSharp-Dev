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

#region

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXChallenger.Abstracts;
using SFXChallenger.Enumerations;
using SFXChallenger.Helpers;
using SFXChallenger.Managers;
using SFXLibrary;
using SFXLibrary.Extensions.NET;
using SFXLibrary.Logger;
using SharpDX;
using Collision = LeagueSharp.Common.Collision;
using Orbwalking = SFXChallenger.Wrappers.Orbwalking;
using TargetSelector = SFXChallenger.Wrappers.TargetSelector;
using Utils = SFXChallenger.Helpers.Utils;

#endregion

namespace SFXChallenger.Champions
{
    internal class Kalista : Champion
    {
        protected override ItemFlags ItemFlags
        {
            get { return ItemFlags.Offensive | ItemFlags.Defensive | ItemFlags.Flee; }
        }

        protected override void OnLoad()
        {
            Obj_AI_Base.OnBuffAdd += OnObjAiBaseBuffAdd;
            Obj_AI_Base.OnProcessSpellCast += OnObjAiBaseProcessSpellCast;
            Spellbook.OnCastSpell += OnSpellbookCastSpell;
            Orbwalking.OnNonKillableMinion += OnOrbwalkingNonKillableMinion;
            Orbwalking.AfterAttack += OnOrbwalkingAfterAttack;
            Core.OnPreUpdate += OnCorePreUpdate;
        }

        protected override void OnUnload()
        {
            Obj_AI_Base.OnBuffAdd -= OnObjAiBaseBuffAdd;
            Obj_AI_Base.OnProcessSpellCast -= OnObjAiBaseProcessSpellCast;
            Spellbook.OnCastSpell -= OnSpellbookCastSpell;
            Orbwalking.OnNonKillableMinion -= OnOrbwalkingNonKillableMinion;
            Orbwalking.AfterAttack -= OnOrbwalkingAfterAttack;
            Core.OnPreUpdate -= OnCorePreUpdate;
        }

        protected override void AddToMenu()
        {
            var comboMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Combo"), Menu.Name + ".combo"));
            HitchanceManager.AddToMenu(
                comboMenu.AddSubMenu(new Menu(Global.Lang.Get("F_MH"), comboMenu.Name + ".hitchance")), "combo",
                new Dictionary<string, int> { { "Q", 2 } });
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));
            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(true));
            comboMenu.AddItem(
                new MenuItem(comboMenu.Name + ".e-min", "E " + Global.Lang.Get("G_Min")).SetValue(new Slider(10, 1, 20)));

            var harassMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Harass"), Menu.Name + ".harass"));
            HitchanceManager.AddToMenu(
                harassMenu.AddSubMenu(new Menu(Global.Lang.Get("F_MH"), harassMenu.Name + ".hitchance")), "harass",
                new Dictionary<string, int> { { "Q", 2 } });
            ManaManager.AddToMenu(harassMenu, "harass", ManaCheckType.Minimum, ManaValueType.Percent);
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));
            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(true));
            harassMenu.AddItem(
                new MenuItem(harassMenu.Name + ".e-min", "E " + Global.Lang.Get("G_Min")).SetValue(new Slider(5, 1, 15)));

            var laneclearMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_LaneClear"), Menu.Name + ".lane-clear"));
            ManaManager.AddToMenu(laneclearMenu, "lane-clear", ManaCheckType.Minimum, ManaValueType.Percent);
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));
            laneclearMenu.AddItem(
                new MenuItem(laneclearMenu.Name + ".q-min", "Q " + Global.Lang.Get("G_Min")).SetValue(
                    new Slider(3, 1, 5)));
            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(false));
            laneclearMenu.AddItem(
                new MenuItem(laneclearMenu.Name + ".e-min", "E " + Global.Lang.Get("G_Min")).SetValue(
                    new Slider(2, 1, 5)));

            var lasthitMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_LastHit"), Menu.Name + ".lasthit"));
            ManaManager.AddToMenu(lasthitMenu, "lasthit", ManaCheckType.Minimum, ManaValueType.Percent);
            lasthitMenu.AddItem(
                new MenuItem(lasthitMenu.Name + ".e-big", "E " + Global.Lang.Get("G_Big")).SetValue(true));
            lasthitMenu.AddItem(
                new MenuItem(lasthitMenu.Name + ".e-unkillable", "E " + Global.Lang.Get("G_Unkillable")).SetValue(true));

            var killstealMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Killsteal"), Menu.Name + ".killsteal"));
            killstealMenu.AddItem(new MenuItem(killstealMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(true));

            var fleeMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Flee"), Menu.Name + ".flee"));
            fleeMenu.AddItem(new MenuItem(fleeMenu.Name + ".aa", Global.Lang.Get("G_UseAutoAttacks")).SetValue(true));

            var miscMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Miscellaneous"), Menu.Name + ".miscellaneous"));
            ManaManager.AddToMenu(miscMenu, "misc", ManaCheckType.Minimum, ManaValueType.Percent);
            miscMenu.AddItem(
                new MenuItem(miscMenu.Name + ".e-reset", Global.Lang.Get("Kalista_EHarassReset")).SetValue(true));
            miscMenu.AddItem(new MenuItem(miscMenu.Name + ".r", Global.Lang.Get("G_UseR")).SetValue(true));

            DrawingManager.Add("E Damage", new Circle(true, DamageIndicator.DrawingColor)).ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs args)
                {
                    DamageIndicator.Enabled = args.GetNewValue<Circle>().Active;
                    DamageIndicator.DrawingColor = args.GetNewValue<Circle>().Color;
                };

            DamageIndicator.Initialize(Rend.GetDamage);
            DamageIndicator.Enabled = DrawingManager.Get<Circle>("E Damage").Active;
            DamageIndicator.DrawingColor = DrawingManager.Get<Circle>("E Damage").Color;
        }

        protected override void SetupSpells()
        {
            Q = new Spell(SpellSlot.Q, 1200f);
            Q.SetSkillshot(0.35f, 40f, 2350f, true, SkillshotType.SkillshotLine);

            W = new Spell(SpellSlot.W, 5000f);

            E = new Spell(SpellSlot.E, 1000f);

            R = new Spell(SpellSlot.R, 1500f);
        }

        private void OnObjAiBaseBuffAdd(Obj_AI_Base sender, Obj_AI_BaseBuffAddEventArgs args)
        {
            try
            {
                if (sender.IsAlly && args.Buff.Name.Equals("kalistapaltarbuff", StringComparison.OrdinalIgnoreCase))
                {
                    var hero = sender as Obj_AI_Hero;
                    if (hero != null)
                    {
                        SoulBound.Unit = hero;
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnSpellbookCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            try
            {
                if (sender.Owner.IsMe && args.Slot == SpellSlot.Q && Player.IsDashing())
                {
                    args.Process = false;
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnObjAiBaseProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            try
            {
                if (sender.IsMe)
                {
                    if (args.SData.Name == "KalistaExpungeWrapper")
                    {
                        Orbwalking.ResetAutoAttackTimer();
                    }
                }
                if (!sender.IsEnemy || SoulBound.Unit == null || R.Level == 0 ||
                    !Menu.Item(Menu.Name + ".miscellaneous.r").GetValue<bool>())
                {
                    return;
                }
                if (args.Target != null && args.Target.NetworkId == SoulBound.Unit.NetworkId &&
                    (!(sender is Obj_AI_Hero) || args.SData.IsAutoAttack()))
                {
                    var time = SoulBound.Unit.ServerPosition.Distance(sender.ServerPosition) / args.SData.MissileSpeed +
                               Game.Time;
                    float val;
                    if (SoulBound.IncomingDamage.TryGetValue(time, out val))
                    {
                        SoulBound.IncomingDamage.TryUpdate(
                            time, val + (float) sender.GetAutoAttackDamage(SoulBound.Unit), val);
                    }
                    else
                    {
                        SoulBound.IncomingDamage.TryAdd(time, (float) sender.GetAutoAttackDamage(SoulBound.Unit));
                    }
                }
                else
                {
                    var hero = sender as Obj_AI_Hero;
                    if (hero != null)
                    {
                        var slot = hero.GetSpellSlot(args.SData.Name);
                        if (slot != SpellSlot.Unknown)
                        {
                            var damage = 0f;
                            if (args.Target != null && args.Target.NetworkId == SoulBound.Unit.NetworkId &&
                                slot == hero.GetSpellSlot("SummonerDot"))
                            {
                                damage =
                                    (float) hero.GetSummonerSpellDamage(SoulBound.Unit, Damage.SummonerSpell.Ignite);
                            }
                            else if ((slot == SpellSlot.Q || slot == SpellSlot.W || slot == SpellSlot.E ||
                                      slot == SpellSlot.R) &&
                                     ((args.Target != null && args.Target.NetworkId == SoulBound.Unit.NetworkId) ||
                                      args.End.Distance(SoulBound.Unit.ServerPosition) <
                                      Math.Pow(args.SData.LineWidth, 2)))
                            {
                                damage = (float) hero.GetSpellDamage(SoulBound.Unit, slot);
                            }
                            var time = Game.Time + 2;
                            float val;
                            if (SoulBound.InstantDamage.TryGetValue(time, out val))
                            {
                                SoulBound.InstantDamage.TryUpdate(time, val + damage, val);
                            }
                            else
                            {
                                SoulBound.InstantDamage.TryAdd(time, damage);
                            }
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
            if (unit.IsMe)
            {
                var forced = Orbwalker.ForcedTarget();
                if (forced != null && target.NetworkId == forced.NetworkId)
                {
                    Orbwalker.ForceTarget(null);
                }
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

        private void OnOrbwalkingNonKillableMinion(AttackableUnit unit)
        {
            try
            {
                if (Menu.Item(Menu.Name + ".lasthit.e-unkillable").GetValue<bool>() && E.IsReady() &&
                    ManaManager.Check("lasthit"))
                {
                    var target = unit as Obj_AI_Base;
                    if (target != null && Rend.IsKillable(target))
                    {
                        E.Cast();
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnCorePreUpdate(EventArgs args)
        {
            try
            {
                if (Menu.Item(Menu.Name + ".lasthit.e-big").GetValue<bool>() && E.IsReady() &&
                    ManaManager.Check("lasthit"))
                {
                    if (
                        ObjectCache.GetMinions()
                            .Any(
                                m =>
                                    m.IsValidTarget(E.Range) &&
                                    (m.BaseSkinName.Contains("MinionSiege") || m.BaseSkinName.StartsWith("SRU_Dragon") ||
                                     m.BaseSkinName.StartsWith("SRU_Baron") || m.BaseSkinName.StartsWith("SRU_Red") ||
                                     m.BaseSkinName.StartsWith("SRU_Blue")) && Rend.IsKillable(m)))

                    {
                        E.Cast();
                    }
                }

                if (Menu.Item(Menu.Name + ".miscellaneous.e-reset").GetValue<bool>() && E.IsReady() &&
                    Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo && ManaManager.Check("misc") &&
                    HeroManager.Enemies.Any(e => Rend.HasBuff(e) && E.IsInRange(e)))
                {
                    if (ObjectCache.GetMinions().Any(e => E.IsInRange(e) && Rend.IsKillable(e)))
                    {
                        E.Cast();
                    }
                }

                if (R.Level > 0)
                {
                    if (Menu.Item(Menu.Name + ".miscellaneous.r").GetValue<bool>() && SoulBound.Unit != null &&
                        R.IsReady())
                    {
                        if (SoulBound.Unit.HealthPercent <= 10 && SoulBound.Unit.CountEnemiesInRange(500) > 0 ||
                            SoulBound.TotalDamage * 1.1f > SoulBound.Unit.Health)
                        {
                            R.Cast();
                        }
                    }
                }
                float old;
                foreach (var entry in SoulBound.IncomingDamage.Where(entry => entry.Key < Game.Time))
                {
                    SoulBound.IncomingDamage.TryRemove(entry.Key, out old);
                }
                foreach (var entry in SoulBound.InstantDamage.Where(entry => entry.Key < Game.Time))
                {
                    SoulBound.InstantDamage.TryRemove(entry.Key, out old);
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
            var useE = Menu.Item(Menu.Name + ".combo.e").GetValue<bool>() && E.IsReady();


            if (useQ)
            {
                Casting.SkillShot(Q, Q.GetHitChance("combo"));
            }

            if (useE)
            {
                var target = TargetSelector.GetTarget(
                    E.Range * 1.2f, LeagueSharp.Common.TargetSelector.DamageType.Physical);
                if (target != null && Rend.HasBuff(target))
                {
                    if (target.Distance(Player) > Orbwalking.GetRealAutoAttackRange(target))
                    {
                        var minions =
                            ObjectCache.GetMinions()
                                .Where(m => m.IsValidTarget(Orbwalking.GetRealAutoAttackRange(m)))
                                .ToList();
                        if (minions.Any(Rend.IsKillable))
                        {
                            E.Cast();
                        }
                        else
                        {
                            var minion =
                                GetDashObjects(minions)
                                    .Find(
                                        m =>
                                            m.Health > Player.GetAutoAttackDamage(m) &&
                                            m.Health <
                                            Player.GetAutoAttackDamage(m) +
                                            Rend.GetDamage(m, (Rend.HasBuff(m) ? Rend.GetBuff(m).Count + 1 : 1)));
                            if (minion != null)
                            {
                                Orbwalker.ForceTarget(minion);
                            }
                        }
                    }
                    else if (E.IsInRange(target))
                    {
                        if (Rend.IsKillable(target))
                        {
                            E.Cast();
                        }
                        else
                        {
                            var buff = Rend.GetBuff(target);
                            if (buff != null &&
                                buff.Count >= Menu.Item(Menu.Name + ".combo.e-min").GetValue<Slider>().Value)
                            {
                                if (target.Distance(Player) > E.Range * 0.8 || buff.EndTime - Game.Time < 0.3)
                                {
                                    E.Cast();
                                }
                            }
                        }
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
            if (Menu.Item(Menu.Name + ".harass.e").GetValue<bool>() && E.IsReady())
            {
                foreach (var enemy in HeroManager.Enemies.Where(e => E.IsInRange(e)))
                {
                    if (Rend.IsKillable(enemy))
                    {
                        E.Cast();
                    }
                    else
                    {
                        var buff = Rend.GetBuff(enemy);
                        if (buff != null &&
                            buff.Count >= Menu.Item(Menu.Name + ".harass.e-min").GetValue<Slider>().Value)
                        {
                            if (enemy.Distance(Player) > E.Range * 0.8 || buff.EndTime - Game.Time < 0.3)
                            {
                                E.Cast();
                            }
                        }
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

            var useQ = Menu.Item(Menu.Name + ".lane-clear.q").GetValue<bool>() && Q.IsReady();
            var useE = Menu.Item(Menu.Name + ".lane-clear.e").GetValue<bool>() && E.IsReady();
            var minQ = Menu.Item(Menu.Name + ".lane-clear.q-min").GetValue<Slider>().Value;
            var minE = Menu.Item(Menu.Name + ".lane-clear.e-min").GetValue<Slider>().Value;

            if (!useQ && !useE)
            {
                return;
            }

            var minions = ObjectCache.GetMinions(
                Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
            if (minions.Count == 0)
            {
                return;
            }

            if (useQ && !Player.IsDashing())
            {
                if (minions.Count >= minQ)
                {
                    var killable = minions.Where(m => m.Health < Q.GetDamage(m)).ToList();
                    if (killable.Any())
                    {
                        var input = new PredictionInput
                        {
                            From = Q.From,
                            Collision = Q.Collision,
                            Delay = Q.Delay,
                            Radius = Q.Width,
                            Range = Q.Range,
                            RangeCheckFrom = Q.RangeCheckFrom,
                            Speed = Q.Speed,
                            Type = Q.Type,
                            CollisionObjects =
                                new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                }
                        };
                        var currentHitNumber = 0;
                        var castPosition = Vector3.Zero;
                        foreach (var target in killable)
                        {
                            input.Unit = target;
                            var colliding =
                                Collision.GetCollision(
                                    new List<Vector3>
                                    {
                                        Player.ServerPosition.Extend(
                                            Prediction.GetPrediction(input).UnitPosition, Q.Range)
                                    }, input)
                                    .DistinctBy(e => e.NetworkId)
                                    .OrderBy(e => e.Distance(Player))
                                    .ToList();

                            if (colliding.Count >= minQ && !colliding.Contains(Player))
                            {
                                var i = 0;
                                foreach (var collide in colliding)
                                {
                                    if (Q.GetDamage(collide) < collide.Health)
                                    {
                                        if (currentHitNumber < i && i >= minQ)
                                        {
                                            currentHitNumber = i;
                                            castPosition = Q.GetPrediction(collide).CastPosition;
                                        }
                                        break;
                                    }
                                    i++;
                                }
                            }
                        }
                        if (castPosition != Vector3.Zero)
                        {
                            if (Q.Cast(castPosition))
                            {
                                return;
                            }
                        }
                    }
                }
            }
            if (useE)
            {
                var minionsInRange = minions.Where(m => E.IsInRange(m)).ToList();
                if (minionsInRange.Count >= minE)
                {
                    var killCount = minionsInRange.Count(Rend.IsKillable);
                    if (killCount >= minE)
                    {
                        E.Cast();
                    }
                }
            }
        }

        protected override void Flee()
        {
            if (Menu.Item(Menu.Name + ".flee.aa").GetValue<bool>())
            {
                var dashObjects = GetDashObjects();
                if (dashObjects.Count > 0)
                {
                    Orbwalking.Orbwalk(dashObjects.First(), Game.CursorPos);
                }
            }
        }

        protected override void Killsteal()
        {
            if (Menu.Item(Menu.Name + ".killsteal.e").GetValue<bool>() && E.IsReady() &&
                HeroManager.Enemies.Any(h => h.IsValidTarget(E.Range) && Rend.IsKillable(h)))
            {
                E.Cast();
            }
        }

        public static List<Obj_AI_Base> GetDashObjects(IEnumerable<Obj_AI_Base> predefinedObjectList = null)
        {
            try
            {
                var objects = predefinedObjectList != null
                    ? predefinedObjectList.ToList()
                    : ObjectManager.Get<Obj_AI_Base>()
                        .Where(o => o.IsValidTarget(Orbwalking.GetRealAutoAttackRange(o)))
                        .ToList();
                var apexPoint = ObjectManager.Player.ServerPosition.To2D() +
                                (ObjectManager.Player.ServerPosition.To2D() - Game.CursorPos.To2D()).Normalized() *
                                Orbwalking.GetRealAutoAttackRange(ObjectManager.Player);
                return
                    objects.Where(
                        o =>
                            Utils.IsLyingInCone(
                                o.ServerPosition.To2D(), apexPoint, ObjectManager.Player.ServerPosition.To2D(), Math.PI))
                        .OrderBy(o => o.Distance(apexPoint, true))
                        .ToList();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return new List<Obj_AI_Base>();
        }

        internal class SoulBound
        {
            public static ConcurrentDictionary<float, float> IncomingDamage = new ConcurrentDictionary<float, float>();
            public static ConcurrentDictionary<float, float> InstantDamage = new ConcurrentDictionary<float, float>();
            public static Obj_AI_Hero Unit { get; set; }

            public static float TotalDamage
            {
                get { return IncomingDamage.Sum(e => e.Value) + InstantDamage.Sum(e => e.Value); }
            }
        }

        internal class Rend
        {
            private static readonly float[] Damage = { 20, 30, 40, 50, 60 };
            private static readonly float[] DamageMultiplier = { 0.6f, 0.6f, 0.6f, 0.6f, 0.6f };
            private static readonly float[] DamagePerSpear = { 10, 14, 19, 25, 32 };
            private static readonly float[] DamagePerSpearMultiplier = { 0.2f, 0.225f, 0.25f, 0.275f, 0.3f };

            public static bool IsKillable(Obj_AI_Base target)
            {
                return GetDamage(target) > target.Health + target.AttackShield;
            }

            public static float GetDamage(Obj_AI_Hero target)
            {
                return GetDamage(target, -1);
            }

            public static float GetDamage(Obj_AI_Base target, int customStacks = -1)
            {
                return
                    ((float)
                        ObjectManager.Player.CalcDamage(
                            target, LeagueSharp.Common.Damage.DamageType.Physical, GetRawDamage(target, customStacks)) -
                     20) * 0.98f;
            }

            public static float GetRawDamage(Obj_AI_Base target, int customStacks = -1)
            {
                try
                {
                    var buff = GetBuff(target);
                    var eLevel = ObjectManager.Player.GetSpell(SpellSlot.E).Level;
                    if (buff != null || customStacks > -1)
                    {
                        var damage = (Damage[eLevel - 1] +
                                      DamageMultiplier[eLevel - 1] * ObjectManager.Player.TotalAttackDamage()) +
                                     ((customStacks < 0 && buff != null ? buff.Count : customStacks) - 1) *
                                     (DamagePerSpear[eLevel - 1] +
                                      DamagePerSpearMultiplier[eLevel - 1] * ObjectManager.Player.TotalAttackDamage());
                        if (ObjectManager.Player.HasBuff("summonerexhaust"))
                        {
                            damage *= 0.7f;
                        }
                        return damage;
                    }
                }
                catch (Exception ex)
                {
                    Global.Logger.AddItem(new LogItem(ex));
                }
                return 0f;
            }

            public static bool HasBuff(Obj_AI_Base target)
            {
                return GetBuff(target) != null;
            }

            public static BuffInstance GetBuff(Obj_AI_Base target)
            {
                return
                    target.Buffs.Find(b => b.Caster.IsMe && b.IsValidBuff() && b.DisplayName == "KalistaExpungeMarker");
            }
        }
    }
}