#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Drawings.cs is part of SFXSafeJungleSpot.

 SFXSafeJungleSpot is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXSafeJungleSpot is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXSafeJungleSpot. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using SFXSafeJungleSpot.Classes;

#endregion

namespace SFXSafeJungleSpot.Features.Drawings
{
    internal class App : Parent
    {
        public override string Name
        {
            get { return Global.Lang.Get("F_App"); }
        }
    }
}