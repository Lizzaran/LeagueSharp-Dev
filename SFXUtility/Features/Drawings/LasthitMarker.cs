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
    using SFXLibrary.IoCContainer;
    using SFXLibrary.Logger;
    using SharpDX;
    using Color = System.Drawing.Color;

    #endregion

    internal class LasthitMarker : Base
    {
        private List<Obj_AI_Minion> _minions = new List<Obj_AI_Minion>();
        private Drawings _parent;

        public LasthitMarker(IContainer container)
            : base(container)
        {
            CustomEvents.Game.OnGameLoad += OnGameLoad;
        }

        public override bool Enabled
        {
            get
            {
                return _parent != null && _parent.Enabled && Menu != null &&
                       Menu.Item(Name + "Enabled").GetValue<bool>();
            }
        }

        public override string Name
        {
            get { return "Lasthit Marker"; }
        }

        private void OnDrawingDraw(EventArgs args)
        {
            try
            {
                if (_minions.Count > 0)
                {
                    var circleColor = Menu.Item(Name + "DrawingCircleColor").GetValue<Color>();
                    var hpKillableColor = Menu.Item(Name + "DrawingHpBarKillableColor").GetValue<Color>();
                    var hpUnkillableColor = Menu.Item(Name + "DrawingHpBarUnkillableColor").GetValue<Color>();
                    var hpLinesThickness = Menu.Item(Name + "DrawingHpBarLinesThickness").GetValue<Slider>().Value;
                    var radius = Menu.Item(Name + "DrawingCircleRadius").GetValue<Slider>().Value;
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
                            hpLinesThickness = tmpThk > hpLinesThickness
                                ? hpLinesThickness
                                : (tmpThk == 0 ? 1 : tmpThk);
                            Drawing.DrawLine(new Vector2(barPos.X + 45 + (float) offset, barPos.Y + 18),
                                new Vector2(barPos.X + 45 + (float) offset, barPos.Y + 23), hpLinesThickness,
                                killable ? hpKillableColor : hpUnkillableColor);
                        }
                        if (circle && killable)
                        {
                            Render.Circle.DrawCircle(minion.Position, minion.BoundingRadius + radius, circleColor);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
            }
        }

        private void OnParentLoaded(object sender, EventArgs eventArgs)
        {
            try
            {
                if (_parent.Menu == null)
                    return;

                Menu = new Menu(Name, Name);

                var drawingMenu = new Menu("Drawing", Name + "Drawing");
                var drawingHpBarMenu = new Menu("HPBar", Name + "HPBar");

                drawingHpBarMenu.AddItem(
                    new MenuItem(Name + "DrawingHpBarKillableColor", "Killable Color").SetValue(Color.Green));
                drawingHpBarMenu.AddItem(
                    new MenuItem(Name + "DrawingHpBarUnkillableColor", "Unkillable Color").SetValue(Color.White));
                drawingHpBarMenu.AddItem(
                    new MenuItem(Name + "DrawingHpBarLinesThickness", "Lines Thickness").SetValue(new Slider(1, 1,
                        10)));
                drawingHpBarMenu.AddItem(new MenuItem(Name + "DrawingHpBarEnabled", "Enabled").SetValue(false));

                var drawingCirclesMenu = new Menu("Circle", Name + "Circle");
                drawingCirclesMenu.AddItem(
                    new MenuItem(Name + "DrawingCircleColor", "Circle Color").SetValue(Color.Fuchsia));
                drawingCirclesMenu.AddItem(
                    new MenuItem(Name + "DrawingCircleRadius", "Circle Radius").SetValue(new Slider(30)));
                drawingCirclesMenu.AddItem(new MenuItem(Name + "DrawingCircleEnabled", "Enabled").SetValue(false));

                drawingMenu.AddSubMenu(drawingHpBarMenu);
                drawingMenu.AddSubMenu(drawingCirclesMenu);

                Menu.AddSubMenu(drawingMenu);

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

        private void OnGameLoad(EventArgs args)
        {
            try
            {
                if (IoC.IsRegistered<Drawings>())
                {
                    _parent = IoC.Resolve<Drawings>();
                    if (_parent.Initialized)
                        OnParentLoaded(null, null);
                    else
                        _parent.OnInitialized += OnParentLoaded;
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
                _minions =
                    ObjectManager.Get<Obj_AI_Minion>()
                        .Where(
                            minion =>
                                minion != null && minion.IsValid && minion.Health > 0.1f && minion.IsEnemy &&
                                minion.Position.IsOnScreen())
                        .ToList();
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
            }
        }
    }
}