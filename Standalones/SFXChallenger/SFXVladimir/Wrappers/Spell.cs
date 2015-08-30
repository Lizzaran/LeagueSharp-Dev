#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Spell.cs is part of SFXVladimir.

 SFXVladimir is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXVladimir is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXVladimir. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using LeagueSharp;
using LeagueSharp.Common;
using SFXLibrary.Logger;
using DamageType = SFXVladimir.Enumerations.DamageType;

#endregion

namespace SFXVladimir.Wrappers
{
    public class Spell : LeagueSharp.Common.Spell
    {
        public Spell(SpellSlot slot, float range = 3.402823E+38f, DamageType damageType = DamageType.Physical)
            : base(
                slot, range,
                damageType == DamageType.Physical
                    ? LeagueSharp.Common.TargetSelector.DamageType.Physical
                    : (damageType == DamageType.Magical
                        ? LeagueSharp.Common.TargetSelector.DamageType.Magical
                        : LeagueSharp.Common.TargetSelector.DamageType.True)) {}

        public new DamageType DamageType
        {
            get { return ConvertDamageType(base.DamageType); }
            set { base.DamageType = ConvertDamageType(value); }
        }

        private DamageType ConvertDamageType(LeagueSharp.Common.TargetSelector.DamageType type)
        {
            switch (type)
            {
                case LeagueSharp.Common.TargetSelector.DamageType.Physical:
                    return DamageType.Physical;
                case LeagueSharp.Common.TargetSelector.DamageType.Magical:
                    return DamageType.Magical;
                case LeagueSharp.Common.TargetSelector.DamageType.True:
                    return DamageType.True;
            }
            return DamageType.True;
        }

        private LeagueSharp.Common.TargetSelector.DamageType ConvertDamageType(DamageType type)
        {
            switch (type)
            {
                case DamageType.Physical:
                    return LeagueSharp.Common.TargetSelector.DamageType.Physical;
                case DamageType.Magical:
                    return LeagueSharp.Common.TargetSelector.DamageType.Magical;
                case DamageType.True:
                    return LeagueSharp.Common.TargetSelector.DamageType.True;
            }
            return LeagueSharp.Common.TargetSelector.DamageType.Physical;
        }

        public float ArrivalTime(Obj_AI_Base target)
        {
            try
            {
                if (target is Obj_AI_Hero && target.IsMoving)
                {
                    var predTarget = Prediction.GetPrediction(
                        target, Delay + (From.Distance(target.ServerPosition) / Speed) + (Game.Ping / 2000f) + 0.1f);
                    return Delay + (From.Distance(predTarget.UnitPosition) / Speed) + (Game.Ping / 2000f) + 0.1f;
                }
                return Delay + (From.Distance(target.ServerPosition) / Speed) + (Game.Ping / 2000f) + 0.1f;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return 0;
        }
    }
}