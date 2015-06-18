#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 LastPosition.cs is part of SFXUtility.

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
using SFXUtility.Features.Detectors;
using SFXUtility.Properties;
using SharpDX;
using SharpDX.Direct3D9;

#endregion

namespace SFXUtility.Features.Trackers
{
    internal class LastPosition : Child<Trackers>
    {
        private Dictionary<int, Texture> _heroTextures;
        private List<LastPositionStruct> _lastPositions;
        private Vector2 _spawnPoint;
        private Sprite _sprite;
        private Texture _teleportTexture;
        private Font _text;
        public LastPosition(SFXUtility sfx) : base(sfx) {}

        public override string Name
        {
            get { return Global.Lang.Get("F_LastPosition"); }
        }

        private void TeleportAbort(object sender, TeleportEventArgs teleportEventArgs)
        {
            var lastPosition = _lastPositions.FirstOrDefault(e => e.Hero.NetworkId == teleportEventArgs.UnitNetworkId);
            if (lastPosition != null)
            {
                lastPosition.IsTeleporting = false;
            }
        }

        private void TeleportFinish(object sender, TeleportEventArgs teleportEventArgs)
        {
            var lastPosition = _lastPositions.FirstOrDefault(e => e.Hero.NetworkId == teleportEventArgs.UnitNetworkId);
            if (lastPosition != null)
            {
                lastPosition.Teleported = true;
                lastPosition.IsTeleporting = false;
            }
        }

        private void TeleportStart(object sender, TeleportEventArgs teleportEventArgs)
        {
            var lastPosition = _lastPositions.FirstOrDefault(e => e.Hero.NetworkId == teleportEventArgs.UnitNetworkId);
            if (lastPosition != null)
            {
                lastPosition.IsTeleporting = true;
            }
        }

        protected override void OnEnable()
        {
            Drawing.OnEndScene += OnDrawingEndScene;

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            Drawing.OnEndScene -= OnDrawingEndScene;

            base.OnDisable();
        }

        private void OnDrawingEndScene(EventArgs args)
        {
            try
            {
                if (Drawing.Direct3DDevice == null || Drawing.Direct3DDevice.IsDisposed)
                {
                    return;
                }

                var totalSeconds = Menu.Item(Name + "DrawingTimeFormat").GetValue<StringList>().SelectedIndex == 1;
                var timerOffset = Menu.Item(Name + "DrawingSSTimerOffset").GetValue<Slider>().Value;
                var timer = Menu.Item(Name + "SSTimer").GetValue<bool>();
                _sprite.Begin(SpriteFlags.AlphaBlend);
                foreach (var lp in _lastPositions)
                {
                    if (lp.Hero.IsVisible)
                    {
                        lp.Teleported = false;
                        if (!lp.Hero.IsDead)
                        {
                            lp.LastSeen = Game.Time;
                        }
                    }
                    if (!lp.Hero.IsVisible && !lp.Hero.IsDead)
                    {
                        var pos = lp.Teleported ? _spawnPoint : Drawing.WorldToMinimap(lp.Hero.Position);

                        _sprite.DrawCentered(_heroTextures[lp.Hero.NetworkId], pos);
                        if (lp.IsTeleporting)
                        {
                            _sprite.DrawCentered(_teleportTexture, pos);
                        }

                        if (timer && !lp.LastSeen.Equals(0f) && (Game.Time - lp.LastSeen) > 3f)
                        {
                            _text.DrawTextCentered(
                                (Game.Time - lp.LastSeen).FormatTime(totalSeconds),
                                new Vector2(pos.X, pos.Y + 15 + timerOffset), Color.White);
                        }
                    }
                }
                _sprite.End();
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
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "TimeFormat", Global.Lang.Get("G_TimeFormat")).SetValue(
                        new StringList(new[] { "mm:ss", "ss" })));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "FontSize", Global.Lang.Get("G_FontSize")).SetValue(
                        new Slider(13, 3, 30)));
                drawingMenu.AddItem(
                    new MenuItem(
                        drawingMenu.Name + "SSTimerOffset",
                        Global.Lang.Get("LastPosition_SSTimer") + " " + Global.Lang.Get("G_Offset")).SetValue(
                            new Slider(5, 0, 20)));

                Menu.AddSubMenu(drawingMenu);

                Menu.AddItem(new MenuItem(Name + "SSTimer", Global.Lang.Get("LastPosition_SSTimer")).SetValue(false));
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
            _heroTextures = new Dictionary<int, Texture>();
            _lastPositions = new List<LastPositionStruct>();

            if (Global.IoC.IsRegistered<Teleport>())
            {
                var rt = Global.IoC.Resolve<Teleport>();
                rt.OnFinish += TeleportFinish;
                rt.OnStart += TeleportStart;
                rt.OnAbort += TeleportAbort;
                rt.OnUnknown += TeleportAbort;
            }

            var spawn = GameObjects.EnemySpawnPoints.FirstOrDefault();
            _spawnPoint = spawn != null ? Drawing.WorldToMinimap(spawn.Position) : Vector2.Zero;

            foreach (var enemy in GameObjects.EnemyHeroes)
            {
                _heroTextures[enemy.NetworkId] =
                    (ImageLoader.Load("LP", enemy.ChampionName) ?? Resources.LP_Default).ToTexture();
                _lastPositions.Add(new LastPositionStruct(enemy));
            }

            _sprite = MDrawing.GetSprite();
            _teleportTexture = Resources.LP_Teleport.ToTexture();
            _text = MDrawing.GetFont(Menu.Item(Name + "DrawingFontSize").GetValue<Slider>().Value);

            base.OnInitialize();
        }

        internal class LastPositionStruct
        {
            public readonly Obj_AI_Hero Hero;
            public bool IsTeleporting;
            public float LastSeen;
            public bool Teleported;

            public LastPositionStruct(Obj_AI_Hero hero)
            {
                Hero = hero;
            }
        }
    }
}