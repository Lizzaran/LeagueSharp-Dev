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
    using Classes;
    using LeagueSharp;
    using LeagueSharp.Common;
    using SFXLibrary;
    using SFXLibrary.Logger;

    #endregion

    internal class Game : Base
    {
        private bool _onEndTriggerd;
        private Events _parent;

        public override bool Enabled
        {
            get { return _parent != null && _parent.Enabled && Menu != null && Menu.Item(Name + "Enabled").GetValue<bool>(); }
        }

        public override string Name
        {
            get { return Language.Get("F_Game"); }
        }

        protected override void OnEnable()
        {
            LeagueSharp.Game.OnStart += OnGameStart;
            LeagueSharp.Game.OnNotify += OnGameNotify;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            LeagueSharp.Game.OnStart -= OnGameStart;
            LeagueSharp.Game.OnNotify -= OnGameNotify;
            base.OnDisable();
        }

        protected override void OnGameLoad(EventArgs args)
        {
            try
            {
                if (Global.IoC.IsRegistered<Events>())
                {
                    _parent = Global.IoC.Resolve<Events>();
                    if (_parent.Initialized)
                        OnParentInitialized(null, null);
                    else
                        _parent.OnInitialized += OnParentInitialized;
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
                if (_parent.Menu == null)
                    return;

                Menu = new Menu(Name, Name);

                var startMenu = new Menu(Language.Get("Game_OnStart"), Name + "OnStart");
                startMenu.AddItem(new MenuItem(startMenu.Name + "Say", Language.Get("Game_SayGLHF")).SetValue(false));

                var endMenu = new Menu(Language.Get("Game_OnEnd"), Name + "OnEnd");
                endMenu.AddItem(new MenuItem(endMenu.Name + "Say", Language.Get("Game_SayGG")).SetValue(false));

                Menu.AddSubMenu(startMenu);
                Menu.AddSubMenu(endMenu);

                Menu.AddItem(new MenuItem(Name + "Enabled", Language.Get("G_Enabled")).SetValue(false));

                _parent.Menu.AddSubMenu(Menu);

                HandleEvents(_parent);

                RaiseOnInitialized();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnGameNotify(GameNotifyEventArgs args)
        {
            if (_onEndTriggerd ||
                (args.EventId != GameEventId.OnEndGame && args.EventId != GameEventId.OnHQDie && args.EventId != GameEventId.OnHQKill))
                return;

            _onEndTriggerd = true;
            try
            {
                if (Menu.Item(Name + "OnEndSay").GetValue<bool>())
                    LeagueSharp.Game.Say("gg");
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnGameStart(EventArgs args)
        {
            try
            {
                if (Menu.Item(Name + "OnStartSay").GetValue<bool>())
                    LeagueSharp.Game.Say("gl & hf");
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }
}