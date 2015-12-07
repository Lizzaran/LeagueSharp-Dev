#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Champion.cs is part of ChampionTemplate.

 ChampionTemplate is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 ChampionTemplate is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with ChampionTemplate. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Collections.Generic;
using System.Linq;
using ChampionTemplate.Interfaces;
using LeagueSharp;
using LeagueSharp.Common;

#endregion

namespace ChampionTemplate.Abstracts
{
    internal abstract class Champion : IChampion
    {
        // Declaring various variables which are needed in all Champion classes
        private static float _minionSearchRange;
        protected readonly Obj_AI_Hero Player;
        private Obj_AI_Base _nearestMinion;
        private List<Spell> _spells;
        protected Spell E;
        protected Spell Q;
        protected Spell R;
        protected Spell W;

        protected Champion()
        {
            // Player is simply a "shortcut" for ObjectManager.Player
            Player = ObjectManager.Player;

            // Register to the event when the Core starts
            Core.OnBoot += OnCoreBoot;
        }

        /*
         * Menues are splitted in 2 parts.
         * MainMenu: Generic things which is needed for every champion, e.g. Orbwalker
         * Menu: Specific for the current champion 
         */
        public Menu MainMenu { get; private set; }
        public Menu Menu { get; private set; }

        // List of all spells
        public List<Spell> Spells
        {
            get { return _spells ?? (_spells = new List<Spell> { Q, W, E, R }); }
        }

        public Orbwalking.Orbwalker Orbwalker { get; private set; }

        /*
         * Methods which are defined in the interface and must be implemented. 
         */

        void IChampion.Combo()
        {
            Combo();
        }

        void IChampion.Harass()
        {
            Harass();
        }

        void IChampion.LaneClear()
        {
            // Determine if it should execute LaneClear or JungleClear.
            if (_nearestMinion == null || !_nearestMinion.IsValid || _nearestMinion.Team != Player.Team)
            {
                LaneClear();
            }
        }

        void IChampion.JungleClear()
        {
            // Determine if it should execute JungleClear or LaneClear.
            if (_nearestMinion == null || !_nearestMinion.IsValid || _nearestMinion.Team == GameObjectTeam.Neutral)
            {
                JungleClear();
            }
        }

        void IChampion.Killsteal()
        {
            Killsteal();
        }

        protected virtual void OnCorePreUpdate(EventArgs args)
        {
            /*
             * Finds the nearest neutral or enemy minion.
             * Needed to determine if it should execute LaneClear or JungleClear.
             * 
             * If you ever need to overwrite / change this behaviour for a single champion you can do it like this:
             * 
             * protected override void OnCorePreUpdate(EventArgs args)
             * {
             *     // Your logic here
             * }
             *  
             */
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                _nearestMinion =
                    MinionManager.GetMinions(
                        _minionSearchRange, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.None)
                        .OrderBy(m => m.Distance(Player))
                        .FirstOrDefault();
            }
            OnPreUpdate();
        }

        protected virtual void OnCorePostUpdate(EventArgs args)
        {
            OnPostUpdate();
        }

        /*
         * Methods which the child class have to implement
         */
        protected abstract void OnLoad();
        protected abstract void SetupSpells();
        protected abstract void AddToMenu();
        protected abstract void OnPreUpdate();
        protected abstract void OnPostUpdate();
        protected abstract void Combo();
        protected abstract void Harass();
        protected abstract void LaneClear();
        protected abstract void JungleClear();
        protected abstract void Killsteal();

        // Runs when the core (game) started.
        private void OnCoreBoot(EventArgs args)
        {
            // Calls these functions in the child class
            OnLoad();
            SetupSpells();
            SetupMenu();

            // Needed for Jungle / Lane Clear
            _minionSearchRange = Math.Min(
                2000,
                Math.Max(
                    ObjectManager.Player.AttackRange + ObjectManager.Player.BoundingRadius * 2,
                    Spells.Select(spell => spell.IsChargedSpell ? spell.ChargedMaxRange : spell.Range)
                        .Concat(new[] { _minionSearchRange })
                        .Max()));

            Core.OnPreUpdate += OnCorePreUpdate;
            Core.OnPostUpdate += OnCorePostUpdate;
        }

        private void SetupMenu()
        {
            // Setup the main menu
            MainMenu = new Menu("Champion Template", "champion.template", true);
            // Setup the champion menu
            Menu = MainMenu.AddSubMenu(new Menu(Player.ChampionName, MainMenu.Name + "." + Player.ChampionName));
            // Load the orbwalker
            Orbwalker =
                new Orbwalking.Orbwalker(MainMenu.AddSubMenu(new Menu("Orbwalker", MainMenu.Name + ".orbwalker")));
            // Needed to be added to the first-level Common Menu
            MainMenu.AddToMainMenu();
            // Calls the champion specific menu code
            AddToMenu();
        }
    }
}