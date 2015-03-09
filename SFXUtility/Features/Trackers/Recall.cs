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
    using SFXLibrary;
    using SFXLibrary.IoCContainer;
    using SFXLibrary.Logger;

    #endregion

    internal class Recall : Base
    {
        private List<RecallObject> _recallObjects = new List<RecallObject>();
        private Trackers _trackers;

        public Recall(IContainer container)
            : base(container)
        {
            CustomEvents.Game.OnGameLoad += OnGameLoad;
        }

        public override bool Enabled
        {
            get
            {
                return _trackers != null && _trackers.Enabled && Menu != null &&
                       Menu.Item(Name + "Enabled").GetValue<bool>();
            }
        }

        public override string Name
        {
            get { return "Recall"; }
        }

        private void OnDraw(EventArgs args)
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

        private void OnGameLoad(EventArgs args)
        {
            try
            {
                if (IoC.IsRegistered<Trackers>() && IoC.Resolve<Trackers>().Initialized)
                {
                    TrackersLoaded(IoC.Resolve<Trackers>());
                }
                else
                {
                    if (IoC.IsRegistered<Mediator>())
                    {
                        IoC.Resolve<Mediator>().Register("Trackers_initialized", TrackersLoaded);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
            }
        }

        private void TrackersLoaded(object o)
        {
            try
            {
                var trackers = o as Trackers;
                if (trackers != null && trackers.Menu != null)
                {
                    _trackers = trackers;

                    Menu = new Menu(Name, Name);

                    Menu.AddItem(new MenuItem(Name + "Enabled", "Enabled").SetValue(true));

                    Menu.Item(Name + "Enabled").ValueChanged +=
                        (sender, args) =>
                            IoC.Resolve<Mediator>().NotifyColleagues(Name + "_Enabled", args.GetNewValue<bool>());

                    _trackers.Menu.AddSubMenu(Menu);

                    _trackers.Menu.Item(_trackers.Name + "Enabled").ValueChanged +=
                        delegate(object sender, OnValueChangeEventArgs args)
                        {
                            if (args.GetNewValue<bool>())
                            {
                                if (Menu != null && Menu.Item(Name + "Enabled").GetValue<bool>())
                                {
                                    Obj_AI_Base.OnTeleport += OnObjAiBaseOnTeleport;
                                    Drawing.OnDraw += OnDraw;
                                }
                            }
                            else
                            {
                                Obj_AI_Base.OnTeleport -= OnObjAiBaseOnTeleport;
                                Drawing.OnDraw -= OnDraw;
                            }
                        };

                    Menu.Item(Name + "Enabled").ValueChanged +=
                        delegate(object sender, OnValueChangeEventArgs args)
                        {
                            if (args.GetNewValue<bool>())
                            {
                                if (_trackers != null && _trackers.Menu != null &&
                                    _trackers.Menu.Item(_trackers.Name + "Enabled").GetValue<bool>())
                                {
                                    Obj_AI_Base.OnTeleport += OnObjAiBaseOnTeleport;
                                    Drawing.OnDraw += OnDraw;
                                }
                            }
                            else
                            {
                                Obj_AI_Base.OnTeleport -= OnObjAiBaseOnTeleport;
                                Drawing.OnDraw -= OnDraw;
                            }
                        };

                    if (Enabled)
                    {
                        Obj_AI_Base.OnTeleport += OnObjAiBaseOnTeleport;
                        Drawing.OnDraw += OnDraw;
                    }

                    _recallObjects =
                        ObjectManager.Get<Obj_AI_Hero>()
                            .Where(hero => hero.IsValid && hero.IsEnemy)
                            .Select(hero => new RecallObject(hero))
                            .ToList();

                    IoC.Resolve<Mediator>().NotifyColleagues(Name + "_Enabled", Menu.Item(Name + "Enabled"));

                    Initialized = true;
                }
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
            }
        }

        private void OnObjAiBaseOnTeleport(GameObject sender, GameObjectTeleportEventArgs args)
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
                            IoC.Resolve<Mediator>().NotifyColleagues(Name + "_Start", packet.UnitNetworkId);
                            break;

                        case Packet.S2C.Teleport.Status.Finish:
                            IoC.Resolve<Mediator>().NotifyColleagues(Name + "_Finish", packet.UnitNetworkId);
                            break;

                        case Packet.S2C.Teleport.Status.Abort:
                            IoC.Resolve<Mediator>().NotifyColleagues(Name + "_Abort", packet.UnitNetworkId);
                            break;

                        case Packet.S2C.Teleport.Status.Unknown:
                            IoC.Resolve<Mediator>().NotifyColleagues(Name + "_Unknown", packet.UnitNetworkId);
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
}