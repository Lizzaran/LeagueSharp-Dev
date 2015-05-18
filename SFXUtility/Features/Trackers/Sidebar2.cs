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

//#pragma warning disable 618

//namespace SFXUtility.Features.Trackers
//{
//    #region

//    using System;
//    using System.Collections.Generic;
//    using System.Drawing;
//    using System.Linq;
//    using Classes;
//    using Detectors;
//    using LeagueSharp;
//    using LeagueSharp.Common;
//    using Properties;
//    using SFXLibrary.Extensions.NET;
//    using SFXLibrary.Extensions.SharpDX;
//    using SFXLibrary.Logger;
//    using SharpDX;
//    using SharpDX.Direct3D9;
//    using Font = SharpDX.Direct3D9.Font;

//    #endregion

//    internal class Sidebar2 : Base
//    {
//        private readonly string[] _champsEnergy =
//        {
//            "Akali", "Kennen", "LeeSin", "Shen", "Zed", "Gnar", "Katarina", "RekSai", "Renekton", "Rengar",
//            "Rumble"
//        };

//        private readonly string[] _champsNoEnergy = {"Aatrox", "DrMundo", "Vladimir", "Zac", "Katarina", "Garen", "Riven"};
//        private readonly string[] _champsRage = {"Shyvana"};
//        private readonly Dictionary<int, Texture> _heroTextures = new Dictionary<int, Texture>();
//        private readonly SpellSlot[] _summonerSpellSlots = {SpellSlot.Summoner1, SpellSlot.Summoner2};
//        private readonly Dictionary<string, Texture> _summonerTextures = new Dictionary<string, Texture>();
//        private Texture _hudTexture;
//        private Texture _invisibleTexture;
//        private Line _line;
//        private Trackers _parent;
//        private Texture _teleportAbortTexture;
//        private Texture _teleportFinishTexture;
//        private Texture _teleportStartTexture;
//        private Font _text12;
//        private Font _text13;
//        private Font _text18;
//        private Font _text30;
//        private Texture _ultimateTexture;
//        private Sprite _sprite;

//        public override bool Enabled
//        {
//            get { return !Unloaded && _parent != null && _parent.Enabled && Menu != null && Menu.Item(Name + "Enabled").GetValue<bool>(); }
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
//                    _parent = Global.IoC.Resolve<Trackers>();
//                    if (_parent.Initialized)
//                        OnParentInitialized(null, null);
//                    else
//                        _parent.OnInitialized += OnParentInitialized;
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
//                base.OnUnload(sender, args);

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
//                    return;

//                var index = 0;
//                var scale = Menu.Item(Menu.Name + "DrawingScale").GetValue<Slider>().Value/10f;

//                var hudWidth = (int)(95 * scale);
//                var hudHeight = (int)(90 * scale);

//                var spacing = 20*scale + hudHeight;

//                var offsetTop = Menu.Item(Menu.Name + "DrawingOffsetTop").GetValue<Slider>().Value + hudHeight/2;
//                var offsetRight = Drawing.Width - Menu.Item(Menu.Name + "DrawingOffsetRight").GetValue<Slider>().Value - hudWidth/2;

//                _sprite.Begin(SpriteFlags.AlphaBlend);

//                foreach (var enemy in HeroManager.Enemies)
//                {
//                    var offset = spacing * index;
//                    _sprite.DrawCentered(_teleportStartTexture, new Vector2(offsetRight, offsetTop + 1 * scale + offset));

//                    for (var i = 0; _summonerSpellSlots.Length > i; i++)
//                    {
//                        var spell = enemy.Spellbook.GetSpell(_summonerSpellSlots[i]);
//                        if (_summonerTextures.ContainsKey(spell.Name))
//                        {
//                            _sprite.DrawCentered(_summonerTextures[spell.Name], new Vector2(offsetRight - 23 * scale, offsetTop - 28 * scale + offset + ((22 * scale + 3 * scale) * i)));
//                        }
//                    }

//                    _sprite.DrawCentered(_heroTextures[enemy.NetworkId], new Vector2(offsetRight + 20 * scale, offsetTop - 13 * scale + offset));
//                    _sprite.DrawCentered(_hudTexture, new Vector2(offsetRight + 3 * scale, offsetTop + offset));
//                    _sprite.DrawCentered(_ultimateTexture, new Vector2(offsetRight + 42 * scale, offsetTop - 35 * scale + offset));
//                    _text12.DrawTextCentered(enemy.Level.ToString(), new Vector2(offsetRight + 42 * scale, offsetTop + 50 * scale + offset), SharpDX.Color.White);

//                    index++;
//                }

//                _sprite.End();
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

//        private void OnParentInitialized(object sender, EventArgs eventArgs)
//        {
//            try
//            {
//                if (_parent.Menu == null)
//                    return;

//                Menu = new Menu(Name, Name);

//                var drawingMenu = new Menu(Global.Lang.Get("G_Drawing"), Name + "Drawing");

//                drawingMenu.AddItem(
//                    new MenuItem(drawingMenu.Name + "OffsetTop", Global.Lang.Get("G_Offset") + " " + Global.Lang.Get("G_Top")).SetValue(new Slider(
//                        150, 0, Drawing.Height)));

//                drawingMenu.AddItem(
//                    new MenuItem(drawingMenu.Name + "OffsetRight", Global.Lang.Get("G_Offset") + " " + Global.Lang.Get("G_Right")).SetValue(new Slider(
//                        0, 0, Drawing.Width)));

//                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + "Scale", Global.Lang.Get("G_Scale")).SetValue(new Slider(10, 1, 20)));

//                Menu.AddSubMenu(drawingMenu);
//                Menu.AddItem(new MenuItem(Name + "Clickable", Global.Lang.Get("Sidebar_Clickable")).SetValue(false));

//                Menu.AddItem(new MenuItem(Name + "Enabled", Global.Lang.Get("G_Enabled")).SetValue(false));

//                _parent.Menu.AddSubMenu(Menu);

//                if (!HeroManager.Enemies.Any())
//                    return;

//                if (Global.IoC.IsRegistered<Teleport>())
//                {
//                    var rt = Global.IoC.Resolve<Teleport>();
//                    rt.OnFinish += TeleportHandle;
//                    rt.OnStart += TeleportHandle;
//                    rt.OnAbort += TeleportHandle;
//                    rt.OnUnknown += TeleportHandle;
//                }

//                var scale = Menu.Item(Menu.Name + "DrawingScale").GetValue<Slider>().Value/10f;

//                _text12 = new Font(Drawing.Direct3DDevice,
//                    new FontDescription
//                    {
//                        FaceName = Global.DefaultFont,
//                        Height = (int)(12 * scale),
//                        OutputPrecision = FontPrecision.Default,
//                        Quality = FontQuality.Default
//                    });
//                _text13 = new Font(Drawing.Direct3DDevice,
//                    new FontDescription
//                    {
//                        FaceName = Global.DefaultFont,
//                        Height = (int)(13 * scale),
//                        OutputPrecision = FontPrecision.Default,
//                        Quality = FontQuality.Default
//                    });
//                _text18 = new Font(Drawing.Direct3DDevice,
//                    new FontDescription
//                    {
//                        FaceName = Global.DefaultFont,
//                        Height = (int)(18 * scale),
//                        OutputPrecision = FontPrecision.Default,
//                        Quality = FontQuality.Default
//                    });
//                _text30 = new Font(Drawing.Direct3DDevice,
//                    new FontDescription
//                    {
//                        FaceName = Global.DefaultFont,
//                        Height = (int)(30 * scale),
//                        OutputPrecision = FontPrecision.Default,
//                        Quality = FontQuality.Default
//                    });

//                foreach (var enemy in HeroManager.Enemies)
//                {
//                    _heroTextures[enemy.NetworkId] =
//                        (((Bitmap)Resources.ResourceManager.GetObject(string.Format("SB_{0}", enemy.ChampionName))).Scale(scale) ?? Resources.SB_Aatrox.Scale(scale)).ToTexture();
//                }

//                foreach (var summonerSlot in _summonerSpellSlots)
//                {
//                    foreach (var enemy in HeroManager.Enemies)
//                    {
//                        var spell = enemy.Spellbook.GetSpell(summonerSlot);
//                        if (!_summonerTextures.ContainsKey(spell.Name))
//                        {
//                            _summonerTextures[spell.Name] =
//                                (((Bitmap)Resources.ResourceManager.GetObject(string.Format("SB_{0}", spell.Name.ToLower()))).Scale(scale) ??
//                                 Resources.SB_summonerbarrier).Scale(scale).ToTexture();
//                        }
//                    }
//                }

//                _hudTexture = Resources.SB_Hud.Scale(scale).ToTexture();
//                _invisibleTexture = Resources.SB_Invisible.Scale(scale).ToTexture();
//                _teleportAbortTexture = Resources.SB_RecallAbort.Scale(scale).ToTexture();
//                _teleportFinishTexture = Resources.SB_RecallFinish.Scale(scale).ToTexture();
//                _teleportStartTexture = Resources.SB_RecallStart.Scale(scale).ToTexture();
//                _ultimateTexture = Resources.SB_Ultimate.Scale(scale).ToTexture();
//                _line = new Line(Drawing.Direct3DDevice);
//                _sprite = new Sprite(Drawing.Direct3DDevice);
//                HandleEvents(_parent);
//                RaiseOnInitialized();
//            }
//            catch (Exception ex)
//            {
//                Global.Logger.AddItem(new LogItem(ex));
//            }
//        }

//        private void TeleportHandle(object sender, TeleportEventArgs teleportEventArgs)
//        {
//            //var enemyObject = _enemyObjects.FirstOrDefault(e => e.Hero.NetworkId == teleportEventArgs.UnitNetworkId);
//            //if (enemyObject != null)
//            //{
//            //    enemyObject.TeleportStatus = teleportEventArgs.Status;
//            //}
//        }

//        private void OnGameWndProc(WndEventArgs args)
//        {
//            //if (!Menu.Item(Name + "Clickable").GetValue<bool>())
//            //    return;

//            //if (args.Msg == (uint) WindowsMessages.WM_LBUTTONUP)
//            //{
//            //    var pos = Utils.GetCursorPos();
//            //    foreach (var enemy in _enemyObjects.Where(e => Utils.IsUnderRectangle(pos, e.Position.X, e.Position.Y, e.Width, e.Height)))
//            //    {
//            //        if (ObjectManager.Player.Spellbook.ActiveSpellSlot != SpellSlot.Unknown)
//            //        {
//            //            var spell = ObjectManager.Player.Spellbook.GetSpell(ObjectManager.Player.Spellbook.ActiveSpellSlot);
//            //            if (spell.SData.TargettingType == SpellDataTargetType.Unit)
//            //            {
//            //                ObjectManager.Player.Spellbook.CastSpell(spell.Slot, enemy.Hero);
//            //            }
//            //            else
//            //            {
//            //                ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo,
//            //                    enemy.Hero.Position.Extend(ObjectManager.Player.Position, spell.SData.CastRange));
//            //            }
//            //        }
//            //        else
//            //        {
//            //            ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, enemy.Hero);
//            //        }
//            //    }
//            //}
//            //if (args.Msg == (uint) WindowsMessages.WM_RBUTTONUP)
//            //{
//            //    var pos = Utils.GetCursorPos();
//            //    foreach (var enemy in
//            //        _enemyObjects.Where(
//            //            e => !e.Hero.IsDead && e.Hero.IsVisible && Utils.IsUnderRectangle(pos, e.Position.X, e.Position.Y, e.Width, e.Height)))
//            //    {
//            //        if (ObjectManager.Player.Path.Length > 0)
//            //            ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, ObjectManager.Player.Path[ObjectManager.Player.Path.Length - 1]);
//            //        else
//            //        {
//            //            ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo,
//            //                ObjectManager.Player.ServerPosition.Distance(enemy.Hero.ServerPosition) >
//            //                ObjectManager.Player.AttackRange + ObjectManager.Player.BoundingRadius
//            //                    ? enemy.Hero.ServerPosition
//            //                    : ObjectManager.Player.Position);
//            //            ObjectManager.Player.IssueOrder(GameObjectOrder.AutoAttack, enemy.Hero);
//            //        }
//            //    }
//            //}
//        }

//        private class EnemyObject
//        {
//            public float DeathDuration { get; set; }
//            public float LastTeleportStatusTime { get; set; }
//            public Packet.S2C.Teleport.Status TeleportStatus { get; set; }

//            public EnemyObject()
//            {
//                TeleportStatus = Packet.S2C.Teleport.Status.Unknown;
//            }
//        }
//    }
//}