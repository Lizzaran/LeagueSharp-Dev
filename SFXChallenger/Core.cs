#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Core.cs is part of SFXChallenger.

 SFXChallenger is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXChallenger is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXChallenger. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

namespace SFXChallenger
{
    #region

    using System;
    using CustomEventArgs;
    using Interfaces;
    using LeagueSharp;
    using LeagueSharp.Common;
    using SFXLibrary.Logger;
    using Orbwalking = Wrappers.Orbwalking;

    #endregion

    internal class Core
    {
        public delegate void EventHandler(EventArgs args);

        private static IChampion _champion;
        private static int _interval;
        private static int _lastTick;
        private static bool _started;
        private static bool _init;
        private static Orbwalking.OrbwalkingMode _lastMode;

        public static void Init(IChampion champion, int interval)
        {
            _champion = champion;
            _interval = interval;
            _init = true;

            Notifications.AddNotification("SFX: ALPHA - Don't use it.", 30000);
        }

        public static event EventHandler OnPreUpdate;
        public static event EventHandler OnPostUpdate;
        public static event EventHandler OnShutdown;
        public static event EventHandler OnBoot;
        public static event EventHandler<ModeSwitchArgs> OnModeSwitch;

        private static void OnGameUpdate(EventArgs args)
        {
            if (!_init)
                return;
            try
            {
                if (Environment.TickCount - _lastTick >= _interval)
                {
                    _lastTick = Environment.TickCount;

                    if (ObjectManager.Player.IsDead || ObjectManager.Player.HasBuff("Recall"))
                        return;

                    RaiseEvent(OnPreUpdate, args);

                    var mode = _champion.Orbwalker.ActiveMode;
                    if (mode != _lastMode)
                    {
                        if (OnModeSwitch != null)
                        {
                            OnModeSwitch(null, new ModeSwitchArgs(_lastMode, mode));
                        }
                    }
                    _lastMode = mode;

                    _champion.Killsteal();

                    switch (mode)
                    {
                        case Orbwalking.OrbwalkingMode.Combo:
                            _champion.Combo();
                            break;
                        case Orbwalking.OrbwalkingMode.Harass:
                        case Orbwalking.OrbwalkingMode.Mixed:
                            _champion.Harass();
                            break;
                        case Orbwalking.OrbwalkingMode.LaneClear:
                            _champion.LaneClear();
                            break;
                        case Orbwalking.OrbwalkingMode.Flee:
                            _champion.Flee();
                            break;
                    }

                    RaiseEvent(OnPostUpdate, args);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private static void OnGameEnd(EventArgs args)
        {
            Shutdown();
        }

        private static void OnExit(object sender, EventArgs e)
        {
            Shutdown();
        }

        private static void RaiseEvent(EventHandler evt, EventArgs args = null)
        {
            if (evt != null)
            {
                evt(args);
            }
        }

        public static void SetInterval(int interval)
        {
            if (interval > 0)
            {
                _interval = interval;
            }
        }

        public static void Shutdown()
        {
            if (_started)
            {
                RaiseEvent(OnShutdown);
                Game.OnUpdate -= OnGameUpdate;
                AppDomain.CurrentDomain.DomainUnload -= OnExit;
                AppDomain.CurrentDomain.ProcessExit -= OnExit;
                CustomEvents.Game.OnGameEnd -= OnGameEnd;
                _started = false;
            }
        }

        public static void Boot()
        {
            if (!_started)
            {
                RaiseEvent(OnBoot);
                Game.OnUpdate += OnGameUpdate;
                AppDomain.CurrentDomain.DomainUnload += OnExit;
                AppDomain.CurrentDomain.ProcessExit += OnExit;
                CustomEvents.Game.OnGameEnd += OnGameEnd;
                _started = true;
            }
        }

        public static void Reboot()
        {
            if (_started)
            {
                Shutdown();
            }
            Boot();
        }
    }
}