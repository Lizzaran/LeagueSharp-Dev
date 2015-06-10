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

#region

using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXLibrary.Extensions.NET;
using SFXLibrary.Logger;
using SFXUtility.Classes;
using SharpDX;
using SharpDX.Direct3D9;

#endregion

namespace SFXUtility.Features.Timers
{
    internal class Ability : Child<Timers>
    {
        // ReSharper disable StringLiteralTypo
        private Dictionary<string, AbilityItem> _abilities;

        // ReSharper restore StringLiteralTypo
        private List<AbilityDraw> _drawings;
        private Font _text;
        public Ability(SFXUtility sfx) : base(sfx) {}

        public override string Name
        {
            get { return Global.Lang.Get("F_Ability"); }
        }

        protected override void OnEnable()
        {
            GameObject.OnCreate += OnGameObjectCreate;
            GameObject.OnDelete += OnGameObjectDelete;
            Drawing.OnEndScene += OnDrawingEndScene;

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            GameObject.OnCreate -= OnGameObjectCreate;
            GameObject.OnDelete -= OnGameObjectDelete;
            Drawing.OnEndScene -= OnDrawingEndScene;

            base.OnDisable();
        }

        private void OnGameObjectDelete(GameObject sender, EventArgs args)
        {
            try
            {
                if (!sender.IsValid || sender.Type != GameObjectType.obj_GeneralParticleEmitter ||
                    (!sender.IsMe && !sender.IsAlly && !sender.IsEnemy))
                {
                    return;
                }

                _drawings.RemoveAll(i => i.Object.NetworkId == sender.NetworkId || Game.Time > i.End);
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
                {
                    return;
                }

                AbilityItem ability;
                if (_abilities.TryGetValue(sender.Name.ToLower(), out ability))
                {
                    if (ability.Enabled)
                    {
                        _drawings.Add(
                            new AbilityDraw { Object = sender, End = Game.Time + ability.Time, Color = ability.Color });
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
                {
                    return;
                }

                var outline = Menu.Item(Menu.Name + "DrawingOutline").GetValue<bool>();
                var offsetTop = Menu.Item(Menu.Name + "DrawingOffsetTop").GetValue<Slider>().Value;
                var offsetLeft = Menu.Item(Menu.Name + "DrawingOffsetLeft").GetValue<Slider>().Value;

                foreach (var ability in
                    _drawings.Where(d => d.Object.IsValid && d.Position.IsOnScreen() && d.End > Game.Time))
                {
                    var position = Drawing.WorldToScreen(ability.Position);
                    var time = (ability.End - Game.Time).ToString("0.0");

                    if (outline)
                    {
                        _text.DrawText(
                            null, time, (int) position.X + 1 + offsetLeft, (int) position.Y + 1 + offsetTop, Color.Black);
                        _text.DrawText(
                            null, time, (int) position.X - 1 + offsetLeft, (int) position.Y - 1 + offsetTop, Color.Black);
                        _text.DrawText(
                            null, time, (int) position.X + 1 + offsetLeft, (int) position.Y + offsetTop, Color.Black);
                        _text.DrawText(
                            null, time, (int) position.X - 1 + offsetLeft, (int) position.Y + offsetTop, Color.Black);
                    }

                    _text.DrawText(
                        null, time, (int) position.X + offsetLeft, (int) position.Y + offsetTop, ability.Color);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected override void OnLoad()
        {
            try
            {
                Menu = new Menu(Name, Name);
                var drawingMenu = new Menu(Global.Lang.Get("G_Drawing"), Name + "Drawing");
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "FontSize", Global.Lang.Get("G_FontSize")).SetValue(
                        new Slider(30, 10, 40)));
                drawingMenu.AddItem(
                    new MenuItem(
                        drawingMenu.Name + "OffsetTop", Global.Lang.Get("G_Offset") + " " + Global.Lang.Get("G_Top"))
                        .SetValue(new Slider(0)));
                drawingMenu.AddItem(
                    new MenuItem(
                        drawingMenu.Name + "OffsetLeft", Global.Lang.Get("G_Offset") + " " + Global.Lang.Get("G_Left"))
                        .SetValue(new Slider(0)));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "Outline", Global.Lang.Get("G_Outline")).SetValue(true));
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + "Self", Global.Lang.Get("G_Self")).SetValue(false));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "Enemy", Global.Lang.Get("G_Enemy")).SetValue(false));
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + "Ally", Global.Lang.Get("G_Ally")).SetValue(false));

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
            _drawings = new List<AbilityDraw>();
            _abilities = new Dictionary<string, AbilityItem>
            {
                { "absolutezero2_green_cas.troy", new AbilityItem("nunu", "R " + Global.Lang.Get("G_Ally"), 3f) },
                { "absolutezero2_red_cas.troy", new AbilityItem("nunu", "R " + Global.Lang.Get("G_Enemy"), 3f) },
                {
                    "akali_base_smoke_bomb_tar_team_green.troy",
                    new AbilityItem("akali", "W " + Global.Lang.Get("G_Ally"), 8f)
                },
                {
                    "akali_base_smoke_bomb_tar_team_red.troy",
                    new AbilityItem("akali", "W " + Global.Lang.Get("G_Enemy"), 8f)
                },
                { "azir_base_w_sandbib.troy", new AbilityItem("azir", "W " + Global.Lang.Get("G_Ally"), 9f) },
                { "azir_base_w_sandbib_enemy.troy", new AbilityItem("azir", "W " + Global.Lang.Get("G_Enemy"), 9f) },
                { "azir_base_w_towersandbib.troy", new AbilityItem("azir", "W2 " + Global.Lang.Get("G_Ally"), 4.5f) },
                {
                    "azir_base_w_towersandbib_enemy.troy",
                    new AbilityItem("azir", "W2 " + Global.Lang.Get("G_Enemy"), 4.5f)
                },
                { "azir_base_r_soldiercape.troy", new AbilityItem("azir", "R " + Global.Lang.Get("G_Ally"), 5f) },
                { "azir_base_r_soldiercape_enemy.troy", new AbilityItem("azir", "R " + Global.Lang.Get("G_Enemy"), 5f) },
                { "bard_base_e_door.troy", new AbilityItem("bard", "E", 10f) },
                { "bard_base_r_stasis_skin_full.troy", new AbilityItem("bard", "R", 2.5f) },
                { "card_blue.troy", new AbilityItem("twistedfate", "W " + Global.Lang.Get("G_Blue"), 6f) },
                { "card_red.troy", new AbilityItem("twistedfate", "W " + Global.Lang.Get("G_Red"), 6f) },
                { "card_yellow.troy", new AbilityItem("twistedfate", "W " + Global.Lang.Get("G_Yellow"), 6f) },
                { "counterstrike_cas.troy", new AbilityItem("jax", "R", 2f) },
                { "ekko_base_w_indicator.troy", new AbilityItem("ekko", "W", 3f) },
                { "diplomaticimmunity_buf.troy", new AbilityItem("poppy", "R", 8f) },
                { "dr_mundo_heal.troy", new AbilityItem("mundo", "R", 12f) },
                { "eggtimer.troy", new AbilityItem("anivia", Global.Lang.Get("Ability_Passive"), 6f) },
                { "eyeforaneye_cas.troy", new AbilityItem("kayle", "R " + Global.Lang.Get("G_Ally"), 3f) },
                { "eyeforaneye_self.troy", new AbilityItem("kayle", "R " + Global.Lang.Get("G_Self"), 3f) },
                { "galio_talion_channel.troy", new AbilityItem("galio", "R", 2f) },
                { "viktor_catalyst_green.troy", new AbilityItem("viktor", "W " + Global.Lang.Get("G_Ally"), 4f) },
                { "viktor_catalyst_red.troy", new AbilityItem("viktor", "W " + Global.Lang.Get("G_Enemy"), 4f) },
                { "viktor_base_w_aug_green.troy", new AbilityItem("viktor", "W+ " + Global.Lang.Get("G_Ally"), 4f) },
                { "viktor_base_w_aug_red.troy", new AbilityItem("viktor", "W+ " + Global.Lang.Get("G_Enemy"), 4f) },
                { "viktor_chaosstorm_green.troy", new AbilityItem("viktor", "R " + Global.Lang.Get("G_Ally"), 7f) },
                { "viktor_chaosstorm_red.troy", new AbilityItem("viktor", "R " + Global.Lang.Get("G_Gnemy"), 7f) },
                { "infiniteduress_tar.troy", new AbilityItem("warwick", "R", 1.8f) },
                { "jinx_base_e_mine_idle_green.troy", new AbilityItem("jinx", "E " + Global.Lang.Get("G_Ally"), 5f) },
                { "jinx_base_e_mine_idle_red.troy", new AbilityItem("jinx", "E " + Global.Lang.Get("G_Enemy"), 5f) },
                { "karthus_base_r_cas.troy", new AbilityItem("karthus", "R", 3f) },
                { "karthus_base_w_wall.troy", new AbilityItem("karthus", "W", 5f) },
                { "kennen_lr_buf.troy", new AbilityItem("kennen", "E", 2f) },
                { "kennen_ss_aoe_green.troy", new AbilityItem("kennen", "R " + Global.Lang.Get("G_Ally"), 3f) },
                { "kennen_ss_aoe_red.troy", new AbilityItem("kennen", "R " + Global.Lang.Get("G_Enemy"), 3f) },
                {
                    "rumble_ult_impact_burn_teamid_green.troy",
                    new AbilityItem("rumble", "R " + Global.Lang.Get("G_Ally"), 5f)
                },
                {
                    "rumble_ult_impact_burn_teamid_red.troy",
                    new AbilityItem("rumble", "R " + Global.Lang.Get("G_Enemy"), 5f)
                },
                { "leblanc_base_rw_return_indicator.troy", new AbilityItem("leblanc", "R W", 4f) },
                { "leblanc_base_w_return_indicator.troy", new AbilityItem("leblanc", "W", 4f) },
                { "lifeaura.troy", new AbilityItem("items", Global.Lang.Get("Ability_Guardian"), 4f) },
                {
                    "lissandra_base_r_iceblock.troy", new AbilityItem("lissandra", "R " + Global.Lang.Get("G_Self"), 2.5f)
                },
                {
                    "lissandra_base_r_ring_green.troy",
                    new AbilityItem("lissandra", "R " + Global.Lang.Get("G_Ally"), 1.5f)
                },
                {
                    "lissandra_base_r_ring_red.troy",
                    new AbilityItem("lissandra", "R " + Global.Lang.Get("G_Enemy"), 1.5f)
                },
                { "malzahar_base_r_tar.troy", new AbilityItem("malzahar", "R", 3f) },
                { "maokai_base_r_aura.troy", new AbilityItem("maokai", "R", 10f) },
                { "masteryi_base_w_buf.troy", new AbilityItem("masteryi", "W", 4f) },
                { "monkeyking_base_r_cas.troy", new AbilityItem("wukong", "R", 4f) },
                { "morgana_base_r_indicator_ring.troy", new AbilityItem("morgana", "R", 3.5f) },
                { "ziggs_base_w_aoe_green.troy", new AbilityItem("ziggs", "W " + Global.Lang.Get("G_Ally"), 4f) },
                { "ziggs_base_w_aoe_red.troy", new AbilityItem("ziggs", "W " + Global.Lang.Get("G_Enemy"), 4f) },
                { "nickoftime_tar.troy", new AbilityItem("zilean", "R", 5f) },
                { "olaf_ragnorok_enraged.troy", new AbilityItem("olaf", "R", 6f) },
                { "pantheon_base_r_cas.troy", new AbilityItem("pantheon", "R 1", 2f) },
                {
                    "pantheon_base_r_indicator_green.troy",
                    new AbilityItem("pantheon", "R 2 " + Global.Lang.Get("G_Ally"), 4.5f)
                },
                {
                    "pantheon_base_r_indicator_red.troy",
                    new AbilityItem("pantheon", "R 2 " + Global.Lang.Get("G_Enemy"), 4.5f)
                },
                { "passive_death_activate.troy", new AbilityItem("aatrox", Global.Lang.Get("Ability_Passive"), 3f) },
                {
                    "pirate_cannonbarrage_aoe_indicator_green.troy",
                    new AbilityItem("gangplank", "R " + Global.Lang.Get("G_Ally"), 7f)
                },
                {
                    "pirate_cannonbarrage_aoe_indicator_red.troy",
                    new AbilityItem("gangplank", "R " + Global.Lang.Get("G_Enemy"), 7f)
                },
                { "reapthewhirlwind_green_cas.troy", new AbilityItem("janna", "R " + Global.Lang.Get("G_Ally"), 3f) },
                { "reapthewhirlwind_red_cas.troy", new AbilityItem("janna", "R " + Global.Lang.Get("G_Enemy"), 3f) },
                { "shen_standunited_shield_v2.troy", new AbilityItem("shen", "R", 3f) },
                { "sion_base_r_cas.troy", new AbilityItem("sion", "R", 8f) },
                { "skarner_base_r_beam.troy", new AbilityItem("skarner", "R", 2f) },
                { "thresh_base_lantern_cas_green.troy", new AbilityItem("tresh", "W " + Global.Lang.Get("G_Ally"), 6f) },
                { "thresh_base_lantern_cas_red.troy", new AbilityItem("tresh", "W " + Global.Lang.Get("G_Enemy"), 6f) },
                { "undyingrage_glow.troy", new AbilityItem("tryndamere", "R", 5f) },
                { "veigar_base_e_cage_green.troy", new AbilityItem("veigar", "E " + Global.Lang.Get("G_Ally"), 3f) },
                { "veigar_base_e_cage_red.troy", new AbilityItem("veigar", "E " + Global.Lang.Get("G_Enemy"), 3f) },
                { "veigar_base_w_cas_green.troy", new AbilityItem("veigar", "W " + Global.Lang.Get("G_Ally"), 1.2f) },
                { "veigar_base_w_cas_red.troy", new AbilityItem("veigar", "W " + Global.Lang.Get("G_Enemy"), 1.2f) },
                { "velkoz_base_r_beam_eye.troy", new AbilityItem("anivia", "R", 2.5f) },
                { "vladimir_base_w_buf.troy", new AbilityItem("vladimir", "W", 2f) },
                { "yasuo_base_w_windwall1.troy", new AbilityItem("yasuo", "W1 " + Global.Lang.Get("G_Ally"), 4f) },
                { "yasuo_base_w_windwall2.troy", new AbilityItem("yasuo", "W2 " + Global.Lang.Get("G_Ally"), 4f) },
                { "yasuo_base_w_windwall3.troy", new AbilityItem("yasuo", "W3 " + Global.Lang.Get("G_Ally"), 4f) },
                { "yasuo_base_w_windwall4.troy", new AbilityItem("yasuo", "W4 " + Global.Lang.Get("G_Ally"), 4f) },
                { "yasuo_base_w_windwall5.troy", new AbilityItem("yasuo", "W5 " + Global.Lang.Get("G_Ally"), 4f) },
                {
                    "yasuo_base_w_windwall_enemy_01.troy",
                    new AbilityItem("yasuo", "W1 " + Global.Lang.Get("G_Enemy"), 4f)
                },
                {
                    "yasuo_base_w_windwall_enemy_02.troy",
                    new AbilityItem("yasuo", "W2 " + Global.Lang.Get("G_Enemy"), 4f)
                },
                {
                    "yasuo_base_w_windwall_enemy_03.troy",
                    new AbilityItem("yasuo", "W3 " + Global.Lang.Get("G_Enemy"), 4f)
                },
                {
                    "yasuo_base_w_windwall_enemy_04.troy",
                    new AbilityItem("yasuo", "W4 " + Global.Lang.Get("G_Enemy"), 4f)
                },
                {
                    "yasuo_base_w_windwall_enemy_05.troy",
                    new AbilityItem("yasuo", "W5 " + Global.Lang.Get("G_Enemy"), 4f)
                },
                { "zac_r_tar.troy", new AbilityItem("zac", "R", 4f) },
                { "zed_base_r_cloneswap_buf.troy", new AbilityItem("zed", "R", 7f) },
                { "zed_base_w_cloneswap_buf.troy", new AbilityItem("zed", "W", 4.5f) },
                { "zilean_base_r_buf.troy", new AbilityItem("zilean", "R " + Global.Lang.Get("Ability_Revive"), 3f) },
                { "zyra_r_cast_green_team.troy", new AbilityItem("zyra", "R " + Global.Lang.Get("G_Ally"), 2f) },
                { "zyra_r_cast_red_team.troy", new AbilityItem("zyra", "R " + Global.Lang.Get("G_Enemy"), 2f) },
                { "zhonyas_ring_activate.troy", new AbilityItem("items", Global.Lang.Get("Ability_Zhonyas"), 2.5f) },
                { "jester_copy.troy", new AbilityItem("shaco", "R", 18f) },
                { "fizz_ring_green.troy", new AbilityItem("fizz", "R " + Global.Lang.Get("G_Ally"), 1.5f) },
                { "fizz_ring_red.troy", new AbilityItem("fizz", "R " + Global.Lang.Get("G_Enemy"), 1.5f) }
            };

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
                        new MenuItem(champMenu.Name + item.Value.Name, item.Value.Name.FirstCharToUpper()).SetValue(
                            new Circle(
                                true,
                                System.Drawing.Color.FromArgb(
                                    item.Value.Color.A, item.Value.Color.R, item.Value.Color.G, item.Value.Color.B)));
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
                    spellMenu =
                        Menu.AddSubMenu(new Menu(Global.Lang.Get("G_Spell") + " " + group, Name + "Spell" + group));
                }
            }

            _text = MDrawing.GetFont(Menu.Item(Name + "DrawingFontSize").GetValue<Slider>().Value);

            base.OnInitialize();
        }

        internal class AbilityItem
        {
            public AbilityItem(string champ, string name, float time, Color color = default(Color))
            {
                Name = name;
                Champ = champ;
                Time = time;
                Enabled = false;
                Color = color == default(Color) ? Color.White : color;
            }

            public string Name { get; private set; }
            public string Champ { get; private set; }
            public float Time { get; private set; }
            public bool Enabled { get; set; }
            public Color Color { get; set; }
        }

        internal class AbilityDraw
        {
            public Color Color { get; set; }

            public Vector3 Position
            {
                get { return Object.Position; }
            }

            public GameObject Object { get; set; }
            public float End { get; set; }
        }
    }
}