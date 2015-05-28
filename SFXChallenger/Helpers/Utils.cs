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
using SFXLibrary.Logger;

#endregion

namespace SFXChallenger.Helpers
{
    public static class Utils
    {
        public static bool IsNearTurret(this Obj_AI_Base target)
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
    }
}