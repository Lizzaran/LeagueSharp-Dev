#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Spectator.cs is part of SFXUtility.

 SFXUtility is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXUtility is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXUtility. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using LeagueSharp;
using SFXLibrary.Extensions.NET;

#endregion

namespace SFXUtility.Classes
{

    #region

    #endregion

    public class Spectator
    {
        private static readonly string DoRecordUrl =
            "http://{region}op.gg/summoner/ajax/requestRecording.json/gameId={game_id}";

        private static readonly string IsRecordingUrl = "http://{region}op.gg/summoner/ajax/spectator/";

        static Spectator()
        {
            Region = PlatformId.Last().IsNumeric()
                ? PlatformId.Remove(PlatformId.Length - 1).ToLower()
                : PlatformId.ToLower();
            Region = Region.Contains("kr", StringComparison.OrdinalIgnoreCase) ? string.Empty : Region + ".";
            DoRecordUrl = DoRecordUrl.Replace("{region}", Region).Replace("{game_id}", GameId.ToString());
            IsRecordingUrl = IsRecordingUrl.Replace("{region}", Region);
        }

        public static string PlatformId
        {
            get { return Game.Region; }
        }

        private static string Region { get; set; }

        public static long GameId
        {
            get { return Game.Id; }
        }

        public static async Task<bool> DoRecord()
        {
            using (var client = new WebClient())
            {
                try
                {
                    var response = await client.DownloadStringTaskAsync(new Uri(DoRecordUrl));
                    return !response.Contains("error", StringComparison.OrdinalIgnoreCase);
                }
                catch
                {
                    return false;
                }
            }
        }

        public static async Task<bool> IsRecoding()
        {
            using (var client = new WebClient())
            {
                try
                {
                    client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                    var response =
                        await
                            client.UploadStringTaskAsync(
                                new Uri(IsRecordingUrl),
                                "userName=" + HttpUtility.UrlEncode(ObjectManager.Player.Name) + "&force=false");
                    return response.Contains("NowRecording", StringComparison.OrdinalIgnoreCase);
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}