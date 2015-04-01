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
    using SFXLibrary.Extensions.NET;
    using SFXLibrary.IoCContainer;
    using SFXLibrary.Logger;
    using SharpDX;

    #endregion

    internal class Jungle : Base
    {
        private readonly List<Camp> _camps = new List<Camp>();
        private Timers _parent;

        public Jungle(IContainer container) : base(container)
        {
            CustomEvents.Game.OnGameLoad += OnGameLoad;
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
            foreach (var camp in _camps)
            {
                camp.Active = true;
            }
            GameObject.OnCreate += OnGameObjectCreate;
            GameObject.OnDelete += OnGameObjectDelete;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            foreach (var camp in _camps)
            {
                camp.Active = false;
            }
            GameObject.OnCreate -= OnGameObjectCreate;
            GameObject.OnDelete -= OnGameObjectDelete;
            base.OnDisable();
        }

        private void OnGameObjectDelete(GameObject sender, EventArgs args)
        {
            try
            {
                if (!sender.IsValid || sender.Type != GameObjectType.obj_AI_Minion || sender.Team != GameObjectTeam.Neutral)
                    return;

                foreach (var camp in _camps)
                {
                    var mob = camp.Mobs.FirstOrDefault(m => m.Name.Contains(sender.Name, StringComparison.OrdinalIgnoreCase));
                    if (mob != null)
                    {
                        mob.Dead = true;
                        camp.Dead = camp.Mobs.All(m => m.Dead);
                        if (camp.Dead)
                        {
                            camp.Dead = true;
                            camp.NextRespawnTime = (int) Game.ClockTime + camp.RespawnTime - 5;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
            }
        }

        private void OnGameObjectCreate(GameObject sender, EventArgs args)
        {
            if (!sender.IsValid || sender.Type != GameObjectType.obj_AI_Minion || sender.Team != GameObjectTeam.Neutral)
                return;

            foreach (var camp in _camps)
            {
                var mob = camp.Mobs.FirstOrDefault(m => m.Name.Contains(sender.Name, StringComparison.OrdinalIgnoreCase));
                if (mob != null)
                {
                    mob.Dead = false;
                    camp.Dead = false;
                }
            }
        }

        private void OnGameLoad(EventArgs args)
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
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + "FontSize", "Font Size").SetValue(new Slider(13, 3, 30)));

                Menu.AddSubMenu(drawingMenu);

                Menu.AddItem(new MenuItem(Name + "Enabled", "Enabled").SetValue(false));

                Menu.Item(Name + "DrawingTimeFormat").ValueChanged += delegate(object o, OnValueChangeEventArgs args)
                {
                    foreach (var camp in _camps)
                    {
                        camp.TotalSeconds = args.GetNewValue<StringList>().SelectedIndex == 1;
                    }
                };

                _parent.Menu.AddSubMenu(Menu);

                SetupCamps(Menu.Item(Name + "DrawingFontSize").GetValue<Slider>().Value,
                    Menu.Item(Name + "DrawingTimeFormat").GetValue<StringList>().SelectedIndex == 1);

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

        private void SetupCamps(int fontSize, bool totalSeconds)
        {
            switch (Utility.Map.GetMap().Type)
            {
                case Utility.Map.MapType.SummonersRift:
                    _camps.AddRange(new List<Camp>
                    {
// Order: Blue
                        new Camp(115, 300, new Vector3(3800.99f, 7883.53f, 52.18f),
                            new[] {new Mob("SRU_Blue1.1.1"), new Mob("SRU_BlueMini1.1.2"), new Mob("SRU_BlueMini21.1.3")}, fontSize, Logger)
                        {
                            TotalSeconds = totalSeconds
                        },
                        //Order: Wolves
                        new Camp(115, 100, new Vector3(3849.95f, 6504.36f, 52.46f),
                            new[] {new Mob("SRU_Murkwolf2.1.1"), new Mob("SRU_MurkwolfMini2.1.2"), new Mob("SRU_MurkwolfMini2.1.3")}, fontSize, Logger)
                        {
                            TotalSeconds = totalSeconds
                        },
                        //Order: Chicken
                        new Camp(115, 100, new Vector3(6943.41f, 5422.61f, 52.62f),
                            new[]
                            {
                                new Mob("SRU_Razorbeak3.1.1"), new Mob("SRU_RazorbeakMini3.1.2"), new Mob("SRU_RazorbeakMini3.1.3"),
                                new Mob("SRU_RazorbeakMini3.1.4")
                            }, fontSize, Logger) {TotalSeconds = totalSeconds},
                        //Order: Red
                        new Camp(115, 300, new Vector3(7813.07f, 4051.33f, 53.81f),
                            new[] {new Mob("SRU_Red4.1.1"), new Mob("SRU_RedMini4.1.2"), new Mob("SRU_RedMini4.1.3")}, fontSize, Logger)
                        {
                            TotalSeconds = totalSeconds
                        },
                        //Order: Krug
                        new Camp(115, 100, new Vector3(8370.58f, 2718.15f, 51.09f), new[] {new Mob("SRU_Krug5.1.2"), new Mob("SRU_KrugMini5.1.1")},
                            fontSize, Logger) {TotalSeconds = totalSeconds},
                        //Order: Gromp
                        new Camp(115, 100, new Vector3(2164.34f, 8383.02f, 51.78f), new[] {new Mob("SRU_Gromp13.1.1")}, fontSize, Logger)
                        {
                            TotalSeconds = totalSeconds
                        },

                        //Chaos: Blue
                        new Camp(115, 300, new Vector3(10984.11f, 6960.31f, 51.72f),
                            new[] {new Mob("SRU_Blue7.1.1"), new Mob("SRU_BlueMini7.1.2"), new Mob("SRU_BlueMini27.1.3")}, fontSize, Logger)
                        {
                            TotalSeconds = totalSeconds
                        },
                        //Chaos: Wolves
                        new Camp(115, 100, new Vector3(10983.83f, 8328.73f, 62.22f),
                            new[] {new Mob("SRU_Murkwolf8.1.1"), new Mob("SRU_MurkwolfMini8.1.2"), new Mob("SRU_MurkwolfMini8.1.3")}, fontSize, Logger)
                        {
                            TotalSeconds = totalSeconds
                        },
                        //Chaos: Chicken
                        new Camp(115, 100, new Vector3(7852.38f, 9562.62f, 52.30f),
                            new[]
                            {
                                new Mob("SRU_Razorbeak9.1.1"), new Mob("SRU_RazorbeakMini9.1.2"), new Mob("SRU_RazorbeakMini9.1.3"),
                                new Mob("SRU_RazorbeakMini9.1.4")
                            }, fontSize, Logger) {TotalSeconds = totalSeconds},
                        //Chaos: Red
                        new Camp(115, 300, new Vector3(7139.29f, 10779.34f, 56.38f),
                            new[] {new Mob("SRU_Red10.1.1"), new Mob("SRU_RedMini10.1.2"), new Mob("SRU_RedMini10.1.3")}, fontSize, Logger)
                        {
                            TotalSeconds = totalSeconds
                        },
                        //Chaos: Krug
                        new Camp(115, 100, new Vector3(6476.17f, 12142.51f, 56.48f), new[] {new Mob("SRU_Krug11.1.2"), new Mob("SRU_KrugMini11.1.1")},
                            fontSize, Logger) {TotalSeconds = totalSeconds},
                        //Chaos: Gromp
                        new Camp(115, 100, new Vector3(12671.83f, 6306.60f, 51.71f), new[] {new Mob("SRU_Gromp14.1.1")}, fontSize, Logger)
                        {
                            TotalSeconds = totalSeconds
                        },

                        //Neutral: Dragon
                        new Camp(150, 360, new Vector3(9813.83f, 4360.19f, -71.24f), new[] {new Mob("SRU_Dragon6.1.1")}, fontSize, Logger)
                        {
                            TotalSeconds = totalSeconds
                        },
                        //Neutral: Baron
                        new Camp(120, 420, new Vector3(4993.14f, 10491.92f, -71.24f), new[] {new Mob("SRU_Baron12.1.1")}, fontSize, Logger)
                        {
                            TotalSeconds = totalSeconds
                        },
                        //Dragon: Crab
                        new Camp(150, 180, new Vector3(10647.70f, 5144.68f, -62.81f), new[] {new Mob("SRU_Crab15.1.1")}, fontSize, Logger)
                        {
                            TotalSeconds = totalSeconds
                        },
                        //Baron: Crab
                        new Camp(150, 180, new Vector3(4285.04f, 9597.52f, -67.60f), new[] {new Mob("SRU_Crab16.1.1")}, fontSize, Logger)
                        {
                            TotalSeconds = totalSeconds
                        }
                    });
                    break;
                case Utility.Map.MapType.TwistedTreeline:
                    _camps.AddRange(new List<Camp>
                    {
//Order: Wraiths
                        new Camp(100, 75, new Vector3(3550f, 6250f, 60f),
                            new[] {new Mob("TT_NWraith1.1.1"), new Mob("TT_NWraith21.1.2"), new Mob("TT_NWraith21.1.3")}, fontSize, Logger)
                        {
                            TotalSeconds = totalSeconds
                        },
                        //Order: Golems
                        new Camp(100, 75, new Vector3(4500f, 8550f, 60f), new[] {new Mob("TT_NGolem2.1.1"), new Mob("TT_NGolem22.1.2")}, fontSize,
                            Logger) {TotalSeconds = totalSeconds},
                        //Order: Wolves
                        new Camp(100, 75, new Vector3(5600f, 6400f, 60f),
                            new[] {new Mob("TT_NWolf3.1.1"), new Mob("TT_NWolf23.1.2"), new Mob("TT_NWolf23.1.3")}, fontSize, Logger)
                        {
                            TotalSeconds = totalSeconds
                        },

                        //Chaos: Wraiths
                        new Camp(100, 75, new Vector3(10300f, 6250f, 60f),
                            new[] {new Mob("TT_NWraith4.1.1"), new Mob("TT_NWraith24.1.2"), new Mob("TT_NWraith24.1.3")}, fontSize, Logger)
                        {
                            TotalSeconds = totalSeconds
                        },
                        //Chaos: Golems
                        new Camp(100, 75, new Vector3(9800f, 8550f, 60f), new[] {new Mob("TT_NGolem5.1.1"), new Mob("TT_NGolem25.1.2")}, fontSize,
                            Logger) {TotalSeconds = totalSeconds},
                        //Chaos: Wolves
                        new Camp(100, 75, new Vector3(8600f, 6400f, 60f),
                            new[] {new Mob("TT_NWolf6.1.1"), new Mob("TT_NWolf26.1.2"), new Mob("TT_NWolf26.1.3")}, fontSize, Logger)
                        {
                            TotalSeconds = totalSeconds
                        },

                        //Neutral: Vilemaw
                        new Camp(600, 300, new Vector3(7150f, 11100f, 60f), new[] {new Mob("TT_Spiderboss8.1.1")}, fontSize, Logger)
                        {
                            TotalSeconds = totalSeconds
                        }
                    });
                    break;
            }
        }

        private class Camp
        {
            private readonly Render.Text _text;
            private bool _active;
            private bool _added;

            public Camp(float spawnTime, float respawnTime, Vector3 position, Mob[] mobs, int fontSize, ILogger logger)
            {
                SpawnTime = spawnTime;
                RespawnTime = respawnTime;
                Position = position;
                Mobs = mobs;

                _text = new Render.Text(Drawing.WorldToMinimap(position), string.Empty, fontSize, Color.White)
                {
                    OutLined = true,
                    Centered = true,
                    VisibleCondition = delegate
                    {
                        try
                        {
                            return Active && Dead;
                        }
                        catch (Exception ex)
                        {
                            logger.AddItem(new LogItem(ex) {Object = this});
                            return false;
                        }
                    },
                    TextUpdate = delegate
                    {
                        try
                        {
                            if (NextRespawnTime - (int) Game.Time <= 0)
                                Dead = false;
                            return (NextRespawnTime - (int) Game.Time).FormatTime(TotalSeconds);
                        }
                        catch (Exception ex)
                        {
                            logger.AddItem(new LogItem(ex) {Object = this});
                            return string.Empty;
                        }
                    }
                };
            }

            public bool TotalSeconds { private get; set; }

            public bool Active
            {
                private get { return _active; }
                set
                {
                    _active = value;
                    Update();
                }
            }

            private void Update()
            {
                if (_active && !_added)
                {
                    _text.Add(0);
                    _added = true;
                }
                else if (!_active && _added)
                {
                    _text.Remove();
                    _added = false;
                }
            }

            // ReSharper disable once MemberCanBePrivate.Local
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public float SpawnTime { get; private set; }
            public float RespawnTime { get; private set; }
            public Vector3 Position { get; private set; }
            public Mob[] Mobs { get; private set; }
            public float NextRespawnTime { get; set; }
            public bool Dead { get; set; }
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