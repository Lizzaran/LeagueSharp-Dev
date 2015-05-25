#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Casting.cs is part of SFXChallenger.

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

using LeagueSharp;
using LeagueSharp.Common;

#endregion

namespace SFXChallenger.Wrappers
{
    #region

    

    #endregion

    internal class Casting
    {
        public static void BasicSkillShot(Spell spell, HitChance hitChance, bool towerCheck = false)
        {
            BasicSkillShot(TargetSelector.GetTarget(spell.Range, spell.DamageType), spell, hitChance, towerCheck);
        }

        public static void BasicSkillShot(Obj_AI_Base target, Spell spell, HitChance hitChance, bool towerCheck = false)
        {
            if (!spell.IsReady())
            {
                return;
            }

            if (target == null || towerCheck && target.UnderTurret(true))
            {
                return;
            }

            spell.UpdateSourcePosition();

            var prediction = spell.GetPrediction(target);
            if (prediction.Hitchance >= hitChance)
            {
                spell.Cast(prediction.CastPosition);
            }
        }

        public static void BasicTargetSkill(Spell spell, bool packet = false, bool towerCheck = false)
        {
            BasicTargetSkill(TargetSelector.GetTarget(spell.Range, spell.DamageType), spell, packet, towerCheck);
        }

        public static void BasicTargetSkill(Obj_AI_Base target,
            Spell spell,
            bool packet = false,
            bool towerCheck = false)
        {
            if (!spell.IsReady())
            {
                return;
            }

            if (target == null || towerCheck && target.UnderTurret(true))
            {
                return;
            }

            spell.Cast(target, packet);
        }

        public static void BasicFarm(Spell spell)
        {
            if (!spell.IsReady())
            {
                return;
            }

            var minion = MinionManager.GetMinions(
                ObjectManager.Player.ServerPosition, spell.Range, MinionTypes.All, MinionTeam.NotAlly);

            if (minion.Count == 0)
            {
                return;
            }

            if (spell.Type == SkillshotType.SkillshotCircle)
            {
                spell.UpdateSourcePosition();

                var prediction = spell.GetCircularFarmLocation(minion);
                if (prediction.MinionsHit >= 2)
                {
                    spell.Cast(prediction.Position);
                }
            }
            else if (spell.Type == SkillshotType.SkillshotLine)
            {
                spell.UpdateSourcePosition();

                var prediction = spell.GetLineFarmLocation(minion);
                if (prediction.MinionsHit >= 2)
                {
                    spell.Cast(prediction.Position);
                }
            }
        }
    }
}