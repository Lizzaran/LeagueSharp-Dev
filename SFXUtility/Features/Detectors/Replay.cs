#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Replay.cs is part of SFXUtility.

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
using System.Threading;
using System.Timers;
using LeagueSharp;
using LeagueSharp.Common;
using SFXLibrary.Extensions.NET;
using SFXLibrary.Extensions.SharpDX;
using SFXLibrary.Logger;
using SFXUtility.Classes;
using SFXUtility.Properties;
using SharpDX.Direct3D9;
using Timer = System.Timers.Timer;

#endregion

namespace SFXUtility.Features.Detectors
{
    internal class Replay : Base
    {
        private bool _isRecording;
        private Detectors _parent;
        private Texture _recordTexture;
        private Sprite _sprite;
        private Timer _timer;
        public Replay(SFXUtility sfx) : base(sfx) {}

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
            get { return Global.Lang.Get("F_Replay"); }
        }

        protected override void OnEnable()
        {
            Drawing.OnPreReset += OnDrawingPreReset;
            Drawing.OnPostReset += OnDrawingPostReset;
            Drawing.OnEndScene += OnDrawingEndScene;

            if (!_isRecording)
            {
                OnTimerElapsed(null, null);

                if (_timer != null)
                {
                    _timer.Enabled = true;
                    _timer.Start();
                }
            }

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            Drawing.OnPreReset -= OnDrawingPreReset;
            Drawing.OnPostReset -= OnDrawingPostReset;
            Drawing.OnEndScene -= OnDrawingEndScene;

            if (_timer != null && _timer.Enabled)
            {
                _timer.Enabled = false;
                _timer.Stop();
            }

            OnUnload(null, new UnloadEventArgs());

            base.OnDisable();
        }

        protected override void OnUnload(object sender, UnloadEventArgs args)
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

        private void OnDrawingEndScene(EventArgs args)
        {
            try
            {
                if (Drawing.Direct3DDevice == null || Drawing.Direct3DDevice.IsDisposed || !_isRecording ||
                    !Menu.Item(Name + "IsRecording").GetValue<bool>())
                {
                    return;
                }

                _sprite.Begin(SpriteFlags.AlphaBlend);

                _sprite.DrawCentered(_recordTexture, 20, 20);

                _sprite.End();
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
                if (_sprite != null)
                {
                    _sprite.OnResetDevice();
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
                if (_sprite != null)
                {
                    _sprite.OnLostDevice();
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
                if (Global.IoC.IsRegistered<Detectors>())
                {
                    _parent = Global.IoC.Resolve<Detectors>();
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

                Menu.AddItem(new MenuItem(Name + "DoRecord", Global.Lang.Get("Replay_DoRecord")).SetValue(false));
                Menu.AddItem(new MenuItem(Name + "IsRecording", Global.Lang.Get("Replay_NotifyRecord")).SetValue(false));
                Menu.AddItem(
                    new MenuItem(Name + "CheckInterval", Global.Lang.Get("Replay_CheckInterval")).SetValue(
                        new Slider(3, 1, 10))).ValueChanged += delegate(object o, OnValueChangeEventArgs args)
                        {
                            if (_timer != null)
                            {
                                _timer.Interval = args.GetNewValue<Slider>().Value * 60 * 1000;
                            }
                        };

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
            _timer = new Timer();
            _sprite = new Sprite(Drawing.Direct3DDevice);
            _recordTexture = Resources.RC_On.ToTexture();

            _timer.Enabled = false;
            _timer.Interval = Menu.Item(Name + "CheckInterval").GetValue<Slider>().Value * 60 * 1000;
            _timer.Elapsed += OnTimerElapsed;

            base.OnInitialize();
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            try
            {
                new Thread(
                    async () =>
                    {
                        try
                        {
                            if (!_isRecording && Menu.Item(Name + "DoRecord").GetValue<bool>())
                            {
                                _isRecording = (await Spectator.DoRecord());
                            }
                            if (!_isRecording && Menu.Item(Name + "IsRecording").GetValue<bool>())
                            {
                                _isRecording = (await Spectator.IsRecoding());
                            }
                            if (_isRecording)
                            {
                                _timer.Enabled = false;
                                _timer.Stop();
                            }
                        }
                        catch (Exception ex)
                        {
                            Global.Logger.AddItem(new LogItem(ex));
                        }
                    }).Start();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }
}