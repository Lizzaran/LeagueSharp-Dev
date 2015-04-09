#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 GoldEfficiency.cs is part of SFXUtility.

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

// Credits: TC-Crew

namespace SFXUtility.Features.Trackers
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Classes;
    using LeagueSharp;
    using LeagueSharp.Common;
    using SFXLibrary;
    using SFXLibrary.Extensions.SharpDX;
    using SFXLibrary.Logger;
    using SharpDX;
    using SharpDX.Direct3D9;

    #endregion

    internal class GoldEfficiency : Base
    {
        private const float CheckInterval = 1000f;
        private readonly Dictionary<Obj_AI_Hero, string> _goldEfficiencies = new Dictionary<Obj_AI_Hero, string>();
        private float _lastCheck = Environment.TickCount;
        private Trackers _parent;
        private Font _text;

        public override bool Enabled
        {
            get { return !Unloaded && _parent != null && _parent.Enabled && Menu != null && Menu.Item(Name + "Enabled").GetValue<bool>(); }
        }

        public override string Name
        {
            get { return Language.Get("F_GoldEfficiency"); }
        }

        protected override void OnEnable()
        {
            Game.OnUpdate += OnGameUpdate;

            Drawing.OnPreReset += OnDrawingPreReset;
            Drawing.OnPostReset += OnDrawingPostReset;
            Drawing.OnEndScene += OnDrawingEndScene;

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            Game.OnUpdate -= OnGameUpdate;

            Drawing.OnPreReset -= OnDrawingPreReset;
            Drawing.OnPostReset -= OnDrawingPostReset;
            Drawing.OnEndScene -= OnDrawingEndScene;

            OnUnload(null, new UnloadEventArgs());

            base.OnDisable();
        }

        protected override void OnUnload(object sender, UnloadEventArgs args)
        {
            if (args != null && args.Real)
                base.OnUnload(sender, args);

            if (Initialized)
            {
                OnDrawingPreReset(null);
                OnDrawingPostReset(null);
            }
        }

        protected override void OnGameLoad(EventArgs args)
        {
            try
            {
                if (Global.IoC.IsRegistered<Trackers>())
                {
                    _parent = Global.IoC.Resolve<Trackers>();
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

        private void OnParentInitialized(object sender, EventArgs eventArgs)
        {
            try
            {
                if (_parent.Menu == null)
                    return;

                Menu = new Menu(Name, Name);

                var drawingMenu = new Menu(Language.Get("G_Drawing"), Name + "Drawing");
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + "FontSize", Language.Get("G_FontSize")).SetValue(new Slider(18, 3, 30)));

                Menu.AddSubMenu(drawingMenu);

                Menu.AddItem(new MenuItem(Name + "Enabled", Language.Get("G_Enabled")).SetValue(false));

                _parent.Menu.AddSubMenu(Menu);

                _text = new Font(Drawing.Direct3DDevice,
                    new FontDescription
                    {
                        FaceName = Global.DefaultFont,
                        Height = Menu.Item(Name + "DrawingFontSize").GetValue<Slider>().Value,
                        OutputPrecision = FontPrecision.Default,
                        Quality = FontQuality.Default
                    });

                HandleEvents(_parent);
                RaiseOnInitialized();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnDrawingEndScene(EventArgs args)
        {
            try
            {
                if (Drawing.Direct3DDevice == null || Drawing.Direct3DDevice.IsDisposed)
                    return;

                foreach (var entry in _goldEfficiencies.Where(e => e.Key.IsValid && e.Key.IsVisible && !e.Key.IsDead && e.Key.IsHPBarRendered))
                {
                    _text.DrawTextLeft(entry.Value, (int) (entry.Key.HPBarPosition.X + 139),
                        (int) (entry.Key.HPBarPosition.Y + (entry.Key.IsMe ? 35 : 55)), Color.Gold);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnDrawingPostReset(EventArgs args)
        {
            try
            {
                _text.OnResetDevice();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnDrawingPreReset(EventArgs args)
        {
            try
            {
                _text.OnLostDevice();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnGameUpdate(EventArgs args)
        {
            if (_lastCheck + CheckInterval > Environment.TickCount)
                return;
            _lastCheck = Environment.TickCount;

            foreach (var hero in HeroManager.AllHeroes.Where(h => h.IsValid && h.IsVisible))
            {
                var value = (hero.BaseAttackDamage + hero.FlatPhysicalDamageMod)*Data.AttackDamage;
                value += (hero.BaseAbilityDamage + hero.FlatMagicDamageMod)*Data.AbilityPower;
                value += hero.Armor*Data.Armor;
                value += hero.SpellBlock*Data.MagicResistance;
                value += hero.MaxHealth*Data.Health;
                value += hero.MaxMana*Data.Mana;
                value += hero.HPRegenRate + hero.FlatHPRegenMod*Data.HealthRegeneration;
                value += hero.PARRegenRate*Data.ManaRegeneration;
                value += hero.Crit*100*Data.CritChance;
                value += hero.MoveSpeed*Data.MoveSpeed;
                value += ((hero.FlatArmorPenetrationMod - 1) + ((1 - hero.PercentArmorPenetrationMod)*100))*Data.ArmorPenetration;
                value += ObjectManager.Player.PercentCooldownMod*-1*100*Data.CooldownReduction;
                value += hero.PercentLifeStealMod*100*Data.LifeSteal;
                value += ((hero.FlatMagicPenetrationMod - 1) +
                          ((hero.CombatType == GameObjectCombatType.Ranged ? 30 : 50) + 10/100f*((1 - hero.PercentMagicPenetrationMod)*100)))*
                         Data.MagicPenetration;
                value += hero.PercentSpellVampMod*100*Data.SpellVamp;

                var attackSpeed = (~(int) ((1/ObjectManager.Player.AttackSpeedMod*100) - (1/ObjectManager.Player.AttackDelay*100)) + 1)*
                                  Data.AttackSpeed;
                value += attackSpeed > 0 ? attackSpeed : 0;

                _goldEfficiencies[hero] = string.Format("{0:0.0}k", value/1000);
            }
        }
    }

    internal class Data
    {
        public const float AttackDamage = 36f;
        public const float AbilityPower = 21.75f;
        public const float Armor = 20f;
        public const float MagicResistance = 20f;
        public const float Health = 2.67f;
        public const float Mana = 2f;
        public const float HealthRegeneration = 3.6f;
        public const float ManaRegeneration = 7.2f;
        public const float CritChance = 50f;
        public const float AttackSpeed = 30f;
        public const float MoveSpeed = 13f;
        public const float ArmorPenetration = 12f;
        public const float CooldownReduction = 31.67f;
        public const float LifeSteal = 55f;
        public const float PercentageMovementSpeed = 39.5f;
        public const float MagicPenetration = 34.33f;
        public const float SpellVamp = 27.5f;
    }
}