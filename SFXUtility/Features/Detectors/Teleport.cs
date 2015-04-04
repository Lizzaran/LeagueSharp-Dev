#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Teleport.cs is part of SFXUtility.

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


#pragma warning disable 618

namespace SFXUtility.Features.Detectors
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using Classes;
    using LeagueSharp;
    using LeagueSharp.Common;
    using SFXLibrary;
    using SFXLibrary.Extensions.NET;
    using SFXLibrary.Logger;

    #endregion

    internal class Teleport : Base
    {
        private Detectors _parent;
        private List<TeleportObject> _teleportObjects = new List<TeleportObject>();

        public override bool Enabled
        {
            get { return _parent != null && _parent.Enabled && Menu != null && Menu.Item(Name + "Enabled").GetValue<bool>(); }
        }

        public override string Name
        {
            get { return Language.Get("F_Teleport"); }
        }

        public event EventHandler<TeleportEventArgs> OnStart;
        public event EventHandler<TeleportEventArgs> OnFinish;
        public event EventHandler<TeleportEventArgs> OnAbort;
        public event EventHandler<TeleportEventArgs> OnUnknown;

        protected override void OnEnable()
        {
            Obj_AI_Base.OnTeleport += OnObjAiBaseTeleport;
            Drawing.OnDraw += OnDrawingDraw;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            Obj_AI_Base.OnTeleport += OnObjAiBaseTeleport;
            Drawing.OnDraw += OnDrawingDraw;
            base.OnDisable();
        }

        private void OnDrawingDraw(EventArgs args)
        {
            try
            {
                if (!Menu.Item(Name + "DrawingTextEnabled").GetValue<bool>())
                    return;

                var count = 0;
                foreach (var teleport in _teleportObjects)
                {
                    if (teleport.LastStatus != Packet.S2C.Teleport.Status.Unknown)
                    {
                        var text = teleport.ToString();
                        if (teleport.Update() && !string.IsNullOrWhiteSpace(text))
                        {
                            Drawing.DrawText(Drawing.Width - 655, Drawing.Height - 200 + 15*count++, teleport.ToColor(), text);
                        }
                    }
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
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + "TextEnabled", Language.Get("Teleport_DrawingTextEnabled")).SetValue(true));

                Menu.AddSubMenu(drawingMenu);

                Menu.AddItem(new MenuItem(Name + "Enabled", Language.Get("G_Enabled")).SetValue(false));

                _parent.Menu.AddSubMenu(Menu);

                _teleportObjects = HeroManager.Enemies.Select(hero => new TeleportObject(hero)).ToList();

                HandleEvents(_parent);
                RaiseOnInitialized();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnObjAiBaseTeleport(GameObject sender, GameObjectTeleportEventArgs args)
        {
            try
            {
                var packet = Packet.S2C.Teleport.Decoded(sender, args);
                var teleport = _teleportObjects.FirstOrDefault(r => r.Hero.NetworkId == packet.UnitNetworkId);
                if (teleport != null)
                {
                    teleport.Duration = packet.Duration;
                    teleport.LastStatus = packet.Status;
                    teleport.LastType = packet.Type;

                    switch (packet.Status)
                    {
                        case Packet.S2C.Teleport.Status.Start:
                            OnStart.RaiseEvent(null, new TeleportEventArgs(packet.UnitNetworkId));
                            break;

                        case Packet.S2C.Teleport.Status.Finish:
                            OnFinish.RaiseEvent(null, new TeleportEventArgs(packet.UnitNetworkId));
                            break;

                        case Packet.S2C.Teleport.Status.Abort:
                            OnAbort.RaiseEvent(null, new TeleportEventArgs(packet.UnitNetworkId));
                            break;

                        case Packet.S2C.Teleport.Status.Unknown:
                            OnUnknown.RaiseEvent(null, new TeleportEventArgs(packet.UnitNetworkId));
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private class TeleportObject
        {
            public readonly Obj_AI_Hero Hero;
            private int _duration;
            private float _lastActionTime;
            private Packet.S2C.Teleport.Status _lastStatus;
            private float _teleportStart;

            public TeleportObject(Obj_AI_Hero hero)
            {
                Hero = hero;
                LastStatus = Packet.S2C.Teleport.Status.Unknown;
            }

            public int Duration
            {
                private get { return _duration; }
                set { _duration = value/1000; }
            }

            public Packet.S2C.Teleport.Status LastStatus
            {
                get { return _lastStatus; }
                set
                {
                    _lastStatus = value;
                    _teleportStart = _lastStatus == Packet.S2C.Teleport.Status.Start ? Game.Time : 0f;
                    _lastActionTime = Game.Time;
                }
            }

            // ReSharper disable once MemberCanBePrivate.Local
            public Packet.S2C.Teleport.Type LastType { get; set; }

            public override string ToString()
            {
                var time = _teleportStart + Duration - Game.Time;
                if (time <= 0)
                {
                    time = Game.Time - _lastActionTime;
                }
                var hPercent = (int) (ObjectManager.Player.Health/ObjectManager.Player.MaxHealth)*100;
                switch (LastType)
                {
                    case Packet.S2C.Teleport.Type.Recall:
                        switch (LastStatus)
                        {
                            case Packet.S2C.Teleport.Status.Start:
                                return string.Format("{0}: {2}({3}%) {1} ({4:0.00})", Language.Get("Teleport_Recall"),
                                    Language.Get("Teleport_Recalling"), Hero.ChampionName, hPercent, time);
                            case Packet.S2C.Teleport.Status.Finish:
                                return string.Format("{0}: {2}({3}%) {1} ({4:0.00})", Language.Get("Teleport_Recall"),
                                    Language.Get("Teleport_Recalled"), Hero.ChampionName, hPercent, time);
                            case Packet.S2C.Teleport.Status.Abort:
                                return string.Format("{0}: {2}({3}%) {1}", Language.Get("Teleport_Recall"), Language.Get("Teleport_Aborted"),
                                    Hero.ChampionName, hPercent);
                        }
                        break;
                    case Packet.S2C.Teleport.Type.Teleport:
                        switch (LastStatus)
                        {
                            case Packet.S2C.Teleport.Status.Start:
                                return string.Format("{0}: {2}({3}%) {1} ({4:0.00})", Language.Get("Teleport_Teleport"),
                                    Language.Get("Teleport_Teleporting"), Hero.ChampionName, hPercent, time);
                            case Packet.S2C.Teleport.Status.Finish:
                                return string.Format("{0}: {2}({3}%) {1} ({4:0.00})", Language.Get("Teleport_Teleport"),
                                    Language.Get("Teleport_Teleported"), Hero.ChampionName, hPercent, time);
                            case Packet.S2C.Teleport.Status.Abort:
                                return string.Format("{0}: {2}({3}%) {1}", Language.Get("Teleport_Teleport"), Language.Get("Teleport_Aborted"),
                                    Hero.ChampionName, hPercent);
                        }
                        break;
                    case Packet.S2C.Teleport.Type.Shen:
                    case Packet.S2C.Teleport.Type.TwistedFate:
                        switch (LastStatus)
                        {
                            case Packet.S2C.Teleport.Status.Start:
                                return string.Format("{0}: {2}({3}%) {1} ({4:0.00})", Language.Get("Teleport_Ability"),
                                    Language.Get("Teleport_Transporting"), Hero.ChampionName, hPercent, time);
                            case Packet.S2C.Teleport.Status.Finish:
                                return string.Format("{0}: {2}({3}%) {1} ({4:0.00})", Language.Get("Teleport_Ability"),
                                    Language.Get("Teleport_Transported"), Hero.ChampionName, hPercent, time);
                            case Packet.S2C.Teleport.Status.Abort:
                                return string.Format("{0}: {2}({3}%) {1}", Language.Get("Teleport_Ability"), Language.Get("Teleport_Aborted"),
                                    Hero.ChampionName, hPercent);
                        }
                        break;
                }
                return string.Empty;
            }

            public Color ToColor()
            {
                switch (LastStatus)
                {
                    case Packet.S2C.Teleport.Status.Start:
                        return Color.Beige;

                    case Packet.S2C.Teleport.Status.Finish:
                        return Color.GreenYellow;

                    case Packet.S2C.Teleport.Status.Abort:
                        return Color.Red;

                    default:
                        return Color.Black;
                }
            }

            public bool Update()
            {
                var additional = LastStatus == Packet.S2C.Teleport.Status.Start ? Duration + 15f : 15f;
                if (_lastActionTime + additional <= Game.Time)
                {
                    _lastActionTime = 0f;
                    return false;
                }
                return true;
            }
        }
    }

    public class TeleportEventArgs : EventArgs
    {
        private readonly int _unitNetworkId;

        public TeleportEventArgs(int unitNetworkId)
        {
            _unitNetworkId = unitNetworkId;
        }

        public int UnitNetworkId
        {
            get { return _unitNetworkId; }
        }
    }
}