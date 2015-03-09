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
    using SFXLibrary.IoCContainer;
    using SFXLibrary.Logger;
    using SharpDX;

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

        // TODO: Add SS Timer

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

                    var eMenuItem = new MenuItem(Name + "Enabled", "Enabled").SetValue(true);
                    eMenuItem.ValueChanged +=
                        (sender, args) => _lastPositionObjects.ForEach(enemy => enemy.Active = args.GetNewValue<bool>());

                    Menu.AddItem(eMenuItem);

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
            private readonly Render.Sprite _recallSprite;
            private readonly Render.Sprite _sprite;
            public readonly Obj_AI_Hero Hero;
            private bool _active;
            private bool _added;
            public bool IsRecalling;
            public bool Recall;
            public bool Recalled;

            public LastPositionObject(Obj_AI_Hero hero, ILogger logger)
            {
                try
                {
                    Hero = hero;
                    var mPos = Drawing.WorldToMinimap(hero.Position);
                    var spawnPoint =
                        ObjectManager.Get<GameObject>().FirstOrDefault(s => s is Obj_SpawnPoint && s.IsEnemy);

                    _sprite =
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
                                            var p =
                                                Drawing.WorldToMinimap(spawnPoint.Position);
                                            return new Vector2(p.X - (_sprite.Size.X/2), p.Y - (_sprite.Size.Y/2));
                                        }
                                    }
                                    var pos = Drawing.WorldToMinimap(hero.Position);
                                    return new Vector2(pos.X - (_sprite.Size.X/2), pos.Y - (_sprite.Size.Y/2));
                                }
                                catch (Exception ex)
                                {
                                    logger.AddItem(new LogItem(ex) {Object = this});
                                    return default(Vector2);
                                }
                            }
                        };
                    _recallSprite =
                        new Render.Sprite(Resources.LP_Recall, new Vector2(mPos.X, mPos.Y))
                        {
                            VisibleCondition = delegate
                            {
                                try
                                {
                                    return Active && !Hero.IsVisible && !Hero.IsDead && Recall && IsRecalling;
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
                                    var pos = Drawing.WorldToMinimap(hero.Position);
                                    return new Vector2(pos.X - (_recallSprite.Size.X/2),
                                        pos.Y - (_recallSprite.Size.Y/2));
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
                    Update();
                }
            }

            private void Update()
            {
                if (_sprite == null)
                    return;

                if (_active && !_added)
                {
                    _recallSprite.Add(0);
                    _sprite.Add(1);
                    _added = true;
                }
                else if (!_active && _added)
                {
                    _recallSprite.Remove();
                    _sprite.Remove();
                    _added = false;
                }
            }
        }
    }
}