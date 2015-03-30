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

namespace SFXUtility.Features.Activators
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Classes;
    using LeagueSharp;
    using LeagueSharp.Common;
    using LeagueSharp.CommonEx.Core.Events;
    using SFXLibrary.IoCContainer;
    using SFXLibrary.Logger;

    #endregion

    internal class Potion : Base
    {
        private Activators _parent;

        private List<PotionStruct> _potions = new List<PotionStruct>
        {
            new PotionStruct("ItemCrystalFlask", ItemId.Crystalline_Flask, 1, 1, new[] {PotionType.Health, PotionType.Mana}),
            new PotionStruct("RegenerationPotion", ItemId.Health_Potion, 0, 2, new[] {PotionType.Health}),
            new PotionStruct("FlaskOfCrystalWater", ItemId.Mana_Potion, 0, 3, new[] {PotionType.Mana})
        };

        public Potion(IContainer container) : base(container)
        {
            Load.OnLoad += OnLoad;
        }

        public override bool Enabled
        {
            get { return _parent != null && _parent.Enabled && Menu != null && Menu.Item(Name + "Enabled").GetValue<bool>(); }
        }

        public override string Name
        {
            get { return "Potion"; }
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

        private void OnParentInitialized(object sender, EventArgs eventArgs)
        {
            try
            {
                if (_parent.Menu == null)
                    return;

                _potions = _potions.OrderBy(x => x.Priority).ToList();
                Menu = new Menu(Name, BaseName + Name);
                var healthMenu = new Menu("Health", Name + "Health");
                healthMenu.AddItem(new MenuItem(healthMenu.Name + "Potion", "Use Health Potion").SetValue(false));
                healthMenu.AddItem(new MenuItem(healthMenu.Name + "Percent", "HP Trigger Percent").SetValue(new Slider(60)));

                var manaMenu = new Menu("Mana", Name + "Mana");
                manaMenu.AddItem(new MenuItem(manaMenu.Name + "Potion", "Use Mana Potion").SetValue(false));
                manaMenu.AddItem(new MenuItem(manaMenu.Name + "Percent", "MP Trigger Percent").SetValue(new Slider(60)));

                Menu.AddSubMenu(healthMenu);
                Menu.AddSubMenu(manaMenu);

                Menu.AddItem(new MenuItem(Name + "MinEnemyDistance", "Min Enemy Distance").SetValue(new Slider(600, 0, 1500)));

                Menu.AddItem(new MenuItem(Name + "Enabled", "Enabled").SetValue(false));
                _parent.Menu.AddSubMenu(Menu);
                HandleEvents(_parent);
                RaiseOnInitialized();
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
            }
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
            return _potions.Where(potion => potion.TypeList.Contains(type)).Any(potion => ObjectManager.Player.HasBuff(potion.BuffName, true));
        }

        private void OnLoad(EventArgs args)
        {
            try
            {
                if (IoC.IsRegistered<Activators>())
                {
                    _parent = IoC.Resolve<Activators>();
                    if (_parent.Initialized)
                        OnParentInitialized(null, null);
                    else
                        _parent.OnInitialized += OnParentInitialized;
                }
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
            }
        }

        private void OnGameUpdate(EventArgs args)
        {
            try
            {
                if (ObjectManager.Player.CountEnemiesInRange(Menu.Item(Name + "MinEnemyDistance").GetValue<Slider>().Value) == 0)
                    return;

                if (Menu.Item(Name + "HealthPotion").GetValue<bool>())
                {
                    if (ObjectManager.Player.HealthPercent <= Menu.Item(Name + "HealthPercent").GetValue<Slider>().Value)
                    {
                        var healthSlot = GetPotionSlot(PotionType.Health);
                        if (healthSlot != null && !IsBuffActive(PotionType.Health))
                            ObjectManager.Player.Spellbook.CastSpell(healthSlot.SpellSlot);
                    }
                }

                if (Menu.Item(Name + "ManaPotion").GetValue<bool>())
                {
                    if (ObjectManager.Player.ManaPercent <= Menu.Item(Name + "ManaPercent").GetValue<Slider>().Value)
                    {
                        var manaSlot = GetPotionSlot(PotionType.Mana);
                        if (manaSlot != null && !IsBuffActive(PotionType.Mana))
                            ObjectManager.Player.Spellbook.CastSpell(manaSlot.SpellSlot);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
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