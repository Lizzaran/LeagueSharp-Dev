#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 AutoLeveler.cs is part of SFXUtility.

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

namespace SFXUtility.Features.Events
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Classes;
    using LeagueSharp;
    using LeagueSharp.Common;
    using SFXLibrary.IoCContainer;
    using SFXLibrary.Logger;
    using Utils = SFXLibrary.Utils;

    #endregion

    internal class AutoLeveler : Base
    {
        private Events _parent;

        public AutoLeveler(IContainer container)
            : base(container)
        {
            CustomEvents.Game.OnGameLoad += OnGameLoad;
        }

        public override bool Enabled
        {
            get
            {
                return _parent != null && _parent.Enabled && Menu != null && Menu.Item(Name + "Enabled").GetValue<bool>();
            }
        }

        public override string Name
        {
            get { return "Auto Leveler"; }
        }

        protected override void OnEnable()
        {
            CustomEvents.Unit.OnLevelUp += OnUnitLevelUp;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            CustomEvents.Unit.OnLevelUp -= OnUnitLevelUp;
            base.OnDisable();
        }

        private List<SpellInfoStruct> GetOrderedPriorityList()
        {
            return new List<SpellInfoStruct>
            {
                new SpellInfoStruct(SpellSlot.Q,
                    Menu.Item(Name + ObjectManager.Player.ChampionName + "PatternQ").GetValue<Slider>().Value),
                new SpellInfoStruct(SpellSlot.W,
                    Menu.Item(Name + ObjectManager.Player.ChampionName + "PatternW").GetValue<Slider>().Value),
                new SpellInfoStruct(SpellSlot.E,
                    Menu.Item(Name + ObjectManager.Player.ChampionName + "PatternE").GetValue<Slider>().Value),
                new SpellInfoStruct(SpellSlot.R, 4)
            }.OrderBy(x => x.Value).Reverse().ToList();
        }

        private void OnGameLoad(EventArgs args)
        {
            try
            {
                if (IoC.IsRegistered<Events>())
                {
                    _parent = IoC.Resolve<Events>();
                    if (_parent.Initialized)
                        OnParentLoaded(null, null);
                    else
                        _parent.OnInitialized += OnParentLoaded;
                }
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
            }
        }

        private void OnParentLoaded(object sender, EventArgs eventArgs)
        {
            try
            {
                if (_parent.Menu == null)
                    return;

                Menu = new Menu(Name, BaseName + Name);

                var championMenu = new Menu(ObjectManager.Player.ChampionName,
                    Name + ObjectManager.Player.ChampionName);
                championMenu.AddItem(
                    new MenuItem(Name + "PatternEarly", "Early Pattern").SetValue(new StringList(new[]
                    {
                        "Q W",
                        "Q E",
                        "Q W E",
                        "Q E W",
                        "W Q",
                        "W E",
                        "W Q E",
                        "W E Q",
                        "E Q",
                        "E W",
                        "E Q W",
                        "E W Q"
                    })));
                championMenu.AddItem(new MenuItem(Name + "PatternQ", "Q").SetValue(new Slider(3, 3, 1)));
                championMenu.AddItem(new MenuItem(Name + "PatternW", "W").SetValue(new Slider(1, 3, 1)));
                championMenu.AddItem(new MenuItem(Name + "PatternE", "E").SetValue(new Slider(2, 3, 1)));
                championMenu.AddItem(new MenuItem(Name + "OnlyR", "Only R").SetValue(true));

                Menu.AddSubMenu(championMenu);

                Menu.AddItem(new MenuItem(Name + "Enabled", "Enabled").SetValue(false));

                _parent.Menu.AddSubMenu(Menu);

                HandleEvents(_parent);

                if (ObjectManager.Player.Level == 1)
                    OnUnitLevelUp(ObjectManager.Player,
                        new CustomEvents.Unit.OnLevelUpEventArgs {NewLevel = 1, RemainingPoints = 1});

                RaiseOnInitialized();
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
            }
        }

        private void OnUnitLevelUp(Obj_AI_Base sender, CustomEvents.Unit.OnLevelUpEventArgs args)
        {
            try
            {
                if (!sender.IsValid || !sender.IsMe)
                    return;

                var availablePoints = args.RemainingPoints;

                var splittedPattern =
                    Menu.Item(Name + ObjectManager.Player.ChampionName + "PatternEarly")
                        .GetValue<StringList>()
                        .SelectedValue.Split(' ');
                if (splittedPattern.Length >= args.NewLevel)
                {
                    for (var i = 0; availablePoints > i; i++)
                    {
                        if (availablePoints <= 0)
                            break;

                        var slot = Utils.GetSpellSlotByChar(splittedPattern[args.NewLevel - availablePoints]);
                        if (slot != SpellSlot.Unknown)
                        {
                            ObjectManager.Player.Spellbook.LevelUpSpell(slot);
                        }
                    }
                    return;
                }

                foreach (var pItem in GetOrderedPriorityList())
                {
                    if (availablePoints <= 0)
                        return;

                    var pointsToLevelSlot = MaxSpellLevel(pItem.Slot, args.NewLevel) -
                                            ObjectManager.Player.Spellbook.GetSpell(pItem.Slot).Level;
                    pointsToLevelSlot = pointsToLevelSlot > availablePoints ? availablePoints : pointsToLevelSlot;

                    for (var i = 0; pointsToLevelSlot > i; i++)
                    {
                        ObjectManager.Player.Spellbook.LevelUpSpell(pItem.Slot);
                        availablePoints--;
                    }

                    if (pItem.Slot == SpellSlot.R &&
                        Menu.Item(Name + ObjectManager.Player.ChampionName + "OnlyR").GetValue<bool>())
                        return;
                }
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
            }
        }

        private int MaxSpellLevel(SpellSlot slot, int level)
        {
            if (slot == SpellSlot.R)
            {
                return level >= 16 ? 3 : level >= 11 ? 2 : level >= 6 ? 1 : 0;
            }
            return level >= 9 ? 5 : level >= 7 ? 4 : level >= 5 ? 3 : level >= 3 ? 2 : 1;
        }

        private struct SpellInfoStruct
        {
            public SpellInfoStruct(SpellSlot slot, int value) : this()
            {
                Slot = slot;
                Value = value;
            }

            public SpellSlot Slot { get; private set; }
            public int Value { get; private set; }
        }
    }
}