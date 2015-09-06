#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 HitchanceManager.cs is part of SFXChallenger.

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
using Spell = SFXChallenger.Wrappers.Spell;

#endregion

namespace SFXChallenger.Managers
{
    internal static class HitchanceManager
    {
        private static readonly Dictionary<string, Menu> Menues = new Dictionary<string, Menu>();

        public static void AddToMenu(Menu menu, string uniqueId, Dictionary<string, HitChance> hitChances)
        {
            try
            {
                if (Menues.ContainsKey(uniqueId))
                {
                    throw new ArgumentException(
                        string.Format("HitchanceManager: UniqueID \"{0}\" already exist.", uniqueId));
                }

                foreach (var hit in hitChances)
                {
                    menu.AddItem(
                        new MenuItem(menu.Name + "." + hit.Key.ToLower(), hit.Key.ToUpper()).SetValue(
                            new StringList(
                                new[] { "Medium", "High", "Very High" },
                                hit.Value == HitChance.Medium ? 0 : (hit.Value == HitChance.High ? 1 : 2))));
                }

                Menues[uniqueId] = menu;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static HitChance Get(string uniqueId, string slot)
        {
            try
            {
                Menu menu;
                if (Menues.TryGetValue(uniqueId, out menu))
                {
                    switch (menu.Item(menu.Name + "." + slot.ToLower()).GetValue<StringList>().SelectedIndex)
                    {
                        case 0:
                            return HitChance.Medium;
                        case 1:
                            return HitChance.High;
                        case 2:
                            return HitChance.VeryHigh;
                    }
                }
                throw new KeyNotFoundException(string.Format("HitchanceManager: UniqueID \"{0}\" not found.", uniqueId));
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return HitChance.High;
        }

        public static HitChance GetHitChance(this Spell spell, string uniqueId)
        {
            try
            {
                if (spell != null && spell.Slot != SpellSlot.Unknown)
                {
                    return Get(uniqueId, spell.Slot.ToString());
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return HitChance.High;
        }
    }
}