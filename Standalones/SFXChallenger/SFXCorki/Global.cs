#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Global.cs is part of SFXCorki.

 SFXCorki is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXCorki is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXCorki. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.IO;
using System.Linq;
using SFXLibrary;
using SFXLibrary.Logger;

#endregion

namespace SFXCorki
{
    internal class Global
    {
        public static string Name = "SFXCorki";
        public static ILogger Logger;
        public static string LogDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Name + " - Logs");
        public static string UpdatePath = "Lizzaran/LeagueSharp-Dev/master/Standalones/SFXChallenger/SFXCorki";
        public static Language Lang = new Language();

        static Global()
        {
            Logger = new FileLogger(LogDir) { LogLevel = LogLevel.High };

            try
            {
                Directory.GetFiles(LogDir)
                    .Select(f => new FileInfo(f))
                    .Where(f => f.CreationTime < DateTime.Now.AddDays(-7))
                    .ToList()
                    .ForEach(f => f.Delete());
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex));
            }
        }
    }
}