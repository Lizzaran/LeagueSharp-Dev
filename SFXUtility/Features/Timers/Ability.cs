#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Ability.cs is part of SFXUtility.

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
    using SFXLibrary.Logger;
    using SharpDX;
    using SharpDX.Direct3D9;

    #endregion

    internal class Ability : Base
    {
        // ReSharper disable StringLiteralTypo
        private readonly Dictionary<string, AbilityItem> _abilities = new Dictionary<string, AbilityItem>
        {
            {"absolutezero2_green_cas.troy", new AbilityItem("nunu", "R " + Global.Lang.Get("G_Ally"), 3f, true)},
            {"absolutezero2_red_cas.troy", new AbilityItem("nunu", "R " + Global.Lang.Get("G_Enemy"), 3f, true)},
            {"akali_base_smoke_bomb_tar_team_green.troy", new AbilityItem("akali", "W " + Global.Lang.Get("G_Ally"), 8f, true)},
            {"akali_base_smoke_bomb_tar_team_red.troy", new AbilityItem("akali", "W " + Global.Lang.Get("G_Enemy"), 8f, true)},
            {"azir_base_r_soldiercape.troy", new AbilityItem("azir", "R", 5f, true)},
            {"azir_base_w_soldiercape.troy", new AbilityItem("azir", "W", 9f, true)},
            {"bard_base_e_door.troy", new AbilityItem("bard", "E", 10f, true)},
            {"bard_base_r_stasis_skin_full.troy", new AbilityItem("bard", "R", 2.5f, true)},
            {"card_blue.troy", new AbilityItem("twistedfate", "W " + Global.Lang.Get("G_Blue"), 6f, false)},
            {"card_red.troy", new AbilityItem("twistedfate", "W " + Global.Lang.Get("G_Red"), 6f, false)},
            {"card_yellow.troy", new AbilityItem("twistedfate", "W " + Global.Lang.Get("G_Yellow"), 6f, false)},
            {"counterstrike_cas.troy", new AbilityItem("jax", "R", 2f, false)},
            {"diplomaticimmunity_buf.troy", new AbilityItem("poppy", "R", 8f, false)},
            {"dr_mundo_heal.troy", new AbilityItem("mundo", "R", 12f, false)},
            {"eggtimer.troy", new AbilityItem("anivia", Global.Lang.Get("Ability_Passive"), 6f, true)},
            {"eyeforaneye_cas.troy", new AbilityItem("kayle", "R " + Global.Lang.Get("G_Ally"), 3f, false)},
            {"eyeforaneye_self.troy", new AbilityItem("kayle", "R " + Global.Lang.Get("G_Self"), 3f, false)},
            {"galio_talion_channel.troy", new AbilityItem("galio", "R", 2f, true)},
            {"infiniteduress_tar.troy", new AbilityItem("warwick", "R", 1.8f, true)},
            {"jinx_base_e_mine_idle_green.troy", new AbilityItem("jinx", "E " + Global.Lang.Get("G_Ally"), 5f, true)},
            {"jinx_base_e_mine_idle_red.troy", new AbilityItem("jinx", "E " + Global.Lang.Get("G_Enemy"), 5f, true)},
            {"karthus_base_r_cas.troy", new AbilityItem("karthus", "R", 3f, false)},
            {"karthus_base_w_wall.troy", new AbilityItem("karthus", "W", 5f, true)},
            {"kennen_lr_buf.troy", new AbilityItem("kennen", "E", 2f, false)},
            {"kennen_ss_aoe_green.troy", new AbilityItem("kennen", "R " + Global.Lang.Get("G_Ally"), 3f, false)},
            {"kennen_ss_aoe_red.troy", new AbilityItem("kennen", "R " + Global.Lang.Get("G_Enemy"), 3f, false)},
            {"leblanc_base_rw_return_indicator.troy", new AbilityItem("leblanc", "R W", 4f, true)},
            {"leblanc_base_w_return_indicator.troy", new AbilityItem("leblanc", "W", 4f, true)},
            {"lifeaura.troy", new AbilityItem("items", Global.Lang.Get("Ability_Guardian"), 4f, true)},
            {"lissandra_base_r_iceblock.troy", new AbilityItem("lissandra", "R " + Global.Lang.Get("G_Self"), 2.5f, true)},
            {"lissandra_base_r_ring_green.troy", new AbilityItem("lissandra", "R " + Global.Lang.Get("G_Ally"), 1.5f, true)},
            {"lissandra_base_r_ring_red.troy", new AbilityItem("lissandra", "R " + Global.Lang.Get("G_Enemy"), 1.5f, true)},
            {"malzahar_base_r_tar.troy", new AbilityItem("malzahar", "R", 3f, true)},
            {"maokai_base_r_aura.troy", new AbilityItem("maokai", "R", 10f, false)},
            {"masteryi_base_w_buf.troy", new AbilityItem("masteryi", "W", 4f, true)},
            {"monkeyking_base_r_cas.troy", new AbilityItem("wukong", "R", 4f, false)},
            {"morgana_base_r_indicator_ring.troy", new AbilityItem("morgana", "R", 3.5f, false)},
            {"nickoftime_tar.troy", new AbilityItem("zilean", "R", 5f, false)},
            {"olaf_ragnorok_enraged.troy", new AbilityItem("olaf", "R", 6f, false)},
            {"pantheon_base_r_cas.troy", new AbilityItem("pantheon", "R 1", 2f, true)},
            {"pantheon_base_r_indicator_green.troy", new AbilityItem("pantheon", "R 2 " + Global.Lang.Get("G_Ally"), 4.5f, true)},
            {"pantheon_base_r_indicator_red.troy", new AbilityItem("pantheon", "R 2 " + Global.Lang.Get("G_Enemy"), 4.5f, true)},
            {"passive_death_activate.troy", new AbilityItem("aatrox", Global.Lang.Get("Ability_Passive"), 3f, true)},
            {"pirate_cannonbarrage_aoe_indicator_green.troy", new AbilityItem("gangplank", "R " + Global.Lang.Get("G_Ally"), 7f, true)},
            {"pirate_cannonbarrage_aoe_indicator_red.troy", new AbilityItem("gangplank", "R " + Global.Lang.Get("G_Enemy"), 7f, true)},
            {"reapthewhirlwind_green_cas.troy", new AbilityItem("janna", "R " + Global.Lang.Get("G_Ally"), 3f, true)},
            {"reapthewhirlwind_red_cas.troy", new AbilityItem("janna", "R " + Global.Lang.Get("G_Enemy"), 3f, true)},
            {"shen_standunited_shield_v2.troy", new AbilityItem("shen", "R", 3f, false)},
            {"sion_base_r_cas.troy", new AbilityItem("sion", "R", 8f, false)},
            {"skarner_base_r_beam.troy", new AbilityItem("skarner", "R", 2f, false)},
            {"thresh_base_lantern_cas_green.troy", new AbilityItem("tresh", "W " + Global.Lang.Get("G_Ally"), 6f, true)},
             {"thresh_base_lantern_cas_red.troy", new AbilityItem("tresh", "W " + Global.Lang.Get("G_Enemy"), 6f, true)},
            {"undyingrage_glow.troy", new AbilityItem("tryndamere", "R", 5f, false)},
            {"veigar_base_e_cage_green.troy", new AbilityItem("veigar", "E " + Global.Lang.Get("G_Ally"), 3f, true)},
            {"veigar_base_e_cage_red.troy", new AbilityItem("veigar", "E " + Global.Lang.Get("G_Enemy"), 3f, true)},
            {"veigar_base_w_cas_green.troy", new AbilityItem("veigar", "W " + Global.Lang.Get("G_Ally"), 1.2f, true)},
            {"veigar_base_w_cas_red.troy", new AbilityItem("veigar", "W " + Global.Lang.Get("G_Enemy"), 1.2f, true)},
            {"velkoz_base_r_beam_eye.troy", new AbilityItem("anivia", "R", 2.5f, true)},
            {"vladimir_base_w_buf.troy", new AbilityItem("vladimir", "W", 2f, false)},
            {"yasuo_base_w_windwall_activate.troy", new AbilityItem("yasuo", "W", 4f, true)},
            {"zac_r_tar.troy", new AbilityItem("zac", "R", 4f, false)},
            {"zed_base_r_cloneswap_buf.troy", new AbilityItem("zed", "R", 7f, true)},
            {"zed_base_w_cloneswap_buf.troy", new AbilityItem("zed", "W", 4.5f, true)},
            {"zilean_base_r_buf.troy", new AbilityItem("zilean", "R " + Global.Lang.Get("Ability_Revive"), 3f, true)},
            {"zyra_r_cast_green_team.troy", new AbilityItem("zyra", "R " + Global.Lang.Get("G_Ally"), 2f, true)},
            {"zyra_r_cast_red_team.troy", new AbilityItem("zyra", "R " + Global.Lang.Get("G_Enemy"), 2f, true)},
            {"zhonyas_ring_activate.troy", new AbilityItem("items", Global.Lang.Get("Ability_Zhonyas"), 2.5f, true)}
        };

        // ReSharper restore StringLiteralTypo
        private readonly List<AbilityDraw> _drawings = new List<AbilityDraw>();
        private Timers _parent;
        private Font _text;

        public override bool Enabled
        {
            get { return !Unloaded && _parent != null && _parent.Enabled && Menu != null && Menu.Item(Name + "Enabled").GetValue<bool>(); }
        }

        public override string Name
        {
            get { return Global.Lang.Get("F_Ability"); }
        }

        protected override void OnEnable()
        {
            GameObject.OnCreate += OnGameObjectCreate;
            GameObject.OnDelete += OnGameObjectDelete;

            Drawing.OnPreReset += OnDrawingPreReset;
            Drawing.OnPostReset += OnDrawingPostReset;
            Drawing.OnEndScene += OnDrawingEndScene;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            GameObject.OnCreate -= OnGameObjectCreate;
            GameObject.OnDelete -= OnGameObjectDelete;

            Drawing.OnPreReset -= OnDrawingPreReset;
            Drawing.OnPostReset -= OnDrawingPostReset;
            Drawing.OnEndScene -= OnDrawingEndScene;

            OnUnload(null, new UnloadEventArgs());

            base.OnDisable();
        }

        protected override void OnUnload(object sender, UnloadEventArgs args)
        {
            try
            {
                if (args != null && args.Final)
                    base.OnUnload(sender, args);

                if (Initialized)
                {
                    OnDrawingPreReset(null);
                    OnDrawingPostReset(null);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnGameObjectDelete(GameObject sender, EventArgs args)
        {
            try
            {
                if (!sender.IsValid || sender.Type != GameObjectType.obj_GeneralParticleEmitter || (!sender.IsMe && !sender.IsAlly && !sender.IsEnemy))
                    return;

                _drawings.RemoveAll(i => i.Id == sender.NetworkId || Game.Time > i.End);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnGameObjectCreate(GameObject sender, EventArgs args)
        {
            try
            {
                if (!sender.IsValid || sender.Type != GameObjectType.obj_GeneralParticleEmitter ||
                    sender.IsMe && !Menu.Item(Menu.Name + "DrawingSelf").GetValue<bool>() ||
                    sender.IsAlly && !Menu.Item(Menu.Name + "DrawingAlly").GetValue<bool>() ||
                    sender.IsEnemy && !Menu.Item(Menu.Name + "DrawingEnemy").GetValue<bool>())
                    return;

                AbilityItem ability;
                if (_abilities.TryGetValue(sender.Name.ToLower(), out ability))
                {
                    if (ability.Enabled)
                    {
                        _drawings.Add(ability.IsFixed
                            ? new AbilityDraw {Id = sender.NetworkId, Position = sender.Position, End = Game.Time + ability.Time, Color = ability.Color}
                            : new AbilityDraw
                            {
                                Id = sender.NetworkId,
                                Hero = HeroManager.AllHeroes.FirstOrDefault(h => h.NetworkId == sender.NetworkId),
                                End = Game.Time + ability.Time,
                                Color = ability.Color
                            });
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnDrawingEndScene(EventArgs args)
        {
            try
            {
                if (Drawing.Direct3DDevice == null || Drawing.Direct3DDevice.IsDisposed)
                    return;

                var outline = Menu.Item(Menu.Name + "DrawingOutline").GetValue<bool>();
                var offsetTop = Menu.Item(Menu.Name + "DrawingOffsetTop").GetValue<Slider>().Value;
                var offsetLeft = Menu.Item(Menu.Name + "DrawingOffsetLeft").GetValue<Slider>().Value;

                foreach (var ability in _drawings.Where(d => d.Position.IsOnScreen() && d.End > Game.Time))
                {
                    var position = Drawing.WorldToScreen(ability.Position);
                    var time = (ability.End - Game.Time).ToString("0.0");

                    if (outline)
                    {
                        _text.DrawText(null, time, (int) position.X + 1 + offsetLeft, (int) position.Y + 1 + offsetTop, Color.Black);
                        _text.DrawText(null, time, (int) position.X - 1 + offsetLeft, (int) position.Y - 1 + offsetTop, Color.Black);
                        _text.DrawText(null, time, (int) position.X + 1 + offsetLeft, (int) position.Y + offsetTop, Color.Black);
                        _text.DrawText(null, time, (int) position.X - 1 + offsetLeft, (int) position.Y + offsetTop, Color.Black);
                    }

                    _text.DrawText(null, time, (int)position.X + offsetLeft, (int)position.Y + offsetTop, ability.Color);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnDrawingPostReset(EventArgs args)
        {
            try
            {
                _text.OnResetDevice();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnDrawingPreReset(EventArgs args)
        {
            try
            {
                _text.OnLostDevice();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected override void OnGameLoad(EventArgs args)
        {
            try
            {
                if (Global.IoC.IsRegistered<Timers>())
                {
                    _parent = Global.IoC.Resolve<Timers>();
                    if (_parent.Initialized)
                        OnParentInitialized(null, null);
                    else
                        _parent.OnInitialized += OnParentInitialized;
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnParentInitialized(object sender, EventArgs eventArgs)
        {
            try
            {
                if (_parent.Menu == null)
                    return;

                Menu = new Menu(Name, Name);

                var drawingMenu = new Menu(Global.Lang.Get("G_Drawing"), Name + "Drawing");
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + "FontSize", Global.Lang.Get("G_FontSize")).SetValue(new Slider(30, 10, 40)));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "OffsetTop", Global.Lang.Get("G_Offset") + " " + Global.Lang.Get("G_Top")).SetValue(new Slider(0)));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "OffsetLeft", Global.Lang.Get("G_Offset") + " " + Global.Lang.Get("G_Left")).SetValue(new Slider(0)));
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + "Outline", Global.Lang.Get("G_Outline")).SetValue(true));
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + "Self", Global.Lang.Get("G_Self")).SetValue(false));
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + "Enemy", Global.Lang.Get("G_Enemy")).SetValue(false));
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + "Ally", Global.Lang.Get("G_Ally")).SetValue(false));

                Menu.AddSubMenu(drawingMenu);

                var group = 1;
                var counter = 0;

                var spellMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Spell") + " " + group, Name + "Spell" + group));
                var listItems = _abilities.OrderBy(a => a.Value.Champ).GroupBy(a => a.Value.Champ).ToList();
                foreach (var items in listItems)
                {
                    var champMenu = new Menu(items.Key.FirstCharToUpper(), spellMenu + items.Key);
                    foreach (var item in items)
                    {
                        var localItem = item;
                        var mItem =
                            new MenuItem(champMenu.Name + item.Value.Name, item.Value.Name.FirstCharToUpper()).SetValue(new Circle(true,
                                System.Drawing.Color.FromArgb(item.Value.Color.A, item.Value.Color.R, item.Value.Color.G, item.Value.Color.B)));
                        champMenu.AddItem(mItem).ValueChanged += delegate(object o, OnValueChangeEventArgs args)
                        {
                            localItem.Value.Enabled = args.GetNewValue<Circle>().Active;
                            var color = args.GetNewValue<Circle>().Color;
                            localItem.Value.Color = new Color(color.R, color.G, color.B, color.A);
                        };

                        localItem.Value.Enabled = mItem.GetValue<Circle>().Active;
                        var tColor = mItem.GetValue<Circle>().Color;
                        localItem.Value.Color = new Color(tColor.R, tColor.G, tColor.B, tColor.A);
                    }
                    spellMenu.AddSubMenu(champMenu);
                    counter++;
                    if (counter == 10 && group * 10 != listItems.Count())
                    {
                        counter = 0;
                        group++;
                        spellMenu = Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Spell") + " " + group, Name + "Spell" + group));
                    }
                }

                Menu.AddItem(new MenuItem(Name + "Enabled", Global.Lang.Get("G_Enabled")).SetValue(false));

                _parent.Menu.AddSubMenu(Menu);

                _text = new Font(Drawing.Direct3DDevice,
                    new FontDescription
                    {
                        FaceName = Global.DefaultFont,
                        Height = Menu.Item(Name + "DrawingFontSize").GetValue<Slider>().Value,
                        OutputPrecision = FontPrecision.Default,
                        Quality = FontQuality.Default
                    });

                HandleEvents(_parent);
                RaiseOnInitialized();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        internal class AbilityItem
        {
            public AbilityItem(string champ, string name, float time, bool isFixed, Color color = default(Color))
            {
                Name = name;
                Champ = champ;
                Time = time;
                IsFixed = isFixed;
                Enabled = false;
                Color = color == default(Color) ? Color.White : color;
            }

            public string Name { get; private set; }
            public string Champ { get; private set; }
            public bool IsFixed { get; private set; }
            public float Time { get; private set; }
            public bool Enabled { get; set; }
            public Color Color { get; set; }
        }

        internal class AbilityDraw
        {
            private Vector3 _position;
            public Color Color { get; set; }

            public Vector3 Position
            {
                get
                {
                    if (Hero != null)
                    {
                        return Hero.Position;
                    }
                    return _position;
                }
                set { _position = value; }
            }

            public Obj_AI_Hero Hero { get; set; }
            public float End { get; set; }

            public int Id { get; set; }
        }
    }
}