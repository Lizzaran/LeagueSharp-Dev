#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Humanizer.cs is part of SFXChallenger.

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
        public static class Humanizer
        {
            public const int MinDelay = 0;
            private const int MaxDelay = 1500;
            private static int _fowDelay = 350;

            public static int FowDelay
            {
                get { return _fowDelay; }
                set
                {
                    _fowDelay = Math.Min(MaxDelay, Math.Max(MinDelay, value));
                    if (MainMenu != null)
                    {
                        var item = MainMenu.Item(MainMenu.Name + ".fow");
                        if (item != null)
                        {
                            item.SetValue(new Slider(_fowDelay, MinDelay, MaxDelay));
                        }
                    }
                }
            }

            internal static void AddToMainMenu()
            {
                MainMenu.AddItem(
                    new MenuItem(MainMenu.Name + ".fow", "Target Acquire Delay").SetShared()
                        .SetValue(new Slider(_fowDelay, MinDelay, MaxDelay))).ValueChanged +=
                    delegate(object sender, OnValueChangeEventArgs args)
                    {
                        _fowDelay = args.GetNewValue<Slider>().Value;
                    };

                _fowDelay = MainMenu.Item(MainMenu.Name + ".fow").GetValue<Slider>().Value;
            }

            public static IEnumerable<Targets.Item> FilterTargets(IEnumerable<Targets.Item> targets)
            {
                if (targets == null)
                {
                    return new List<Targets.Item>();
                }
                var finalTargets = targets.ToList();
                if (FowDelay > 0)
                {
                    finalTargets =
                        finalTargets.Where(item => Game.Time - item.LastVisibleChange > FowDelay / 1000f).ToList();
                }
                return finalTargets;
            }
        }
    }
}