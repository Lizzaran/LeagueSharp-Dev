#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Selected.cs is part of SFXChallenger.

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
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXChallenger.Library.Logger;
using SharpDX;
using Color = System.Drawing.Color;
using DamageType = SFXChallenger.Enumerations.DamageType;

#endregion

namespace SFXChallenger.SFXTargetSelector
{
    public static class Selected
    {
        private static Menu _mainMenu;

        static Selected()
        {
            ClickBuffer = 100f;
            Game.OnWndProc += OnGameWndProc;
        }

        public static float ClickBuffer { get; set; }
        public static Obj_AI_Hero Target { get; set; }

        internal static void AddToMenu(Menu mainMenu, Menu drawingMenu)
        {
            try
            {
                _mainMenu = mainMenu;

                var drawingSelectedMenu =
                    drawingMenu.AddSubMenu(new Menu("Selected Target", drawingMenu.Name + ".selected"));
                drawingSelectedMenu.AddItem(
                    new MenuItem(drawingSelectedMenu.Name + ".color", "Color").SetShared().SetValue(Color.Yellow));
                drawingSelectedMenu.AddItem(
                    new MenuItem(drawingSelectedMenu.Name + ".radius", "Radius").SetShared().SetValue(new Slider(35)));
                drawingSelectedMenu.AddItem(
                    new MenuItem(drawingSelectedMenu.Name + ".enabled", "Enabled").SetShared().SetValue(true));

                Drawing.OnDraw += OnDrawingDraw;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static Obj_AI_Hero GetTarget(float range, DamageType damageType, bool ignoreShields, Vector3 from)
        {
            try
            {
                if (Target != null &&
                    TargetSelector.IsValidTarget(
                        Target, TargetSelector.ForceFocus ? float.MaxValue : range, damageType, ignoreShields, from))
                {
                    return Target;
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return null;
        }

        private static void OnDrawingDraw(EventArgs args)
        {
            try
            {
                if (_mainMenu == null)
                {
                    return;
                }

                if (Target != null && Target.IsValidTarget() && Target.Position.IsOnScreen() && TargetSelector.Focus)
                {
                    var selectedEnabled = _mainMenu.Item(_mainMenu.Name + ".drawing.selected.enabled").GetValue<bool>();
                    var selectedRadius =
                        _mainMenu.Item(_mainMenu.Name + ".drawing.selected.radius").GetValue<Slider>().Value;
                    var selectedColor = _mainMenu.Item(_mainMenu.Name + ".drawing.selected.color").GetValue<Color>();
                    var circleThickness =
                        _mainMenu.Item(_mainMenu.Name + ".drawing.circle-thickness").GetValue<Slider>().Value;

                    if (selectedEnabled)
                    {
                        Render.Circle.DrawCircle(
                            Target.Position, Target.BoundingRadius + selectedRadius, selectedColor, circleThickness,
                            true);
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private static void OnGameWndProc(WndEventArgs args)
        {
            try
            {
                if (args.Msg != (ulong) WindowsMessages.WM_LBUTTONDOWN)
                {
                    return;
                }

                Target =
                    Targets.Items.Select(t => t.Hero)
                        .Where(h => h.IsValidTarget() && h.Distance(Game.CursorPos) < h.BoundingRadius + ClickBuffer)
                        .OrderBy(h => h.Distance(Game.CursorPos))
                        .FirstOrDefault();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }
}