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

namespace SFXUtility.Features.Drawings
{
    #region

    using System;
    using System.Drawing;
    using Classes;
    using LeagueSharp;
    using LeagueSharp.Common;
    using SFXLibrary;
    using SFXLibrary.IoCContainer;

    #endregion

    internal class Clock : Base
    {
        private Drawings _drawings;

        public Clock(IContainer container) : base(container)
        {
            CustomEvents.Game.OnGameLoad += OnGameLoad;
        }

        public override bool Enabled
        {
            get
            {
                return _drawings != null && _drawings.Enabled && Menu != null &&
                       Menu.Item(Name + "Enabled").GetValue<bool>();
            }
        }

        public override string Name
        {
            get { return "Clock"; }
        }

        private void OnDraw(EventArgs args)
        {
            try
            {
                Drawing.DrawText(Drawing.Width - Menu.Item(Name + "OffsetRight").GetValue<Slider>().Value,
                    Menu.Item(Name + "OffsetTop").GetValue<Slider>().Value, Menu.Item(Name + "Color").GetValue<Color>(),
                    DateTime.Now.ToShortTimeString());
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

                if (IoC.IsRegistered<Drawings>() && IoC.Resolve<Drawings>().Initialized)
                {
                    DrawingsLoaded(IoC.Resolve<Drawings>());
                }
                else
                {
                    if (IoC.IsRegistered<Mediator>())
                    {
                        IoC.Resolve<Mediator>().Register("Drawings_initialized", DrawingsLoaded);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteBlock(ex);
            }
        }

        private void DrawingsLoaded(object o)
        {
            try
            {
                var drawings = o as Drawings;
                if (drawings != null && drawings.Menu != null)
                {
                    _drawings = drawings;

                    Menu = new Menu(Name, Name);

                    Menu.AddItem(new MenuItem(Name + "OffsetTop", "Offset Top").SetValue(new Slider(75, 0, 500)));
                    Menu.AddItem(new MenuItem(Name + "OffsetRight", "Offset Right").SetValue(new Slider(100, 0, 500)));
                    Menu.AddItem(new MenuItem(Name + "Color", "Color").SetValue(Color.Gold));
                    Menu.AddItem(new MenuItem(Name + "Enabled", "Enabled").SetValue(true));

                    _drawings.Menu.AddSubMenu(Menu);

                    _drawings.Menu.Item(_drawings.Name + "Enabled").ValueChanged +=
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
                                Drawing.OnDraw -= OnDraw;
                            }
                        };

                    Menu.Item(Name + "Enabled").ValueChanged +=
                        delegate(object sender, OnValueChangeEventArgs args)
                        {
                            if (args.GetNewValue<bool>())
                            {
                                if (_drawings != null && _drawings.Enabled)
                                {
                                    Drawing.OnDraw += OnDraw;
                                }
                            }
                            else
                            {
                                Drawing.OnDraw -= OnDraw;
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