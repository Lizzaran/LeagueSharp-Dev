#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 DamageItem.cs is part of SFXTarget.

 SFXTarget is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXTarget is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXTarget. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License
namespace SFXTarget
{
    #region

    using System.Collections.Generic;
    using LeagueSharp;

    #endregion

    internal class DamageItem
    {
        private readonly float _damageFadeTime;
        public List<TargetItem> Targets = new List<TargetItem>();

        public DamageItem(Obj_AI_Hero target, float damage, float damageFadeTime)
        {
            _damageFadeTime = damageFadeTime;
            Add(target, damage);
        }

        public void Add(Obj_AI_Hero target, float damage)
        {
            Targets.Add(new TargetItem(target, damage));
        }

        public void Update()
        {
            Targets.RemoveAll(t => (Game.Time - t.Timestamp) <= _damageFadeTime);
        }
    }
}