#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 SFXHumanizerPro.cs is part of SFXHumanizer Pro.

 SFXHumanizer Pro is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXHumanizer Pro is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXHumanizer Pro. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;
using Color = System.Drawing.Color;

#endregion

namespace SFXHumanizer_Pro
{
    internal class SFXHumanizerPro
    {
        private readonly List<SpellSlot> _items = new List<SpellSlot>
        {
            SpellSlot.Item1,
            SpellSlot.Item2,
            SpellSlot.Item3,
            SpellSlot.Item4,
            SpellSlot.Item5,
            SpellSlot.Item6,
            SpellSlot.Trinket
        };

        private readonly CryptoRandom _random = new CryptoRandom();
        private readonly Dictionary<GameObjectOrder, bool> _randomizedOrders = new Dictionary<GameObjectOrder, bool>();
        private readonly Dictionary<SpellSlot, bool> _randomizedSpells = new Dictionary<SpellSlot, bool>();
        private readonly Dictionary<GameObjectOrder, Sequence> _sequences = new Dictionary<GameObjectOrder, Sequence>();

        private readonly List<SpellSlot> _spells = new List<SpellSlot>
        {
            SpellSlot.Q,
            SpellSlot.W,
            SpellSlot.E,
            SpellSlot.R
        };

        private readonly List<string> _targetTypes = new List<string> { "Cone", "Unit", "Location" };
        private int _blockedOrders;
        private int _blockedSpells;
        private Font _font;
        private bool _isCasting;
        private Vector3 _lastAttackPosition = Vector3.Zero;
        private GameObject _lastAttackTarget;
        private Vector3 _lastCastPosition = Vector3.Zero;
        private int _lastFlashCast;
        private int _lastItemCast;
        private int _lastSpellCast;
        private Menu _menu;

        public SFXHumanizerPro()
        {
            CustomEvents.Game.OnGameLoad += OnGameLoad;
        }

        private void OnGameLoad(EventArgs args)
        {
            try
            {
                _menu = new Menu("SFXHumanizer Pro", "sfx.humanizer-pro", true);

                var drawingMenu = _menu.AddSubMenu(new Menu("Drawings", _menu.Name + ".drawings"));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + ".top", "Offset Top").SetValue(new Slider(100, 0, Drawing.Height)));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + ".right", "Offset Right").SetValue(
                        new Slider(Drawing.Width - 20, 0, Drawing.Width)));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + ".size", "Font Size").SetValue(new Slider(17, 5, 30)));
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + ".color", "Color").SetValue(Color.Lime));
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + ".spells", "Blocked Spells").SetValue(true));
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + ".orders", "Blocked Orders").SetValue(true));

                var spellMenu = _menu.AddSubMenu(new Menu("Spells", _menu.Name + ".spells"));

                var blackListMenu =
                    spellMenu.AddSubMenu(
                        new Menu("Blacklist", spellMenu.Name + ".blacklist-" + ObjectManager.Player.ChampionName));
                foreach (var spell in _spells)
                {
                    blackListMenu.AddItem(
                        new MenuItem(blackListMenu.Name + "." + spell, spell.ToString()).SetValue(false));
                }

                spellMenu.AddItem(
                    new MenuItem(spellMenu.Name + ".delay", "Average Delay").SetValue(new Slider(75, 0, 500))
                        .SetTooltip("Delay between spells."));
                spellMenu.AddItem(
                    new MenuItem(spellMenu.Name + ".range-delay", "Dynamic Range Delay %").SetValue(
                        new Slider(100, 0, 200)).SetTooltip("0 = Disabled. As higher the value as higher is the delay."));
                spellMenu.AddItem(
                    new MenuItem(spellMenu.Name + ".position", "Randomized Position").SetValue(new Slider(10, 0, 25))
                        .SetTooltip("Randomize the cast position based on the value."));
                spellMenu.AddItem(
                    new MenuItem(spellMenu.Name + ".checks", "Additional Checks").SetValue(true)
                        .SetTooltip("Checks if Chat / Shop is open and if you can cast."));
                spellMenu.AddItem(
                    new MenuItem(spellMenu.Name + ".screen", "Block Offscreen").SetValue(false)
                        .SetTooltip("Block all spells which are outside of your screen / view."));
                spellMenu.AddItem(
                    new MenuItem(spellMenu.Name + ".flash", "Disable after Flash").SetValue(new Slider(3, 0, 10))
                        .SetTooltip("Disable humanizer after flash for x seconds."));

                var orderMenu = _menu.AddSubMenu(new Menu("Orders", _menu.Name + ".orders"));
                orderMenu.AddItem(
                    new MenuItem(orderMenu.Name + ".clicks", "Max. Average Per Second").SetValue(new Slider(10, 1, 20))
                        .SetTooltip("Average of maximum orders per second."));
                orderMenu.AddItem(
                    new MenuItem(orderMenu.Name + ".range-delay", "Dynamic Attack Range Delay %").SetValue(
                        new Slider(100, 0, 200)).SetTooltip("0 = Disabled. As higher the value as higher is the delay."));
                orderMenu.AddItem(
                    new MenuItem(orderMenu.Name + ".position", "Randomized Position").SetValue(new Slider(20, 0, 50))
                        .SetTooltip("Randomize the click position based on the value."));
                orderMenu.AddItem(
                    new MenuItem(orderMenu.Name + ".sharp-turn", "Check Sharp Turns").SetValue(true)
                        .SetTooltip("Reduce the delay if you run in a other direction."));
                orderMenu.AddItem(
                    new MenuItem(orderMenu.Name + ".screen", "Block Offscreen").SetValue(false)
                        .SetTooltip("Block all orders which are outside of your screen / view."));

                _menu.AddItem(new MenuItem(_menu.Name + ".enabled", "Enabled").SetValue(true));

                _menu.AddToMainMenu();

                _font = new Font(
                    Drawing.Direct3DDevice,
                    new FontDescription
                    {
                        FaceName = "Calibri",
                        Height = _menu.Item(_menu.Name + ".drawings.size").GetValue<Slider>().Value,
                        OutputPrecision = FontPrecision.Default,
                        Quality = FontQuality.Default
                    });

                Obj_AI_Base.OnIssueOrder += OnObjAiBaseIssueOrder;
                Spellbook.OnCastSpell += OnSpellbookCastSpell;
                Spellbook.OnStopCast += OnSpellbookStopCast;
                Obj_AI_Base.OnProcessSpellCast += OnObjAiBaseProcessSpellCast;
                Drawing.OnEndScene += OnDrawingEndScene;
                Drawing.OnPreReset += OnDrawingPreReset;
                Drawing.OnPostReset += OnDrawingPostReset;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void OnDrawingPostReset(EventArgs args)
        {
            try
            {
                if (_font != null && !_font.IsDisposed)
                {
                    _font.OnResetDevice();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void OnDrawingPreReset(EventArgs args)
        {
            try
            {
                if (_font != null && !_font.IsDisposed)
                {
                    _font.OnLostDevice();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void OnDrawingEndScene(EventArgs args)
        {
            try
            {
                if (!_menu.Item(_menu.Name + ".enabled").GetValue<bool>())
                {
                    return;
                }
                if (Drawing.Direct3DDevice == null || Drawing.Direct3DDevice.IsDisposed)
                {
                    return;
                }

                var offsetTop = _menu.Item(_menu.Name + ".drawings.top").GetValue<Slider>().Value;
                var offsetRight = _menu.Item(_menu.Name + ".drawings.right").GetValue<Slider>().Value;
                var size = _menu.Item(_menu.Name + ".drawings.size").GetValue<Slider>().Value;
                var color = _menu.Item(_menu.Name + ".drawings.color").GetValue<Color>();
                var sharpColor = new SharpDX.Color(color.R, color.G, color.B);
                var spells = _menu.Item(_menu.Name + ".drawings.spells").GetValue<bool>();
                var orders = _menu.Item(_menu.Name + ".drawings.spells").GetValue<bool>();

                if (spells)
                {
                    _font.DrawText(
                        string.Format("Spells: {0}", _blockedSpells), new Vector2(offsetRight, offsetTop), sharpColor);
                }

                if (orders)
                {
                    _font.DrawText(
                        string.Format("Orders: {0}", _blockedOrders),
                        new Vector2(offsetRight, offsetTop + (spells ? size + 3 : 0)), sharpColor);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void OnObjAiBaseProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            try
            {
                if (!_menu.Item(_menu.Name + ".enabled").GetValue<bool>())
                {
                    return;
                }
                if (sender.IsMe)
                {
                    if (args.SData.Name.Equals("SummonerFlash", StringComparison.OrdinalIgnoreCase))
                    {
                        _lastFlashCast = Utils.GameTimeTickCount;
                    }
                    var isSpell = _spells.Any(s => s.Equals(args.Slot));
                    var isItem = _items.Any(s => s.Equals(args.Slot));
                    if (isSpell || isItem || args.SData.IsAutoAttack())
                    {
                        _isCasting = false;
                    }
                    if (isSpell || isItem)
                    {
                        _randomizedSpells[args.Slot] = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void OnSpellbookStopCast(Spellbook sender, SpellbookStopCastEventArgs args)
        {
            try
            {
                if (sender.Owner.IsMe)
                {
                    _isCasting = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private bool IsSpellEnabled(SpellSlot slot)
        {
            var item = _menu.Item(_menu.Name + ".spells.blacklist-" + ObjectManager.Player.ChampionName + "." + slot);
            return item != null && !item.GetValue<bool>();
        }

        private void OnSpellbookCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            try
            {
                if (!sender.Owner.IsMe || !_menu.Item(_menu.Name + ".enabled").GetValue<bool>())
                {
                    return;
                }

                var flash = _menu.Item(_menu.Name + ".spells.flash").GetValue<Slider>().Value;
                if (flash > 0 && Utils.GameTimeTickCount - _lastFlashCast <= flash * 1000)
                {
                    return;
                }

                var isSpell = _spells.Any(s => s.Equals(args.Slot) && IsSpellEnabled(s));
                var isItem = _items.Any(s => s.Equals(args.Slot));
                if (!isSpell && !isItem)
                {
                    return;
                }

                if (!_randomizedSpells.ContainsKey(args.Slot))
                {
                    _randomizedSpells[args.Slot] = false;
                }
                if (_randomizedSpells[args.Slot])
                {
                    _randomizedSpells[args.Slot] = false;
                    return;
                }

                var spell = ObjectManager.Player.Spellbook.GetSpell(args.Slot);
                if (spell == null)
                {
                    return;
                }

                var position = args.Target != null
                    ? args.Target.Position
                    : (args.StartPosition.IsValid()
                        ? args.StartPosition
                        : (args.EndPosition.IsValid() ? args.EndPosition : ObjectManager.Player.Position));

                #region Screen

                if (_menu.Item(_menu.Name + ".spells.screen").GetValue<bool>() && position.IsValid() &&
                    !position.IsOnScreen())
                {
                    args.Process = false;
                    _blockedOrders++;
                    return;
                }

                #endregion Screen

                #region Checks

                if (_menu.Item(_menu.Name + ".spells.checks").GetValue<bool>())
                {
                    if (Utils.GameTimeTickCount - _lastSpellCast >=
                        _random.Next((int) (1000f * 0.975f), (int) (1000f * 1.025f)))
                    {
                        _isCasting = false;
                    }
                    if (MenuGUI.IsShopOpen || MenuGUI.IsChatOpen ||
                        ((_isCasting || ObjectManager.Player.Spellbook.IsCastingSpell) && isSpell) || !spell.IsReady())
                    {
                        args.Process = false;
                        _blockedSpells++;
                        return;
                    }
                }

                #endregion Checks

                #region Delay

                var defaultDelay = _menu.Item(_menu.Name + ".spells.delay").GetValue<Slider>().Value;
                var delay = Math.Max(
                    _random.Next((int) (defaultDelay * 0.85f), (int) (defaultDelay * 1.15f)),
                    _random.Next((int) (Game.Ping * 0.45f), (int) (Game.Ping * 0.55f)));
                var type = spell.SData.TargettingType.ToString();
                if (_targetTypes.Any(t => type.Contains(t)))
                {
                    delay = Math.Max(
                        delay,
                        GetRangeDelay(
                            position, _lastCastPosition,
                            _menu.Item(_menu.Name + ".spells.range-delay").GetValue<Slider>().Value));
                }

                if (Utils.GameTimeTickCount - (isSpell ? _lastSpellCast : _lastItemCast) <= delay)
                {
                    args.Process = false;
                    _blockedSpells++;
                    return;
                }

                #endregion Delay

                _lastCastPosition = position.IsValid() && _targetTypes.Any(t => type.Contains(t))
                    ? position
                    : Vector3.Zero;

                #region Randomize

                var randomPosition = _menu.Item(_menu.Name + ".spells.position").GetValue<Slider>().Value;
                if (randomPosition > 0 && args.Target == null && (type.Contains("Cone") || type.Contains("Location")) &&
                    !_randomizedSpells[args.Slot])
                {
                    var startPos = Vector3.Zero;
                    var endPos = Vector3.Zero;

                    if (args.StartPosition.IsValid())
                    {
                        startPos = _random.Randomize(args.StartPosition, randomPosition);
                    }
                    if (args.EndPosition.IsValid())
                    {
                        endPos = _random.Randomize(args.EndPosition, randomPosition);
                    }
                    if (startPos.IsValid() || endPos.IsValid())
                    {
                        args.Process = false;
                        _randomizedSpells[args.Slot] = true;
                        if (startPos.IsValid() && endPos.IsValid())
                        {
                            ObjectManager.Player.Spellbook.CastSpell(args.Slot, startPos, endPos);
                        }
                        else if (startPos.IsValid())
                        {
                            ObjectManager.Player.Spellbook.CastSpell(args.Slot, startPos);
                        }
                        else if (endPos.IsValid())
                        {
                            ObjectManager.Player.Spellbook.CastSpell(args.Slot, endPos);
                        }
                    }
                }

                #endregion Randomize

                _isCasting = true;
                if (isSpell)
                {
                    _lastSpellCast = Utils.GameTimeTickCount;
                }
                else
                {
                    _lastItemCast = Utils.GameTimeTickCount;
                }
            }
            catch (Exception ex)
            {
                args.Process = true;
                Console.WriteLine(ex);
            }
        }

        private int GetRangeDelay(Vector3 position, Vector3 lastPosition, int percent)
        {
            if (percent > 0 && position.IsValid() && lastPosition.IsValid())
            {
                var distance = position.Distance(lastPosition);
                if (Helpers.AngleBetween(lastPosition, position) > _random.Next(10, 16) && distance > 250)
                {
                    var rangeDelay = (int) ((distance * _random.Next(75, 86) / 100) / 100 * percent);
                    return Math.Min(_random.Next(1250, 1500), rangeDelay);
                }
            }
            return 0;
        }

        private void OnObjAiBaseIssueOrder(Obj_AI_Base sender, GameObjectIssueOrderEventArgs args)
        {
            try
            {
                if (!_menu.Item(_menu.Name + ".enabled").GetValue<bool>())
                {
                    return;
                }
                if ((sender.IsMe ||
                     ObjectManager.Player.Pet != null && ObjectManager.Player.Pet.NetworkId.Equals(sender.NetworkId)) &&
                    !args.IsAttackMove)
                {
                    if (!_randomizedOrders.ContainsKey(args.Order))
                    {
                        _randomizedOrders[args.Order] = false;
                    }
                    if (_randomizedOrders[args.Order])
                    {
                        _randomizedOrders[args.Order] = false;
                        return;
                    }
                    var position = args.Target != null ? args.Target.Position : args.TargetPosition;
                    if (_menu.Item(_menu.Name + ".orders.screen").GetValue<bool>() && !position.IsOnScreen())
                    {
                        args.Process = false;
                        _blockedOrders++;
                        return;
                    }
                    UpdateSequence(args.Order);
                    var sequence = _sequences[args.Order];
                    var delay = sequence.Items[sequence.Index];
                    var isSharpTurn = false;

                    if ((args.Order == GameObjectOrder.AttackTo || args.Order == GameObjectOrder.AttackUnit) &&
                        (_lastAttackTarget == null || args.Target == null ||
                         !_lastAttackTarget.NetworkId.Equals(args.Target.NetworkId)))
                    {
                        var percent = _menu.Item(_menu.Name + ".orders.range-delay").GetValue<Slider>().Value;
                        delay = Math.Max(
                            delay,
                            Math.Max(
                                GetRangeDelay(position, _lastAttackPosition, percent),
                                GetRangeDelay(position, _lastCastPosition, percent)));
                    }
                    else
                    {
                        isSharpTurn = _menu.Item(_menu.Name + ".orders.sharp-turn").GetValue<bool>() &&
                                      Helpers.IsSharpTurn(position, _random.Next(80, 101));
                    }

                    if (Utils.GameTimeTickCount - sequence.LastIndexChange <= (isSharpTurn ? delay / 2 : delay))
                    {
                        args.Process = false;
                        _blockedOrders++;
                    }
                    else
                    {
                        if (args.Order == GameObjectOrder.AttackTo || args.Order == GameObjectOrder.AttackUnit)
                        {
                            _lastAttackPosition = position.IsValid() ? position : Vector3.Zero;
                            _lastAttackTarget = args.Target;
                        }
                        sequence.Index++;
                        if (args.Target == null && position.IsValid() && !_randomizedOrders[args.Order])
                        {
                            args.Process = false;
                            _randomizedOrders[args.Order] = true;
                            ObjectManager.Player.IssueOrder(
                                args.Order,
                                _random.Randomize(
                                    position, _menu.Item(_menu.Name + ".orders.position").GetValue<Slider>().Value));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                args.Process = true;
                Console.WriteLine(ex);
            }
        }

        private int[] CreateSequence(int averageClicks)
        {
            var clicks = Math.Max(
                1, _random.Next((int) Math.Round(averageClicks * 0.85f, 0), (int) Math.Round(averageClicks * 1.15f, 0)));
            var second = _random.Next((int) (1000f * 0.975f), (int) (1000f * 1.025f));
            var average = second / clicks;
            var minValue = _random.Next((int) (average * 0.40f), (int) (average * 0.60f));
            var maxValue = _random.Next((int) (average * 1.10f), (int) (average * 1.30f));
            return _random.CreateSequence(clicks, minValue, maxValue, second);
        }

        private void UpdateSequence(GameObjectOrder order)
        {
            if (!_sequences.ContainsKey(order))
            {
                _sequences[order] = new Sequence();
            }
            var sequence = _sequences[order];
            if (sequence.Items == null || sequence.Index >= sequence.Items.Length)
            {
                sequence.Items = CreateSequence(_menu.Item(_menu.Name + ".orders.clicks").GetValue<Slider>().Value);
            }
        }
    }
}