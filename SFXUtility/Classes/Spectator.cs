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

namespace SFXUtility.Classes
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using LeagueSharp;
    using SFXLibrary.Extensions.NET;

    #endregion

    public class Spectator
    {
        private const string ChunkUrl = "http://{spectate_url}/observer-mode/rest/consumer/getGameDataChunk/{platform_id}/{game_id}/{chunk_id}/null";
        private const string ChunkIdUrl = "http://{spectate_url}/observer-mode/rest/consumer/getLastChunkInfo/{platform_id}/{game_id}/30000/null";
        private const string EncryptionKeyUrl = "http://{region}.op.gg/match/observer/id={game_id}";
        private const string RecordUrl = "http://{region}.op.gg/summoner/ajasdax/requestRecording.json/gameId={game_id}";
        private static readonly List<byte[]> Chunks = new List<byte[]>();

        static Spectator()
        {
            Region = PlatformId.Last().IsNumeric() ? PlatformId.Remove(PlatformId.Length - 1).ToLower() : PlatformId.ToLower();
        }

        public static string PlatformId
        {
            get { return Game.Region; }
        }

        public static string Region { get; set; }

        public static long GameId
        {
            get { return Game.Id; }
        }

        public static string SpectateUrl { get; set; }
        public static string EncryptionKey { get; set; }

        public static int GetLatestChunkId()
        {
            using (var client = new WebClient())
            {
                var url = ChunkIdUrl.Replace("{spectate_url}", SpectateUrl)
                    .Replace("{platform_id}", PlatformId)
                    .Replace("{game_id}", GameId.ToString());
                try
                {
                    return Convert.ToInt32((client.DownloadString(new Uri(url))).Between("\"chunkId\":", ",", StringComparison.OrdinalIgnoreCase));
                }
                catch
                {
                    return 0;
                }
            }
        }

        public static string[] GetSpectateData()
        {
            using (var client = new WebClient())
            {
                var url = EncryptionKeyUrl.Replace("{region}", Region).Replace("{game_id}", GameId.ToString());
                try
                {
                    return client.DownloadString(new Uri(url)).Between("spectator ", " " + GameId, StringComparison.OrdinalIgnoreCase).Split(' ');
                }
                catch
                {
                    return null;
                }
            }
        }

        public static bool DoRecord()
        {
            var success = false;
            using (var client = new WebClient())
            {
                var url = RecordUrl.Replace("{region}", Region).Replace("{game_id}", GameId.ToString());
                client.DownloadStringCompleted += delegate(object sender, DownloadStringCompletedEventArgs args)
                {
                    if (!args.Cancelled && args.Error == null)
                    {
                        success = !args.Result.Contains("error", StringComparison.OrdinalIgnoreCase);
                    }
                };
                client.DownloadString(new Uri(url));
            }
            return success;
        }

        public static List<byte[]> GetChunks()
        {
            var url = ChunkUrl.Replace("{spectate_url}", SpectateUrl).Replace("{platform_id}", PlatformId).Replace("{game_id}", GameId.ToString());
            var latestChunkId = GetLatestChunkId();
            using (var client = new WebClient())
            {
                for (var i = Chunks.Count + 1; latestChunkId > i; i++)
                {
                    try
                    {
                        Chunks.Add(client.DownloadData(new Uri(url.Replace("{chunk_id}", i.ToString()))));
                    }
                    catch
                    {
                    }
                }
            }
            return Chunks;
        }

        public static bool Init()
        {
            var data = GetSpectateData();
            if (data != null)
            {
                SpectateUrl = data[0];
                EncryptionKey = data[1];
            }
            return !string.IsNullOrWhiteSpace(SpectateUrl) && !string.IsNullOrWhiteSpace(EncryptionKey);
        }
    }
}