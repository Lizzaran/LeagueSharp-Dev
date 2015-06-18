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
using Color = System.Drawing.Color;

#endregion

namespace SFXUtility.Features.Drawings
{
    internal class LasthitMarker : Child<Drawings>
    {
        private IEnumerable<Obj_AI_Minion> _minions;
        public LasthitMarker(SFXUtility sfx) : base(sfx) {}

        public override string Name
        {
            get { return Global.Lang.Get("F_LasthitMarker"); }
        }

        private void OnDrawingDraw(EventArgs args)
        {
            try
            {
                if (!_minions.Any())
                {
                    return;
                }

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
                    if (hpBar)
                    {
                        var barPos = minion.HPBarPosition;
                        var barWidth =
                            minion.Buffs.Any(b => b.Name.Equals("turretshield", StringComparison.OrdinalIgnoreCase))
                                ? 88
                                : 63;
                        var offset = (float) (barWidth / (minion.MaxHealth / aaDamage));
                        offset = offset < barWidth ? offset : barWidth;
                        Drawing.DrawLine(
                            new Vector2(barPos.X + 45 + offset, barPos.Y + 17),
                            new Vector2(barPos.X + 45 + offset, barPos.Y + 24), hpLinesThickness,
                            killable ? hpKillableColor : hpUnkillableColor);
                    }
                    if (circle && killable)
                    {
                        Render.Circle.DrawCircle(
                            minion.Position, minion.BoundingRadius + radius, circleColor, thickness);
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected override void OnLoad()
        {
            try
            {
                Menu = new Menu(Name, Name);
                var drawingMenu = new Menu(Global.Lang.Get("G_Drawing"), Name + "Drawing");

                var drawingHpBarMenu = new Menu(Global.Lang.Get("LasthitMarker_HpBar"), drawingMenu.Name + "HpBar");
                drawingHpBarMenu.AddItem(
                    new MenuItem(
                        drawingHpBarMenu.Name + "KillableColor",
                        Global.Lang.Get("LasthitMarker_Killable") + " " + Global.Lang.Get("G_Color")).SetValue(
                            Color.Green));
                drawingHpBarMenu.AddItem(
                    new MenuItem(
                        drawingHpBarMenu.Name + "UnkillableColor",
                        Global.Lang.Get("LasthitMarker_Unkillable") + " " + Global.Lang.Get("G_Color")).SetValue(
                            Color.White));
                drawingHpBarMenu.AddItem(
                    new MenuItem(
                        drawingHpBarMenu.Name + "LineThickness",
                        Global.Lang.Get("G_Line") + " " + Global.Lang.Get("G_Thickness")).SetValue(new Slider(1, 1, 10)));
                drawingHpBarMenu.AddItem(
                    new MenuItem(drawingHpBarMenu.Name + "Enabled", Global.Lang.Get("G_Enabled")).SetValue(false));

                var drawingCirclesMenu = new Menu(Global.Lang.Get("G_Circle"), drawingMenu.Name + "Circle");
                drawingCirclesMenu.AddItem(
                    new MenuItem(drawingCirclesMenu.Name + "Color", Global.Lang.Get("G_Color")).SetValue(Color.Fuchsia));
                drawingCirclesMenu.AddItem(
                    new MenuItem(drawingCirclesMenu.Name + "Radius", Global.Lang.Get("G_Radius")).SetValue(
                        new Slider(30)));
                drawingCirclesMenu.AddItem(
                    new MenuItem(drawingCirclesMenu.Name + "Thickness", Global.Lang.Get("G_Thickness")).SetValue(
                        new Slider(2, 1, 10)));
                drawingCirclesMenu.AddItem(
                    new MenuItem(drawingCirclesMenu.Name + "Enabled", Global.Lang.Get("G_Enabled")).SetValue(false));

                drawingMenu.AddSubMenu(drawingHpBarMenu);
                drawingMenu.AddSubMenu(drawingCirclesMenu);

                Menu.AddSubMenu(drawingMenu);

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
            _minions = new List<Obj_AI_Minion>();
            base.OnInitialize();
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

        private void OnGameUpdate(EventArgs args)
        {
            try
            {
                _minions = GameObjects.EnemyMinions.Where(m => m.IsHPBarRendered && m.IsValidTarget());
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }
}