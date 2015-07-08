#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Champion.cs is part of SFXChallenger.

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
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXChallenger.Enumerations;
using SFXChallenger.Interfaces;
using SFXChallenger.Managers;
using SFXChallenger.Menus;
using SFXLibrary.Logger;
using Orbwalking = SFXChallenger.Wrappers.Orbwalking;
using TargetSelector = SFXChallenger.Wrappers.TargetSelector;

#endregion

namespace SFXChallenger.Abstracts
{
    internal abstract class Champion : IChampion
    {
        protected readonly Obj_AI_Hero Player = ObjectManager.Player;
        private List<Spell> _spells;
        protected Spell E;
        protected Spell Q;
        protected Spell R;
        protected Spell W;

        protected Champion()
        {
            Core.OnBoot += OnCoreBoot;
            Core.OnShutdown += OnCoreShutdown;
        }

        protected abstract ItemFlags ItemFlags { get; }
        public Menu SFXMenu { get; private set; }

        public List<Spell> Spells
        {
            get { return _spells ?? (_spells = new List<Spell> { Q, W, E, R }); }
        }

        public Menu Menu { get; private set; }
        public Orbwalking.Orbwalker Orbwalker { get; private set; }

        void IChampion.Combo()
        {
            try
            {
                Combo();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        void IChampion.Harass()
        {
            try
            {
                Harass();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        void IChampion.LaneClear()
        {
            try
            {
                LaneClear();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        void IChampion.Flee()
        {
            try
            {
                Orbwalking.MoveTo(Game.CursorPos, Orbwalker.HoldAreaRadius);
                Utility.DelayAction.Add(
                    750, delegate
                    {
                        if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Flee)
                        {
                            Flee();
                            ItemManager.UseFleeItems();
                        }
                    });
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        void IChampion.Killsteal()
        {
            try
            {
                Killsteal();
                KillstealManager.Killsteal();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected abstract void SetupSpells();
        protected abstract void OnLoad();
        protected abstract void OnUnload();
        protected abstract void AddToMenu();

        protected void DrawingOnDraw(EventArgs args)
        {
            try
            {
                DrawingManager.Draw();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected abstract void Combo();
        protected abstract void Harass();
        protected abstract void LaneClear();
        protected abstract void Flee();
        protected abstract void Killsteal();

        private void OnCoreBoot(EventArgs args)
        {
            try
            {
                OnLoad();
                SetupSpells();
                SetupMenu();

                Drawing.OnDraw += DrawingOnDraw;
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
                OnUnload();

                Drawing.OnDraw -= DrawingOnDraw;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void SetupMenu()
        {
            try
            {
                SFXMenu = new Menu(Global.Name + " - ALPHA", "sfx", true);

                InfoMenu.AddToMenu(SFXMenu.AddSubMenu(new Menu(Global.Lang.Get("F_Info"), SFXMenu.Name + ".info")));

                TargetSelector.AddToMenu(
                    SFXMenu.AddSubMenu(
                        new Menu(Global.Lang.Get("F_TargetSelector"), SFXMenu.Name + ".ts." + Player.ChampionName)));
                TargetSelector.SetDebugRange(Spells.Select(s => s.Range).Max());

                Orbwalker =
                    new Orbwalking.Orbwalker(
                        SFXMenu.AddSubMenu(new Menu(Global.Lang.Get("F_Orbwalker"), SFXMenu.Name + ".orb")));
                KillstealManager.AddToMenu(
                    SFXMenu.AddSubMenu(new Menu(Global.Lang.Get("F_MK"), SFXMenu.Name + ".killsteal")));
                ItemManager.AddToMenu(
                    SFXMenu.AddSubMenu(new Menu(Global.Lang.Get("F_MI"), SFXMenu.Name + ".items")), ItemFlags);
                SummonerManager.AddToMenu(
                    SFXMenu.AddSubMenu(new Menu(Global.Lang.Get("F_MS"), SFXMenu.Name + ".summoners")));

                TickMenu.AddToMenu(SFXMenu);
                LanguageMenu.AddToMenu(SFXMenu);

                SFXMenu.AddToMainMenu();

                Menu = new Menu(Global.ChampionPrefix + " " + Player.ChampionName, Player.ChampionName, true);
                DrawingManager.AddToMenu(
                    Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Drawing"), Menu.Name + ".drawing")), this);
                try
                {
                    AddToMenu();
                }
                catch (Exception ex)
                {
                    Global.Logger.AddItem(new LogItem(ex));
                }

                Menu.AddToMainMenu();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }
}