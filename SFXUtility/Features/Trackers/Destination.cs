#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Destination.cs is part of SFXUtility.

 SFXUtility is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXUtility is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXUtility. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXLibrary;
using SFXLibrary.Logger;
using SFXUtility.Classes;
using SharpDX;
using Color = System.Drawing.Color;

#endregion

namespace SFXUtility.Features.Trackers
{
    // Credits: Screeder

    internal class Destination : Child<Trackers>
    {
        private List<DestinationObject> _destinations;
        public Destination(SFXUtility sfx) : base(sfx) {}

        public override string Name
        {
            get { return Global.Lang.Get("F_Destination"); }
        }

        protected override void OnEnable()
        {
            Game.OnUpdate += OnGameUpdate;
            Obj_AI_Base.OnProcessSpellCast += OnObjAiBaseProcessSpellCast;
            GameObject.OnCreate += OnObjAiBaseCreate;
            Drawing.OnDraw += OnDrawingDraw;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            Game.OnUpdate -= OnGameUpdate;
            Obj_AI_Base.OnProcessSpellCast -= OnObjAiBaseProcessSpellCast;
            GameObject.OnCreate -= OnObjAiBaseCreate;
            Drawing.OnDraw -= OnDrawingDraw;
            base.OnDisable();
        }

        protected override void OnLoad()
        {
            try
            {
                Menu = new Menu(Name, Name);
                var drawingMenu = new Menu(Global.Lang.Get("G_Drawing"), Name + "Drawing");
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "Color", Global.Lang.Get("G_Color")).SetValue(Color.YellowGreen));
                drawingMenu.AddItem(
                    new MenuItem(
                        drawingMenu.Name + "CircleRadius",
                        Global.Lang.Get("G_Circle") + " " + Global.Lang.Get("G_Radius")).SetValue(new Slider(30)));
                drawingMenu.AddItem(
                    new MenuItem(
                        drawingMenu.Name + "CircleThickness",
                        Global.Lang.Get("G_Circle") + " " + Global.Lang.Get("G_Thickness")).SetValue(
                            new Slider(2, 1, 10)));

                Menu.AddSubMenu(drawingMenu);

                Menu.AddItem(new MenuItem(Name + "Enabled", Global.Lang.Get("G_Enabled")).SetValue(false));

                Parent.Menu.AddSubMenu(Menu);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected override void OnInitialize()
        {
            _destinations = new List<DestinationObject>();

            SetupDestinations();

            if (_destinations.Count == 0)
            {
                OnUnload(null, new UnloadEventArgs(true));
                return;
            }

            base.OnInitialize();
        }

        private void SetupDestinations()
        {
            foreach (var hero in GameObjects.EnemyHeroes)
            {
                foreach (var spell in
                    hero.Spellbook.Spells.Where(
                        spell => spell.Name.Equals("SummonerFlash", StringComparison.OrdinalIgnoreCase)))
                {
                    _destinations.Add(new DestinationObject(hero, spell));
                }

                switch (hero.ChampionName)
                {
                    case "Ezreal":
                        _destinations.Add(
                            new DestinationObject(
                                hero,
                                hero.Spellbook.Spells.FirstOrDefault(
                                    s => s.SData.Name.Equals("EzrealArcaneShift", StringComparison.OrdinalIgnoreCase))));
                        break;
                    case "Fiora":
                        _destinations.Add(
                            new DestinationObject(
                                hero,
                                hero.Spellbook.Spells.FirstOrDefault(
                                    s => s.SData.Name.Equals("FioraDance", StringComparison.OrdinalIgnoreCase))));
                        break;
                    case "Kassadin":
                        _destinations.Add(
                            new DestinationObject(
                                hero,
                                hero.Spellbook.Spells.FirstOrDefault(
                                    s => s.SData.Name.Equals("RiftWalk", StringComparison.OrdinalIgnoreCase))));
                        break;
                    case "Katarina":
                        _destinations.Add(
                            new DestinationObject(
                                hero,
                                hero.Spellbook.Spells.FirstOrDefault(
                                    s => s.SData.Name.Equals("KatarinaE", StringComparison.OrdinalIgnoreCase))));
                        break;
                    case "Leblanc":
                        _destinations.Add(
                            new DestinationObject(
                                hero,
                                hero.Spellbook.Spells.FirstOrDefault(
                                    s => s.SData.Name.Equals("LeblancSlide", StringComparison.OrdinalIgnoreCase))));
                        _destinations.Add(
                            new DestinationObject(
                                hero,
                                hero.Spellbook.Spells.FirstOrDefault(
                                    s => s.SData.Name.Equals("LeblancSlideReturn", StringComparison.OrdinalIgnoreCase))));
                        _destinations.Add(
                            new DestinationObject(
                                hero,
                                hero.Spellbook.Spells.FirstOrDefault(
                                    s => s.SData.Name.Equals("LeblancSlideM", StringComparison.OrdinalIgnoreCase))));
                        _destinations.Add(
                            new DestinationObject(
                                hero,
                                hero.Spellbook.Spells.FirstOrDefault(
                                    s => s.SData.Name.Equals("LeblancSlideReturnM", StringComparison.OrdinalIgnoreCase))));
                        break;
                    case "Lissandra":
                        _destinations.Add(
                            new DestinationObject(
                                hero,
                                hero.Spellbook.Spells.FirstOrDefault(
                                    s => s.SData.Name.Equals("LissandraE", StringComparison.OrdinalIgnoreCase))));
                        break;
                    case "MasterYi":
                        _destinations.Add(
                            new DestinationObject(
                                hero,
                                hero.Spellbook.Spells.FirstOrDefault(
                                    s => s.SData.Name.Equals("AlphaStrike", StringComparison.OrdinalIgnoreCase))));
                        break;
                    case "Shaco":
                        _destinations.Add(
                            new DestinationObject(
                                hero,
                                hero.Spellbook.Spells.FirstOrDefault(
                                    s => s.SData.Name.Equals("Deceive", StringComparison.OrdinalIgnoreCase))));
                        break;
                    case "Talon":
                        _destinations.Add(
                            new DestinationObject(
                                hero,
                                hero.Spellbook.Spells.FirstOrDefault(
                                    s => s.SData.Name.Equals("TalonCutthroat", StringComparison.OrdinalIgnoreCase))));
                        break;
                    case "Vayne":
                        _destinations.Add(
                            new DestinationObject(
                                hero,
                                hero.Spellbook.Spells.FirstOrDefault(
                                    s => s.SData.Name.Equals("VayneTumble", StringComparison.OrdinalIgnoreCase))));
                        break;
                    case "Zed":
                        _destinations.Add(
                            new DestinationObject(
                                hero,
                                hero.Spellbook.Spells.FirstOrDefault(
                                    s => s.SData.Name.Equals("ZedShadowDash", StringComparison.OrdinalIgnoreCase))));
                        break;
                }
            }
            _destinations.RemoveAll(d => string.IsNullOrEmpty(d.SpellName));
        }

        private void OnDrawingDraw(EventArgs args)
        {
            var color = Menu.Item(Name + "DrawingColor").GetValue<Color>();
            var radius = Menu.Item(Name + "DrawingCircleRadius").GetValue<Slider>().Value;
            var thickness = Menu.Item(Name + "DrawingCircleThickness").GetValue<Slider>().Value;

            foreach (var destination in
                _destinations.Where(destination => destination.Casted)
                    .Where(destination => destination.EndPos.IsOnScreen() || destination.StartPos.IsOnScreen()))
            {
                if (destination.OutOfBush)
                {
                    Render.Circle.DrawCircle(destination.EndPos, destination.Range, color, thickness);
                }
                else
                {
                    Render.Circle.DrawCircle(destination.EndPos, radius, color, thickness);
                    Drawing.DrawLine(
                        Drawing.WorldToScreen(destination.StartPos), Drawing.WorldToScreen(destination.EndPos), 2f,
                        color);
                }
            }
        }

        private void OnObjAiBaseProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            var hero = sender as Obj_AI_Hero;
            if (hero == null || !hero.IsValid || !hero.IsEnemy)
            {
                return;
            }

            var index = 0;
            foreach (var destination in _destinations.Where(destination => destination.Hero.NetworkId == hero.NetworkId)
                )
            {
                var target = args.Target as Obj_AI_Hero;
                if (target != null && target.IsValid)
                {
                    destination.Target = target;
                }

                if (args.SData.Name.Equals("VayneInquisition", StringComparison.OrdinalIgnoreCase))
                {
                    if (destination.ExtraTicks > 0)
                    {
                        destination.ExtraTicks = (int) Game.Time + 6 + 2 * args.Level;
                        return;
                    }
                }
                if (args.SData.Name.Equals(destination.SpellName, StringComparison.OrdinalIgnoreCase))
                {
                    switch (destination.SpellName.ToLower())
                    {
                        case "vaynetumble":
                            if (Game.Time >= destination.ExtraTicks)
                            {
                                return;
                            }
                            destination.StartPos = args.Start;
                            destination.EndPos = CalculateEndPos(destination, args);
                            break;

                        case "deceive":
                            destination.OutOfBush = false;
                            destination.StartPos = args.Start;
                            destination.EndPos = CalculateEndPos(destination, args);
                            break;

                        case "leblancslidem":
                            _destinations[index - 2].Casted = false;
                            destination.StartPos = _destinations[index - 2].StartPos;
                            destination.EndPos = CalculateEndPos(destination, args);
                            break;

                        case "leblancslidereturn":
                        case "leblancslidereturnm":
                            if (destination.SpellName == "leblancslidereturn")
                            {
                                _destinations[index - 1].Casted = false;
                                _destinations[index + 1].Casted = false;
                                _destinations[index + 2].Casted = false;
                            }
                            else
                            {
                                _destinations[index - 3].Casted = false;
                                _destinations[index - 2].Casted = false;
                                _destinations[index - 1].Casted = false;
                            }
                            destination.StartPos = args.Start;
                            destination.EndPos = _destinations[index - 1].StartPos;
                            break;

                        case "fioraDance":
                        case "alphaStrike":
                            destination.StartPos = args.Start;
                            destination.EndPos = args.Target.Position;
                            break;

                        default:
                            destination.StartPos = args.Start;
                            destination.EndPos = CalculateEndPos(destination, args);
                            break;
                    }
                    destination.Casted = true;
                    destination.TimeCasted = (int) Game.Time;
                    return;
                }

                index++;
            }
        }

        private Vector3 CalculateEndPos(DestinationObject destination, GameObjectProcessSpellCastEventArgs args)
        {
            var dist = Vector3.Distance(args.Start, args.End);
            if (dist <= destination.Range)
            {
                destination.EndPos = args.End;
            }
            else
            {
                var norm = args.Start - args.End;
                norm.Normalize();
                var endPos = args.Start - norm * destination.Range;
                destination.EndPos = endPos;
            }
            return destination.EndPos;
        }

        private void OnObjAiBaseCreate(GameObject sender, EventArgs args)
        {
            foreach (var destination in _destinations)
            {
                if (destination.Hero.ChampionName.Equals("Shaco", StringComparison.OrdinalIgnoreCase))
                {
                    if (sender.Type != GameObjectType.obj_LampBulb &&
                        sender.Name.Equals("JackInTheBoxPoof2.troy", StringComparison.OrdinalIgnoreCase) &&
                        !destination.Casted)
                    {
                        destination.StartPos = sender.Position;
                        destination.EndPos = sender.Position;
                        destination.Casted = true;
                        destination.TimeCasted = (int) Game.Time;
                        destination.OutOfBush = true;
                    }
                }
            }
        }

        private void OnGameUpdate(EventArgs args)
        {
            foreach (var destination in _destinations.Where(destination => destination.Casted))
            {
                if (destination.SpellName.Equals("FioraDance", StringComparison.OrdinalIgnoreCase) ||
                    destination.SpellName.Equals("AlphaStrike", StringComparison.OrdinalIgnoreCase) &&
                    destination.Target != null && !destination.Target.IsDead)
                {
                    if (Game.Time > (destination.TimeCasted + destination.Delay + 0.2f))
                    {
                        destination.Casted = false;
                    }
                }
                else if (destination.Target != null && destination.Target.IsDead)
                {
                    var temp = destination.EndPos;
                    destination.EndPos = destination.StartPos;
                    destination.StartPos = temp;
                }
                else if (destination.Hero.IsDead ||
                         (!destination.Hero.IsValid && Game.Time > (destination.TimeCasted + 2)) ||
                         Game.Time > (destination.TimeCasted + 5 + destination.Delay))
                {
                    destination.Casted = false;
                }
                else if (!destination.OutOfBush && destination.Hero.IsVisible &&
                         Game.Time > (destination.TimeCasted + destination.Delay))
                {
                    destination.EndPos = destination.Hero.Position;
                }
            }
        }

        private class DestinationObject
        {
            public readonly float Delay;
            public readonly Obj_AI_Hero Hero;
            public readonly float Range;
            public readonly string SpellName;
            public bool Casted;
            public Vector3 EndPos;
            public int ExtraTicks;
            public bool OutOfBush;
            public Vector3 StartPos;
            public Obj_AI_Hero Target;
            public int TimeCasted;

            public DestinationObject(Obj_AI_Hero hero, SpellDataInst spell)
            {
                Hero = hero;
                if (spell != null)
                {
                    SpellName = spell.SData.Name;
                    Range = spell.SData.CastRange;
                    Delay = spell.SData.SpellCastTime;
                }
            }
        }
    }
}