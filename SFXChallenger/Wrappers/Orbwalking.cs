#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Orbwalking.cs is part of SFXChallenger.

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
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXLibrary.Extensions.NET;
using SFXLibrary.Extensions.SharpDX;
using SFXLibrary.Logger;
using SharpDX;
using Color = System.Drawing.Color;

#endregion

namespace SFXChallenger.Wrappers
{
    /// <summary>
    ///     This class offers everything related to auto-attacks and orbwalking.
    /// </summary>
    public static class Orbwalking
    {
        public delegate void AfterAttackEvenH(AttackableUnit unit, AttackableUnit target);

        public delegate void BeforeAttackEvenH(BeforeAttackEventArgs args);

        public delegate void OnAttackEvenH(AttackableUnit unit, AttackableUnit target);

        public delegate void OnNonKillableMinionH(AttackableUnit minion);

        public delegate void OnTargetChangeH(AttackableUnit oldTarget, AttackableUnit newTarget);

        public enum OrbwalkingMode
        {
            Combo,
            Mixed,
            Harass,
            LaneClear,
            LastHit,
            Flee,
            None
        }

        // ReSharper disable once InconsistentNaming
        public static int LastAATick;
        public static bool Attack = true;
        public static bool DisableNextAttack;
        public static bool Move = true;
        public static int LastMoveCommandT;
        public static Vector3 LastMoveCommandPosition = Vector3.Zero;
        private static AttackableUnit _lastTarget;
        private static readonly Obj_AI_Hero Player;
        private static int _delay = 80;
        private static float _minDistance = 400;
        private static readonly Random Random = new Random(DateTime.Now.Millisecond);

        static Orbwalking()
        {
            Player = ObjectManager.Player;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpell;
            Spellbook.OnStopCast += SpellbookOnStopCast;
        }

        private static int TickCount
        {
            get { return (int) (Game.ClockTime * 1000); }
        }

        /// <summary>
        ///     This event is fired before the player auto attacks.
        /// </summary>
        public static event BeforeAttackEvenH BeforeAttack;

        /// <summary>
        ///     This event is fired when a unit is about to auto-attack another unit.
        /// </summary>
        public static event OnAttackEvenH OnAttack;

        /// <summary>
        ///     This event is fired after a unit finishes auto-attacking another unit (Only works with player for now).
        /// </summary>
        public static event AfterAttackEvenH AfterAttack;

        /// <summary>
        ///     Gets called on target changes
        /// </summary>
        public static event OnTargetChangeH OnTargetChange;

        //  <summary>
        //      Gets called if you can't kill a minion with auto attacks
        //  </summary>
        public static event OnNonKillableMinionH OnNonKillableMinion;

        private static void FireBeforeAttack(AttackableUnit target)
        {
            if (BeforeAttack != null)
            {
                BeforeAttack(new BeforeAttackEventArgs { Target = target });
            }
            else
            {
                DisableNextAttack = false;
            }
        }

        private static void FireOnAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (OnAttack != null)
            {
                OnAttack(unit, target);
            }
        }

        private static void FireAfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (AfterAttack != null && target.IsValidTarget())
            {
                AfterAttack(unit, target);
            }
        }

        private static void FireOnTargetSwitch(AttackableUnit newTarget)
        {
            if (OnTargetChange != null && (!_lastTarget.IsValidTarget() || _lastTarget != newTarget))
            {
                OnTargetChange(_lastTarget, newTarget);
            }
        }

        private static void FireOnNonKillableMinion(AttackableUnit minion)
        {
            if (OnNonKillableMinion != null)
            {
                OnNonKillableMinion(minion);
            }
        }

        /// <summary>
        ///     Returns true if the spellname resets the attack timer.
        /// </summary>
        public static bool IsAutoAttackReset(string name)
        {
            return Enumerable.Contains(AttackResets, name.ToLower());
        }

        /// <summary>
        ///     Returns true if the unit is melee
        /// </summary>
        public static bool IsMelee(this Obj_AI_Base unit)
        {
            return unit.CombatType == GameObjectCombatType.Melee;
        }

        public static void SetMovementDelay(int delay)
        {
            _delay = delay;
        }

        /// <summary>
        ///     Returns true if the spellname is an auto-attack.
        /// </summary>
        public static bool IsAutoAttack(string name)
        {
            return (name.ToLower().Contains("attack") && !Enumerable.Contains(NoAttacks, name.ToLower())) ||
                   Enumerable.Contains(Attacks, name.ToLower());
        }

        /// <summary>
        ///     Returns the auto-attack range.
        /// </summary>
        public static float GetRealAutoAttackRange(AttackableUnit target)
        {
            var result = Player.AttackRange + Player.BoundingRadius;
            if (target.IsValidTarget())
            {
                return result + target.BoundingRadius;
            }
            return result;
        }

        /// <summary>
        ///     Returns true if the target is in auto-attack range.
        /// </summary>
        public static bool InAutoAttackRange(AttackableUnit target)
        {
            if (!target.IsValidTarget())
            {
                return false;
            }
            var myRange = GetRealAutoAttackRange(target);
            return
                Vector2.DistanceSquared(
                    (target is Obj_AI_Base) ? ((Obj_AI_Base) target).ServerPosition.To2D() : target.Position.To2D(),
                    Player.ServerPosition.To2D()) <= myRange * myRange;
        }

        /// <summary>
        ///     Returns player auto-attack missile speed.
        /// </summary>
        public static float GetMyProjectileSpeed()
        {
            return IsMelee(Player) || Player.ChampionName == "Azir" ? float.MaxValue : Player.BasicAttack.MissileSpeed;
        }

        /// <summary>
        ///     Returns if the player's auto-attack is ready.
        /// </summary>
        public static bool CanAttack()
        {
            if (LastAATick <= TickCount)
            {
                return TickCount + Game.Ping / 2 + 25 >= LastAATick + Player.AttackDelay * 1000 && Attack;
            }

            return false;
        }

        /// <summary>
        ///     Returns true if moving won't cancel the auto-attack.
        /// </summary>
        public static bool CanMove(float extraWindup)
        {
            if (!Move)
            {
                return false;
            }

            if (LastAATick <= TickCount)
            {
                return Move && Enumerable.Contains(NoCancelChamps, Player.ChampionName)
                    ? (TickCount - LastAATick > 250)
                    : (TickCount + Game.Ping / 2 >= LastAATick + Player.AttackCastDelay * 1000 + extraWindup);
            }

            return false;
        }

        public static void SetMinimumOrbwalkDistance(float d)
        {
            _minDistance = d;
        }

        public static float GetLastMoveTime()
        {
            return LastMoveCommandT;
        }

        public static Vector3 GetLastMovePosition()
        {
            return LastMoveCommandPosition;
        }

        public static void MoveTo(Vector3 position,
            float holdAreaRadius = 0,
            bool overrideTimer = false,
            bool useFixedDistance = true,
            bool randomizeMinDistance = true)
        {
            if (TickCount - LastMoveCommandT < _delay && !overrideTimer)
            {
                return;
            }

            LastMoveCommandT = TickCount;

            if (Player.ServerPosition.Distance(position, true) < holdAreaRadius * holdAreaRadius)
            {
                if (Player.Path.Length > 1)
                {
                    Player.IssueOrder((GameObjectOrder) 10, Player.ServerPosition);
                    Player.IssueOrder(GameObjectOrder.HoldPosition, Player.ServerPosition);
                    LastMoveCommandPosition = Player.ServerPosition;
                }
                return;
            }

            var point = position;
            if (useFixedDistance)
            {
                point = Player.ServerPosition +
                        (randomizeMinDistance ? (Random.NextFloat(0.6f, 1) + 0.2f) * _minDistance : _minDistance) *
                        (position.To2D() - Player.ServerPosition.To2D()).Normalized().To3D();
            }
            else
            {
                if (randomizeMinDistance)
                {
                    point = Player.ServerPosition +
                            (Random.NextFloat(0.6f, 1) + 0.2f) * _minDistance *
                            (position.To2D() - Player.ServerPosition.To2D()).Normalized().To3D();
                }
                else if (Player.ServerPosition.Distance(position) > _minDistance)
                {
                    point = Player.ServerPosition +
                            _minDistance * (position.To2D() - Player.ServerPosition.To2D()).Normalized().To3D();
                }
            }

            Player.IssueOrder(GameObjectOrder.MoveTo, point);
            LastMoveCommandPosition = point;
        }

        /// <summary>
        ///     Orbwalk a target while moving to Position.
        /// </summary>
        public static void Orbwalk(AttackableUnit target,
            Vector3 position,
            float extraWindup = 90,
            float holdAreaRadius = 0,
            bool useFixedDistance = true,
            bool randomizeMinDistance = true)
        {
            try
            {
                if (target.IsValidTarget() && CanAttack())
                {
                    DisableNextAttack = false;
                    FireBeforeAttack(target);

                    if (!DisableNextAttack)
                    {
                        Player.IssueOrder(GameObjectOrder.AttackUnit, target);

                        if (_lastTarget != null && _lastTarget.IsValid && _lastTarget != target)
                        {
                            LastAATick = TickCount + Game.Ping / 2;
                        }

                        _lastTarget = target;
                        return;
                    }
                }

                if (CanMove(extraWindup))
                {
                    MoveTo(position, holdAreaRadius, false, useFixedDistance, randomizeMinDistance);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        /// <summary>
        ///     Resets the Auto-Attack timer.
        /// </summary>
        public static void ResetAutoAttackTimer()
        {
            LastAATick = 0;
        }

        private static void SpellbookOnStopCast(Spellbook spellbook, SpellbookStopCastEventArgs args)
        {
            if (spellbook.Owner.IsValid && spellbook.Owner.IsMe && args.DestroyMissile && args.StopAnimation)
            {
                ResetAutoAttackTimer();
            }
        }

        private static void OnProcessSpell(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs spell)
        {
            try
            {
                var spellName = spell.SData.Name;

                if (IsAutoAttackReset(spellName) && unit.IsMe)
                {
                    Utility.DelayAction.Add(250, ResetAutoAttackTimer);
                }

                if (!IsAutoAttack(spellName))
                {
                    return;
                }

                if (unit.IsMe &&
                    (spell.Target is Obj_AI_Base || spell.Target is Obj_BarracksDampener || spell.Target is Obj_HQ))
                {
                    LastAATick = TickCount - Game.Ping / 2;

                    var target = spell.Target as Obj_AI_Base;
                    if (target != null)
                    {
                        if (target.IsValid)
                        {
                            FireOnTargetSwitch(target);
                            _lastTarget = target;
                        }

                        //Trigger it for ranged until the missiles catch normal attacks again!
                        Utility.DelayAction.Add(
                            (int) (unit.AttackCastDelay * 1000 + 40), () => FireAfterAttack(unit, _lastTarget));
                    }
                }

                FireOnAttack(unit, _lastTarget);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public class BeforeAttackEventArgs
        {
            private bool _process = true;
            public AttackableUnit Target;
            public Obj_AI_Base Unit = ObjectManager.Player;

            public bool Process
            {
                get { return _process; }
                set
                {
                    DisableNextAttack = !value;
                    _process = value;
                }
            }
        }

        /// <summary>
        ///     This class allows you to add an instance of "Orbwalker" to your assembly in order to control the orbwalking in an
        ///     easy way.
        /// </summary>
        public class Orbwalker
        {
            private const float LaneClearWaitTimeMod = 2f;
            private static Menu _menu;
            private Obj_AI_Base _forcedTarget;
            private OrbwalkingMode _mode = OrbwalkingMode.None;
            private Vector3 _orbwalkingPoint;
            private Obj_AI_Minion _prevMinion;

            public Orbwalker(Menu attachToMenu)
            {
                _menu = attachToMenu;
                /* Drawings submenu */
                var drawings = new Menu(Global.Lang.Get("G_Drawing"), _menu.Name + ".drawing");
                drawings.AddItem(
                    new MenuItem(drawings.Name + ".aa-range", Global.Lang.Get("Orbwalker_AttackRange")).SetValue(
                        new Circle(true, Color.FromArgb(255, 255, 0, 255))));
                drawings.AddItem(
                    new MenuItem(drawings.Name + ".aa-enemy-range", Global.Lang.Get("Orbwalker_EnemyAttackRange"))
                        .SetValue(new Circle(false, Color.FromArgb(255, 255, 0, 255))));
                drawings.AddItem(
                    new MenuItem(drawings.Name + ".hold-zone", Global.Lang.Get("Orbwalker_HoldZone")).SetValue(
                        new Circle(false, Color.FromArgb(255, 255, 0, 255))));
                drawings.AddItem(
                    new MenuItem(drawings.Name + ".circle-thickness", Global.Lang.Get("G_CircleThickness")).SetValue(
                        new Slider(5, 1, 15)));
                _menu.AddSubMenu(drawings);

                /* Misc options */
                var misc = new Menu(Global.Lang.Get("G_Miscellaneous"), _menu.Name + ".miscellaneous");
                misc.AddItem(
                    new MenuItem(misc.Name + ".extra-windup-time", Global.Lang.Get("Orbwalker_ExtraWindupTime"))
                        .SetValue(new Slider(80, 0, 200)));
                misc.AddItem(
                    new MenuItem(misc.Name + ".farm-delay", Global.Lang.Get("Orbwalker_FarmDelay")).SetShared()
                        .SetValue(new Slider(0, 0, 200)));
                misc.AddItem(
                    new MenuItem(misc.Name + ".movement-delay", Global.Lang.Get("Orbwalker_MovementDelay")).SetShared()
                        .SetValue(new Slider(80, 0, 250)));

                misc.AddItem(
                    new MenuItem(misc.Name + ".hold-position-radius", Global.Lang.Get("Orbwalker_HoldPositionRadius"))
                        .SetValue(new Slider(0, 0, 250)));
                misc.AddItem(
                    new MenuItem(misc.Name + ".prioritize-lasthit", Global.Lang.Get("Orbwalker_PrioritizeLastHit"))
                        .SetValue(true));
                _menu.AddSubMenu(misc);

                /*Load the menu*/
                _menu.AddItem(
                    new MenuItem(_menu.Name + ".flee", Global.Lang.Get("Orbwalker_Flee")).SetValue(
                        new KeyBind('G', KeyBindType.Press)));
                _menu.AddItem(
                    new MenuItem(_menu.Name + ".lasthit", Global.Lang.Get("Orbwalker_LastHit")).SetValue(
                        new KeyBind('X', KeyBindType.Press)));
                _menu.AddItem(
                    new MenuItem(_menu.Name + ".laneclear", Global.Lang.Get("Orbwalker_LaneClear")).SetValue(
                        new KeyBind('V', KeyBindType.Press)));
                _menu.AddItem(
                    new MenuItem(_menu.Name + ".harass", Global.Lang.Get("Orbwalker_Harass")).SetValue(
                        new KeyBind('T', KeyBindType.Press)));
                _menu.AddItem(
                    new MenuItem(_menu.Name + ".mixed", Global.Lang.Get("Orbwalker_Mixed")).SetValue(
                        new KeyBind('C', KeyBindType.Press)));
                _menu.AddItem(
                    new MenuItem(_menu.Name + ".combo", Global.Lang.Get("Orbwalker_Combo")).SetValue(
                        new KeyBind(32, KeyBindType.Press)));

                _delay = _menu.Item(_menu.Name + ".miscellaneous.movement-delay").GetValue<Slider>().Value;
                Game.OnUpdate += GameOnOnGameUpdate;
                Drawing.OnDraw += DrawingOnOnDraw;
            }

            public int HoldAreaRadius
            {
                get { return _menu.Item(_menu.Name + ".miscellaneous.hold-position-radius").GetValue<Slider>().Value; }
            }

            private int FarmDelay
            {
                get { return _menu.Item(_menu.Name + ".miscellaneous.farm-delay").GetValue<Slider>().Value; }
            }

            public OrbwalkingMode ActiveMode
            {
                get
                {
                    if (_mode != OrbwalkingMode.None)
                    {
                        return _mode;
                    }

                    if (_menu.Item(_menu.Name + ".combo").GetValue<KeyBind>().Active)
                    {
                        return OrbwalkingMode.Combo;
                    }

                    if (_menu.Item(_menu.Name + ".mixed").GetValue<KeyBind>().Active)
                    {
                        return OrbwalkingMode.Mixed;
                    }

                    if (_menu.Item(_menu.Name + ".harass").GetValue<KeyBind>().Active)
                    {
                        return OrbwalkingMode.Harass;
                    }

                    if (_menu.Item(_menu.Name + ".laneclear").GetValue<KeyBind>().Active)
                    {
                        return OrbwalkingMode.LaneClear;
                    }

                    if (_menu.Item(_menu.Name + ".lasthit").GetValue<KeyBind>().Active)
                    {
                        return OrbwalkingMode.LastHit;
                    }

                    if (_menu.Item(_menu.Name + ".flee").GetValue<KeyBind>().Active)
                    {
                        return OrbwalkingMode.Flee;
                    }

                    return OrbwalkingMode.None;
                }
                set { _mode = value; }
            }

            // ReSharper disable once MemberHidesStaticFromOuterClass
            public virtual bool InAutoAttackRange(AttackableUnit target)
            {
                return Orbwalking.InAutoAttackRange(target);
            }

            /// <summary>
            ///     Enables or disables the auto-attacks.
            /// </summary>
            public void SetAttack(bool b)
            {
                Attack = b;
            }

            /// <summary>
            ///     Enables or disables the movement.
            /// </summary>
            public void SetMovement(bool b)
            {
                Move = b;
            }

            /// <summary>
            ///     Forces the orbwalker to attack the set target if valid and in range.
            /// </summary>
            public void ForceTarget(Obj_AI_Base target)
            {
                _forcedTarget = target;
            }

            /// <summary>
            ///     Forces the orbwalker to move to that point while orbwalking (Game.CursorPos by default).
            /// </summary>
            public void SetOrbwalkingPoint(Vector3 point)
            {
                _orbwalkingPoint = point;
            }

            private bool ShouldWait()
            {
                return
                    ObjectManager.Get<Obj_AI_Minion>()
                        .Any(
                            minion =>
                                minion.IsValidTarget() && minion.Team != GameObjectTeam.Neutral &&
                                InAutoAttackRange(minion) &&
                                HealthPrediction.LaneClearHealthPrediction(
                                    minion, (int) ((Player.AttackDelay * 1000) * LaneClearWaitTimeMod), FarmDelay) <=
                                Player.GetAutoAttackDamage(minion));
            }

            public virtual AttackableUnit GetTarget()
            {
                AttackableUnit result = null;
                if ((ActiveMode == OrbwalkingMode.Mixed || ActiveMode == OrbwalkingMode.Harass ||
                     ActiveMode == OrbwalkingMode.LaneClear) &&
                    !_menu.Item(_menu.Name + ".miscellaneous.prioritize-lasthit").GetValue<bool>())
                {
                    var target = TargetSelector.GetTarget(-1, LeagueSharp.Common.TargetSelector.DamageType.Physical);
                    if (target != null)
                    {
                        return target;
                    }
                }

                /*Killable Minion*/
                if (ActiveMode == OrbwalkingMode.LaneClear || ActiveMode == OrbwalkingMode.Mixed ||
                    ActiveMode == OrbwalkingMode.LastHit)
                {
                    foreach (var minion in
                        ObjectManager.Get<Obj_AI_Minion>()
                            .Where(
                                minion =>
                                    minion.IsValidTarget() && InAutoAttackRange(minion) &&
                                    !minion.BaseSkinName.Contains("ward", StringComparison.OrdinalIgnoreCase) &&
                                    !minion.BaseSkinName.Contains("trinket", StringComparison.OrdinalIgnoreCase))
                            .OrderByDescending(
                                m => m.BaseSkinName.Contains("MinionSiege", StringComparison.OrdinalIgnoreCase)))
                    {
                        var t = (int) (Player.AttackCastDelay * 1000) - 100 + Game.Ping / 2 +
                                1000 * (int) Player.Distance(minion) / (int) GetMyProjectileSpeed();
                        var predHealth = HealthPrediction.GetHealthPrediction(minion, t, FarmDelay);

                        if (minion.Team != GameObjectTeam.Neutral && MinionManager.IsMinion(minion, true))
                        {
                            if (predHealth <= 0)
                            {
                                FireOnNonKillableMinion(minion);
                            }

                            if (predHealth > 0 && predHealth <= Player.GetAutoAttackDamage(minion, true))
                            {
                                return minion;
                            }
                        }
                    }
                }

                //Forced target
                if (_forcedTarget.IsValidTarget() && InAutoAttackRange(_forcedTarget))
                {
                    return _forcedTarget;
                }

                /* turrets / inhibitors / nexus */
                if (ActiveMode == OrbwalkingMode.LaneClear)
                {
                    /* turrets */
                    foreach (var turret in
                        ObjectManager.Get<Obj_AI_Turret>().Where(t => t.IsValidTarget() && InAutoAttackRange(t)))
                    {
                        return turret;
                    }

                    /* inhibitor */
                    foreach (var turret in
                        ObjectManager.Get<Obj_BarracksDampener>().Where(t => t.IsValidTarget() && InAutoAttackRange(t)))
                    {
                        return turret;
                    }

                    /* nexus */
                    foreach (var nexus in
                        ObjectManager.Get<Obj_HQ>().Where(t => t.IsValidTarget() && InAutoAttackRange(t)))
                    {
                        return nexus;
                    }
                }

                /*Champions*/
                if (ActiveMode != OrbwalkingMode.LastHit)
                {
                    var target = TargetSelector.GetTarget(-1, LeagueSharp.Common.TargetSelector.DamageType.Physical);
                    if (target.IsValidTarget())
                    {
                        return target;
                    }
                }

                /*Jungle minions*/
                if (ActiveMode == OrbwalkingMode.LaneClear || ActiveMode == OrbwalkingMode.Mixed)
                {
                    result =
                        ObjectManager.Get<Obj_AI_Minion>()
                            .Where(
                                mob =>
                                    mob.IsValidTarget() && InAutoAttackRange(mob) && mob.Team == GameObjectTeam.Neutral)
                            .MaxOrDefault(mob => mob.MaxHealth);
                    if (result != null)
                    {
                        return result;
                    }
                }

                /*Lane Clear minions*/
                if (ActiveMode == OrbwalkingMode.LaneClear)
                {
                    if (!ShouldWait())
                    {
                        if (_prevMinion.IsValidTarget() && InAutoAttackRange(_prevMinion))
                        {
                            var predHealth = HealthPrediction.LaneClearHealthPrediction(
                                _prevMinion, (int) ((Player.AttackDelay * 1000) * LaneClearWaitTimeMod), FarmDelay);
                            if (predHealth >= Player.GetAutoAttackDamage(_prevMinion) * 2 ||
                                Math.Abs(predHealth - _prevMinion.Health) < float.Epsilon)
                            {
                                return _prevMinion;
                            }
                        }

                        result = (from minion in
                            ObjectManager.Get<Obj_AI_Minion>()
                                .Where(minion => minion.IsValidTarget() && InAutoAttackRange(minion))
                            let predHealth =
                                HealthPrediction.LaneClearHealthPrediction(
                                    minion, (int) ((Player.AttackDelay * 1000) * LaneClearWaitTimeMod), FarmDelay)
                            where
                                predHealth >= Player.GetAutoAttackDamage(minion) * 2 ||
                                Math.Abs(predHealth - minion.Health) < float.Epsilon
                            where
                                !minion.BaseSkinName.Contains("ward", StringComparison.OrdinalIgnoreCase) &&
                                !minion.BaseSkinName.Contains("trinket", StringComparison.OrdinalIgnoreCase)
                            orderby
                                minion.BaseSkinName.Contains("MinionSiege", StringComparison.OrdinalIgnoreCase)
                                    descending
                            select minion).MaxOrDefault(m => m.Health);

                        if (result != null)
                        {
                            _prevMinion = (Obj_AI_Minion) result;
                        }
                    }
                }

                return result;
            }

            private void GameOnOnGameUpdate(EventArgs args)
            {
                try
                {
                    if (ActiveMode == OrbwalkingMode.None)
                    {
                        return;
                    }

                    //Prevent canceling important spells
                    if (Player.IsCastingInterruptableSpell(true))
                    {
                        return;
                    }

                    if (ActiveMode == OrbwalkingMode.Flee)
                    {
                        MoveTo(
                            (_orbwalkingPoint.To2D().IsValid()) ? _orbwalkingPoint : Game.CursorPos,
                            _menu.Item(_menu.Name + ".miscellaneous.hold-position-radius").GetValue<Slider>().Value,
                            true);
                        return;
                    }
                    Orbwalk(
                        GetTarget(), (_orbwalkingPoint.To2D().IsValid()) ? _orbwalkingPoint : Game.CursorPos,
                        _menu.Item(_menu.Name + ".miscellaneous.extra-windup-time").GetValue<Slider>().Value,
                        _menu.Item(_menu.Name + ".miscellaneous.hold-position-radius").GetValue<Slider>().Value);
                }
                catch (Exception ex)
                {
                    Global.Logger.AddItem(new LogItem(ex));
                }
            }

            private void DrawingOnOnDraw(EventArgs args)
            {
                try
                {
                    if (ObjectManager.Player.IsDead)
                    {
                        return;
                    }

                    if (_menu.Item(_menu.Name + ".drawing.aa-range").GetValue<Circle>().Active)
                    {
                        Render.Circle.DrawCircle(
                            Player.Position, GetRealAutoAttackRange(null) + 65,
                            _menu.Item(_menu.Name + ".drawing.aa-range").GetValue<Circle>().Color,
                            _menu.Item(_menu.Name + ".drawing.circle-thickness").GetValue<Slider>().Value);
                    }

                    if (_menu.Item(_menu.Name + ".drawing.aa-enemy-range").GetValue<Circle>().Active)
                    {
                        foreach (var target in
                            HeroManager.Enemies.Where(
                                target =>
                                    target.IsValidTarget(1500) &&
                                    target.Position.IsOnScreen(GetRealAutoAttackRange(target) + 65)))
                        {
                            Render.Circle.DrawCircle(
                                target.Position, GetRealAutoAttackRange(target) + 65,
                                _menu.Item(_menu.Name + ".drawing.aa-enemy-range").GetValue<Circle>().Color,
                                _menu.Item(_menu.Name + ".drawing.circle-thickness").GetValue<Slider>().Value);
                        }
                    }

                    if (_menu.Item(_menu.Name + ".drawing.hold-zone").GetValue<Circle>().Active)
                    {
                        Render.Circle.DrawCircle(
                            Player.Position,
                            _menu.Item(_menu.Name + ".miscellaneous.hold-position-radius").GetValue<Slider>().Value,
                            _menu.Item(_menu.Name + ".drawing.hold-zone").GetValue<Circle>().Color,
                            _menu.Item(_menu.Name + ".drawing.circle-thickness").GetValue<Slider>().Value);
                    }
                }
                catch (Exception ex)
                {
                    Global.Logger.AddItem(new LogItem(ex));
                }
            }
        }

        // ReSharper disable StringLiteralTypo
        //Spells that reset the attack timer.
        private static readonly string[] AttackResets =
        {
            "dariusnoxiantacticsonh", "fioraflurry", "garenq",
            "hecarimrapidslash", "jaxempowertwo", "jaycehypercharge", "leonashieldofdaybreak", "luciane", "lucianq",
            "monkeykingdoubleattack", "mordekaisermaceofspades", "nasusq", "nautiluspiercinggaze", "netherblade",
            "parley", "poppydevastatingblow", "powerfist", "renektonpreexecute", "rengarq", "shyvanadoubleattack",
            "sivirw", "takedown", "talonnoxiandiplomacy", "trundletrollsmash", "vaynetumble", "vie", "volibearq",
            "xenzhaocombotarget", "yorickspectral", "reksaiq"
        };

        //Spells that are not attacks even if they have the "attack" word in their name.
        private static readonly string[] NoAttacks =
        {
            "jarvanivcataclysmattack", "monkeykingdoubleattack",
            "shyvanadoubleattack", "shyvanadoubleattackdragon", "zyragraspingplantattack", "zyragraspingplantattack2",
            "zyragraspingplantattackfire", "zyragraspingplantattack2fire", "viktorpowertransfer"
        };

        //Spells that are attacks even if they dont have the "attack" word in their name.
        private static readonly string[] Attacks =
        {
            "caitlynheadshotmissile", "frostarrow", "garenslash2",
            "kennenmegaproc", "lucianpassiveattack", "masteryidoublestrike", "quinnwenhanced", "renektonexecute",
            "renektonsuperexecute", "rengarnewpassivebuffdash", "trundleq", "xenzhaothrust", "xenzhaothrust2",
            "xenzhaothrust3", "viktorqbuff"
        };

        // Champs whose auto attacks can't be cancelled
        private static readonly string[] NoCancelChamps = { "Kalista" };

        // ReSharper restore StringLiteralTypo
    }
}