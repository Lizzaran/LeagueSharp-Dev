#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Champion.cs is part of SFXKalista.

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
using System.Collections.Generic;
using LeagueSharp;
using LeagueSharp.Common;
using SFXKalista.Enumerations;
using SFXKalista.Interfaces;
using SFXKalista.Managers;
using SFXKalista.Menus;
using SFXLibrary.Logger;
using Orbwalking = SFXKalista.Wrappers.Orbwalking;
using Spell = SFXKalista.Wrappers.Spell;
using TargetSelector = SFXKalista.Wrappers.TargetSelector;

#endregion

namespace SFXKalista.Abstracts
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
                Flee();
                Utility.DelayAction.Add(
                    750, delegate
                    {
                        if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Flee)
                        {
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
                SFXMenu = new Menu(Global.Name, "sfx", true);

                Menu = SFXMenu.AddSubMenu(new Menu(Player.ChampionName, "." + Player.ChampionName));

                DrawingManager.AddToMenu(
                    Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Drawing"), Menu.Name + ".drawing")), this);

                TargetSelector.AddToMenu(
                    SFXMenu.AddSubMenu(
                        new Menu(Global.Lang.Get("F_TargetSelector"), SFXMenu.Name + ".ts." + Player.ChampionName)));

                Orbwalker =
                    new Orbwalking.Orbwalker(
                        SFXMenu.AddSubMenu(new Menu(Global.Lang.Get("F_Orbwalker"), SFXMenu.Name + ".orb")));
                KillstealManager.AddToMenu(
                    SFXMenu.AddSubMenu(new Menu(Global.Lang.Get("F_MK"), SFXMenu.Name + ".killsteal")));
                ItemManager.AddToMenu(
                    SFXMenu.AddSubMenu(new Menu(Global.Lang.Get("F_MI"), SFXMenu.Name + ".items")), ItemFlags);
                SummonerManager.AddToMenu(
                    SFXMenu.AddSubMenu(new Menu(Global.Lang.Get("F_MS"), SFXMenu.Name + ".summoners")));

                LanguageMenu.AddToMenu(
                    SFXMenu.AddSubMenu(new Menu(Global.Lang.Get("F_Settings"), SFXMenu.Name + ".settings")));

                InfoMenu.AddToMenu(SFXMenu.AddSubMenu(new Menu(Global.Lang.Get("F_Info"), SFXMenu.Name + ".info")));

                TickMenu.AddToMenu(SFXMenu);

                SFXMenu.AddToMainMenu();

                try
                {
                    AddToMenu();
                }
                catch (Exception ex)
                {
                    Global.Logger.AddItem(new LogItem(ex));
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }
}