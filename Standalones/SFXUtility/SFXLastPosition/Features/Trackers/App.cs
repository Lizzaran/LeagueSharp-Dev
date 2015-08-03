#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Trackers.cs is part of SFXLastPosition.

 SFXLastPosition is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXLastPosition is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXLastPosition. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using SFXLastPosition.Classes;

#endregion

namespace SFXLastPosition.Features.Trackers
{
    internal class App : Parent
    {
        public override string Name
        {
            get { return Global.Lang.Get("F_App"); }
        }
    }
}