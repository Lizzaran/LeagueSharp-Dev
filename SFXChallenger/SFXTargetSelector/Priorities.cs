#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Priorities.cs is part of SFXChallenger.

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
using SFXChallenger.Library.Logger;

#endregion

namespace SFXChallenger.SFXTargetSelector
{
    public static class Priorities
    {
        public enum Priority
        {
            Highest = 4,
            High = 3,
            Medium = 2,
            Low = 1
        }

        public const int MinPriority = 1;
        public const int MaxPriority = 5;
        private static Menu _mainMenu;

        static Priorities()
        {
            try
            {
                Items = new HashSet<Item>
                {
                    new Item
                    {
                        Champions =
                            new[]
                            {
                                "Ahri", "Anivia", "Annie", "Ashe", "Azir", "Brand", "Caitlyn", "Cassiopeia", "Corki",
                                "Draven", "Ezreal", "Graves", "Jinx", "Kalista", "Karma", "Karthus", "Katarina",
                                "Kennen", "KogMaw", "Leblanc", "Lucian", "Lux", "Malzahar", "MasterYi", "MissFortune",
                                "Orianna", "Quinn", "Sivir", "Syndra", "Talon", "Teemo", "Tristana", "TwistedFate",
                                "Twitch", "Varus", "Vayne", "Veigar", "VelKoz", "Viktor", "Xerath", "Zed", "Ziggs"
                            },
                        Priority = Priority.Highest
                    },
                    new Item
                    {
                        Champions =
                            new[]
                            {
                                "Akali", "Diana", "Ekko", "Fiddlesticks", "Fiora", "Fizz", "Heimerdinger", "Jayce",
                                "Kassadin", "Kayle", "Kha'Zix", "Lissandra", "Mordekaiser", "Nidalee", "Riven", "Shaco",
                                "Vladimir", "Yasuo", "Zilean"
                            },
                        Priority = Priority.High
                    },
                    new Item
                    {
                        Champions =
                            new[]
                            {
                                "Aatrox", "Darius", "Elise", "Evelynn", "Galio", "Gangplank", "Gragas", "Irelia", "Jax",
                                "Lee Sin", "Maokai", "Morgana", "Nocturne", "Pantheon", "Poppy", "Rengar", "Rumble",
                                "Ryze", "Swain", "Trundle", "Tryndamere", "Udyr", "Urgot", "Vi", "XinZhao", "RekSai"
                            },
                        Priority = Priority.Medium
                    },
                    new Item
                    {
                        Champions =
                            new[]
                            {
                                "Alistar", "Amumu", "Bard", "Blitzcrank", "Braum", "Cho'Gath", "Dr. Mundo", "Garen",
                                "Gnar", "Hecarim", "Janna", "Jarvan IV", "Leona", "Lulu", "Malphite", "Nami", "Nasus",
                                "Nautilus", "Nunu", "Olaf", "Rammus", "Renekton", "Sejuani", "Shen", "Shyvana", "Singed",
                                "Sion", "Skarner", "Sona", "Soraka", "Taric", "Thresh", "Volibear", "Warwick",
                                "MonkeyKing", "Yorick", "Zac", "Zyra"
                            },
                        Priority = Priority.Low
                    }
                };
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static HashSet<Item> Items { get; private set; }

        internal static void AddToMenu(Menu mainMenu)
        {
            try
            {
                _mainMenu = mainMenu;

                var prioritiesMenu = _mainMenu.AddSubMenu(new Menu("Priorities", _mainMenu.Name + ".priorities"));

                var autoPriority =
                    new MenuItem(prioritiesMenu.Name + ".auto", "Auto Priority").SetShared().SetValue(false);

                foreach (var enemy in Targets.Items)
                {
                    var item =
                        new MenuItem(prioritiesMenu.Name + "." + enemy.Hero.ChampionName, enemy.Hero.ChampionName)
                            .SetShared().SetValue(new Slider(MinPriority, MinPriority, MaxPriority));
                    prioritiesMenu.AddItem(item);
                    if (autoPriority.GetValue<bool>())
                    {
                        item.SetShared()
                            .SetValue(new Slider((int) GetDefaultPriority(enemy.Hero), MinPriority, MaxPriority));
                    }
                }

                prioritiesMenu.AddItem(autoPriority).ValueChanged +=
                    delegate(object sender, OnValueChangeEventArgs args)
                    {
                        if (args.GetNewValue<bool>())
                        {
                            foreach (var enemy in Targets.Items)
                            {
                                _mainMenu.Item(prioritiesMenu.Name + "." + enemy.Hero.ChampionName)
                                    .SetShared()
                                    .SetValue(
                                        new Slider((int) GetDefaultPriority(enemy.Hero), MinPriority, MaxPriority));
                            }
                        }
                    };
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static Priority GetDefaultPriority(Obj_AI_Hero hero)
        {
            try
            {
                var item = Items.FirstOrDefault(i => i.Champions.Contains(hero.ChampionName));
                if (item != null)
                {
                    return item.Priority;
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return Priority.Low;
        }

        public static int GetPriority(Obj_AI_Hero hero)
        {
            try
            {
                if (_mainMenu != null)
                {
                    var item = _mainMenu.Item(_mainMenu.Name + ".priorities." + hero.ChampionName);
                    if (item != null)
                    {
                        return item.GetValue<Slider>().Value;
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return (int) Priority.Low;
        }

        public static void SetPriority(Obj_AI_Hero hero, int value)
        {
            try
            {
                if (_mainMenu != null)
                {
                    var item = _mainMenu.Item(_mainMenu.Name + ".priorities." + hero.ChampionName);
                    if (item != null)
                    {
                        item.SetValue(
                            new Slider(Math.Max(MinPriority, Math.Min(MaxPriority, value)), MinPriority, MaxPriority));
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static void SetPriority(Obj_AI_Hero hero, Priority type)
        {
            try
            {
                if (_mainMenu != null)
                {
                    var item = _mainMenu.Item(_mainMenu.Name + ".priorities." + hero.ChampionName);
                    if (item != null)
                    {
                        item.SetValue(
                            new Slider(
                                Math.Max(MinPriority, Math.Min(MaxPriority, (int) type)), MinPriority, MaxPriority));
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static IEnumerable<Targets.Item> OrderChampions(List<Targets.Item> heroes)
        {
            try
            {
                return heroes.OrderByDescending(x => GetPriority(x.Hero));
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return new List<Targets.Item>();
        }

        public class Item
        {
            public Priority Priority { get; set; }
            public string[] Champions { get; set; }
        }
    }
}