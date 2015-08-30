#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 TargetSpellManager.cs is part of SFXKogMaw.

 SFXKogMaw is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXKogMaw is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXKogMaw. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXKogMaw.Events;
using SFXLibrary;
using SFXLibrary.Extensions.NET;
using SFXLibrary.Logger;

#endregion

namespace SFXKogMaw.Managers
{
    internal class TargetSpellManager
    {
        private static Menu _menu;

        private static readonly List<SpellSlot> SpellSlots = new List<SpellSlot>
        {
            SpellSlot.Q,
            SpellSlot.W,
            SpellSlot.E,
            SpellSlot.R
        };

        static TargetSpellManager()
        {
            try
            {
                Obj_AI_Base.OnProcessSpellCast += OnObjAiBaseProcessSpellCast;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static event EventHandler<TargetCastArgs> OnTargetCast;
        public static event EventHandler<TargetCastArgs> OnAllyTargetCast;
        public static event EventHandler<TargetCastArgs> OnEnemyTargetCast;

        private static void OnObjAiBaseProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            try
            {
                var hero = sender as Obj_AI_Hero;
                if (hero == null || !hero.IsValid || hero.IsMe)
                {
                    return;
                }
                var target = FixTarget(hero, args.Target, args.SData);
                if (target == null)
                {
                    return;
                }
                var enabled = true;
                if (_menu != null)
                {
                    var item = _menu.Item(_menu.Name + "." + hero.ChampionName + "." + args.SData.Name);
                    enabled = item != null && item.GetValue<bool>();
                }
                if (enabled)
                {
                    var eventArgs = new TargetCastArgs(
                        hero, target, args.SData.TargettingType, FixDelay(hero, args.SData), FixSpeed(hero, args.SData),
                        args.SData);
                    OnTargetCast.RaiseEvent(null, eventArgs);
                    (hero.IsAlly ? OnAllyTargetCast : OnEnemyTargetCast).RaiseEvent(null, eventArgs);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private static Obj_AI_Hero FixTarget(Obj_AI_Hero sender, GameObject target, SpellData data)
        {
            if (target == null)
            {
                var range = (data.CastRange + data.CastRadius + sender.BoundingRadius +
                             ObjectManager.Player.BoundingRadius) * 1.25f;
                if (ObjectManager.Player.Distance(sender) < range)
                {
                    return ObjectManager.Player;
                }
            }
            return target != null && target.IsMe ? ObjectManager.Player : null;
        }

        private static float FixDelay(Obj_AI_Hero hero, SpellData data)
        {
            try
            {
                var spell = hero.Spellbook.Spells.FirstOrDefault(s => s.SData.Name.Equals(data.Name));
                if (spell == null)
                {
                    return data.CastFrame / 30;
                }
                var slot = spell.Slot;
                if (hero.ChampionName.Equals("Caitlyn", StringComparison.OrdinalIgnoreCase) && slot == SpellSlot.R)
                {
                    return 1f;
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return data.CastFrame / 30;
        }

        // ReSharper disable once UnusedParameter.Local
        private static float FixSpeed(Obj_AI_Hero hero, SpellData data)
        {
            return data.MissileSpeed;
        }

        public static void AddToMenu(Menu menu, bool ally, bool enemy)
        {
            try
            {
                _menu = menu;

                foreach (var hero in GameObjects.Heroes.Where(h => (ally && h.IsAlly || enemy && h.IsEnemy) && !h.IsMe))
                {
                    var spells =
                        SpellSlots.Select(slot => hero.GetSpell(slot))
                            .Where(spell => spell != null && !spell.SData.IsAutoAttack())
                            .Where(
                                spell =>
                                    spell.SData.TargettingType == SpellDataTargetType.Unit ||
                                    spell.SData.TargettingType == SpellDataTargetType.SelfAoe)
                            .ToList();
                    if (spells.Any())
                    {
                        var heroMenu = menu.AddSubMenu(new Menu(hero.ChampionName, menu.Name + "." + hero.ChampionName));
                        foreach (var spell in spells)
                        {
                            heroMenu.AddItem(
                                new MenuItem(heroMenu.Name + "." + spell.SData.Name, spell.Slot.ToString().ToUpper()))
                                .SetValue(true);
                        }
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