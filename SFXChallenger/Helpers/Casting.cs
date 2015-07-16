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

using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXLibrary.Extensions.NET;
using SharpDX;
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

        public static void Farm(Spell spell, int minHit = 3, float overrideWidth = -1f)
        {
            if (!spell.IsReady())
            {
                return;
            }
            var spellWidth = overrideWidth > 0 ? overrideWidth : spell.Width;
            var minions = MinionManager.GetMinions(
                ((spell.Range + spellWidth) * 1.5f), MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);

            if (minions.Count == 0)
            {
                return;
            }

            minHit = minions.Any(m => m.Team == GameObjectTeam.Neutral) ? 1 : minHit;

            if (spell.Type == SkillshotType.SkillshotCircle)
            {
                CircleFarm(spell, minions, minHit, overrideWidth);
            }
            else if (spell.Type == SkillshotType.SkillshotLine)
            {
                LineFarm(spell, minions, minHit, overrideWidth);
            }
            else if (spell.Type == SkillshotType.SkillshotCone)
            {
                ConeFarm(spell, minions, minHit, overrideWidth);
            }
        }

        private static void ConeFarm(Spell spell, List<Obj_AI_Base> minions, int min, float overrideWidth = -1f)
        {
            var spellWidth = overrideWidth > 0 ? overrideWidth : spell.Width;
            var pred = spell.GetCircularFarmLocation(minions, spellWidth);
            if (pred.MinionsHit >= min)
            {
                spell.Cast(pred.Position);
            }
        }

        private static void CircleFarm(Spell spell, List<Obj_AI_Base> minions, int min, float overrideWidth = -1f)
        {
            var spellWidth = overrideWidth > 0 ? overrideWidth : spell.Width;
            var points = (from minion in minions
                select spell.GetPrediction(minion)
                into pred
                where pred.Hitchance >= HitChance.Medium
                select pred.UnitPosition.To2D()).ToList();
            if (points.Any())
            {
                var possibilities = ListExtensions.ProduceEnumeration(points).Where(p => p.Count >= min).ToList();
                if (possibilities.Any())
                {
                    var hits = 0;
                    var radius = float.MaxValue;
                    var pos = Vector3.Zero;
                    foreach (var possibility in possibilities)
                    {
                        var mec = MEC.GetMec(possibility);
                        if (mec.Radius < spellWidth * 0.95f)
                        {
                            if (possibility.Count > hits || possibility.Count == hits && radius > mec.Radius)
                            {
                                hits = possibility.Count;
                                radius = mec.Radius;
                                pos = mec.Center.To3D();
                            }
                        }
                    }
                    if (hits >= min && !pos.Equals(Vector3.Zero))
                    {
                        spell.Cast(pos);
                    }
                }
            }
        }

        private static void LineFarm(Spell spell, List<Obj_AI_Base> minions, int min, float overrideWidth = -1f)
        {
            var spellWidth = overrideWidth > 0 ? overrideWidth : spell.Width;
            var totalHits = 0;
            var castPos = Vector3.Zero;
            foreach (var minion in minions)
            {
                var lMinion = minion;
                var pred = spell.GetPrediction(minion);
                if (pred.Hitchance < HitChance.Medium)
                {
                    continue;
                }
                var rect = new Geometry.Polygon.Rectangle(
                    ObjectManager.Player.Position.To2D(),
                    pred.CastPosition.Extend(pred.CastPosition, spell.Range).To2D(), spellWidth);
                var count = 1 + (from minion2 in minions.Where(m => m.NetworkId != lMinion.NetworkId)
                    let pred2 = spell.GetPrediction(minion2)
                    where pred.Hitchance >= HitChance.Medium
                    where
                        new Geometry.Polygon.Circle(pred2.UnitPosition, minion.BoundingRadius * 0.8f).Points.Any(
                            p => rect.IsInside(p))
                    select 1).Sum();
                if (count > totalHits)
                {
                    totalHits = count;
                    castPos = pred.CastPosition;
                }
                if (totalHits == minions.Count)
                {
                    break;
                }
                if (!castPos.Equals(Vector3.Zero) && totalHits >= min)
                {
                    spell.Cast(castPos);
                }
            }
        }
    }
}