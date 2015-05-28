#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 WeakAction.cs is part of SFXLibrary.

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
using System.Reflection;

#endregion

namespace SFXLibrary
{
    [Serializable]
    public class WeakAction : WeakReference
    {
        private readonly MethodInfo _method;

        public WeakAction(Action<object> action) : base(action.Target)
        {
            try
            {
                _method = action.Method;
            }
            catch (MemberAccessException memberAccessException)
            {
                Console.WriteLine(memberAccessException);
            }
        }

        public Action<object> CreateAction()
        {
            if (!IsAlive)
            {
                return null;
            }

            try
            {
                return Delegate.CreateDelegate(typeof(Action<object>), Target, _method.Name) as Action<object>;
            }
            catch
            {
                return null;
            }
        }
    }
}