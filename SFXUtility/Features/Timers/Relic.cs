#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Relic.cs is part of SFXUtility.

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
using SFXLibrary.Extensions.NET;
using SFXLibrary.Extensions.SharpDX;
using SFXLibrary.Logger;
using SFXUtility.Classes;
using SFXUtility.Data;
using SharpDX;
using SharpDX.Direct3D9;

#endregion

namespace SFXUtility.Features.Timers
{
    internal class Relic : Child<Timers>
    {
        private Font _mapText;
        private Font _minimapText;
        private List<RelicObj> _relicObjs;
        public Relic(SFXUtility sfx) : base(sfx) {}

        public override string Name
        {
            get { return Global.Lang.Get("F_Relic"); }
        }

        protected override void OnEnable()
        {
            GameObject.OnCreate += OnGameObjectCreate;
            GameObject.OnDelete += OnGameObjectDelete;
            Drawing.OnEndScene += OnDrawingEndScene;

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            GameObject.OnCreate -= OnGameObjectCreate;
            GameObject.OnDelete -= OnGameObjectDelete;
            Drawing.OnEndScene -= OnDrawingEndScene;

            base.OnDisable();
        }

        private void OnGameObjectDelete(GameObject sender, EventArgs args)
        {
            try
            {
                if (!sender.IsValid)
                {
                    return;
                }

                foreach (var relic in _relicObjs.Where(h => !h.Picked && h.Position.Distance(sender.Position) < 300f))
                {
                    relic.Picked = true;
                    relic.NextRespawnTime = (int) Game.Time + relic.RespawnTime;
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnGameObjectCreate(GameObject sender, EventArgs args)
        {
            try
            {
                if (!sender.IsValid)
                {
                    return;
                }

                foreach (var relic in _relicObjs.Where(h => h.Picked && h.Position.Distance(sender.Position) < 300f))
                {
                    relic.Picked = false;
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
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

                foreach (var relic in _relicObjs.Where(h => h.Picked))
                {
                    if (relic.NextRespawnTime - Game.Time <= 0)
                    {
                        relic.Picked = false;
                        continue;
                    }

                    if (mapEnabled && relic.Position.IsOnScreen())
                    {
                        _mapText.DrawTextCentered(
                            (relic.NextRespawnTime - (int) Game.Time).FormatTime(mapTotalSeconds),
                            Drawing.WorldToScreen(relic.Position), Color.White, true);
                    }
                    if (minimapEnabled)
                    {
                        _minimapText.DrawTextCentered(
                            (relic.NextRespawnTime - (int) Game.Time).FormatTime(minimapTotalSeconds),
                            relic.MinimapPosition, Color.White);
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
            _relicObjs = new List<RelicObj>();

            _relicObjs.AddRange(
                Relics.Objects.Where(c => c.MapType == Utility.Map.GetMap().Type)
                    .Select(c => new RelicObj(c.SpawnTime, c.RespawnTime, c.Position, c.ObjectName, c.MapType)));

            if (!_relicObjs.Any())
            {
                OnUnload(null, new UnloadEventArgs(true));
                return;
            }

            _minimapText = MDrawing.GetFont(Menu.Item(Name + "DrawingMinimapFontSize").GetValue<Slider>().Value);
            _mapText = MDrawing.GetFont(Menu.Item(Name + "DrawingMapFontSize").GetValue<Slider>().Value);

            base.OnInitialize();
        }

        private class RelicObj : Relics.RelicObject
        {
            public RelicObj(float spawnTime,
                float respawnTime,
                Vector3 position,
                string objectName,
                Utility.Map.MapType mapType,
                bool picked = false) : base(spawnTime, respawnTime, position, objectName, mapType)
            {
                Picked = picked;
            }

            public float NextRespawnTime { get; set; }
            public bool Picked { get; set; }
        }
    }
}