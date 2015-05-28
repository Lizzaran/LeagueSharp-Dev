#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Mediator.cs is part of SFXLibrary.

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

#endregion

namespace SFXLibrary
{
    public class Mediator
    {
        private readonly MessageToActionsMap _messageToCallbacksMap;

        public Mediator()
        {
            _messageToCallbacksMap = new MessageToActionsMap();
        }

        public void NotifyColleagues(object from, object message)
        {
            try
            {
                var actions = _messageToCallbacksMap.GetActions(from);
                if (actions != null)
                {
                    actions.ForEach(action => action(message));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public void Register(object from, Action<object> callback)
        {
            _messageToCallbacksMap.AddAction(from, callback);
        }
    }
}