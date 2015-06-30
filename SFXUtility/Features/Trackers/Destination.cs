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
using SharpDX.Direct3D9;
using Color = System.Drawing.Color;

#endregion

namespace SFXUtility.Features.Trackers
{
    // Credits: Screeder

    internal class Destination : Child<Trackers>
    {
        private const float CheckInterval = 300f;
        private List<DestinationObject> _destinations;
        private float _lastCheck;
        private Line _line;
        public Destination(SFXUtility sfx) : base(sfx) {}

        public override string Name
        {
            get { return Global.Lang.Get("F_Destination"); }
        }

        protected override void OnEnable()
        {
            Game.OnUpdate += OnGameUpdate;
            Obj_AI_Base.OnProcessSpellCast += OnObjAiBaseProcessSpellCast;
            Drawing.OnEndScene += OnDrawingEndScene;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            Game.OnUpdate -= OnGameUpdate;
            Obj_AI_Base.OnProcessSpellCast -= OnObjAiBaseProcessSpellCast;
            Drawing.OnEndScene -= OnDrawingEndScene;
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

            _line = MDrawing.GetLine(2);

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
            _destinations.RemoveAll(d => string.IsNullOrWhiteSpace(d.SpellName));
        }

        private void OnDrawingEndScene(EventArgs args)
        {
            var color = Menu.Item(Name + "DrawingColor").GetValue<Color>();
            var radius = Menu.Item(Name + "DrawingCircleRadius").GetValue<Slider>().Value;
            var thickness = Menu.Item(Name + "DrawingCircleThickness").GetValue<Slider>().Value;

            foreach (var destination in
                _destinations.Where(
                    destination =>
                        destination.Casted && (destination.EndPos.IsOnScreen() || destination.StartPos.IsOnScreen())))
            {
                _line.Begin();
                _line.Draw(
                    new[] { Drawing.WorldToScreen(destination.EndPos), Drawing.WorldToScreen(destination.StartPos) },
                    new ColorBGRA(color.R, color.G, color.B, color.A));
                _line.End();
                Render.Circle.DrawCircle(destination.EndPos, radius, color, thickness);
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
                if (args.SData.Name.Equals("VayneInquisition", StringComparison.OrdinalIgnoreCase))
                {
                    if (destination.ExtraTicks > 0)
                    {
                        destination.ExtraTicks = (int) Game.Time + 5 + 2 * args.Level;
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
                            destination.EndPos = CalculateEndPos(args.Start, args.End, destination.Range);
                            break;

                        case "deceive":
                            destination.StartPos = args.Start;
                            destination.EndPos = CalculateEndPos(args.Start, args.End, destination.Range);
                            break;

                        case "leblancslidem":
                            _destinations[index - 2].Casted = false;
                            destination.StartPos = _destinations[index - 2].StartPos;
                            destination.EndPos = CalculateEndPos(args.Start, args.End, destination.Range);
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
                            destination.EndPos = CalculateEndPos(args.Start, args.End, destination.Range);
                            break;
                    }
                    destination.Casted = true;
                    destination.TimeCasted = (int) Game.Time;
                    return;
                }

                index++;
            }
        }

        private Vector3 CalculateEndPos(Vector3 start, Vector3 end, float maxRange)
        {
            var dist = start.Distance(end);
            var endPos = end;
            if (dist > maxRange)
            {
                endPos = start.Extend(end, maxRange);
            }
            if (endPos.IsWall())
            {
                for (var i = 0; i < 200; i = i + 10)
                {
                    var pos = start.Extend(endPos, dist + i);
                    if (!pos.IsWall())
                    {
                        return pos;
                    }
                }
            }
            return endPos;
        }

        private void OnGameUpdate(EventArgs args)
        {
            if (_lastCheck + CheckInterval > Environment.TickCount)
            {
                return;
            }

            _lastCheck = Environment.TickCount;

            foreach (var destination in _destinations.Where(destination => destination.Casted))
            {
                if (Game.Time > destination.TimeCasted + 5f || destination.Hero.IsDead)
                {
                    destination.Casted = false;
                }
                if (destination.Hero.IsVisible)
                {
                    destination.EndPos = destination.Hero.Position;
                }
            }
        }

        private class DestinationObject
        {
            public DestinationObject(Obj_AI_Hero hero, SpellDataInst spell)
            {
                Hero = hero;
                if (spell != null && spell.Slot != SpellSlot.Unknown)
                {
                    SpellName = spell.SData.Name;
                    Range = spell.SData.CastRange;
                }
            }

            public Obj_AI_Hero Hero { get; private set; }
            public float Range { get; private set; }
            public string SpellName { get; private set; }
            public bool Casted { get; set; }
            public Vector3 EndPos { get; set; }
            public int ExtraTicks { get; set; }
            public Vector3 StartPos { get; set; }
            public int TimeCasted { get; set; }
        }
    }
}