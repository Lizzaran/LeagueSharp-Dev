#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 AutoLantern.cs is part of SFXUtility.

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

#region

using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXLibrary.Logger;
using SFXUtility.Classes;

#endregion

namespace SFXUtility.Features.Others
{
    internal class AutoLantern : Child<Others>
    {
        public AutoLantern(SFXUtility sfx) : base(sfx) {}

        public override string Name
        {
            get { return Global.Lang.Get("F_AutoLantern"); }
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

        protected override void OnGameLoad(EventArgs args)
        {
            try
            {
                if (Global.IoC.IsRegistered<Others>())
                {
                    Parent = Global.IoC.Resolve<Others>();
                    if (Parent.Initialized)
                    {
                        OnParentInitialized(null, null);
                    }
                    else
                    {
                        Parent.OnInitialized += OnParentInitialized;
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnParentInitialized(object sender, EventArgs eventArgs)
        {
            try
            {
                Menu = new Menu(Name, Name);

                Menu.AddItem(
                    new MenuItem(Name + "Percent", Global.Lang.Get("G_Health") + " " + Global.Lang.Get("G_Percent"))
                        .SetValue(new Slider(20, 0, 50)));
                Menu.AddItem(
                    new MenuItem(Name + "Hotkey", Global.Lang.Get("G_Hotkey")).SetValue(
                        new KeyBind('U', KeyBindType.Press)));

                Menu.AddItem(new MenuItem(Name + "Enabled", Global.Lang.Get("G_Enabled")).SetValue(false));

                Parent.Menu.AddSubMenu(Menu);

                if (
                    HeroManager.Allies.Any(
                        a => !a.IsMe && a.ChampionName.Equals("Thresh", StringComparison.OrdinalIgnoreCase)))
                {
                    return;
                }

                HandleEvents();
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

                if (ObjectManager.Player.HealthPercent <= Menu.Item(Name + "Percent").GetValue<Slider>().Value ||
                    Menu.Item(Name + "Hotkey").IsActive())
                {
                    var lantern =
                        ObjectManager.Get<Obj_AI_Base>()
                            .FirstOrDefault(
                                obj =>
                                    obj.IsValid && obj.IsAlly &&
                                    obj.Name.Equals("ThreshLantern", StringComparison.OrdinalIgnoreCase));
                    if (lantern != null && lantern.IsValidTarget(500, false, ObjectManager.Player.ServerPosition))
                    {
                        ObjectManager.Player.Spellbook.CastSpell((SpellSlot) 62, lantern);
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