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
        private readonly CryptoRandom _random = new CryptoRandom();
        private readonly Dictionary<GameObjectOrder, Sequence> _sequences = new Dictionary<GameObjectOrder, Sequence>();

        private readonly List<SpellSlot> _spells = new List<SpellSlot>
        {
            SpellSlot.Q,
            SpellSlot.W,
            SpellSlot.E,
            SpellSlot.R,
            SpellSlot.Item1,
            SpellSlot.Item2,
            SpellSlot.Item3,
            SpellSlot.Item4,
            SpellSlot.Item5,
            SpellSlot.Item6,
            SpellSlot.Trinket
        };

        private int _blockedOrders;
        private int _blockedSpells;
        private Font _font;
        private bool _isCasting;
        private int _lastSpellCast;
        private Vector3 _lastSpellPosition;
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
                        new Slider(Drawing.Width - 25, 0, Drawing.Width)));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + ".size", "Font Size").SetValue(new Slider(20, 5, 30)));
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + ".color", "Color").SetValue(Color.Lime));
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + ".spells", "Blocked Spells").SetValue(true));
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + ".orders", "Blocked Orders").SetValue(true));

                var spellMenu = _menu.AddSubMenu(new Menu("Spells", _menu.Name + ".spells"));
                spellMenu.AddItem(new MenuItem(spellMenu.Name + ".delay", "Average Delay").SetValue(new Slider(100, 0, 500)));
                spellMenu.AddItem(new MenuItem(spellMenu.Name + ".range-delay", "Dynamic Range Delay %").SetValue(new Slider(100, 0, 200)));
                spellMenu.AddItem(new MenuItem(spellMenu.Name + ".position", "Randomized Position").SetValue(new Slider(10, 0, 50)));
                spellMenu.AddItem(new MenuItem(spellMenu.Name + ".checks", "Additional Checks").SetValue(true));
                spellMenu.AddItem(new MenuItem(spellMenu.Name + ".screen", "Block Offscreen").SetValue(false));

                var orderMenu = _menu.AddSubMenu(new Menu("Orders", _menu.Name + ".orders"));
                orderMenu.AddItem(
                    new MenuItem(orderMenu.Name + ".clicks", "Max. Average Per Second").SetValue(new Slider(10, 1, 20)));
                orderMenu.AddItem(
                    new MenuItem(orderMenu.Name + ".position", "Randomized Position").SetValue(new Slider(20, 0, 50)));
                orderMenu.AddItem(new MenuItem(orderMenu.Name + ".sharp-turn", "Check Sharp Turns").SetValue(true));
                orderMenu.AddItem(new MenuItem(orderMenu.Name + ".screen", "Block Offscreen").SetValue(false));

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
                if (sender.IsMe && (args.SData.IsAutoAttack() || _spells.Any(s => s.Equals(args.Slot))))
                {
                    _isCasting = false;
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
                if (!_menu.Item(_menu.Name + ".enabled").GetValue<bool>())
                {
                    return;
                }
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

        private void OnSpellbookCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            try
            {
                if (!_menu.Item(_menu.Name + ".enabled").GetValue<bool>())
                {
                    return;
                }
                if (!sender.Owner.IsMe || !_spells.Any(s => s.Equals(args.Slot)))
                {
                    return;
                }

                var spell = ObjectManager.Player.Spellbook.GetSpell(args.Slot);
                if (spell == null)
                {
                    return;
                }

                var position = args.Target != null
                    ? args.Target.Position
                    : (args.StartPosition.IsValid() ? args.StartPosition : args.EndPosition);

                if (_menu.Item(_menu.Name + ".spells.screen").GetValue<bool>() && position.IsValid() &&
                    !position.IsOnScreen())
                {
                    args.Process = false;
                    _blockedOrders++;
                    return;
                }

                if (_menu.Item(_menu.Name + ".spells.checks").GetValue<bool>())
                {
                    if (Utils.GameTimeTickCount - _lastSpellCast >=
                        _random.Next((int) (1000f * 0.975f), (int) (1000f * 1.025f)))
                    {
                        _isCasting = false;
                    }
                    if (MenuGUI.IsShopOpen || MenuGUI.IsChatOpen || _isCasting ||
                        ObjectManager.Player.Spellbook.IsCastingSpell || !spell.IsReady())
                    {
                        args.Process = false;
                        _blockedSpells++;
                        return;
                    }
                }

                var defaultDelay = _menu.Item(_menu.Name + ".spells.delay").GetValue<Slider>().Value;
                var delay = Math.Max(
                    _random.Next((int) (defaultDelay * 0.85f), (int) (defaultDelay * 1.15f)),
                    _random.Next((int) (Game.Ping * 0.45f), (int) (Game.Ping * 0.55f)));
                var type = spell.SData.TargettingType;
                var rangeDelayPercent = _menu.Item(_menu.Name + ".spells.range-delay").GetValue<Slider>().Value;
                if (rangeDelayPercent > 0 && _lastSpellPosition.IsValid())
                {
                    if (type == SpellDataTargetType.Cone || type.ToString().Contains("Location") ||
                        type.ToString().Contains("Unit"))
                    {
                        if (position.IsValid() &&
                            (Helpers.AngleBetween(_lastSpellPosition, position) > _random.Next(8, 13) ||
                             type.ToString().Contains("Unit")))
                        {
                            var rangeDelay =
                                (int)
                                    ((_lastSpellPosition.Distance(position) * _random.Next(75, 86) / 100) / 100 *
                                     rangeDelayPercent);
                            delay = Math.Min(_random.Next(1400, 1500), Math.Max(delay, rangeDelay));
                        }
                    }
                }

                if (Utils.GameTimeTickCount - _lastSpellCast <= delay)
                {
                    args.Process = false;
                    _blockedSpells++;
                    return;
                }

                _isCasting = true;
                _lastSpellCast = Utils.GameTimeTickCount;

                if (type == SpellDataTargetType.Cone || type.ToString().Contains("Location") ||
                    type.ToString().Contains("Unit"))
                {
                    _lastSpellPosition = args.Target != null
                        ? args.Target.Position
                        : (args.StartPosition.IsValid() ? args.StartPosition : args.EndPosition);
                }

                if (args.Target == null && (type == SpellDataTargetType.Cone || type.ToString().Contains("Location")))
                {
                    var startPos = Vector3.Zero;
                    var endPos = Vector3.Zero;
                    var randomPosition = _menu.Item(_menu.Name + ".spells.position").GetValue<Slider>().Value;
                    if (args.StartPosition.IsValid())
                    {
                        startPos = _random.Randomize(args.StartPosition, randomPosition);
                    }
                    if (args.EndPosition.IsValid())
                    {
                        endPos = _random.Randomize(args.EndPosition, randomPosition);
                    }
                    if (startPos.IsValid() && endPos.IsValid())
                    {
                        ObjectManager.Player.Spellbook.CastSpell(args.Slot, startPos, endPos, false);
                        args.Process = false;
                    }
                    else if (startPos.IsValid())
                    {
                        ObjectManager.Player.Spellbook.CastSpell(args.Slot, startPos, false);
                        args.Process = false;
                    }
                    else if (endPos.IsValid())
                    {
                        ObjectManager.Player.Spellbook.CastSpell(args.Slot, endPos, false);
                        args.Process = false;
                    }
                }
            }
            catch (Exception ex)
            {
                args.Process = true;
                Console.WriteLine(ex);
            }
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
                    var position = args.Target != null ? args.Target.Position : args.TargetPosition;
                    if (_menu.Item(_menu.Name + ".orders.screen").GetValue<bool>() && !position.IsOnScreen())
                    {
                        args.Process = false;
                        _blockedOrders++;
                        return;
                    }
                    UpdateSequence(args.Order);
                    var sequence = _sequences[args.Order];
                    var isSharpTurn = _menu.Item(_menu.Name + ".orders.sharp-turn").GetValue<bool>() &&
                                      Helpers.IsSharpTurn(position, _random.Next(80, 101));
                    if (Utils.GameTimeTickCount - sequence.LastIndexChange <=
                        (isSharpTurn ? sequence.Items[sequence.Index] / 2 : sequence.Items[sequence.Index]))
                    {
                        args.Process = false;
                        _blockedOrders++;
                    }
                    else
                    {
                        sequence.Index++;
                        if (args.Target == null && args.TargetPosition.IsValid())
                        {
                            ObjectManager.Player.IssueOrder(
                                args.Order,
                                _random.Randomize(
                                    args.TargetPosition,
                                    _menu.Item(_menu.Name + ".orders.position").GetValue<Slider>().Value), false);
                            args.Process = false;
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
            if (sequence.Items == null || sequence.Index >= sequence.Items.Length ||
                Utils.GameTimeTickCount - sequence.LastItemsChange >
                _random.Next((int) (1000f * 0.975f), (int) (1000f * 1.025f)))
            {
                sequence.Items = CreateSequence(_menu.Item(_menu.Name + ".orders.clicks").GetValue<Slider>().Value);
            }
        }
    }
}