#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 GenericExtensions.cs is part of SFXLibrary.

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
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Threading.Tasks;

    #endregion

    internal static class GenericExtensions
    {
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
    }
}