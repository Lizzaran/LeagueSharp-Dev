#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Ward.cs is part of SFXUtility.

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

// Credits: TC-Crew

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
    using Properties;
    using SFXLibrary;
    using SFXLibrary.Extensions.NET;
    using SFXLibrary.Logger;
    using SharpDX;
    using Color = System.Drawing.Color;

    #endregion

    internal class Ward : Base
    {
        private const float CheckInterval = 300f;
        private readonly List<WardObject> _wardObjects = new List<WardObject>();

        private readonly List<WardStruct> _wardStructs = new List<WardStruct>
        {
            new WardStruct(60, "YellowTrinket", "TrinketTotemLvl1", WardType.Green),
            new WardStruct(60*3, "YellowTrinketUpgrade", "TrinketTotemLvl2", WardType.Green),
            new WardStruct(60*3, "SightWard", "TrinketTotemLvl3", WardType.Green),
            new WardStruct(60*3, "SightWard", "SightWard", WardType.Green),
            new WardStruct(60*3, "SightWard", "ItemGhostWard", WardType.Green),
            new WardStruct(60*3, "SightWard", "wrigglelantern", WardType.Green),
            new WardStruct(60*3, "SightWard", "ItemFeralFlare", WardType.Green),
            new WardStruct(int.MaxValue, "VisionWard", "TrinketTotemLvl3B", WardType.Pink),
            new WardStruct(int.MaxValue, "VisionWard", "VisionWard", WardType.Pink),
            new WardStruct(60*4, "CaitlynTrap", "CaitlynYordleTrap", WardType.Trap),
            new WardStruct(60*10, "TeemoMushroom", "BantamTrap", WardType.Trap),
            new WardStruct(60*1, "ShacoBox", "JackInTheBox", WardType.Trap),
            new WardStruct(60*2, "Nidalee_Spear", "Bushwhack", WardType.Trap),
            new WardStruct(60*10, "Noxious_Trap", "BantamTrap", WardType.Trap)
        };

        private float _lastCheck = Environment.TickCount;
        private Trackers _parent;

        public override bool Enabled
        {
            get { return _parent != null && _parent.Enabled && Menu != null && Menu.Item(Name + "Enabled").GetValue<bool>(); }
        }

        public override string Name
        {
            get { return Language.Get("F_Ward"); }
        }

        protected override void OnEnable()
        {
            Game.OnUpdate += OnGameUpdate;
            Obj_AI_Base.OnProcessSpellCast += OnObjAiBaseProcessSpellCast;
            GameObject.OnCreate += OnGameObjectCreate;

            foreach (var obj in ObjectManager.Get<GameObject>().Where(o => o is Obj_AI_Base))
            {
                OnGameObjectCreate(obj, null);
            }

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            Game.OnUpdate -= OnGameUpdate;
            Obj_AI_Base.OnProcessSpellCast -= OnObjAiBaseProcessSpellCast;
            GameObject.OnCreate -= OnGameObjectCreate;
            base.OnDisable();
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
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + "CircleRadius", Language.Get("G_CircleRadius")).SetValue(new Slider(150, 25, 300)));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "CircleThickness", Language.Get("G_CircleThickness")).SetValue(new Slider(2, 1, 10)));

                Menu.AddSubMenu(drawingMenu);

                Menu.AddItem(new MenuItem(Name + "Enabled", Language.Get("G_Enabled")).SetValue(false));

                Menu.Item(Name + "DrawingTimeFormat").ValueChanged +=
                    (o, args) => _wardObjects.ForEach(enemy => enemy.TextTotalSeconds = args.GetNewValue<StringList>().SelectedIndex == 1);
                Menu.Item(Name + "DrawingCircleRadius").ValueChanged +=
                    (o, args) => _wardObjects.ForEach(enemy => enemy.Radius = args.GetNewValue<Slider>().Value);
                Menu.Item(Name + "DrawingCircleThickness").ValueChanged +=
                    (o, args) => _wardObjects.ForEach(enemy => enemy.Thickness = args.GetNewValue<Slider>().Value);
                Menu.Item(Name + "Enabled").ValueChanged +=
                    (o, args) => _wardObjects.ForEach(enemy => enemy.Active = args.GetNewValue<bool>() && _parent != null && _parent.Enabled);

                _parent.Menu.AddSubMenu(Menu);

                HandleEvents(_parent);
                RaiseOnInitialized();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnGameObjectCreate(GameObject sender, EventArgs args)
        {
            var spellMissile = sender as Obj_SpellMissile;
            if (spellMissile != null)
            {
                var missile = spellMissile;
                if (missile.SpellCaster.IsEnemy)
                {
                    if (missile.SData.Name.Equals("itemplacementmissile", StringComparison.OrdinalIgnoreCase) && !missile.SpellCaster.IsVisible)
                    {
                        var sPos = missile.StartPosition;
                        var ePos = missile.EndPosition;
                        Utility.DelayAction.Add(1000, delegate
                        {
                            if (
                                !_wardObjects.Any(
                                    w => w.Position.To2D().Distance(sPos.To2D(), ePos.To2D(), false) < 300 && Math.Abs(w.StartT - Game.Time) < 2000))
                            {
                                _wardObjects.Add(new WardObject(_wardStructs[3],
                                    new Vector3(ePos.X, ePos.Y, NavMesh.GetHeightForPosition(ePos.X, ePos.Y)), (int) Game.Time,
                                    Menu.Item(Name + "DrawingFontSize").GetValue<Slider>().Value, null, true)
                                {
                                    StartPosition = new Vector3(sPos.X, sPos.Y, NavMesh.GetHeightForPosition(sPos.X, sPos.Y)),
                                    TextTotalSeconds = Menu.Item(Name + "DrawingTimeFormat").GetValue<StringList>().SelectedIndex == 1,
                                    Radius = Menu.Item(Name + "DrawingCircleRadius").GetValue<Slider>().Value,
                                    Thickness = Menu.Item(Name + "DrawingCircleThickness").GetValue<Slider>().Value,
                                    Active = Enabled
                                });
                            }
                        });
                    }
                }
            }
            else
            {
                var o = sender as Obj_AI_Base;
                if (o != null)
                {
                    var wardObject = o;
                    if (wardObject.IsEnemy)
                    {
                        foreach (var ward in _wardStructs)
                        {
                            if (wardObject.BaseSkinName.Equals(ward.ObjectBaseSkinName, StringComparison.InvariantCultureIgnoreCase))
                            {
                                var startT = Game.Time - (int) ((wardObject.MaxMana - wardObject.Mana));
                                _wardObjects.RemoveAll(
                                    w =>
                                        w.Position.Distance(wardObject.Position) < 200 &&
                                        (Math.Abs(w.StartT - startT) < 1000 || ward.Type != WardType.Green) && w.Remove());
                                _wardObjects.Add(new WardObject(ward, wardObject.Position, (int) startT,
                                    Menu.Item(Name + "DrawingFontSize").GetValue<Slider>().Value, wardObject)
                                {
                                    TextTotalSeconds = Menu.Item(Name + "DrawingTimeFormat").GetValue<StringList>().SelectedIndex == 1,
                                    Radius = Menu.Item(Name + "DrawingCircleRadius").GetValue<Slider>().Value,
                                    Thickness = Menu.Item(Name + "DrawingCircleThickness").GetValue<Slider>().Value,
                                    Active = Enabled
                                });
                            }
                        }
                    }
                }
            }
        }

        private void OnObjAiBaseProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsEnemy)
                return;

            foreach (var ward in _wardStructs)
            {
                if (args.SData.Name.Equals(ward.SpellName, StringComparison.OrdinalIgnoreCase))
                {
                    var endPosition = ObjectManager.Player.GetPath(args.End).ToList().Last();
                    _wardObjects.Add(new WardObject(ward, endPosition, (int) Game.Time, Menu.Item(Name + "DrawingFontSize").GetValue<Slider>().Value)
                    {
                        TextTotalSeconds = Menu.Item(Name + "DrawingTimeFormat").GetValue<StringList>().SelectedIndex == 1,
                        Radius = Menu.Item(Name + "DrawingCircleRadius").GetValue<Slider>().Value,
                        Thickness = Menu.Item(Name + "DrawingCircleThickness").GetValue<Slider>().Value,
                        Active = Enabled
                    });
                }
            }
        }

        private void OnGameUpdate(EventArgs args)
        {
            if (_lastCheck + CheckInterval > Environment.TickCount)
                return;
            _lastCheck = Environment.TickCount;

            _wardObjects.RemoveAll(w => w.EndT <= Game.Time && w.Duration != int.MaxValue && w.Remove());
            _wardObjects.RemoveAll(w => w.Object != null && !w.Object.IsValid && w.Remove());
        }

        private class WardObject
        {
            private readonly Render.Circle _circle;
            private readonly Render.Sprite _minimapSprite;
            private readonly Render.Line _missileLine;
            private readonly Render.Text _timerText;
            public readonly Obj_AI_Base Object;
            public readonly int StartT;
            private bool _active;
            private bool _added;
            private WardStruct _wardData;
            public Vector3 Position;
            public int Radius;
            public Vector3 StartPosition;
            public bool TextTotalSeconds;
            public int Thickness;

            public WardObject(WardStruct data, Vector3 position, int startT, int fontSize, Obj_AI_Base wardObject = null, bool isFromMissile = false)
            {
                _wardData = data;
                Position = position;
                StartT = startT;
                Object = wardObject;

                try
                {
                    _circle = new Render.Circle(Position, Radius, data.Color, Thickness)
                    {
                        VisibleCondition = sender => _active && Position.IsOnScreen()
                    };

                    if (data.Type != WardType.Trap)
                    {
                        var minimapPos = Drawing.WorldToMinimap(Position);
                        _minimapSprite = new Render.Sprite(_wardData.Bitmap,
                            new Vector2(minimapPos.X - _wardData.Bitmap.Width/2f, minimapPos.Y - _wardData.Bitmap.Height/2f))
                        {
                            VisibleCondition = sender => Active
                        };
                    }

                    if (isFromMissile)
                    {
                        _missileLine = new Render.Line(Drawing.WorldToScreen(Position), Drawing.WorldToScreen(StartPosition), 2, SharpDX.Color.White)
                        {
                            EndPositionUpdate = () => Drawing.WorldToScreen(Position),
                            StartPositionUpdate = () => Drawing.WorldToScreen(StartPosition),
                            VisibleCondition = sender => Active
                        };
                    }

                    if (Duration != int.MaxValue)
                    {
                        _timerText = new Render.Text(string.Empty, Drawing.WorldToScreen(Position), fontSize, SharpDX.Color.White)
                        {
                            OutLined = true,
                            PositionUpdate = () => Drawing.WorldToScreen(Position),
                            Centered = true,
                            VisibleCondition = sender => Active && Position.IsOnScreen(),
                            TextUpdate = () => _timerText.Visible ? (EndT - Game.Time).FormatTime(TextTotalSeconds) : string.Empty
                        };
                    }
                }
                catch (Exception ex)
                {
                    Global.Logger.AddItem(new LogItem(ex));
                }
            }

            public bool Active
            {
                private get { return _active; }
                set
                {
                    _active = value;
                    Update();
                }
            }

            public int Duration
            {
                get { return _wardData.Duration; }
            }

            public int EndT
            {
                get { return StartT + Duration; }
            }

            private void Update()
            {
                if (_active && !_added)
                {
                    if (_circle != null)
                        _circle.Add(0);
                    if (_missileLine != null)
                        _missileLine.Add(1);
                    if (_timerText != null)
                        _timerText.Add(2);
                    if (_minimapSprite != null)
                        _minimapSprite.Add(1);
                    _added = true;
                }
                else if (!_active && _added)
                {
                    if (_circle != null)
                        _circle.Remove();
                    if (_missileLine != null)
                        _missileLine.Remove();
                    if (_timerText != null)
                        _timerText.Remove();
                    if (_minimapSprite != null)
                        _minimapSprite.Remove();
                    _added = false;
                }
            }

            public bool Remove()
            {
                Active = false;
                return true;
            }
        }

        private enum WardType
        {
            Green,
            Pink,
            Trap
        }

        private struct WardStruct
        {
            public readonly int Duration;
            public readonly string ObjectBaseSkinName;
            public readonly string SpellName;
            public readonly WardType Type;

            public WardStruct(int duration, string objectBaseSkinName, string spellName, WardType type)
            {
                Duration = duration;
                ObjectBaseSkinName = objectBaseSkinName;
                SpellName = spellName;
                Type = type;
            }

            public Bitmap Bitmap
            {
                get
                {
                    switch (Type)
                    {
                        case WardType.Green:
                            return Resources.WT_Green;
                        case WardType.Pink:
                            return Resources.WT_Pink;
                        default:
                            return Resources.WT_Green;
                    }
                }
            }

            public Color Color
            {
                get
                {
                    switch (Type)
                    {
                        case WardType.Green:
                            return Color.Lime;
                        case WardType.Pink:
                            return Color.Magenta;
                        default:
                            return Color.Red;
                    }
                }
            }
        }
    }
}