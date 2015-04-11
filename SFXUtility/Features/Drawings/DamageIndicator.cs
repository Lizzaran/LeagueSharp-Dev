#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 DamageIndicator.cs is part of SFXUtility.

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

namespace SFXUtility.Features.Drawings
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using Classes;
    using LeagueSharp;
    using LeagueSharp.Common;
    using SFXLibrary;
    using SFXLibrary.Extensions.NET;
    using SFXLibrary.Logger;

    #endregion

    internal class DamageIndicator : Base
    {
        private readonly List<Spell> _spells = new List<Spell>
        {
            new Spell(SpellSlot.Q),
            new Spell(SpellSlot.W),
            new Spell(SpellSlot.E),
            new Spell(SpellSlot.R)
        };

        private Drawings _parent;

        public override bool Enabled
        {
            get { return !Unloaded && _parent != null && _parent.Enabled && Menu != null && Menu.Item(Name + "Enabled").GetValue<bool>(); }
        }

        public override string Name
        {
            get { return Language.Get("F_DamageIndicator"); }
        }

        private void OnDrawingDraw(EventArgs args)
        {
            try
            {
                var lineColor = Menu.Item(Name + "DrawingLineColor").GetValue<Color>();
                var fillColor = Menu.Item(Name + "DrawingFillColor").GetValue<Color>();

                foreach (var enemy in HeroManager.Enemies.Where(e => e.IsValid && !e.IsDead && e.IsHPBarRendered && e.Position.IsOnScreen()))
                {
                    var barPos = enemy.HPBarPosition;
                    var damage = (float) CalculateComboDamage(enemy);
                    if (damage > 1)
                    {
                        var percentHealthAfterDamage = Math.Max(0, enemy.Health - damage)/enemy.MaxHealth;
                        var yPos = barPos.Y + 20;
                        var xPosDamage = barPos.X + 10 + 103*percentHealthAfterDamage;
                        var xPosCurrentHp = barPos.X + 10 + 103*enemy.Health/enemy.MaxHealth;
                        var posX = barPos.X + 9 + (107*percentHealthAfterDamage);

                        Drawing.DrawLine(xPosDamage, yPos, xPosDamage, yPos + 8, 2, lineColor);
                        Drawing.DrawLine(posX, yPos, posX + (xPosCurrentHp - xPosDamage), yPos, 8, fillColor);
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected override void OnEnable()
        {
            Drawing.OnDraw += OnDrawingDraw;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            Drawing.OnDraw -= OnDrawingDraw;
            base.OnDisable();
        }

        protected override void OnGameLoad(EventArgs args)
        {
            try
            {
                if (Global.IoC.IsRegistered<Drawings>())
                {
                    _parent = Global.IoC.Resolve<Drawings>();
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
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "LineColor", Language.Get("G_Line") + " " + Language.Get("G_Color")).SetValue(
                        Color.DarkRed.ToArgb(90)));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "FillColor", Language.Get("G_Fill") + " " + Language.Get("G_Color")).SetValue(Color.Red.ToArgb(90)));

                Menu.AddSubMenu(drawingMenu);

                Menu.AddItem(new MenuItem(Name + "AutoAttacks", Language.Get("DamageIndicator_AutoAttacks")).SetValue(new Slider(2, 0, 5)));
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

        private double CalculateComboDamage(Obj_AI_Hero enemy)
        {
            var damage = 0d;

            foreach (var spell in _spells.Where(spell => spell.IsReady()))
            {
                switch (spell.DamageType)
                {
                    case TargetSelector.DamageType.Physical:
                        damage += ObjectManager.Player.CalcDamage(enemy, Damage.DamageType.Physical,
                            ObjectManager.Player.GetSpellDamage(enemy, spell.Slot)*ObjectManager.Player.PercentArmorPenetrationMod);
                        break;
                    case TargetSelector.DamageType.Magical:
                        damage += ObjectManager.Player.CalcDamage(enemy, Damage.DamageType.Magical,
                            ObjectManager.Player.GetSpellDamage(enemy, spell.Slot)*ObjectManager.Player.PercentMagicPenetrationMod);
                        break;
                    case TargetSelector.DamageType.True:
                        damage += ObjectManager.Player.CalcDamage(enemy, Damage.DamageType.True,
                            ObjectManager.Player.GetSpellDamage(enemy, spell.Slot));
                        break;
                }
            }

            damage += ObjectManager.Player.GetAutoAttackDamage(enemy)*Menu.Item(Name + "AutoAttacks").GetValue<Slider>().Value;

            return damage;
        }
    }
}