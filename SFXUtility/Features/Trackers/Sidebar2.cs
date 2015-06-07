#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Sidebar2.cs is part of SFXUtility.

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

///*
// Copyright 2014 - 2015 Nikita Bernthaler
// Sidebar2.cs is part of SFXUtility.

// SFXUtility is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// SFXUtility is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with SFXUtility. If not, see <http://www.gnu.org/licenses/>.
//*/

//#endregion License

//#region

//using System;
//using System.Collections.Generic;
//using System.Drawing;
//using System.Linq;
//using LeagueSharp;
//using LeagueSharp.Common;
//using SFXLibrary.Extensions.NET;
//using SFXLibrary.Extensions.SharpDX;
//using SFXLibrary.Logger;
//using SFXUtility.Classes;
//using SFXUtility.Features.Detectors;
//using SFXUtility.Properties;
//using SharpDX;
//using SharpDX.Direct3D9;
//using Color = SharpDX.Color;
//using Font = SharpDX.Direct3D9.Font;

//#endregion

//#pragma warning disable 618

//namespace SFXUtility.Features.Trackers
//{

//    #region

//    #endregion

//    internal class Sidebar2 : Child<>
//    {
//        private readonly string[] _champsEnergy = { "Akali", "Kennen", "LeeSin", "Shen", "Zed", "Gnar", "Rengar" };

//        private readonly string[] _champsNoEnergy =
//        {
//            "Aatrox", "DrMundo", "Vladimir", "Zac", "Katarina", "Garen",
//            "Riven"
//        };

//        private readonly string[] _champsRage = { "Shyvana", "RekSai", "Renekton", "Rumble" };
//        private readonly List<EnemyObject> _enemyObjects = new List<EnemyObject>();
//        private readonly SpellSlot[] _summonerSpellSlots = { SpellSlot.Summoner1, SpellSlot.Summoner2 };
//        private readonly Dictionary<string, Texture> _summonerTextures = new Dictionary<string, Texture>();
//        private Texture _hudTexture;
//        private Texture _invisibleTexture;
//        private Line _line;
//        private Trackers Parent;
//        private Sprite _sprite;
//        private Texture _teleportAbortTexture;
//        private Texture _teleportFinishTexture;
//        private Texture _teleportStartTexture;
//        private Font _text12;
//        private Font _text13;
//        private Font _text18;
//        private Font _text30;
//        private Texture _ultimateTexture;
//        private const float HudWidth = 95f;
//        private const float HudHeight = 90f;
//        private const float SummonerWidth = 22f;
//        private const float SummonerHeight = 22f;
//        public Sidebar2(SFXUtility sfx) : base(sfx) {}

//        public override bool Enabled
//        {
//            get
//            {
//                return !Unloaded && Parent != null && Parent.Enabled && Menu != null &&
//                       Menu.Item(Name + "Enabled").GetValue<bool>();
//            }
//        }

//        public override string Name
//        {
//            get { return Global.Lang.Get("F_Sidebar"); }
//        }

//        protected override void OnGameLoad(EventArgs args)
//        {
//            try
//            {
//                if (Global.IoC.IsRegistered<Trackers>())
//                {
//                    Parent = Global.IoC.Resolve<Trackers>();
//                    if (Parent.Initialized)
//                    {
//                        OnParentInitialized(null, null);
//                    }
//                    else
//                    {
//                        Parent.OnInitialized += OnParentInitialized;
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Global.Logger.AddItem(new LogItem(ex));
//            }
//        }

//        protected override void OnEnable()
//        {
//            Game.OnWndProc += OnGameWndProc;
//            Drawing.OnPreReset += OnDrawingPreReset;
//            Drawing.OnPostReset += OnDrawingPostReset;
//            Drawing.OnEndScene += OnDrawingEndScene;

//            base.OnEnable();
//        }

//        protected override void OnDisable()
//        {
//            Game.OnWndProc -= OnGameWndProc;
//            Drawing.OnPreReset -= OnDrawingPreReset;
//            Drawing.OnPostReset -= OnDrawingPostReset;
//            Drawing.OnEndScene -= OnDrawingEndScene;

//            OnUnload(null, new UnloadEventArgs());

//            base.OnDisable();
//        }

//        protected override void OnUnload(object sender, UnloadEventArgs args)
//        {
//            if (args != null && args.Final)
//            {
//                base.OnUnload(sender, args);
//            }

//            if (Initialized)
//            {
//                OnDrawingPreReset(null);
//                OnDrawingPostReset(null);
//            }
//        }

//        private void OnDrawingEndScene(EventArgs args)
//        {
//            try
//            {
//                if (Drawing.Direct3DDevice == null || Drawing.Direct3DDevice.IsDisposed)
//                {
//                    return;
//                }

//                var index = 0;
//                var scale = Menu.Item(Menu.Name + "DrawingScale").GetValue<Slider>().Value / 10f;

//                var hudWidth = (float) (Math.Ceiling(HudWidth * scale));
//                var hudHeight = (float)(Math.Ceiling(HudHeight * scale));

//                var spacing = (float) (Math.Ceiling(20f * scale)) + hudHeight;

//                var offsetTop = Menu.Item(Menu.Name + "DrawingOffsetTop").GetValue<Slider>().Value + hudHeight / 2;
//                var offsetRight = Drawing.Width - Menu.Item(Menu.Name + "DrawingOffsetRight").GetValue<Slider>().Value -
//                                  hudWidth / 2;

//                foreach (var enemy in _enemyObjects)
//                {
//                    if (enemy.Unit.IsDead && Game.Time > enemy.DeathEndTime)
//                    {
//                        enemy.DeathEndTime = Game.Time + enemy.Unit.DeathDuration + 1;
//                    }
//                    else if (!enemy.Unit.IsDead)
//                    {
//                        enemy.DeathEndTime = 0;
//                    }

//                    var offset = spacing * index;

//                    if (enemy.TeleportStatus == Packet.S2C.Teleport.Status.Start ||
//                        (enemy.TeleportStatus == Packet.S2C.Teleport.Status.Finish ||
//                         enemy.TeleportStatus == Packet.S2C.Teleport.Status.Abort) &&
//                        Game.Time <= enemy.LastTeleportStatusTime + 5f)
//                    {
//                        _sprite.Begin(SpriteFlags.AlphaBlend);
//                        _sprite.DrawCentered(
//                            enemy.TeleportStatus == Packet.S2C.Teleport.Status.Start
//                                ? _teleportStartTexture
//                                : (enemy.TeleportStatus == Packet.S2C.Teleport.Status.Finish
//                                    ? _teleportFinishTexture
//                                    : _teleportAbortTexture),
//                            new Vector2(
//                                offsetRight + (float) (Math.Ceiling(4 * scale)),
//                                offsetTop + (float) (Math.Ceiling(1 * scale)) + offset));
//                        _sprite.End();
//                    }

//                    for (var i = 0; _summonerSpellSlots.Length > i; i++)
//                    {
//                        var spell = enemy.Unit.Spellbook.GetSpell(_summonerSpellSlots[i]);
//                        if (spell != null && _summonerTextures.ContainsKey(spell.Name))
//                        {
//                            _sprite.Begin(SpriteFlags.AlphaBlend);
//                            _sprite.DrawCentered(
//                                _summonerTextures[spell.Name],
//                                new Vector2(
//                                    offsetRight - hudWidth * 0.23f,
//                                    offsetTop - hudHeight * 0.3f + offset + ((float) (Math.Ceiling(24 * scale)) * i)));
//                            _sprite.End();
//                            if (spell.CooldownExpires - Game.Time > 0)
//                            {
//                                _text13.DrawTextCentered(
//                                    ((int) (spell.CooldownExpires - Game.Time)).ToStringLookUp(),
//                                    new Vector2(
//                                        offsetRight - hudWidth * 0.23f,
//                                        offsetTop - hudHeight * 0.3f + offset + ((float) (Math.Ceiling(24 * scale)) * i)),
//                                    Color.White, true);
//                            }
//                        }
//                    }

//                    _sprite.Begin(SpriteFlags.AlphaBlend);

//                    _sprite.DrawCentered(
//                        enemy.Texture,
//                        new Vector2(offsetRight + hudWidth * 0.21f, offsetTop - hudHeight * 0.13f + offset));
//                    _sprite.DrawCentered(
//                        _hudTexture, new Vector2(offsetRight + (float) (Math.Ceiling(3 * scale)), offsetTop + offset));

//                    if (enemy.RSpell != null && enemy.RSpell.CooldownExpires - Game.Time < 0)
//                    {
//                        _sprite.DrawCentered(
//                            _ultimateTexture,
//                            new Vector2(offsetRight + hudWidth * 0.445f, offsetTop - hudHeight * 0.385f + offset));
//                    }

//                    _sprite.End();

//                    if (enemy.RSpell != null && enemy.RSpell.CooldownExpires - Game.Time > 0)
//                    {
//                        _text12.DrawTextCentered(
//                            ((int) (enemy.RSpell.CooldownExpires - Game.Time)).ToStringLookUp(),
//                            new Vector2(offsetRight + hudWidth * 0.45f, offsetTop - hudHeight * 0.37f + offset),
//                            Color.White, true);
//                    }

//                    _text12.DrawTextCentered(
//                        enemy.Unit.Level.ToStringLookUp(),
//                        new Vector2(offsetRight + hudWidth * 0.43f, offsetTop + hudHeight * 0.13f + offset), Color.White);

//                    if (!Enumerable.Contains(_champsNoEnergy, enemy.Unit.ChampionName))
//                    {
//                        _line.Draw(
//                            new[]
//                            {
//                                new Vector2(offsetRight - hudWidth * 0.1f, offsetTop + hudHeight * 0.415f + offset),
//                                new Vector2(offsetRight + hudWidth * 0.51f, offsetTop + hudHeight * 0.415f + offset)
//                            },
//                            Enumerable.Contains(_champsEnergy, enemy.Unit.ChampionName)
//                                ? Color.Yellow
//                                : (Enumerable.Contains(_champsRage, enemy.Unit.ChampionName)
//                                    ? Color.DarkRed
//                                    : Color.Blue));
//                        _text13.DrawTextCentered(
//                            (int) (enemy.Unit.Mana) + " / " + (int) (enemy.Unit.MaxMana),
//                            new Vector2(offsetRight + hudWidth * 0.21f, offsetTop + hudHeight * 0.425f + offset),
//                            Color.White, true);
//                    }

//                    _line.Draw(
//                        new[]
//                        {
//                            new Vector2(offsetRight - hudWidth * 0.1f, offsetTop + hudHeight * 0.265f + offset),
//                            new Vector2(offsetRight + hudWidth * 0.51f, offsetTop + hudHeight * 0.265f + offset)
//                        },
//                        Color.Green);
//                    _text13.DrawTextCentered(
//                        (int) (enemy.Unit.Health) + " / " + (int) (enemy.Unit.MaxHealth),
//                        new Vector2(offsetRight + hudWidth * 0.21f, offsetTop + hudHeight * 0.275f + offset),
//                        Color.White, true);

//                    _text18.DrawTextCentered(
//                        (enemy.Unit.MinionsKilled + enemy.Unit.NeutralMinionsKilled).ToStringLookUp(),
//                        new Vector2(offsetRight - hudWidth * 0.28f, offsetTop + hudHeight * 0.24f + offset), Color.White);

//                    if (enemy.Unit.IsDead)
//                    {
//                        _text30.DrawTextCentered(
//                            ((int) (enemy.DeathEndTime - Game.Time)).ToStringLookUp(),
//                            new Vector2(offsetRight + hudWidth * 0.21f, offsetTop - hudHeight * 0.11f + offset),
//                            Color.White, true);
//                    }

//                    if (!enemy.Unit.IsVisible || enemy.Unit.IsDead)
//                    {
//                        _sprite.Begin(SpriteFlags.AlphaBlend);
//                        _sprite.DrawCentered(_invisibleTexture, new Vector2(offsetRight + 3, offsetTop + 1 + offset));
//                        _sprite.End();
//                    }

//                    index++;
//                }
//            }
//            catch (Exception ex)
//            {
//                Global.Logger.AddItem(new LogItem(ex));
//            }
//        }

//        private void OnDrawingPostReset(EventArgs args)
//        {
//            try
//            {
//                _sprite.OnResetDevice();
//                _text12.OnResetDevice();
//            }
//            catch (Exception ex)
//            {
//                Global.Logger.AddItem(new LogItem(ex));
//            }
//        }

//        private void OnDrawingPreReset(EventArgs args)
//        {
//            try
//            {
//                _sprite.OnLostDevice();
//                _text12.OnLostDevice();
//            }
//            catch (Exception ex)
//            {
//                Global.Logger.AddItem(new LogItem(ex));
//            }
//        }

//        protected override void OnLoad()
//        {
//            try
//            {
//                if (Parent.Menu == null)
//                {
//                    return;
//                }

//                Menu = new Menu(Name, Name);

//                var drawingMenu = new Menu(Global.Lang.Get("G_Drawing"), Name + "Drawing");

//                drawingMenu.AddItem(
//                    new MenuItem(
//                        drawingMenu.Name + "OffsetTop", Global.Lang.Get("G_Offset") + " " + Global.Lang.Get("G_Top"))
//                        .SetValue(new Slider(150, 0, Drawing.Height)));

//                drawingMenu.AddItem(
//                    new MenuItem(
//                        drawingMenu.Name + "OffsetRight", Global.Lang.Get("G_Offset") + " " + Global.Lang.Get("G_Right"))
//                        .SetValue(new Slider(0, 0, Drawing.Width)));

//                drawingMenu.AddItem(
//                    new MenuItem(drawingMenu.Name + "Scale", Global.Lang.Get("G_Scale")).SetValue(new Slider(10, 5, 15)));

//                Menu.AddSubMenu(drawingMenu);
//                Menu.AddItem(new MenuItem(Name + "Clickable", Global.Lang.Get("Sidebar_Clickable")).SetValue(false));

//                Menu.AddItem(new MenuItem(Name + "Enabled", Global.Lang.Get("G_Enabled")).SetValue(false));

//                Parent.Menu.AddSubMenu(Menu);

//                if (!HeroManager.Enemies.Any())
//                {
//                    return;
//                }

//                if (Global.IoC.IsRegistered<Teleport>())
//                {
//                    var rt = Global.IoC.Resolve<Teleport>();
//                    rt.OnFinish += TeleportHandle;
//                    rt.OnStart += TeleportHandle;
//                    rt.OnAbort += TeleportHandle;
//                    rt.OnUnknown += TeleportHandle;
//                }

//                var scale = Menu.Item(Menu.Name + "DrawingScale").GetValue<Slider>().Value / 10f;

//                _text12 = new Font(
//                    Drawing.Direct3DDevice,
//                    new FontDescription
//                    {
//                        FaceName = Global.DefaultFont,
//                        Height = (int) (Math.Ceiling(12 * scale)),
//                        OutputPrecision = FontPrecision.Default,
//                        Quality = FontQuality.Default
//                    });
//                _text13 = new Font(
//                    Drawing.Direct3DDevice,
//                    new FontDescription
//                    {
//                        FaceName = Global.DefaultFont,
//                        Height = (int)(Math.Ceiling(13 * scale)),
//                        OutputPrecision = FontPrecision.Default,
//                        Quality = FontQuality.Default
//                    });
//                _text18 = new Font(
//                    Drawing.Direct3DDevice,
//                    new FontDescription
//                    {
//                        FaceName = Global.DefaultFont,
//                        Height = (int) (Math.Ceiling(18 * scale)),
//                        OutputPrecision = FontPrecision.Default,
//                        Quality = FontQuality.Default
//                    });
//                _text30 = new Font(
//                    Drawing.Direct3DDevice,
//                    new FontDescription
//                    {
//                        FaceName = Global.DefaultFont,
//                        Height = (int) (Math.Ceiling(30 * scale)),
//                        OutputPrecision = FontPrecision.Default,
//                        Quality = FontQuality.Default
//                    });

//                foreach (var enemy in HeroManager.Enemies)
//                {
//                    _enemyObjects.Add(
//                        new EnemyObject(
//                            enemy,
//                            (((Bitmap) Resources.ResourceManager.GetObject(string.Format("SB_{0}", enemy.ChampionName)))
//                                .Scale(scale) ?? Resources.SB_Aatrox.Scale(scale)).ToTexture()));
//                }

//                foreach (var summonerSlot in _summonerSpellSlots)
//                {
//                    foreach (var enemy in HeroManager.Enemies)
//                    {
//                        var spell = enemy.Spellbook.GetSpell(summonerSlot);
//                        if (!_summonerTextures.ContainsKey(spell.Name))
//                        {
//                            _summonerTextures[spell.Name] =
//                                (((Bitmap)
//                                    Resources.ResourceManager.GetObject(string.Format("SB_{0}", spell.Name.ToLower())))
//                                    .Scale(scale) ?? Resources.SB_summonerbarrier).Scale(scale).ToTexture();
//                        }
//                    }
//                }

//                _hudTexture = Resources.SB_Hud.Scale(scale).ToTexture();
//                _invisibleTexture = Resources.SB_Invisible.Scale(scale).ToTexture();
//                _teleportAbortTexture = Resources.SB_RecallAbort.Scale(scale).ToTexture();
//                _teleportFinishTexture = Resources.SB_RecallFinish.Scale(scale).ToTexture();
//                _teleportStartTexture = Resources.SB_RecallStart.Scale(scale).ToTexture();
//                _ultimateTexture = Resources.SB_Ultimate.Scale(scale).ToTexture();
//                _line = new Line(Drawing.Direct3DDevice) { Width = (int)(Math.Ceiling(9*scale)) };
//                _sprite = new Sprite(Drawing.Direct3DDevice);
//                
//                RaiseOnInitialized();
//            }
//            catch (Exception ex)
//            {
//                Global.Logger.AddItem(new LogItem(ex));
//            }
//        }

//        private void TeleportHandle(object sender, TeleportEventArgs teleportEventArgs)
//        {
//            var enemyObject = _enemyObjects.FirstOrDefault(e => e.Unit.NetworkId == teleportEventArgs.UnitNetworkId);
//            if (enemyObject != null)
//            {
//                enemyObject.TeleportStatus = teleportEventArgs.Status;
//            }
//        }

//        private void OnGameWndProc(WndEventArgs args)
//        {
//            if (!Menu.Item(Name + "Clickable").GetValue<bool>())
//                return;

//            var index = 0;
//            var scale = Menu.Item(Menu.Name + "DrawingScale").GetValue<Slider>().Value / 10f;

//            var hudWidth = (float)(Math.Ceiling(HudWidth * scale));
//            var hudHeight = (float)(Math.Ceiling(HudHeight * scale));

//            var spacing = (float)(Math.Ceiling(20f * scale)) + hudHeight;

//            var offsetTop = Menu.Item(Menu.Name + "DrawingOffsetTop").GetValue<Slider>().Value + hudHeight / 2;
//            var offsetRight = Drawing.Width - Menu.Item(Menu.Name + "DrawingOffsetRight").GetValue<Slider>().Value -
//                              hudWidth / 2;

//            if (args.Msg == (uint)WindowsMessages.WM_RBUTTONUP || args.Msg == (uint)WindowsMessages.WM_LBUTTONDBLCLCK)
//            {
//                var pos = Utils.GetCursorPos();
//                foreach (var enemy in _enemyObjects)
//                {
//                    var offset = spacing * index;
//                    if (args.Msg == (uint) WindowsMessages.WM_LBUTTONDBLCLCK)
//                    {
//                        for (var i = 0; _summonerSpellSlots.Length > i; i++)
//                        {
//                            var spell = enemy.Unit.Spellbook.GetSpell(_summonerSpellSlots[i]);
//                            if (spell != null)
//                            {
//                                if (Utils.IsUnderRectangle(
//                                pos, offsetRight - hudWidth * 0.23f - SummonerWidth/2f,
//                                offsetTop - hudHeight * 0.3f + offset + ((float)(Math.Ceiling(24 * scale)) * i) - SummonerHeight/2f, SummonerWidth, SummonerHeight))
//                                {
//                                    if (spell.CooldownExpires - Game.Time < 0)
//                                    {
//                                        Game.Say(string.Format("{0} {1} {2}", enemy.Unit.ChampionName, spell.Name, (Game.ClockTime).FormatTime()));
//                                    }
//                                }
//                            }
//                        }
//                    }
//                    else
//                    {
//                        if (enemy.Unit.IsVisible && !enemy.Unit.IsDead && Utils.IsUnderRectangle(
//                           pos, offsetRight + (float)(Math.Ceiling(3 * scale)) - hudWidth / 2f,
//                           offsetTop + offset - hudHeight / 2f, hudWidth, hudHeight))
//                        {
//                            ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, enemy.Unit);
//                        }
//                    }
//                    index++;
//                }
//            }
//        }

//        private class EnemyObject
//        {
//            private Packet.S2C.Teleport.Status _teleportStatus;

//            public EnemyObject(Obj_AI_Hero unit, Texture texture)
//            {
//                TeleportStatus = Packet.S2C.Teleport.Status.Unknown;
//                Unit = unit;
//                Texture = texture;
//                RSpell = unit.GetSpell(SpellSlot.R);
//            }

//            public Texture Texture { get; private set; }
//            public SpellDataInst RSpell { get; private set; }
//            public Obj_AI_Hero Unit { get; private set; }
//            public float DeathEndTime { get; set; }
//            public float LastTeleportStatusTime { get; private set; }

//            public Packet.S2C.Teleport.Status TeleportStatus
//            {
//                get { return _teleportStatus; }
//                set
//                {
//                    _teleportStatus = value;
//                    LastTeleportStatusTime = Game.Time;
//                }
//            }
//        }
//    }
//}
