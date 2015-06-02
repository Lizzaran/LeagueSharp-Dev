#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Ping.cs is part of SFXUtility.

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
using LeagueSharp;
using LeagueSharp.Common;
using SFXLibrary.Extensions.SharpDX;
using SFXLibrary.Logger;
using SFXUtility.Classes;
using SharpDX;
using SharpDX.Direct3D9;

#endregion

namespace SFXUtility.Features.Others
{
    internal class Ping : Base
    {
        private Others _parent;
        private List<PingItem> _pingItems;
        private Font _text;
        public Ping(SFXUtility sfx) : base(sfx) {}

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
            get { return Global.Lang.Get("F_Ping"); }
        }

        protected override void OnEnable()
        {
            Game.OnPing += OnGamePing;
            Drawing.OnPreReset += OnDrawingPreReset;
            Drawing.OnPostReset += OnDrawingPostReset;
            Drawing.OnEndScene += OnDrawingEndScene;

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            Game.OnPing -= OnGamePing;
            Drawing.OnPreReset -= OnDrawingPreReset;
            Drawing.OnPostReset -= OnDrawingPostReset;
            Drawing.OnEndScene -= OnDrawingEndScene;

            base.OnDisable();
        }

        protected override void OnUnload(object sender, UnloadEventArgs args)
        {
            try
            {
                if (args != null && args.Final)
                {
                    base.OnUnload(sender, args);
                }

                if (Initialized)
                {
                    OnDrawingPreReset(null);
                    OnDrawingPostReset(null);
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
                if (Global.IoC.IsRegistered<Others>())
                {
                    _parent = Global.IoC.Resolve<Others>();
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
                    new MenuItem(drawingMenu.Name + "FontSize", Global.Lang.Get("G_FontSize")).SetValue(
                        new Slider(25, 10, 30)));

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

        protected override void OnInitialize()
        {
            _pingItems = new List<PingItem>();

            _text = new Font(
                Drawing.Direct3DDevice,
                new FontDescription
                {
                    FaceName = Global.DefaultFont,
                    Height = Menu.Item(Name + "DrawingFontSize").GetValue<Slider>().Value,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.Default
                });

            base.OnInitialize();
        }

        private void OnGamePing(GamePingEventArgs args)
        {
            var hero = args.Source as Obj_AI_Hero;
            if (hero != null && hero.IsValid && args.PingType != PingCategory.OnMyWay)
            {
                _pingItems.Add(
                    new PingItem(
                        hero.ChampionName, Game.Time + (args.PingType == PingCategory.Danger ? 1f : 1.8f), args.Position,
                        args.Target));
            }
        }

        private void OnDrawingEndScene(EventArgs args)
        {
            try
            {
                if (Drawing.Direct3DDevice == null || Drawing.Direct3DDevice.IsDisposed)
                {
                    return;
                }

                _pingItems.RemoveAll(p => p.EndTime < Game.Time);
                foreach (var ping in _pingItems)
                {
                    var pos = ping.Target != null && ping.Target.IsValid
                        ? Drawing.WorldToScreen(ping.Target.Position)
                        : Drawing.WorldToScreen(ping.Position.To3D());
                    _text.DrawTextCentered(ping.Name, (int) pos.X, (int) pos.Y - 25, Color.White);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnDrawingPostReset(EventArgs args)
        {
            try
            {
                if (_text != null)
                {
                    _text.OnResetDevice();
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnDrawingPreReset(EventArgs args)
        {
            try
            {
                if (_text != null)
                {
                    _text.OnLostDevice();
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        internal class PingItem
        {
            public PingItem(string name, float endTime, Vector2 position, GameObject target)
            {
                Name = name;
                EndTime = endTime;
                Position = position;
                Target = target;
            }

            public string Name { get; set; }
            public float EndTime { get; set; }
            public Vector2 Position { get; set; }
            public GameObject Target { get; set; }
        }
    }
}