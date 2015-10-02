#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 GapcloserManager.cs is part of SFXChallenger.

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
using SFXChallenger.Args;
using SFXChallenger.Library;
using SFXChallenger.Library.Extensions.NET;
using SFXChallenger.Library.Logger;
using SharpDX;

#endregion

namespace SFXChallenger.Managers
{
    internal static class GapcloserManager
    {
        private static readonly Dictionary<string, Menu> Menues = new Dictionary<string, Menu>();
        private static readonly Dictionary<int, float> LastTriggers = new Dictionary<int, float>();

        static GapcloserManager()
        {
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            CustomEvents.Unit.OnDash += OnUnitDash;
        }

        public static void AddToMenu(Menu menu, HeroListManagerArgs args, bool dangerous = false)
        {
            try
            {
                if (Menues.ContainsKey(args.UniqueId))
                {
                    throw new ArgumentException(
                        string.Format("GapcloserManager: UniqueID \"{0}\" already exist.", args.UniqueId));
                }

                args.Enemies = true;
                args.Allies = false;

                menu.AddItem(
                    new MenuItem(menu.Name + ".gap-" + args.UniqueId + ".delay", "Delay").SetValue(
                        new Slider(100, 0, 500)));
                menu.AddItem(
                    new MenuItem(menu.Name + ".gap-" + args.UniqueId + ".distance", "Min. Distance").SetValue(
                        new Slider(150, 0, 500)));
                menu.AddItem(
                    new MenuItem(menu.Name + ".gap-" + args.UniqueId + ".dangerous", "Only Dangerous").SetValue(
                        dangerous));

                menu.AddItem(new MenuItem(menu.Name + ".gap-" + args.UniqueId + ".separator", string.Empty));

                HeroListManager.AddToMenu(menu, args);

                Menues[args.UniqueId] = menu;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static event EventHandler<GapcloserManagerArgs> OnGapcloser;

        private static void OnUnitDash(Obj_AI_Base sender, Dash.DashItem args)
        {
            try
            {
                var hero = sender as Obj_AI_Hero;
                if (hero != null && hero.IsEnemy)
                {
                    Utility.DelayAction.Add(
                        100,
                        delegate
                        {
                            Check(
                                true, hero, args.StartPos.To3D(), args.EndPos.To3D(), (args.EndTick / 1000f) - 0.1f,
                                false);
                        });
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private static void OnEnemyGapcloser(ActiveGapcloser args)
        {
            try
            {
                if (args.Sender.IsEnemy)
                {
                    if (args.Sender.ChampionName.Equals("Pantheon", StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                    var endPos = args.End;
                    if (args.Sender.ChampionName.Equals("Fizz", StringComparison.OrdinalIgnoreCase))
                    {
                        endPos = args.Start.Extend(endPos, 550f);
                    }
                    else if (args.Sender.ChampionName.Equals("Yasuo", StringComparison.OrdinalIgnoreCase))
                    {
                        endPos = args.Start.Extend(endPos, 475f);
                    }
                    else if (args.Sender.ChampionName.Equals("Poppy", StringComparison.OrdinalIgnoreCase))
                    {
                        endPos = args.Start.Extend(endPos, 475f);
                    }
                    var spell = args.Sender.GetSpell(args.Slot);
                    if (args.Sender.ChampionName.Equals("Gragas", StringComparison.OrdinalIgnoreCase) &&
                        args.Slot == SpellSlot.E ||
                        args.Sender.ChampionName.Equals("Sejuani", StringComparison.OrdinalIgnoreCase) &&
                        args.Slot == SpellSlot.Q)
                    {
                        if (spell != null)
                        {
                            var colObjects =
                                GameObjects.AllyHeroes.Select(a => a as Obj_AI_Base)
                                    .Concat(
                                        GameObjects.AllyMinions.Where(m => m.Distance(args.Sender) <= 2000).ToList());
                            var rect = new Geometry.Polygon.Rectangle(
                                args.Start, endPos, spell.SData.LineWidth + args.Sender.BoundingRadius);
                            var collision =
                                colObjects.FirstOrDefault(
                                    col =>
                                        new Geometry.Polygon.Circle(col.ServerPosition, col.BoundingRadius).Points.Any(
                                            p => rect.IsInside(p)));
                            if (collision != null)
                            {
                                endPos = collision.ServerPosition.Extend(
                                    args.Sender.ServerPosition, args.Sender.BoundingRadius + collision.BoundingRadius);
                                if (collision is Obj_AI_Minion && endPos.Distance(args.Start) <= 100)
                                {
                                    return;
                                }
                            }
                        }
                    }
                    var endTime = Game.Time;
                    if (spell != null)
                    {
                        var time = args.Start.Distance(endPos) /
                                   Math.Max(spell.SData.MissileSpeed, args.Sender.MoveSpeed * 1.25f);
                        if (time <= 3)
                        {
                            endTime += time;
                        }
                    }
                    Check(false, args.Sender, args.Start, endPos, endTime, args.SkillType == GapcloserType.Targeted);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private static void Check(bool dash,
            Obj_AI_Hero sender,
            Vector3 startPosition,
            Vector3 endPosition,
            float endTime,
            bool targeted)
        {
            try
            {
                if (!sender.IsValid || !sender.IsEnemy || sender.IsDead)
                {
                    return;
                }
                if (Game.Time - endTime >= 5)
                {
                    return;
                }
                if (endPosition.Distance(ObjectManager.Player.ServerPosition) >= 2000)
                {
                    return;
                }
                float lastTrigger;
                if (LastTriggers.TryGetValue(sender.NetworkId, out lastTrigger))
                {
                    if (Game.Time - lastTrigger <= 1)
                    {
                        return;
                    }
                }
                LastTriggers[sender.NetworkId] = Game.Time;

                foreach (var entry in Menues)
                {
                    var uniqueId = entry.Key;
                    var menu = entry.Value;
                    if (HeroListManager.Check(entry.Key, sender))
                    {
                        var distance = menu.Item(menu.Name + ".gap-" + uniqueId + ".distance").GetValue<Slider>().Value;
                        var dangerous = menu.Item(menu.Name + ".gap-" + uniqueId + ".dangerous").GetValue<bool>();
                        if (startPosition.Distance(ObjectManager.Player.Position) >= distance &&
                            (!dangerous || IsDangerous(sender, startPosition, endPosition, targeted)))
                        {
                            var delay = menu.Item(menu.Name + ".gap-" + uniqueId + ".delay").GetValue<Slider>().Value;
                            Utility.DelayAction.Add(
                                Math.Max(1, dash ? delay - 100 : delay),
                                delegate
                                {
                                    OnGapcloser.RaiseEvent(
                                        null,
                                        new GapcloserManagerArgs(
                                            uniqueId, sender, startPosition, endPosition, endTime - (delay / 1000f)));
                                });
                        }
                    }
                }
                OnGapcloser.RaiseEvent(
                    null, new GapcloserManagerArgs(string.Empty, sender, startPosition, endPosition, endTime));
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private static bool IsDangerous(Obj_AI_Hero sender, Vector3 startPosition, Vector3 endPosition, bool targeted)
        {
            try
            {
                var endDistance = endPosition.Distance(ObjectManager.Player.Position);
                var startDistance = startPosition.Distance(ObjectManager.Player.Position);
                if (targeted)
                {
                    return true;
                }
                if (endDistance <= 150)
                {
                    return true;
                }
                if (endDistance - 150f < startDistance)
                {
                    var spell = sender.GetSpell(SpellSlot.R);
                    if (spell != null && endDistance <= 600)
                    {
                        return spell.Cooldown >= 25 && spell.IsReady(2500);
                    }
                    if (endDistance <= 500 && ObjectManager.Player.HealthPercent < 50)
                    {
                        return true;
                    }
                }
                if (endDistance > startDistance)
                {
                    return false;
                }
                if (endDistance >= 450)
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return true;
        }
    }
}