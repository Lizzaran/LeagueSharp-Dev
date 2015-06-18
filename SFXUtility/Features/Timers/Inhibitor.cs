#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Inhibitor.cs is part of SFXUtility.

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
using SFXLibrary;
using SFXLibrary.Extensions.NET;
using SFXLibrary.Extensions.SharpDX;
using SFXLibrary.Logger;
using SFXUtility.Classes;
using SharpDX;
using SharpDX.Direct3D9;

#endregion

namespace SFXUtility.Features.Timers
{
    internal class Inhibitor : Child<Timers>
    {
        private const float CheckInterval = 800f;
        private List<InhibitorObject> _inhibs;
        private float _lastCheck;
        private Font _mapText;
        private Font _minimapText;
        public Inhibitor(SFXUtility sfx) : base(sfx) {}

        public override string Name
        {
            get { return Global.Lang.Get("F_Inhibitor"); }
        }

        protected override void OnEnable()
        {
            Game.OnUpdate += OnGameUpdate;
            Drawing.OnEndScene += OnDrawingEndScene;

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            Game.OnUpdate -= OnGameUpdate;
            Drawing.OnEndScene -= OnDrawingEndScene;

            base.OnDisable();
        }

        private void OnGameUpdate(EventArgs args)
        {
            if (_lastCheck + CheckInterval > Environment.TickCount)
            {
                return;
            }

            _lastCheck = Environment.TickCount;

            if (_inhibs == null)
            {
                return;
            }

            foreach (var inhib in _inhibs)
            {
                if (inhib.Object.Health > 0)
                {
                    inhib.LastHealth = inhib.Object.Health;
                    inhib.Destroyed = false;
                }
                else if (!inhib.Destroyed && inhib.LastHealth > 0 && inhib.Object.Health <= 0)
                {
                    inhib.Destroyed = true;
                    inhib.NextRespawnTime = (int) Game.ClockTime + inhib.RespawnTime;
                }
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

                var mapTotalSeconds = Menu.Item(Name + "DrawingMapTimeFormat").GetValue<StringList>().SelectedIndex == 1;
                var minimapTotalSeconds =
                    Menu.Item(Name + "DrawingMinimapTimeFormat").GetValue<StringList>().SelectedIndex == 1;
                var mapEnabled = Menu.Item(Name + "DrawingMapEnabled").GetValue<bool>();
                var minimapEnabled = Menu.Item(Name + "DrawingMinimapEnabled").GetValue<bool>();

                if (!mapEnabled && !minimapEnabled)
                {
                    return;
                }

                foreach (var inhib in _inhibs.Where(i => i != null && i.Destroyed && i.NextRespawnTime > Game.Time))
                {
                    if (mapEnabled && inhib.Object.Position.IsOnScreen())
                    {
                        _mapText.DrawTextCentered(
                            (inhib.NextRespawnTime - (int) Game.Time).FormatTime(mapTotalSeconds),
                            Drawing.WorldToScreen(inhib.Object.Position), Color.White);
                    }
                    if (minimapEnabled)
                    {
                        _minimapText.DrawTextCentered(
                            (inhib.NextRespawnTime - (int) Game.Time).FormatTime(minimapTotalSeconds),
                            Drawing.WorldToMinimap(inhib.Object.Position), Color.White, true);
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected override void OnLoad()
        {
            try
            {
                Menu = new Menu(Name, Name);
                var drawingMenu = new Menu(Global.Lang.Get("G_Drawing"), Name + "Drawing");
                var drawingMapMenu = new Menu(Global.Lang.Get("G_Map"), drawingMenu.Name + "Map");
                var drawingMinimapMenu = new Menu(Global.Lang.Get("G_Minimap"), drawingMenu.Name + "Minimap");

                drawingMapMenu.AddItem(
                    new MenuItem(drawingMapMenu.Name + "TimeFormat", Global.Lang.Get("G_TimeFormat")).SetValue(
                        new StringList(new[] { "mm:ss", "ss" })));
                drawingMapMenu.AddItem(
                    new MenuItem(drawingMapMenu.Name + "FontSize", Global.Lang.Get("G_FontSize")).SetValue(
                        new Slider(20, 3, 30)));
                drawingMapMenu.AddItem(
                    new MenuItem(drawingMapMenu.Name + "Enabled", Global.Lang.Get("G_Enabled")).SetValue(false));

                drawingMinimapMenu.AddItem(
                    new MenuItem(drawingMinimapMenu.Name + "TimeFormat", Global.Lang.Get("G_TimeFormat")).SetValue(
                        new StringList(new[] { "mm:ss", "ss" })));
                drawingMinimapMenu.AddItem(
                    new MenuItem(drawingMinimapMenu.Name + "FontSize", Global.Lang.Get("G_FontSize")).SetValue(
                        new Slider(13, 3, 30)));
                drawingMinimapMenu.AddItem(
                    new MenuItem(drawingMinimapMenu.Name + "Enabled", Global.Lang.Get("G_Enabled")).SetValue(false));

                drawingMenu.AddSubMenu(drawingMapMenu);
                drawingMenu.AddSubMenu(drawingMinimapMenu);

                Menu.AddSubMenu(drawingMenu);

                Menu.AddItem(new MenuItem(Name + "Enabled", Global.Lang.Get("G_Enabled")).SetValue(false));

                Parent.Menu.AddSubMenu(Menu);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected override void OnInitialize()
        {
            _inhibs = new List<InhibitorObject>();

            foreach (var inhib in GameObjects.Inhibitors)
            {
                _inhibs.Add(new InhibitorObject(inhib));
            }

            if (!_inhibs.Any())
            {
                OnUnload(null, new UnloadEventArgs(true));
                return;
            }

            _minimapText = MDrawing.GetFont(Menu.Item(Name + "DrawingMinimapFontSize").GetValue<Slider>().Value);
            _mapText = MDrawing.GetFont(Menu.Item(Name + "DrawingMapFontSize").GetValue<Slider>().Value);

            base.OnInitialize();
        }

        private class InhibitorObject
        {
            public InhibitorObject(Obj_BarracksDampener inhibitor)
            {
                Object = inhibitor;
                Destroyed = false;
                NextRespawnTime = -1;
                RespawnTime = 300;
                LastHealth = float.MinValue;
            }

            public Obj_BarracksDampener Object { get; private set; }
            public bool Destroyed { get; set; }
            public int RespawnTime { get; private set; }
            public int NextRespawnTime { get; set; }
            public float LastHealth { get; set; }
        }
    }
}