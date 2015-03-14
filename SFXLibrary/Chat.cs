#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Chat.cs is part of SFXLibrary.

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

    using LeagueSharp;

    #endregion

    public class Chat
    {
        public const string DefaultColor = "#F7A100";

        public static void Local(string message, string hexColor = DefaultColor)
        {
            Game.PrintChat(string.Format("<font color='{0}'>{1}</font>", hexColor, message));
        }
    }
}