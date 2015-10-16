#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 SpellQueueManager.cs is part of SFXChallenger.

 SFXChallenger is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXChallenger is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXChallenger. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXChallenger.Library.Logger;

#endregion

namespace SFXChallenger.Managers
{
    public class SpellQueueManager
    {
        private static int _lastSend;
        private static int _lastCast;
        private static bool _isCasting;
        private static float _currentDelay;
        private static readonly Random Random = new Random(DateTime.Now.Millisecond);

        private static readonly Dictionary<SpellSlot, bool> SpellSlots = new Dictionary<SpellSlot, bool>
        {
            { SpellSlot.Q, true },
            { SpellSlot.W, true },
            { SpellSlot.E, true },
            { SpellSlot.R, true }
        };

        private static Menu _menu;
        public static readonly List<SpellSlot> IgnoreSpellSlots = new List<SpellSlot>();
        private static bool Enabled { get; set; }
        private static int Delay { get; set; }
        private static bool Humanizer { get; set; }
        private static int DelayMinMultiplicator { get; set; }
        private static int DelayMaxMultiplicator { get; set; }
        private static int DelayProbability { get; set; }

        public static void AddToMenu(Menu menu)
        {
            try
            {
                _menu = menu;

                var spellMenu = menu.AddSubMenu(new Menu("Spells", menu.Name + ".spells"));
                foreach (var spellSlot in SpellSlots.ToArray())
                {
                    var slot = spellSlot.Key;
                    var name = spellMenu.Name + "." + slot;
                    spellMenu.AddItem(new MenuItem(name, slot.ToString().ToUpper()))
                        .SetValue(spellSlot.Value)
                        .ValueChanged += delegate(object sender, OnValueChangeEventArgs args)
                        {
                            Reset();
                            SpellSlots[slot] = args.GetNewValue<bool>();
                        };
                    SpellSlots[slot] = _menu.Item(name).GetValue<bool>();
                }

                var humanizerMenu = menu.AddSubMenu(new Menu("Humanizer", menu.Name + ".humanizer"));
                humanizerMenu.AddItem(
                    new MenuItem(humanizerMenu.Name + ".min-delay", "Min. Multi %").SetValue(new Slider(170, 100, 300)))
                    .ValueChanged += (sender, args) => DelayMinMultiplicator = args.GetNewValue<Slider>().Value;
                humanizerMenu.AddItem(
                    new MenuItem(humanizerMenu.Name + ".max-delay", "Max. Multi %").SetValue(new Slider(220, 100, 300)))
                    .ValueChanged += (sender, args) => DelayMaxMultiplicator = args.GetNewValue<Slider>().Value;
                humanizerMenu.AddItem(
                    new MenuItem(humanizerMenu.Name + ".probability", "Probability %").SetValue(new Slider(30)))
                    .ValueChanged += (sender, args) => DelayProbability = args.GetNewValue<Slider>().Value;
                humanizerMenu.AddItem(new MenuItem(humanizerMenu.Name + ".enabled", "Enabled").SetValue(false))
                    .ValueChanged += delegate(object sender, OnValueChangeEventArgs args)
                    {
                        _currentDelay = 0;
                        Humanizer = args.GetNewValue<bool>();
                    };

                menu.AddItem(new MenuItem(menu.Name + ".delay", "Delay").SetValue(new Slider(0, 0, 500))).ValueChanged
                    += delegate(object sender, OnValueChangeEventArgs args)
                    {
                        _currentDelay = 0;
                        Delay = args.GetNewValue<Slider>().Value;
                    };

                menu.AddItem(new MenuItem(menu.Name + ".enabled", "Enabled").SetValue(false)).ValueChanged +=
                    delegate(object sender, OnValueChangeEventArgs args) { Enabled = args.GetNewValue<bool>(); };

                DelayMinMultiplicator = menu.Item(humanizerMenu.Name + ".min-delay").GetValue<Slider>().Value;
                DelayMaxMultiplicator = menu.Item(humanizerMenu.Name + ".max-delay").GetValue<Slider>().Value;
                DelayProbability = menu.Item(humanizerMenu.Name + ".probability").GetValue<Slider>().Value;
                Humanizer = menu.Item(humanizerMenu.Name + ".enabled").GetValue<bool>();

                Delay = menu.Item(menu.Name + ".delay").GetValue<Slider>().Value;
                Enabled = menu.Item(menu.Name + ".enabled").GetValue<bool>();

                Spellbook.OnCastSpell += OnSpellbookCastSpell;
                Spellbook.OnStopCast += OnSpellbookStopCast;
                Obj_AI_Base.OnProcessSpellCast += OnObjAiBaseProcessSpellCast;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private static bool CheckSpellSlot(SpellSlot slot)
        {
            try
            {
                if (SpellSlots.ContainsKey(slot))
                {
                    if (!IgnoreSpellSlots.Any(i => i.Equals(slot)))
                    {
                        return SpellSlots[slot];
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return false;
        }

        private static void OnSpellbookCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (!Enabled)
            {
                return;
            }
            try
            {
                if (sender.Owner.IsMe && CheckSpellSlot(args.Slot))
                {
                    if (Utils.GameTimeTickCount > _lastSend + 1500 || Utils.GameTimeTickCount > _lastCast + 1000)
                    {
                        Reset();
                    }
                    if (_isCasting || ObjectManager.Player.Spellbook.IsCastingSpell ||
                        Utils.GameTimeTickCount <= _lastSend + (Game.Ping / 2) + _currentDelay ||
                        Utils.GameTimeTickCount <= _lastCast + _currentDelay)
                    {
                        args.Process = false;
                    }
                    else
                    {
                        _isCasting = true;
                        _lastSend = Utils.GameTimeTickCount;
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private static void OnObjAiBaseProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!Enabled)
            {
                return;
            }
            try
            {
                if (sender.IsMe)
                {
                    _isCasting = false;
                    if (CheckSpellSlot(args.Slot))
                    {
                        Reset();

                        _lastCast = Utils.GameTimeTickCount;

                        if (Delay > 0)
                        {
                            if (Humanizer && Random.Next(0, 101) >= (100 - DelayProbability))
                            {
                                var min = (Delay / 100f) * DelayMinMultiplicator;
                                var max = (Delay / 100f) * DelayMaxMultiplicator;
                                _currentDelay = Random.Next(
                                    (int) Math.Floor(Math.Min(min, max)), (int) Math.Ceiling(Math.Max(min, max)) + 1);
                            }
                            else
                            {
                                _currentDelay = Random.Next(
                                    (int) Math.Floor(Delay * 0.9f), (int) Math.Ceiling(Delay * 1.1f) + 1);
                            }
                        }
                        else
                        {
                            _currentDelay = 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private static void OnSpellbookStopCast(Spellbook sender, SpellbookStopCastEventArgs args)
        {
            if (!Enabled)
            {
                return;
            }
            try
            {
                if (sender.Owner.IsMe)
                {
                    Reset();
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private static void Reset()
        {
            _isCasting = false;
            _lastSend = 0;
            _lastCast = 0;
            _currentDelay = 0;
        }
    }
}