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
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXLibrary.Logger;

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

                _mainMenu.AddItem(
                    new MenuItem(_mainMenu.Name + ".focus-selected", Global.Lang.Get("TS_FocusSelectedTarget")).SetValue
                        (true));
                _mainMenu.AddItem(
                    new MenuItem(
                        _mainMenu.Name + ".force-focus-selected", Global.Lang.Get("TS_OnlyAttackSelectedTarget"))
                        .SetValue(false));

                var drawingSelectedMenu =
                    drawingMenu.AddSubMenu(
                        new Menu(Global.Lang.Get("TS_SelectedTarget"), drawingMenu.Name + ".selected"));
                drawingSelectedMenu.AddItem(
                    new MenuItem(drawingSelectedMenu.Name + ".color", Global.Lang.Get("G_Color")).SetValue(Color.Red));
                drawingSelectedMenu.AddItem(
                    new MenuItem(drawingSelectedMenu.Name + ".radius", Global.Lang.Get("G_Radius")).SetValue(
                        new Slider(50)));
                drawingSelectedMenu.AddItem(
                    new MenuItem(drawingSelectedMenu.Name + ".enabled", Global.Lang.Get("G_Enabled")).SetValue(true));

                Drawing.OnDraw += OnDrawingDraw;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private static void OnDrawingDraw(EventArgs args)
        {
            try
            {
                if (_mainMenu == null)
                {
                    return;
                }

                if (Target != null && Target.IsValidTarget() && Target.Position.IsOnScreen())
                {
                    var selectedEnabled = _mainMenu.Item(_mainMenu.Name + ".drawing.selected.enabled").GetValue<bool>();
                    var selectedRadius =
                        _mainMenu.Item(_mainMenu.Name + ".drawing.selected.radius").GetValue<Slider>().Value;
                    var selectedColor = _mainMenu.Item(_mainMenu.Name + ".drawing.selected.color").GetValue<Color>();
                    var focusSelected = _mainMenu.Item(_mainMenu.Name + ".focus-selected").GetValue<bool>();
                    var circleThickness =
                        _mainMenu.Item(_mainMenu.Name + ".drawing.circle-thickness").GetValue<Slider>().Value;

                    if (selectedEnabled && focusSelected)
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