#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 JSON.cs is part of SFXLibrary.

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

    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;
    using Extensions.NET;

    #endregion

    public delegate string Serialize(object data);

    public sealed class JSONParameters
    {
        /// <summary>
        ///     Serialize DateTime milliseconds i.e. yyyy-MM-dd HH:mm:ss.nnn (default = false)
        /// </summary>
        public bool DateTimeMilliseconds = false;

        /// <summary>
        ///     Anonymous types have read only properties
        /// </summary>
        public bool EnableAnonymousTypes = false;

        /// <summary>
        ///     Enable to filter sensitive data (default = False)
        /// </summary>
        public bool FilterSensitiveData = true;

        /// <summary>
        ///     Ignore attributes to check for (default : XmlIgnoreAttribute)
        /// </summary>
        public List<Type> IgnoreAttributes = new List<Type> {typeof (XmlIgnoreAttribute)};

        /// <summary>
        ///     Inline circular or already seen objects instead of replacement with $i (default = False)
        /// </summary>
        public bool InlineCircularReferences;

        /// <summary>
        ///     Output string key dictionaries as "k"/"v" format (default = False)
        /// </summary>
        public bool KvStyleStringDictionary = false;

        /// <summary>
        ///     If you have parametric and no default constructor for you classes (default = False)
        ///     IMPORTANT NOTE : If True then all initial values within the class will be ignored and will be not set
        /// </summary>
        public bool ParametricConstructorOverride = false;

        /// <summary>
        ///     Sensitive data which should be filtered
        /// </summary>
        public string[] SensitiveData;

        /// <summary>
        ///     Serialize null values to the output (default = True)
        /// </summary>
        public bool SerializeNullValues = true;

        /// <summary>
        ///     Maximum depth for circular references in inline mode (default = 20)
        /// </summary>
        public byte SerializerMaxDepth = 20;

        /// <summary>
        ///     Save property/field names as lowercase (default = false)
        /// </summary>
        public bool SerializeToLowerCaseNames = false;

        /// <summary>
        ///     Show the readonly properties of types in the output (default = False)
        /// </summary>
        public bool ShowReadOnlyProperties;

        /// <summary>
        ///     Use escaped unicode i.e. \uXXXX format for non ASCII characters (default = True)
        /// </summary>
        public bool UseEscapedUnicode = true;

        /// <summary>
        ///     Enable fastJSON extensions $types, $type, $map (default = True)
        /// </summary>
        public bool UseExtensions = true;

        /// <summary>
        ///     Use the fast GUID format (default = True)
        /// </summary>
        public bool UseFastGuid = true;

        /// <summary>
        ///     Use the optimized fast Dataset Schema format (default = True)
        /// </summary>
        public bool UseOptimizedDatasetSchema = true;

        /// <summary>
        ///     Use the UTC date format (default = True)
        /// </summary>
        public bool UseUTCDateTime = true;

        /// <summary>
        ///     Output Enum values instead of names (default = False)
        /// </summary>
        public bool UseValuesOfEnums = true;

        /// <summary>
        ///     Use the $types extension to optimize the output json (default = True)
        /// </summary>
        public bool UsingGlobalTypes = true;

        public void FixValues()
        {
            if (UseExtensions == false)
            {
                UsingGlobalTypes = false;
                InlineCircularReferences = true;
            }
            if (EnableAnonymousTypes)
                ShowReadOnlyProperties = true;
        }
    }

    public static class JSON
    {
        /// <summary>
        ///     Globally set-able parameters for controlling the serializer
        /// </summary>
        public static JSONParameters Parameters = new JSONParameters();

        /// <summary>
        ///     Create a formatted json string (beautified) from an object
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string ToNiceJSON(object obj, JSONParameters param)
        {
            return Beautify(ToJSON(obj, param));
        }

        /// <summary>
        ///     Create a formatted json string (beautified) from an object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string ToNiceJSON(object obj)
        {
            return Beautify(ToJSON(obj, Parameters));
        }

        /// <summary>
        ///     Create a json representation for an object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string ToJSON(object obj)
        {
            return ToJSON(obj, Parameters);
        }

        /// <summary>
        ///     Create a json representation for an object with parameter override on this call
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string ToJSON(object obj, JSONParameters param)
        {
            if (param != null)
                param.FixValues();
            else
                param = Parameters;

            Type t = null;

            if (obj == null)
                return "null";

            if (obj.GetType().IsGenericType)
                t = Reflection.Instance.GetGenericTypeDefinition(obj.GetType());
            if (t == typeof (Dictionary<,>) || t == typeof (List<>))
                param.UsingGlobalTypes = false;

            if (param.EnableAnonymousTypes)
            {
                param.UseExtensions = false;
                param.UsingGlobalTypes = false;
            }
            return param.FilterSensitiveData
                ? FilterSensitiveData(new JSONSerializer(param).ConvertToJSON(obj), param.SensitiveData)
                : new JSONSerializer(param).ConvertToJSON(obj);
        }

        /// <summary>
        ///     Create a human readable string from the json
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string Beautify(string input)
        {
            return Formatter.PrettyPrint(input);
        }

        /// <summary>
        ///     Register custom type handlers for your own types not natively handled by json
        /// </summary>
        /// <param name="type"></param>
        /// <param name="serializer"></param>
        public static void RegisterCustomType(Type type, Serialize serializer)
        {
            Reflection.Instance.RegisterCustomType(type, serializer);
        }

        /// <summary>
        ///     Clear the internal reflection cache so you can start from new (you will lose performance)
        /// </summary>
        public static void ClearReflectionCache()
        {
            Reflection.Instance.ClearReflectionCache();
        }

        private static string FilterSensitiveData(string json, string[] sensitiveData)
        {
            return json.Replace(sensitiveData, "[filtered]");
        }
    }
}