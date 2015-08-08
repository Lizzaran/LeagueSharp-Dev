#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Champion.cs is part of SFXViktor.

 SFXViktor is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXViktor is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXViktor. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXViktor.Enumerations;
using SFXViktor.Interfaces;
using SFXViktor.Managers;
using SFXViktor.Menus;
using SFXLibrary;
using SFXLibrary.Logger;
using Orbwalking = SFXViktor.Wrappers.Orbwalking;
using Spell = SFXViktor.Wrappers.Spell;
using TargetSelector = SFXViktor.Wrappers.TargetSelector;

#endregion

namespace SFXViktor.Abstracts
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
        protected abstract ItemUsageType ItemUsage { get; }
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
                Orbwalker.SetAttack(false);
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
                Utility.DelayAction.Add(
                    125, delegate
                    {
                        if (Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Flee)
                        {
                            Orbwalker.SetAttack(true);
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

                if (ItemUsage == ItemUsageType.AfterAttack)
                {
                    Orbwalking.AfterAttack += OnOrbwalkingAfterAttack;
                    Spellbook.OnCastSpell += OnSpellbookCastSpell;
                }

                Drawing.OnDraw += DrawingOnDraw;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnSpellbookCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            try
            {
                if (sender.Owner != null && sender.Owner.IsMe)
                {
                    if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                    {
                        var endPos = args.EndPosition;
                        var spell = Spells.FirstOrDefault(s => s.Slot == args.Slot);
                        if (spell != null)
                        {
                            var enemy1 =
                                GameObjects.EnemyHeroes.FirstOrDefault(
                                    e => e.IsValidTarget() && e.Distance(endPos) < spell.Range / 2f);
                            var enemy2 =
                                GameObjects.EnemyHeroes.FirstOrDefault(
                                    e => e.IsValidTarget() && e.Distance(Player) < spell.Range / 2f);
                            if (enemy1 != null)
                            {
                                ItemManager.Muramana(
                                    enemy1, true,
                                    spell.Range + spell.Width + enemy1.BoundingRadius + Player.BoundingRadius);
                            }
                            if (enemy2 != null)
                            {
                                ItemManager.Muramana(
                                    enemy2, true,
                                    spell.Range + spell.Width + enemy2.BoundingRadius + Player.BoundingRadius);
                            }
                        }
                    }
                    else
                    {
                        ItemManager.Muramana(null, false);
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnOrbwalkingAfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            try
            {
                if (unit.IsMe)
                {
                    Orbwalker.ForceTarget(null);
                    if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                    {
                        var enemy = target as Obj_AI_Hero;
                        if (enemy != null)
                        {
                            ItemManager.Muramana(enemy, true);
                            ItemManager.UseComboItems(enemy);
                            SummonerManager.UseComboSummoners(enemy);
                        }
                    }
                    else
                    {
                        ItemManager.Muramana(null, false);
                    }
                }
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
                    SFXMenu.AddSubMenu(new Menu(Global.Lang.Get("F_TargetSelector"), SFXMenu.Name + ".ts")));

                Orbwalker =
                    new Orbwalking.Orbwalker(
                        SFXMenu.AddSubMenu(new Menu(Global.Lang.Get("F_Orbwalker"), SFXMenu.Name + ".orb")));
                KillstealManager.AddToMenu(
                    SFXMenu.AddSubMenu(new Menu(Global.Lang.Get("F_MK"), SFXMenu.Name + ".killsteal")));
                ItemManager.AddToMenu(
                    SFXMenu.AddSubMenu(new Menu(Global.Lang.Get("F_MI"), SFXMenu.Name + ".items")), ItemFlags);
                SummonerManager.AddToMenu(
                    SFXMenu.AddSubMenu(new Menu(Global.Lang.Get("F_MS"), SFXMenu.Name + ".summoners")));

                var miscMenu =
                    SFXMenu.AddSubMenu(new Menu(Global.Lang.Get("G_Miscellaneous"), SFXMenu.Name + ".miscellaneous"));
                SpellQueueManager.AddToMenu(
                    miscMenu.AddSubMenu(new Menu(Global.Lang.Get("F_MSQ"), miscMenu.Name + ".spell-queue")));
                OverkillManager.AddToMenu(
                    miscMenu.AddSubMenu(new Menu(Global.Lang.Get("F_MO"), miscMenu.Name + ".overkill")));
                DebugMenu.AddToMenu(miscMenu, Spells);

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