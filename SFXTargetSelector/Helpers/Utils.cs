#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Utils.cs is part of SFXTargetSelector.

 SFXTargetSelector is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXTargetSelector is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXTargetSelector. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using LeagueSharp;
using LeagueSharp.Common;
using SFXTargetSelector.Others;
using SharpDX;

#endregion

namespace SFXTargetSelector
{
    internal class Utils
    {
        internal static void RaiseEvent<T>(EventHandler<T> @event, object sender, T e) where T : EventArgs
        {
            try
            {
                if (@event != null)
                {
                    @event(sender, e);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        internal static bool IsValidTarget(Obj_AI_Hero target,
            float range,
            DamageType damageType,
            bool ignoreShields = true,
            Vector3 from = default(Vector3))
        {
            return target.IsValidTarget() &&
                   target.Distance((from.Equals(default(Vector3)) ? ObjectManager.Player.ServerPosition : from), true) <
                   Math.Pow((range <= 0 ? Orbwalking.GetRealAutoAttackRange(target) : range), 2) &&
                   !Invulnerable.Check(target, damageType, ignoreShields);
        }
    }
}