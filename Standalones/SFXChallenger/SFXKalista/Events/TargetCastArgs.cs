#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 TargetCastArgs.cs is part of SFXKalista.

 SFXKalista is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXKalista is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXKalista. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using LeagueSharp;

#endregion

namespace SFXKalista.Events
{
    public class TargetCastArgs : EventArgs
    {
        public TargetCastArgs(Obj_AI_Hero sender,
            GameObject target,
            SpellDataTargetType type,
            float delay,
            float speed,
            SpellData sData)
        {
            Sender = sender;
            Target = target;
            Delay = delay;
            Speed = speed;
            SData = sData;
            Type = type;
        }

        public Obj_AI_Hero Sender { get; private set; }
        public GameObject Target { get; private set; }
        public float Delay { get; private set; }
        public float Speed { get; private set; }
        public SpellData SData { get; private set; }
        public SpellDataTargetType Type { get; private set; }
    }
}