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
using SFXUtility.Classes;
using SFXUtility.Library;
using SFXUtility.Library.Extensions.NET;
using SFXUtility.Library.Extensions.SharpDX;
using SFXUtility.Library.Logger;
using SFXUtility.Properties;
using SharpDX;
using SharpDX.Direct3D9;

#endregion

#pragma warning disable 618

namespace SFXUtility.Features.Trackers
{
    internal class LastPosition : Child<Trackers>
    {
        private readonly Dictionary<int, Texture> _heroTextures = new Dictionary<int, Texture>();
        private readonly List<LastPositionStruct> _lastPositions = new List<LastPositionStruct>();
        private Vector2 _spawnPoint;
        private Sprite _sprite;
        private Texture _teleportTexture;
        private Font _text;

        public LastPosition(Trackers parent) : base(parent)
        {
            OnLoad();
        }

        public override string Name
        {
            get { return "Last Position"; }
        }

        protected override void OnEnable()
        {
            Drawing.OnEndScene += OnDrawingEndScene;
            Obj_AI_Base.OnTeleport += OnObjAiBaseTeleport;

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            Drawing.OnEndScene -= OnDrawingEndScene;
            Obj_AI_Base.OnTeleport -= OnObjAiBaseTeleport;

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

        private void OnObjAiBaseTeleport(Obj_AI_Base sender, GameObjectTeleportEventArgs args)
        {
            try
            {
                var packet = Packet.S2C.Teleport.Decoded(sender, args);
                var lastPosition = _lastPositions.FirstOrDefault(e => e.Hero.NetworkId == packet.UnitNetworkId);
                if (lastPosition != null)
                {
                    switch (packet.Status)
                    {
                        case Packet.S2C.Teleport.Status.Start:
                            lastPosition.IsTeleporting = true;
                            break;
                        case Packet.S2C.Teleport.Status.Abort:
                            lastPosition.IsTeleporting = false;
                            break;
                        case Packet.S2C.Teleport.Status.Finish:
                            lastPosition.Teleported = true;
                            lastPosition.IsTeleporting = false;
                            lastPosition.LastSeen = Game.Time;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected override sealed void OnLoad()
        {
            try
            {
                Menu = new Menu(Name, Name);
                var drawingMenu = new Menu("Drawing", Name + "Drawing");
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "TimeFormat", "Time Format").SetValue(
                        new StringList(new[] { "mm:ss", "ss" })));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "FontSize", "Font Size").SetValue(new Slider(13, 3, 30)));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "SSTimerOffset", "SS Timer Offset").SetValue(new Slider(5, 0, 20)));

                Menu.AddSubMenu(drawingMenu);

                Menu.AddItem(new MenuItem(Name + "SSTimer", "SS Timer").SetValue(false));
                Menu.AddItem(new MenuItem(Name + "Enabled", "Enabled").SetValue(false));

                Parent.Menu.AddSubMenu(Menu);

                _sprite = MDrawing.GetSprite();
                _text = MDrawing.GetFont(Menu.Item(Name + "DrawingFontSize").GetValue<Slider>().Value);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected override void OnInitialize()
        {
            try
            {
                if (!GameObjects.EnemyHeroes.Any())
                {
                    OnUnload(null, new UnloadEventArgs(true));
                    return;
                }

                _teleportTexture = Resources.LP_Teleport.ToTexture();

                var spawn = GameObjects.EnemySpawnPoints.FirstOrDefault();
                _spawnPoint = spawn != null ? Drawing.WorldToMinimap(spawn.Position) : Vector2.Zero;

                foreach (var enemy in GameObjects.EnemyHeroes)
                {
                    _heroTextures[enemy.NetworkId] =
                        (ImageLoader.Load("LP", enemy.ChampionName) ?? Resources.LP_Default).ToTexture();
                    _lastPositions.Add(new LastPositionStruct(enemy));
                }

                base.OnInitialize();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
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