#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Waypoint.cs is part of SFXUtility.

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
    using System.Collections.Generic;
    using System.Linq;
    using Classes;
    using LeagueSharp;
    using LeagueSharp.Common;
    using SFXLibrary;
    using SFXLibrary.Extensions.SharpDX;
    using SFXLibrary.IoCContainer;
    using SFXLibrary.Logger;
    using SharpDX;
    using Color = System.Drawing.Color;

    #endregion

    internal class Waypoint : Base
    {
        private const float UpdateInterval = 300f;
        private float _lastUpdate;
        private Trackers _trackers;
        private IEnumerable<List<Vector2>> _waypoints = new List<List<Vector2>>();

        public Waypoint(IContainer container)
            : base(container)
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
            get { return "Waypoint"; }
        }

        private void OnGameUpdate(EventArgs args)
        {
            try
            {
                if (_lastUpdate + UpdateInterval > Environment.TickCount)
                    return;

                _lastUpdate = Environment.TickCount;

                _waypoints = (HeroManager.AllHeroes.Where(hero => hero.IsValid && !hero.IsDead)
                    .Where(hero => hero.IsAlly && Menu.Item(Name + "DrawAlly").GetValue<bool>())
                    .Where(hero => hero.IsEnemy && Menu.Item(Name + "DrawEnemy").GetValue<bool>())).Select(
                        hero => hero.GetWaypoints());
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
            }
        }

        private void OnDraw(EventArgs args)
        {
            try
            {
                var crossColor = Menu.Item(Name + "DrawingCrossColor").GetValue<Color>();
                var lineColor = Menu.Item(Name + "DrawingLineColor").GetValue<Color>();

                foreach (var waypoints in _waypoints)
                {
                    var arrivalTime = 0.0f;
                    for (int i = 0, l = waypoints.Count - 1; i < l; i++)
                    {
                        if (!waypoints[i].IsValid() || !waypoints[i + 1].IsValid())
                            continue;

                        var current = Drawing.WorldToScreen(waypoints[i].To3D());
                        var next = Drawing.WorldToScreen(waypoints[i + 1].To3D());

                        arrivalTime += (Vector3.Distance(waypoints[i].To3D(), waypoints[i + 1].To3D())/
                                        (ObjectManager.Player.MoveSpeed/1000))/1000;

                        if (current.IsOnScreen(next))
                        {
                            Drawing.DrawLine(current.X, current.Y, next.X, next.Y, 1, lineColor);
                            if (i == l - 1 && arrivalTime > 0.1f)
                            {
                                Draw.Cross(next, 10f, 2f, crossColor);
                                Draw.TextCentered(new Vector2(next.X - 5, next.Y + 15), crossColor,
                                    arrivalTime.ToString("0.0"));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
            }
        }

        private void OnGameLoad(EventArgs args)
        {
            try
            {
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
                Logger.AddItem(new LogItem(ex) {Object = this});
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
                    drawingMenu.AddItem(new MenuItem(Name + "DrawingCrossColor", "Cross Color").SetValue(Color.DarkRed));
                    drawingMenu.AddItem(new MenuItem(Name + "DrawingLineColor", "Line Color").SetValue(Color.White));

                    Menu.AddSubMenu(drawingMenu);

                    Menu.AddItem(new MenuItem(Name + "DrawAlly", "Ally").SetValue(false));
                    Menu.AddItem(new MenuItem(Name + "DrawEnemy", "Enemy").SetValue(true));
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
                                    Game.OnUpdate += OnGameUpdate;
                                }
                            }
                            else
                            {
                                Drawing.OnDraw -= OnDraw;
                                Game.OnUpdate -= OnGameUpdate;
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
                                    Game.OnUpdate += OnGameUpdate;
                                }
                            }
                            else
                            {
                                Drawing.OnDraw -= OnDraw;
                                Game.OnUpdate -= OnGameUpdate;
                            }
                        };

                    if (Enabled)
                    {
                        Drawing.OnDraw += OnDraw;
                        Game.OnUpdate += OnGameUpdate;
                    }

                    Initialized = true;
                }
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
            }
        }
    }
}