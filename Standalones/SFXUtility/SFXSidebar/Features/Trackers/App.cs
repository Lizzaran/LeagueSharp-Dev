#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Trackers.cs is part of SFXSidebar.

 SFXSidebar is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXSidebar is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXSidebar. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using SFXSidebar.Classes;

#endregion

namespace SFXSidebar.Features.Trackers
{
    internal class App : Parent
    {
        public override string Name
        {
            get { return Global.Lang.Get("F_App"); }
        }
    }
}