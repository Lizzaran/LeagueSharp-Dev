#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Global.cs is part of SFXUtility.

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
using System.IO;
using SFXLibrary;
using SFXLibrary.IoCContainer;
using SFXLibrary.Logger;

#endregion

namespace SFXUtility
{
    public class Global
    {
        public static ILogger Logger;
        public static Container IoC = new Container();
        public static Language Lang = new Language();
        public static string DefaultFont = "Calibri";
        public static string Name = "SFXUtility";
        public static string UpdatePath = "Lizzaran/LeagueSharp-Dev/master/SFXUtility";

        static Global()
        {
            Logger = new FileLogger(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SFXUtility - Logs"));
        }
    }
}