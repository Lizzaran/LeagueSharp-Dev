#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 ItemManager.cs is part of SFXChallenger.

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
using SFXChallenger.Enumerations;
using SFXChallenger.Wrappers;
using SFXLibrary;
using SFXLibrary.Logger;
using ItemData = LeagueSharp.Common.Data.ItemData;
using Orbwalking = SFXChallenger.Wrappers.Orbwalking;

#endregion

namespace SFXChallenger.Managers
{
    internal class CustomItem
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public Items.Item Item { get; set; }
        public ItemFlags Flags { get; set; }
        public CombatFlags CombatFlags { get; set; }
        public CastType CastType { get; set; }
        public EffectFlags EffectFlags { get; set; }
        public Damage.DamageItems Damage { get; set; }
        public float Range { get; set; }
        public float Delay { get; set; }
        public float Radius { get; set; }
        public float Speed { get; set; }
    }

    internal class ItemManager
    {
        private static Menu _menu;
        private static ItemFlags _itemFlags;
        public static CustomItem Youmuu;
        public static CustomItem Tiamat;
        public static CustomItem Hydra;
        public static CustomItem BilgewaterCutlass;
        public static CustomItem BladeRuinedKing;
        public static CustomItem HextechGunblade;
        public static CustomItem MikaelsCrucible;
        public static CustomItem LocketIronSolari;
        public static CustomItem Sightstone;
        public static CustomItem RubySightstone;
        public static List<CustomItem> Items;

        static ItemManager()
        {
            try
            {
                // Speed + Atk Speed
                Youmuu = new CustomItem
                {
                    Name = "youmuus-ghostblade",
                    DisplayName = Global.Lang.Get("MI_YoumuusGhostblade"),
                    Item = ItemData.Youmuus_Ghostblade.GetItem(),
                    Flags = ItemFlags.Offensive | ItemFlags.Flee,
                    CombatFlags = CombatFlags.Melee | CombatFlags.Ranged,
                    EffectFlags = EffectFlags.AttackSpeed | EffectFlags.MovementSpeed,
                    CastType = CastType.Self,
                    Range =
                        ObjectManager.Player.IsMeele
                            ? ObjectManager.Player.AttackRange * 3
                            : Orbwalking.GetRealAutoAttackRange(ObjectManager.Player)
                };

                // AOE damage, only melee
                Tiamat = new CustomItem
                {
                    Name = "tiamat",
                    DisplayName = Global.Lang.Get("MI_Tiamat"),
                    Item = ItemData.Tiamat_Melee_Only.GetItem(),
                    Flags = ItemFlags.Offensive,
                    CombatFlags = CombatFlags.Melee,
                    EffectFlags = EffectFlags.Damage,
                    CastType = CastType.Self,
                    Damage = Damage.DamageItems.Tiamat,
                    Range = ItemData.Tiamat_Melee_Only.GetItem().Range
                };

                // AOE damage, only melee
                Hydra = new CustomItem
                {
                    Name = "hydra",
                    DisplayName = Global.Lang.Get("MI_Hydra"),
                    Item = ItemData.Ravenous_Hydra_Melee_Only.GetItem(),
                    Flags = ItemFlags.Offensive,
                    CombatFlags = CombatFlags.Melee,
                    EffectFlags = EffectFlags.Damage,
                    CastType = CastType.Self,
                    Damage = Damage.DamageItems.Hydra,
                    Range = ItemData.Ravenous_Hydra_Melee_Only.GetItem().Range
                };
                // Slow + Damage
                BilgewaterCutlass = new CustomItem
                {
                    Name = "bilgewater-cutlass",
                    DisplayName = Global.Lang.Get("MI_BilgewaterCutlass"),
                    Item = ItemData.Bilgewater_Cutlass.GetItem(),
                    Flags = ItemFlags.Offensive | ItemFlags.Flee,
                    CombatFlags = CombatFlags.Melee | CombatFlags.Ranged,
                    EffectFlags = EffectFlags.Damage | EffectFlags.MovementSlow,
                    CastType = CastType.Target,
                    Damage = Damage.DamageItems.Bilgewater,
                    Range = ItemData.Bilgewater_Cutlass.GetItem().Range
                };

                // Slow + Damage
                BladeRuinedKing = new CustomItem
                {
                    Name = "blade-ruined-king",
                    DisplayName = Global.Lang.Get("MI_BladeRuinedKing"),
                    Item = ItemData.Blade_of_the_Ruined_King.GetItem(),
                    Flags = ItemFlags.Offensive | ItemFlags.Flee,
                    CombatFlags = CombatFlags.Melee | CombatFlags.Ranged,
                    EffectFlags = EffectFlags.Damage | EffectFlags.MovementSlow,
                    CastType = CastType.Target,
                    Damage = Damage.DamageItems.Botrk,
                    Range = ItemData.Blade_of_the_Ruined_King.GetItem().Range
                };

                // Damage + Slow
                HextechGunblade = new CustomItem
                {
                    Name = "hextech-gunblade",
                    DisplayName = Global.Lang.Get("MI_HextechGunblade"),
                    Item = ItemData.Hextech_Gunblade.GetItem(),
                    Flags = ItemFlags.Offensive | ItemFlags.Flee,
                    CombatFlags = CombatFlags.Melee | CombatFlags.Ranged,
                    EffectFlags = EffectFlags.Damage | EffectFlags.MovementSlow,
                    CastType = CastType.Target,
                    Damage = Damage.DamageItems.Hexgun,
                    Range = ItemData.Hextech_Gunblade.GetItem().Range
                };

                // Remove stun + heal
                MikaelsCrucible = new CustomItem
                {
                    Name = "mikaels-crucible",
                    DisplayName = Global.Lang.Get("MI_MikaelsCrucible"),
                    Item = ItemData.Mikaels_Crucible.GetItem(),
                    Flags = ItemFlags.Supportive | ItemFlags.Defensive,
                    CombatFlags = CombatFlags.Melee | CombatFlags.Ranged,
                    EffectFlags = EffectFlags.RemoveStun | EffectFlags.Heal,
                    CastType = CastType.Target,
                    Range = ItemData.Mikaels_Crucible.GetItem().Range
                };

                // AOE Shield
                LocketIronSolari = new CustomItem
                {
                    Name = "locket-iron-solari",
                    DisplayName = Global.Lang.Get("MI_LocketIronSolari"),
                    Item = ItemData.Locket_of_the_Iron_Solari.GetItem(),
                    Flags = ItemFlags.Supportive | ItemFlags.Defensive,
                    CombatFlags = CombatFlags.Melee | CombatFlags.Ranged,
                    EffectFlags = EffectFlags.Shield,
                    CastType = CastType.Self,
                    Range = ItemData.Locket_of_the_Iron_Solari.GetItem().Range
                };

                // Place wards
                Sightstone = new CustomItem
                {
                    Name = "sightstone",
                    DisplayName = Global.Lang.Get("MI_Sightstone"),
                    Item = ItemData.Sightstone.GetItem(),
                    Flags = ItemFlags.Supportive,
                    CombatFlags = CombatFlags.Melee | CombatFlags.Ranged,
                    CastType = CastType.Position,
                    Range = ItemData.Sightstone.GetItem().Range
                };

                // Place wards
                RubySightstone = new CustomItem
                {
                    Name = "ruby-sightstone",
                    DisplayName = Global.Lang.Get("MI_RubySightstone"),
                    Item = ItemData.Ruby_Sightstone.GetItem(),
                    Flags = ItemFlags.Supportive,
                    CombatFlags = CombatFlags.Melee | CombatFlags.Ranged,
                    CastType = CastType.Position,
                    Range = ItemData.Ruby_Sightstone.GetItem().Range
                };

                Items = new List<CustomItem>
                {
                    Youmuu,
                    Tiamat,
                    Hydra,
                    BilgewaterCutlass,
                    BladeRuinedKing,
                    HextechGunblade,
                    MikaelsCrucible,
                    LocketIronSolari,
                    Sightstone,
                    RubySightstone
                };

                MaxRange = Items.Max(s => s.Range);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static float MaxRange { get; set; }

        public static void AddToMenu(Menu menu, ItemFlags itemFlags)
        {
            try
            {
                _menu = menu;
                _itemFlags = itemFlags;

                foreach (var item in
                    Items.Where(
                        i =>
                            i.CombatFlags.HasFlag(ObjectManager.Player.IsMeele ? CombatFlags.Melee : CombatFlags.Ranged) &&
                            ((i.Flags & (_itemFlags)) != 0)))
                {
                    if (item.Flags.HasFlag(ItemFlags.Offensive) || item.Flags.HasFlag(ItemFlags.Flee))
                    {
                        var itemMenu = _menu.AddSubMenu(new Menu(item.DisplayName, _menu.Name + "." + item.Name));

                        itemMenu.AddItem(
                            new MenuItem(itemMenu.Name + ".min-enemies-range", Global.Lang.Get("MI_MinEnemiesRange"))
                                .SetValue(new Slider(1, 0, 5)));

                        if (item.Flags.HasFlag(ItemFlags.Flee))
                        {
                            itemMenu.AddItem(
                                new MenuItem(itemMenu.Name + ".flee", Global.Lang.Get("MI_UseFlee")).SetValue(true));
                        }
                        if (item.Flags.HasFlag(ItemFlags.Offensive))
                        {
                            itemMenu.AddItem(
                                new MenuItem(itemMenu.Name + ".combo", Global.Lang.Get("MI_UseCombo")).SetValue(true));
                        }
                    }
                }

                menu.AddItem(new MenuItem(menu.Name + ".enabled", Global.Lang.Get("G_Enabled")).SetValue(false));
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static float CalculateComboDamage(Obj_AI_Hero target)
        {
            if (_menu == null || !_menu.Item(_menu.Name + ".enabled").GetValue<bool>())
            {
                return 0f;
            }
            try
            {
                var distance = target.Distance(ObjectManager.Player.Position, true);
                if (distance >= Math.Pow(MaxRange, 2))
                {
                    return 0f;
                }
                return
                    (float)
                        Items.Where(
                            i =>
                                i.EffectFlags.HasFlag(EffectFlags.Damage) && ((i.Flags & (_itemFlags)) != 0) &&
                                _menu.Item(_menu.Name + "." + i.Name + ".combo").GetValue<bool>() && i.Item.IsOwned() &&
                                i.Item.IsReady() && distance <= Math.Pow(i.Range, 2) &&
                                ObjectManager.Player.CountEnemiesInRange(i.Range) >=
                                _menu.Item(_menu.Name + "." + i.Name + ".min-enemies-range").GetValue<Slider>().Value)
                            .Sum(item => ObjectManager.Player.GetItemDamage(target, item.Damage));
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return 0f;
        }

        public static void UseComboItems(Obj_AI_Hero target)
        {
            if (_menu == null || !_menu.Item(_menu.Name + ".enabled").GetValue<bool>())
            {
                return;
            }
            try
            {
                var distance = target.Distance(ObjectManager.Player.Position, true);
                if (distance >= Math.Pow(MaxRange, 2))
                {
                    return;
                }
                foreach (var item in
                    Items.Where(
                        i =>
                            ((i.Flags & (_itemFlags)) != 0) &&
                            _menu.Item(_menu.Name + "." + i.Name + ".combo").GetValue<bool>() && i.Item.IsOwned() &&
                            i.Item.IsReady() && distance <= Math.Pow(i.Range, 2) &&
                            ObjectManager.Player.CountEnemiesInRange(i.Range) >=
                            _menu.Item(_menu.Name + "." + i.Name + ".min-enemies-range").GetValue<Slider>().Value))
                {
                    switch (item.CastType)
                    {
                        case CastType.Target:
                            item.Item.Cast(target);
                            break;
                        case CastType.Self:
                            item.Item.Cast();
                            break;
                        case CastType.Position:
                            var prediction = Prediction.GetPrediction(target, item.Delay, item.Radius, item.Speed);
                            if (prediction.Hitchance >= HitChance.Medium)
                            {
                                item.Item.Cast(prediction.CastPosition);
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static void UseFleeItems()
        {
            if (_menu == null || !_menu.Item(_menu.Name + ".enabled").GetValue<bool>())
            {
                return;
            }
            try
            {
                foreach (var item in
                    Items.Where(
                        i =>
                            i.Flags.HasFlag(ItemFlags.Flee) &&
                            _menu.Item(_menu.Name + "." + i.Name + ".flee").GetValue<bool>() && i.Item.IsOwned() &&
                            i.Item.IsReady() && i.Item.IsOwned() && i.Item.IsReady() &&
                            ObjectManager.Player.CountEnemiesInRange(i.Range) >=
                            _menu.Item(_menu.Name + "." + i.Name + ".min-enemies-range").GetValue<Slider>().Value))
                {
                    if (item.CastType != CastType.Self)
                    {
                        var localItem = item;
                        foreach (var enemy in
                            GameObjects.EnemyHeroes.OrderByDescending(
                                e =>
                                    !Invulnerable.HasBuff(e) &&
                                    e.Position.Distance(ObjectManager.Player.Position, true) <
                                    Math.Pow(localItem.Range, 2)))
                        {
                            if (!enemy.HasBuffOfType(BuffType.Slow) && !enemy.HasBuffOfType(BuffType.Stun) &&
                                !enemy.HasBuffOfType(BuffType.Fear) && !enemy.HasBuffOfType(BuffType.Flee) &&
                                !enemy.HasBuffOfType(BuffType.Charm) && !enemy.HasBuffOfType(BuffType.Snare) &&
                                !enemy.HasBuffOfType(BuffType.Invulnerability) && !enemy.HasBuffOfType(BuffType.Knockup) &&
                                !enemy.HasBuffOfType(BuffType.Polymorph) && !enemy.HasBuffOfType(BuffType.Sleep) &&
                                !enemy.HasBuffOfType(BuffType.Taunt))
                            {
                                switch (localItem.CastType)
                                {
                                    case CastType.Target:
                                        localItem.Item.Cast(enemy);
                                        break;
                                    case CastType.Position:
                                        var prediction = Prediction.GetPrediction(
                                            enemy, localItem.Delay, localItem.Radius, localItem.Speed);
                                        if (prediction.Hitchance >= HitChance.Medium)
                                        {
                                            localItem.Item.Cast(prediction.CastPosition);
                                        }
                                        break;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (ObjectManager.Player.CountEnemiesInRange(item.Range) >
                            _menu.Item(_menu.Name + "." + item.Name + ".min-enemies-range").GetValue<Slider>().Value)
                        {
                            item.Item.Cast();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }
}