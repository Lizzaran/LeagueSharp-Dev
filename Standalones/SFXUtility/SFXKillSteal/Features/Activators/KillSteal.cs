#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 KillSteal.cs is part of SFXKillSteal.

 SFXKillSteal is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXKillSteal is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXKillSteal. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXLibrary;
using SFXLibrary.Logger;
using SFXKillSteal.Classes;
using SFXKillSteal.Data;
using Items = SFXKillSteal.Data.Items;

#endregion

namespace SFXKillSteal.Features.Activators
{
    internal class KillSteal : Child<App>
    {
        public KillSteal(App parent) : base(parent)
        {
            OnLoad();
        }

        public override string Name
        {
            get { return Global.Lang.Get("F_KillSteal"); }
        }

        protected override void OnEnable()
        {
            Game.OnUpdate += OnGameUpdate;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            Game.OnUpdate -= OnGameUpdate;
            base.OnDisable();
        }

        protected override sealed void OnLoad()
        {
            try
            {
                Menu = new Menu(Name, Name);
                Menu.AddItem(new MenuItem(Name + "Summoners", Global.Lang.Get("KillSteal_Summoners")).SetValue(false));
                Menu.AddItem(new MenuItem(Name + "Items", Global.Lang.Get("KillSteal_Items")).SetValue(false));

                Menu.AddItem(new MenuItem(Name + "Enabled", Global.Lang.Get("G_Enabled")).SetValue(false));

                Parent.Menu.AddSubMenu(Menu);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnGameUpdate(EventArgs args)
        {
            try
            {
                if (ObjectManager.Player.IsDead)
                {
                    return;
                }

                try
                {
                    var items = Menu.Item(Name + "Items").GetValue<bool>();
                    var summoners = Menu.Item(Name + "Summoners").GetValue<bool>();

                    if (!items && !summoners)
                    {
                        return;
                    }

                    foreach (var enemy in GameObjects.EnemyHeroes.Where(e => e.IsVisible && !e.IsDead))
                    {
                        var itemDamage = (items ? Items.CalculateComboDamage(enemy) : 0) - 20;
                        var summonerDamage = (items ? Summoners.CalculateComboDamage(enemy, true, true) : 0) - 20;
                        if (items && itemDamage > enemy.Health)
                        {
                            Items.UseComboItems(enemy);
                        }
                        else if (summoners && summonerDamage > (enemy.Health + enemy.HPRegenRate * 3))
                        {
                            Summoners.UseComboSummoners(enemy, true, true);
                        }
                        else if (items && summoners &&
                                 (summonerDamage + itemDamage) > (enemy.Health + enemy.HPRegenRate * 3))
                        {
                            Items.UseComboItems(enemy);
                            Summoners.UseComboSummoners(enemy, true, true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Global.Logger.AddItem(new LogItem(ex));
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }
}