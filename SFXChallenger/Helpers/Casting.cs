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

using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using MinionManager = SFXLibrary.MinionManager;
using MinionOrderTypes = SFXLibrary.MinionOrderTypes;
using MinionTeam = SFXLibrary.MinionTeam;
using MinionTypes = SFXLibrary.MinionTypes;
using TargetSelector = SFXChallenger.Wrappers.TargetSelector;

#endregion

namespace SFXChallenger.Helpers
{
    internal class Casting
    {
        public static void SkillShot(Spell spell, HitChance hitChance, bool aoe = false, bool towerCheck = false)
        {
            SkillShot(TargetSelector.GetTarget(spell.Range, spell.DamageType), spell, hitChance, aoe, towerCheck);
        }

        public static void SkillShot(Obj_AI_Base target,
            Spell spell,
            HitChance hitChance,
            bool aoe = false,
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

            spell.UpdateSourcePosition();

            var prediction = spell.GetPrediction(target, aoe);
            if (prediction.Hitchance >= hitChance)
            {
                spell.Cast(prediction.CastPosition);
            }
        }

        public static void TargetSkill(Spell spell, bool packet = false, bool towerCheck = false)
        {
            TargetSkill(TargetSelector.GetTarget(spell.Range, spell.DamageType), spell, packet, towerCheck);
        }

        public static void TargetSkill(Obj_AI_Base target, Spell spell, bool packet = false, bool towerCheck = false)
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

        public static void Farm(Spell spell, int minHit = 2, float overrideWidth = -1f)
        {
            if (!spell.IsReady())
            {
                return;
            }
            var minions =
                MinionManager.GetMinions(
                    ObjectManager.Player.ServerPosition, spell.Range, MinionTypes.All, MinionTeam.NotAlly,
                    MinionOrderTypes.MaxHealth).ToList();

            if (minions.Count == 0)
            {
                return;
            }

            if (spell.Type == SkillshotType.SkillshotCircle)
            {
                spell.UpdateSourcePosition();

                var prediction = spell.GetCircularFarmLocation(minions, overrideWidth);
                if (prediction.MinionsHit >= minHit)
                {
                    spell.Cast(prediction.Position);
                }
            }
            else if (spell.Type == SkillshotType.SkillshotLine)
            {
                spell.UpdateSourcePosition();

                var prediction = spell.GetLineFarmLocation(minions, overrideWidth);
                if (prediction.MinionsHit >= minHit)
                {
                    spell.Cast(prediction.Position);
                }
            }
        }
    }
}