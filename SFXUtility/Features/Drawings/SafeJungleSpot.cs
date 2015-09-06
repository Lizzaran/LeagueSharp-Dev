#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 SafeJungleSpot.cs is part of SFXUtility.

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
using SFXUtility.Classes;
using SFXUtility.Library.Logger;
using SharpDX;
using Color = System.Drawing.Color;

#endregion

namespace SFXUtility.Features.Drawings
{
    internal class SafeJungleSpot : Child<Drawings>
    {
        // Credits: Screeder
        private readonly List<Vector3> _jungleSpots = new List<Vector3>
        {
            new Vector3(7600f, 3140f, 60f),
            new Vector3(7160, 4600f, 60f),
            new Vector3(4570f, 6170f, 60f),
            new Vector3(3370f, 8610f, 60f),
            new Vector3(7650f, 2120f, 60f),
            new Vector3(7320f, 11610f, 60f),
            new Vector3(7290f, 10090f, 60f),
            new Vector3(10220f, 9000f, 60f),
            new Vector3(11550f, 6230f, 60f),
            new Vector3(7120f, 12800f, 60f),
            new Vector3(10930f, 5400f, 60f)
        };

        public SafeJungleSpot(Drawings parent) : base(parent)
        {
            OnLoad();
        }

        public override string Name
        {
            get { return "Safe Jungle Spot"; }
        }

        private void OnDrawingDraw(EventArgs args)
        {
            try
            {
                var radius = Menu.Item(Name + "DrawingRadius").GetValue<Slider>().Value;
                var color = Menu.Item(Name + "DrawingColor").GetValue<Color>();
                var thickness = Menu.Item(Name + "DrawingCircleThickness").GetValue<Slider>().Value;

                foreach (var jungleSpot in _jungleSpots.Where(Utility.IsOnScreen))
                {
                    Render.Circle.DrawCircle(jungleSpot, radius, color, thickness);
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

        protected override sealed void OnLoad()
        {
            try
            {
                Menu = new Menu(Name, Name);
                var drawingMenu = new Menu("Drawing", Name + "Drawing");
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + "Color", "Color").SetValue(Color.Fuchsia));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "Radius", "Radius").SetValue(new Slider(50, 5, 250)));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "CircleThickness", "Circle Thickness").SetValue(
                        new Slider(2, 1, 10)));

                Menu.AddSubMenu(drawingMenu);

                Menu.AddItem(new MenuItem(Name + "Enabled", "Enabled").SetValue(false));

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
                if (Utility.Map.GetMap().Type != Utility.Map.MapType.SummonersRift)
                {
                    OnUnload(null, new UnloadEventArgs(true));
                    return;
                }

                base.OnInitialize();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }
}