#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Priorities.cs is part of SFXTargetSelector.

 SFXTargetSelector is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXTargetSelector is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXTargetSelector. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

#endregion

namespace SFXTargetSelector
{
    public static partial class TargetSelector
    {
        public static partial class Priorities
        {
            public const int MinPriority = 1;
            public const int MaxPriority = 5;

            static Priorities()
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
                                "Kassadin", "Kayle", "Kha'Zix", "Kindred", "Lissandra", "Mordekaiser", "Nidalee",
                                "Riven", "Shaco", "Vladimir", "Yasuo", "Zilean"
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
                                "Sion", "Skarner", "Sona", "Soraka", "TahmKench", "Taric", "Thresh", "Volibear",
                                "Warwick", "MonkeyKing", "Yorick", "Zac", "Zyra"
                            },
                        Priority = Priority.Low
                    }
                };
            }

            public static Menu PrioritiesMenu { get; private set; }
            public static HashSet<Item> Items { get; private set; }

            internal static void AddToMainMenu()
            {
                PrioritiesMenu = MainMenu.AddSubMenu(new Menu("Priorities", MainMenu.Name + ".priorities"));

                var autoPriority =
                    new MenuItem(PrioritiesMenu.Name + ".auto", "Auto Priority").SetShared().SetValue(false);

                foreach (var enemy in Targets.Items)
                {
                    var item =
                        new MenuItem(PrioritiesMenu.Name + "." + enemy.Hero.ChampionName, enemy.Hero.ChampionName)
                            .SetShared().SetValue(new Slider(MinPriority, MinPriority, MaxPriority));
                    PrioritiesMenu.AddItem(item);
                    if (autoPriority.GetValue<bool>())
                    {
                        item.SetShared()
                            .SetValue(new Slider((int) GetDefaultPriority(enemy.Hero), MinPriority, MaxPriority));
                    }
                }

                PrioritiesMenu.AddItem(autoPriority).ValueChanged +=
                    delegate(object sender, OnValueChangeEventArgs args)
                    {
                        if (args.GetNewValue<bool>())
                        {
                            foreach (var enemy in Targets.Items)
                            {
                                MainMenu.Item(PrioritiesMenu.Name + "." + enemy.Hero.ChampionName)
                                    .SetShared()
                                    .SetValue(
                                        new Slider((int) GetDefaultPriority(enemy.Hero), MinPriority, MaxPriority));
                            }
                        }
                    };
            }

            public static Priority GetDefaultPriority(Obj_AI_Hero hero)
            {
                var item = Items.FirstOrDefault(i => i.Champions.Contains(hero.ChampionName));
                if (item != null)
                {
                    return item.Priority;
                }
                return Priority.Low;
            }

            public static int GetPriority(Obj_AI_Hero hero)
            {
                if (MainMenu != null)
                {
                    var item = MainMenu.Item(MainMenu.Name + ".priorities." + hero.ChampionName);
                    if (item != null)
                    {
                        return item.GetValue<Slider>().Value;
                    }
                }
                return (int) Priority.Low;
            }

            public static void SetPriority(Obj_AI_Hero hero, int value)
            {
                if (MainMenu != null)
                {
                    var item = MainMenu.Item(MainMenu.Name + ".priorities." + hero.ChampionName);
                    if (item != null)
                    {
                        item.SetValue(
                            new Slider(Math.Max(MinPriority, Math.Min(MaxPriority, value)), MinPriority, MaxPriority));
                    }
                }
            }

            public static void SetPriority(Obj_AI_Hero hero, Priority type)
            {
                if (MainMenu != null)
                {
                    var item = MainMenu.Item(MainMenu.Name + ".priorities." + hero.ChampionName);
                    if (item != null)
                    {
                        item.SetValue(
                            new Slider(
                                Math.Max(MinPriority, Math.Min(MaxPriority, (int) type)), MinPriority, MaxPriority));
                    }
                }
            }

            public static IEnumerable<Targets.Item> OrderChampions(IEnumerable<Targets.Item> targets)
            {
                if (targets == null)
                {
                    return new List<Targets.Item>();
                }
                return targets.OrderByDescending(x => GetPriority(x.Hero));
            }

            public class Item
            {
                public Priority Priority { get; set; }
                public string[] Champions { get; set; }
            }
        }
    }
}