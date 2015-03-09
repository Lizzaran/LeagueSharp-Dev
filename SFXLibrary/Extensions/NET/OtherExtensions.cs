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
    using System.Linq;

    #endregion

    public static class OtherExtensions
    {
        public static bool Is24Hrs(this CultureInfo cultureInfo)
        {
            return cultureInfo.DateTimeFormat.ShortTimePattern.Contains("H");
        }

        public static bool IsNumber(this object value)
        {
            return value is sbyte
                   || value is byte
                   || value is short
                   || value is ushort
                   || value is int
                   || value is uint
                   || value is long
                   || value is ulong
                   || value is float
                   || value is double
                   || value is decimal;
        }

        public static string ToDebugString<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
        {
            return dictionary == null
                ? string.Empty
                : string.Join("," + Environment.NewLine, dictionary.Select(kv => kv.Key + " = " + kv.Value));
        }
    }
}