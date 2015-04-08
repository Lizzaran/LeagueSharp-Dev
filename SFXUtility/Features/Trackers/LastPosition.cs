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

namespace SFXUtility.Features.Trackers
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using Classes;
    using Detectors;
    using LeagueSharp;
    using LeagueSharp.Common;
    using Properties;
    using SFXLibrary;
    using SFXLibrary.Extensions.NET;
    using SFXLibrary.Extensions.SharpDX;
    using SFXLibrary.Logger;
    using SharpDX;
    using SharpDX.Direct3D9;
    using Color = SharpDX.Color;
    using Font = SharpDX.Direct3D9.Font;

    #endregion

    internal class LastPosition : Base
    {
        private readonly Dictionary<int, Texture> _heroTextures = new Dictionary<int, Texture>();
        private readonly List<LastPositionStruct> _lastPositions = new List<LastPositionStruct>();
        private Trackers _parent;
        private Vector2 _spawnPoint;
        private Sprite _sprite;
        private Texture _teleportTexture;
        private Font _text;

        public override bool Enabled
        {
            get { return !Unloaded && _parent != null && _parent.Enabled && Menu != null && Menu.Item(Name + "Enabled").GetValue<bool>(); }
        }

        public override string Name
        {
            get { return Language.Get("F_LastPosition"); }
        }

        protected override void OnGameLoad(EventArgs args)
        {
            try
            {
                if (Global.IoC.IsRegistered<Trackers>())
                {
                    _parent = Global.IoC.Resolve<Trackers>();
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
            Drawing.OnPreReset += OnDrawingPreReset;
            Drawing.OnPostReset += OnDrawingPostReset;
            Drawing.OnEndScene += OnDrawingEndScene;

            base.OnEnable();
        }

        protected override void OnDisable()
        {
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

        private void OnDrawingEndScene(EventArgs args)
        {
            try
            {
                if (Drawing.Direct3DDevice == null || Drawing.Direct3DDevice.IsDisposed)
                    return;

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
                            lp.LastSeen = Game.Time;
                    }
                    if (!lp.Hero.IsVisible && !lp.Hero.IsDead)
                    {
                        var pos = Drawing.WorldToMinimap(lp.Hero.Position);

                        _sprite.DrawCentered(_heroTextures[lp.Hero.NetworkId], lp.Teleported ? _spawnPoint : pos);
                        if (lp.IsTeleporting)
                            _sprite.DrawCentered(_teleportTexture, pos);

                        if (timer && lp.LastSeen != 0f && (Game.Time - lp.LastSeen) > 3f)
                        {
                            _text.DrawTextCentered((Game.Time - lp.LastSeen).FormatTime(totalSeconds), new Vector2(pos.X, pos.Y + 15 + timerOffset),
                                Color.White);
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

        private void OnDrawingPostReset(EventArgs args)
        {
            _text.OnResetDevice();
            _sprite.OnResetDevice();
        }

        private void OnDrawingPreReset(EventArgs args)
        {
            _text.OnLostDevice();
            _sprite.OnLostDevice();
        }

        private void OnParentInitialized(object sender, EventArgs eventArgs)
        {
            try
            {
                if (_parent.Menu == null)
                    return;

                Menu = new Menu(Name, Name);

                var drawingMenu = new Menu(Language.Get("G_Drawing"), Name + "Drawing");
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "TimeFormat", Language.Get("G_TimeFormat")).SetValue(new StringList(new[] {"mm:ss", "ss"})));
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + "FontSize", Language.Get("G_FontSize")).SetValue(new Slider(13, 3, 30)));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "SSTimerOffset", Language.Get("LastPosition_SSTimer") + " " + Language.Get("G_Offset")).SetValue(
                        new Slider(5, 0, 20)));

                Menu.AddSubMenu(drawingMenu);

                Menu.AddItem(new MenuItem(Name + "SSTimer", Language.Get("LastPosition_SSTimer")).SetValue(false));
                Menu.AddItem(new MenuItem(Name + "Enabled", Language.Get("G_Enabled")).SetValue(false));

                _parent.Menu.AddSubMenu(Menu);

                if (Global.IoC.IsRegistered<Teleport>())
                {
                    var rt = Global.IoC.Resolve<Teleport>();
                    rt.OnFinish += TeleportFinish;
                    rt.OnStart += TeleportStart;
                    rt.OnAbort += TeleportAbort;
                    rt.OnUnknown += TeleportAbort;
                }

                var spawn = ObjectManager.Get<GameObject>().FirstOrDefault(s => s is Obj_SpawnPoint && s.IsEnemy);
                _spawnPoint = spawn != null ? Drawing.WorldToMinimap(spawn.Position) : Vector2.Zero;

                foreach (var enemy in HeroManager.Enemies)
                {
                    _heroTextures[enemy.NetworkId] =
                        ((Bitmap) Resources.ResourceManager.GetObject(string.Format("LP_{0}", enemy.ChampionName)) ?? Resources.LP_Aatrox).ToTexture();
                    _lastPositions.Add(new LastPositionStruct(enemy));
                }

                _sprite = new Sprite(Drawing.Direct3DDevice);
                _teleportTexture = Resources.LP_Teleport.ToTexture();
                _text = new Font(Drawing.Direct3DDevice,
                    new FontDescription
                    {
                        FaceName = Global.DefaultFont,
                        Height = Menu.Item(Name + "DrawingFontSize").GetValue<Slider>().Value,
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