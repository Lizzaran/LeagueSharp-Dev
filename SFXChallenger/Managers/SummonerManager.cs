#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 SummonerManager.cs is part of SFXChallenger.

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

namespace SFXChallenger.Managers
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Enumerations;
    using LeagueSharp;
    using LeagueSharp.Common;
    using SFXLibrary.Logger;
    using SharpDX;

    #endregion

    internal class SummonerSpell
    {
        public string Name { get; set; }
        public float Range { get; set; }
        public CastType CastType { get; set; }

        public SpellSlot Slot
        {
            get { return ObjectManager.Player.GetSpellSlot(Name); }
        }
    }

    internal static class SummonerManager
    {
        private static Menu _menu;
        public static SummonerSpell BlueSmite;
        public static SummonerSpell RedSmite;
        public static SummonerSpell Ghost;
        public static SummonerSpell Clarity;
        public static SummonerSpell Heal;
        public static SummonerSpell Barrier;
        public static SummonerSpell Exhaust;
        public static SummonerSpell Cleanse;
        public static SummonerSpell Flash;
        public static SummonerSpell Ignite;
        public static SummonerSpell Smite;
        public static List<SummonerSpell> SummonerSpells;

        static SummonerManager()
        {
            // ReSharper disable once StringLiteralTypo
            BlueSmite = new SummonerSpell {Name = "s5_summonersmiteplayerganker", CastType = CastType.Target, Range = 750f};
            RedSmite = new SummonerSpell {Name = "s5_summonersmiteduel", CastType = CastType.Target, Range = 750f};
            Ghost = new SummonerSpell {Name = "SummonerHaste", CastType = CastType.Self, Range = float.MaxValue};
            Clarity = new SummonerSpell {Name = "SummonerMana", CastType = CastType.Self, Range = 600f};
            Heal = new SummonerSpell {Name = "SummonerHeal", CastType = CastType.Self, Range = 850f};
            Barrier = new SummonerSpell {Name = "SummonerBarrier", CastType = CastType.Self, Range = float.MaxValue};
            Exhaust = new SummonerSpell {Name = "SummonerExhaust", CastType = CastType.Target, Range = 650f};
            Cleanse = new SummonerSpell {Name = "SummonerBoost", CastType = CastType.Self, Range = float.MaxValue};
            Flash = new SummonerSpell {Name = "SummonerFlash", CastType = CastType.Position, Range = 425f};
            Ignite = new SummonerSpell {Name = "SummonerDot", CastType = CastType.Target, Range = 600f};
            Smite = new SummonerSpell {Name = "SummonerSmite", CastType = CastType.Target, Range = 750f};

            SummonerSpells = new List<SummonerSpell> {Ghost, Clarity, Heal, Barrier, Exhaust, Cleanse, Flash, Ignite, Smite, BlueSmite, RedSmite};
        }

        public static bool IsReady(this SummonerSpell spell)
        {
            return (spell.Slot != SpellSlot.Unknown && spell.Slot.IsReady());
        }

        public static List<SummonerSpell> AvailableSummoners()
        {
            return SummonerSpells.Where(ss => ss.Exists()).ToList();
        }

        public static bool Exists(this SummonerSpell spell)
        {
            return spell.Slot != SpellSlot.Unknown;
        }

        public static void Cast(this SummonerSpell spell)
        {
            ObjectManager.Player.Spellbook.CastSpell(spell.Slot);
        }

        public static void Cast(this SummonerSpell spell, Obj_AI_Hero target)
        {
            ObjectManager.Player.Spellbook.CastSpell(spell.Slot, target);
        }

        public static void Cast(this SummonerSpell spell, Vector3 position)
        {
            ObjectManager.Player.Spellbook.CastSpell(spell.Slot, position);
        }

        public static float CalculateBlueSmiteDamage()
        {
            return 20 + ObjectManager.Player.Level*8;
        }

        public static float CalculateRedSmiteDamage()
        {
            return 54 + ObjectManager.Player.Level*6;
        }

        public static float CalculateComboDamage(Obj_AI_Hero target)
        {
            if (_menu == null)
                return 0f;
            try
            {
                var damage = 0f;
                if (_menu.Item(_menu.Name + ".ignite").GetValue<bool>() && Ignite.Exists() && Ignite.IsReady() &&
                    target.Position.Distance(ObjectManager.Player.Position) <= Ignite.Range)
                {
                    damage += (float) ObjectManager.Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);
                }
                if (_menu.Item(_menu.Name + ".smite").GetValue<bool>())
                {
                    if (BlueSmite.Exists() && BlueSmite.IsReady() && target.Position.Distance(ObjectManager.Player.Position) <= BlueSmite.Range)
                    {
                        damage += CalculateBlueSmiteDamage();
                    }
                    else if (RedSmite.Exists() && RedSmite.IsReady() && target.Position.Distance(ObjectManager.Player.Position) <= RedSmite.Range)
                    {
                        damage += CalculateRedSmiteDamage();
                    }
                }
                return damage;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return 0f;
        }

        public static void UseComboSummoners(Obj_AI_Hero target)
        {
            try
            {
                if (_menu.Item(_menu.Name + ".ignite").GetValue<bool>() && Ignite.Exists() && Ignite.IsReady() &&
                    target.Position.Distance(ObjectManager.Player.Position) <= Ignite.Range)
                {
                    Ignite.Cast(target);
                }
                if (_menu.Item(_menu.Name + ".smite").GetValue<bool>())
                {
                    if (BlueSmite.Exists() && BlueSmite.IsReady() && target.Position.Distance(ObjectManager.Player.Position) <= BlueSmite.Range)
                    {
                        BlueSmite.Cast(target);
                    }
                    else if (RedSmite.Exists() && RedSmite.IsReady() && target.Position.Distance(ObjectManager.Player.Position) <= RedSmite.Range)
                    {
                        RedSmite.Cast(target);
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static void AddToMenu(Menu menu)
        {
            try
            {
                _menu = menu;

                menu.AddItem(new MenuItem(_menu.Name + ".ignite", Global.Lang.Get("MS_UseIgnite")).SetValue(true));
                menu.AddItem(new MenuItem(_menu.Name + ".smite", Global.Lang.Get("MS_UseSmite")).SetValue(true));
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }
}