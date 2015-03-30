#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 OtherExtensions.cs is part of SFXLibrary.

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

namespace SFXLibrary.Extensions.NET
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Threading.Tasks;

    #endregion

    public static class OtherExtensions
    {
        public static bool Is24Hrs(this CultureInfo cultureInfo)
        {
            return cultureInfo.DateTimeFormat.ShortTimePattern.Contains("H");
        }

        public static bool IsNumber(this object value)
        {
            return value is sbyte || value is byte || value is short || value is ushort || value is int || value is uint || value is long ||
                   value is ulong || value is float || value is double || value is decimal;
        }

        public static string ToDebugString<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
        {
            return dictionary == null ? string.Empty : string.Join("," + Environment.NewLine, dictionary.Select(kv => kv.Key + " = " + kv.Value));
        }

        public static Task<List<T>> ToListAsync<T>(this IQueryable<T> list)
        {
            return Task.Run(() => list.ToList());
        }

        public static IEnumerable<T> Randomize<T>(this IEnumerable<T> target)
        {
            var r = new Random();
            return target.OrderBy(x => (r.Next()));
        }

        public static T DeepClone<T>(this T input) where T : ISerializable
        {
            using (var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                try
                {
                    formatter.Serialize(stream, input);
                    stream.Position = 0;
                    return (T) formatter.Deserialize(stream);
                }
                catch
                {
                    return default(T);
                }
            }
        }

        /// <exception cref="Exception">A delegate callback throws an exception. </exception>
        public static void RaiseEvent(this EventHandler @event, object sender, EventArgs e)
        {
            if (@event != null)
                @event(sender, e);
        }

        /// <exception cref="Exception">A delegate callback throws an exception. </exception>
        public static void RaiseEvent<T>(this EventHandler<T> @event, object sender, T e) where T : EventArgs
        {
            if (@event != null)
                @event(sender, e);
        }
    }
}