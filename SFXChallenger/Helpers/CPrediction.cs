#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 CPrediction.cs is part of SFXChallenger.

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
using SFXLibrary;
using SFXLibrary.Extensions.NET;
using SFXLibrary.Logger;
using SharpDX;

#endregion

namespace SFXChallenger.Helpers
{
    internal class CPrediction
    {
        public static Result Circle(Spell spell, Obj_AI_Hero target, HitChance hitChance, bool boundingRadius = true)
        {
            try
            {
                var hits = new List<Obj_AI_Hero>();
                var center = Vector2.Zero;
                var radius = float.MaxValue;
                var range = spell.Range + spell.Width + (boundingRadius ? target.BoundingRadius * 0.75f : 0);
                var width = spell.Width + (boundingRadius ? target.BoundingRadius * 2f : 0);
                var points = (from t in GameObjects.EnemyHeroes
                    where t.IsValidTarget(range)
                    let prediction = spell.GetPrediction(t)
                    where prediction.Hitchance >= (hitChance - 1)
                    select new Tuple<Obj_AI_Hero, Vector2>(t, prediction.UnitPosition.To2D())).ToList();
                if (points.Any())
                {
                    var possibilities = ListExtensions.ProduceEnumeration(points).Where(p => p.Count > 0).ToList();
                    if (possibilities.Any())
                    {
                        foreach (var possibility in possibilities)
                        {
                            var mec = MEC.GetMec(possibility.Select(p => p.Item2).ToList());
                            var distance = ObjectManager.Player.Distance(mec.Center);
                            if (mec.Radius < width && distance < range)
                            {
                                var lHits = new List<Obj_AI_Hero>();
                                var circle =
                                    new Geometry.Polygon.Circle(
                                        ObjectManager.Player.Position.Extend(
                                            mec.Center.To3D(), spell.Range > distance ? distance : spell.Range),
                                        spell.Width);

                                if (boundingRadius)
                                {
                                    lHits.AddRange(
                                        (from point in points
                                            where
                                                new Geometry.Polygon.Circle(point.Item2, point.Item1.BoundingRadius)
                                                    .Points.Any(p => circle.IsInside(p))
                                            select point.Item1));
                                }
                                else
                                {
                                    lHits.AddRange(
                                        from point in points where circle.IsInside(point.Item2) select point.Item1);
                                }
                                if ((lHits.Count > hits.Count || lHits.Count == hits.Count && mec.Radius < radius ||
                                     lHits.Count == hits.Count &&
                                     ObjectManager.Player.Distance(circle.Center) <
                                     ObjectManager.Player.Distance(center)) &&
                                    lHits.Any(p => p.NetworkId == target.NetworkId))
                                {
                                    center = circle.Center;
                                    radius = mec.Radius;
                                    hits.Clear();
                                    hits.AddRange(lHits);
                                }
                            }
                        }
                        if (!center.Equals(Vector2.Zero))
                        {
                            return new Result(center.To3D2(), hits);
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

        internal struct Result
        {
            public Vector3 CastPosition;
            public List<Obj_AI_Hero> Hits;
            public int TotalHits;

            public Result(Vector3 castPosition, List<Obj_AI_Hero> hits)
            {
                CastPosition = castPosition;
                Hits = hits;
                TotalHits = hits.Count;
            }
        }
    }
}