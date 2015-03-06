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

namespace SFXUtility.Features.Trackers
{
    #region

    using System;
    using System.Drawing;
    using System.Linq;
    using Classes;
    using LeagueSharp;
    using LeagueSharp.Common;
    using SFXLibrary;
    using SFXLibrary.Extensions.NET;
    using SFXLibrary.IoCContainer;

    #endregion

    internal class Clone : Base
    {
        private readonly string[] _cloneHeroes = {"Shaco", "LeBlanc", "MonkeyKing", "Yorick"};
        private Trackers _trackers;

        public Clone(IContainer container) : base(container)
        {
            CustomEvents.Game.OnGameLoad += OnGameLoad;
        }

        public override bool Enabled
        {
            get
            {
                return _trackers != null && _trackers.Enabled && Menu != null &&
                       Menu.Item(Name + "Enabled").GetValue<bool>();
            }
        }

        public override string Name
        {
            get { return "Clone"; }
        }

        private void OnDraw(EventArgs args)
        {
            try
            {
                if (!Enabled)
                    return;

                var circleColor = Menu.Item(Name + "DrawingCircleColor").GetValue<Color>();
                var radius = Menu.Item(Name + "DrawingCircleRadius").GetValue<Slider>().Value;

                foreach (
                    var hero in
                        ObjectManager.Get<Obj_AI_Hero>()
                            .Where(hero => hero.IsValid && hero.IsEnemy && !hero.IsDead && hero.IsVisible)
                            .Where(
                                hero =>
                                    _cloneHeroes.Contains(hero.ChampionName, StringComparison.OrdinalIgnoreCase) &&
                                    hero.Position.IsOnScreen()))
                {
                    Render.Circle.DrawCircle(hero.ServerPosition, hero.BoundingRadius + radius, circleColor);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteBlock(ex);
            }
        }

        private void OnGameLoad(EventArgs args)
        {
            try
            {
                Logger.Prefix = string.Format("{0} - {1}", BaseName, Name);

                if (IoC.IsRegistered<Trackers>() && IoC.Resolve<Trackers>().Initialized)
                {
                    TrackersLoaded(IoC.Resolve<Trackers>());
                }
                else
                {
                    if (IoC.IsRegistered<Mediator>())
                    {
                        IoC.Resolve<Mediator>().Register("Trackers_initialized", TrackersLoaded);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteBlock(ex);
            }
        }

        private void TrackersLoaded(object o)
        {
            try
            {
                var trackers = o as Trackers;
                if (trackers != null && trackers.Menu != null)
                {
                    _trackers = trackers;

                    Menu = new Menu(Name, Name);

                    var drawingMenu = new Menu("Drawing", Name + "Drawing");
                    drawingMenu.AddItem(
                        new MenuItem(Name + "DrawingCircleColor", "Circle Color").SetValue(Color.YellowGreen));
                    drawingMenu.AddItem(
                        new MenuItem(Name + "DrawingCircleRadius", "Circle Radius").SetValue(new Slider(30)));

                    Menu.AddSubMenu(drawingMenu);

                    Menu.AddItem(new MenuItem(Name + "Enabled", "Enabled").SetValue(false));

                    _trackers.Menu.AddSubMenu(Menu);

                    _trackers.Menu.Item(_trackers.Name + "Enabled").ValueChanged +=
                        delegate(object sender, OnValueChangeEventArgs args)
                        {
                            if (args.GetNewValue<bool>())
                            {
                                if (Menu != null && Menu.Item(Name + "Enabled").GetValue<bool>())
                                {
                                    Drawing.OnDraw += OnDraw;
                                }
                            }
                            else
                            {
                                Drawing.OnDraw += OnDraw;
                            }
                        };

                    Menu.Item(Name + "Enabled").ValueChanged +=
                        delegate(object sender, OnValueChangeEventArgs args)
                        {
                            if (args.GetNewValue<bool>())
                            {
                                if (_trackers != null && _trackers.Menu != null &&
                                    _trackers.Menu.Item(_trackers.Name + "Enabled").GetValue<bool>())
                                {
                                    Drawing.OnDraw += OnDraw;
                                }
                            }
                            else
                            {
                                Drawing.OnDraw += OnDraw;
                            }
                        };

                    if (Enabled)
                    {
                        Drawing.OnDraw += OnDraw;
                    }

                    Initialized = true;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteBlock(ex);
            }
        }
    }
}