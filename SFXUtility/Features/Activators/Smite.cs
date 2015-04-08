#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Smite.cs is part of SFXUtility.

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

namespace SFXUtility.Features.Activators
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using Classes;
    using LeagueSharp;
    using LeagueSharp.Common;
    using SFXLibrary;
    using SFXLibrary.Extensions.NET;
    using SFXLibrary.Extensions.SharpDX;
    using SFXLibrary.Logger;
    using Utils = SFXLibrary.Utils;

    #endregion

    internal class Smite : Base
    {
        public const float SmiteRange = 570f;
        private readonly List<Jungle.Camp> _camps = new List<Jungle.Camp>();
        private readonly List<HeroSpell> _heroSpells = new List<HeroSpell>();
        private Obj_AI_Minion _currentMinion;
        private bool _delayActive;
        private HeroSpell _heroSpell;
        private string[] _mobNames = new string[0];
        private Activators _parent;
        private Spell _smiteSpell;

        public override bool Enabled
        {
            get { return !Unloaded && _parent != null && _parent.Enabled && Menu != null && Menu.Item(Name + "Enabled").GetValue<bool>(); }
        }

        public override string Name
        {
            get { return Language.Get("F_Smite"); }
        }

        protected override void OnEnable()
        {
            Game.OnUpdate += OnGameUpdate;
            Drawing.OnDraw += OnDrawingDraw;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            Game.OnUpdate -= OnGameUpdate;
            Drawing.OnDraw -= OnDrawingDraw;
            base.OnDisable();
        }

        private void OnParentInitialized(object sender, EventArgs eventArgs)
        {
            try
            {
                if (_parent.Menu == null)
                    return;

                #region Hero Spells

                _heroSpells.AddRange(new List<HeroSpell>
                {
                    new HeroSpell("Nunu", SpellSlot.Q, TargetSelector.DamageType.True, null, true, 250, false),
                    new HeroSpell("Olaf", SpellSlot.E, TargetSelector.DamageType.True, null, true, 250, false),
                    new HeroSpell("Chogath", SpellSlot.R, TargetSelector.DamageType.True, null, true, 250, false)
                });

                #endregion

                Menu = new Menu(Name, Name);

                var drawingMenu = new Menu(Language.Get("G_Drawing"), Name + "Drawing");
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "UseableColor", Language.Get("G_Useable") + " " + Language.Get("G_Color")).SetValue(Color.Blue));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "UnusableColor", Language.Get("G_Unusable") + " " + Language.Get("G_Color")).SetValue(Color.Gray));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "DamageColor",
                        Language.Get("G_Damage") + " " + Language.Get("G_Indicator") + " " + Language.Get("G_Color")).SetValue(Color.SkyBlue));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "SmiteRange", Language.Get("Smite_Smite") + " " + Language.Get("G_Range")).SetValue(false));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "SpellRange", Language.Get("G_Spell") + " " + Language.Get("G_Range")).SetValue(false));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "DamageIndicator", Language.Get("G_Damage") + " " + Language.Get("G_Indicator")).SetValue(false));

                var spellMenu = new Menu(Language.Get("G_Spell"), Name + "Spell");
                var spellHeroMenu = new Menu(Language.Get("G_Hero"), spellMenu.Name + "Hero");

                spellHeroMenu.AddItem(
                    new MenuItem(spellHeroMenu.Name + "MinHitChance", Language.Get("G_Minimum") + " " + Language.Get("G_HitChance")).SetValue(
                        new StringList(Language.Get("Smite_MinHitChanceList").Split('|'))));

                foreach (var champion in _heroSpells.GroupBy(s => s.ChampionName))
                {
                    var championMenu = new Menu(string.Empty, string.Empty);
                    foreach (var heroSpell in champion)
                    {
                        championMenu.DisplayName = heroSpell.ChampionName;
                        championMenu.Name = spellHeroMenu.Name + heroSpell.ChampionName;
                        championMenu.AddItem(
                            new MenuItem(championMenu.Name + Utils.GetEnumName(heroSpell.Spell.Slot), Utils.GetEnumName(heroSpell.Spell.Slot))
                                .SetValue(false));
                    }
                    spellHeroMenu.AddSubMenu(championMenu);
                }

                spellMenu.AddSubMenu(spellHeroMenu);
                spellMenu.AddItem(new MenuItem(spellMenu.Name + "Smite", Language.Get("G_Use") + " " + Language.Get("Smite_Smite")).SetValue(false));

                Menu.AddSubMenu(drawingMenu);
                Menu.AddSubMenu(spellMenu);

                Menu.AddItem(new MenuItem(Name + "SmallCamps", Language.Get("Smite_SmallCamps")).SetValue(false));

                Menu.AddItem(new MenuItem(Name + "Enabled", Language.Get("G_Enabled")).SetValue(false));

                Menu.Item(Name + "SmallCamps").ValueChanged +=
                    delegate(object o, OnValueChangeEventArgs args)
                    {
                        _mobNames = args.GetNewValue<bool>()
                            ? (from c in _camps from m in c.Mobs.Where(m => m.IsBig) select m.Name).ToArray()
                            : (from c in _camps.Where(c => c.IsBig) from m in c.Mobs.Where(m => m.IsBig) select m.Name).ToArray();
                    };

                _parent.Menu.AddSubMenu(Menu);

                var spell = ObjectManager.Player.Spellbook.Spells.FirstOrDefault(s => s.Name.Contains("Smite", StringComparison.OrdinalIgnoreCase));
                if (spell != null)
                    _smiteSpell = new Spell(spell.Slot, SmiteRange, TargetSelector.DamageType.True);

                var hSpell =
                    _heroSpells.FirstOrDefault(s => s.ChampionName.Equals(ObjectManager.Player.ChampionName, StringComparison.OrdinalIgnoreCase));
                if (hSpell != null)
                    _heroSpell = hSpell;

                _camps.AddRange(Jungle.Camps.Where(c => c.MapType == Utility.Map.GetMap().Type));

                _mobNames = Menu.Item(Name + "SmallCamps").GetValue<bool>()
                    ? (from c in _camps from m in c.Mobs.Where(m => m.IsBig) select m.Name).ToArray()
                    : (from c in _camps.Where(c => c.IsBig) from m in c.Mobs.Where(m => m.IsBig) select m.Name).ToArray();

                if (!_camps.Any() || _smiteSpell == null && _heroSpell == null)
                    return;

                HandleEvents(_parent);
                RaiseOnInitialized();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected override void OnGameLoad(EventArgs args)
        {
            try
            {
                if (Global.IoC.IsRegistered<Activators>())
                {
                    _parent = Global.IoC.Resolve<Activators>();
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

        private void OnDrawingDraw(EventArgs args)
        {
            var useableColor = Menu.Item(Name + "DrawingUseableColor").GetValue<Color>();
            var unusableColor = Menu.Item(Name + "DrawingUnusableColor").GetValue<Color>();

            if (_smiteSpell != null && Menu.Item(Name + "DrawingSmiteRange").GetValue<bool>())
            {
                Render.Circle.DrawCircle(ObjectManager.Player.ServerPosition, SmiteRange,
                    _smiteSpell.IsReady() && _smiteSpell.CanCast(_currentMinion) ? useableColor : unusableColor);
            }
            if (_heroSpell != null && Menu.Item(Name + "DrawingSpellRange").GetValue<bool>())
            {
                Render.Circle.DrawCircle(ObjectManager.Player.ServerPosition, _heroSpell.Spell.Range,
                    _heroSpell.CanCast(_currentMinion,
                        (HitChance) (Menu.Item(Name + "SpellHeroMinHitChance").GetValue<StringList>().SelectedIndex + 3))
                        ? useableColor
                        : unusableColor);
            }
            if (Menu.Item(Name + "DrawingDamageIndicator").GetValue<bool>() && _currentMinion != null && !_currentMinion.IsDead &&
                _currentMinion.IsValid)
            {
                var pos = Drawing.WorldToScreen(_currentMinion.Position);
                Drawing.DrawText(pos.X, pos.Y + _currentMinion.BoundingRadius/2f, Menu.Item(Name + "DrawingDamageColor").GetValue<Color>(),
                    ((int) (_currentMinion.Health - ObjectManager.Player.GetSummonerSpellDamage(_currentMinion, Damage.SummonerSpell.Smite))).ToString
                        ());
            }
        }

        private void OnGameUpdate(EventArgs args)
        {
            try
            {
                if (_delayActive)
                    return;

                var smiteSpell = _smiteSpell != null && Menu.Item(Name + "SpellSmite").GetValue<bool>();
                var heroSpell = _heroSpell != null &&
                                Menu.Item(Name + "SpellHero" + _heroSpell.ChampionName + Utils.GetEnumName(_heroSpell.Spell.Slot)).GetValue<bool>();
                var minHitChance = (HitChance) (Menu.Item(Name + "SpellHeroMinHitChance").GetValue<StringList>().SelectedIndex + 3);

                _currentMinion = ObjectManager.Player.ServerPosition.GetNearestMinionByNames(_mobNames);
                if (_currentMinion != null)
                {
                    double totalDamage = 0;
                    if (smiteSpell && _smiteSpell.CanCast(_currentMinion))
                        totalDamage += ObjectManager.Player.GetSummonerSpellDamage(_currentMinion, Damage.SummonerSpell.Smite);
                    if (heroSpell)
                        totalDamage += _heroSpell.CalculateDamage(_currentMinion, minHitChance);
                    if (totalDamage >= _currentMinion.Health)
                    {
                        if (heroSpell)
                        {
                            _heroSpell.Cast(_currentMinion);
                            if (smiteSpell && _smiteSpell.CanCast(_currentMinion))
                            {
                                _delayActive = true;
                                Utility.DelayAction.Add((int) _heroSpell.CalculateHitDelay(_currentMinion), delegate
                                {
                                    if (_smiteSpell.CanCast(_currentMinion))
                                        _smiteSpell.Cast(_currentMinion);
                                    _delayActive = false;
                                });
                            }
                        }
                        else if (smiteSpell && _smiteSpell.CanCast(_currentMinion))
                            _smiteSpell.Cast(_currentMinion);
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }

    internal class HeroSpell
    {
        private readonly Func<Spell, Obj_AI_Minion, HitChance, double> _calculateDamage;
        private readonly bool _enemyHitbox;

        public HeroSpell(string champName, SpellSlot slot, TargetSelector.DamageType damageType, SkillshotType? skillshotType, bool enemyHitbox,
            float delay, bool collision, float range = float.MaxValue, float speed = float.MaxValue, float width = float.MaxValue,
            Func<Spell, Obj_AI_Minion, HitChance, double> calculateDamage = null)
        {
            ChampionName = champName;
            _enemyHitbox = enemyHitbox;
            _calculateDamage = calculateDamage;
            Spell = new Spell(slot, range, damageType);
            if (ObjectManager.Player.ChampionName.Equals(champName, StringComparison.OrdinalIgnoreCase))
            {
                var spell = ObjectManager.Player.Spellbook.GetSpell(slot);
                if (speed == float.MaxValue)
                    speed = spell.SData.MissileSpeed;
                if (range == float.MaxValue)
                    range = spell.SData.CastRange > spell.SData.CastRangeDisplayOverride + 1000
                        ? spell.SData.CastRangeDisplayOverride
                        : spell.SData.CastRange;
                if (width == float.MaxValue)
                    width = spell.SData.LineWidth;

                Spell.Range = range + ObjectManager.Player.BoundingRadius;

                if (skillshotType == null)
                    Spell.SetTargetted(delay, speed);
                else
                    Spell.SetSkillshot(delay, width, speed, collision, (SkillshotType) skillshotType);
            }
        }

        public string ChampionName { get; set; }
        public Spell Spell { get; set; }

        public double CalculateDamage(Obj_AI_Minion minion, HitChance minChance = HitChance.High)
        {
            if (!Spell.IsReady() || Spell.GetPrediction(minion, false, Spell.Range + (_enemyHitbox ? minion.BoundingRadius : 0)).Hitchance < minChance)
                return 0;
            if (_calculateDamage != null)
            {
                try
                {
                    return _calculateDamage(Spell, minion, minChance);
                }
                catch (Exception ex)
                {
                    Global.Logger.AddItem(new LogItem(ex));
                    return 0;
                }
            }
            return Spell.GetDamage(minion);
        }

        public bool CanCast(Obj_AI_Minion minion, HitChance minChance = HitChance.High)
        {
            return Spell.IsReady() &&
                   Spell.GetPrediction(minion, false, Spell.Range + (_enemyHitbox ? minion.BoundingRadius : 0)).Hitchance >= minChance;
        }

        public void Cast(Obj_AI_Minion minion)
        {
            if (minion == null || Spell == null)
                return;
            Spell.Range += _enemyHitbox ? minion.BoundingRadius : 0;
            Spell.Cast(minion);
            Spell.Range -= _enemyHitbox ? minion.BoundingRadius : 0;
        }

        public float CalculateHitDelay(Obj_AI_Base target)
        {
            return Spell.Delay + (Spell.Speed > 0 ? ((ObjectManager.Player.ServerPosition.Distance(target.ServerPosition)/(Spell.Speed/1000))) : 0) +
                   Game.Ping/2f;
        }
    }
}