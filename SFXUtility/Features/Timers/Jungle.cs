#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Jungle.cs is part of SFXUtility.

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

namespace SFXUtility.Features.Timers
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Classes;
    using LeagueSharp;
    using LeagueSharp.Common;
    using SFXLibrary;
    using SFXLibrary.Extensions.NET;
    using SFXLibrary.Extensions.SharpDX;
    using SFXLibrary.Logger;
    using SharpDX;
    using SharpDX.Direct3D9;

    #endregion

    internal class Jungle : Base
    {
        private readonly List<Camp> _camps = new List<Camp>();
        private Font _mapText;
        private Font _minimapText;
        private Timers _parent;

        public override bool Enabled
        {
            get { return !Unloaded && _parent != null && _parent.Enabled && Menu != null && Menu.Item(Name + "Enabled").GetValue<bool>(); }
        }

        public override string Name
        {
            get { return Language.Get("F_Jungle"); }
        }

        protected override void OnEnable()
        {
            GameObject.OnCreate += OnGameObjectCreate;
            GameObject.OnDelete += OnGameObjectDelete;

            Drawing.OnPreReset += OnDrawingPreReset;
            Drawing.OnPostReset += OnDrawingPostReset;
            Drawing.OnEndScene += OnDrawingEndScene;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            GameObject.OnCreate -= OnGameObjectCreate;
            GameObject.OnDelete -= OnGameObjectDelete;

            Drawing.OnPreReset -= OnDrawingPreReset;
            Drawing.OnPostReset -= OnDrawingPostReset;
            Drawing.OnEndScene -= OnDrawingEndScene;

            OnUnload(null, new UnloadEventArgs());

            base.OnDisable();
        }

        protected override void OnUnload(object sender, UnloadEventArgs args)
        {
            if (args != null && args.Real)
                base.OnUnload(sender, args);

            if (Initialized)
            {
                OnDrawingPreReset(null);
                OnDrawingPostReset(null);
            }
        }

        private void OnGameObjectDelete(GameObject sender, EventArgs args)
        {
            try
            {
                if (!sender.IsValid || sender.Type != GameObjectType.obj_AI_Minion || sender.Team != GameObjectTeam.Neutral)
                    return;

                foreach (var camp in _camps)
                {
                    var mob = camp.Mobs.FirstOrDefault(m => m.Name.Contains(sender.Name, StringComparison.OrdinalIgnoreCase));
                    if (mob != null)
                    {
                        mob.Dead = true;
                        camp.Dead = camp.Mobs.All(m => m.Dead);
                        if (camp.Dead)
                        {
                            camp.Dead = true;
                            camp.NextRespawnTime = (int) Game.ClockTime + camp.RespawnTime - 5;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnGameObjectCreate(GameObject sender, EventArgs args)
        {
            if (!sender.IsValid || sender.Type != GameObjectType.obj_AI_Minion || sender.Team != GameObjectTeam.Neutral)
                return;

            foreach (var camp in _camps)
            {
                var mob = camp.Mobs.FirstOrDefault(m => m.Name.Contains(sender.Name, StringComparison.OrdinalIgnoreCase));
                if (mob != null)
                {
                    mob.Dead = false;
                    camp.Dead = false;
                }
            }
        }

        private void OnDrawingEndScene(EventArgs args)
        {
            try
            {
                if (Drawing.Direct3DDevice == null || Drawing.Direct3DDevice.IsDisposed)
                    return;

                var mapTotalSeconds = Menu.Item(Name + "DrawingMapTimeFormat").GetValue<StringList>().SelectedIndex == 1;
                var minimapTotalSeconds = Menu.Item(Name + "DrawingMinimapTimeFormat").GetValue<StringList>().SelectedIndex == 1;
                var mapEnabled = Menu.Item(Name + "DrawingMapEnabled").GetValue<bool>();
                var minimapEnabled = Menu.Item(Name + "DrawingMinimapEnabled").GetValue<bool>();

                if (!mapEnabled && !minimapEnabled)
                    return;

                foreach (var camp in _camps.Where(c => c.Dead))
                {
                    if (camp.NextRespawnTime - Game.Time <= 0)
                        camp.Dead = false;

                    if (mapEnabled && camp.Position.IsOnScreen())
                    {
                        _mapText.DrawTextCentered((camp.NextRespawnTime - (int) Game.Time).FormatTime(mapTotalSeconds),
                            Drawing.WorldToScreen(camp.Position), Color.White);
                    }
                    if (minimapEnabled)
                    {
                        _minimapText.DrawTextCentered((camp.NextRespawnTime - (int) Game.Time).FormatTime(minimapTotalSeconds), camp.MinimapPosition,
                            Color.White);
                    }
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
                _mapText.OnResetDevice();
                _minimapText.OnResetDevice();
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
                _mapText.OnLostDevice();
                _minimapText.OnResetDevice();
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
                if (Global.IoC.IsRegistered<Timers>())
                {
                    _parent = Global.IoC.Resolve<Timers>();
                    if (_parent.Initialized)
                        OnParentInitialized(null, null);
                    else
                        _parent.OnInitialized += OnParentInitialized;
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
                    return;

                Menu = new Menu(Name, Name);

                var drawingMenu = new Menu(Language.Get("G_Drawing"), Name + "Drawing");
                var drawingMapMenu = new Menu(Language.Get("G_Map"), drawingMenu.Name + "Map");
                var drawingMinimapMenu = new Menu(Language.Get("G_Minimap"), drawingMenu.Name + "Minimap");

                drawingMapMenu.AddItem(
                    new MenuItem(drawingMapMenu.Name + "TimeFormat", Language.Get("G_TimeFormat")).SetValue(new StringList(new[] {"mm:ss", "ss"})));
                drawingMapMenu.AddItem(new MenuItem(drawingMapMenu.Name + "FontSize", Language.Get("G_FontSize")).SetValue(new Slider(20, 3, 30)));
                drawingMapMenu.AddItem(new MenuItem(drawingMapMenu.Name + "Enabled", Language.Get("G_Enabled")).SetValue(false));

                drawingMinimapMenu.AddItem(
                    new MenuItem(drawingMinimapMenu.Name + "TimeFormat", Language.Get("G_TimeFormat")).SetValue(new StringList(new[] {"mm:ss", "ss"})));
                drawingMinimapMenu.AddItem(
                    new MenuItem(drawingMinimapMenu.Name + "FontSize", Language.Get("G_FontSize")).SetValue(new Slider(13, 3, 30)));
                drawingMinimapMenu.AddItem(new MenuItem(drawingMinimapMenu.Name + "Enabled", Language.Get("G_Enabled")).SetValue(false));

                drawingMenu.AddSubMenu(drawingMapMenu);
                drawingMenu.AddSubMenu(drawingMinimapMenu);

                Menu.AddSubMenu(drawingMenu);

                Menu.AddItem(new MenuItem(Name + "Enabled", Language.Get("G_Enabled")).SetValue(false));

                _parent.Menu.AddSubMenu(Menu);

                _camps.AddRange(
                    Data.Jungle.Camps.Where(c => c.MapType == Utility.Map.GetMap().Type)
                        .Select(c => new Camp(c.SpawnTime, c.RespawnTime, c.Position, c.Mobs, c.IsBig, c.MapType, c.Team)));

                if (!_camps.Any())
                    return;

                _minimapText = new Font(Drawing.Direct3DDevice,
                    new FontDescription
                    {
                        FaceName = Global.DefaultFont,
                        Height = Menu.Item(Name + "DrawingMinimapFontSize").GetValue<Slider>().Value,
                        OutputPrecision = FontPrecision.Default,
                        Quality = FontQuality.Default
                    });

                _mapText = new Font(Drawing.Direct3DDevice,
                    new FontDescription
                    {
                        FaceName = Global.DefaultFont,
                        Height = Menu.Item(Name + "DrawingMapFontSize").GetValue<Slider>().Value,
                        OutputPrecision = FontPrecision.Default,
                        Quality = FontQuality.Default
                    });

                HandleEvents(_parent);
                RaiseOnInitialized();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private class Camp : Data.Jungle.Camp
        {
            public Camp(float spawnTime, float respawnTime, Vector3 position, List<Data.Jungle.Mob> mobs, bool isBig, Utility.Map.MapType mapType,
                GameObjectTeam team, bool dead = false) : base(spawnTime, respawnTime, position, mobs, isBig, mapType, team)
            {
                Dead = dead;
                Mobs = mobs.Select(mob => new Mob(mob.Name)).ToList();
            }

            public new List<Mob> Mobs { get; private set; }
            public float NextRespawnTime { get; set; }
            public bool Dead { get; set; }
        }

        private class Mob : Data.Jungle.Mob
        {
            public Mob(string name, bool dead = false) : base(name)
            {
                Dead = dead;
            }

            public bool Dead { get; set; }
        }
    }
}