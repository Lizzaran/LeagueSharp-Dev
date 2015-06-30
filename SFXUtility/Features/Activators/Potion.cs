#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Potion.cs is part of SFXUtility.

 SFXUtility is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXUtility is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXUtility. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXLibrary;
using SFXLibrary.Extensions.NET;
using SFXLibrary.Logger;
using SFXUtility.Classes;
using ItemData = LeagueSharp.Common.Data.ItemData;

#endregion

namespace SFXUtility.Features.Activators
{
    internal class Potion : Child<Activators>
    {
        private List<PotionStruct> _potions;
        public Potion(SFXUtility sfx) : base(sfx) {}

        public override string Name
        {
            get { return Global.Lang.Get("F_Potion"); }
        }

        protected override void OnEnable()
        {
            Game.OnUpdate += OnGameUpdate;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            Game.OnUpdate -= OnGameUpdate;
            base.OnDisable();
        }

        protected override void OnLoad()
        {
            try
            {
                Menu = new Menu(Name, Name);
                var healthMenu = new Menu(Global.Lang.Get("G_Health"), Name + "Health");
                healthMenu.AddItem(
                    new MenuItem(
                        healthMenu.Name + "Percent", Global.Lang.Get("G_Health") + " " + Global.Lang.Get("G_Percent"))
                        .SetValue(new Slider(60)));
                healthMenu.AddItem(
                    new MenuItem(healthMenu.Name + "Enabled", Global.Lang.Get("G_Enabled")).SetValue(false));

                var manaMenu = new Menu(Global.Lang.Get("G_Mana"), Name + "Mana");
                manaMenu.AddItem(
                    new MenuItem(
                        manaMenu.Name + "Percent", Global.Lang.Get("G_Mana") + " " + Global.Lang.Get("G_Percent"))
                        .SetValue(new Slider(60)));
                manaMenu.AddItem(new MenuItem(manaMenu.Name + "Enabled", Global.Lang.Get("G_Enabled")).SetValue(false));

                Menu.AddSubMenu(healthMenu);
                Menu.AddSubMenu(manaMenu);

                Menu.AddItem(
                    new MenuItem(
                        Name + "MinEnemyDistance",
                        Global.Lang.Get("G_Minimum") + " " + Global.Lang.Get("G_Enemy") + " " +
                        Global.Lang.Get("G_Distance")).SetValue(new Slider(1000, 0, 1500)));

                Menu.AddItem(new MenuItem(Name + "Enabled", Global.Lang.Get("G_Enabled")).SetValue(false));

                Parent.Menu.AddSubMenu(Menu);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected override void OnInitialize()
        {
            _potions =
                new List<PotionStruct>
                {
                    new PotionStruct(
                        "ItemCrystalFlask", (ItemId) ItemData.Crystalline_Flask.Id, 1, 1,
                        new[] { PotionType.Health, PotionType.Mana }),
                    new PotionStruct(
                        "RegenerationPotion", (ItemId) ItemData.Health_Potion.Id, 0, 2, new[] { PotionType.Health }),
                    new PotionStruct(
                        "ItemMiniRegenPotion", (ItemId) ItemData.Total_Biscuit_of_Rejuvenation.Id, 0, 3,
                        new[] { PotionType.Health }),
                    new PotionStruct(
                        "ItemMiniRegenPotion", (ItemId) ItemData.Total_Biscuit_of_Rejuvenation2.Id, 0, 4,
                        new[] { PotionType.Health }),
                    new PotionStruct(
                        "FlaskOfCrystalWater", (ItemId) ItemData.Mana_Potion.Id, 0, 5, new[] { PotionType.Mana })
                }
                    .OrderBy(x => x.Priority).ToList();

            base.OnInitialize();
        }

        private InventorySlot GetPotionSlot(PotionType type)
        {
            return (from potion in _potions
                where potion.TypeList.Contains(type)
                from item in ObjectManager.Player.InventoryItems
                where item.Id == potion.ItemId && item.Charges >= potion.MinCharges
                select item).FirstOrDefault();
        }

        private bool IsBuffActive(PotionType type)
        {
            return
                _potions.Where(potion => potion.TypeList.Contains(type))
                    .Any(
                        potion =>
                            ObjectManager.Player.Buffs.Any(
                                b => b.Name.Equals(potion.BuffName, StringComparison.OrdinalIgnoreCase)));
        }

        private void OnGameUpdate(EventArgs args)
        {
            try
            {
                if (ObjectManager.Player.IsDead || ObjectManager.Player.InFountain() ||
                    ObjectManager.Player.Buffs.Any(
                        b =>
                            b.Name.Contains("Recall", StringComparison.OrdinalIgnoreCase) ||
                            b.Name.Contains("Teleport", StringComparison.OrdinalIgnoreCase)))
                {
                    return;
                }

                var enemyDist = Menu.Item(Name + "MinEnemyDistance").GetValue<Slider>().Value;
                if (enemyDist != 0 &&
                    !GameObjects.EnemyHeroes.Any(e => e.Position.Distance(ObjectManager.Player.Position) <= enemyDist))
                {
                    return;
                }

                if (Menu.Item(Name + "HealthEnabled").GetValue<bool>())
                {
                    if (ObjectManager.Player.HealthPercent <= Menu.Item(Name + "HealthPercent").GetValue<Slider>().Value)
                    {
                        var healthSlot = GetPotionSlot(PotionType.Health);
                        if (healthSlot != null && !IsBuffActive(PotionType.Health))
                        {
                            ObjectManager.Player.Spellbook.CastSpell(healthSlot.SpellSlot);
                        }
                    }
                }

                if (Menu.Item(Name + "ManaEnabled").GetValue<bool>())
                {
                    if (ObjectManager.Player.ManaPercent <= Menu.Item(Name + "ManaPercent").GetValue<Slider>().Value)
                    {
                        var manaSlot = GetPotionSlot(PotionType.Mana);
                        if (manaSlot != null && !IsBuffActive(PotionType.Mana))
                        {
                            ObjectManager.Player.Spellbook.CastSpell(manaSlot.SpellSlot);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private enum PotionType
        {
            Health,
            Mana
        };

        private struct PotionStruct
        {
            public readonly string BuffName;
            public readonly ItemId ItemId;
            public readonly int MinCharges;
            public readonly int Priority;
            public readonly PotionType[] TypeList;

            public PotionStruct(string buffName, ItemId itemId, int minCharges, int priority, PotionType[] typeList)
            {
                BuffName = buffName;
                ItemId = itemId;
                MinCharges = minCharges;
                Priority = priority;
                TypeList = typeList;
            }
        }
    }
}