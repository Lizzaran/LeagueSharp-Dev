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
using SFXLibrary;
using SFXLibrary.Logger;
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
                return
                    GameObjects.Turrets
                        .Any(turret => turret.IsValidTarget(900f + extraRange, true, target.Position));
            }

            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return false;
        }

        public static float GetSpellDelay(this Spell spell, Obj_AI_Base target)
        {
            try
            {
                if (target is Obj_AI_Hero)
                {
                    var predTarget = Prediction.GetPrediction(
                        target,
                        spell.Delay +
                        (ObjectManager.Player.ServerPosition.Distance(target.ServerPosition) / (spell.Speed)) +
                        (Game.Ping / 2000f) + 0.1f);
                    return spell.Delay +
                           (ObjectManager.Player.ServerPosition.Distance(predTarget.UnitPosition) / (spell.Speed)) +
                           (Game.Ping / 2000f) + 0.1f;
                }
                return spell.Delay +
                       (ObjectManager.Player.ServerPosition.Distance(target.ServerPosition) / (spell.Speed)) +
                       (Game.Ping / 2000f) + 0.1f;
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
            return GameObjects.AllyTurrets.Any(t => t.Distance(position) < 925f);
        }
    }
}