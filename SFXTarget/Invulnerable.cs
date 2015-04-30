#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Invulnerable.cs is part of SFXTarget.

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
    using System.Collections.Generic;
    using System.Linq;
    using LeagueSharp;
    using LeagueSharp.Common;

    #endregion

    internal static class Invulnerable
    {
        private static readonly List<InvulnerableStruct> Invulnerables = new List<InvulnerableStruct>
        {
            new InvulnerableStruct("Undying Rage", null, false, (target, type) => (target.Health/target.MaxHealth*100) < 5),
            new InvulnerableStruct("JudicatorIntervention", null, false),
            new InvulnerableStruct("BlackShield", TargetSelector.DamageType.Magical, true),
            new InvulnerableStruct("BansheesVeil", TargetSelector.DamageType.Magical, true),
            new InvulnerableStruct("SivirShield", TargetSelector.DamageType.Magical, true),
            new InvulnerableStruct("ShroudofDarkness", TargetSelector.DamageType.Magical, true)
        };

        public static bool HasBuff(Obj_AI_Base target, TargetSelector.DamageType damageType, bool ignoreShields = true)
        {
            try
            {
                return (from i in Invulnerables
                    where !ignoreShields || !i.IsShield
                    where target.HasBuff(i.BuffName)
                    where i.DamageType == null || i.DamageType == damageType
                    select i.CustomCheck == null || i.CustomCheck(target, damageType)).FirstOrDefault();
            }
            catch
            {
                return false;
            }
        }
    }

    internal struct InvulnerableStruct
    {
        public InvulnerableStruct(string buffName, TargetSelector.DamageType? damageType, bool isShield,
            Func<Obj_AI_Base, TargetSelector.DamageType, bool> customCheck = null) : this()
        {
            BuffName = buffName;
            DamageType = damageType;
            IsShield = isShield;
            CustomCheck = customCheck;
        }

        public string BuffName { get; private set; }
        public TargetSelector.DamageType? DamageType { get; private set; }
        public bool IsShield { get; private set; }
        public Func<Obj_AI_Base, TargetSelector.DamageType, bool> CustomCheck { get; private set; }
    }
}