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
    using SFXLibrary;
    using SFXLibrary.Extensions.NET;
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

        public override bool Enabled
        {
            get { return !Unloaded && _parent != null && _parent.Enabled && Menu != null && Menu.Item(Name + "Enabled").GetValue<bool>(); }
        }

        public override string Name
        {
            get { return Language.Get("F_Potion"); }
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
                Menu = new Menu(Name, Name);
                var healthMenu = new Menu(Language.Get("G_Health"), Name + "Health");
                healthMenu.AddItem(
                    new MenuItem(healthMenu.Name + "Percent", Language.Get("G_Health") + " " + Language.Get("G_Percent")).SetValue(new Slider(60)));
                healthMenu.AddItem(new MenuItem(healthMenu.Name + "Enabled", Language.Get("G_Enabled")).SetValue(false));

                var manaMenu = new Menu(Language.Get("G_Mana"), Name + "Mana");
                manaMenu.AddItem(
                    new MenuItem(manaMenu.Name + "Percent", Language.Get("G_Mana") + " " + Language.Get("G_Percent")).SetValue(new Slider(60)));
                manaMenu.AddItem(new MenuItem(manaMenu.Name + "Enabled", Language.Get("G_Enabled")).SetValue(false));

                Menu.AddSubMenu(healthMenu);
                Menu.AddSubMenu(manaMenu);

                Menu.AddItem(
                    new MenuItem(Name + "MinEnemyDistance",
                        Language.Get("G_Minimum") + " " + Language.Get("G_Enemy") + " " + Language.Get("G_Distance")).SetValue(new Slider(1000, 0,
                            1500)));

                Menu.AddItem(new MenuItem(Name + "Enabled", Language.Get("G_Enabled")).SetValue(false));

                _parent.Menu.AddSubMenu(Menu);

                HandleEvents(_parent);
                RaiseOnInitialized();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
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

        protected override void OnGameLoad(EventArgs args)
        {
            try
            {
                if (Global.IoC.IsRegistered<Activators>())
                {
                    _parent = Global.IoC.Resolve<Activators>();
                    if (_parent.Initialized)
                        OnParentInitialized(null, null);
                    else
                        _parent.OnInitialized += OnParentInitialized;
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
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
                    return;

                var enemyDist = Menu.Item(Name + "MinEnemyDistance").GetValue<Slider>().Value;
                if (enemyDist != 0 && !HeroManager.Enemies.Any(e => e.Position.Distance(ObjectManager.Player.Position) <= enemyDist))
                    return;

                if (Menu.Item(Name + "HealthEnabled").GetValue<bool>())
                {
                    if (ObjectManager.Player.HealthPercent <= Menu.Item(Name + "HealthPercent").GetValue<Slider>().Value)
                    {
                        var healthSlot = GetPotionSlot(PotionType.Health);
                        if (healthSlot != null && !IsBuffActive(PotionType.Health))
                            ObjectManager.Player.Spellbook.CastSpell(healthSlot.SpellSlot);
                    }
                }

                if (Menu.Item(Name + "ManaEnabled").GetValue<bool>())
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