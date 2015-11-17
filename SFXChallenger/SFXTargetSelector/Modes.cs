#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Modes.cs is part of SFXChallenger.

 SFXChallenger is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXChallenger is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXChallenger. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

#endregion

namespace SFXChallenger.SFXTargetSelector
{
    public static partial class TargetSelector
    {
        public static partial class Modes
        {
            private const string CustomPostfix = " | c";
            public static readonly List<Item> Items;
            private static Item _current;

            static Modes()
            {
                ProtectedModes = new List<Item>
                {
                    new Item("weights", "Weights", Mode.Weights, Weights.OrderChampions),
                    new Item("priorities", "Priorities", Mode.Priorities, Priorities.OrderChampions)
                };
                Items =
                    ProtectedModes.Union(
                        new List<Item>
                        {
                            new Item(
                                "less-attacks-to-kill", "Less Attacks To Kill", Mode.LessAttacksToKill,
                                list => list.OrderBy(x => x.Hero.Health / ObjectManager.Player.TotalAttackDamage)),
                            new Item(
                                "less-cast-priority", "Less Cast Priority", Mode.LessCastPriority,
                                list => list.OrderBy(x => x.Hero.Health / ObjectManager.Player.TotalMagicalDamage)),
                            new Item(
                                "most-ability-power", "Most Ability Power", Mode.MostAbilityPower,
                                list => list.OrderByDescending(x => x.Hero.TotalMagicalDamage)),
                            new Item(
                                "most-attack-damage", "Most Attack Damage", Mode.MostAttackDamage,
                                list => list.OrderByDescending(x => x.Hero.TotalAttackDamage)),
                            new Item(
                                "closest", "Closest", Mode.Closest,
                                list => list.OrderBy(x => x.Hero.Distance(ObjectManager.Player))),
                            new Item(
                                "near-mouse", "Near Mouse", Mode.NearMouse,
                                list => list.OrderBy(x => x.Hero.Distance(Game.CursorPos))),
                            new Item(
                                "least-health", "Least Health", Mode.Weights, list => list.OrderBy(x => x.Hero.Health))
                        }).ToList();

                Current = Default;
            }

            public static List<Item> ProtectedModes { get; private set; }

            public static Item Default
            {
                get { return ProtectedModes.FirstOrDefault(); }
            }

            public static Item Current
            {
                get { return _current; }
                set
                {
                    var raiseEvent = _current == null || !_current.UniqueName.Equals(value.UniqueName);
                    _current = value;
                    if (raiseEvent)
                    {
                        UpdateModeMenu();
                        Utils.RaiseEvent(OnChange, null, new OnChangeArgs(value));
                    }
                }
            }

            internal static void AddToMainMenu()
            {
                MainMenu.AddItem(
                    new MenuItem(MainMenu.Name + ".mode", "Mode").SetShared()
                        .SetValue(
                            new StringList(
                                Items.Select(
                                    i => i.DisplayName + (i.Mode == Mode.Custom ? CustomPostfix : string.Empty))
                                    .ToArray()))).ValueChanged +=
                    delegate(object sender, OnValueChangeEventArgs args)
                    {
                        Current = GetItemBySelectedIndex(args.GetNewValue<StringList>().SelectedIndex);
                    };

                Current =
                    GetItemBySelectedIndex(MainMenu.Item(MainMenu.Name + ".mode").GetValue<StringList>().SelectedIndex);
            }

            private static Item GetItemBySelectedIndex(int index)
            {
                try
                {
                    if (index < Items.Count && index >= 0)
                    {
                        return Items[index];
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                return Default;
            }

            private static int GetIndexBySelectedItem(Item item)
            {
                try
                {
                    var index = Items.FindIndex(i => i.UniqueName.Equals(item.UniqueName));
                    if (index >= 0)
                    {
                        return index;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                return 0;
            }

            private static void UpdateModeMenu()
            {
                if (MainMenu != null)
                {
                    var item = MainMenu.Item(MainMenu.Name + ".mode");
                    if (item != null)
                    {
                        item.SetShared()
                            .SetValue(
                                new StringList(
                                    Items.Select(
                                        i => i.DisplayName + (i.Mode == Mode.Custom ? CustomPostfix : string.Empty))
                                        .ToArray(), GetIndexBySelectedItem(Current)));
                    }
                }
            }

            public static IEnumerable<Targets.Item> GetOrderedChampions(IEnumerable<Targets.Item> items)
            {
                var targetList = items.ToList();
                try
                {
                    return Current.OrderFunction(targetList);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                return targetList;
            }

            public static event EventHandler<OnChangeArgs> OnChange;

            /// <exception cref="ArgumentException">Unique Name does already exist.</exception>
            /// <exception cref="ArgumentException">Display Name is empty or null.</exception>
            /// <exception cref="ArgumentException">Order Function is null.</exception>
            public static void Register(Item item)
            {
                if (Items.Any(i => i.UniqueName.Equals(item.UniqueName)))
                {
                    throw new ArgumentException(
                        string.Format("Modes: Unique Name \"{0}\" already exist.", item.UniqueName));
                }
                if (string.IsNullOrEmpty(item.DisplayName))
                {
                    throw new ArgumentException(
                        string.Format("Modes: Display Name \"{0}\" can't be empty or null.", item.DisplayName));
                }
                if (item.OrderFunction == null)
                {
                    throw new ArgumentException("Modes: Order Function can't be null.");
                }
                item.Mode = Mode.Custom;
                Items.Add(item);
                UpdateModeMenu();
            }

            /// <exception cref="ArgumentException">Unique Name does not exist.</exception>
            /// <exception cref="ArgumentException">Can't deregister protected mode.</exception>
            public static void Deregister(Item item)
            {
                if (!Items.Any(i => i.UniqueName.Equals(item.UniqueName)))
                {
                    throw new ArgumentException(
                        string.Format("Modes: Unique Name \"{0}\" does not exist.", item.UniqueName));
                }
                if (ProtectedModes.Any(m => m.Equals(item)))
                {
                    throw new ArgumentException(
                        string.Format("Modes: Can't remove \"{0}\", it's procted.", item.UniqueName));
                }
                if (Current.Mode.Equals(item.Mode))
                {
                    Current = Default;
                }
                Items.Remove(item);
            }

            public class Item
            {
                public Item(string uniqueName,
                    string displayName,
                    Mode mode,
                    Func<IEnumerable<Targets.Item>, IEnumerable<Targets.Item>> orderFunction)
                {
                    UniqueName = uniqueName;
                    DisplayName = displayName;
                    Mode = mode;
                    OrderFunction = orderFunction;
                }

                public string UniqueName { get; private set; }
                public string DisplayName { get; private set; }
                public Mode Mode { get; set; }
                public Func<IEnumerable<Targets.Item>, IEnumerable<Targets.Item>> OrderFunction { get; set; }
            }
        }
    }
}