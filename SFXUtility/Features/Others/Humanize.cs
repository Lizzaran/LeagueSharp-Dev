#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Humanize.cs is part of SFXUtility.

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

namespace SFXUtility.Features.Others
{
    #region

    using System;
    using System.Collections.Generic;
    using Classes;
    using LeagueSharp;
    using LeagueSharp.Common;
    using SFXLibrary;
    using SFXLibrary.IoCContainer;
    using SFXLibrary.Logger;

    #endregion

    internal class Humanize : Base
    {
        private readonly List<float> _lastSpell = new List<float>();
        private float _lastMovement;
        private Others _others;

        public Humanize(IContainer container)
            : base(container)
        {
            CustomEvents.Game.OnGameLoad += OnGameLoad;
        }

        public override bool Enabled
        {
            get
            {
                return _others != null && _others.Enabled && Menu != null && Menu.Item(Name + "Enabled").GetValue<bool>();
            }
        }

        public override string Name
        {
            get { return "Humanize"; }
        }

        private void OnGameLoad(EventArgs args)
        {
            try
            {
                if (IoC.IsRegistered<Others>() && IoC.Resolve<Others>().Initialized)
                {
                    OthersLoaded(IoC.Resolve<Others>());
                }
                else
                {
                    if (IoC.IsRegistered<Mediator>())
                    {
                        IoC.Resolve<Mediator>().Register("Others_initialized", OthersLoaded);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
            }
        }

        private void OthersLoaded(object o)
        {
            try
            {
                var others = o as Others;
                if (others != null && others.Menu != null)
                {
                    _others = others;

                    Menu = new Menu(Name, Name);

                    var delayMenu = new Menu("Delay", Name + "Delay");
                    delayMenu.AddItem(new MenuItem(Name + "DelaySpells", "Spells (ms)").SetValue(new Slider(50, 0, 250)));
                    delayMenu.AddItem(
                        new MenuItem(Name + "DelayMovement", "Movement (ms)").SetValue(new Slider(50, 0, 250)));

                    Menu.AddSubMenu(delayMenu);

                    Menu.AddItem(new MenuItem(Name + "Enabled", "Enabled").SetValue(true));

                    _others.Menu.AddSubMenu(Menu);

                    Obj_AI_Base.OnIssueOrder += OnObjAiBaseOnIssueOrder;
                    Spellbook.OnCastSpell += OnSpellbookOnCastSpell;

                    Initialized = true;
                }
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
            }
        }

        private void OnSpellbookOnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (sender == null || !sender.Owner.IsMe ||
                !(args.Slot == SpellSlot.Q || args.Slot == SpellSlot.W || args.Slot == SpellSlot.E ||
                  args.Slot == SpellSlot.R) || !Enabled)
            {
                return;
            }

            if (Environment.TickCount - _lastSpell[(int) args.Slot] <
                Menu.Item(Name + "DelaySpells").GetValue<Slider>().Value)
            {
                args.Process = false;
                return;
            }

            _lastSpell[(int) args.Slot] = Environment.TickCount;
        }

        private void OnObjAiBaseOnIssueOrder(Obj_AI_Base sender, GameObjectIssueOrderEventArgs args)
        {
            if (sender == null || !sender.IsValid || !sender.IsMe || args.Order != GameObjectOrder.MoveTo || !Enabled)
            {
                return;
            }

            if (Environment.TickCount - _lastMovement < Menu.Item(Name + "DelayMovement").GetValue<Slider>().Value)
            {
                args.Process = false;
                return;
            }

            _lastMovement = Environment.TickCount;
        }
    }
}