#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 SpellQueue.cs is part of SFXChallenger.

 SFXChallenger is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXChallenger is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXChallenger. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using LeagueSharp;
using LeagueSharp.Common;

#endregion

namespace SFXChallenger.Helpers
{
    public class SpellQueue
    {
        private static float _sendTime;

        public static bool IsBusy
        {
            get
            {
                var busy = _sendTime > 0 && _sendTime + (Game.Ping / 1000f) + 0.1f - Game.Time > 0 ||
                           ObjectManager.Player.Spellbook.IsCastingSpell || ObjectManager.Player.Spellbook.IsChanneling ||
                           ObjectManager.Player.Spellbook.IsCharging;

                IsBusy = busy;

                return busy;
            }
            private set
            {
                if (!value)
                {
                    _sendTime = 0;
                }
            }
        }

        public static bool IsReady
        {
            get { return !IsBusy; }
        }

        public static void Init()
        {
            Spellbook.OnCastSpell += OnSpellbookCastSpell;
            Obj_AI_Base.OnProcessSpellCast += OnObjAiBaseProcessSpellCast;
            Spellbook.OnStopCast += OnSpellbookStopCast;
        }

        private static void OnSpellbookCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (!sender.Owner.IsMe)
            {
                return;
            }

            switch (args.Slot)
            {
                case SpellSlot.Q:
                case SpellSlot.W:
                case SpellSlot.E:
                case SpellSlot.R:
                    if (IsReady)
                    {
                        _sendTime = Game.Time;
                    }
                    else
                    {
                        args.Process = false;
                    }
                    break;
            }
        }

        private static void OnObjAiBaseProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && !args.SData.IsAutoAttack())
            {
                IsBusy = false;
            }
        }

        private static void OnSpellbookStopCast(Spellbook sender, SpellbookStopCastEventArgs args)
        {
            if (sender.Owner.IsMe)
            {
                IsBusy = false;
            }
        }
    }
}