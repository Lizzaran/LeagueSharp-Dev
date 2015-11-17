#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Focus.cs is part of SFXTargetSelector.

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

using LeagueSharp.Common;

#endregion

namespace SFXTargetSelector
{
    public static partial class TargetSelector
    {
        public static class Focus
        {
            private static bool _force;
            private static bool _enabled;

            public static bool Force
            {
                get { return _force; }
                set
                {
                    _force = value;
                    if (MainMenu != null)
                    {
                        var item = MainMenu.Item(MainMenu.Name + ".force-focus");
                        if (item != null)
                        {
                            item.SetValue(value);
                        }
                    }
                }
            }

            public static bool Enabled
            {
                get { return _enabled; }
                set
                {
                    _enabled = value;
                    if (MainMenu != null)
                    {
                        var item = MainMenu.Item(MainMenu.Name + ".focus");
                        if (item != null)
                        {
                            item.SetValue(value);
                        }
                    }
                }
            }

            internal static void AddToMainMenu()
            {
                MainMenu.AddItem(
                    new MenuItem(MainMenu.Name + ".focus", "Focus Selected Target").SetShared().SetValue(true))
                    .ValueChanged +=
                    delegate(object sender, OnValueChangeEventArgs args) { _enabled = args.GetNewValue<bool>(); };
                MainMenu.AddItem(
                    new MenuItem(MainMenu.Name + ".force-focus", "Only Attack Selected Target").SetShared()
                        .SetValue(false)).ValueChanged +=
                    delegate(object sender, OnValueChangeEventArgs args) { _force = args.GetNewValue<bool>(); };

                Enabled = MainMenu.Item(MainMenu.Name + ".focus").GetValue<bool>();
                Force = MainMenu.Item(MainMenu.Name + ".force-focus").GetValue<bool>();
            }
        }
    }
}