#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 VectorExtensions.cs is part of SFXLibrary.

 SFXLibrary is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXLibrary is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXLibrary. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using Drawing = LeagueSharp.Drawing;
using Geometry = LeagueSharp.Common.Geometry;
using ObjectManager = LeagueSharp.ObjectManager;
using Obj_AI_Minion = LeagueSharp.Obj_AI_Minion;
using Vector2 = SharpDX.Vector2;
using Vector3 = SharpDX.Vector3;

#endregion

namespace SFXLibrary.Extensions.SharpDX
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Linq;

    #endregion

    public static class VectorExtensions
    {
        public static bool IsOnScreen(this Vector3 position, float radius)
        {
            var pos = Drawing.WorldToScreen(position);
            return !(pos.X + radius < 0) && !(pos.X - radius > Drawing.Width) && !(pos.Y + radius < 0) &&
                   !(pos.Y - radius > Drawing.Height);
        }

        public static bool IsOnScreen(this Vector2 position, float radius)
        {
            return Geometry.To3D(position).IsOnScreen(radius);
        }

        public static bool IsOnScreen(this Vector2 start, Vector2 end)
        {
            if (start.X > 0 && start.X < Drawing.Width && start.Y > 0 && start.Y < Drawing.Height && end.X > 0 &&
                end.X < Drawing.Width && end.Y > 0 && end.Y < Drawing.Height)
            {
                return true;
            }

            return new List<Geometry.IntersectionResult>
            {
                Geometry.Intersection(new Vector2(0, 0), new Vector2(0, Drawing.Width), start, end),
                Geometry.Intersection(new Vector2(0, Drawing.Width), new Vector2(Drawing.Height, Drawing.Width), start,
                    end),
                Geometry.Intersection(new Vector2(Drawing.Height, Drawing.Width), new Vector2(Drawing.Height, 0), start,
                    end),
                Geometry.Intersection(new Vector2(Drawing.Height, 0), new Vector2(0, 0), start, end)
            }.Any(intersection => intersection.Intersects);
        }

        public static Vector2 ClosestIntersection(this List<Geometry.IntersectionResult> intersections,
            Vector2 lineStart)
        {
            if (intersections.Count == 1)
                return intersections[0].Point;

            var point = new Vector2();
            var distance = float.MaxValue;
            foreach (var intersection in intersections.Where(intersection => intersection.Intersects))
            {
                var dist = Vector2.Distance(lineStart, intersection.Point);
                if (dist < distance)
                {
                    distance = dist;
                    point = intersection.Point;
                }
            }
            return point;
        }

        // Find the points of intersection.
        public static List<Geometry.IntersectionResult> FindLineCircleIntersections(this Vector2 lineStart,
            Vector2 lineEnd, Vector2 circleCenter, float circleRadius)
        {
            var intersections = new List<Geometry.IntersectionResult>();
            float t;
            var dx = lineEnd.X - lineStart.X;
            var dy = lineEnd.Y - lineStart.Y;
            var a = dx*dx + dy*dy;
            var b = 2*(dx*(lineStart.X - circleCenter.X) + dy*(lineStart.Y - circleCenter.Y));
            var c = (lineStart.X - circleCenter.X)*(lineStart.X - circleCenter.X) +
                    (lineStart.Y - circleCenter.Y)*(lineStart.Y - circleCenter.Y) - circleRadius*circleRadius;

            var det = b*b - 4*a*c;
            if ((a <= 0.0000001) || (det < 0))
            {
                return intersections;
            }
            if (det == 0)
            {
                t = -b/(2*a);
                intersections.Add(new Geometry.IntersectionResult(true,
                    new Vector2(lineStart.X + t*dx, lineStart.X + t*dx)));
            }
            else
            {
                t = (float) ((-b + Math.Sqrt(det))/(2*a));
                var point1 = new Vector2(lineStart.X + t*dx, lineStart.Y + t*dy);
                point1.Normalize();
                intersections.Add(new Geometry.IntersectionResult(true, point1));
                t = (float) ((-b - Math.Sqrt(det))/(2*a));
                var point2 = new Vector2(lineStart.X + t*dx, lineStart.Y + t*dy);
                point2.Normalize();
                intersections.Add(new Geometry.IntersectionResult(true, point2));
            }
            return intersections;
        }

        public static Obj_AI_Minion GetNearestMinionByNames(this Vector3 position, string[] names)
        {
            var nearest = float.MaxValue;
            var sMinion = default(Obj_AI_Minion);
            foreach (var minion in ObjectManager.Get<Obj_AI_Minion>()
                .Where(
                    minion =>
                        minion != null &&
                        minion.IsValid &&
                        names.Any(
                            name => String.Equals(minion.SkinName, name, StringComparison.CurrentCultureIgnoreCase))))
            {
                var distance = Vector3.Distance(position, minion.ServerPosition);
                if (nearest > distance || nearest == float.MaxValue)
                {
                    nearest = distance;
                    sMinion = minion;
                }
            }
            return sMinion;
        }

        public static Obj_AI_Minion GetNearestMinionByNames(this Vector2 position, string[] names)
        {
            return GetNearestMinionByNames(Geometry.To3D(position), names);
        }
    }
}