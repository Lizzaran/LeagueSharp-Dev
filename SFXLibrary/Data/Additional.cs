#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Additional.cs is part of SFXLibrary.

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

namespace SFXLibrary.Data
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using LeagueSharp;
    using Microsoft.Win32;

    #endregion

    public static class Additional
    {
        private const string Unknown = "Unknown";
        private static Dictionary<string, string> _data;

        public static Dictionary<string, string> Data
        {
            get
            {
                if (_data != null)
                    return _data;

                _data = new Dictionary<string, string>
                {
                    {"Operating System", OperatingSystem()},
                    {"NET Version", NETVersion()},
                    {"Game Version", Game.Version}
                };
                return _data;
            }
        }

        private static string OperatingSystem()
        {
            try
            {
                var name = Unknown;
                var osVersion = Environment.OSVersion;
                var majorVersion = osVersion.Version.Major;
                var minorVersion = osVersion.Version.Minor;

                switch (osVersion.Platform)
                {
                    case PlatformID.Win32S:
                        name = "Windows 3.1";
                        break;
                    case PlatformID.WinCE:
                        name = "Windows CE";
                        break;
                    case PlatformID.Win32Windows:
                    {
                        if (majorVersion == 4)
                        {
                            switch (minorVersion)
                            {
                                case 0:
                                    name = "Windows 95";
                                    break;
                                case 10:
                                    name = "Windows 98";
                                    break;
                                case 90:
                                    name = "Windows Me";
                                    break;
                            }
                        }
                        break;
                    }
                    case PlatformID.Win32NT:
                    {
                        switch (majorVersion)
                        {
                            case 3:
                                name = "Windows NT 3.51";
                                break;
                            case 4:
                                name = "Windows NT 4.0";
                                break;
                            case 5:
                                switch (minorVersion)
                                {
                                    case 0:
                                        name = "Windows 2000";
                                        break;
                                    case 1:
                                        name = "Windows XP";
                                        break;
                                    case 2:
                                        name = "Windows Server 2003";
                                        break;
                                }
                                break;
                            case 6:
                                switch (minorVersion)
                                {
                                    case 0:
                                        name = "Windows Vista";
                                        break;
                                    case 1:
                                        name = "Windows 7";
                                        break;
                                    case 2:
                                        name = "Windows 8";
                                        break;
                                }
                                break;
                        }
                        break;
                    }
                }

                if (name != Unknown)
                {
                    if (osVersion.ServicePack != "")
                    {
                        name += " " + osVersion.ServicePack;
                    }
                    name += " " + (Environment.Is64BitOperatingSystem ? "64" : "32") + "-bit";
                }

                return name;
            }
            catch
            {
            }
            return Unknown;
        }

        private static string NETVersion()
        {
            try
            {
                Version version = null;
                var fod = Assembly.GetExecutingAssembly().GetReferencedAssemblies().FirstOrDefault(x => x.Name == "System.Core");
                if (fod != null)
                {
                    version = fod.Version;
                }
                else
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP"))
                    {
                        if (key != null)
                        {
                            var versionNames = key.GetSubKeyNames();

                            var max = new Version(0, 0);
                            foreach (var v in versionNames)
                            {
                                Version a;
                                Version.TryParse(v.Remove(0, 1), out a);
                                if (a != null)
                                {
                                    if (a.Major > max.Major || a.Major == max.Major && a.Minor > max.Minor)
                                        max = a;
                                }
                            }
                            version = max;
                        }
                    }
                }
                if (version != null)
                    return string.Format("{0}.{1}", version.Major, version.Minor);
            }
            catch
            {
            }
            return Unknown;
        }
    }
}