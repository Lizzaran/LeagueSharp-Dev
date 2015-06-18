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
using SharpDX;
using SharpDX.Direct3D9;
using Color = System.Drawing.Color;
using Font = SharpDX.Direct3D9.Font;

#endregion

#pragma warning disable 618

namespace SFXUtility.Features.Detectors
{
    internal class Teleport : Child<Detectors>
    {
        private Font _barText;
        private Line _line;
        private List<TeleportObject> _teleportObjects;
        private Font _text;
        public Teleport(SFXUtility sfx) : base(sfx) {}

        public override string Name
        {
            get { return Global.Lang.Get("F_Teleport"); }
        }

        public event EventHandler<TeleportEventArgs> OnStart;
        public event EventHandler<TeleportEventArgs> OnFinish;
        public event EventHandler<TeleportEventArgs> OnAbort;
        public event EventHandler<TeleportEventArgs> OnUnknown;

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

                try
                {
                    if (Menu.Item(Name + "DrawingTextEnabled").GetValue<bool>())
                    {
                        var posX = Menu.Item(Name + "DrawingTextOffsetLeft").GetValue<Slider>().Value;
                        var posY = Menu.Item(Name + "DrawingTextOffsetTop").GetValue<Slider>().Value;
                        var count = 0;
                        foreach (var teleport in
                            _teleportObjects.Where(
                                t => t.Hero.IsEnemy && t.LastStatus != Packet.S2C.Teleport.Status.Unknown && t.Update())
                            )
                        {
                            var text = teleport.ToString();
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                var color = teleport.ToColor(true);
                                _text.DrawTextCentered(
                                    text, posX, posY + (_text.Description.Height + 1) * count++,
                                    new SharpDX.Color(color.R, color.G, color.B));
                            }
                        }
                    }

                    if (Menu.Item(Name + "DrawingBarEnabled").GetValue<bool>())
                    {
                        var dScale = Menu.Item(Name + "DrawingBarScale").GetValue<Slider>().Value / 10d;
                        var barHeight =
                            (int) Math.Ceiling(Menu.Item(Name + "DrawingBarHeight").GetValue<Slider>().Value * dScale);
                        var seperatorHeight = (int) Math.Ceiling(barHeight / 2d);
                        var top = true;
                        var posX = Menu.Item(Name + "DrawingBarOffsetLeft").GetValue<Slider>().Value;
                        var posY = Menu.Item(Name + "DrawingBarOffsetTop").GetValue<Slider>().Value;
                        var barWidth =
                            (int) Math.Ceiling(Menu.Item(Name + "DrawingBarWidth").GetValue<Slider>().Value * dScale);
                        var teleports =
                            _teleportObjects.Where(
                                t =>
                                    t.Hero.IsEnemy && t.LastStatus != Packet.S2C.Teleport.Status.Unknown &&
                                    t.Update(true)).OrderBy(t => t.Countdown);
                        foreach (var teleport in teleports.Where(t => t.Duration > 0 && !t.Hero.IsDead))
                        {
                            var scale = barWidth / teleport.Duration;
                            var hPercent = ((int) ((teleport.Hero.Health / teleport.Hero.MaxHealth) * 100)).ToString();
                            var color = teleport.ToColor();
                            var width = (int) (scale * teleport.Countdown);
                            width = width > barWidth ? barWidth : width;

                            _line.Width = barHeight;
                            _line.Begin();

                            _line.Draw(
                                new[]
                                {
                                    new Vector2(posX, posY + barHeight / 2f),
                                    new Vector2(posX + width, posY + barHeight / 2f)
                                },
                                new SharpDX.Color((int) color.R, color.G, color.B, 100));

                            _line.End();

                            _line.Width = 1;
                            _line.Begin();

                            _line.Draw(
                                new[]
                                {
                                    new Vector2(
                                        posX + width,
                                        (top ? posY - seperatorHeight - barHeight / 2f : posY + barHeight + 2)),
                                    new Vector2(posX + width, (top ? posY : posY + seperatorHeight * 2 + barHeight + 2))
                                }, SharpDX.Color.White);

                            _line.End();

                            _barText.DrawTextCentered(
                                teleport.Hero.ChampionName, posX + width,
                                (top
                                    ? posY - barHeight - seperatorHeight - 2
                                    : posY + barHeight * 2 + seperatorHeight * 2 + 2),
                                new ColorBGRA(color.R, color.G, color.B, color.A));

                            _barText.DrawTextCentered(
                                hPercent, posX + width - 1,
                                (top
                                    ? posY - barHeight - 3 - seperatorHeight - _barText.Description.Height + 3
                                    : posY + barHeight * 2 + 3 + seperatorHeight * 2 + _barText.Description.Height - 1),
                                new ColorBGRA(color.R, color.G, color.B, color.A));
                            top = !top;
                        }
                        if (teleports.Any())
                        {
                            _line.Width = 1;
                            _line.Begin();

                            _line.Draw(
                                new[] { new Vector2(posX, posY), new Vector2(posX + barWidth, posY) },
                                SharpDX.Color.White);
                            _line.Draw(
                                new[]
                                { new Vector2(posX + barWidth, posY), new Vector2(posX + barWidth, posY + barHeight) },
                                SharpDX.Color.White);
                            _line.Draw(
                                new[]
                                { new Vector2(posX, posY + barHeight), new Vector2(posX + barWidth, posY + barHeight) },
                                SharpDX.Color.White);
                            _line.Draw(
                                new[] { new Vector2(posX, posY), new Vector2(posX, posY + barHeight) },
                                SharpDX.Color.White);

                            _line.End();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Global.Logger.AddItem(new LogItem(ex));
                }
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

                var drawingTextMenu = new Menu(Global.Lang.Get("G_Text"), drawingMenu.Name + "Text");
                drawingTextMenu.AddItem(
                    new MenuItem(
                        drawingTextMenu.Name + "OffsetTop", Global.Lang.Get("G_Offset") + " " + Global.Lang.Get("G_Top"))
                        .SetValue(new Slider((int) (Drawing.Height * 0.75d), 0, Drawing.Height)));
                drawingTextMenu.AddItem(
                    new MenuItem(
                        drawingTextMenu.Name + "OffsetLeft",
                        Global.Lang.Get("G_Offset") + " " + Global.Lang.Get("G_Left")).SetValue(
                            new Slider((int) (Drawing.Width * 0.68d), 0, Drawing.Width)));
                drawingTextMenu.AddItem(
                    new MenuItem(drawingTextMenu.Name + "FontSize", Global.Lang.Get("G_FontSize")).SetValue(
                        new Slider(15, 5, 30)));
                drawingTextMenu.AddItem(
                    new MenuItem(drawingTextMenu.Name + "AdditionalTime", Global.Lang.Get("Teleport_AdditionalTime"))
                        .SetValue(new Slider(10, 0, 10))).ValueChanged +=
                    delegate(object o, OnValueChangeEventArgs args)
                    {
                        if (_teleportObjects != null)
                        {
                            _teleportObjects.ForEach(t => t.AdditionalTextTime = args.GetNewValue<Slider>().Value);
                        }
                    };
                drawingTextMenu.AddItem(
                    new MenuItem(drawingTextMenu.Name + "Enabled", Global.Lang.Get("G_Enabled")).SetValue(false));

                var drawingBarMenu = new Menu(Global.Lang.Get("G_Bar"), drawingMenu.Name + "Bar");
                drawingBarMenu.AddItem(
                    new MenuItem(drawingBarMenu.Name + "FontSize", Global.Lang.Get("G_FontSize")).SetValue(
                        new Slider(13, 5, 30)));
                drawingBarMenu.AddItem(
                    new MenuItem(drawingBarMenu.Name + "Scale", Global.Lang.Get("G_Scale")).SetValue(
                        new Slider(10, 1, 20)));
                drawingBarMenu.AddItem(
                    new MenuItem(drawingBarMenu.Name + "Height", Global.Lang.Get("G_Height")).SetValue(
                        new Slider(10, 3, 20)));
                drawingBarMenu.AddItem(
                    new MenuItem(drawingBarMenu.Name + "Width", Global.Lang.Get("G_Width")).SetValue(
                        new Slider(475, 0, (int) (Drawing.Width / 1.5d))));
                drawingBarMenu.AddItem(
                    new MenuItem(
                        drawingBarMenu.Name + "OffsetTop", Global.Lang.Get("G_Offset") + " " + Global.Lang.Get("G_Top"))
                        .SetValue(new Slider((int) (Drawing.Height * 0.75d), 0, Drawing.Height)));
                drawingBarMenu.AddItem(
                    new MenuItem(
                        drawingBarMenu.Name + "OffsetLeft",
                        Global.Lang.Get("G_Offset") + " " + Global.Lang.Get("G_Left")).SetValue(
                            new Slider((int) (Drawing.Width * 0.425d), 0, Drawing.Width)));
                drawingBarMenu.AddItem(
                    new MenuItem(drawingBarMenu.Name + "AdditionalTime", Global.Lang.Get("Teleport_AdditionalTime"))
                        .SetValue(new Slider(5, 0, 10))).ValueChanged += delegate(object o, OnValueChangeEventArgs args)
                        {
                            if (_teleportObjects != null)
                            {
                                _teleportObjects.ForEach(t => t.AdditionalBarTime = args.GetNewValue<Slider>().Value);
                            }
                        };
                drawingBarMenu.AddItem(
                    new MenuItem(drawingBarMenu.Name + "Enabled", Global.Lang.Get("G_Enabled")).SetValue(false));

                drawingMenu.AddSubMenu(drawingTextMenu);
                drawingMenu.AddSubMenu(drawingBarMenu);

                Menu.AddSubMenu(drawingMenu);

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
            _teleportObjects = new List<TeleportObject>();
            _teleportObjects =
                GameObjects.Heroes.Select(
                    hero =>
                        new TeleportObject(hero)
                        {
                            AdditionalTextTime =
                                Menu.Item(Menu.Name + "DrawingTextAdditionalTime").GetValue<Slider>().Value,
                            AdditionalBarTime =
                                Menu.Item(Menu.Name + "DrawingBarAdditionalTime").GetValue<Slider>().Value
                        }).ToList();

            Obj_AI_Base.OnTeleport += OnObjAiBaseTeleport;

            _text = MDrawing.GetFont(Menu.Item(Name + "DrawingTextFontSize").GetValue<Slider>().Value);
            _barText =
                MDrawing.GetFont(
                    (int)
                        (Math.Ceiling(
                            Menu.Item(Name + "DrawingBarFontSize").GetValue<Slider>().Value *
                            (Menu.Item(Menu.Name + "DrawingBarScale").GetValue<Slider>().Value / 10d))));
            _line = MDrawing.GetLine(1);

            base.OnInitialize();
        }

        private void OnObjAiBaseTeleport(GameObject sender, GameObjectTeleportEventArgs args)
        {
            try
            {
                var packet = Packet.S2C.Teleport.Decoded(sender, args);
                var teleport = _teleportObjects.FirstOrDefault(r => r.Hero.NetworkId == packet.UnitNetworkId);
                if (teleport != null)
                {
                    var duration = packet.Duration;
                    if (packet.Type == Packet.S2C.Teleport.Type.Recall)
                    {
                        duration = teleport.Hero.HasBuff("exaltedwithbaronnashor") ? 4000 : 8000;
                        if (Utility.Map.GetMap().Type == Utility.Map.MapType.CrystalScar)
                        {
                            duration = 4500;
                        }
                    }
                    if (packet.Type == Packet.S2C.Teleport.Type.Shen)
                    {
                        duration = 3000;
                    }
                    if (packet.Type == Packet.S2C.Teleport.Type.TwistedFate)
                    {
                        duration = 1500;
                    }
                    if (packet.Type == Packet.S2C.Teleport.Type.Teleport)
                    {
                        duration = 3500;
                    }
                    teleport.Duration = duration;
                    teleport.LastStatus = packet.Status;
                    teleport.LastType = packet.Type;

                    switch (packet.Status)
                    {
                        case Packet.S2C.Teleport.Status.Start:
                            OnStart.RaiseEvent(null, new TeleportEventArgs(packet.UnitNetworkId, packet.Status));
                            break;

                        case Packet.S2C.Teleport.Status.Finish:
                            OnFinish.RaiseEvent(null, new TeleportEventArgs(packet.UnitNetworkId, packet.Status));
                            break;

                        case Packet.S2C.Teleport.Status.Abort:
                            OnAbort.RaiseEvent(null, new TeleportEventArgs(packet.UnitNetworkId, packet.Status));
                            break;

                        case Packet.S2C.Teleport.Status.Unknown:
                            OnUnknown.RaiseEvent(null, new TeleportEventArgs(packet.UnitNetworkId, packet.Status));
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

            public int AdditionalTextTime { private get; set; }
            public int AdditionalBarTime { private get; set; }

            public int Duration
            {
                get { return _duration; }
                set { _duration = value / 1000; }
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
                            return Game.Time - _lastActionTime > AdditionalBarTime ? 0 : Game.Time - _preLastActionTime;
                        case Packet.S2C.Teleport.Status.Abort:
                            return Game.Time - _lastActionTime > AdditionalBarTime
                                ? 0
                                : _lastActionTime - _preLastActionTime;
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
                switch (LastType)
                {
                    case Packet.S2C.Teleport.Type.Recall:
                        switch (LastStatus)
                        {
                            case Packet.S2C.Teleport.Status.Start:
                                return string.Format(
                                    "{1}({2}%) {0} ({3:0.00})", Global.Lang.Get("Teleport_Recalling"), Hero.ChampionName,
                                    (int) Hero.HealthPercent, time);

                            case Packet.S2C.Teleport.Status.Finish:
                                return string.Format(
                                    "{1}({2}%) {0} ({3:0.00})", Global.Lang.Get("Teleport_Recalled"), Hero.ChampionName,
                                    (int) Hero.HealthPercent, time);

                            case Packet.S2C.Teleport.Status.Abort:
                                return string.Format(
                                    "{1}({2}%) {0} ({3:0.00})", Global.Lang.Get("Teleport_Aborted"), Hero.ChampionName,
                                    (int) Hero.HealthPercent, time);
                        }
                        break;

                    case Packet.S2C.Teleport.Type.Teleport:
                        switch (LastStatus)
                        {
                            case Packet.S2C.Teleport.Status.Start:
                                return string.Format(
                                    "{1}({2}%) {0} ({3:0.00})", Global.Lang.Get("Teleport_Teleporting"),
                                    Hero.ChampionName, (int) Hero.HealthPercent, time);

                            case Packet.S2C.Teleport.Status.Finish:
                                return string.Format(
                                    "{1}({2}%) {0} ({3:0.00})", Global.Lang.Get("Teleport_Teleported"),
                                    Hero.ChampionName, (int) Hero.HealthPercent, time);

                            case Packet.S2C.Teleport.Status.Abort:
                                return string.Format(
                                    "{1}({2}%) {0} ({3:0.00})", Global.Lang.Get("Teleport_Aborted"), Hero.ChampionName,
                                    (int) Hero.HealthPercent, time);
                        }
                        break;

                    case Packet.S2C.Teleport.Type.Shen:
                    case Packet.S2C.Teleport.Type.TwistedFate:
                        switch (LastStatus)
                        {
                            case Packet.S2C.Teleport.Status.Start:
                                return string.Format(
                                    "{1}({2}%) {0} ({3:0.00})", Global.Lang.Get("Teleport_Transporting"),
                                    Hero.ChampionName, (int) Hero.HealthPercent, time);

                            case Packet.S2C.Teleport.Status.Finish:
                                return string.Format(
                                    "{1}({2}%) {0} ({3:0.00})", Global.Lang.Get("Teleport_Transported"),
                                    Hero.ChampionName, (int) Hero.HealthPercent, time);

                            case Packet.S2C.Teleport.Status.Abort:
                                return string.Format(
                                    "{1}({2}%) {0} ({3:0.00})", Global.Lang.Get("Teleport_Aborted"), Hero.ChampionName,
                                    (int) Hero.HealthPercent, time);
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

            public bool Update(bool bar = false)
            {
                var additional = LastStatus == Packet.S2C.Teleport.Status.Start
                    ? Duration + (bar ? AdditionalBarTime : AdditionalTextTime)
                    : (bar ? AdditionalBarTime : AdditionalTextTime);
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
        private readonly Packet.S2C.Teleport.Status _status;
        private readonly int _unitNetworkId;

        public TeleportEventArgs(int unitNetworkId, Packet.S2C.Teleport.Status status)
        {
            _unitNetworkId = unitNetworkId;
            _status = status;
        }

        public Packet.S2C.Teleport.Status Status
        {
            get { return _status; }
        }

        public int UnitNetworkId
        {
            get { return _unitNetworkId; }
        }
    }
}