#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Timers.cs is part of SFXAltar.

 SFXAltar is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXAltar is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXAltar. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using SFXAltar.Classes;

#endregion

namespace SFXAltar.Features.Timers
{
    internal class App : Parent
    {
        public override string Name
        {
            get { return Global.Lang.Get("F_App"); }
        }
    }
}