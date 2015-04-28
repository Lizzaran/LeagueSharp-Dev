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