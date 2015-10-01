#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Targets.cs is part of SFXTargetSelector.

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
using LeagueSharp;
using LeagueSharp.Common;

#endregion

namespace SFXTargetSelector
{
    public static class Targets
    {
        static Targets()
        {
            Items = new HashSet<Item>();
            foreach (var enemy in HeroManager.Enemies)
            {
                Items.Add(new Item(enemy));
            }
            Game.OnUpdate += OnGameUpdate;
        }

        public static HashSet<Item> Items { get; private set; }

        private static void OnGameUpdate(EventArgs args)
        {
            foreach (var item in Items)
            {
                if (item.Visible && !item.Hero.IsVisible || !item.Visible && item.Hero.IsVisible)
                {
                    item.Visible = item.Hero.IsVisible;
                    item.LastVisibleChange = Game.Time;
                }
            }
        }

        public class Item
        {
            public Item(Obj_AI_Hero hero)
            {
                Hero = hero;
                LastVisibleChange = Game.Time;
                Visible = false;
            }

            public Obj_AI_Hero Hero { get; private set; }
            public float Weight { get; set; }
            public float SimulatedWeight { get; set; }
            public float LastVisibleChange { get; set; }
            public bool Visible { get; set; }
        }
    }
}