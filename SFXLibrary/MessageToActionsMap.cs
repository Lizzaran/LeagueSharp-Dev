#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 MessageToActionsMap.cs is part of SFXLibrary.

 SFXLibrary is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXLibrary is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXLibrary. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Collections.Generic;

#endregion

namespace SFXLibrary
{
    public class MessageToActionsMap
    {
        private readonly Dictionary<object, List<WeakAction>> _map;

        public MessageToActionsMap()
        {
            _map = new Dictionary<object, List<WeakAction>>();
        }

        public void AddAction(object message, Action<object> callback)
        {
            if (!_map.ContainsKey(message))
            {
                _map[message] = new List<WeakAction>();
            }

            _map[message].Add(new WeakAction(callback));
        }

        public List<Action<object>> GetActions(object message)
        {
            if (!_map.ContainsKey(message))
            {
                return null;
            }

            var weakActions = _map[message];
            var actions = new List<Action<object>>();
            for (var i = weakActions.Count - 1; i > -1; --i)
            {
                var weakAction = weakActions[i];
                if (!weakAction.IsAlive)
                {
                    weakActions.RemoveAt(i);
                }
                else
                {
                    actions.Add(weakAction.CreateAction());
                }
            }

            RemoveMessageIfNecessary(weakActions, message);

            return actions;
        }

        private void RemoveMessageIfNecessary(List<WeakAction> weakActions, object message)
        {
            if (weakActions.Count == 0)
            {
                _map.Remove(message);
            }
        }
    }
}