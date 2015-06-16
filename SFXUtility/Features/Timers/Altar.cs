#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 altar.cs is part of SFXUtility.

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
using SharpDX;
using SharpDX.Direct3D9;

#endregion

namespace SFXUtility.Features.Timers
{
    internal class Altar : Child<Timers>
    {
        private List<AltarObject> _altars;
        private Font _mapText;
        private Font _minimapText;
        public Altar(SFXUtility sfx) : base(sfx) {}

        public override string Name
        {
            get { return Global.Lang.Get("F_Altar"); }
        }

        protected override void OnEnable()
        {
            Obj_AI_Base.OnBuffAdd += OnObjAiBaseBuffAdd;
            Obj_AI_Base.OnBuffRemove += OnObjAiBaseBuffRemove;
            Drawing.OnEndScene += OnDrawingEndScene;

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            Obj_AI_Base.OnBuffAdd -= OnObjAiBaseBuffAdd;
            Obj_AI_Base.OnBuffRemove -= OnObjAiBaseBuffRemove;
            Drawing.OnEndScene -= OnDrawingEndScene;

            base.OnDisable();
        }

        private void OnObjAiBaseBuffAdd(Obj_AI_Base sender, Obj_AI_BaseBuffAddEventArgs args)
        {
            if (sender.IsValid && sender is Obj_AI_Minion &&
                args.Buff.Name.Equals("treelinelanternlock", StringComparison.OrdinalIgnoreCase))
            {
                var altar = _altars.FirstOrDefault(a => a.Object.NetworkId == sender.NetworkId);
                if (altar != null)
                {
                    altar.Locked = true;
                    altar.NextRespawnTime = (int) Game.Time + altar.RespawnTime;
                }
            }
        }

        private void OnObjAiBaseBuffRemove(Obj_AI_Base sender, Obj_AI_BaseBuffRemoveEventArgs args)
        {
            if (sender.IsValid && sender is Obj_AI_Minion &&
                args.Buff.Name.Equals("treelinelanternlock", StringComparison.OrdinalIgnoreCase))
            {
                var altar = _altars.FirstOrDefault(a => a.Object.NetworkId == sender.NetworkId);
                if (altar != null)
                {
                    altar.Locked = false;
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

                foreach (var altar in _altars.Where(i => i != null && i.Locked && i.NextRespawnTime > Game.Time))
                {
                    if (mapEnabled && altar.Object.Position.IsOnScreen())
                    {
                        _mapText.DrawTextCentered(
                            (altar.NextRespawnTime - (int) Game.Time).FormatTime(mapTotalSeconds),
                            Drawing.WorldToScreen(altar.Object.Position), Color.White);
                    }
                    if (minimapEnabled)
                    {
                        _minimapText.DrawTextCentered(
                            (altar.NextRespawnTime - (int) Game.Time).FormatTime(minimapTotalSeconds),
                            Drawing.WorldToMinimap(altar.Object.Position), Color.White);
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
            _altars = new List<AltarObject>();

            if (Utility.Map.GetMap().Type != Utility.Map.MapType.TwistedTreeline)
            {
                OnUnload(null, new UnloadEventArgs(true));
                return;
            }

            foreach (var altar in ObjectManager.Get<Obj_AI_Minion>())
            {
                if (altar.Name.Contains("Buffplat", StringComparison.OrdinalIgnoreCase))
                {
                    _altars.Add(new AltarObject(altar));
                }
            }

            if (!_altars.Any())
            {
                OnUnload(null, new UnloadEventArgs(true));
                return;
            }

            _minimapText = MDrawing.GetFont(Menu.Item(Name + "DrawingMinimapFontSize").GetValue<Slider>().Value);
            _mapText = MDrawing.GetFont(Menu.Item(Name + "DrawingMapFontSize").GetValue<Slider>().Value);

            base.OnInitialize();
        }

        private class AltarObject
        {
            public AltarObject(Obj_AI_Minion obj)
            {
                Object = obj;
                Locked = false;
                NextRespawnTime = -1;
                RespawnTime = 90;
            }

            public Obj_AI_Minion Object { get; private set; }
            public bool Locked { get; set; }
            public int RespawnTime { get; private set; }
            public int NextRespawnTime { get; set; }
        }
    }
}