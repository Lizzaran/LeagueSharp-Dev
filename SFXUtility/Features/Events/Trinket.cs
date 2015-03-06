#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Trinket.cs is part of SFXUtility.

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
    using SFXLibrary.Extensions.LeagueSharp;
    using SFXLibrary.Extensions.NET;
    using SFXLibrary.IoCContainer;

    #endregion

    internal class Trinket : Base
    {
        private const float CheckInterval = 125f;
        private Events _events;
        private float _lastCheck = Environment.TickCount;

        public Trinket(IContainer container) : base(container)
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
            get { return "Trinket"; }
        }

        private void OnGameLoad(EventArgs args)
        {
            try
            {
                Logger.Prefix = string.Format("{0} - {1}", BaseName, Name);

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
                Logger.WriteBlock(ex);
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

                    var timersMenu = new Menu("Timers", Name + "Timers");
                    timersMenu.AddItem(
                        new MenuItem(Name + "TimersWardingTotem", "Warding Totem @ Minute").SetValue(new Slider(0, 0, 60)));
                    timersMenu.AddItem(
                        new MenuItem(Name + "TimersSweepingLens", "Sweeping Lens @ Minute").SetValue(new Slider(20, 0,
                            60)));
                    timersMenu.AddItem(
                        new MenuItem(Name + "TimersScryingOrb", "Scrying Orb @ Minute").SetValue(new Slider(45, 0, 60)));
                    timersMenu.AddItem(
                        new MenuItem(Name + "TimersWardingTotemEnabled", "Buy Warding Totem").SetValue(true));
                    timersMenu.AddItem(
                        new MenuItem(Name + "TimersSweepingLensEnabled", "Buy Sweeping Lens").SetValue(true));
                    timersMenu.AddItem(new MenuItem(Name + "TimersScryingOrbEnabled", "Buy Scrying Orb").SetValue(false));
                    timersMenu.AddItem(new MenuItem(Name + "TimersEnabled", "Enabled").SetValue(true));

                    var eventsMenu = new Menu("Events", Name + "Events");
                    eventsMenu.AddItem(new MenuItem(Name + "EventsSightstone", "Sightstone").SetValue(true));
                    eventsMenu.AddItem(new MenuItem(Name + "EventsRubySightstone", "Ruby Sightstone").SetValue(true));
                    eventsMenu.AddItem(new MenuItem(Name + "EventsWrigglesLantern", "Wriggle's Lantern").SetValue(true));
                    eventsMenu.AddItem(
                        new MenuItem(Name + "EventsBuyTrinket", "Buy Trinket").SetValue(
                            new StringList(new[] {"Yellow", "Red", "Blue"})));
                    eventsMenu.AddItem(new MenuItem(Name + "EventsEnabled", "Enabled").SetValue(true));

                    Menu.AddSubMenu(timersMenu);
                    Menu.AddSubMenu(eventsMenu);

                    Menu.AddItem(new MenuItem(Name + "SellUpgraded", "Sell Upgraded").SetValue(false));
                    Menu.AddItem(new MenuItem(Name + "Enabled", "Enabled").SetValue(true));

                    _events.Menu.AddSubMenu(Menu);

                    _events.Menu.Item(_events.Name + "Enabled").ValueChanged +=
                        delegate(object sender, OnValueChangeEventArgs args)
                        {
                            if (args.GetNewValue<bool>())
                            {
                                if (Menu != null && Menu.Item(Name + "Enabled").GetValue<bool>())
                                {
                                    Game.OnGameUpdate += OnGameUpdate;
                                }
                            }
                            else
                            {
                                Game.OnGameUpdate -= OnGameUpdate;
                            }
                        };

                    Menu.Item(Name + "Enabled").ValueChanged +=
                        delegate(object sender, OnValueChangeEventArgs args)
                        {
                            if (args.GetNewValue<bool>())
                            {
                                if (_events != null && _events.Enabled)
                                {
                                    Game.OnGameUpdate += OnGameUpdate;
                                }
                            }
                            else
                            {
                                Game.OnGameUpdate -= OnGameUpdate;
                            }
                        };

                    if (Enabled)
                    {
                        Game.OnGameUpdate += OnGameUpdate;
                    }

                    Initialized = true;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteBlock(ex);
            }
        }

        private void OnGameUpdate(EventArgs args)
        {
            try
            {
                if (_lastCheck + CheckInterval > Environment.TickCount)
                    return;

                _lastCheck = Environment.TickCount;
                if (ObjectManager.Player.IsDead || ObjectManager.Player.InShop())
                {
                    if (!Menu.Item(Name + "SellUpgraded").GetValue<bool>())
                    {
                        if (ObjectManager.Player.HasItem(ItemId.Greater_Vision_Totem_Trinket) ||
                            ObjectManager.Player.HasItem(ItemId.Greater_Stealth_Totem_Trinket) ||
                            ObjectManager.Player.HasItem(ItemId.Farsight_Orb_Trinket) ||
                            ObjectManager.Player.HasItem(ItemId.Oracles_Lens_Trinket))
                            return;
                    }

                    var hasYellow = ObjectManager.Player.HasItem(ItemId.Warding_Totem_Trinket) ||
                                    ObjectManager.Player.HasItem(ItemId.Greater_Vision_Totem_Trinket) ||
                                    ObjectManager.Player.HasItem(ItemId.Greater_Stealth_Totem_Trinket);
                    var hasBlue = ObjectManager.Player.HasItem(ItemId.Scrying_Orb_Trinket) ||
                                  ObjectManager.Player.HasItem(ItemId.Farsight_Orb_Trinket);
                    var hasRed = ObjectManager.Player.HasItem(ItemId.Sweeping_Lens_Trinket) ||
                                 ObjectManager.Player.HasItem(ItemId.Oracles_Lens_Trinket);

                    if (Menu.Item(Name + "EventsEnabled").GetValue<bool>())
                    {
                        bool hasTrinket;
                        var trinketId = -1;
                        switch (Menu.Item(Name + "EventsBuyTrinket").GetValue<StringList>().SelectedIndex)
                        {
                            case 0:
                                hasTrinket = hasYellow;
                                trinketId = (int) ItemId.Warding_Totem_Trinket;
                                break;

                            case 1:
                                hasTrinket = hasRed;
                                trinketId = (int) ItemId.Sweeping_Lens_Trinket;
                                break;

                            case 2:
                                hasTrinket = hasBlue;
                                trinketId = (int) ItemId.Scrying_Orb_Trinket;
                                break;

                            default:
                                hasTrinket = true;
                                break;
                        }

                        if (!hasTrinket && ObjectManager.Player.HasItem(ItemId.Sightstone) &&
                            Menu.Item(Name + "EventsSightstone").GetValue<bool>())
                        {
                            SwitchTrinket(trinketId);
                        }
                        if (!hasTrinket && ObjectManager.Player.HasItem(ItemId.Ruby_Sightstone) &&
                            Menu.Item(Name + "EventsRubySightstone").GetValue<bool>())
                        {
                            SwitchTrinket(trinketId);
                        }
                        if (!hasTrinket && ObjectManager.Player.HasItem(ItemId.Wriggles_Lantern) &&
                            Menu.Item(Name + "EventsWrigglesLantern").GetValue<bool>())
                        {
                            SwitchTrinket(trinketId);
                        }
                    }

                    if (Menu.Item(Name + "TimersEnabled").GetValue<bool>())
                    {
                        var time = Math.Floor(Game.Time/60f);
                        var tsList = new List<TrinketStruct>
                        {
                            new TrinketStruct
                            {
                                ItemId = ItemId.Warding_Totem_Trinket,
                                Time = Menu.Item(Name + "TimersWardingTotem").GetValue<Slider>().Value,
                                Buy = Menu.Item(Name + "TimersWardingTotemEnabled").GetValue<bool>(),
                                HasItem = hasYellow
                            },
                            new TrinketStruct
                            {
                                ItemId = ItemId.Sweeping_Lens_Trinket,
                                Time = Menu.Item(Name + "TimersSweepingLens").GetValue<Slider>().Value,
                                Buy = Menu.Item(Name + "TimersSweepingLensEnabled").GetValue<bool>(),
                                HasItem = hasRed
                            },
                            new TrinketStruct
                            {
                                ItemId = ItemId.Scrying_Orb_Trinket,
                                Time = Menu.Item(Name + "TimersScryingOrb").GetValue<Slider>().Value,
                                Buy = Menu.Item(Name + "TimersScryingOrbEnabled").GetValue<bool>(),
                                HasItem = hasBlue
                            }
                        };
                        tsList = tsList.OrderBy(ts => ts.Time).ToList();

                        for (int i = 0, l = tsList.Count; i < l; i++)
                        {
                            if (time >= tsList[i].Time)
                            {
                                var hasHigher = false;
                                if (i != l - 1)
                                {
                                    for (var j = i + 1; j < l; j++)
                                    {
                                        if (time >= tsList[j].Time && tsList[j].Buy)
                                            hasHigher = true;
                                    }
                                }
                                if (!hasHigher && tsList[i].Buy && !tsList[i].HasItem)
                                {
                                    SwitchTrinket((int) tsList[i].ItemId);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteBlock(ex);
            }
        }

        private void SwitchTrinket(int itemId)
        {
            try
            {
                if (itemId == 0)
                    return;

                var iItem = ObjectManager.Player.InventoryItems.FirstOrDefault(
                    slot =>
                        slot.Name.Contains("Trinket", StringComparison.OrdinalIgnoreCase) ||
                        slot.DisplayName.Contains("Trinket", StringComparison.OrdinalIgnoreCase));
                if (iItem != null)
                {
                    ObjectManager.Player.SellItem(iItem.Slot);
                }
                ObjectManager.Player.BuyItem((ItemId) itemId);
            }
            catch (Exception ex)
            {
                Logger.WriteBlock(ex);
            }
        }

        private struct TrinketStruct
        {
            public bool Buy { get; set; }
            public bool HasItem { get; set; }
            public ItemId ItemId { get; set; }
            public int Time { get; set; }
        }
    }
}