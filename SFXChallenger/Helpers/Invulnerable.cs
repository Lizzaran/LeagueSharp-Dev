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
using LeagueSharp;
using LeagueSharp.Common;
using SFXChallenger.Library.Logger;
using DamageType = SFXChallenger.Enumerations.DamageType;
using Orbwalking = SFXChallenger.Wrappers.Orbwalking;

#endregion

namespace SFXChallenger.Helpers
{
    public class Invulnerable
    {
        public static readonly HashSet<Item> Items = new HashSet<Item>
        {
            new Item(
                "Alistar", "FerociousHowl", null, false,
                (target, type) =>
                    ObjectManager.Player.CountEnemiesInRange(Orbwalking.GetRealAutoAttackRange(target)) > 1),
            new Item(
                "MasterYi", "Meditate", null, false,
                (target, type) =>
                    ObjectManager.Player.CountEnemiesInRange(Orbwalking.GetRealAutoAttackRange(target)) > 1),
            new Item("Tryndamere", "UndyingRage", null, false, (target, type) => target.HealthPercent < 5),
            new Item("Kayle", "JudicatorIntervention", null, false),
            new Item("Fizz", "fizztrickslamsounddummy", null, false),
            new Item("Vladimir", "VladimirSanguinePool", null, false),
            new Item(null, "BlackShield", DamageType.Magical, true),
            new Item(null, "BansheesVeil", DamageType.Magical, true),
            new Item("Sivir", "SivirE", null, true),
            new Item("Nocturne", "ShroudofDarkness", null, true)
        };

        public static bool Check(Obj_AI_Hero target, DamageType damageType = DamageType.True, bool ignoreShields = true)
        {
            try
            {
                if (target.HasBuffOfType(BuffType.Invulnerability) || target.IsInvulnerable)
                {
                    return true;
                }
                foreach (var invulnerable in Items)
                {
                    if (invulnerable.Champion == null || invulnerable.Champion == target.ChampionName)
                    {
                        if (invulnerable.DamageType == null || invulnerable.DamageType == damageType)
                        {
                            if (!ignoreShields && invulnerable.IsShield && target.HasBuff(invulnerable.BuffName))
                            {
                                return true;
                            }
                            if (invulnerable.CustomCheck != null && CustomCheck(invulnerable, target, damageType))
                            {
                                return true;
                            }
                        }
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return false;
        }

        private static bool CustomCheck(Item invulnerable, Obj_AI_Hero target, DamageType damageType)
        {
            try
            {
                if (invulnerable != null)
                {
                    if (invulnerable.CustomCheck(target, damageType))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return false;
        }

        public class Item
        {
            public Item(string champion,
                string buffName,
                DamageType? damageType,
                bool isShield,
                Func<Obj_AI_Base, DamageType, bool> customCheck = null)
            {
                Champion = champion;
                BuffName = buffName;
                DamageType = damageType;
                IsShield = isShield;
                CustomCheck = customCheck;
            }

            public string Champion { get; set; }
            public string BuffName { get; private set; }
            public DamageType? DamageType { get; private set; }
            public bool IsShield { get; private set; }
            public Func<Obj_AI_Base, DamageType, bool> CustomCheck { get; private set; }
        }
    }
}