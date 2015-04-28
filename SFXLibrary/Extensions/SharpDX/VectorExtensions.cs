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
            return !(pos.X + radius < 0) && !(pos.X - radius > Drawing.Width) && !(pos.Y + radius < 0) && !(pos.Y - radius > Drawing.Height);
        }

        public static bool IsOnScreen(this Vector2 position, float radius)
        {
            return Geometry.To3D(position).IsOnScreen(radius);
        }

        public static bool IsOnScreen(this Vector2 start, Vector2 end)
        {
            if (start.X > 0 && start.X < Drawing.Width && start.Y > 0 && start.Y < Drawing.Height && end.X > 0 && end.X < Drawing.Width && end.Y > 0 &&
                end.Y < Drawing.Height)
            {
                return true;
            }
            return
                new List<Geometry.IntersectionResult>
                {
                    Geometry.Intersection(new Vector2(0, 0), new Vector2(0, Drawing.Width), start, end),
                    Geometry.Intersection(new Vector2(0, Drawing.Width), new Vector2(Drawing.Height, Drawing.Width), start, end),
                    Geometry.Intersection(new Vector2(Drawing.Height, Drawing.Width), new Vector2(Drawing.Height, 0), start, end),
                    Geometry.Intersection(new Vector2(Drawing.Height, 0), new Vector2(0, 0), start, end)
                }.Any(intersection => intersection.Intersects);
        }

        public static Geometry.IntersectionResult Intersects(this Vector2 lineStart, Vector2 lineEnd, Vector2 circlePos, float circleRadius)
        {
            if (IsIntersecting(lineStart, lineEnd, circlePos, circleRadius))
            {
                var m = (lineEnd.Y - lineStart.Y)/(lineEnd.X - lineStart.X);
                var d = lineStart.Y - m*lineStart.X;
                var a = 1 + m*m;
                var b = 2*(m*d - m*circlePos.Y - circlePos.X);
                var c = circlePos.X*circlePos.X + d*d + circlePos.Y*circlePos.Y - circleRadius*circleRadius - 2*d*circlePos.Y;
                var sqRtTerm = Math.Sqrt(b*b - 4*a*c);
                var x = ((-b) + sqRtTerm)/(2*a);
                if ((x < Math.Min(lineStart.X, lineEnd.X) || (x > Math.Max(lineStart.X, lineEnd.X))))
                {
                    x = ((-b) - sqRtTerm)/(2*a);
                }
                var y = m*x + d;
                return new Geometry.IntersectionResult(true, new Vector2((float) x, (float) y));
            }
            return new Geometry.IntersectionResult(false);
        }

        public static bool IsInsideCircle(this Vector2 point, Vector2 circlePos, float circleRad)
        {
            return Math.Sqrt(Math.Pow((circlePos.X - point.X), 2) + Math.Pow((circlePos.Y - point.Y), 2)) < circleRad;
        }

        public static bool IsIntersecting(this Vector2 lineStart, Vector2 lineEnd, Vector2 circlePos, float circleRadius)
        {
            return IsInsideCircle(lineStart, circlePos, circleRadius) ^ IsInsideCircle(lineEnd, circlePos, circleRadius);
        }

        public static Obj_AI_Minion GetNearestMinionByNames(this Vector3 position, string[] names)
        {
            var nearest = float.MaxValue;
            Obj_AI_Minion sMinion = null;
            foreach (var minion in
                ObjectManager.Get<Obj_AI_Minion>()
                    .Where(
                        minion =>
                            minion != null && minion.IsValid && names.Any(name => minion.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase)))
                )
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