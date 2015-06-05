#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 DamageIndicator.cs is part of SFXChallenger.

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
using SharpDX;
using Color = System.Drawing.Color;

#endregion

namespace SFXChallenger.Helpers
{
    public class DamageIndicator
    {
        private const int BarWidth = 104;
        private const int LineThickness = 9;
        private static Utility.HpBarDamageIndicator.DamageToUnitDelegate _damageToUnit;
        private static readonly Vector2 BarOffset = new Vector2(10, 25);
        private static Color _drawingColor;

        public static Color DrawingColor
        {
            get { return _drawingColor; }
            set { _drawingColor = Color.FromArgb(150, value); }
        }

        public static bool Enabled { get; set; }

        public static void Initialize(Utility.HpBarDamageIndicator.DamageToUnitDelegate damageToUnit)
        {
            _damageToUnit = damageToUnit;
            DrawingColor = Color.Green;
            Enabled = true;
            Drawing.OnDraw += OnDrawingDraw;
        }

        private static void OnDrawingDraw(EventArgs args)
        {
            if (Enabled)
            {
                foreach (var unit in HeroManager.Enemies.Where(u => u.IsValidTarget() && u.IsHPBarRendered))
                {
                    var damage = _damageToUnit(unit);
                    if (damage <= 0)
                    {
                        continue;
                    }
                    var damagePercentage = ((unit.Health - damage) > 0 ? (unit.Health - damage) : 0) / unit.MaxHealth;
                    var currentHealthPercentage = unit.Health / unit.MaxHealth;
                    var startPoint =
                        new Vector2(
                            (int) (unit.HPBarPosition.X + BarOffset.X + damagePercentage * BarWidth),
                            (int) (unit.HPBarPosition.Y + BarOffset.Y) - 5);
                    var endPoint =
                        new Vector2(
                            (int) (unit.HPBarPosition.X + BarOffset.X + currentHealthPercentage * BarWidth) + 1,
                            (int) (unit.HPBarPosition.Y + BarOffset.Y) - 5);
                    Drawing.DrawLine(startPoint, endPoint, LineThickness, DrawingColor);
                }
            }
        }
    }
}