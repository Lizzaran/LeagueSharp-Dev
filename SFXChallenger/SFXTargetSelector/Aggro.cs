#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Aggro.cs is part of SFXChallenger.

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
using SFXChallenger.Enumerations;
using SFXChallenger.Library.Logger;

#endregion

namespace SFXChallenger.SFXTargetSelector
{
    public static class Aggro
    {
        static Aggro()
        {
            Items = new Dictionary<int, Item>();
            FadeTime = 10;
            Obj_AI_Base.OnAggro += OnObjAiBaseAggro;
        }

        public static Dictionary<int, Item> Items { get; private set; }
        public static float FadeTime { get; set; }

        public static Item GetSenderTargetEntry(Obj_AI_Base sender, Obj_AI_Base target)
        {
            return GetSenderEntries(sender)
                .FirstOrDefault(entry => entry.Target.Hero.NetworkId.Equals(target.NetworkId));
        }

        public static IEnumerable<Item> GetSenderEntries(Obj_AI_Base sender)
        {
            return Items.Where(i => i.Key.Equals(sender.NetworkId)).Select(i => i.Value);
        }

        public static IEnumerable<Item> GetTargetEntries(Obj_AI_Base target)
        {
            return Items.Where(i => i.Value.Target.Hero.NetworkId.Equals(target.NetworkId)).Select(i => i.Value);
        }

        private static void OnObjAiBaseAggro(Obj_AI_Base sender, GameObjectAggroEventArgs args)
        {
            try
            {
                if (!sender.IsEnemy || TargetSelector.Mode != TargetSelectorModeType.Weights)
                {
                    return;
                }
                var hero = sender as Obj_AI_Hero;
                var target = Targets.Items.FirstOrDefault(h => h.Hero.NetworkId == args.NetworkId);
                if (hero != null && target != null)
                {
                    Item aggro;
                    if (Items.TryGetValue(hero.NetworkId, out aggro))
                    {
                        aggro.Target = target;
                    }
                    else
                    {
                        Items[target.Hero.NetworkId] = new Item(hero, target);
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public class Item
        {
            public Item(Obj_AI_Hero sender, Targets.Item target)
            {
                Sender = sender;
                Target = target;
                Timestamp = Game.Time;
            }

            public float Value
            {
                get { return Math.Max(0f, FadeTime - (Game.Time - Timestamp)); }
            }

            public Obj_AI_Hero Sender { get; set; }
            public Targets.Item Target { get; set; }
            public float Timestamp { get; private set; }
        }
    }
}