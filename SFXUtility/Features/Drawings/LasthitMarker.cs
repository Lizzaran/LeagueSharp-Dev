#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 LasthitMarker.cs is part of SFXUtility.

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
    using System.Linq;
    using Classes;
    using LeagueSharp;
    using LeagueSharp.Common;
    using SFXLibrary;
    using SFXLibrary.Logger;
    using SharpDX;
    using Color = System.Drawing.Color;

    #endregion

    internal class LasthitMarker : Base
    {
        private IEnumerable<Obj_AI_Minion> _minions = new List<Obj_AI_Minion>();
        private Drawings _parent;

        public override bool Enabled
        {
            get { return _parent != null && _parent.Enabled && Menu != null && Menu.Item(Name + "Enabled").GetValue<bool>(); }
        }

        public override string Name
        {
            get { return Language.Get("F_LasthitMarker"); }
        }

        private void OnDrawingDraw(EventArgs args)
        {
            try
            {
                if (!_minions.Any())
                    return;

                var circleColor = Menu.Item(Name + "DrawingCircleColor").GetValue<Color>();
                var hpKillableColor = Menu.Item(Name + "DrawingHpBarKillableColor").GetValue<Color>();
                var hpUnkillableColor = Menu.Item(Name + "DrawingHpBarUnkillableColor").GetValue<Color>();
                var hpLinesThickness = Menu.Item(Name + "DrawingHpBarLineThickness").GetValue<Slider>().Value;
                var radius = Menu.Item(Name + "DrawingCircleRadius").GetValue<Slider>().Value;
                var thickness = Menu.Item(Name + "DrawingCircleThickness").GetValue<Slider>().Value;

                var hpBar = Menu.Item(Name + "DrawingHpBarEnabled").GetValue<bool>();
                var circle = Menu.Item(Name + "DrawingCircleEnabled").GetValue<bool>();

                foreach (var minion in _minions)
                {
                    var aaDamage = ObjectManager.Player.GetAutoAttackDamage(minion, true);
                    var killable = minion.Health <= aaDamage;
                    if (hpBar && minion.IsHPBarRendered)
                    {
                        var barPos = minion.HPBarPosition;
                        var offset = 62/(minion.MaxHealth/aaDamage);
                        offset = offset > 62 ? 62 : offset;
                        var tmpThk = (int) (62 - offset);
                        hpLinesThickness = tmpThk > hpLinesThickness ? hpLinesThickness : (tmpThk == 0 ? 1 : tmpThk);
                        Drawing.DrawLine(new Vector2(barPos.X + 45 + (float) offset, barPos.Y + 18),
                            new Vector2(barPos.X + 45 + (float) offset, barPos.Y + 23), hpLinesThickness,
                            killable ? hpKillableColor : hpUnkillableColor);
                    }
                    if (circle && killable)
                    {
                        Render.Circle.DrawCircle(minion.Position, minion.BoundingRadius + radius, circleColor, thickness);
                    }
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
                var drawingHpBarMenu = new Menu(Language.Get("LasthitMarker_HpBar"), drawingMenu.Name + "HpBar");
                var drawingCirclesMenu = new Menu(Language.Get("G_Circle"), drawingMenu.Name + "Circle");

                drawingHpBarMenu.AddItem(
                    new MenuItem(drawingHpBarMenu.Name + "KillableColor", Language.Get("LasthitMarker_Killable") + " " + Language.Get("G_Color"))
                        .SetValue(Color.Green));
                drawingHpBarMenu.AddItem(
                    new MenuItem(drawingHpBarMenu.Name + "UnkillableColor", Language.Get("LasthitMarker_Unkillable") + " " + Language.Get("G_Color"))
                        .SetValue(Color.White));
                drawingHpBarMenu.AddItem(
                    new MenuItem(drawingHpBarMenu.Name + "LineThickness", Language.Get("G_Line") + " " + Language.Get("G_Thickness")).SetValue(
                        new Slider(1, 1, 10)));
                drawingHpBarMenu.AddItem(new MenuItem(drawingHpBarMenu.Name + "Enabled", Language.Get("G_Enabled")).SetValue(false));

                drawingCirclesMenu.AddItem(
                    new MenuItem(drawingCirclesMenu.Name + "Color", Language.Get("G_Circle") + " " + Language.Get("G_Color")).SetValue(Color.Fuchsia));
                drawingCirclesMenu.AddItem(
                    new MenuItem(drawingCirclesMenu.Name + "Radius", Language.Get("G_Circle") + " " + Language.Get("G_Radius")).SetValue(new Slider(30)));
                drawingCirclesMenu.AddItem(
                    new MenuItem(drawingCirclesMenu.Name + "CircleThickness", Language.Get("G_Circle") + " " + Language.Get("G_Thickness")).SetValue(
                        new Slider(2, 1, 10)));
                drawingCirclesMenu.AddItem(new MenuItem(drawingCirclesMenu.Name + "Enabled", Language.Get("G_Enabled")).SetValue(false));

                drawingMenu.AddSubMenu(drawingHpBarMenu);
                drawingMenu.AddSubMenu(drawingCirclesMenu);

                Menu.AddSubMenu(drawingMenu);

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

        protected override void OnEnable()
        {
            Game.OnUpdate += OnGameUpdate;
            Drawing.OnDraw += OnDrawingDraw;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            Game.OnUpdate -= OnGameUpdate;
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

        private void OnGameUpdate(EventArgs args)
        {
            try
            {
                _minions =
                    ObjectManager.Get<Obj_AI_Minion>()
                        .Where(
                            minion =>
                                minion != null && minion.IsValid && minion.IsTargetable && minion.Health > 0.1f &&
                                minion.Team == (ObjectManager.Player.Team == GameObjectTeam.Order ? GameObjectTeam.Chaos : GameObjectTeam.Order) &&
                                minion.Position.IsOnScreen());
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }
}