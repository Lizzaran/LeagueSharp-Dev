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
    using LeagueSharp;
    using LeagueSharp.Common;
    using Properties;
    using SFXLibrary;
    using SFXLibrary.Extensions.NET;
    using SFXLibrary.IoCContainer;
    using SFXLibrary.Logger;
    using SharpDX;
    using Color = SharpDX.Color;

    #endregion

    internal class LastPosition : Base
    {
        private readonly List<LastPositionObject> _lastPositionObjects = new List<LastPositionObject>();
        private Trackers _trackers;

        public LastPosition(IContainer container)
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
            get { return "Last Position"; }
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

                if (IoC.IsRegistered<Mediator>())
                {
                    IoC.Resolve<Mediator>().Register("Recall_Finish", RecallFinish);
                    IoC.Resolve<Mediator>().Register("Recall_Start", RecallStart);
                    IoC.Resolve<Mediator>().Register("Recall_Abort", RecallAbort);
                    IoC.Resolve<Mediator>().Register("Recall_Unknown", RecallAbort);
                    IoC.Resolve<Mediator>().Register("Recall_Enabled", RecallEnabled);
                }
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
            }
        }

        private void RecallAbort(object o)
        {
            var unitId = o as int? ?? 0;
            var lastPosition = _lastPositionObjects.FirstOrDefault(e => e.Hero.NetworkId == unitId);
            if (!Equals(lastPosition, default(LastPositionObject)))
            {
                lastPosition.IsRecalling = false;
            }
        }

        private void RecallEnabled(object o)
        {
            _lastPositionObjects.ForEach(e => e.Recall = o is bool && (bool) o);
        }

        private void RecallFinish(object o)
        {
            var unitId = o as int? ?? 0;
            var lastPosition = _lastPositionObjects.FirstOrDefault(e => e.Hero.NetworkId == unitId);
            if (!Equals(lastPosition, default(LastPositionObject)))
            {
                lastPosition.Recalled = true;
                lastPosition.IsRecalling = false;
            }
        }

        private void RecallStart(object o)
        {
            var unitId = o as int? ?? 0;
            var lastPosition = _lastPositionObjects.FirstOrDefault(e => e.Hero.NetworkId == unitId);
            if (!Equals(lastPosition, default(LastPositionObject)))
            {
                lastPosition.IsRecalling = true;
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

                    var drawingMenu = new Menu("Drawing", Name + "Drawing");
                    drawingMenu.AddItem(
                        new MenuItem(Name + "DrawingTimeFormat", "Time Format").SetValue(
                            new StringList(new[] {"mm:ss", "ss"})));
                    drawingMenu.AddItem(
                        new MenuItem(Name + "DrawingFontSize", "Font Size").SetValue(new Slider(13, 3, 30)));
                    drawingMenu.AddItem(
                        new MenuItem(Name + "DrawingSSTimerOffset", "SS Timer Offset").SetValue(new Slider(5, 0, 50)));

                    Menu.AddItem(new MenuItem(Name + "SSTimer", "SS Timer").SetValue(true));
                    Menu.AddItem(new MenuItem(Name + "Enabled", "Enabled").SetValue(true));

                    Menu.Item(Name + "DrawingTimeFormat").ValueChanged +=
                        (sender, args) =>
                            _lastPositionObjects.ForEach(
                                enemy => enemy.TextTotalSeconds = args.GetNewValue<StringList>().SelectedIndex == 1);
                    Menu.Item(Name + "DrawingFontSize").ValueChanged +=
                        (sender, args) =>
                            _lastPositionObjects.ForEach(enemy => enemy.TextSize = args.GetNewValue<Slider>().Value);
                    Menu.Item(Name + "DrawingSSTimerOffset").ValueChanged +=
                        (sender, args) =>
                            _lastPositionObjects.ForEach(enemy => enemy.TextOffset = args.GetNewValue<Slider>().Value);
                    Menu.Item(Name + "SSTimer").ValueChanged +=
                        (sender, args) =>
                            _lastPositionObjects.ForEach(enemy => enemy.SSTimer = args.GetNewValue<bool>());
                    Menu.Item(Name + "Enabled").ValueChanged +=
                        (sender, args) => _lastPositionObjects.ForEach(enemy => enemy.Active = args.GetNewValue<bool>());

                    _trackers.Menu.AddSubMenu(Menu);

                    var recall = false;

                    if (IoC.IsRegistered<Recall>())
                    {
                        var rt = IoC.Resolve<Recall>();
                        if (rt.Initialized)
                        {
                            recall = rt.Menu.Item(rt.Name + "Enabled").GetValue<bool>();
                        }
                    }

                    foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValid && hero.IsEnemy))
                    {
                        try
                        {
                            _lastPositionObjects.Add(new LastPositionObject(hero, Logger)
                            {
                                Active = Enabled,
                                TextTotalSeconds =
                                    Menu.Item(Name + "DrawingTimeFormat").GetValue<StringList>().SelectedIndex == 1,
                                TextSize = Menu.Item(Name + "DrawingFontSize").GetValue<Slider>().Value,
                                TextOffset = Menu.Item(Name + "DrawingSSTimerOffset").GetValue<Slider>().Value,
                                SSTimer = Menu.Item(Name + "SSTimer").GetValue<bool>(),
                                Recall = recall
                            });
                        }
                        catch (Exception ex)
                        {
                            Logger.AddItem(new LogItem(ex) {Object = this});
                        }
                    }

                    Initialized = true;
                }
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
            }
        }

        private class LastPositionObject
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

            public int TextSize = 13;
            public int TextOffset = 5;
            public bool TextTotalSeconds;
            public bool Recall;
            public bool Recalled;
            private float _lastSeen;

            public LastPositionObject(Obj_AI_Hero hero, ILogger logger)
            {
                try
                {
                    Hero = hero;
                    var mPos = Drawing.WorldToMinimap(hero.Position);
                    var spawnPoint =
                        ObjectManager.Get<GameObject>().FirstOrDefault(s => s is Obj_SpawnPoint && s.IsEnemy);

                    _championSprite =
                        new Render.Sprite(
                            (Bitmap) Resources.ResourceManager.GetObject(string.Format("LP_{0}", hero.ChampionName)) ??
                            Resources.LP_Aatrox, new Vector2(mPos.X, mPos.Y))
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
                                    logger.AddItem(new LogItem(ex) {Object = this});
                                    return false;
                                }
                            },
                            PositionUpdate = delegate
                            {
                                try
                                {
                                    if (Recall && Recalled)
                                    {
                                        if (!Equals(spawnPoint, default(Obj_SpawnPoint)))
                                        {
                                            var p = Drawing.WorldToMinimap(spawnPoint.Position);
                                            return new Vector2(p.X - (_championSprite.Size.X/2),
                                                p.Y - (_championSprite.Size.Y/2));
                                        }
                                    }
                                    var pos = Drawing.WorldToMinimap(hero.Position);
                                    return new Vector2(pos.X - (_championSprite.Size.X/2),
                                        pos.Y - (_championSprite.Size.Y/2));
                                }
                                catch (Exception ex)
                                {
                                    logger.AddItem(new LogItem(ex) {Object = this});
                                    return default(Vector2);
                                }
                            }
                        };
                    _text = new Render.Text(string.Empty, new Vector2(mPos.X, mPos.Y), TextSize, Color.White)
                    {
                        Centered = true,
                        PositionUpdate = delegate
                        {
                            try
                            {
                                return new Vector2(_championSprite.Position.X - (_championSprite.Size.X/2),
                                    _championSprite.Position.Y - (_championSprite.Size.Y/2) + TextOffset);
                            }
                            catch (Exception ex)
                            {
                                logger.AddItem(new LogItem(ex) {Object = this});
                                return default(Vector2);
                            }
                        },
                        VisibleCondition = delegate
                        {
                            try
                            {
                                _lastSeen = !_championSprite.Visible ? Game.ClockTime : 0f;
                                return SSTimer && _championSprite.Visible && Game.ClockTime - _lastSeen > 10f;
                            }
                            catch (Exception ex)
                            {
                                logger.AddItem(new LogItem(ex) {Object = this});
                                return false;
                            }
                        },
                        TextUpdate =
                            () =>
                                _text.Visible ? (Game.ClockTime - _lastSeen).FormatTime(TextTotalSeconds) : string.Empty
                    };
                    _recallSprite =
                        new Render.Sprite(Resources.LP_Recall, new Vector2(mPos.X, mPos.Y))
                        {
                            VisibleCondition = delegate
                            {
                                try
                                {
                                    return _championSprite.Visible && Recall && IsRecalling;
                                }
                                catch (Exception ex)
                                {
                                    logger.AddItem(new LogItem(ex) {Object = this});
                                    return false;
                                }
                            },
                            PositionUpdate = delegate
                            {
                                try
                                {
                                    return new Vector2(_championSprite.Position.X - (_recallSprite.Size.X/2),
                                        _championSprite.Position.Y - (_recallSprite.Size.Y/2));
                                }
                                catch (Exception ex)
                                {
                                    logger.AddItem(new LogItem(ex) {Object = this});
                                    return default(Vector2);
                                }
                            }
                        };
                }
                catch (Exception ex)
                {
                    logger.AddItem(new LogItem(ex) {Object = this});
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