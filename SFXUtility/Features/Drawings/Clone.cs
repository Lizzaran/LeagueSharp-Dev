#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Clone.cs is part of SFXUtility.

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
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXLibrary.Logger;
using SFXUtility.Classes;

#endregion

namespace SFXUtility.Features.Drawings
{
    #region

    

    #endregion

    internal class Clone : Base
    {
        private readonly List<string> _cloneHeroes = new List<string> { "shaco", "leblanc", "monkeyking", "yorick" };
        private readonly List<Obj_AI_Hero> _heroes = new List<Obj_AI_Hero>();
        private Drawings _parent;

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
            get { return Global.Lang.Get("F_Clone"); }
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

        private void OnDrawingDraw(EventArgs args)
        {
            try
            {
                var color = Menu.Item(Name + "DrawingCircleColor").GetValue<Color>();
                var radius = Menu.Item(Name + "DrawingCircleRadius").GetValue<Slider>().Value;
                var thickness = Menu.Item(Name + "DrawingCircleThickness").GetValue<Slider>().Value;

                foreach (var hero in
                    _heroes.Where(hero => !hero.IsDead && hero.IsVisible && hero.Position.IsOnScreen()))
                {
                    Render.Circle.DrawCircle(hero.Position, hero.BoundingRadius + radius, color, thickness);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
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
                        drawingMenu.Name + "CircleColor", Global.Lang.Get("G_Circle") + " " + Global.Lang.Get("G_Color"))
                        .SetValue(Color.YellowGreen));
                drawingMenu.AddItem(
                    new MenuItem(
                        drawingMenu.Name + "CircleRadius",
                        Global.Lang.Get("G_Circle") + " " + Global.Lang.Get("G_Radius")).SetValue(new Slider(30)));
                drawingMenu.AddItem(
                    new MenuItem(
                        drawingMenu.Name + "CircleThickness",
                        Global.Lang.Get("G_Circle") + " " + Global.Lang.Get("G_Thickness")).SetValue(
                            new Slider(2, 1, 10)));

                Menu.AddSubMenu(drawingMenu);

                Menu.AddItem(new MenuItem(Name + "Enabled", Global.Lang.Get("G_Enabled")).SetValue(false));

                _parent.Menu.AddSubMenu(Menu);

                _heroes.AddRange(HeroManager.Enemies.Where(hero => _cloneHeroes.Contains(hero.ChampionName.ToLower())));

                if (!_heroes.Any())
                {
                    return;
                }

                HandleEvents(_parent);
                RaiseOnInitialized();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }
}