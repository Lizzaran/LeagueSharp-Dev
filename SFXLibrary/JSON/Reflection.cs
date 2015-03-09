#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Reflection.cs is part of SFXLibrary.

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

#endregion

namespace SFXLibrary.JSON
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;

    #endregion

    internal struct Getters
    {
        public Reflection.GenericGetter Getter;
        public string LcName;
        public string Name;
    }

    internal sealed class Reflection
    {
        private static readonly Reflection _instance = new Reflection();
        private SafeDictionary<Type, Type> _genericTypeDef = new SafeDictionary<Type, Type>();
        private SafeDictionary<Type, Getters[]> _getterscache = new SafeDictionary<Type, Getters[]>();
        private SafeDictionary<Type, string> _tyname = new SafeDictionary<Type, string>();

        private Reflection()
        {
        }

        public static Reflection Instance
        {
            get { return _instance; }
        }

        public Type GetGenericTypeDefinition(Type t)
        {
            Type tt;
            if (_genericTypeDef.TryGetValue(t, out tt))
                return tt;
            tt = t.GetGenericTypeDefinition();
            _genericTypeDef.Add(t, tt);
            return tt;
        }

        internal void ClearReflectionCache()
        {
            _tyname = new SafeDictionary<Type, string>();
            _getterscache = new SafeDictionary<Type, Getters[]>();
            _genericTypeDef = new SafeDictionary<Type, Type>();
        }

        internal delegate object GenericSetter(object target, object value);

        internal delegate object GenericGetter(object obj);

        #region json custom types

        internal SafeDictionary<Type, Serialize> CustomSerializer = new SafeDictionary<Type, Serialize>();

        internal void RegisterCustomType(Type type, Serialize serializer)
        {
            if (type != null && serializer != null)
            {
                CustomSerializer.Add(type, serializer);
            }
        }

        internal bool IsTypeRegistered(Type t)
        {
            if (CustomSerializer.Count == 0)
                return false;
            Serialize s;
            return CustomSerializer.TryGetValue(t, out s);
        }

        #endregion json custom types

        #region [   PROPERTY GET SET   ]

        internal string GetTypeAssemblyName(Type t)
        {
            string val;
            if (_tyname.TryGetValue(t, out val))
                return val;
            var s = t.AssemblyQualifiedName;
            _tyname.Add(t, s);
            return s;
        }

        internal static GenericGetter CreateGetField(Type type, FieldInfo fieldInfo)
        {
            var dynamicGet = new DynamicMethod("_", typeof (object), new[] {typeof (object)}, type);

            var il = dynamicGet.GetILGenerator();

            if (!type.IsClass)
            {
                var lv = il.DeclareLocal(type);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Unbox_Any, type);
                il.Emit(OpCodes.Stloc_0);
                il.Emit(OpCodes.Ldloca_S, lv);
                il.Emit(OpCodes.Ldfld, fieldInfo);
                if (fieldInfo.FieldType.IsValueType)
                    il.Emit(OpCodes.Box, fieldInfo.FieldType);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, fieldInfo);
                if (fieldInfo.FieldType.IsValueType)
                    il.Emit(OpCodes.Box, fieldInfo.FieldType);
            }

            il.Emit(OpCodes.Ret);

            return (GenericGetter) dynamicGet.CreateDelegate(typeof (GenericGetter));
        }

        internal static GenericGetter CreateGetMethod(Type type, PropertyInfo propertyInfo)
        {
            var getMethod = propertyInfo.GetGetMethod();
            if (getMethod == null)
                return null;

            var getter = new DynamicMethod("_", typeof (object), new[] {typeof (object)}, type);

            var il = getter.GetILGenerator();

            if (!type.IsClass)
            {
                var lv = il.DeclareLocal(type);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Unbox_Any, type);
                il.Emit(OpCodes.Stloc_0);
                il.Emit(OpCodes.Ldloca_S, lv);
                il.EmitCall(OpCodes.Call, getMethod, null);
                if (propertyInfo.PropertyType.IsValueType)
                    il.Emit(OpCodes.Box, propertyInfo.PropertyType);
            }
            else
            {
                if (!getMethod.IsStatic)
                {
                    il.Emit(OpCodes.Ldarg_0);
                    if (propertyInfo.DeclaringType != null) il.Emit(OpCodes.Castclass, propertyInfo.DeclaringType);
                    il.EmitCall(OpCodes.Callvirt, getMethod, null);
                }
                else
                    il.Emit(OpCodes.Call, getMethod);

                if (propertyInfo.PropertyType.IsValueType)
                    il.Emit(OpCodes.Box, propertyInfo.PropertyType);
            }

            il.Emit(OpCodes.Ret);

            return (GenericGetter) getter.CreateDelegate(typeof (GenericGetter));
        }

        internal Getters[] GetGetters(Type type, bool showReadOnlyProperties, List<Type> ignoreAttributes)
        {
            Getters[] val;
            if (_getterscache.TryGetValue(type, out val))
                return val;

            var props =
                type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                   BindingFlags.Static);
            var getters = new List<Getters>();
            foreach (
                var p in
                    props.Where(p => p.GetIndexParameters().Length <= 0)
                        .Where(p => p.CanWrite || showReadOnlyProperties))
            {
                if (ignoreAttributes != null)
                {
                    var found = ignoreAttributes.Any(ignoreAttr => p.IsDefined(ignoreAttr, false));
                    if (found)
                        continue;
                }
                var g = CreateGetMethod(type, p);
                if (g != null)
                    getters.Add(new Getters {Getter = g, Name = p.Name, LcName = p.Name.ToLower()});
            }

            var fi =
                type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public |
                               BindingFlags.Static);
            foreach (var f in fi)
            {
                if (ignoreAttributes != null)
                {
                    var found = ignoreAttributes.Any(ignoreAttr => f.IsDefined(ignoreAttr, false));
                    if (found)
                        continue;
                }
                if (f.IsLiteral == false)
                {
                    var g = CreateGetField(type, f);
                    if (g != null)
                        getters.Add(new Getters {Getter = g, Name = f.Name, LcName = f.Name.ToLower()});
                }
            }
            val = getters.ToArray();
            _getterscache.Add(type, val);
            return val;
        }

        #endregion [   PROPERTY GET SET   ]
    }
}