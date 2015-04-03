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
    using SFXLibrary.Extensions.NET;
    using SFXLibrary.Logger;
    using SharpDX;
    using Color = SharpDX.Color;

    #endregion

    internal class LastPosition : Base
    {
        private readonly List<LastPositionObject> _lastPositionObjects = new List<LastPositionObject>();
        private Trackers _parent;

        public override bool Enabled
        {
            get { return _parent != null && _parent.Enabled && Menu != null && Menu.Item(Name + "Enabled").GetValue<bool>(); }
        }

        public override string Name
        {
            get { return "Last Position"; }
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

        private void RecallAbort(object sender, RecallEventArgs recallEventArgs)
        {
            var lastPosition = _lastPositionObjects.FirstOrDefault(e => e.Hero.NetworkId == recallEventArgs.UnitNetworkId);
            if (lastPosition != null)
            {
                lastPosition.IsRecalling = false;
            }
        }

        private void RecallEnabled(object sender, EventArgs eventArgs)
        {
            _lastPositionObjects.ForEach(e => e.Recall = true);
        }

        private void RecallDisabled(object sender, EventArgs eventArgs)
        {
            _lastPositionObjects.ForEach(e => e.Recall = false);
        }

        private void RecallFinish(object sender, RecallEventArgs recallEventArgs)
        {
            var lastPosition = _lastPositionObjects.FirstOrDefault(e => e.Hero.NetworkId == recallEventArgs.UnitNetworkId);
            if (lastPosition != null)
            {
                lastPosition.Recalled = true;
                lastPosition.IsRecalling = false;
            }
        }

        private void RecallStart(object sender, RecallEventArgs recallEventArgs)
        {
            var lastPosition = _lastPositionObjects.FirstOrDefault(e => e.Hero.NetworkId == recallEventArgs.UnitNetworkId);
            if (lastPosition != null)
            {
                lastPosition.IsRecalling = true;
            }
        }

        protected override void OnEnable()
        {
            _lastPositionObjects.ForEach(enemy => enemy.Active = true);
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            _lastPositionObjects.ForEach(enemy => enemy.Active = false);
            base.OnDisable();
        }

        private void OnParentInitialized(object sender, EventArgs eventArgs)
        {
            try
            {
                if (_parent.Menu == null)
                    return;

                Menu = new Menu(Name, Name);

                var drawingMenu = new Menu("Drawing", Name + "Drawing");
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + "TimeFormat", "Time Format").SetValue(new StringList(new[] {"mm:ss", "ss"})));
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + "FontSize", "Font Size").SetValue(new Slider(13, 3, 30)));
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + "SSTimerOffset", "SS Timer Offset").SetValue(new Slider(5, 0, 50)));

                Menu.AddSubMenu(drawingMenu);

                Menu.AddItem(new MenuItem(Name + "SSTimer", "SS Timer").SetValue(false));
                Menu.AddItem(new MenuItem(Name + "Enabled", "Enabled").SetValue(false));

                Menu.Item(Name + "DrawingTimeFormat").ValueChanged +=
                    (o, args) => _lastPositionObjects.ForEach(enemy => enemy.TextTotalSeconds = args.GetNewValue<StringList>().SelectedIndex == 1);
                Menu.Item(Name + "DrawingSSTimerOffset").ValueChanged +=
                    (o, args) => _lastPositionObjects.ForEach(enemy => enemy.FontOffset = args.GetNewValue<Slider>().Value);
                Menu.Item(Name + "SSTimer").ValueChanged +=
                    (o, args) => _lastPositionObjects.ForEach(enemy => enemy.SSTimer = args.GetNewValue<bool>());

                _parent.Menu.AddSubMenu(Menu);

                var recall = false;

                if (Global.IoC.IsRegistered<Recall>())
                {
                    var rt = Global.IoC.Resolve<Recall>();

                    recall = rt.Initialized && rt.Enabled;

                    rt.OnEnabled += RecallEnabled;
                    rt.OnDisabled += RecallDisabled;
                    rt.OnFinish += RecallFinish;
                    rt.OnStart += RecallStart;
                    rt.OnAbort += RecallAbort;
                    rt.OnUnknown += RecallAbort;
                }

                foreach (var enemy in HeroManager.Enemies)
                {
                    try
                    {
                        _lastPositionObjects.Add(new LastPositionObject(enemy, Menu.Item(Name + "DrawingFontSize").GetValue<Slider>().Value)
                        {
                            Active = Enabled,
                            TextTotalSeconds = Menu.Item(Name + "DrawingTimeFormat").GetValue<StringList>().SelectedIndex == 1,
                            FontOffset = Menu.Item(Name + "DrawingSSTimerOffset").GetValue<Slider>().Value,
                            SSTimer = Menu.Item(Name + "SSTimer").GetValue<bool>(),
                            Recall = recall
                        });
                    }
                    catch (Exception ex)
                    {
                        Global.Logger.AddItem(new LogItem(ex));
                    }
                }

                HandleEvents(_parent);
                RaiseOnInitialized();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        internal class LastPositionObject
        {
            private readonly Render.Sprite _championSprite;
            private readonly Render.Sprite _recallSprite;
            private readonly Render.Text _text;
            public readonly Obj_AI_Hero Hero;
            private bool _active;
            private bool _added;
            public bool IsRecalling;
            // ReSharper disable once InconsistentNaming
            public bool SSTimer;
            public int FontOffset = 5;
            public bool TextTotalSeconds;
            public bool Recall;
            public bool Recalled;
            private float _lastSeen;

            public LastPositionObject(Obj_AI_Hero hero, int fontSize)
            {
                try
                {
                    Hero = hero;
                    var mPos = Drawing.WorldToMinimap(hero.Position);
                    var spawnPoint = ObjectManager.Get<GameObject>().FirstOrDefault(s => s is Obj_SpawnPoint && s.IsEnemy);

                    _championSprite =
                        new Render.Sprite(
                            (Bitmap) Resources.ResourceManager.GetObject(string.Format("LP_{0}", hero.ChampionName)) ?? Resources.LP_Aatrox,
                            new Vector2(mPos.X, mPos.Y))
                        {
                            VisibleCondition = delegate
                            {
                                try
                                {
                                    if (hero.IsVisible)
                                    {
                                        Recalled = false;
                                    }
                                    return Active && !Hero.IsVisible && !Hero.IsDead;
                                }
                                catch (Exception ex)
                                {
                                    Global.Logger.AddItem(new LogItem(ex));
                                    return false;
                                }
                            },
                            PositionUpdate = delegate
                            {
                                try
                                {
                                    if (Recall && Recalled)
                                    {
                                        if (spawnPoint != null)
                                        {
                                            var p = Drawing.WorldToMinimap(spawnPoint.Position);
                                            return new Vector2(p.X - (_championSprite.Size.X/2), p.Y - (_championSprite.Size.Y/2));
                                        }
                                    }
                                    var pos = Drawing.WorldToMinimap(hero.Position);
                                    return new Vector2(pos.X - (_championSprite.Size.X/2), pos.Y - (_championSprite.Size.Y/2));
                                }
                                catch (Exception ex)
                                {
                                    Global.Logger.AddItem(new LogItem(ex));
                                    return default(Vector2);
                                }
                            }
                        };
                    _text = new Render.Text(string.Empty, new Vector2(mPos.X, mPos.Y), fontSize, Color.White)
                    {
                        OutLined = true,
                        Centered = true,
                        PositionUpdate = delegate
                        {
                            try
                            {
                                return new Vector2(_championSprite.Position.X + (_championSprite.Size.X/2),
                                    _championSprite.Position.Y + (_championSprite.Size.Y) + FontOffset);
                            }
                            catch (Exception ex)
                            {
                                Global.Logger.AddItem(new LogItem(ex));
                                return default(Vector2);
                            }
                        },
                        VisibleCondition = delegate
                        {
                            try
                            {
                                if (Hero.IsVisible && !Hero.IsDead)
                                    _lastSeen = Game.Time;
                                return SSTimer && _championSprite.Visible && _lastSeen != 0f && (Game.Time - _lastSeen) > 3f;
                            }
                            catch (Exception ex)
                            {
                                Global.Logger.AddItem(new LogItem(ex));
                                return false;
                            }
                        },
                        TextUpdate = () => _text.Visible ? (Game.Time - _lastSeen).FormatTime(TextTotalSeconds) : string.Empty
                    };
                    _recallSprite = new Render.Sprite(Resources.LP_Recall, new Vector2(mPos.X, mPos.Y))
                    {
                        VisibleCondition = delegate
                        {
                            try
                            {
                                return _championSprite.Visible && Recall && IsRecalling;
                            }
                            catch (Exception ex)
                            {
                                Global.Logger.AddItem(new LogItem(ex));
                                return false;
                            }
                        },
                        PositionUpdate = delegate
                        {
                            try
                            {
                                var pos = Drawing.WorldToMinimap(hero.Position);
                                return new Vector2(pos.X - (_recallSprite.Size.X/2), pos.Y - (_recallSprite.Size.Y/2));
                            }
                            catch (Exception ex)
                            {
                                Global.Logger.AddItem(new LogItem(ex));
                                return default(Vector2);
                            }
                        }
                    };
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
                    Toggle();
                }
            }

            private void Toggle()
            {
                if (_championSprite == null)
                    return;

                if (_active && !_added)
                {
                    _recallSprite.Add(0);
                    _championSprite.Add(1);
                    _text.Add(2);
                    _added = true;
                }
                else if (!_active && _added)
                {
                    _recallSprite.Remove();
                    _championSprite.Remove();
                    _text.Remove();
                    _added = false;
                }
            }
        }
    }
}