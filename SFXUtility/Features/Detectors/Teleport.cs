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
    using System.Linq;
    using Classes;
    using LeagueSharp;
    using LeagueSharp.Common;
    using SFXLibrary;
    using SFXLibrary.Extensions.NET;
    using SFXLibrary.Logger;
    using SharpDX;
    using SharpDX.Direct3D9;
    using Color = System.Drawing.Color;
    using Font = SharpDX.Direct3D9.Font;

    #endregion

    internal class Teleport : Base
    {
        private Detectors _parent;
        private List<TeleportObject> _teleportObjects = new List<TeleportObject>();
        private Font _text;

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
            Drawing.OnDraw += OnDrawingDraw;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            Drawing.OnDraw -= OnDrawingDraw;
            base.OnDisable();
        }

        private void OnDrawingDraw(EventArgs args)
        {
            try
            {
                if (Menu.Item(Name + "DrawingTextEnabled").GetValue<bool>())
                {
                    var posX = Drawing.Width*0.66f;
                    var posY = Drawing.Height*0.75f;
                    var count = 0;
                    foreach (var teleport in _teleportObjects.Where(t => t.LastStatus != Packet.S2C.Teleport.Status.Unknown && t.Update()))
                    {
                        var text = teleport.ToString();
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            Drawing.DrawText(posX, posY + 15*count++, teleport.ToColor(), text);
                        }
                    }
                }

                // Credits: Beaving
                if (Menu.Item(Name + "DrawingBarEnabled").GetValue<bool>())
                {
                    const int barHeight = 10;
                    const int seperatorHeight = barHeight/2;
                    var top = true;
                    var posX = Drawing.Width*0.425f;
                    var posY = Drawing.Height*0.75f;
                    var barWidth = (int) (Drawing.Width - 2*posX);
                    var scale = (float) barWidth/8;
                    var teleports = _teleportObjects.Where(t => t.Countdown > 0).OrderBy(t => t.Countdown);

                    foreach (var teleport in teleports)
                    {
                        var hPercent = ((int) ((teleport.Hero.Health/teleport.Hero.MaxHealth)*100)).ToString();
                        var color = teleport.ToColor();
                        var width = (int) (scale*teleport.Countdown);
                        width = width > barWidth ? barWidth : width;
                        Draw.RectangleFilled(new Vector2(posX, posY), width, barHeight, color.ToArgb(100));
                        Draw.Line(new Vector2(posX + width, (top ? posY - seperatorHeight - barHeight/2f : posY + barHeight/2f + 2)), 1,
                            seperatorHeight, Color.White);
                        _text.DrawText(null, teleport.Hero.ChampionName, (int) (posX + width - teleport.Hero.ChampionName.Length*3),
                            (top
                                ? (int) (posY - barHeight - seperatorHeight - _text.Description.Height/2f)
                                : (int) (posY + barHeight + _text.Description.Height/2f)), new ColorBGRA(color.R, color.G, color.B, color.A));
                        _text.DrawText(null, hPercent, (int) (posX + width - hPercent.Length*3 - 1),
                            (top
                                ? (int) (posY - barHeight - 3 - seperatorHeight - _text.Description.Height)
                                : (int) (posY + barHeight + 3 + _text.Description.Height)), new ColorBGRA(color.R, color.G, color.B, color.A));
                        top = !top;
                    }

                    if (teleports.Any())
                        Draw.Rectangle(new Vector2(posX, posY), barWidth, barHeight, 1, Color.White);
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

                _text = new Font(Drawing.Direct3DDevice,
                    new FontDescription {FaceName = "Calibri", Height = 13, OutputPrecision = FontPrecision.Default, Quality = FontQuality.Default});

                Menu = new Menu(Name, Name);

                var drawingMenu = new Menu(Language.Get("G_Drawing"), Name + "Drawing");

                var drawingTextMenu = new Menu(Language.Get("G_Text"), drawingMenu.Name + "Text");
                drawingTextMenu.AddItem(new MenuItem(drawingTextMenu.Name + "Enabled", Language.Get("G_Enabled")).SetValue(false));

                var drawingBarMenu = new Menu(Language.Get("G_Bar"), drawingMenu.Name + "Bar");
                drawingBarMenu.AddItem(new MenuItem(drawingBarMenu.Name + "Enabled", Language.Get("G_Enabled")).SetValue(false));

                drawingMenu.AddSubMenu(drawingTextMenu);
                drawingMenu.AddSubMenu(drawingBarMenu);

                Menu.AddSubMenu(drawingMenu);

                Menu.AddItem(new MenuItem(Name + "Enabled", Language.Get("G_Enabled")).SetValue(false));

                _parent.Menu.AddSubMenu(Menu);

                _teleportObjects = HeroManager.Enemies.Select(hero => new TeleportObject(hero)).ToList();

                Obj_AI_Base.OnTeleport += OnObjAiBaseTeleport;

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
            private float _preLastActionTime;
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
                    _preLastActionTime = _lastActionTime;
                    _lastActionTime = Game.Time;
                }
            }

            // ReSharper disable once MemberCanBePrivate.Local
            public Packet.S2C.Teleport.Type LastType { get; set; }

            public float Countdown
            {
                get
                {
                    switch (LastStatus)
                    {
                        case Packet.S2C.Teleport.Status.Start:
                            return Game.Time - _teleportStart;
                        case Packet.S2C.Teleport.Status.Finish:
                            return Game.Time - _lastActionTime > 5f ? 0 : Game.Time - _preLastActionTime;
                        case Packet.S2C.Teleport.Status.Abort:
                            return Game.Time - _lastActionTime > 5f ? 0 : _lastActionTime - _preLastActionTime;
                    }
                    return 0;
                }
            }

            public override string ToString()
            {
                var time = _teleportStart + Duration - Game.Time;
                if (time <= 0)
                {
                    time = Game.Time - _lastActionTime;
                }
                var hPercent = (int) ((Hero.Health/Hero.MaxHealth)*100);
                switch (LastType)
                {
                    case Packet.S2C.Teleport.Type.Recall:
                        switch (LastStatus)
                        {
                            case Packet.S2C.Teleport.Status.Start:
                                return string.Format("{1}({2}%) {0} ({3:0.00})", Language.Get("Teleport_Recalling"), Hero.ChampionName, hPercent, time);

                            case Packet.S2C.Teleport.Status.Finish:
                                return string.Format("{1}({2}%) {0} ({3:0.00})", Language.Get("Teleport_Recalled"), Hero.ChampionName, hPercent, time);

                            case Packet.S2C.Teleport.Status.Abort:
                                return string.Format("{1}({2}%) {0} ({3:0.00})", Language.Get("Teleport_Aborted"), Hero.ChampionName, hPercent, time);
                        }
                        break;

                    case Packet.S2C.Teleport.Type.Teleport:
                        switch (LastStatus)
                        {
                            case Packet.S2C.Teleport.Status.Start:
                                return string.Format("{1}({2}%) {0} ({3:0.00})", Language.Get("Teleport_Teleporting"), Hero.ChampionName, hPercent,
                                    time);

                            case Packet.S2C.Teleport.Status.Finish:
                                return string.Format("{1}({2}%) {0} ({3:0.00})", Language.Get("Teleport_Teleported"), Hero.ChampionName, hPercent,
                                    time);

                            case Packet.S2C.Teleport.Status.Abort:
                                return string.Format("{1}({2}%) {0} ({3:0.00})", Language.Get("Teleport_Aborted"), Hero.ChampionName, hPercent, time);
                        }
                        break;

                    case Packet.S2C.Teleport.Type.Shen:
                    case Packet.S2C.Teleport.Type.TwistedFate:
                        switch (LastStatus)
                        {
                            case Packet.S2C.Teleport.Status.Start:
                                return string.Format("{1}({2}%) {0} ({3:0.00})", Language.Get("Teleport_Transporting"), Hero.ChampionName, hPercent,
                                    time);

                            case Packet.S2C.Teleport.Status.Finish:
                                return string.Format("{1}({2}%) {0} ({3:0.00})", Language.Get("Teleport_Transported"), Hero.ChampionName, hPercent,
                                    time);

                            case Packet.S2C.Teleport.Status.Abort:
                                return string.Format("{1}({2}%) {0} ({3:0.00})", Language.Get("Teleport_Aborted"), Hero.ChampionName, hPercent, time);
                        }
                        break;
                }
                return string.Empty;
            }

            public Color ToColor(bool text = false)
            {
                switch (LastStatus)
                {
                    case Packet.S2C.Teleport.Status.Start:
                        return text ? Color.Beige : Color.White;

                    case Packet.S2C.Teleport.Status.Finish:
                        return text ? Color.GreenYellow : Color.White;

                    case Packet.S2C.Teleport.Status.Abort:
                        return text ? Color.Red : Color.Yellow;

                    default:
                        return text ? Color.Black : Color.White;
                }
            }

            public bool Update()
            {
                var additional = LastStatus == Packet.S2C.Teleport.Status.Start ? Duration + 10f : 10f;
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