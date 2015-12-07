#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 DynamicInitializer.cs is part of ChampionTemplate.

 ChampionTemplate is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 ChampionTemplate is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with ChampionTemplate. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Reflection.Emit;

#endregion

namespace ChampionTemplate.Helpers
{
    /*
     * Here happpens some magic to create an instace of a class.
     * Needed because Sandbox blocks Activator / Reflection 
     */

    public class DynamicInitializer
    {
        public static TV NewInstance<TV>() where TV : class
        {
            return ObjectGenerator(typeof(TV)) as TV;
        }

        public static object NewInstance(Type type)
        {
            return ObjectGenerator(type);
        }

        private static object ObjectGenerator(Type type)
        {
            var target = type.GetConstructor(Type.EmptyTypes);
            if (target != null && target.DeclaringType != null)
            {
                var dynamic = new DynamicMethod(string.Empty, type, new Type[0], target.DeclaringType);
                var il = dynamic.GetILGenerator();
                il.DeclareLocal(target.DeclaringType);
                il.Emit(OpCodes.Newobj, target);
                il.Emit(OpCodes.Stloc_0);
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Ret);

                var method = (Func<object>) dynamic.CreateDelegate(typeof(Func<object>));
                return method();
            }
            return null;
        }
    }
}