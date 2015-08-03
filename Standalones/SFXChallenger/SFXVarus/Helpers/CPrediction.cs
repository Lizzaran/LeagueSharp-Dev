#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 cprediction.cs is part of SFXVarus.

 SFXVarus is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXVarus is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXVarus. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXLibrary;
using SFXLibrary.Extensions.NET;
using SFXLibrary.Logger;
using SharpDX;
using Spell = SFXVarus.Wrappers.Spell;

#endregion

namespace SFXVarus.Helpers
{
    internal class CPrediction
    {
        public static Result Circle(Spell spell, Obj_AI_Hero target, HitChance hitChance, bool boundingRadius = true)
        {
            try
            {
                if (spell == null || target == null)
                {
                    return new Result(Vector3.Zero, new List<Obj_AI_Hero>());
                }
                var hits = new List<Obj_AI_Hero>();
                var center = Vector3.Zero;
                var radius = float.MaxValue;
                var range = spell.Range + (spell.Width * 0.9f) + (boundingRadius ? target.BoundingRadius * 0.85f : 0);
                var positions = (from t in GameObjects.EnemyHeroes
                    where t.IsValidTarget(range, true, spell.RangeCheckFrom)
                    let prediction = spell.GetPrediction(t)
                    where prediction.Hitchance >= (hitChance - 1)
                    select new Position(t, prediction.UnitPosition)).ToList();
                var width = spell.Width +
                            (boundingRadius ? positions.Select(p => p.Hero).Min(p => p.BoundingRadius) : 0);
                if (positions.Any())
                {
                    var possibilities = ListExtensions.ProduceEnumeration(positions).Where(p => p.Count > 0).ToList();
                    if (possibilities.Any())
                    {
                        foreach (var possibility in possibilities)
                        {
                            var mec = MEC.GetMec(possibility.Select(p => p.UnitPosition.To2D()).ToList());
                            var distance = spell.From.Distance(mec.Center.To3D());
                            if (mec.Radius < width && distance < range)
                            {
                                var lHits = new List<Obj_AI_Hero>();
                                var circle =
                                    new Geometry.Polygon.Circle(
                                        spell.From.Extend(
                                            mec.Center.To3D(), spell.Range > distance ? distance : spell.Range),
                                        spell.Width);

                                if (boundingRadius)
                                {
                                    lHits.AddRange(
                                        (from position in positions
                                            where
                                                new Geometry.Polygon.Circle(
                                                    position.UnitPosition, (position.Hero.BoundingRadius * 0.85f))
                                                    .Points.Any(p => circle.IsInside(p))
                                            select position.Hero));
                                }
                                else
                                {
                                    lHits.AddRange(
                                        from position in positions
                                        where circle.IsInside(position.UnitPosition)
                                        select position.Hero);
                                }
                                if ((lHits.Count > hits.Count || lHits.Count == hits.Count && mec.Radius < radius ||
                                     lHits.Count == hits.Count &&
                                     spell.From.Distance(circle.Center.To3D()) < spell.From.Distance(center)) &&
                                    lHits.Any(p => p.NetworkId == target.NetworkId))
                                {
                                    center = circle.Center.To3D2();
                                    radius = mec.Radius;
                                    hits.Clear();
                                    hits.AddRange(lHits);
                                }
                            }
                        }
                        if (!center.Equals(Vector3.Zero))
                        {
                            return new Result(center, hits);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return new Result(Vector3.Zero, new List<Obj_AI_Hero>());
        }

        public static Result Line(Spell spell,
            Obj_AI_Hero target,
            HitChance hitChance,
            bool boundingRadius = true,
            bool maxRange = true)
        {
            try
            {
                if (spell == null || target == null)
                {
                    return new Result(Vector3.Zero, new List<Obj_AI_Hero>());
                }
                var range = (spell.IsChargedSpell && maxRange ? spell.ChargedMaxRange : spell.Range) +
                            (spell.Width * 0.9f) + (boundingRadius ? target.BoundingRadius * 0.85f : 0);
                var positions = (from t in GameObjects.EnemyHeroes
                    where t.IsValidTarget(range, true, spell.RangeCheckFrom)
                    let prediction = spell.GetPrediction(t)
                    where prediction.Hitchance >= (hitChance - 1)
                    select new Position(t, prediction.UnitPosition)).ToList();
                if (positions.Any())
                {
                    var hits = new List<Obj_AI_Hero>();
                    var pred = spell.GetPrediction(target);
                    if (pred.Hitchance >= hitChance)
                    {
                        hits.Add(target);
                        var rect = new Geometry.Polygon.Rectangle(
                            spell.From, spell.From.Extend(pred.CastPosition, range), spell.Width);
                        if (boundingRadius)
                        {
                            hits.AddRange(
                                from point in positions.Where(p => p.Hero.NetworkId != target.NetworkId)
                                let circle =
                                    new Geometry.Polygon.Circle(point.UnitPosition, point.Hero.BoundingRadius * 0.85f)
                                where circle.Points.Any(p => rect.IsInside(p))
                                select point.Hero);
                        }
                        else
                        {
                            hits.AddRange(
                                from position in positions
                                where rect.IsInside(position.UnitPosition)
                                select position.Hero);
                        }
                        return new Result(pred.CastPosition, hits);
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return new Result(Vector3.Zero, new List<Obj_AI_Hero>());
        }

        private struct Position
        {
            public readonly Obj_AI_Hero Hero;
            public readonly Vector3 UnitPosition;

            public Position(Obj_AI_Hero hero, Vector3 unitPosition)
            {
                Hero = hero;
                UnitPosition = unitPosition;
            }
        }

        internal struct Result
        {
            public readonly Vector3 CastPosition;
            public readonly List<Obj_AI_Hero> Hits;
            public readonly int TotalHits;

            public Result(Vector3 castPosition, List<Obj_AI_Hero> hits)
            {
                CastPosition = castPosition;
                Hits = hits;
                TotalHits = hits.Count;
            }
        }
    }
}