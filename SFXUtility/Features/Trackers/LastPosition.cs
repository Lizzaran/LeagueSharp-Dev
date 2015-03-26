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
    using LeagueSharp.CommonEx.Core.Events;
    using Properties;
    using SFXLibrary.Extensions.NET;
    using SFXLibrary.IoCContainer;
    using SFXLibrary.Logger;
    using SharpDX;
    using Color = SharpDX.Color;
    using ObjectHandler = LeagueSharp.CommonEx.Core.ObjectHandler;

    #endregion

    internal class LastPosition : Base
    {
        private readonly List<LastPositionObject> _lastPositionObjects = new List<LastPositionObject>();
        private Trackers _parent;

        public LastPosition(IContainer container)
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
            get { return "Last Position"; }
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

                    if (IoC.IsRegistered<Recall>())
                    {
                        var recall = IoC.Resolve<Recall>();
                        recall.OnEnabled += RecallEnabled;
                        recall.OnDisabled += RecallDisabled;
                        recall.OnFinish += RecallFinish;
                        recall.OnStart += RecallStart;
                        recall.OnAbort += RecallAbort;
                        recall.OnUnknown += RecallAbort;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
            }
        }

        private void RecallAbort(object sender, RecallEventArgs recallEventArgs)
        {
            var lastPosition =
                _lastPositionObjects.FirstOrDefault(e => e.Hero.NetworkId == recallEventArgs.UnitNetworkId);
            if (!Equals(lastPosition, default(LastPositionObject)))
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
            var lastPosition =
                _lastPositionObjects.FirstOrDefault(e => e.Hero.NetworkId == recallEventArgs.UnitNetworkId);
            if (!Equals(lastPosition, default(LastPositionObject)))
            {
                lastPosition.Recalled = true;
                lastPosition.IsRecalling = false;
            }
        }

        private void RecallStart(object sender, RecallEventArgs recallEventArgs)
        {
            var lastPosition =
                _lastPositionObjects.FirstOrDefault(e => e.Hero.NetworkId == recallEventArgs.UnitNetworkId);
            if (!Equals(lastPosition, default(LastPositionObject)))
            {
                lastPosition.IsRecalling = true;
            }
        }

        private void OnParentInitialized(object sender, EventArgs eventArgs)
        {
            try
            {
                if (_parent.Menu == null)
                    return;

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
                Menu.AddItem(new MenuItem(Name + "Enabled", "Enabled").SetValue(false));

                Menu.Item(Name + "DrawingTimeFormat").ValueChanged +=
                    (o, args) =>
                        _lastPositionObjects.ForEach(
                            enemy => enemy.TextTotalSeconds = args.GetNewValue<StringList>().SelectedIndex == 1);
                Menu.Item(Name + "DrawingFontSize").ValueChanged +=
                    (o, args) =>
                        _lastPositionObjects.ForEach(enemy => enemy.TextSize = args.GetNewValue<Slider>().Value);
                Menu.Item(Name + "DrawingSSTimerOffset").ValueChanged +=
                    (o, args) =>
                        _lastPositionObjects.ForEach(enemy => enemy.TextOffset = args.GetNewValue<Slider>().Value);
                Menu.Item(Name + "SSTimer").ValueChanged +=
                    (o, args) =>
                        _lastPositionObjects.ForEach(enemy => enemy.SSTimer = args.GetNewValue<bool>());
                Menu.Item(Name + "Enabled").ValueChanged +=
                    (o, args) => _lastPositionObjects.ForEach(enemy => enemy.Active = args.GetNewValue<bool>());

                _parent.Menu.AddSubMenu(Menu);

                var recall = false;

                if (IoC.IsRegistered<Recall>())
                {
                    var rt = IoC.Resolve<Recall>();
                    if (rt.Initialized)
                    {
                        recall = rt.Menu.Item(rt.Name + "Enabled").GetValue<bool>();
                    }
                }

                foreach (var enemy in ObjectHandler.EnemyHeroes)
                {
                    try
                    {
                        _lastPositionObjects.Add(new LastPositionObject(enemy, Logger)
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

                RaiseOnInitialized();
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
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
                        ObjectHandler.GetFast<GameObject>().FirstOrDefault(s => s is Obj_SpawnPoint && s.IsEnemy);

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