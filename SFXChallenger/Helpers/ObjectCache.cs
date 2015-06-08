#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 ObjectCache.cs is part of SFXChallenger.

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
using SFXLibrary;
using SharpDX;

#endregion

namespace SFXChallenger.Helpers
{
    public class ObjectCache
    {
        private static readonly ConcurrentSet<Obj_AI_Minion> Minions = new ConcurrentSet<Obj_AI_Minion>();
        private static int _lastRefresh;
        private static readonly int RefreshInterval = 60 * 1000;
        private static int _lastCheck;
        private static readonly int CheckInterval = 10 * 1000;

        static ObjectCache()
        {
            foreach (var minion in ObjectManager.Get<Obj_AI_Minion>().Where(m => m.IsValid && !m.IsAlly && !m.IsDead))
            {
                Minions.Add(minion);
            }
            _lastRefresh = Environment.TickCount;

            GameObject.OnCreate += OnGameObjectCreate;
            GameObject.OnDelete += OnGameObjectDelete;
            Game.OnUpdate += OnGameUpdate;
        }

        private static void OnGameObjectCreate(GameObject sender, EventArgs args)
        {
            var minion = sender as Obj_AI_Minion;
            if (minion != null && minion.IsValid && !minion.IsAlly && !Minions.Contains(minion))
            {
                Minions.Add(minion);
            }
        }

        private static void OnGameObjectDelete(GameObject sender, EventArgs args)
        {
            var minion = sender as Obj_AI_Minion;
            if (minion != null && Minions.Contains(minion))
            {
                Minions.Remove(minion);
            }
        }

        private static void OnGameUpdate(EventArgs args)
        {
            if (_lastRefresh + RefreshInterval > Environment.TickCount)
            {
                Minions.Clear();
                foreach (
                    var minion in ObjectManager.Get<Obj_AI_Minion>().Where(m => m.IsValid && !m.IsAlly && !m.IsDead))
                {
                    Minions.Add(minion);
                }
                _lastRefresh = Environment.TickCount;
                _lastCheck = Environment.TickCount;
                return;
            }
            if (_lastCheck + CheckInterval > Environment.TickCount)
            {
                foreach (var minion in GetMinions().Where(m => m == null || !m.IsValid || m.IsDead))
                {
                    Minions.Remove(minion);
                }
                _lastCheck = Environment.TickCount;
            }
        }

        public static ConcurrentSet<Obj_AI_Minion> GetMinions()
        {
            return Minions;
        }

        public static List<Obj_AI_Base> GetMinions(Vector3 from,
            float range,
            MinionTypes type = MinionTypes.All,
            MinionTeam team = MinionTeam.Enemy,
            MinionOrderTypes order = MinionOrderTypes.Health)
        {
            var result = (from minion in GetMinions()
                where minion.IsValidTarget(range, false, @from)
                let minionTeam = minion.Team
                where
                    team == MinionTeam.Neutral && minionTeam == GameObjectTeam.Neutral ||
                    team == MinionTeam.Ally &&
                    minionTeam ==
                    (ObjectManager.Player.Team == GameObjectTeam.Chaos ? GameObjectTeam.Chaos : GameObjectTeam.Order) ||
                    team == MinionTeam.Enemy &&
                    minionTeam ==
                    (ObjectManager.Player.Team == GameObjectTeam.Chaos ? GameObjectTeam.Order : GameObjectTeam.Chaos) ||
                    team == MinionTeam.NotAlly && minionTeam != ObjectManager.Player.Team ||
                    team == MinionTeam.NotAllyForEnemy &&
                    (minionTeam == ObjectManager.Player.Team || minionTeam == GameObjectTeam.Neutral) ||
                    team == MinionTeam.All
                where
                    minion.IsMelee() && type == MinionTypes.Melee || !minion.IsMelee() && type == MinionTypes.Ranged ||
                    type == MinionTypes.All
                where IsMinion(minion) || minionTeam == GameObjectTeam.Neutral
                select minion).Cast<Obj_AI_Base>().ToList();

            switch (order)
            {
                case MinionOrderTypes.Health:
                    result = result.OrderBy(o => o.Health).ToList();
                    break;
                case MinionOrderTypes.MaxHealth:
                    result = result.OrderBy(o => o.MaxHealth).Reverse().ToList();
                    break;
            }

            return result;
        }

        public static List<Obj_AI_Base> GetMinions(float range,
            MinionTypes type = MinionTypes.All,
            MinionTeam team = MinionTeam.Enemy,
            MinionOrderTypes order = MinionOrderTypes.Health)
        {
            return GetMinions(ObjectManager.Player.ServerPosition, range, type, team, order);
        }

        public static bool IsMinion(Obj_AI_Minion minion, bool includeWards = false)
        {
            var name = minion.BaseSkinName.ToLower();
            return name.Contains("minion") || (includeWards && (name.Contains("ward") || name.Contains("trinket")));
        }
    }
}