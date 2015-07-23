#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Revealer.cs is part of SFXUtility.

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
using SFXLibrary.Logger;
using SFXUtility.Classes;
using SharpDX;
using ItemData = LeagueSharp.Common.Data.ItemData;

#endregion

namespace SFXUtility.Features.Activators
{
    internal class Revealer : Child<Activators>
    {
        private const float MaxRange = 600f;
        private const float Delay = 2f;
        private float _lastReveal;
        private Obj_AI_Hero _leBlanc;
        private Obj_AI_Hero _rengar;
        private Obj_AI_Hero _vayne;
        private HashSet<SpellData> spellList = new HashSet<SpellData>();
        public Revealer(SFXUtility sfx) : base(sfx) {}

        public override string Name
        {
            get { return Global.Lang.Get("F_Revealer"); }
        }

        protected override void OnEnable()
        {
            GameObject.OnCreate += OnGameObjectCreate;
            Obj_AI_Base.OnProcessSpellCast += OnObjAiBaseProcessSpellCast;
            AttackableUnit.OnLeaveTeamVisiblity += OnAttackableUnitLeaveTeamVisiblity;

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            GameObject.OnCreate -= OnGameObjectCreate;
            Obj_AI_Base.OnProcessSpellCast -= OnObjAiBaseProcessSpellCast;
            AttackableUnit.OnLeaveTeamVisiblity -= OnAttackableUnitLeaveTeamVisiblity;

            base.OnDisable();
        }

        protected override void OnLoad()
        {
            try
            {
                Menu = new Menu(Name, Name);

                Menu.AddItem(new MenuItem(Name + "Bush", Global.Lang.Get("Revealer_Bush")).SetValue(false));
                Menu.AddItem(
                    new MenuItem(Name + "Hotkey", Global.Lang.Get("G_Hotkey")).SetValue(
                        new KeyBind(32, KeyBindType.Press)));
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
            try
            {
                spellList = new HashSet<SpellData>
                {
                    new SpellData("Akali", SpellSlot.W),
                    new SpellData("Rengar", SpellSlot.R, true),
                    new SpellData("KhaZix", SpellSlot.R),
                    new SpellData("KhaZix", SpellSlot.R, false, "khazixrlong"),
                    new SpellData("Monkeyking", SpellSlot.W),
                    new SpellData("Shaco", SpellSlot.Q),
                    new SpellData("Talon", SpellSlot.R),
                    new SpellData("LeBlanc", SpellSlot.R, true),
                    new SpellData("Vayne", SpellSlot.Q, true),
                    new SpellData("Twitch", SpellSlot.Q)
                };

                var menuList =
                    spellList.OrderBy(s => s.Hero).GroupBy(s => s.Hero).Select(h => new { Hero = h.Key }).ToList();

                var invisibleMenu = new Menu(Global.Lang.Get("Revealer_Invisible"), Name + "Invisible");
                foreach (var spell in menuList)
                {
                    invisibleMenu.AddItem(
                        new MenuItem(invisibleMenu.Name + spell.Hero.ToLower(), spell.Hero).SetValue(true));
                }
                Menu.AddSubMenu(invisibleMenu);

                _rengar =
                    GameObjects.EnemyHeroes.FirstOrDefault(
                        e => e.ChampionName.Equals("Rengar", StringComparison.OrdinalIgnoreCase));
                _vayne =
                    GameObjects.EnemyHeroes.FirstOrDefault(
                        e => e.ChampionName.Equals("Vayne", StringComparison.OrdinalIgnoreCase));
                _leBlanc =
                    GameObjects.EnemyHeroes.FirstOrDefault(
                        e => e.ChampionName.Equals("Leblanc", StringComparison.OrdinalIgnoreCase));

                base.OnInitialize();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnAttackableUnitLeaveTeamVisiblity(AttackableUnit sender, EventArgs args)
        {
            try
            {
                var hero = sender as Obj_AI_Hero;
                if (hero == null || !hero.IsEnemy || !Menu.Item(Name + "Bush").GetValue<bool>() ||
                    !Menu.Item(Name + "Hotkey").GetValue<KeyBind>().Active)
                {
                    return;
                }
                var pos = GetGrassPosition(hero.Position);
                if (!pos.Equals(Vector3.Zero))
                {
                    CastLogic(pos, true);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private Vector3 GetGrassPosition(Vector3 pos)
        {
            try
            {
                var dist = MaxRange - ObjectManager.Player.Distance(pos);
                if (dist < 0)
                {
                    return Vector3.Zero;
                }
                for (var i = 10; i <= dist; i = i + 5)
                {
                    var circle = new Geometry.Polygon.Circle(pos, i);
                    foreach (var point in circle.Points)
                    {
                        if (NavMesh.GetCollisionFlags(point.To3D2()).HasFlag(CollisionFlags.Grass))
                        {
                            return point.To3D2();
                        }
                    }
                }
                return Vector3.Zero;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return Vector3.Zero;
        }

        private void CastLogic(Vector3 pos, bool bush)
        {
            try
            {
                if (pos.Distance(ObjectManager.Player.Position) > MaxRange || _lastReveal + Delay > Game.Time)
                {
                    return;
                }
                if (!bush)
                {
                    if (
                        GameObjects.AllyMinions.Any(
                            m =>
                                m.Name.Equals("VisionWard", StringComparison.OrdinalIgnoreCase) &&
                                ObjectManager.Player.Distance(m) < 400f))
                    {
                        return;
                    }
                }
                var slot = GetWardSlot(bush);
                if (slot != SpellSlot.Unknown)
                {
                    ObjectManager.Player.Spellbook.CastSpell(slot, pos);
                    _lastReveal = Game.Time;
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private SpellSlot GetWardSlot(bool bush)
        {
            try
            {
                if (!bush)
                {
                    if (ItemData.Oracles_Lens_Trinket.GetItem().IsOwned() &&
                        ItemData.Oracles_Lens_Trinket.GetItem().IsReady())
                    {
                        return ItemData.Oracles_Lens_Trinket.GetItem().Slots.FirstOrDefault();
                    }
                    if (ItemData.Greater_Vision_Totem_Trinket.GetItem().IsOwned() &&
                        ItemData.Greater_Vision_Totem_Trinket.GetItem().IsReady())
                    {
                        return ItemData.Greater_Vision_Totem_Trinket.GetItem().Slots.FirstOrDefault();
                    }
                    if (ItemData.Vision_Ward.GetItem().IsOwned() && ItemData.Vision_Ward.GetItem().IsReady())
                    {
                        return ItemData.Vision_Ward.GetItem().Slots.FirstOrDefault();
                    }
                }
                else
                {
                    if (ItemData.Warding_Totem_Trinket.GetItem().IsOwned() &&
                        ItemData.Warding_Totem_Trinket.GetItem().IsReady())
                    {
                        return ItemData.Warding_Totem_Trinket.GetItem().Slots.FirstOrDefault();
                    }
                    if (ItemData.Greater_Stealth_Totem_Trinket.GetItem().IsOwned() &&
                        ItemData.Greater_Stealth_Totem_Trinket.GetItem().IsReady())
                    {
                        return ItemData.Greater_Stealth_Totem_Trinket.GetItem().Slots.FirstOrDefault();
                    }
                    if (ItemData.Scrying_Orb_Trinket.GetItem().IsOwned() &&
                        ItemData.Scrying_Orb_Trinket.GetItem().IsReady())
                    {
                        return ItemData.Scrying_Orb_Trinket.GetItem().Slots.FirstOrDefault();
                    }
                    if (ItemData.Farsight_Orb_Trinket.GetItem().IsOwned() &&
                        ItemData.Farsight_Orb_Trinket.GetItem().IsReady())
                    {
                        return ItemData.Farsight_Orb_Trinket.GetItem().Slots.FirstOrDefault();
                    }
                    if (ItemData.Stealth_Ward.GetItem().IsOwned() && ItemData.Stealth_Ward.GetItem().IsReady())
                    {
                        return ItemData.Stealth_Ward.GetItem().Slots.FirstOrDefault();
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return SpellSlot.Unknown;
        }

        private void OnObjAiBaseProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            try
            {
                var hero = sender as Obj_AI_Hero;
                if (!sender.IsEnemy || hero == null || !Menu.Item(Name + "Hotkey").GetValue<KeyBind>().Active)
                {
                    return;
                }
                var spell =
                    spellList.FirstOrDefault(
                        s =>
                            !string.IsNullOrEmpty(s.Name) &&
                            s.Name.Equals(args.SData.Name, StringComparison.OrdinalIgnoreCase));
                if (spell != null && !spell.Custom && Menu.Item(Name + "Invisibleshaco").GetValue<bool>())
                {
                    CastLogic(args.End, false);
                }

                if (_vayne != null && spell != null &&
                    spell.Hero.Equals(_vayne.ChampionName, StringComparison.OrdinalIgnoreCase) &&
                    Menu.Item(Name + "Invisible" + spell.Hero.ToLower()).GetValue<bool>())
                {
                    var buff =
                        _vayne.Buffs.FirstOrDefault(
                            b => b.Name.Equals("VayneInquisition", StringComparison.OrdinalIgnoreCase));
                    if (buff != null)
                    {
                        CastLogic(args.End, false);
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnGameObjectCreate(GameObject sender, EventArgs args)
        {
            try
            {
                if (!sender.IsEnemy || !Menu.Item(Name + "Hotkey").GetValue<KeyBind>().Active)
                {
                    return;
                }
                if (_rengar != null && Menu.Item(Menu.Name + "Invisiblerengar").GetValue<bool>())
                {
                    if (sender.Name.Contains("Rengar_Base_R_Alert"))
                    {
                        if (ObjectManager.Player.HasBuff("rengarralertsound") && !_rengar.IsVisible && !_rengar.IsDead)
                        {
                            CastLogic(ObjectManager.Player.Position, false);
                        }
                    }
                }
                if (_leBlanc != null && Menu.Item(Menu.Name + "Invisibleleblanc").GetValue<bool>())
                {
                    if (sender.Name == "LeBlanc_Base_P_poof.troy" &&
                        ObjectManager.Player.Distance(sender.Position) <= MaxRange)
                    {
                        if (!_leBlanc.IsVisible && !_leBlanc.IsDead)
                        {
                            CastLogic(ObjectManager.Player.Position, false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        internal class SpellData
        {
            public SpellData(string hero, SpellSlot slot, bool custom = false, string name = null)
            {
                try
                {
                    Hero = hero;
                    Slot = slot;
                    Custom = custom;
                    if (name != null)
                    {
                        Name = name;
                    }
                    else if (slot != SpellSlot.Unknown)
                    {
                        var champ =
                            GameObjects.EnemyHeroes.FirstOrDefault(
                                h => h.ChampionName.Equals(hero, StringComparison.OrdinalIgnoreCase));
                        if (champ != null)
                        {
                            var spell = champ.GetSpell(Slot);
                            if (spell != null)
                            {
                                Name = spell.Name;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Global.Logger.AddItem(new LogItem(ex));
                }
            }

            public string Hero { get; private set; }
            public SpellSlot Slot { get; private set; }
            public string Name { get; private set; }
            public bool Custom { get; private set; }
        }
    }
}