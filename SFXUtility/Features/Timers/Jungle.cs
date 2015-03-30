#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Jungle.cs is part of SFXUtility.

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

namespace SFXUtility.Features.Timers
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Classes;
    using LeagueSharp;
    using LeagueSharp.Common;
    using LeagueSharp.CommonEx.Core.Enumerations;
    using LeagueSharp.CommonEx.Core.Events;
    using LeagueSharp.CommonEx.Core.Wrappers;
    using SFXLibrary;
    using SFXLibrary.Extensions.NET;
    using SFXLibrary.IoCContainer;
    using SFXLibrary.Logger;
    using SharpDX;
    using Color = System.Drawing.Color;
    using ObjectHandler = LeagueSharp.CommonEx.Core.ObjectHandler;

    #endregion

    internal class Jungle : Base
    {
        private readonly List<Camp> _camps = new List<Camp>();
        private Timers _parent;

        public Jungle(IContainer container) : base(container)
        {
            Load.OnLoad += OnLoad;
        }

        public override bool Enabled
        {
            get { return _parent != null && _parent.Enabled && Menu != null && Menu.Item(Name + "Enabled").GetValue<bool>(); }
        }

        public override string Name
        {
            get { return "Jungle"; }
        }

        protected override void OnEnable()
        {
            Game.OnUpdate += OnGameUpdate;
            Drawing.OnDraw += OnDrawingDraw;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            Game.OnUpdate -= OnGameUpdate;
            Drawing.OnDraw -= OnDrawingDraw;
            base.OnDisable();
        }

        private void OnGameUpdate(EventArgs args)
        {
            try
            {
                var minions =
                    ObjectHandler.GetFast<Obj_AI_Minion>()
                        .Where(
                            minion =>
                                minion.IsValid && !minion.IsDead && minion.Team == GameObjectTeam.Neutral &&
                                (minion.Name.StartsWith("SRU_", StringComparison.OrdinalIgnoreCase) ||
                                 minion.Name.StartsWith("TT_", StringComparison.OrdinalIgnoreCase)))
                        .ToList();

                foreach (var camp in _camps)
                {
                    var dead = camp.Mobs.All(c => c.Dead);
                    if (dead && camp.NextRespawnTime < Game.Time + 10f)
                    {
                        camp.NextRespawnTime = (int) Game.Time + camp.RespawnTime;
                    }
                    else
                    {
                        foreach (var mob in camp.Mobs)
                        {
                            mob.Dead = !minions.Any(m => m.Name.Equals(mob.Name, StringComparison.OrdinalIgnoreCase));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
            }
        }

        private void OnDrawingDraw(EventArgs args)
        {
            try
            {
                var totalSeconds = Menu.Item("DrawingTimeFormat").GetValue<StringList>().SelectedIndex == 1;
                foreach (var camp in _camps.Where(camp => camp.Mobs.All(c => c.Dead)))
                {
                    Draw.TextCentered(Drawing.WorldToMinimap(camp.Position), Color.White,
                        (camp.NextRespawnTime - (int) Game.Time).FormatTime(totalSeconds));
                }
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
            }
        }

        private void OnLoad(EventArgs args)
        {
            try
            {
                if (IoC.IsRegistered<Timers>())
                {
                    _parent = IoC.Resolve<Timers>();
                    if (_parent.Initialized)
                        OnParentInitialized(null, null);
                    else
                        _parent.OnInitialized += OnParentInitialized;
                }
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
            }
        }

        private void OnParentInitialized(object sender, EventArgs eventArgs)
        {
            try
            {
                if (_parent.Menu == null)
                    return;

                Menu = new Menu(Name, Name);

                var drawingMenu = new Menu("Drawing", Name + "Drawing");
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + "TimeFormat", "Time Format").SetValue(new StringList(new[] {"mm:ss", "ss"})));

                Menu.AddSubMenu(drawingMenu);

                Menu.AddItem(new MenuItem(Name + "Enabled", "Enabled").SetValue(false));

                _parent.Menu.AddSubMenu(Menu);

                SetupCamps();

                if (_camps.Count == 0)
                    return;

                HandleEvents(_parent);
                RaiseOnInitialized();
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
            }
        }

        private void SetupCamps()
        {
            switch (Map.GetMap().Type)
            {
                case MapType.SummonersRift:
                    _camps.AddRange(new List<Camp>
                    {
// Order: Blue
                        new Camp(115, 300, new Vector3(3388.2f, 8400f, 55.2f),
                            new[] {new Mob("SRU_Blue1.1.1"), new Mob("SRU_BlueMini1.1.2"), new Mob("SRU_BlueMini21.1.3")}),
                        //Order: Wolves
                        new Camp(115, 100, new Vector3(3415.8f, 6950f, 55.6f),
                            new[] {new Mob("SRU_Murkwolf2.1.1"), new Mob("SRU_MurkwolfMini2.1.2"), new Mob("SRU_MurkwolfMini2.1.3")}),
                        //Order: Chicken
                        new Camp(115, 100, new Vector3(6500f, 5900f, 60f),
                            new[]
                            {
                                new Mob("SRU_Razorbeak3.1.1"), new Mob("SRU_RazorbeakMini3.1.2"), new Mob("SRU_RazorbeakMini3.1.3"),
                                new Mob("SRU_RazorbeakMini3.1.4")
                            }),
                        //Order: Red
                        new Camp(115, 300, new Vector3(7300.4f, 4600.1f, 56.9f),
                            new[] {new Mob("SRU_Red4.1.1"), new Mob("SRU_RedMini4.1.2"), new Mob("SRU_RedMini4.1.3")}),
                        //Order: Krug
                        new Camp(115, 100, new Vector3(7700.2f, 3200f, 54.3f), new[] {new Mob("SRU_Krug5.1.2"), new Mob("SRU_KrugMini5.1.1")}),
                        //Order: Gromp
                        new Camp(115, 100, new Vector3(1900.1f, 9200f, 54.9f), new[] {new Mob("SRU_Gromp13.1.1")}),

                        //Chaos: Blue
                        new Camp(115, 300, new Vector3(10440f, 7500f, 54.9f),
                            new[] {new Mob("SRU_Blue7.1.1"), new Mob("SRU_BlueMini7.1.2"), new Mob("SRU_BlueMini27.1.3")}),
                        //Chaos: Wolves
                        new Camp(115, 100, new Vector3(10350f, 9000f, 65.5f),
                            new[] {new Mob("SRU_Murkwolf8.1.1"), new Mob("SRU_MurkwolfMini8.1.2"), new Mob("SRU_MurkwolfMini8.1.3")}),
                        //Chaos: Chicken
                        new Camp(115, 100, new Vector3(7100f, 10000f, 55.5f),
                            new[]
                            {
                                new Mob("SRU_Razorbeak9.1.1"), new Mob("SRU_RazorbeakMini9.1.2"), new Mob("SRU_RazorbeakMini9.1.3"),
                                new Mob("SRU_RazorbeakMini9.1.4")
                            }),
                        //Chaos: Red
                        new Camp(115, 300, new Vector3(6450.2f, 11400f, 54.6f),
                            new[] {new Mob("SRU_Red10.1.1"), new Mob("SRU_RedMini10.1.2"), new Mob("SRU_RedMini10.1.3")}),
                        //Chaos: Krug
                        new Camp(115, 100, new Vector3(6005f, 13000f, 39.6f), new[] {new Mob("SRU_Krug11.1.2"), new Mob("SRU_KrugMini11.1.1")}),
                        //Chaos: Gromp
                        new Camp(115, 100, new Vector3(12000f, 7000f, 54.8f), new[] {new Mob("SRU_Gromp14.1.1")}),

                        //Neutral: Dragon
                        new Camp(150, 360, new Vector3(9300.8f, 4200.5f, -60.3f), new[] {new Mob("SRU_Dragon6.1.1")}),
                        //Neutral: Baron
                        new Camp(120, 420, new Vector3(4300.1f, 11600.7f, -63.1f), new[] {new Mob("SRU_Baron12.1.1")}),
                        //Dragon: Crab
                        new Camp(150, 180, new Vector3(10600f, 5600.5f, -60.3f), new[] {new Mob("SRU_Crab15.1.1")}),
                        //Baron: Crab
                        new Camp(150, 180, new Vector3(4200.1f, 9900.7f, -63.1f), new[] {new Mob("SRU_Crab16.1.1")})
                    });
                    break;
                case MapType.TwistedTreeline:
                    _camps.AddRange(new List<Camp>
                    {
//Order: Wraiths
                        new Camp(100, 75, new Vector3(3550f, 6250f, 60f),
                            new[] {new Mob("TT_NWraith1.1.1"), new Mob("TT_NWraith21.1.2"), new Mob("TT_NWraith21.1.3")}),
                        //Order: Golems
                        new Camp(100, 75, new Vector3(4500f, 8550f, 60f), new[] {new Mob("TT_NGolem2.1.1"), new Mob("TT_NGolem22.1.2")}),
                        //Order: Wolves
                        new Camp(100, 75, new Vector3(5600f, 6400f, 60f),
                            new[] {new Mob("TT_NWolf3.1.1"), new Mob("TT_NWolf23.1.2"), new Mob("TT_NWolf23.1.3")}),

                        //Chaos: Wraiths
                        new Camp(100, 75, new Vector3(10300f, 6250f, 60f),
                            new[] {new Mob("TT_NWraith4.1.1"), new Mob("TT_NWraith24.1.2"), new Mob("TT_NWraith24.1.3")}),
                        //Chaos: Golems
                        new Camp(100, 75, new Vector3(9800f, 8550f, 60f), new[] {new Mob("TT_NGolem5.1.1"), new Mob("TT_NGolem25.1.2")}),
                        //Chaos: Wolves
                        new Camp(100, 75, new Vector3(8600f, 6400f, 60f),
                            new[] {new Mob("TT_NWolf6.1.1"), new Mob("TT_NWolf26.1.2"), new Mob("TT_NWolf26.1.3")}),

                        //Neutral: Vilemaw
                        new Camp(600, 300, new Vector3(7150f, 11100f, 60f), new[] {new Mob("TT_Spiderboss8.1.1")})
                    });
                    break;
            }
        }

        private class Camp
        {
            public Camp(float spawnTime, float respawnTime, Vector3 position, Mob[] mobs)
            {
                SpawnTime = spawnTime;
                RespawnTime = respawnTime;
                Position = position;
                Mobs = mobs;
            }

            // ReSharper disable once MemberCanBePrivate.Local
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public float SpawnTime { get; private set; }
            public float RespawnTime { get; private set; }
            public Vector3 Position { get; private set; }
            public Mob[] Mobs { get; private set; }
            public float NextRespawnTime { get; set; }
        }

        private class Mob
        {
            public Mob(string name, bool dead = true)
            {
                Name = name;
                Dead = dead;
            }

            public bool Dead { get; set; }
            public string Name { get; private set; }
        }
    }
}