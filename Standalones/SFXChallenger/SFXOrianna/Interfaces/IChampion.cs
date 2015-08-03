#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 IChampion.cs is part of SFXOrianna.

 SFXOrianna is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXOrianna is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXOrianna. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System.Collections.Generic;
using LeagueSharp.Common;
using Orbwalking = SFXOrianna.Wrappers.Orbwalking;
using Spell = SFXOrianna.Wrappers.Spell;

#endregion

namespace SFXOrianna.Interfaces
{
    internal interface IChampion
    {
        Menu SFXMenu { get; }
        Menu Menu { get; }
        Orbwalking.Orbwalker Orbwalker { get; }
        List<Spell> Spells { get; }
        void Combo();
        void Harass();
        void LaneClear();
        void Flee();
        void Killsteal();
    }
}