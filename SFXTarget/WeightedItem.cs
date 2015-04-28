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