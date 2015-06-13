#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Ezreal.cs is part of SFXChallenger.

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

//namespace SFXChallenger.Champions
//{
//    #region

//    using System;
//    using System.Drawing;
//    using Abstracts;
//    using Enumerations;
//    using LeagueSharp;
//    using LeagueSharp.Common;
//    using Managers;
//    using SFXLibrary.Logger;
//    using Wrappers;
//    using TargetSelector = Wrappers.TargetSelector;

//    #endregion

//    internal class Ezreal : Champion
//    {
//        protected override ItemFlags ItemFlags
//        {
//            get { return ItemFlags.Offensive | ItemFlags.Defensive | ItemFlags.Flee; }
//        }

//        protected override void OnLoad()
//        {
//            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
//        }

//        protected override void OnUnload()
//        {
//            AntiGapcloser.OnEnemyGapcloser -= OnEnemyGapcloser;
//        }

//        protected override void AddToMenu()
//        {
//            var drawingMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Drawing"), Menu.Name + ".drawing"));
//            drawingMenu.AddItem(
//                new MenuItem(drawingMenu.Name + ".circle-thickness", Global.Lang.Get("G_CircleThickness")).SetValue(new Slider(2, 0, 10)));
//            drawingMenu.AddItem(new MenuItem(drawingMenu.Name + ".q", "Q").SetValue(new Circle(false, Color.White)));
//            drawingMenu.AddItem(new MenuItem(drawingMenu.Name + ".w", "W").SetValue(new Circle(false, Color.White)));
//            drawingMenu.AddItem(new MenuItem(drawingMenu.Name + ".e", "E").SetValue(new Circle(false, Color.White)));

//            var comboMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("C_Combo"), Menu.Name + ".combo"));
//            ManaManager.AddToMenu(comboMenu, "combo", 0);
//            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));
//            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".w", Global.Lang.Get("G_UseW")).SetValue(true));
//            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(true));
//            comboMenu.AddItem(new MenuItem(comboMenu.Name + ".r", Global.Lang.Get("G_UseR")).SetValue(true));

//            var harassMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("C_Harass"), Menu.Name + ".harass"));
//            ManaManager.AddToMenu(harassMenu, "harass");
//            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));
//            harassMenu.AddItem(new MenuItem(harassMenu.Name + ".w", Global.Lang.Get("G_UseW")).SetValue(true));

//            var laneclearMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("C_LaneClear"), Menu.Name + ".lane-clear"));
//            ManaManager.AddToMenu(laneclearMenu, "lane-clear");
//            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".q", Global.Lang.Get("G_UseQ")).SetValue(true));
//            laneclearMenu.AddItem(new MenuItem(laneclearMenu.Name + ".w", Global.Lang.Get("G_UseW")).SetValue(false));

//            var fleeMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("C_Flee"), Menu.Name + ".flee"));
//            ManaManager.AddToMenu(fleeMenu, "flee", 15);
//            fleeMenu.AddItem(new MenuItem(fleeMenu.Name + ".e", Global.Lang.Get("G_UseE")).SetValue(true));

//            var miscMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Miscellaneous"), Menu.Name + ".miscellaneous"));
//            miscMenu.AddItem(new MenuItem(miscMenu.Name + ".anti-gapcloser", Global.Lang.Get("C_AntiGapcloser")).SetValue(true));
//        }

//        protected override void SetupSpells()
//        {
//            Q = new Spell(SpellSlot.Q, 1200f);
//            Q.SetSkillshot(0.25f, 50f, 2000f, true, SkillshotType.SkillshotLine);

//            W = new Spell(SpellSlot.W, 1000f);
//            W.SetSkillshot(0.25f, 80f, 1600f, false, SkillshotType.SkillshotLine);

//            E = new Spell(SpellSlot.E, 475f);
//            E.SetSkillshot(0.25f, 80f, 1600f, false, SkillshotType.SkillshotCircle);

//            R = new Spell(SpellSlot.R, 2500f);
//            R.SetSkillshot(1.2f, 160f, 2000f, false, SkillshotType.SkillshotLine);
//        }

//        protected override void Combo()
//        {
//            var q = Menu.Item(Menu.Name + ".combo.q").GetValue<bool>();
//            var w = Menu.Item(Menu.Name + ".combo.w").GetValue<bool>();
//            var e = Menu.Item(Menu.Name + ".combo.e").GetValue<bool>();
//            var r = Menu.Item(Menu.Name + ".combo.r").GetValue<bool>() && !Player.UnderTurret(true);

//            if (q)
//            {
//                Casting.SkillShot(Q, HitchanceManager.Get("q"));
//            }
//            if (w)
//            {
//                Casting.SkillShot(W, HitchanceManager.Get("w"));
//            }

//            var target = TargetSelector.GetTarget(-1);

//            if (target == null)
//                return;

//            var cDmg = CalculateComboDamage(target, q, w, false, false, 1) + (r ? CalculateRDamage(target) : 0);
//            if (cDmg >= target.Health - 20)
//            {
//                if (e)
//                {
//                    Casting.SkillShot(E, HitchanceManager.Get("e"), true);
//                }
//                if (r)
//                {
//                    Casting.SkillShot(R, HitchanceManager.Get("r"));
//                }
//            }
//            if (cDmg*1.5 > target.Health)
//            {
//                ItemManager.UseComboItems(target);
//                SummonerManager.UseComboSummoners(target);
//            }
//        }

//        private float CalculateRDamage(Obj_AI_Hero target)
//        {
//            var dmg = Player.GetSpellDamage(target, SpellSlot.R);

//            var prediction = R.GetPrediction(target);
//            var collisionCount = prediction.CollisionObjects.Count;

//            if (collisionCount >= 7)
//                dmg = dmg*.3;
//            else if (collisionCount != 0)
//                dmg = dmg*((10d - collisionCount)/10);

//            return (float) dmg;
//        }

//        protected override void Harass()
//        {
//            if (Menu.Item(Menu.Name + ".harass.q").GetValue<bool>())
//            {
//                Casting.SkillShot(Q, HitchanceManager.Get("q"));
//            }
//            if (Menu.Item(Menu.Name + ".harass.w").GetValue<bool>())
//            {
//                Casting.SkillShot(W, HitchanceManager.Get("w"));
//            }
//        }

//        protected override void LaneClear()
//        {
//            if (Menu.Item(Menu.Name + ".lane-clear.q").GetValue<bool>())
//            {
//                Casting.Farm(Q);
//            }
//            if (Menu.Item(Menu.Name + ".lane-clear.w").GetValue<bool>())
//            {
//                Casting.Farm(W);
//            }
//        }

//        protected override void Flee()
//        {
//            ItemManager.UseFleeItems();
//            if (Menu.Item(Menu.Name + ".flee.e").GetValue<bool>() && E.IsReady())
//            {
//                E.Cast(ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range));
//            }
//        }

//        protected override void Killsteal()
//        {
//            KillstealManager.Killsteal();
//        }

//        private void OnEnemyGapcloser(ActiveGapcloser gapcloser)
//        {
//            try
//            {
//                if (Menu.Item(Menu.Name + ".miscellaneous.anti-gapcloser").GetValue<bool>() && E.IsReady())
//                {
//                    E.Cast(ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range));
//                }
//            }
//            catch (Exception ex)
//            {
//                Global.Logger.AddItem(new LogItem(ex));
//            }
//        }

//        protected override void OnDraw()
//        {
//            var q = Menu.Item(Menu.Name + ".drawing.q").GetValue<Circle>();
//            var w = Menu.Item(Menu.Name + ".drawing.w").GetValue<Circle>();
//            var e = Menu.Item(Menu.Name + ".drawing.e").GetValue<Circle>();
//            var circleThickness = Menu.Item(Menu.Name + ".drawing.circle-thickness").GetValue<Slider>().Value;

//            if (q.Active)
//            {
//                Render.Circle.DrawCircle(Player.Position, Q.Range, q.Color, circleThickness);
//            }
//            if (w.Active)
//            {
//                Render.Circle.DrawCircle(Player.Position, W.Range, w.Color, circleThickness);
//            }
//            if (e.Active)
//            {
//                Render.Circle.DrawCircle(Player.Position, E.Range, e.Color, circleThickness);
//            }
//        }
//    }
//}
