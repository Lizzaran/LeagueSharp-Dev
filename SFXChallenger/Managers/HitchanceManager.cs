#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 HitchanceManager.cs is part of SFXChallenger.

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

namespace SFXChallenger.Managers
{
    #region

    using System;
    using LeagueSharp.Common;
    using SFXLibrary.Logger;

    #endregion

    internal class HitchanceManager
    {
        private static Menu _menu;

        public static void AddToMenu(Menu menu)
        {
            try
            {
                _menu = menu;

                _menu.AddItem(
                    new MenuItem(_menu.Name + ".q", "Q").SetValue(
                        new StringList(new[] {Global.Lang.Get("MH_Medium"), Global.Lang.Get("MH_High"), Global.Lang.Get("MH_VeryHigh")}, 1)));
                _menu.AddItem(
                    new MenuItem(_menu.Name + ".w", "W").SetValue(
                        new StringList(new[] {Global.Lang.Get("MH_Medium"), Global.Lang.Get("MH_High"), Global.Lang.Get("MH_VeryHigh")}, 1)));
                _menu.AddItem(
                    new MenuItem(_menu.Name + ".e", "E").SetValue(
                        new StringList(new[] {Global.Lang.Get("MH_Medium"), Global.Lang.Get("MH_High"), Global.Lang.Get("MH_VeryHigh")}, 1)));
                _menu.AddItem(
                    new MenuItem(_menu.Name + ".r", "R").SetValue(
                        new StringList(new[] {Global.Lang.Get("MH_Medium"), Global.Lang.Get("MH_High"), Global.Lang.Get("MH_VeryHigh")}, 1)));
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static HitChance Get(string slot)
        {
            if (_menu == null)
                return HitChance.High;
            try
            {
                switch (_menu.Item(_menu.Name + "." + slot).GetValue<StringList>().SelectedIndex)
                {
                    case 0:
                        return HitChance.Medium;
                    case 1:
                        return HitChance.High;
                    case 2:
                        return HitChance.VeryHigh;
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return HitChance.High;
        }
    }
}