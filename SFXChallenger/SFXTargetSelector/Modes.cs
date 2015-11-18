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
using System.Collections.ObjectModel;
using System.Linq;
using System.Security;
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
                var protectedModes = new List<Item>
                {
                    new Item("weights", "Weights", Weights.OrderChampions) { Mode = Mode.Weights },
                    new Item("priorities", "Priorities", Priorities.OrderChampions) { Mode = Mode.Priorities }
                };

                Items =
                    protectedModes.Union(
                        new List<Item>
                        {
                            new Item(
                                "less-attacks-to-kill", "Less Attacks To Kill",
                                list => list.OrderBy(x => x.Hero.Health / ObjectManager.Player.TotalAttackDamage))
                            {
                                Mode = Mode.LessAttacksToKill
                            },
                            new Item(
                                "less-cast-priority", "Less Cast Priority",
                                list => list.OrderBy(x => x.Hero.Health / ObjectManager.Player.TotalMagicalDamage))
                            {
                                Mode = Mode.LessCastPriority
                            },
                            new Item(
                                "most-ability-power", "Most Ability Power",
                                list => list.OrderByDescending(x => x.Hero.TotalMagicalDamage))
                            {
                                Mode = Mode.MostAbilityPower
                            },
                            new Item(
                                "most-attack-damage", "Most Attack Damage",
                                list => list.OrderByDescending(x => x.Hero.TotalAttackDamage))
                            {
                                Mode = Mode.MostAttackDamage
                            },
                            new Item(
                                "closest", "Closest", list => list.OrderBy(x => x.Hero.Distance(ObjectManager.Player)))
                            {
                                Mode = Mode.Closest
                            },
                            new Item(
                                "near-mouse", "Near Mouse", list => list.OrderBy(x => x.Hero.Distance(Game.CursorPos)))
                            {
                                Mode = Mode.NearMouse
                            },
                            new Item("least-health", "Least Health", list => list.OrderBy(x => x.Hero.Health))
                            {
                                Mode = Mode.LeastHealth
                            }
                        }).ToList();

                ProtectedModes = protectedModes.AsReadOnly();

                Current = Default;
            }

            public static ReadOnlyCollection<Item> ProtectedModes { get; private set; }

            public static Item Default
            {
                get { return ProtectedModes.FirstOrDefault(); }
            }

            /// <exception cref="ArgumentException" accessor="set">Mode doesn't exist.</exception>
            public static Item Current
            {
                get { return _current; }
                set
                {
                    if (!Items.Any(i => i.UniqueName.Equals(value.UniqueName)))
                    {
                        throw new ArgumentException(string.Format("Modes: \"{0}\" doesn't exist.", value.UniqueName));
                    }
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

            public static Item GetItem(string name, StringComparison comp = StringComparison.OrdinalIgnoreCase)
            {
                return Items.FirstOrDefault(w => w.UniqueName.Equals(name, comp));
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
            /// <exception cref="SecurityException">Can't edit protected mode.</exception>
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
            /// <exception cref="SecurityException">Can't deregister protected mode.</exception>
            public static void Deregister(Item item)
            {
                if (!Items.Any(i => i.UniqueName.Equals(item.UniqueName)))
                {
                    throw new ArgumentException(
                        string.Format("Modes: Unique Name \"{0}\" does not exist.", item.UniqueName));
                }
                if (ProtectedModes.Any(m => m.Equals(item)))
                {
                    throw new SecurityException(
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
                private Mode _mode;
                private Func<IEnumerable<Targets.Item>, IEnumerable<Targets.Item>> _orderFunction;

                public Item(string uniqueName,
                    string displayName,
                    Func<IEnumerable<Targets.Item>, IEnumerable<Targets.Item>> orderFunction)
                {
                    UniqueName = uniqueName;
                    DisplayName = displayName;
                    _mode = Mode.Custom;
                    _orderFunction = orderFunction;
                }

                public string UniqueName { get; private set; }
                public string DisplayName { get; private set; }

                /// <exception cref="SecurityException">Can't edit protected mode.</exception>
                public Mode Mode
                {
                    get { return _mode; }
                    set
                    {
                        if (IsProtected)
                        {
                            throw new SecurityException(
                                string.Format("Modes: Can't edit \"{0}\", it's procted.", UniqueName));
                        }
                        _mode = value;
                    }
                }

                /// <exception cref="SecurityException">Can't edit protected mode.</exception>
                public Func<IEnumerable<Targets.Item>, IEnumerable<Targets.Item>> OrderFunction
                {
                    get { return _orderFunction; }
                    set
                    {
                        if (IsProtected)
                        {
                            throw new SecurityException(
                                string.Format("Modes: Can't edit \"{0}\", it's procted.", UniqueName));
                        }
                        _orderFunction = value;
                    }
                }

                public bool IsProtected
                {
                    get { return ProtectedModes != null && ProtectedModes.Contains(this); }
                }
            }
        }
    }
}