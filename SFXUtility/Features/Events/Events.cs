#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Events.cs is part of SFXUtility.

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

namespace SFXUtility.Features.Events
{
    #region

    using System;
    using Classes;
    using LeagueSharp.Common;
    using SFXLibrary;
    using SFXLibrary.IoCContainer;

    #endregion

    internal class Events : Base
    {
        public Events(IContainer container) : base(container)
        {
            CustomEvents.Game.OnGameLoad += OnGameLoad;
        }

        public override bool Enabled
        {
            get { return Menu != null && Menu.Item(Name + "Enabled").GetValue<bool>(); }
        }

        public override string Name
        {
            get { return "Events"; }
        }

        private void OnGameLoad(EventArgs args)
        {
            try
            {
                Logger.Prefix = string.Format("{0} - {1}", BaseName, Name);

                Menu = new Menu(Name, Name);

                Menu.AddItem(new MenuItem(Name + "Enabled", "Enabled").SetValue(true));

                BaseMenu.AddSubMenu(Menu);

                if (IoC.IsRegistered<Mediator>())
                {
                    IoC.Resolve<Mediator>().NotifyColleagues(Name + "_initialized", this);
                }

                Initialized = true;
            }
            catch (Exception ex)
            {
                Logger.WriteBlock(ex);
            }
        }
    }
}