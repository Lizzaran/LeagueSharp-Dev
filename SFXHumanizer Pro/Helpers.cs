#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Helpers.cs is part of SFXHumanizer Pro.

 SFXHumanizer Pro is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXHumanizer Pro is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXHumanizer Pro. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;

#endregion

namespace SFXHumanizer_Pro
{
    public static class Helpers
    {
        public static void DrawText(this Font font, string text, Vector2 position, Color color)
        {
            var measure = font.MeasureText(null, text, FontDrawFlags.Center);
            font.DrawText(
                null, text, (int) (position.X - measure.Width), (int) (position.Y - measure.Height * 0.5f), color);
        }

        public static Vector3 Randomize(this CryptoRandom rnd, Vector3 position, int amount)
        {
            if (amount > 0)
            {
                position.X += rnd.Next(0, amount * 2 + 1) - amount;
                position.Y += rnd.Next(0, amount * 2 + 1) - amount;
            }

            position.X = Truncate((int) (position.X) + (float) rnd.NextDouble(), 3);
            position.Y = Truncate((int) (position.Y) + (float) rnd.NextDouble(), 3);
            position.Z = NavMesh.GetHeightForPosition(position.X, position.Y);

            return position;
        }

        public static bool IsSharpTurn(Vector3 position, int minAngle)
        {
            if (position.IsValid())
            {
                var currentPath = ObjectManager.Player.GetWaypoints();
                if (currentPath.Count > 1 && currentPath.PathLength() > 100)
                {
                    var movePath = ObjectManager.Player.GetPath(position);
                    if (movePath.Length > 1)
                    {
                        var angle = (currentPath[1] - currentPath[0]).AngleBetween((movePath[1] - movePath[0]).To2D());
                        var distance = movePath.Last().To2D().Distance(currentPath.Last(), true);
                        return !(angle < 10 && distance < 500 * 500 || distance < 50 * 50) && angle > minAngle;
                    }
                }
            }
            return false;
        }

        public static float AngleBetween(Vector3 oldPosition, Vector3 newPosition)
        {
            if (oldPosition.IsValid() && newPosition.IsValid())
            {
                return
                    (oldPosition - ObjectManager.Player.Position).To2D()
                        .AngleBetween((newPosition - ObjectManager.Player.Position).To2D());
            }
            return 0f;
        }

        public static int[] CreateSequence(this CryptoRandom rnd, int count, int minValue, int maxValue, int total)
        {
            var sequences = new int[count];
            for (var i = 0; i < count; i++)
            {
                sequences[i] = rnd.Next(minValue, maxValue + 1);
            }
            return ModifySequenceToTotal(rnd, sequences, minValue, maxValue, total);
        }

        public static float Truncate(float value, int precision)
        {
            var step = (decimal) Math.Pow(10, precision);
            return (float) ((int) Math.Truncate(step * (decimal) value) / step);
        }

        private static int[] ModifySequenceToTotal(CryptoRandom rnd,
            int[] sequences,
            int minValue,
            int maxValue,
            int total)
        {
            var sum = sequences.Sum();
            var runs = Math.Abs(total - sum);
            if (runs > 0)
            {
                sequences = sequences.OrderBy(s => rnd.Next()).ToArray();
                var doIncrease = sum < total;
                var currentIndex = 0;
                for (var i = 0; i < runs;)
                {
                    var currentValue = sequences[currentIndex];
                    if (doIncrease && currentValue + 1 <= maxValue || !doIncrease && currentValue - 1 >= minValue)
                    {
                        sequences[currentIndex] = currentValue + (doIncrease ? 1 : -1);
                        i++;
                    }
                    currentIndex++;
                    if (currentIndex >= sequences.Length)
                    {
                        currentIndex = 0;
                    }
                }
            }
            return sequences;
        }
    }
}