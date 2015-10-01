#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Utils.cs is part of SFXChallenger.

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
using SFXChallenger.Library;
using SFXChallenger.Library.Logger;
using SharpDX;

#endregion

namespace SFXChallenger.Helpers
{
    public static class Utils
    {
        public static bool IsNearTurret(this Obj_AI_Base target, float extraRange = 300f)
        {
            try
            {
                return GameObjects.Turrets.Any(turret => turret.IsValidTarget(900f + extraRange, true, target.Position));
            }

            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return false;
        }

        public static Vector2 PositionAfter(Obj_AI_Base unit, float t, float speed = float.MaxValue)
        {
            var distance = t * speed;
            var path = unit.GetWaypoints();

            for (var i = 0; i < path.Count - 1; i++)
            {
                var a = path[i];
                var b = path[i + 1];
                var d = a.Distance(b);

                if (d < distance)
                {
                    distance -= d;
                }
                else
                {
                    return a + distance * (b - a).Normalized();
                }
            }

            return path[path.Count - 1];
        }

        public static float SpellArrivalTime(Obj_AI_Base sender,
            Obj_AI_Base target,
            float delay,
            float speed,
            bool prediction = false)
        {
            try
            {
                var additional = sender.IsMe ? (Game.Ping / 2000f) + 0.1f : 0f;
                if (prediction && target is Obj_AI_Hero && target.IsMoving)
                {
                    var predTarget = Prediction.GetPrediction(
                        target,
                        delay + (sender.ServerPosition.Distance(target.ServerPosition) * 1.1f / speed) + additional);
                    return delay + (sender.ServerPosition.Distance(predTarget.UnitPosition) * 1.1f / speed + additional);
                }
                return delay + (sender.ServerPosition.Distance(target.ServerPosition) / speed + additional);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return 0;
        }

        public static bool IsLyingInCone(Vector2 position, Vector2 apexPoint, Vector2 circleCenter, double aperture)
        {
            try
            {
                var halfAperture = aperture / 2;
                var apexToXVector = apexPoint - position;
                var axisVector = apexPoint - circleCenter;
                var isInInfiniteCone = DotProd(apexToXVector, axisVector) / Magn(apexToXVector) / Magn(axisVector) >
                                       Math.Cos(halfAperture);
                return isInInfiniteCone && DotProd(apexToXVector, axisVector) / Magn(axisVector) < Magn(axisVector);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return false;
        }

        private static float DotProd(Vector2 a, Vector2 b)
        {
            return a.X * b.X + a.Y * b.Y;
        }

        private static float Magn(Vector2 a)
        {
            return (float) (Math.Sqrt(a.X * a.X + a.Y * a.Y));
        }

        public static bool UnderAllyTurret(Vector3 position)
        {
            return GameObjects.AllyTurrets.Any(t => t.Distance(position) < 925f && !t.IsDead && t.Health > 1);
        }

        public static bool IsImmobile(Obj_AI_Base t)
        {
            return t.HasBuffOfType(BuffType.Stun) || t.HasBuffOfType(BuffType.Charm) || t.HasBuffOfType(BuffType.Snare) ||
                   t.HasBuffOfType(BuffType.Knockup) || t.HasBuffOfType(BuffType.Polymorph) ||
                   t.HasBuffOfType(BuffType.Fear) || t.HasBuffOfType(BuffType.Taunt) || t.IsStunned;
        }

        public static bool IsSlowed(Obj_AI_Base t)
        {
            return t.HasBuffOfType(BuffType.Slow);
        }

        public static float GetImmobileTime(Obj_AI_Base target)
        {
            var buffs =
                target.Buffs.Where(
                    t =>
                        t.Type == BuffType.Charm || t.Type == BuffType.Snare || t.Type == BuffType.Knockback ||
                        t.Type == BuffType.Polymorph || t.Type == BuffType.Fear || t.Type == BuffType.Taunt ||
                        t.Type == BuffType.Stun).ToList();
            if (buffs.Any())
            {
                return buffs.Max(t => t.EndTime) - Game.Time;
            }
            return 0f;
        }

        public static bool IsFacing(this Obj_AI_Base source, Vector3 position, float angle = 90)
        {
            if (source == null || position.Equals(Vector3.Zero))
            {
                return false;
            }
            return source.Direction.To2D().Perpendicular().AngleBetween((position - source.Position).To2D()) < angle;
        }

        public static bool ShouldDraw(bool checkScreen = false)
        {
            return !ObjectManager.Player.IsDead && !MenuGUI.IsShopOpen &&
                   (!checkScreen || ObjectManager.Player.Position.IsOnScreen());
        }
    }
}