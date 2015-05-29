#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Update.cs is part of SFXChallenger.

 SFXChallenger is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXChallenger is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXChallenger. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using LeagueSharp.Common;
using SFXLibrary.Logger;
using Version = System.Version;

#endregion

namespace SFXChallenger
{
    internal class Update
    {
        public static void Init(string path, int displayTime)
        {
            try
            {
                new Thread(
                    async () =>
                    {
                        try
                        {
                            using (var client = new WebClient())
                            {
                                var data =
                                    await
                                        client.DownloadStringTaskAsync(
                                            string.Format(
                                                "https://raw.githubusercontent.com/{0}/Properties/AssemblyInfo.cs", path));

                                var version =
                                    Version.Parse(
                                        new Regex("AssemblyFileVersion\\((\"(.+?)\")\\)").Match(data).Groups[1].Value
                                            .Replace("\"", ""));

                                var assemblyName = Assembly.GetExecutingAssembly().GetName();
                                if (version > assemblyName.Version)
                                {
                                    Notifications.AddNotification(
                                        string.Format(
                                            "[{0}] {1}: {2} => {3}!", assemblyName.Name,
                                            Global.Lang.Get("G_UpdateAvailable"), assemblyName.Version, version),
                                        displayTime);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Global.Logger.AddItem(new LogItem(ex));
                        }
                    }).Start();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }
}