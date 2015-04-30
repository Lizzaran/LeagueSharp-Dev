#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 WeightedItem.cs is part of SFXTarget.

 SFXTarget is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXTarget is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXTarget. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License
namespace SFXTarget
{
    #region

    using System;
    using LeagueSharp;

    #endregion

    internal class WeightedItem
    {
        private readonly Func<Obj_AI_Hero, float> _getValue;

        public WeightedItem(string name, int weight, bool inverted, Func<Obj_AI_Hero, float> getValue)
        {
            _getValue = getValue;
            Name = name;
            Weight = weight;
            Inverted = inverted;
        }

        public string Name { get; set; }
        public int Weight { get; set; }
        public bool Inverted { get; set; }
        public float CurrentMin { get; set; }
        public float CurrentMax { get; set; }

        public float CalculatedWeight(Obj_AI_Hero target)
        {
            return CalculatedWeight(GetValue(target), CurrentMin, CurrentMax, Inverted ? Selector.MaxWeight : Selector.MinWeight,
                Inverted ? Selector.MinWeight : Selector.MaxWeight);
        }

        public float CalculatedWeight(float currentValue, float currentMin, float currentMax, float newMin, float newMax)
        {
            return (((currentValue - currentMin)*(newMax - newMin))/(currentMax - currentMin)) + newMin;
        }

        public float GetValue(Obj_AI_Hero target)
        {
            try
            {
                return _getValue(target);
            }
            catch
            {
                return Inverted ? float.MaxValue : float.MinValue;
            }
        }
    }
}