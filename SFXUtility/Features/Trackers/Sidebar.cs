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
using SFXUtility.Classes;
using SFXUtility.Library;
using SFXUtility.Library.Extensions.NET;
using SFXUtility.Library.Extensions.SharpDX;
using SFXUtility.Library.Logger;
using SFXUtility.Properties;
using SharpDX;
using SharpDX.Direct3D9;
using Color = SharpDX.Color;
using Font = SharpDX.Direct3D9.Font;
using Utils = LeagueSharp.Common.Utils;

#endregion

#pragma warning disable 618

namespace SFXUtility.Features.Trackers
{
    internal class Sidebar : Child<Trackers>
    {
        private const float HudWidth = 95f;
        private const float HudHeight = 90f;
        private const float SummonerWidth = 22f;
        private const float SummonerHeight = 22f;
        private readonly string[] _champsEnergy = { "Akali", "Kennen", "LeeSin", "Shen", "Zed", "Gnar", "Rengar" };

        private readonly string[] _champsNoEnergy =
        {
            "Aatrox", "DrMundo", "Vladimir", "Zac", "Katarina", "Garen",
            "Riven"
        };

        private readonly string[] _champsRage = { "Shyvana", "RekSai", "Renekton", "Rumble" };
        private readonly List<EnemyObject> _enemyObjects = new List<EnemyObject>();
        private readonly Dictionary<int, List<SpellDataInst>> _spellDatas = new Dictionary<int, List<SpellDataInst>>();
        private readonly SpellSlot[] _summonerSpellSlots = { SpellSlot.Summoner1, SpellSlot.Summoner2 };
        private readonly Dictionary<string, Texture> _summonerTextures = new Dictionary<string, Texture>();
        private readonly Dictionary<int, float> _teleports = new Dictionary<int, float>();
        private Texture _hudTexture;
        private Texture _invisibleTexture;
        private float _lastChatSend;
        private Line _line;
        private float _scale;
        private Sprite _sprite;
        private Texture _teleportAbortTexture;
        private Texture _teleportFinishTexture;
        private Texture _teleportStartTexture;
        private Font _text12;
        private Font _text13;
        private Font _text18;
        private Font _text30;
        private Texture _ultimateTexture;

        public Sidebar(Trackers parent) : base(parent)
        {
            OnLoad();
        }

        public override string Name
        {
            get { return "Sidebar"; }
        }

        protected override void OnEnable()
        {
            Game.OnWndProc += OnGameWndProc;
            Drawing.OnEndScene += OnDrawingEndScene;
            Obj_AI_Base.OnTeleport += OnObjAiBaseTeleport;

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            Game.OnWndProc -= OnGameWndProc;
            Drawing.OnEndScene -= OnDrawingEndScene;
            Obj_AI_Base.OnTeleport -= OnObjAiBaseTeleport;

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

                var index = 0;

                var hudWidth = (float) (Math.Ceiling(HudWidth * _scale));
                var hudHeight = (float) (Math.Ceiling(HudHeight * _scale));

                var spacing = (float) (Math.Ceiling(20f * _scale)) + hudHeight;

                var offsetTop = Menu.Item(Menu.Name + "DrawingOffsetTop").GetValue<Slider>().Value + hudHeight / 2;
                var offsetRight = Drawing.Width - Menu.Item(Menu.Name + "DrawingOffsetRight").GetValue<Slider>().Value -
                                  hudWidth / 2;

                foreach (var enemy in _enemyObjects)
                {
                    if (enemy.Unit.IsDead && Game.Time > enemy.DeathEndTime)
                    {
                        enemy.DeathEndTime = Game.Time + enemy.Unit.DeathDuration + 1;
                    }
                    else if (!enemy.Unit.IsDead)
                    {
                        enemy.DeathEndTime = 0;
                    }

                    var offset = spacing * index;

                    if (enemy.TeleportStatus == Packet.S2C.Teleport.Status.Start ||
                        (enemy.TeleportStatus == Packet.S2C.Teleport.Status.Finish ||
                         enemy.TeleportStatus == Packet.S2C.Teleport.Status.Abort) &&
                        Game.Time <= enemy.LastTeleportStatusTime + 5f)
                    {
                        _sprite.Begin(SpriteFlags.AlphaBlend);
                        _sprite.DrawCentered(
                            enemy.TeleportStatus == Packet.S2C.Teleport.Status.Start
                                ? _teleportStartTexture
                                : (enemy.TeleportStatus == Packet.S2C.Teleport.Status.Finish
                                    ? _teleportFinishTexture
                                    : _teleportAbortTexture),
                            new Vector2(
                                offsetRight + (float) (Math.Ceiling(4 * _scale)),
                                offsetTop + (float) (Math.Ceiling(1 * _scale)) + offset));
                        _sprite.End();
                    }
                    var spellData = _spellDatas[enemy.Unit.NetworkId];
                    for (var i = 0; spellData.Count > i; i++)
                    {
                        var spell = spellData[i];
                        if (spell != null && _summonerTextures.ContainsKey(FixSummonerName(spell.Name)))
                        {
                            var teleportCd = 0f;
                            if (spell.Name.Contains("Teleport", StringComparison.OrdinalIgnoreCase) &&
                                _teleports.ContainsKey(enemy.Unit.NetworkId))
                            {
                                _teleports.TryGetValue(enemy.Unit.NetworkId, out teleportCd);
                            }
                            var time = (teleportCd > 0.1f ? teleportCd : spell.CooldownExpires) - Game.Time;
                            _sprite.Begin(SpriteFlags.AlphaBlend);
                            _sprite.DrawCentered(
                                _summonerTextures[FixSummonerName(spell.Name)],
                                new Vector2(
                                    offsetRight - hudWidth * 0.23f,
                                    offsetTop - hudHeight * 0.3f + offset + ((float) (Math.Ceiling(24 * _scale)) * i)));
                            _sprite.End();
                            if (time > 0)
                            {
                                _text13.DrawTextCentered(
                                    ((int) time).ToStringLookUp(),
                                    new Vector2(
                                        offsetRight - hudWidth * 0.23f,
                                        offsetTop - hudHeight * 0.3f + offset +
                                        ((float) (Math.Ceiling(24 * _scale)) * i)), Color.White, true);
                            }
                        }
                    }

                    _sprite.Begin(SpriteFlags.AlphaBlend);

                    _sprite.DrawCentered(
                        enemy.Texture,
                        new Vector2(offsetRight + hudWidth * 0.21f, offsetTop - hudHeight * 0.13f + offset));
                    _sprite.DrawCentered(
                        _hudTexture, new Vector2(offsetRight + (float) (Math.Ceiling(3 * _scale)), offsetTop + offset));

                    if (enemy.RSpell != null && enemy.RSpell.CooldownExpires - Game.Time < 0)
                    {
                        _sprite.DrawCentered(
                            _ultimateTexture,
                            new Vector2(offsetRight + hudWidth * 0.445f, offsetTop - hudHeight * 0.385f + offset));
                    }

                    _sprite.End();

                    if (enemy.RSpell != null && enemy.RSpell.CooldownExpires - Game.Time > 0 &&
                        enemy.RSpell.CooldownExpires - Game.Time < 100)
                    {
                        _text12.DrawTextCentered(
                            ((int) (enemy.RSpell.CooldownExpires - Game.Time)).ToStringLookUp(),
                            new Vector2(offsetRight + hudWidth * 0.4555f, offsetTop - hudHeight * 0.365f + offset),
                            Color.White, true);
                    }

                    _text12.DrawTextCentered(
                        enemy.Unit.Level.ToStringLookUp(),
                        new Vector2(offsetRight + hudWidth * 0.43f, offsetTop + hudHeight * 0.12f + offset), Color.White);

                    if (!Enumerable.Contains(_champsNoEnergy, enemy.Unit.ChampionName))
                    {
                        _line.Draw(
                            new[]
                            {
                                new Vector2(offsetRight - hudWidth * 0.1f, offsetTop + hudHeight * 0.415f + offset),
                                new Vector2(
                                    offsetRight - hudWidth * 0.1f +
                                    (offsetRight + hudWidth * 0.51f - offsetRight - hudWidth * 0.1f) *
                                    (enemy.Unit.ManaPercent * 0.01f), offsetTop + hudHeight * 0.41f + offset)
                            },
                            Enumerable.Contains(_champsEnergy, enemy.Unit.ChampionName)
                                ? Color.Yellow
                                : (Enumerable.Contains(_champsRage, enemy.Unit.ChampionName)
                                    ? Color.DarkRed
                                    : Color.Blue));
                        _text13.DrawTextCentered(
                            (int) (enemy.Unit.Mana) + " / " + (int) (enemy.Unit.MaxMana),
                            new Vector2(offsetRight + hudWidth * 0.21f, offsetTop + hudHeight * 0.425f + offset),
                            Color.White, true);
                    }

                    _line.Draw(
                        new[]
                        {
                            new Vector2(offsetRight - hudWidth * 0.1f, offsetTop + hudHeight * 0.265f + offset),
                            new Vector2(
                                offsetRight - hudWidth * 0.1f +
                                (offsetRight + hudWidth * 0.51f - offsetRight - hudWidth * 0.1f) *
                                (enemy.Unit.HealthPercent * 0.01f), offsetTop + hudHeight * 0.265f + offset)
                        },
                        Color.Green);

                    _text13.DrawTextCentered(
                        (int) (enemy.Unit.Health) + " / " + (int) (enemy.Unit.MaxHealth),
                        new Vector2(offsetRight + hudWidth * 0.21f, offsetTop + hudHeight * 0.275f + offset),
                        Color.White, true);

                    _text18.DrawTextCentered(
                        (enemy.Unit.MinionsKilled /*+ enemy.Unit.NeutralMinionsKilled*/).ToStringLookUp(),
                        new Vector2(offsetRight - hudWidth * 0.275f, offsetTop + hudHeight * 0.24f + offset),
                        Color.White);

                    if (enemy.Unit.IsDead)
                    {
                        _text30.DrawTextCentered(
                            ((int) (enemy.DeathEndTime - Game.Time)).ToStringLookUp(),
                            new Vector2(offsetRight + hudWidth * 0.21f, offsetTop - hudHeight * 0.11f + offset),
                            Color.White, true);
                    }

                    if (!enemy.Unit.IsVisible || enemy.Unit.IsDead)
                    {
                        _sprite.Begin(SpriteFlags.AlphaBlend);
                        _sprite.DrawCentered(_invisibleTexture, new Vector2(offsetRight + 3, offsetTop + 1 + offset));
                        _sprite.End();
                    }

                    index++;
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected override sealed void OnLoad()
        {
            try
            {
                Menu = new Menu(Name, Name);

                var drawingMenu = new Menu("Drawing", Name + "Drawing");

                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "OffsetTop", "Offset Top").SetValue(
                        new Slider(150, 0, Drawing.Height)));

                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "OffsetRight", "Offset Right").SetValue(
                        new Slider(0, 0, Drawing.Width)));

                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + "Scale", "Scale").SetValue(new Slider(10, 5, 15)));

                Menu.AddSubMenu(drawingMenu);
                Menu.AddItem(new MenuItem(Name + "Clickable", "Clickable").SetValue(false));

                Menu.AddItem(new MenuItem(Name + "Enabled", "Enabled").SetValue(false));

                Parent.Menu.AddSubMenu(Menu);

                _scale = Menu.Item(Menu.Name + "DrawingScale").GetValue<Slider>().Value / 10f;

                _text12 = MDrawing.GetFont((int) (Math.Ceiling(12 * _scale)));
                _text13 = MDrawing.GetFont((int) (Math.Ceiling(13 * _scale)));
                _text18 = MDrawing.GetFont((int) (Math.Ceiling(18 * _scale)));
                _text30 = MDrawing.GetFont((int) (Math.Ceiling(30 * _scale)));
                _line = MDrawing.GetLine((int) (Math.Ceiling(9 * _scale)));
                _sprite = MDrawing.GetSprite();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected override void OnInitialize()
        {
            try
            {
                if (!GameObjects.EnemyHeroes.Any())
                {
                    OnUnload(null, new UnloadEventArgs(true));
                    return;
                }

                _hudTexture = Resources.SB_Hud.Scale(_scale).ToTexture();
                _invisibleTexture = Resources.SB_Invisible.Scale(_scale).ToTexture();
                _teleportAbortTexture = Resources.SB_RecallAbort.Scale(_scale).ToTexture();
                _teleportFinishTexture = Resources.SB_RecallFinish.Scale(_scale).ToTexture();
                _teleportStartTexture = Resources.SB_RecallStart.Scale(_scale).ToTexture();
                _ultimateTexture = Resources.SB_Ultimate.Scale(_scale).ToTexture();

                foreach (var enemy in GameObjects.EnemyHeroes)
                {
                    var lEnemy = enemy;
                    _spellDatas.Add(enemy.NetworkId, _summonerSpellSlots.Select(slot => lEnemy.GetSpell(slot)).ToList());
                }

                foreach (var enemy in GameObjects.EnemyHeroes)
                {
                    _enemyObjects.Add(
                        new EnemyObject(
                            enemy,
                            ((ImageLoader.Load("SB", enemy.ChampionName) ?? Resources.SB_Default).Scale(_scale))
                                .ToTexture()));
                }

                foreach (var summonerSlot in _summonerSpellSlots)
                {
                    foreach (var enemy in GameObjects.EnemyHeroes)
                    {
                        var spell = enemy.Spellbook.GetSpell(summonerSlot);
                        if (!_summonerTextures.ContainsKey(FixSummonerName(spell.Name)))
                        {
                            _summonerTextures[FixSummonerName(spell.Name)] =
                                (((Bitmap)
                                    Resources.ResourceManager.GetObject(
                                        string.Format("SB_{0}", FixSummonerName(spell.Name)))) ??
                                 Resources.SB_summonerbarrier).Scale(_scale).ToTexture();
                        }
                    }
                }

                base.OnInitialize();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private string FixSummonerName(string name)
        {
            return name.Contains("Smite", StringComparison.OrdinalIgnoreCase)
                ? "summonersmite"
                : (name.Contains("Teleport", StringComparison.OrdinalIgnoreCase) ? "summonerteleport" : name.ToLower());
        }

        private string ReadableSummonerName(string name)
        {
            name = FixSummonerName(name);
            switch (name)
            {
                case "summonerflash":
                    return "Flash";
                case "summonerdot":
                    return "Ignite";
                case "summonerheal":
                    return "Heal";
                case "summonerteleport":
                    return "Teleport";
                case "summonerexhaust":
                    return "Exhaust";
                case "summonerhaste":
                    return "Ghost";
                case "summonerbarrier":
                    return "Barrier";
                case "summonerboost":
                    return "Cleanse";
                case "summonermana":
                    return "Clarity";
                case "summonerclairvoyance":
                    return "Clairvoyance";
                case "summonerodingarrison":
                    return "Garrison";
                case "summonersnowball":
                    return "Mark";
                case "summonersmite":
                    return "Smite";
            }
            return null;
        }

        private void OnObjAiBaseTeleport(Obj_AI_Base sender, GameObjectTeleportEventArgs args)
        {
            try
            {
                var packet = Packet.S2C.Teleport.Decoded(sender, args);
                var enemyObject = _enemyObjects.FirstOrDefault(e => e.Unit.NetworkId == packet.UnitNetworkId);
                if (enemyObject != null)
                {
                    if (packet.Type == Packet.S2C.Teleport.Type.Teleport &&
                        (packet.Status == Packet.S2C.Teleport.Status.Finish ||
                         packet.Status == Packet.S2C.Teleport.Status.Abort))
                    {
                        var time = Game.Time;
                        Utility.DelayAction.Add(
                            250, delegate
                            {
                                var cd = packet.Status == Packet.S2C.Teleport.Status.Finish
                                    ? (GameObjects.EnemyHeroes.Any(
                                        e =>
                                            e.NetworkId == packet.UnitNetworkId &&
                                            GameObjects.EnemyTurrets.Any(t => e.Distance(t) < 400))
                                        ? 240
                                        : 300)
                                    : 200;
                                _teleports[packet.UnitNetworkId] = time + cd;
                            });
                    }
                    enemyObject.TeleportStatus = packet.Status;
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnGameWndProc(WndEventArgs args)
        {
            try
            {
                if (!Menu.Item(Name + "Clickable").GetValue<bool>())
                {
                    return;
                }

                var index = 0;

                var hudWidth = (float) (Math.Ceiling(HudWidth * _scale));
                var hudHeight = (float) (Math.Ceiling(HudHeight * _scale));

                var spacing = (float) (Math.Ceiling(20f * _scale)) + hudHeight;

                var offsetTop = Menu.Item(Menu.Name + "DrawingOffsetTop").GetValue<Slider>().Value + hudHeight / 2;
                var offsetRight = Drawing.Width - Menu.Item(Menu.Name + "DrawingOffsetRight").GetValue<Slider>().Value -
                                  hudWidth / 2;

                if (args.Msg == (uint) WindowsMessages.WM_RBUTTONUP ||
                    args.Msg == (uint) WindowsMessages.WM_LBUTTONDBLCLCK)
                {
                    var pos = Utils.GetCursorPos();
                    foreach (var enemy in _enemyObjects)
                    {
                        var offset = spacing * index;
                        if (args.Msg == (uint) WindowsMessages.WM_LBUTTONDBLCLCK)
                        {
                            var spellData = _spellDatas[enemy.Unit.NetworkId];
                            for (var i = 0; spellData.Count > i; i++)
                            {
                                var spell = spellData[i];
                                if (spell != null)
                                {
                                    if (Utils.IsUnderRectangle(
                                        pos, offsetRight - hudWidth * 0.23f - SummonerWidth / 2f,
                                        offsetTop - hudHeight * 0.3f + offset +
                                        ((float) (Math.Ceiling(24 * _scale)) * i) - SummonerHeight / 2f, SummonerWidth,
                                        SummonerHeight))
                                    {
                                        var teleportCd = 0f;
                                        if (spell.Name.Contains("Teleport", StringComparison.OrdinalIgnoreCase) &&
                                            _teleports.ContainsKey(enemy.Unit.NetworkId))
                                        {
                                            _teleports.TryGetValue(enemy.Unit.NetworkId, out teleportCd);
                                        }
                                        var time = (teleportCd > 0.1f ? teleportCd : spell.CooldownExpires) - Game.Time;
                                        if (time > 0 && Environment.TickCount > _lastChatSend + 1500)
                                        {
                                            _lastChatSend = Environment.TickCount;
                                            var sName = ReadableSummonerName(spell.Name);
                                            Game.Say(
                                                string.Format(
                                                    "{0} {1} {2}", enemy.Unit.ChampionName, sName,
                                                    ((float) (Math.Round(time * 2f, MidpointRounding.AwayFromZero) / 2f))
                                                        .FormatTime()));
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (enemy.Unit.IsVisible && !enemy.Unit.IsDead &&
                                Utils.IsUnderRectangle(
                                    pos, offsetRight + (float) (Math.Ceiling(3 * _scale)) - hudWidth / 2f,
                                    offsetTop + offset - hudHeight / 2f, hudWidth, hudHeight))
                            {
                                ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, enemy.Unit);
                            }
                        }
                        index++;
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private class EnemyObject
        {
            private Packet.S2C.Teleport.Status _teleportStatus;

            public EnemyObject(Obj_AI_Hero unit, Texture texture)
            {
                TeleportStatus = Packet.S2C.Teleport.Status.Unknown;
                Unit = unit;
                Texture = texture;
                RSpell = unit.GetSpell(SpellSlot.R);
            }

            public Texture Texture { get; private set; }
            public SpellDataInst RSpell { get; private set; }
            public Obj_AI_Hero Unit { get; private set; }
            public float DeathEndTime { get; set; }
            public float LastTeleportStatusTime { get; private set; }

            public Packet.S2C.Teleport.Status TeleportStatus
            {
                get { return _teleportStatus; }
                set
                {
                    _teleportStatus = value;
                    LastTeleportStatusTime = Game.Time;
                }
            }
        }
    }
}