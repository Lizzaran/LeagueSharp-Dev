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

namespace SFXUtility.Features.Drawings
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Classes;
    using LeagueSharp;
    using LeagueSharp.Common;
    using LeagueSharp.CommonEx.Core.Enumerations;
    using LeagueSharp.CommonEx.Core.Events;
    using LeagueSharp.CommonEx.Core.Extensions.SharpDX;
    using LeagueSharp.CommonEx.Core.Wrappers;
    using SFXLibrary.IoCContainer;
    using SFXLibrary.Logger;
    using SharpDX;
    using Circle = LeagueSharp.CommonEx.Core.Render._2D.Circle;
    using Color = System.Drawing.Color;

    #endregion

    internal class SafeJungleSpots : Base
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

        private Drawings _parent;

        public SafeJungleSpots(IContainer container)
            : base(container)
        {
            Load.OnLoad += OnLoad;
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
            get { return "Safe Jungle Spot"; }
        }

        private void OnDrawingDraw(EventArgs args)
        {
            try
            {
                var radius = Menu.Item(Name + "DrawingRadius").GetValue<Slider>().Value;
                var color = Menu.Item(Name + "DrawingColor").GetValue<Color>();

                foreach (var jungleSpot in _jungleSpots.Where(jungleSpot => Utility.IsOnScreen(jungleSpot)))
                {
                    Circle.Draw(jungleSpot.ToVector2(), radius, 1, CircleType.Full, false, 1, color);
                }
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
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

        private void OnLoad(EventArgs args)
        {
            try
            {
                if (IoC.IsRegistered<Drawings>())
                {
                    _parent = IoC.Resolve<Drawings>();
                    if (_parent.Initialized)
                        OnParentInitialized(null, null);
                    else
                        _parent.OnInitialized += OnParentInitialized;
                }
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
            }
        }

        private void OnParentInitialized(object sender, EventArgs eventArgs)
        {
            try
            {
                if (_parent.Menu == null)
                    return;

                Menu = new Menu(Name, BaseName + Name);

                var drawingMenu = new Menu("Drawing", Name + "Drawing");
                drawingMenu.AddItem(new MenuItem(Name + "DrawingRadius", "Radius").SetValue(new Slider(50, 5, 250)));
                drawingMenu.AddItem(new MenuItem(Name + "DrawingColor", "Color").SetValue(Color.Fuchsia));

                Menu.AddSubMenu(drawingMenu);

                Menu.AddItem(new MenuItem(Name + "Enabled", "Enabled").SetValue(false));

                _parent.Menu.AddSubMenu(Menu);

                if (Map.GetMap().Type != MapType.SummonersRift)
                    return;

                HandleEvents(_parent);
                RaiseOnInitialized();
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
            }
        }
    }
}