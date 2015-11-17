#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Aggro.cs is part of SFXTargetSelector.

 SFXTargetSelector is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXTargetSelector is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXTargetSelector. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;

#endregion

namespace SFXTargetSelector
{
    public static partial class TargetSelector
    {
        public static partial class Weights
        {
            public static class Aggro
            {
                static Aggro()
                {
                    Entries = new Dictionary<int, Entry>();
                    FadeTime = 10;
                    Obj_AI_Base.OnAggro += OnObjAiBaseAggro;
                }

                public static Dictionary<int, Entry> Entries { get; private set; }
                public static float FadeTime { get; set; }

                public static Entry GetSenderTargetEntry(Obj_AI_Base sender, Obj_AI_Base target)
                {
                    return
                        GetSenderEntries(sender)
                            .FirstOrDefault(entry => entry.Target.Hero.NetworkId.Equals(target.NetworkId));
                }

                public static IEnumerable<Entry> GetSenderEntries(Obj_AI_Base sender)
                {
                    return Entries.Where(i => i.Key.Equals(sender.NetworkId)).Select(i => i.Value);
                }

                public static IEnumerable<Entry> GetTargetEntries(Obj_AI_Base target)
                {
                    return
                        Entries.Where(i => i.Value.Target.Hero.NetworkId.Equals(target.NetworkId)).Select(i => i.Value);
                }

                private static void OnObjAiBaseAggro(Obj_AI_Base sender, GameObjectAggroEventArgs args)
                {
                    if (!sender.IsEnemy || Modes.Current.Mode != Mode.Weights)
                    {
                        return;
                    }
                    var hero = sender as Obj_AI_Hero;
                    var target = Targets.Items.FirstOrDefault(h => h.Hero.NetworkId == args.NetworkId);
                    if (hero != null && target != null)
                    {
                        Entry aggro;
                        if (Entries.TryGetValue(hero.NetworkId, out aggro))
                        {
                            aggro.Target = target;
                        }
                        else
                        {
                            Entries[target.Hero.NetworkId] = new Entry(hero, target);
                        }
                    }
                }

                public class Entry
                {
                    public Entry(Obj_AI_Hero sender, Targets.Item target)
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
    }
}