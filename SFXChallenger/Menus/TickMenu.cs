#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 TickMenu.cs is part of SFXChallenger.

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
using LeagueSharp.Common;
using SFXLibrary.Logger;

#endregion

namespace SFXChallenger.Menus
{
    internal class TickMenu
    {
        public static void AddToMenu(Menu menu)
        {
            try
            {
                menu.AddItem(
                    new MenuItem(menu.Name + ".tick", Global.Lang.Get("F_Tick")).SetValue(new Slider(100, 1, 300)))
                    .ValueChanged +=
                    delegate(object sender, OnValueChangeEventArgs args)
                    {
                        Core.SetInterval(args.GetNewValue<Slider>().Value);
                    };
                Core.SetInterval(menu.Item(menu.Name + ".tick").GetValue<Slider>().Value);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }
}