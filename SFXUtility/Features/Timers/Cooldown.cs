#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Cooldown.cs is part of SFXUtility.

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

namespace SFXUtility.Features.Timers
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
    using SFXLibrary.Extensions.SharpDX;
    using SFXLibrary.Logger;
    using SharpDX;
    using SharpDX.Direct3D9;
    using Font = SharpDX.Direct3D9.Font;
    using Rectangle = SharpDX.Rectangle;

    #endregion

    internal class Cooldown : Base
    {
        private readonly List<ManualSpell> _manualSpells = new List<ManualSpell>
        {
            new ManualSpell("Lux", "LuxLightStrikeKugel", SpellSlot.E),
            new ManualSpell("Gragas", "GragasQ", SpellSlot.Q),
            new ManualSpell("Riven", "rivenizunablade", SpellSlot.R),
            new ManualSpell("TwistedFate", "PickACard", SpellSlot.W),
            new ManualSpell("Velkoz", "VelkozQ", SpellSlot.Q),
            new ManualSpell("Xerath", "xeratharcanopulse2", SpellSlot.Q),
            new ManualSpell("Ziggs", "ZiggsW", SpellSlot.W),
            new ManualSpell("Rumble", "RumbleGrenade", SpellSlot.E),
            new ManualSpell("Riven", "RivenTriCleave", SpellSlot.Q),
            new ManualSpell("Zyra", "ZyraSeed", SpellSlot.W),
            new ManualSpell("Velkoz", "VelkozW", SpellSlot.W),
            new ManualSpell("Corki", "MissileBarrage", SpellSlot.R),
            new ManualSpell("Akali", "AkaliShadowDance", SpellSlot.R),
            new ManualSpell("Teemo", "BantamTrap", SpellSlot.R),
            new ManualSpell("Azir", "AzirW", SpellSlot.W)
        };
        private readonly SpellSlot[] _spellSlots = {SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R};
        private readonly SpellSlot[] _summonerSlots = {SpellSlot.Summoner1, SpellSlot.Summoner2};
        private readonly Dictionary<string, Texture> _summonerTextures = new Dictionary<string, Texture>();
        private List<Obj_AI_Hero> _heroes = new List<Obj_AI_Hero>();
        private Texture _hudTexture;
        private Line _line;
        private Timers _parent;
        private Sprite _sprite;
        private Font _text;

        public override bool Enabled
        {
            get { return !Unloaded && _parent != null && _parent.Enabled && Menu != null && Menu.Item(Name + "Enabled").GetValue<bool>(); }
        }

        public override string Name
        {
            get { return Language.Get("F_Cooldown"); }
        }

        protected override void OnEnable()
        {
            Drawing.OnPreReset += OnDrawingPreReset;
            Drawing.OnPostReset += OnDrawingPostReset;
            Drawing.OnEndScene += OnDrawingEndScene;

            Obj_AI_Base.OnProcessSpellCast += OnObjAiBaseProcessSpellCast;

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            Drawing.OnPreReset -= OnDrawingPreReset;
            Drawing.OnPostReset -= OnDrawingPostReset;
            Drawing.OnEndScene -= OnDrawingEndScene;

            Obj_AI_Base.OnProcessSpellCast -= OnObjAiBaseProcessSpellCast;

            OnUnload(null, new UnloadEventArgs());

            base.OnDisable();
        }

        private void OnObjAiBaseProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            var hero = sender as Obj_AI_Hero;
            if (hero != null && !hero.IsMe)
            {
                var data = _manualSpells.FirstOrDefault(m => m.Spell.Equals(args.SData.Name, StringComparison.OrdinalIgnoreCase));
                if (data != null)
                {
                    if (args.SData.MaxAmmo > 0)
                    {
                        var spell = hero.GetSpell(data.Slot);
                        if (spell != null)
                        {
                            if (spell.Ammo == 0)
                            {
                                var cooldown = spell.SData.AmmoRechargeTimeArray.FirstOrDefault(s => s > 0);
                                cooldown = spell.SData.AmmoNotAffectedByCDR ? cooldown : (cooldown - (cooldown / 100 * (hero.PercentCooldownMod * -1 * 100)));
                                data.Cooldown = cooldown;
                                data.CooldownExpires = spell.AmmoRechargeStart;
                            }
                            else
                            {
                                data.Cooldown = args.SData.Cooldown - (args.SData.Cooldown / 100 * (hero.PercentCooldownMod * -1 * 100));
                                data.CooldownExpires = Game.Time + data.Cooldown;
                            }
                        }
                    }
                    else if (data.CooldownExpires - Game.Time < 0.5)
                    {
                        data.Cooldown = args.SData.Cooldown - (args.SData.Cooldown / 100 * (hero.PercentCooldownMod * -1 * 100));
                        data.CooldownExpires = Game.Time + data.Cooldown;
                    }
                }
            }
        }

        protected override void OnUnload(object sender, UnloadEventArgs args)
        {
            if (args != null && args.Final)
                base.OnUnload(sender, args);

            if (Initialized)
            {
                OnDrawingPreReset(null);
                OnDrawingPostReset(null);
            }
        }

        protected override void OnGameLoad(EventArgs args)
        {
            try
            {
                if (Global.IoC.IsRegistered<Timers>())
                {
                    _parent = Global.IoC.Resolve<Timers>();
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
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + "Enemy", Language.Get("G_Enemy")).SetValue(false));
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + "Ally", Language.Get("G_Ally")).SetValue(false));

                Menu.AddSubMenu(drawingMenu);

                Menu.AddItem(new MenuItem(Name + "Enabled", Language.Get("G_Enabled")).SetValue(false));

                Menu.Item(Name + "DrawingEnemy").ValueChanged += delegate(object o, OnValueChangeEventArgs args)
                {
                    var ally = Menu.Item(Name + "DrawingAlly").GetValue<bool>();
                    var enemy = args.GetNewValue<bool>();
                    _heroes = ally && enemy
                        ? HeroManager.AllHeroes
                        : (ally ? HeroManager.Allies : (enemy ? HeroManager.Enemies : new List<Obj_AI_Hero>()));
                };

                Menu.Item(Name + "DrawingAlly").ValueChanged += delegate(object o, OnValueChangeEventArgs args)
                {
                    var ally = args.GetNewValue<bool>();
                    var enemy = Menu.Item(Name + "DrawingEnemy").GetValue<bool>();
                    _heroes = ally && enemy
                        ? HeroManager.AllHeroes
                        : (ally ? HeroManager.Allies : (enemy ? HeroManager.Enemies : new List<Obj_AI_Hero>()));
                };

                _parent.Menu.AddSubMenu(Menu);

                foreach (var sName in
                    HeroManager.AllHeroes.Where(h => !h.IsMe)
                        .SelectMany(
                            h =>
                                _summonerSlots.Select(summoner => h.Spellbook.GetSpell(summoner).Name.ToLower())
                                    .Where(sName => !_summonerTextures.ContainsKey(FixSummonerName(sName)))))
                {
                    _summonerTextures[FixSummonerName(sName)] =
                        ((Bitmap) Resources.ResourceManager.GetObject(string.Format("CD_{0}", FixSummonerName(sName))) ?? Resources.CD_summonerbarrier)
                            .ToTexture();
                }

                _sprite = new Sprite(Drawing.Direct3DDevice);
                _hudTexture = Resources.CD_Hud.ToTexture();
                _line = new Line(Drawing.Direct3DDevice) {Width = 4};
                _text = new Font(Drawing.Direct3DDevice,
                    new FontDescription
                    {
                        FaceName = Global.DefaultFont,
                        Height = 13,
                        OutputPrecision = FontPrecision.Default,
                        Quality = FontQuality.Default
                    });

                _heroes = Menu.Item(Name + "DrawingAlly").GetValue<bool>() && Menu.Item(Name + "DrawingEnemy").GetValue<bool>()
                    ? HeroManager.AllHeroes
                    : (Menu.Item(Name + "DrawingAlly").GetValue<bool>()
                        ? HeroManager.Allies
                        : (Menu.Item(Name + "DrawingEnemy").GetValue<bool>() ? HeroManager.Enemies : new List<Obj_AI_Hero>()));

                HandleEvents(_parent);
                RaiseOnInitialized();
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

        private void OnDrawingEndScene(EventArgs args)
        {
            try
            {
                if (Drawing.Direct3DDevice == null || Drawing.Direct3DDevice.IsDisposed)
                    return;

                foreach (var hero in
                    _heroes.Where(hero => hero != null && hero.IsValid && !hero.IsMe && hero.IsHPBarRendered && hero.Position.IsOnScreen()))
                {
                    try
                    {
                        if (!hero.Position.IsValid() || !hero.HPBarPosition.IsValid())
                            return;

                        var x = (int) hero.HPBarPosition.X - 8;
                        var y = (int) hero.HPBarPosition.Y + (hero.IsEnemy ? 17 : 14);

                        _sprite.Begin(SpriteFlags.AlphaBlend);

                        for (var i = 0; i < _summonerSlots.Length; i++)
                        {
                            var spell = hero.Spellbook.GetSpell(_summonerSlots[i]);
                            if (spell != null)
                            {
                                var t = spell.CooldownExpires - Game.Time;
                                var percent = (Math.Abs(spell.Cooldown) > float.Epsilon) ? t/spell.Cooldown : 1f;
                                var n = (t > 0) ? (int) (19*(1f - percent)) : 19;
                                var ts = TimeSpan.FromSeconds((int) t);
                                var s = t > 60 ? string.Format("{0}:{1:D2}", ts.Minutes, ts.Seconds) : string.Format("{0:0}", t);
                                if (t > 0)
                                {
                                    _text.DrawTextCentered(s, x - 5, y + 7 + 13*i, new ColorBGRA(255, 255, 255, 255));
                                }
                                if (_summonerTextures.ContainsKey(FixSummonerName(spell.Name)))
                                {
                                    _sprite.Draw(_summonerTextures[FixSummonerName(spell.Name)], new ColorBGRA(255, 255, 255, 255),
                                        new Rectangle(0, 12*n, 12, 12), new Vector3(-x - 3, -y - 1 - 13*i, 0));
                                }
                            }
                        }

                        _sprite.Draw(_hudTexture, new ColorBGRA(255, 255, 255, 255), null, new Vector3(-x, -y, 0));

                        _sprite.End();

                        var x2 = x + 19;
                        var y2 = y + 21;

                        _line.Begin();
                        foreach (var slot in _spellSlots)
                        {
                            var spell = hero.Spellbook.GetSpell(slot);
                            if (spell != null)
                            {
                                var manual = _manualSpells.FirstOrDefault(m => m.Slot.Equals(slot) && m.Champ.Equals(hero.ChampionName, StringComparison.OrdinalIgnoreCase));
                                var t = (manual != null ? manual.CooldownExpires : spell.CooldownExpires) - Game.Time;
                                var spellCooldown = manual != null ? manual.Cooldown : spell.Cooldown;
                                var percent = (t > 0 && Math.Abs(spellCooldown) > float.Epsilon) ? 1f - (t / spellCooldown) : 1f;
                                if (t > 0 && t < 100)
                                {
                                    var s = string.Format(t < 1f ? "{0:0.0}" : "{0:0}", t);
                                    _text.DrawTextCentered(s, x2 + 23/2, y2 + 13, new ColorBGRA(255, 255, 255, 255));
                                }
                                if (hero.Spellbook.CanUseSpell(slot) != SpellState.NotLearned)
                                {
                                    _line.Draw(new[] {new Vector2(x2, y2), new Vector2(x2 + percent*23, y2)},
                                        (t > 0) ? new ColorBGRA(235, 137, 0, 255) : new ColorBGRA(0, 168, 25, 255));
                                }
                                x2 = x2 + 27;
                            }
                        }
                        _line.End();
                    }
                    catch (Exception ex)
                    {
                        Global.Logger.AddItem(new LogItem(ex));
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnDrawingPostReset(EventArgs args)
        {
            try
            {
                _line.OnResetDevice();
                _text.OnResetDevice();
                _sprite.OnResetDevice();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnDrawingPreReset(EventArgs args)
        {
            try
            {
                _line.OnLostDevice();
                _text.OnLostDevice();
                _sprite.OnLostDevice();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }

    internal class ManualSpell
    {
        public string Champ { get; private set; }
        public string Spell { get; private set; }
        public SpellSlot Slot { get; private set; }
        public float Cooldown { get; set; }
        public float CooldownExpires { get; set; }

        public ManualSpell(string champ, string spell, SpellSlot slot)
        {
            Champ = champ;
            Spell = spell;
            Slot = slot;
        }
    }
}