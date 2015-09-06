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

#region

using System;
using System.Collections.Generic;
using LeagueSharp;
using LeagueSharp.Common;
using SFXUtility.Classes;
using SFXUtility.Library.Logger;

#endregion

namespace SFXUtility.Features.Others
{
    internal class Humanize : Child<Others>
    {
        private readonly Dictionary<SpellSlot, float> _lastSpell = new Dictionary<SpellSlot, float>();
        private float _lastMovement;

        public Humanize(Others parent) : base(parent)
        {
            OnLoad();
        }

        public override string Name
        {
            get { return "Humanize"; }
        }

        protected override void OnEnable()
        {
            Obj_AI_Base.OnIssueOrder += OnObjAiBaseIssueOrder;
            Spellbook.OnCastSpell += OnSpellbookCastSpell;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            Obj_AI_Base.OnIssueOrder -= OnObjAiBaseIssueOrder;
            Spellbook.OnCastSpell -= OnSpellbookCastSpell;
            base.OnDisable();
        }

        protected override sealed void OnLoad()
        {
            try
            {
                Menu = new Menu(Name, Name);
                var delayMenu = new Menu("Delay", Name + "Delay");
                delayMenu.AddItem(new MenuItem(delayMenu.Name + "Spell", "Spell").SetValue(new Slider(50, 0, 250)));
                delayMenu.AddItem(
                    new MenuItem(delayMenu.Name + "Movement", "Movement").SetValue(new Slider(50, 0, 250)));

                Menu.AddSubMenu(delayMenu);

                Menu.AddItem(new MenuItem(Name + "Enabled", "Enabled").SetValue(false));

                Parent.Menu.AddSubMenu(Menu);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnSpellbookCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            try
            {
                if (sender == null || !sender.Owner.IsMe ||
                    !(args.Slot == SpellSlot.Q || args.Slot == SpellSlot.W || args.Slot == SpellSlot.E ||
                      args.Slot == SpellSlot.R))
                {
                    return;
                }

                float timestamp;
                if (_lastSpell.TryGetValue(args.Slot, out timestamp))
                {
                    if (Environment.TickCount - timestamp < Menu.Item(Name + "DelaySpell").GetValue<Slider>().Value)
                    {
                        args.Process = false;
                        return;
                    }
                }

                _lastSpell[args.Slot] = Environment.TickCount;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnObjAiBaseIssueOrder(Obj_AI_Base sender, GameObjectIssueOrderEventArgs args)
        {
            try
            {
                if (sender == null || !sender.IsValid || !sender.IsMe || args.Order != GameObjectOrder.MoveTo)
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
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }
}