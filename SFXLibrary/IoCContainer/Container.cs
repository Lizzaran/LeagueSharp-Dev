#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Container.cs is part of SFXLibrary.

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
using System.Linq;

#endregion

namespace SFXLibrary.IoCContainer
{
    public class Container : IContainer
    {
        public readonly Dictionary<MappingKey, Func<object>> Mappings;

        public Container()
        {
            Mappings = new Dictionary<MappingKey, Func<object>>();
        }

        public void Deregister(Type type, string instanceName = null)
        {
            var key = new MappingKey(type, default(bool), instanceName);
            Func<object> obj;
            if (Mappings.TryGetValue(key, out obj))
            {
                try
                {
                    Mappings.Remove(Mappings.FirstOrDefault(x => x.Value == obj).Key);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        public void Deregister<T>(string instanceName = null)
        {
            Deregister(typeof(T), instanceName);
        }

        public bool IsRegistered(Type type, string instanceName = null)
        {
            return type != null && Mappings.ContainsKey(new MappingKey(type, default(bool), instanceName));
        }

        public bool IsRegistered<T>(string instanceName = null)
        {
            return IsRegistered(typeof(T), instanceName);
        }

        /// <exception cref="InvalidOperationException">Condition. </exception>
        /// <exception cref="ArgumentNullException">The value of 'to' cannot be null. </exception>
        public void Register(Type from,
            Type to,
            bool singleton = false,
            bool initialize = false,
            string instanceName = null)
        {
            if (to == null)
            {
                throw new ArgumentNullException("to");
            }

            if (!from.IsAssignableFrom(to))
            {
                throw new InvalidOperationException(
                    string.Format(
                        "Error trying to register the instance: '{0}' is not assignable from '{1}'", from.FullName,
                        to.FullName));
            }
            //Register(from, CustomActivator.FuncGenerator(to), singleton, initialize, instanceName);
        }

        public void Register<TFrom, TTo>(bool singleton = false, bool initialize = false, string instanceName = null)
            where TTo : TFrom
        {
            Register(typeof(TFrom), typeof(TTo), singleton, initialize, instanceName);
        }

        /// <exception cref="ArgumentNullException">The value of 'type' cannot be null. </exception>
        public void Register(Type type,
            Func<object> createInstanceDelegate,
            bool singleton = false,
            bool initialize = false,
            string instanceName = null)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (createInstanceDelegate == null)
            {
                throw new ArgumentNullException("createInstanceDelegate");
            }

            var key = new MappingKey(type, singleton, instanceName);

            if (!Mappings.ContainsKey(key))
            {
                if (initialize)
                {
                    try
                    {
                        if (singleton)
                        {
                            key.Instance = createInstanceDelegate();
                        }
                        else
                        {
                            createInstanceDelegate();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
                Mappings.Add(key, createInstanceDelegate);
            }
        }

        /// <exception cref="ArgumentNullException">The value of 'createInstanceDelegate' cannot be null. </exception>
        public void Register<T>(Func<T> createInstanceDelegate,
            bool singleton = false,
            bool initialize = false,
            string instanceName = null)
        {
            if (createInstanceDelegate == null)
            {
                throw new ArgumentNullException("createInstanceDelegate");
            }
            Register(typeof(T), createInstanceDelegate as Func<object>, singleton, initialize, instanceName);
        }

        /// <exception cref="InvalidOperationException">Condition. </exception>
        public object Resolve(Type type, string instanceName = null)
        {
            var key = new MappingKey(type, default(bool), instanceName);
            Func<object> obj;
            try
            {
                if (Mappings.TryGetValue(key, out obj))
                {
                    var mk = Mappings.FirstOrDefault(x => x.Value == obj).Key;

                    if (mk.Singleton)
                    {
                        return mk.Instance ?? (mk.Instance = obj());
                    }
                    return obj();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            throw new InvalidOperationException(string.Format("Could not find mapping for type '{0}'", type.FullName));
        }

        public T Resolve<T>(string instanceName = null)
        {
            return (T) Resolve(typeof(T), instanceName);
        }

        public override string ToString()
        {
            return Mappings == null ? "No mappings" : string.Join(Environment.NewLine, Mappings.Keys);
        }
    }
}