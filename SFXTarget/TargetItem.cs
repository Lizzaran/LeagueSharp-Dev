#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 TargetItem.cs is part of SFXTarget.

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

    using LeagueSharp;

    #endregion

    internal class TargetItem
    {
        private Obj_AI_Hero _target;

        public TargetItem(Obj_AI_Hero target, float damage = 0)
        {
            Target = target;
            Damage = damage;
        }

        public Obj_AI_Hero Target
        {
            get { return _target; }
            set
            {
                _target = value;
                Timestamp = Game.Time;
            }
        }

        public float Damage { get; set; }
        public float Timestamp { get; private set; }
    }
}