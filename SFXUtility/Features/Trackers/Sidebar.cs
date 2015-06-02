#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Sidebar.cs is part of SFXUtility.

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
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXLibrary.Extensions.NET;
using SFXLibrary.Logger;
using SFXUtility.Classes;
using SFXUtility.Features.Detectors;
using SFXUtility.Properties;
using SharpDX;
using Color = SharpDX.Color;

#endregion

#pragma warning disable 618

namespace SFXUtility.Features.Trackers
{
    internal class Sidebar : Base
    {
        private List<EnemyObject> _enemyObjects;
        private Trackers _parent;
        public Sidebar(SFXUtility sfx) : base(sfx) {}

        public override bool Enabled
        {
            get
            {
                return !Unloaded && _parent != null && _parent.Enabled && Menu != null &&
                       Menu.Item(Name + "Enabled").GetValue<bool>();
            }
        }

        public override string Name
        {
            get { return Global.Lang.Get("F_Sidebar"); }
        }

        protected override void OnGameLoad(EventArgs args)
        {
            try
            {
                if (Global.IoC.IsRegistered<Trackers>())
                {
                    _parent = Global.IoC.Resolve<Trackers>();
                    if (_parent.Initialized)
                    {
                        OnParentInitialized(null, null);
                    }
                    else
                    {
                        _parent.OnInitialized += OnParentInitialized;
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected override void OnEnable()
        {
            Game.OnWndProc += OnGameWndProc;
            if (_enemyObjects != null)
            {
                _enemyObjects.ForEach(cd => cd.Active = true);
            }
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            Game.OnWndProc -= OnGameWndProc;
            if (_enemyObjects != null)
            {
                _enemyObjects.ForEach(cd => cd.Active = false);
            }
            base.OnDisable();
        }

        private void OnParentInitialized(object sender, EventArgs eventArgs)
        {
            try
            {
                if (_parent.Menu == null)
                {
                    return;
                }

                Menu = new Menu(Name, Name);

                var drawingMenu = new Menu(Global.Lang.Get("G_Drawing"), Name + "Drawing");

                drawingMenu.AddItem(
                    new MenuItem(
                        drawingMenu.Name + "OffsetTop", Global.Lang.Get("G_Offset") + " " + Global.Lang.Get("G_Top"))
                        .SetValue(new Slider(150, 0, 1000)));

                Menu.AddSubMenu(drawingMenu);
                Menu.AddItem(new MenuItem(Name + "Clickable", Global.Lang.Get("Sidebar_Clickable")).SetValue(false));

                Menu.AddItem(new MenuItem(Name + "Enabled", Global.Lang.Get("G_Enabled")).SetValue(false));

                _parent.Menu.AddSubMenu(Menu);

                HandleEvents(_parent);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected override void OnInitialize()
        {
            _enemyObjects = new List<EnemyObject>();

            if (Global.IoC.IsRegistered<Teleport>())
            {
                var rt = Global.IoC.Resolve<Teleport>();
                rt.OnFinish += TeleportHandle;
                rt.OnStart += TeleportHandle;
                rt.OnAbort += TeleportHandle;
                rt.OnUnknown += TeleportHandle;
            }

            var index = 0;
            foreach (var enemy in HeroManager.Enemies)
            {
                _enemyObjects.Add(
                    new EnemyObject(enemy, index++, Menu.Item(Name + "DrawingOffsetTop").GetValue<Slider>().Value)
                    {
                        Active = true
                    });
            }

            base.OnInitialize();
        }

        private void TeleportHandle(object sender, TeleportEventArgs teleportEventArgs)
        {
            var enemyObject = _enemyObjects.FirstOrDefault(e => e.Hero.NetworkId == teleportEventArgs.UnitNetworkId);
            if (enemyObject != null)
            {
                enemyObject.TeleportStatus = teleportEventArgs.Status;
            }
        }

        private void OnGameWndProc(WndEventArgs args)
        {
            if (!Menu.Item(Name + "Clickable").GetValue<bool>())
            {
                return;
            }

            if (args.Msg == (uint) WindowsMessages.WM_LBUTTONUP)
            {
                var pos = Utils.GetCursorPos();
                foreach (var enemy in
                    _enemyObjects.Where(e => Utils.IsUnderRectangle(pos, e.Position.X, e.Position.Y, e.Width, e.Height))
                    )
                {
                    if (ObjectManager.Player.Spellbook.ActiveSpellSlot != SpellSlot.Unknown)
                    {
                        var spell =
                            ObjectManager.Player.Spellbook.GetSpell(ObjectManager.Player.Spellbook.ActiveSpellSlot);
                        if (spell.SData.TargettingType == SpellDataTargetType.Unit)
                        {
                            ObjectManager.Player.Spellbook.CastSpell(spell.Slot, enemy.Hero);
                        }
                        else
                        {
                            ObjectManager.Player.IssueOrder(
                                GameObjectOrder.MoveTo,
                                enemy.Hero.Position.Extend(ObjectManager.Player.Position, spell.SData.CastRange));
                        }
                    }
                    else
                    {
                        ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, enemy.Hero);
                    }
                }
            }
            if (args.Msg == (uint) WindowsMessages.WM_RBUTTONUP)
            {
                var pos = Utils.GetCursorPos();
                foreach (var enemy in
                    _enemyObjects.Where(
                        e =>
                            !e.Hero.IsDead && e.Hero.IsVisible &&
                            Utils.IsUnderRectangle(pos, e.Position.X, e.Position.Y, e.Width, e.Height)))
                {
                    if (ObjectManager.Player.Path.Length > 0)
                    {
                        ObjectManager.Player.IssueOrder(
                            GameObjectOrder.MoveTo, ObjectManager.Player.Path[ObjectManager.Player.Path.Length - 1]);
                    }
                    else
                    {
                        ObjectManager.Player.IssueOrder(
                            GameObjectOrder.MoveTo,
                            ObjectManager.Player.ServerPosition.Distance(enemy.Hero.ServerPosition) >
                            ObjectManager.Player.AttackRange + ObjectManager.Player.BoundingRadius
                                ? enemy.Hero.ServerPosition
                                : ObjectManager.Player.Position);
                        ObjectManager.Player.IssueOrder(GameObjectOrder.AutoAttack, enemy.Hero);
                    }
                }
            }
        }

        private class EnemyObject
        {
            private readonly string[] _champsEnergy =
            {
                "Akali", "Kennen", "LeeSin", "Shen", "Zed", "Gnar", "Katarina",
                "RekSai", "Renekton", "Rengar", "Rumble"
            };

            private readonly string[] _champsNoEnergy =
            {
                "Aatrox", "DrMundo", "Vladimir", "Zac", "Katarina", "Garen",
                "Riven"
            };

            private readonly string[] _champsRage = { "Shyvana" };
            private readonly Render.Text _csText;
            private readonly Render.Text _deathText;
            private readonly Render.Line _healthLine;
            private readonly Render.Text _healthText;
            private readonly Render.Sprite _heroSprite;
            private readonly Render.Sprite _hudSprite;
            private readonly Render.Sprite _invisibleSprite;
            private readonly Render.Text _levelText;
            private readonly Render.Line _manaLine;
            private readonly Render.Text _manaText;
            private readonly SpellSlot[] _summonerSpellSlots = { SpellSlot.Summoner1, SpellSlot.Summoner2 };
            private readonly List<Render.Sprite> _summonerSprites = new List<Render.Sprite>();
            private readonly List<Render.Text> _summonerTexts = new List<Render.Text>();
            private readonly Render.Sprite _teleportAbortSprite;
            private readonly Render.Sprite _teleportFinishSprite;
            private readonly Render.Sprite _teleportStartSprite;
            private readonly Render.Sprite _ultimateSprite;
            private readonly Render.Text _ultimateText;
            private bool _active;
            private bool _added;
            private float _deathDuration;
            private float _lastTeleportStatusTime;
            private Packet.S2C.Teleport.Status _teleportStatus = Packet.S2C.Teleport.Status.Unknown;

            public EnemyObject(Obj_AI_Hero hero, int index, int offsetTop)
            {
                Hero = hero;
                try
                {
                    _hudSprite = new Render.Sprite(Resources.SB_Hud, Vector2.Zero)
                    {
                        VisibleCondition = delegate
                        {
                            try
                            {
                                return Active;
                            }
                            catch (Exception ex)
                            {
                                Global.Logger.AddItem(new LogItem(ex));
                                return false;
                            }
                        }
                    };
                    _hudSprite.Position = new Vector2(
                        Drawing.Width - _hudSprite.Width, index * (_hudSprite.Height + 5) + offsetTop);

                    _invisibleSprite = new Render.Sprite(Resources.SB_Invisible, _hudSprite.Position)
                    {
                        VisibleCondition = delegate
                        {
                            try
                            {
                                return Active && (!Hero.IsVisible || Hero.IsDead);
                            }
                            catch (Exception ex)
                            {
                                Global.Logger.AddItem(new LogItem(ex));
                                return false;
                            }
                        }
                    };

                    _heroSprite =
                        new Render.Sprite(
                            (Bitmap) Resources.ResourceManager.GetObject(string.Format("SB_{0}", Hero.ChampionName)) ??
                            Resources.SB_Aatrox, Vector2.Zero)
                        {
                            VisibleCondition = delegate
                            {
                                try
                                {
                                    return Active;
                                }
                                catch (Exception ex)
                                {
                                    Global.Logger.AddItem(new LogItem(ex));
                                    return false;
                                }
                            }
                        };
                    _heroSprite.Position = new Vector2(Drawing.Width - _heroSprite.Width - 1, _hudSprite.Y + 1);

                    _teleportStartSprite = new Render.Sprite(
                        Resources.SB_RecallStart, new Vector2(_hudSprite.Position.X - 4, _hudSprite.Position.Y - 4))
                    {
                        VisibleCondition =
                            delegate { return Active && TeleportStatus == Packet.S2C.Teleport.Status.Start; }
                    };

                    _teleportFinishSprite = new Render.Sprite(Resources.SB_RecallFinish, _teleportStartSprite.Position)
                    {
                        VisibleCondition =
                            delegate
                            {
                                return Active && TeleportStatus == Packet.S2C.Teleport.Status.Finish &&
                                       Game.Time <= _lastTeleportStatusTime + 5f;
                            }
                    };

                    _teleportAbortSprite = new Render.Sprite(Resources.SB_RecallAbort, _teleportStartSprite.Position)
                    {
                        VisibleCondition =
                            delegate
                            {
                                return Active && TeleportStatus == Packet.S2C.Teleport.Status.Abort &&
                                       Game.Time <= _lastTeleportStatusTime + 5f;
                            }
                    };

                    var spell = Hero.Spellbook.GetSpell(SpellSlot.R);
                    _ultimateSprite = new Render.Sprite(Resources.SB_Ultimate, Vector2.Zero)
                    {
                        VisibleCondition = delegate
                        {
                            try
                            {
                                return Active && spell != null && spell.Level > 0 &&
                                       spell.CooldownExpires - Game.Time <= 0;
                            }
                            catch (Exception ex)
                            {
                                Global.Logger.AddItem(new LogItem(ex));
                                return false;
                            }
                        }
                    };
                    _ultimateSprite.Position = new Vector2(Drawing.Width - _ultimateSprite.Width, _hudSprite.Y + 2);

                    _ultimateText = new Render.Text(
                        new Vector2(_ultimateSprite.X + 8, _ultimateSprite.Y + 8), string.Empty, 12, Color.LightGray)
                    {
                        Centered = true,
                        VisibleCondition = delegate
                        {
                            try
                            {
                                return Active && spell != null && spell.Level > 0 &&
                                       spell.CooldownExpires - Game.Time > 0;
                            }
                            catch (Exception ex)
                            {
                                Global.Logger.AddItem(new LogItem(ex));
                                return false;
                            }
                        },
                        TextUpdate =
                            () =>
                                ((int) (Hero.Spellbook.GetSpell(SpellSlot.R).CooldownExpires - Game.Time))
                                    .ToStringLookUp()
                    };


                    _healthLine = new Render.Line(
                        new Vector2(_heroSprite.X + 2, _heroSprite.Y + _heroSprite.Height + 6),
                        new Vector2(_heroSprite.X + _heroSprite.Width - 2, _heroSprite.Y + _heroSprite.Height + 6), 9,
                        Color.Green)
                    {
                        VisibleCondition = delegate
                        {
                            try
                            {
                                return Active;
                            }
                            catch (Exception ex)
                            {
                                Global.Logger.AddItem(new LogItem(ex));
                                return false;
                            }
                        },
                        EndPositionUpdate =
                            () =>
                                new Vector2(
                                    _heroSprite.X + (_heroSprite.Width - 2) * (Hero.Health / Hero.MaxHealth),
                                    _heroSprite.Y + _heroSprite.Height + 6)
                    };

                    _healthText = new Render.Text(
                        new Vector2(_healthLine.Start.X + 29, _healthLine.Start.Y), string.Empty, 13, Color.LightGray)
                    {
                        Centered = true,
                        VisibleCondition = delegate
                        {
                            try
                            {
                                return Active;
                            }
                            catch (Exception ex)
                            {
                                Global.Logger.AddItem(new LogItem(ex));
                                return false;
                            }
                        },
                        TextUpdate = () => string.Format("{0} / {1}", (int) Hero.Health, (int) Hero.MaxHealth)
                    };

                    if (!Enumerable.Contains(_champsNoEnergy, Hero.ChampionName))
                    {
                        _manaLine =
                            new Render.Line(
                                new Vector2(_healthLine.Start.X, _healthLine.Start.Y + _healthLine.Width + 4),
                                new Vector2(
                                    _heroSprite.X + _heroSprite.Width - 2,
                                    _heroSprite.Y + _heroSprite.Height + _healthLine.Width + 4), 9,
                                Enumerable.Contains(_champsEnergy, Hero.ChampionName)
                                    ? Color.Yellow
                                    : (Enumerable.Contains(_champsRage, Hero.ChampionName) ? Color.DarkRed : Color.Blue))
                            {
                                VisibleCondition = delegate
                                {
                                    try
                                    {
                                        return Active;
                                    }
                                    catch (Exception ex)
                                    {
                                        Global.Logger.AddItem(new LogItem(ex));
                                        return false;
                                    }
                                },
                                EndPositionUpdate =
                                    () =>
                                        new Vector2(
                                            _heroSprite.X + (_heroSprite.Width - 2) * (Hero.Mana / Hero.MaxMana),
                                            _heroSprite.Y + _heroSprite.Height + _healthLine.Width + 10)
                            };

                        _manaText = new Render.Text(
                            new Vector2(_manaLine.Start.X + 29, _manaLine.Start.Y), string.Empty, 13, Color.LightGray)
                        {
                            Centered = true,
                            VisibleCondition = delegate
                            {
                                try
                                {
                                    return Active;
                                }
                                catch (Exception ex)
                                {
                                    Global.Logger.AddItem(new LogItem(ex));
                                    return false;
                                }
                            },
                            TextUpdate = () => string.Format("{0} / {1}", (int) Hero.Mana, (int) Hero.MaxMana)
                        };
                    }

                    _deathText =
                        new Render.Text(
                            new Vector2(_heroSprite.X + _heroSprite.Width / 2f, _heroSprite.Y + _heroSprite.Height / 2f),
                            string.Empty, 30, Color.White)
                        {
                            OutLined = true,
                            Centered = true,
                            VisibleCondition = delegate
                            {
                                try
                                {
                                    return Active && Hero.IsDead;
                                }
                                catch (Exception ex)
                                {
                                    Global.Logger.AddItem(new LogItem(ex));
                                    return false;
                                }
                            },
                            TextUpdate = delegate
                            {
                                if (Hero.IsDead && Game.Time > _deathDuration)
                                {
                                    _deathDuration = Game.Time + Hero.DeathDuration + 1;
                                }
                                else if (!Hero.IsDead)
                                {
                                    _deathDuration = 0;
                                }
                                return ((int) (_deathDuration - Game.Time)).ToStringLookUp();
                            }
                        };

                    _levelText = new Render.Text(
                        new Vector2(Drawing.Width - 11, _heroSprite.Y + _heroSprite.Height - 8), string.Empty, 14,
                        Color.LightGray)
                    {
                        Centered = true,
                        VisibleCondition = delegate
                        {
                            try
                            {
                                return Active;
                            }
                            catch (Exception ex)
                            {
                                Global.Logger.AddItem(new LogItem(ex));
                                return false;
                            }
                        },
                        TextUpdate = () => Hero.Level.ToStringLookUp()
                    };

                    _csText = new Render.Text(
                        new Vector2(_heroSprite.X - 16, _heroSprite.Y + _heroSprite.Height + 3), string.Empty, 18,
                        Color.LightGray)
                    {
                        Centered = true,
                        VisibleCondition = delegate
                        {
                            try
                            {
                                return Active;
                            }
                            catch (Exception ex)
                            {
                                Global.Logger.AddItem(new LogItem(ex));
                                return false;
                            }
                        },
                        TextUpdate = () => Hero.MinionsKilled.ToStringLookUp()
                    };
                }
                catch (Exception ex)
                {
                    Global.Logger.AddItem(new LogItem(ex));
                }

                for (var i = 0; _summonerSpellSlots.Length > i; i++)
                {
                    try
                    {
                        var spell = Hero.Spellbook.GetSpell(_summonerSpellSlots[i]);
                        var sprite =
                            new Render.Sprite(
                                (Bitmap)
                                    Resources.ResourceManager.GetObject(string.Format("SB_{0}", spell.Name.ToLower())) ??
                                Resources.SB_summonerbarrier, Vector2.Zero)
                            {
                                VisibleCondition = delegate
                                {
                                    try
                                    {
                                        return Active;
                                    }
                                    catch (Exception ex)
                                    {
                                        Global.Logger.AddItem(new LogItem(ex));
                                        return false;
                                    }
                                }
                            };
                        sprite.Position = new Vector2(
                            _heroSprite.X - sprite.Width + 2, _heroSprite.Y + 6 + i * sprite.Height + i * 2);
                        _summonerSprites.Add(sprite);

                        var text =
                            new Render.Text(
                                new Vector2(
                                    sprite.Position.X - 1 - sprite.Width / 2f, sprite.Position.Y + sprite.Height / 2f),
                                string.Empty, 15, Color.LightGray)
                            {
                                OutLined = true,
                                Centered = true,
                                VisibleCondition = delegate
                                {
                                    try
                                    {
                                        return Active && spell.Slot != SpellSlot.Unknown &&
                                               spell.CooldownExpires - Game.Time > 0;
                                    }
                                    catch (Exception ex)
                                    {
                                        Global.Logger.AddItem(new LogItem(ex));
                                        return false;
                                    }
                                },
                                TextUpdate = () => ((int) (spell.CooldownExpires - Game.Time)).ToStringLookUp()
                            };
                        _summonerTexts.Add(text);
                    }
                    catch (Exception ex)
                    {
                        Global.Logger.AddItem(new LogItem(ex));
                    }
                }
            }

            public Packet.S2C.Teleport.Status TeleportStatus
            {
                private get { return _teleportStatus; }
                set
                {
                    _teleportStatus = value;
                    _lastTeleportStatusTime = Game.Time;
                }
            }

            public Obj_AI_Hero Hero { get; private set; }

            public Vector2 Position
            {
                get
                {
                    if (_hudSprite != null)
                    {
                        return _hudSprite.Position;
                    }
                    return Vector2.Zero;
                }
            }

            public int Height
            {
                get
                {
                    if (_hudSprite != null)
                    {
                        return _hudSprite.Height;
                    }
                    return 0;
                }
            }

            public int Width
            {
                get
                {
                    if (_hudSprite != null)
                    {
                        return _hudSprite.Width;
                    }
                    return 0;
                }
            }

            public bool Active
            {
                private get { return _active && Hero != null && Hero.IsValid; }
                set
                {
                    _active = value;
                    Update();
                }
            }

            private void Update()
            {
                if (_active && !_added)
                {
                    _heroSprite.Add(0);
                    _hudSprite.Add(1);
                    _teleportStartSprite.Add(0);
                    _teleportFinishSprite.Add(0);
                    _teleportAbortSprite.Add(0);
                    _ultimateSprite.Add(2);
                    _ultimateText.Add(2);
                    _healthLine.Add(2);
                    _healthText.Add(2);
                    if (_manaLine != null)
                    {
                        _manaLine.Add(2);
                    }
                    if (_manaText != null)
                    {
                        _manaText.Add(2);
                    }
                    _deathText.Add(2);
                    _levelText.Add(2);
                    _csText.Add(2);
                    _summonerSprites.ForEach(sp => sp.Add(0));
                    _summonerTexts.ForEach(sp => sp.Add(2));
                    _invisibleSprite.Add(3);
                    _added = true;
                }
                else if (!_active && _added)
                {
                    _heroSprite.Remove();
                    _hudSprite.Remove();
                    _ultimateSprite.Remove();
                    _healthLine.Remove();
                    if (_manaLine != null)
                    {
                        _manaLine.Remove();
                    }
                    _summonerSprites.ForEach(sp => sp.Remove());
                    _added = false;
                }
            }
        }
    }
}