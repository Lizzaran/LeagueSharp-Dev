#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 KillstealManager.cs is part of SFXChallenger.

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
    using System.Linq;
    using LeagueSharp.Common;
    using SFXLibrary.Logger;
    using Wrappers;

    #endregion

    internal class KillstealManager
    {
        private static Menu _menu;

        public static void AddToMenu(Menu menu)
        {
            try
            {
                _menu = menu;
                menu.AddItem(new MenuItem(menu.Name + ".items", Global.Lang.Get("MK_UseItems")).SetValue(true));
                menu.AddItem(new MenuItem(menu.Name + ".summoners", Global.Lang.Get("MK_UseSummoners")).SetValue(true));
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static void Killsteal()
        {
            if (_menu == null)
                return;
            try
            {
                var items = _menu.Item(_menu.Name + ".items").GetValue<bool>();
                var summoners = _menu.Item(_menu.Name + ".summoners").GetValue<bool>();

                if (!items && !summoners)
                    return;

                foreach (var enemy in HeroManager.Enemies.Where(e => !Invulnerable.HasBuff(e)))
                {
                    var itemDamage = (items ? ItemManager.CalculateComboDamage(enemy) : 0) - 20;
                    var summonerDamage = (items ? SummonerManager.CalculateComboDamage(enemy) : 0) - 20;
                    if (items && itemDamage > enemy.Health)
                    {
                        ItemManager.UseComboItems(enemy);
                    }
                    else if (summoners && summonerDamage > (enemy.Health + enemy.HPRegenRate*3))
                    {
                        SummonerManager.UseComboSummoners(enemy);
                    }
                    else if (items && summoners && (summonerDamage + itemDamage) > (enemy.Health + enemy.HPRegenRate*3))
                    {
                        ItemManager.UseComboItems(enemy);
                        SummonerManager.UseComboSummoners(enemy);
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }
}