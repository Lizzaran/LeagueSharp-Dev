#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Sensitive.cs is part of SFXLibrary.

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

    using System.Collections.Generic;
    using System.Linq;
    using LeagueSharp;

    #endregion

    public static class Sensitive
    {
        private static string[] _data;

        public static string[] Data
        {
            get
            {
                if (_data == null)
                    SetData();
                return _data;
            }
        }

        private static void SetData()
        {
            var data = ObjectManager.Get<Obj_AI_Hero>().Select(hero => hero.Name).ToList();
            data.AddRange(new List<string>
            {
                Game.IP,
                Game.Region,
                Game.Id.ToString()
            });
            _data = data.ToArray();
        }
    }
}