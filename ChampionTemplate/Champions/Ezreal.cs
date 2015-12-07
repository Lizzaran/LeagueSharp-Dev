#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Ezreal.cs is part of ChampionTemplate.

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

using System.Linq;
using ChampionTemplate.Abstracts;
using LeagueSharp;
using LeagueSharp.Common;

#endregion

namespace ChampionTemplate.Champions
{
    // Make sure the class name equals the Player.ChampionName, otherwise it won't get loaded.
    internal class Ezreal : Champion
    {
        protected override void OnLoad()
        {
            /*
             * Gets called from the parent when it gets loaded, nothing special here.
             * Mainly used to register to events.
             */
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
        }

        protected override void SetupSpells()
        {
            /*
             * Setup all the spells for the champions which are needed for Prediction, Casting etc.
             * The variables Q, W, E, R are already declared in the Champion.cs file.
             */
            Q = new Spell(SpellSlot.Q, 1150f);
            Q.SetSkillshot(0.25f, 60f, 2000f, true, SkillshotType.SkillshotLine);

            W = new Spell(SpellSlot.W, 900f);
            W.SetSkillshot(0.25f, 80f, 1600f, false, SkillshotType.SkillshotLine);

            E = new Spell(SpellSlot.E, 475f);

            R = new Spell(SpellSlot.R, 1750f);
            R.SetSkillshot(1.2f, 160f, 2000f, false, SkillshotType.SkillshotLine);
        }

        // Here you can setup your menu
        protected override void AddToMenu()
        {
            Menu.AddItem(new MenuItem(Menu.Name + ".example", "Example").SetValue(false));
            Menu.AddItem(new MenuItem(Menu.Name + ".e-gapcloser", "E Gapcloser").SetValue(true));
        }

        /*
         * This function runs before every other.
         * E.g. OnPreUpdate -> Killsteal -> Combo / Harass / LaneClear / JungleClear -> OnPostUpdate
         */

        protected override void OnPreUpdate()
        {
            // Not needed in this example
        }

        /*
         * This function runs after every other one is done.
         * E.g. OnPreUpdate -> Killsteal -> Combo / Harass / LaneClear / JungleClear -> OnPostUpdate
         */

        protected override void OnPostUpdate()
        {
            // Not needed in this example
        }

        // Gets called if Combo is active
        protected override void Combo()
        {
            // Simple logic, shouldn't be used in a real project
            if (Q.IsReady())
            {
                var target = TargetSelector.GetTarget(Q.Range, Q.DamageType);
                if (target != null)
                {
                    Q.CastIfWillHit(target);
                }
            }
            if (W.IsReady())
            {
                W.CastOnBestTarget();
            }
            if (E.IsReady())
            {
                E.Cast(Player.Position.Extend(Game.CursorPos, E.Range));
            }
            if (R.IsReady())
            {
                var target = TargetSelector.GetTarget(R.Range, R.DamageType);
                if (target != null)
                {
                    var prediction = R.GetPrediction(target);
                    if (prediction.Hitchance >= HitChance.VeryHigh)
                    {
                        R.Cast(prediction.CastPosition);
                    }
                }
            }

            // Other example
            foreach (var spell in Spells)
            {
                if (spell != null && spell.IsSkillshot && spell.IsReady())
                {
                    var target = TargetSelector.GetTarget(spell.Range, spell.DamageType);
                    if (target != null)
                    {
                        var prediction = spell.GetPrediction(target);
                        if (prediction.Hitchance >= HitChance.High)
                        {
                            spell.Cast(prediction.CastPosition);
                        }
                    }
                }
            }
        }

        // Gets called if Harass is active
        protected override void Harass()
        {
            // Simple logic, shouldn't be used in a real project
            if (Q.IsReady())
            {
                Q.CastOnBestTarget();
            }
        }

        // Gets called if LaneClear is active
        protected override void LaneClear()
        {
            // Simple logic, shouldn't be used in a real project
            var minion = MinionManager.GetMinions(Q.Range).FirstOrDefault();
            if (minion != null)
            {
                Q.Cast(minion.Position);
            }
        }

        // Gets called if LaneClear is active
        protected override void JungleClear()
        {
            // Simple logic, shouldn't be used in a real project
            var minion = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Neutral).FirstOrDefault();
            if (minion != null)
            {
                Q.Cast(minion.Position);
            }
        }

        // Gets called every tick. Here you should add your killsteal logic, if you have any.
        protected override void Killsteal()
        {
            var prediction =
                HeroManager.Enemies.Where(e => e.IsValidTarget() && Q.IsKillable(e))
                    .OrderBy(e => e.Distance(Player))
                    .Select(e => Q.GetPrediction(e))
                    .FirstOrDefault(p => p.Hitchance >= HitChance.High);
            if (prediction != null)
            {
                Q.Cast(prediction.CastPosition);
            }
        }

        // Gets triggered when an enemy casts a gapcloser
        private void OnEnemyGapcloser(ActiveGapcloser args)
        {
            // Simple logic, shouldn't be used in a real project
            if (!args.Sender.IsEnemy || !Menu.Item(Menu.Name + ".e-gapcloser").GetValue<bool>())
            {
                return;
            }
            if (E.IsReady())
            {
                E.Cast(args.End.Extend(args.Start, -E.Range));
            }
        }
    }
}