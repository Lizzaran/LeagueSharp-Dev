#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 TChampion.cs is part of SFXChallenger.

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

using System;
using System.Collections.Generic;
using LeagueSharp;
using SFXChallenger.Wrappers;
using SFXLibrary.Logger;

#endregion

namespace SFXChallenger.Abstracts
{
    // ReSharper disable once InconsistentNaming
    internal abstract class TChampion : Champion
    {
        public readonly float MaxRange;
        public List<Obj_AI_Hero> Targets = new List<Obj_AI_Hero>();

        protected TChampion(float maxRange)
        {
            MaxRange = maxRange;
            Core.OnBoot += OnCoreBoot;
            Core.OnShutdown += OnCoreShutdown;
        }

        private void OnCorePreUpdate(EventArgs args)
        {
            try
            {
                var targets = TargetSelector.GetTargets(MaxRange);
                Targets = targets != null && targets.Count > 0 ? targets : new List<Obj_AI_Hero>();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnCoreBoot(EventArgs args)
        {
            try
            {
                Core.OnPreUpdate += OnCorePreUpdate;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnCoreShutdown(EventArgs args)
        {
            try
            {
                Core.OnPreUpdate -= OnCorePreUpdate;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }
}