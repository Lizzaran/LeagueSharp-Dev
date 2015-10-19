#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Sequence.cs is part of SFXHumanizer Pro.

 SFXHumanizer Pro is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXHumanizer Pro is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXHumanizer Pro. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using LeagueSharp.Common;

#endregion

namespace SFXHumanizer_Pro
{
    internal class Sequence
    {
        private int _index;
        private int[] _items;

        public int Index
        {
            get { return _index; }
            set
            {
                _index = value;
                LastIndexChange = Utils.GameTimeTickCount;
            }
        }

        public int[] Items
        {
            get { return _items; }
            set
            {
                _items = value;
                _index = 0;
                LastItemsChange = Utils.GameTimeTickCount;
            }
        }

        public int LastIndexChange { get; private set; }
        public int LastItemsChange { get; private set; }
    }
}