#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Invulnerable.cs is part of SFXChallenger.

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
using SFXLibrary.Logger;

#endregion

namespace SFXChallenger.Wrappers
{

    #region

    #endregion

    internal static class Invulnerable
    {
        // ReSharper disable StringLiteralTypo
        private static readonly List<InvulnerableStruct> Invulnerables = new List<InvulnerableStruct>
        {
            new InvulnerableStruct(
                "FerociousHowl", null, false,
                (target, type) =>
                    ObjectManager.Player.CountEnemiesInRange(Orbwalking.GetRealAutoAttackRange(ObjectManager.Player)) >
                    1),
            new InvulnerableStruct("UndyingRage", null, false, (target, type) => target.HealthPercent < 5),
            new InvulnerableStruct("JudicatorIntervention", null, false),
            new InvulnerableStruct("BlackShield", LeagueSharp.Common.TargetSelector.DamageType.Magical, true),
            new InvulnerableStruct("BansheesVeil", LeagueSharp.Common.TargetSelector.DamageType.Magical, true),
            new InvulnerableStruct("SivirShield", LeagueSharp.Common.TargetSelector.DamageType.Magical, true),
            new InvulnerableStruct("ShroudofDarkness", LeagueSharp.Common.TargetSelector.DamageType.Magical, true)
        };

        // ReSharper restore StringLiteralTypo
        public static bool HasBuff(Obj_AI_Base target,
            LeagueSharp.Common.TargetSelector.DamageType damageType = LeagueSharp.Common.TargetSelector.DamageType.True,
            bool ignoreShields = true)
        {
            try
            {
                return target.HasBuffOfType(BuffType.Invulnerability) || (from i in Invulnerables
                    where !ignoreShields || !i.IsShield
                    where target.HasBuff(i.BuffName)
                    where i.DamageType == null || i.DamageType == damageType
                    select i.CustomCheck == null || i.CustomCheck(target, damageType)).Any();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return false;
        }
    }

    internal struct InvulnerableStruct
    {
        public InvulnerableStruct(string buffName,
            LeagueSharp.Common.TargetSelector.DamageType? damageType,
            bool isShield,
            Func<Obj_AI_Base, LeagueSharp.Common.TargetSelector.DamageType, bool> customCheck = null) : this()
        {
            BuffName = buffName;
            DamageType = damageType;
            IsShield = isShield;
            CustomCheck = customCheck;
        }

        public string BuffName { get; private set; }
        public LeagueSharp.Common.TargetSelector.DamageType? DamageType { get; private set; }
        public bool IsShield { get; private set; }
        public Func<Obj_AI_Base, LeagueSharp.Common.TargetSelector.DamageType, bool> CustomCheck { get; private set; }
    }
}