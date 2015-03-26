#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Recall.cs is part of SFXUtility.

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

namespace SFXUtility.Features.Trackers
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using Classes;
    using LeagueSharp;
    using LeagueSharp.Common;
    using LeagueSharp.CommonEx.Core.Events;
    using SFXLibrary.Extensions.NET;
    using SFXLibrary.IoCContainer;
    using SFXLibrary.Logger;
    using ObjectHandler = LeagueSharp.CommonEx.Core.ObjectHandler;

    #endregion

    internal class Recall : Base
    {
        private Trackers _parent;
        private IEnumerable<RecallObject> _recallObjects = new List<RecallObject>();

        public Recall(IContainer container)
            : base(container)
        {
            Load.OnLoad += OnLoad;
        }

        public override bool Enabled
        {
            get
            {
                return _parent != null && _parent.Enabled && Menu != null &&
                       Menu.Item(Name + "Enabled").GetValue<bool>();
            }
        }

        public override string Name
        {
            get { return "Recall"; }
        }

        public event EventHandler<RecallEventArgs> OnStart;
        public event EventHandler<RecallEventArgs> OnFinish;
        public event EventHandler<RecallEventArgs> OnAbort;
        public event EventHandler<RecallEventArgs> OnUnknown;

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
                var count = 0;
                foreach (var recall in _recallObjects)
                {
                    if (recall.LastStatus != Packet.S2C.Teleport.Status.Unknown)
                    {
                        var text = recall.ToString();
                        if (recall.Update() && !string.IsNullOrWhiteSpace(text))
                        {
                            Drawing.DrawText(Drawing.Width - 655, Drawing.Height - 200 + 15*count++, recall.ToColor(),
                                text);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
            }
        }

        private void OnLoad(EventArgs args)
        {
            try
            {
                if (IoC.IsRegistered<Trackers>())
                {
                    _parent = IoC.Resolve<Trackers>();
                    if (_parent.Initialized)
                        OnParentInitialized(null, null);
                    else
                        _parent.OnInitialized += OnParentInitialized;
                }
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
            }
        }

        private void OnParentInitialized(object sender, EventArgs eventArgs)
        {
            try
            {
                if (_parent.Menu == null)
                    return;

                Menu = new Menu(Name, BaseName + Name);

                Menu.AddItem(new MenuItem(Name + "Enabled", "Enabled").SetValue(false));

                _parent.Menu.AddSubMenu(Menu);

                HandleEvents(_parent);

                _recallObjects = ObjectHandler.EnemyHeroes.Select(hero => new RecallObject(hero));

                RaiseOnInitialized();
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
            }
        }

        private void OnObjAiBaseTeleport(GameObject sender, GameObjectTeleportEventArgs args)
        {
            try
            {
                var packet = Packet.S2C.Teleport.Decoded(sender, args);
                var recall = _recallObjects.FirstOrDefault(r => r.Hero.NetworkId == packet.UnitNetworkId);
                if (!Equals(recall, default(RecallObject)))
                {
                    recall.Duration = packet.Duration;
                    recall.LastStatus = packet.Status;

                    switch (packet.Status)
                    {
                        case Packet.S2C.Teleport.Status.Start:
                            OnStart.RaiseEvent(null, new RecallEventArgs(packet.UnitNetworkId));
                            break;

                        case Packet.S2C.Teleport.Status.Finish:
                            OnFinish.RaiseEvent(null, new RecallEventArgs(packet.UnitNetworkId));
                            break;

                        case Packet.S2C.Teleport.Status.Abort:
                            OnAbort.RaiseEvent(null, new RecallEventArgs(packet.UnitNetworkId));
                            break;

                        case Packet.S2C.Teleport.Status.Unknown:
                            OnUnknown.RaiseEvent(null, new RecallEventArgs(packet.UnitNetworkId));
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
            }
        }

        private class RecallObject
        {
            public readonly Obj_AI_Hero Hero;
            private int _duration;
            private float _lastActionTime;
            private Packet.S2C.Teleport.Status _lastStatus;
            private float _recallStart;

            public RecallObject(Obj_AI_Hero hero)
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
                    _recallStart = _lastStatus == Packet.S2C.Teleport.Status.Start ? Game.Time : 0f;
                    _lastActionTime = Game.Time;
                }
            }

            public override string ToString()
            {
                var time = _recallStart + Duration - Game.Time;
                if (time <= 0)
                {
                    time = Game.Time - _lastActionTime;
                }
                switch (LastStatus)
                {
                    case Packet.S2C.Teleport.Status.Start:
                        return string.Format("Recall: {0}({1}%) Teleporting ({2:0.00})", Hero.ChampionName,
                            (int) Hero.HealthPercentage(), time);

                    case Packet.S2C.Teleport.Status.Finish:
                        return string.Format("Recall: {0}({1}%) Teleported ({2:0.00})", Hero.ChampionName,
                            (int) Hero.HealthPercentage(), time);

                    case Packet.S2C.Teleport.Status.Abort:
                        return string.Format("Recall: {0}({1}%) Aborted", Hero.ChampionName,
                            (int) Hero.HealthPercentage());

                    default:
                        return string.Empty;
                }
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
                var additional = LastStatus == Packet.S2C.Teleport.Status.Start ? Duration + 20f : 20f;
                if (_lastActionTime + additional <= Game.Time)
                {
                    _lastActionTime = 0f;
                    return false;
                }
                return true;
            }
        }
    }

    public class RecallEventArgs : EventArgs
    {
        private readonly int _unitNetworkId;

        public RecallEventArgs(int unitNetworkId)
        {
            _unitNetworkId = unitNetworkId;
        }

        public int UnitNetworkId
        {
            get { return _unitNetworkId; }
        }
    }
}