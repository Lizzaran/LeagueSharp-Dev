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

using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXChallenger.Library.Extensions.NET;
using SharpDX;
using MinionManager = SFXChallenger.Library.MinionManager;
using MinionOrderTypes = SFXChallenger.Library.MinionOrderTypes;
using MinionTeam = SFXChallenger.Library.MinionTeam;
using MinionTypes = SFXChallenger.Library.MinionTypes;
using Spell = SFXChallenger.Wrappers.Spell;
using TargetSelector = SFXChallenger.SFXTargetSelector.TargetSelector;

#endregion

namespace SFXChallenger.Helpers
{
    internal class Casting
    {
        public static void SkillShot(Spell spell, HitChance hitChance, bool boundingRadius = true, bool maxRange = true)
        {
            SkillShot(TargetSelector.GetTarget(spell), spell, hitChance);
        }

        public static void SkillShot(Obj_AI_Hero target,
            Spell spell,
            HitChance hitChance,
            bool boundingRadius = true,
            bool maxRange = true)
        {
            if (!spell.IsReady() || target == null)
            {
                return;
            }

            if (spell.Type == SkillshotType.SkillshotLine)
            {
                var prediction = CPrediction.Line(spell, target, hitChance, boundingRadius, maxRange);
                if (prediction.TotalHits >= 1)
                {
                    spell.Cast(prediction.CastPosition);
                }
            }
            else if (spell.Type == SkillshotType.SkillshotCircle)
            {
                var prediction = CPrediction.Circle(spell, target, hitChance);
                if (prediction.TotalHits >= 1)
                {
                    spell.Cast(prediction.CastPosition);
                }
            }
            else
            {
                var prediction = spell.GetPrediction(target);
                if (prediction.Hitchance >= hitChance)
                {
                    spell.Cast(prediction.CastPosition);
                }
            }
        }

        public static void TargetSkill(Spell spell)
        {
            TargetSkill(TargetSelector.GetTarget(spell), spell);
        }

        public static void TargetSkill(Obj_AI_Base target, Spell spell)
        {
            if (!spell.IsReady() || target == null)
            {
                return;
            }

            spell.CastOnUnit(target);
        }

        public static void FarmSelfAoe(Spell spell,
            int minHit = 3,
            float overrideWidth = -1f,
            List<Obj_AI_Base> minions = null)
        {
            if (!spell.IsReady())
            {
                return;
            }
            var spellWidth = overrideWidth > 0 ? overrideWidth : (spell.Width > 25f ? spell.Width : spell.Range);

            if (minions == null)
            {
                minions = MinionManager.GetMinions(
                    spellWidth, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
            }

            if (minions.Count == 0)
            {
                return;
            }

            if (minHit > 1)
            {
                var nearest = minions.OrderBy(m => m.Distance(ObjectManager.Player)).FirstOrDefault();
                if (nearest != null && nearest.Team == GameObjectTeam.Neutral)
                {
                    minHit = 1;
                }
            }
            if (minions.Count >= minHit)
            {
                spell.Cast();
            }
        }

        public static void Farm(Spell spell,
            int minHit = 3,
            float overrideWidth = -1f,
            bool chargeMax = true,
            List<Obj_AI_Base> minions = null)
        {
            if (!spell.IsReady())
            {
                return;
            }
            var spellWidth = overrideWidth > 0 ? overrideWidth : spell.Width;

            if (minions == null)
            {
                minions =
                    MinionManager.GetMinions(
                        (((chargeMax && spell.IsChargedSpell ? spell.ChargedMaxRange : spell.Range) + spellWidth) * 1.5f),
                        MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
            }

            if (minions.Count == 0)
            {
                return;
            }

            if (minHit > 1)
            {
                var nearest = minions.OrderBy(m => m.Distance(ObjectManager.Player)).FirstOrDefault();
                if (nearest != null && nearest.Team == GameObjectTeam.Neutral)
                {
                    minHit = 1;
                }
            }
            if (spell.IsSkillshot)
            {
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
            else
            {
                var minion =
                    minions.OrderBy(m => spell.IsKillable(m))
                        .FirstOrDefault(
                            m =>
                                spell.IsInRange(m) && spell.GetDamage(m) > m.Health ||
                                m.Health - spell.GetDamage(m) > m.MaxHealth * 0.25f);
                if (minion != null)
                {
                    spell.CastOnUnit(minion);
                }
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
            var spellWidth = (overrideWidth > 0 ? overrideWidth : spell.Width) + minions.Average(m => m.BoundingRadius);
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
                        if (mec.Radius < spellWidth)
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

            var positions = (from minion in minions
                let pred = spell.GetPrediction(minion)
                where pred.Hitchance >= HitChance.Medium
                select new Tuple<Obj_AI_Base, Vector3>(minion, pred.UnitPosition)).ToList();

            if (positions.Any())
            {
                foreach (var position in positions)
                {
                    var rect = new Geometry.Polygon.Rectangle(
                        ObjectManager.Player.Position, ObjectManager.Player.Position.Extend(position.Item2, spell.Range),
                        spellWidth);
                    var count =
                        positions.Select(
                            position2 =>
                                new Geometry.Polygon.Circle(position2.Item2, position2.Item1.BoundingRadius * 0.9f))
                            .Count(circle => circle.Points.Any(p => rect.IsInside(p)));
                    if (count > totalHits)
                    {
                        totalHits = count;
                        castPos = position.Item2;
                    }
                    if (totalHits == minions.Count)
                    {
                        break;
                    }
                }
                if (!castPos.Equals(Vector3.Zero) && totalHits >= min)
                {
                    spell.Cast(castPos);
                }
            }
        }
    }
}