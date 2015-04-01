#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 ExtendFlash.cs is part of SFXUtility.

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
    using System.Linq;
    using Classes;
    using LeagueSharp;
    using LeagueSharp.Common;
    using SFXLibrary.IoCContainer;
    using SFXLibrary.Logger;

    #endregion

    internal class ExtendFlash : Base
    {
        private Others _parent;

        public ExtendFlash(IContainer container) : base(container)
        {
            CustomEvents.Game.OnGameLoad += OnGameLoad;
        }

        public override bool Enabled
        {
            get { return _parent != null && _parent.Enabled && Menu != null && Menu.Item(Name + "Enabled").GetValue<bool>(); }
        }

        public override string Name
        {
            get { return "Extend Flash"; }
        }

        protected override void OnEnable()
        {
            Spellbook.OnCastSpell += OnSpellbookCastSpell;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            Spellbook.OnCastSpell -= OnSpellbookCastSpell;
            base.OnDisable();
        }

        private void OnGameLoad(EventArgs args)
        {
            try
            {
                if (IoC.IsRegistered<Others>())
                {
                    _parent = IoC.Resolve<Others>();
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

                Menu = new Menu(Name, Name);

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

        private void OnSpellbookCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (sender == null || !sender.Owner.IsMe ||
                !ObjectManager.Player.Spellbook.Spells.Any(
                    s => s.Slot == args.Slot && s.Name.Equals("SummonerFlash", StringComparison.OrdinalIgnoreCase)))
                return;

            if (ObjectManager.Player.ServerPosition.To2D().Distance(args.StartPosition) < 390f)
            {
                args.Process = false;
                ObjectManager.Player.Spellbook.CastSpell(args.Slot, ObjectManager.Player.ServerPosition.Extend(args.StartPosition, 400f));
            }
        }
    }
}