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
using SFXLibrary.Logger;
using SFXUtility.Classes;

#endregion

namespace SFXUtility.Features.Others
{
    internal class Humanize : Child<Others>
    {
        private float _lastMovement;
        private Dictionary<SpellSlot, float> _lastSpell;
        public Humanize(SFXUtility sfx) : base(sfx) {}

        public override string Name
        {
            get { return Global.Lang.Get("F_Humanize"); }
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

        protected override void OnLoad()
        {
            try
            {
                Menu = new Menu(Name, Name);
                var delayMenu = new Menu(Global.Lang.Get("G_Delay"), Name + "Delay");
                delayMenu.AddItem(
                    new MenuItem(delayMenu.Name + "Spell", Global.Lang.Get("G_Spell")).SetValue(new Slider(50, 0, 250)));
                delayMenu.AddItem(
                    new MenuItem(delayMenu.Name + "Movement", Global.Lang.Get("G_Movement")).SetValue(
                        new Slider(50, 0, 250)));

                Menu.AddSubMenu(delayMenu);

                Menu.AddItem(new MenuItem(Name + "Enabled", Global.Lang.Get("G_Enabled")).SetValue(false));

                Parent.Menu.AddSubMenu(Menu);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected override void OnInitialize()
        {
            _lastSpell = new Dictionary<SpellSlot, float>();
            base.OnInitialize();
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