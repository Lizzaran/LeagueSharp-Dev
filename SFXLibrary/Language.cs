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

#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

#endregion

namespace SFXLibrary
{
    public class Language
    {
        private readonly Dictionary<string, Dictionary<string, string>> _languagesDictionary =
            new Dictionary<string, Dictionary<string, string>>();

        public List<string> Languages = new List<string>();
        public string Default { get; set; }
        public string Current { get; set; }

        public void Parse(string xml)
        {
            try
            {
                var entries = new Dictionary<string, string>();
                string language = null;
                using (var reader = XmlReader.Create(new StringReader(xml)))
                {
                    while (reader.Read())
                    {
                        switch (reader.Name)
                        {
                            case "language":
                                var lang = reader["lang"];
                                if (lang != null)
                                {
                                    language = lang;
                                }
                                break;
                            case "entry":
                                var key = reader["key"];
                                if (key != null)
                                {
                                    if (reader.Read())
                                    {
                                        entries[key] = reader.Value;
                                    }
                                }
                                break;
                        }
                    }
                }
                if (!string.IsNullOrEmpty(language))
                {
                    if (!Languages.Contains(language))
                    {
                        Languages.Add(language);
                    }
                    _languagesDictionary[language] = entries;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public string Get(string key, string overrideLanguage = null)
        {
            Dictionary<string, string> entries;
            var language = string.IsNullOrEmpty(overrideLanguage) ? Current : overrideLanguage;
            if (_languagesDictionary.TryGetValue(language, out entries))
            {
                string value;
                if (entries.TryGetValue(key, out value))
                {
                    return value;
                }
            }
            if (language != Default)
            {
                return Get(key, Default);
            }
            return string.Format("[{0}]", key);
        }

        public string[] GetList(string key, char separator = '|', string overrideLanguage = null)
        {
            return Get(key, overrideLanguage).Split(separator).ToArray();
        }
    }
}