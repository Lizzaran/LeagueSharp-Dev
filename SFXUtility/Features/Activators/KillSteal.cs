#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 KillSteal.cs is part of SFXUtility.

 SFXUtility is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXUtility is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXUtility. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

namespace SFXUtility.Features.Activators
{
    #region

    using System;
    using System.Linq;
    using Classes;
    using Data;
    using LeagueSharp;
    using LeagueSharp.Common;
    using SFXLibrary;
    using SFXLibrary.Logger;
    using Items = Data.Items;

    #endregion

    internal class KillSteal : Base
    {
        private Activators _parent;

        public override bool Enabled
        {
            get { return !Unloaded && _parent != null && _parent.Enabled && Menu != null && Menu.Item(Name + "Enabled").GetValue<bool>(); }
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

        private void OnParentInitialized(object sender, EventArgs eventArgs)
        {
            try
            {
                if (_parent.Menu == null)
                    return;

                Menu = new Menu(Name, Name);

                Menu.AddItem(new MenuItem(Name + "Summoners", Global.Lang.Get("KillSteal_Summoners")).SetValue(false));
                Menu.AddItem(new MenuItem(Name + "Items", Global.Lang.Get("KillSteal_Items")).SetValue(false));

                Menu.AddItem(new MenuItem(Name + "Enabled", Global.Lang.Get("G_Enabled")).SetValue(false));

                _parent.Menu.AddSubMenu(Menu);

                HandleEvents(_parent);
                RaiseOnInitialized();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected override void OnGameLoad(EventArgs args)
        {
            try
            {
                if (Global.IoC.IsRegistered<Activators>())
                {
                    _parent = Global.IoC.Resolve<Activators>();
                    if (_parent.Initialized)
                        OnParentInitialized(null, null);
                    else
                        _parent.OnInitialized += OnParentInitialized;
                }
                else if (Global.IoC.IsRegistered<Mediator>())
                {
                    Global.IoC.Resolve<Mediator>().Register(_parent.Name, delegate(object o) { OnParentInitialized(o, new EventArgs()); });
                }
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
                    return;

                try
                {
                    var items = Menu.Item(Name + "Items").GetValue<bool>();
                    var summoners = Menu.Item(Name + "Summoners").GetValue<bool>();

                    if (!items && !summoners)
                        return;

                    foreach (var enemy in HeroManager.Enemies.Where(e => e.IsVisible && !e.IsDead))
                    {
                        var itemDamage = (items ? Items.CalculateComboDamage(enemy) : 0) - 20;
                        var summonerDamage = (items ? Summoners.CalculateComboDamage(enemy, true, true) : 0) - 20;
                        if (items && itemDamage > enemy.Health)
                        {
                            Items.UseComboItems(enemy);
                        }
                        else if (summoners && summonerDamage > (enemy.Health + enemy.HPRegenRate*3))
                        {
                            Summoners.UseComboSummoners(enemy, true, true);
                        }
                        else if (items && summoners && (summonerDamage + itemDamage) > (enemy.Health + enemy.HPRegenRate*3))
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