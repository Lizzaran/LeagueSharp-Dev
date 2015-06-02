#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Clock.cs is part of SFXUtility.

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
using System.Drawing;
using LeagueSharp;
using LeagueSharp.Common;
using SFXLibrary.Logger;
using SFXUtility.Classes;

#endregion

namespace SFXUtility.Features.Drawings
{
    internal class Clock : Base
    {
        private Drawings _parent;
        public Clock(SFXUtility sfx) : base(sfx) {}

        public override bool Enabled
        {
            get
            {
                return !Unloaded && _parent != null && _parent.Enabled && Menu != null &&
                       Menu.Item(Name + "Enabled").GetValue<bool>();
            }
        }

        public override string Name
        {
            get { return Global.Lang.Get("F_Clock"); }
        }

        private void OnDrawingDraw(EventArgs args)
        {
            try
            {
                Drawing.DrawText(
                    Drawing.Width - Menu.Item(Name + "DrawingOffsetRight").GetValue<Slider>().Value,
                    Menu.Item(Name + "DrawingOffsetTop").GetValue<Slider>().Value,
                    Menu.Item(Name + "DrawingColor").GetValue<Color>(), DateTime.Now.ToShortTimeString());
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
                    {
                        OnParentInitialized(null, null);
                    }
                    else
                    {
                        _parent.OnInitialized += OnParentInitialized;
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
                {
                    return;
                }

                Menu = new Menu(Name, Name);

                var drawingMenu = new Menu(Global.Lang.Get("G_Drawing"), Name + "Drawing");
                drawingMenu.AddItem(
                    new MenuItem(
                        drawingMenu.Name + "OffsetTop", Global.Lang.Get("G_Offset") + " " + Global.Lang.Get("G_Top"))
                        .SetValue(new Slider(75, 0, 500)));
                drawingMenu.AddItem(
                    new MenuItem(
                        drawingMenu.Name + "OffsetRight", Global.Lang.Get("G_Offset") + " " + Global.Lang.Get("G_Right"))
                        .SetValue(new Slider(100, 0, 500)));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "Color", Global.Lang.Get("G_Color")).SetValue(Color.Gold));

                Menu.AddSubMenu(drawingMenu);

                Menu.AddItem(new MenuItem(Name + "Enabled", Global.Lang.Get("G_Enabled")).SetValue(false));

                _parent.Menu.AddSubMenu(Menu);

                HandleEvents(_parent);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }
}