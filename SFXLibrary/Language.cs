#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Language.cs is part of SFXLibrary.

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

namespace SFXLibrary
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;

    #endregion

    public class Language
    {
        private static readonly Dictionary<string, Dictionary<string, string>> LanguagesDictionary =
            new Dictionary<string, Dictionary<string, string>>();

        public static List<string> Languages = new List<string>();
        public static string Default { get; set; }
        public static string Current { get; set; }

        public static void Parse(string xml)
        {
            try
            {
                var language = XDocument.Parse(xml).Root;
                if (language != null)
                {
                    var lang = language.Attribute("lang").Value;
                    if (!Languages.Contains(lang))
                    {
                        Languages.Add(lang);
                    }
                    var entries = new Dictionary<string, string>();
                    foreach (var entry in language.Descendants("entry"))
                    {
                        entries[entry.Attribute("key").Value] = entry.Value;
                    }
                    LanguagesDictionary[lang] = entries;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public static string Get(string key)
        {
            Dictionary<string, string> entries;
            if (LanguagesDictionary.TryGetValue(Current, out entries))
            {
                string value;
                if (entries.TryGetValue(key, out value))
                {
                    return value;
                }
            }
            return string.Format("[{0}]", key);
        }
    }
}