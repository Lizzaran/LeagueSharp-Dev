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
    using Others;
    using SFXLibrary;
    using SFXLibrary.IoCContainer;
    using SFXLibrary.Logger;

    #endregion

    internal class AutoLeveler : Base
    {
        private Events _events;

        public AutoLeveler(IContainer container)
            : base(container)
        {
            CustomEvents.Game.OnGameLoad += OnGameLoad;
        }

        public override bool Enabled
        {
            get
            {
                return _events != null && _events.Enabled && Menu != null && Menu.Item(Name + "Enabled").GetValue<bool>();
            }
        }

        public override string Name
        {
            get { return "Auto Leveler"; }
        }

        private SpellInfoStruct GetMenuInfoByPriority(int priority)
        {
            return new List<SpellInfoStruct>
            {
                new SpellInfoStruct
                {
                    Slot = SpellSlot.Q,
                    Value = Menu.Item(Name + "PatternQ").GetValue<Slider>().Value
                },
                new SpellInfoStruct
                {
                    Slot = SpellSlot.W,
                    Value = Menu.Item(Name + "PatternW").GetValue<Slider>().Value
                },
                new SpellInfoStruct
                {
                    Slot = SpellSlot.E,
                    Value = Menu.Item(Name + "PatternE").GetValue<Slider>().Value
                }
            }.OrderBy(x => x.Value).Reverse().First(s => s.Value == priority);
        }

        private List<SpellInfoStruct> GetOrderedList()
        {
            return new List<SpellInfoStruct>
            {
                new SpellInfoStruct
                {
                    Slot = SpellSlot.Q,
                    Value = Menu.Item(Name + "PatternQ").GetValue<Slider>().Value
                },
                new SpellInfoStruct
                {
                    Slot = SpellSlot.W,
                    Value = Menu.Item(Name + "PatternW").GetValue<Slider>().Value
                },
                new SpellInfoStruct
                {
                    Slot = SpellSlot.E,
                    Value = Menu.Item(Name + "PatternE").GetValue<Slider>().Value
                }
            }.OrderBy(x => x.Value).Reverse().ToList();
        }

        private void OnGameLoad(EventArgs args)
        {
            try
            {
                if (IoC.IsRegistered<Events>() && IoC.Resolve<Events>().Initialized)
                {
                    EventsLoaded(IoC.Resolve<Others>());
                }
                else
                {
                    if (IoC.IsRegistered<Mediator>())
                    {
                        IoC.Resolve<Mediator>().Register("Events_initialized", EventsLoaded);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
            }
        }

        private void EventsLoaded(object o)
        {
            try
            {
                var events = o as Events;
                if (events != null && events.Menu != null)
                {
                    _events = events;

                    Menu = new Menu(Name, Name);

                    var patternMenu = new Menu("Pattern", Name + "Pattern");
                    patternMenu.AddItem(
                        new MenuItem(Name + "PatternEarly", "Early Pattern").SetValue(new StringList(new[]
                        {
                            "x 2 3 1",
                            "x 2 1",
                            "x 1 3",
                            "x 1 2"
                        })));
                    patternMenu.AddItem(new MenuItem(Name + "PatternQ", "Q").SetValue(new Slider(3, 3, 1)));
                    patternMenu.AddItem(new MenuItem(Name + "PatternW", "W").SetValue(new Slider(1, 3, 1)));
                    patternMenu.AddItem(new MenuItem(Name + "PatternE", "E").SetValue(new Slider(2, 3, 1)));

                    Menu.AddSubMenu(patternMenu);

                    Menu.AddItem(new MenuItem(Name + "OnlyR", "Only R").SetValue(false));
                    Menu.AddItem(new MenuItem(Name + "Enabled", "Enabled").SetValue(false));

                    _events.Menu.AddSubMenu(Menu);

                    _events.Menu.Item(_events.Name + "Enabled").ValueChanged +=
                        delegate(object sender, OnValueChangeEventArgs args)
                        {
                            if (args.GetNewValue<bool>())
                            {
                                if (Menu != null && Menu.Item(Name + "Enabled").GetValue<bool>())
                                {
                                    CustomEvents.Unit.OnLevelUp += OnLevelUp;
                                }
                            }
                            else
                            {
                                CustomEvents.Unit.OnLevelUp -= OnLevelUp;
                            }
                        };

                    Menu.Item(Name + "Enabled").ValueChanged +=
                        delegate(object sender, OnValueChangeEventArgs args)
                        {
                            if (args.GetNewValue<bool>())
                            {
                                if (_events != null && _events.Enabled)
                                {
                                    CustomEvents.Unit.OnLevelUp += OnLevelUp;
                                }
                            }
                            else
                            {
                                CustomEvents.Unit.OnLevelUp -= OnLevelUp;
                            }
                        };

                    if (Enabled)
                    {
                        CustomEvents.Unit.OnLevelUp += OnLevelUp;
                    }

                    Initialized = true;
                }
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
            }
        }

        private void OnLevelUp(Obj_AI_Base sender, CustomEvents.Unit.OnLevelUpEventArgs args)
        {
            try
            {
                if (!sender.IsValid || !sender.IsMe)
                    return;

                var map = Utility.Map.GetMap().Type;
                var points = args.RemainingPoints;

                if ((map == Utility.Map.MapType.SummonersRift || map == Utility.Map.MapType.TwistedTreeline) &&
                    args.NewLevel <= 1)
                    return;

                if ((map == Utility.Map.MapType.CrystalScar || map == Utility.Map.MapType.HowlingAbyss) &&
                    args.NewLevel <= 3)
                    return;

                if (args.NewLevel == 6 || args.NewLevel == 11 || args.NewLevel == 16)
                {
                    ObjectManager.Player.Spellbook.LevelUpSpell(SpellSlot.R);
                    points--;
                }

                if (Menu.Item(Name + "OnlyR").GetValue<bool>())
                    return;

                var patternIndex = Menu.Item(Name + "PatternEarly").GetValue<StringList>().SelectedIndex;
                SpellInfoStruct mf = default(SpellInfoStruct);
                switch (args.NewLevel)
                {
                    case 2:
                        switch (patternIndex)
                        {
                            case 0:
                            case 1:
                                mf = GetMenuInfoByPriority(2);
                                break;

                            case 2:
                            case 3:
                                mf = GetMenuInfoByPriority(1);
                                break;
                        }
                        break;

                    case 3:
                        switch (patternIndex)
                        {
                            case 0:
                            case 2:
                                mf = GetMenuInfoByPriority(3);
                                break;

                            case 1:
                                mf = GetMenuInfoByPriority(1);
                                break;

                            case 3:
                                mf = GetMenuInfoByPriority(2);
                                break;
                        }
                        break;

                    case 4:
                        switch (patternIndex)
                        {
                            case 0:
                                mf = GetMenuInfoByPriority(1);
                                break;
                        }
                        break;
                }
                if (!mf.Equals(default(SpellInfoStruct)) && points > 0)
                {
                    ObjectManager.Player.Spellbook.LevelUpSpell(mf.Slot);
                    points--;
                }
                foreach (var mi in GetOrderedList())
                {
                    if (points > 0)
                    {
                        ObjectManager.Player.Spellbook.LevelUpSpell(mi.Slot);
                        points--;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
            }
        }

        private struct SpellInfoStruct
        {
            public SpellSlot Slot { get; set; }
            public int Value { get; set; }
        }
    }
}