#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Game.cs is part of SFXUtility.

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
    using System.Diagnostics;
    using Classes;
    using LeagueSharp;
    using LeagueSharp.Common;
    using LeagueSharp.CommonEx.Core.Events;
    using SFXLibrary.IoCContainer;
    using SFXLibrary.Logger;

    #endregion

    internal class Game : Base
    {
        private Events _parent;

        public Game(IContainer container) : base(container)
        {
            Load.OnLoad += OnLoad;
        }

        public override bool Enabled
        {
            get { return _parent != null && _parent.Enabled && Menu != null && Menu.Item(Name + "Enabled").GetValue<bool>(); }
        }

        public override string Name
        {
            get { return "Game"; }
        }

        protected override void OnEnable()
        {
            LeagueSharp.Game.OnStart += GameOnStart;
            LeagueSharp.Game.OnEnd += GameOnEnd;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            LeagueSharp.Game.OnStart -= GameOnStart;
            LeagueSharp.Game.OnEnd -= GameOnEnd;
            base.OnDisable();
        }

        private void OnLoad(EventArgs args)
        {
            try
            {
                if (IoC.IsRegistered<Events>())
                {
                    _parent = IoC.Resolve<Events>();
                    if (_parent.Initialized)
                        OnParentInitialized(null, null);
                    else
                        _parent.OnInitialized += OnParentInitialized;
                }
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
            }
        }

        private void OnParentInitialized(object sender, EventArgs eventArgs)
        {
            try
            {
                if (_parent.Menu == null)
                    return;

                Menu = new Menu(Name, BaseName + Name);

                var startMenu = new Menu(Name + "GameOnStart", "Game.OnStart");
                startMenu.AddItem(new MenuItem(startMenu.Name + "SayGlHf", "Say \"gl & hf\"").SetValue(false));

                var endMenu = new Menu(Name + "GameOnEnd", "Game.OnEnd");
                endMenu.AddItem(new MenuItem(endMenu.Name + "SayGg", "Say \"gg\"").SetValue(false));
                endMenu.AddItem(new MenuItem(endMenu.Name + "CloseLoL", "Close LoL").SetValue(false));

                Menu.AddSubMenu(startMenu);
                Menu.AddSubMenu(endMenu);

                Menu.AddItem(new MenuItem(Name + "Enabled", "Enabled").SetValue(false));

                _parent.Menu.AddSubMenu(Menu);

                HandleEvents(_parent);

                RaiseOnInitialized();
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
            }
        }

        private void GameOnEnd(GameEndEventArgs args)
        {
            try
            {
                if (Menu.Item(Name + "GameOnEndSayGg").GetValue<bool>())
                    LeagueSharp.Game.Say("gg");
                if (Menu.Item(Name + "GameOnEndCloseLoL").GetValue<bool>())
                    Process.GetProcessById(Process.GetCurrentProcess().Id).Kill();
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
            }
        }

        private void GameOnStart(EventArgs args)
        {
            try
            {
                if (Menu.Item(Name + "GameOnStartSayGlHf").GetValue<bool>())
                    LeagueSharp.Game.Say("gl & hf");
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
            }
        }
    }
}