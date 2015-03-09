#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 SafeDictionary.cs is part of SFXLibrary.

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

namespace SFXLibrary.JSON
{
    #region

    using System.Collections.Generic;

    #endregion

    public sealed class SafeDictionary<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> _dictionary;
        private readonly object _padlock = new object();

        public SafeDictionary(int capacity)
        {
            _dictionary = new Dictionary<TKey, TValue>(capacity);
        }

        public SafeDictionary()
        {
            _dictionary = new Dictionary<TKey, TValue>();
        }

        public int Count
        {
            get { lock (_padlock) return _dictionary.Count; }
        }

        public TValue this[TKey key]
        {
            get
            {
                lock (_padlock)
                    return _dictionary[key];
            }
            set
            {
                lock (_padlock)
                    _dictionary[key] = value;
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (_padlock)
                return _dictionary.TryGetValue(key, out value);
        }

        public void Add(TKey key, TValue value)
        {
            lock (_padlock)
            {
                if (_dictionary.ContainsKey(key) == false)
                    _dictionary.Add(key, value);
            }
        }
    }
}