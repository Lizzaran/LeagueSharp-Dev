#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 SkinChanger.cs is part of SFXSkinChanger.

 SFXSkinChanger is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXSkinChanger is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXSkinChanger. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Collections.Generic;
using LeagueSharp;
using LeagueSharp.Common;
using SFXLibrary;
using SFXLibrary.Logger;
using SFXSkinChanger.Classes;
using SFXSkinChanger.Data;

#endregion

namespace SFXSkinChanger.Features.Others
{
    // Credits: Tree
    internal class SkinChanger : Child<App>
    {
        private readonly List<ModelUnit> _playerList = new List<ModelUnit>();
        private ModelUnit _player;

        public SkinChanger(App parent) : base(parent)
        {
            OnLoad();
        }

        public override string Name
        {
            get { return Global.Lang.Get("F_SkinChanger"); }
        }

        protected override void OnEnable()
        {
            Game.OnInput += OnGameInput;
            Game.OnUpdate += OnGameUpdate;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            Game.OnInput -= OnGameInput;
            Game.OnUpdate -= OnGameUpdate;
            base.OnDisable();
        }

        protected override sealed void OnLoad()
        {
            try
            {
                Menu = new Menu(Name, Name);
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
            try
            {
                foreach (var hero in GameObjects.Heroes)
                {
                    try
                    {
                        var localHero = hero;
                        var champMenu = new Menu(
                            (hero.IsAlly ? "A: " : "E: ") + hero.ChampionName,
                            (hero.IsAlly ? "a" : "e") + hero.ChampionName);
                        var modelUnit = new ModelUnit(hero);

                        _playerList.Add(modelUnit);

                        if (hero.IsMe)
                        {
                            _player = modelUnit;
                        }

                        foreach (Dictionary<string, object> skin in ModelManager.GetSkins(hero.ChampionName))
                        {
                            try
                            {
                                var localSkin = skin;
                                var skinName = skin["name"].ToString().Equals("default")
                                    ? hero.ChampionName
                                    : skin["name"].ToString();
                                var changeSkin = champMenu.AddItem(new MenuItem(skinName, skinName).SetValue(false));
                                if (changeSkin.IsActive())
                                {
                                    modelUnit.SetModel(hero.CharData.BaseSkinName, (int) skin["num"]);
                                }

                                changeSkin.ValueChanged += (s, e) =>
                                {
                                    if (e.GetNewValue<bool>())
                                    {
                                        champMenu.Items.ForEach(
                                            p =>
                                            {
                                                if (p.GetValue<bool>() && p.Name != skinName)
                                                {
                                                    p.SetValue(false);
                                                }
                                            });
                                        modelUnit.SetModel(localHero.ChampionName, (int) localSkin["num"]);
                                    }
                                };
                            }
                            catch (Exception ex)
                            {
                                Global.Logger.AddItem(new LogItem(ex));
                            }
                        }
                        Menu.AddSubMenu(champMenu);
                    }
                    catch (Exception ex)
                    {
                        Global.Logger.AddItem(new LogItem(ex));
                    }
                }
                base.OnInitialize();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnGameInput(GameInputEventArgs args)
        {
            try
            {
                if (args.Input.StartsWith("/model"))
                {
                    args.Process = false;
                    var model = args.Input.Replace("/model ", string.Empty).GetValidModel();

                    if (!model.IsValidModel())
                    {
                        return;
                    }
                    _player.SetModel(model);
                    return;
                }

                if (args.Input.StartsWith("/skin"))
                {
                    args.Process = false;
                    var skin = Convert.ToInt32(args.Input.Replace("/skin ", string.Empty));
                    _player.SetModel(_player.Unit.CharData.BaseSkinName, skin);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnGameUpdate(EventArgs args)
        {
            try
            {
                foreach (var unit in _playerList)
                {
                    unit.OnUpdate();
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }

    internal class ModelUnit
    {
        public static List<string> AdditionalObjects = new List<string>();
        public static List<string> IgnoredModels = new List<string>();
        public string Model;
        public int SkinIndex;
        public List<Obj_AI_Base> SpawnedUnits = new List<Obj_AI_Base>();
        public Obj_AI_Hero Unit;

        public ModelUnit(Obj_AI_Hero unit)
        {
            Unit = unit;
            Model = Unit.CharData.BaseSkinName;
            SkinIndex = unit.BaseSkinId;
        }

        #region UpdateAdditionalObjects

        // ReSharper disable once UnusedMember.Local
        private void UpdateAdditionalObjects()
        {
            var championName = Unit.ChampionName;
            // these need testing
            if (championName.Equals("Lulu"))
            {
                // not sure these are needed
                // can just update RobotBuddy
                AdditionalObjects.Add("LuluCupcake");
                AdditionalObjects.Add("LuluDragon");
                AdditionalObjects.Add("LuluFaerie");
                AdditionalObjects.Add("LuluKitty");
                AdditionalObjects.Add("LuluLadybug");
                AdditionalObjects.Add("LuluPig");
                AdditionalObjects.Add("LuluSnowman");
                AdditionalObjects.Add("LuluSquill");
                return;
            }

            if (championName.Equals("Rammus"))
            {
                AdditionalObjects.Add("RammusDBC"); // is this right?
                IgnoredModels.Add("RammusPB");
                return;
            }

            if (championName.Equals("Udyr"))
            {
                IgnoredModels.Add("UdyrPhoenix");
                IgnoredModels.Add("UdyrPhoenixUlt");
                IgnoredModels.Add("UdyrTiger");
                IgnoredModels.Add("UdyrTigerUlt");
                IgnoredModels.Add("UdyrTurtle");
                IgnoredModels.Add("UdyrTurtleUlt");
                IgnoredModels.Add("UdyrUlt");
                return;
            }
            //

            if (championName.Equals("Anivia"))
            {
                IgnoredModels.Add("AniviaEgg");
                AdditionalObjects.Add("AniviaIceblock");
                return;
            }

            if (championName.Equals("Annie"))
            {
                //covered through pet
                AdditionalObjects.Add("AnnieTibbers");
                return;
            }

            if (championName.Equals("Azir"))
            {
                AdditionalObjects.Add("AzirSoldier");
                AdditionalObjects.Add("AzirSunDisc");
                AdditionalObjects.Add("AzirUltSoldier");
                return;
            }

            if (championName.Equals("Bard"))
            {
                AdditionalObjects.Add("BardFollower");
                AdditionalObjects.Add("BardHealthShrine");
                AdditionalObjects.Add("BardPickup");
                AdditionalObjects.Add("BardPickupNoIcon");
                return;
            }

            if (championName.Equals("Caitlyn"))
            {
                AdditionalObjects.Add("CaitlynTrap");
                return;
            }

            if (championName.Equals("Cassiopeia"))
            {
                IgnoredModels.Add("Cassiopeia_Death");
                return;
            }

            if (championName.Equals("Elise"))
            {
                AdditionalObjects.Add("EliseSpiderling");
                IgnoredModels.Add("EliseSpider");
                return;
            }

            if (championName.Equals("Fizz"))
            {
                AdditionalObjects.Add("FizzBait");
                AdditionalObjects.Add("FizzShark");
                return;
            }

            if (championName.Equals("Gnar"))
            {
                IgnoredModels.Add("GnarBig");
                return;
            }

            if (championName.Equals("Heimerdinger"))
            {
                AdditionalObjects.Add("HeimerTBlue");
                AdditionalObjects.Add("HeimerTYellow");
                return;
            }

            if (championName.Equals("JarvanIV"))
            {
                AdditionalObjects.Add("JarvanIVStandard");
                AdditionalObjects.Add("JarvanIVWall");
                return;
            }

            if (championName.Equals("Jinx"))
            {
                AdditionalObjects.Add("JinxMine");
                return;
            }

            if (championName.Equals("KogMaw"))
            {
                IgnoredModels.Add("KogMawDead");
                return;
            }

            if (championName.Equals("Lulu"))
            {
                AdditionalObjects.Add("RobotBuddy");
                return;
            }

            if (championName.Equals("Malzahar"))
            {
                AdditionalObjects.Add("MalzaharVoidling");
                return;
            }

            if (championName.Equals("Maokai"))
            {
                AdditionalObjects.Add("MaokaiSproutling");
                return;
            }

            if (championName.Equals("MonkeyKing"))
            {
                AdditionalObjects.Add("MonkeyKingClone");
                IgnoredModels.Add("MonkeyKingFlying");
                return;
            }

            if (championName.Equals("Nasus"))
            {
                IgnoredModels.Add("NasusUlt");
                return;
            }

            if (championName.Equals("Olaf"))
            {
                AdditionalObjects.Add("OlafAxe");
                return;
            }

            if (championName.Equals("Reksai"))
            {
                AdditionalObjects.Add("RekSaiTunnel");
                return;
            }

            if (championName.Equals("Shaco"))
            {
                AdditionalObjects.Add("ShacoBox");
                return;
            }

            if (championName.Equals("Shyvana"))
            {
                IgnoredModels.Add("ShyvanaDragon");
                return;
            }

            if (championName.Equals("Syndra"))
            {
                AdditionalObjects.Add("SyndraSphere");
                AdditionalObjects.Add("SyndraOrbs"); // needs an update function
                return;
            }

            if (championName.Equals("Teemo"))
            {
                AdditionalObjects.Add("TeemoMushroom");
                return;
            }

            if (championName.Equals("Thresh"))
            {
                AdditionalObjects.Add("ThreshLantern");
                return;
            }

            if (championName.Equals("Trundle"))
            {
                AdditionalObjects.Add("TrundleWall");
                return;
            }

            if (championName.Equals("Viktor"))
            {
                AdditionalObjects.Add("ViktorSingularity");
                return;
            }

            if (championName.Equals("Xerath"))
            {
                AdditionalObjects.Add("XerathArcaneBarrageLauncher");
                return;
            }

            if (championName.Equals("Yorick"))
            {
                AdditionalObjects.Add("YorickDecayedGhoul");
                AdditionalObjects.Add("YorickRavenousGhoul");
                AdditionalObjects.Add("YorickSpectralGhoul");
                return;
            }

            if (championName.Equals("Zac"))
            {
                IgnoredModels.Add("ZacRebirthBloblet");
                return;
            }

            if (championName.Equals("Zed"))
            {
                AdditionalObjects.Add("ZedShadow");
                return;
            }

            if (championName.Equals("Zyra"))
            {
                AdditionalObjects.Add("ZyraGraspingPlant");
                AdditionalObjects.Add("ZyraSeed");
                AdditionalObjects.Add("ZyraThornPlant");
                IgnoredModels.Add("ZyraPassive");
            }
        }

        #endregion

        // ReSharper disable once UnusedMember.Local
        private void UpdateSpawnedUnits()
        {
            SpawnedUnits.RemoveAll(obj => !obj.IsValid);
            if (Unit.AI_LastPetSpawnedID == 0)
            {
                return;
            }
            var unit = ObjectManager.GetUnitByNetworkId<Obj_AI_Base>(Unit.AI_LastPetSpawnedID);
            if (unit != null && unit.IsValid && !SpawnedUnits.Contains(unit))
            {
                SpawnedUnits.Add(unit);
            }
        }

        public void OnUpdate()
        {
            if (Unit.IsDead)
            {
                return;
            }

            //var model = IgnoredModels.Contains(Unit.CharData.BaseSkinName) ? Unit.CharData.BaseSkinName : Model;
            var skin = SkinIndex;

            if (!Unit.CharData.BaseSkinName.Equals(Model) || !Unit.BaseSkinId.Equals(skin))
            {
                Unit.SetSkin(Model, skin);
            }
        }

        public void SetModel(string model, int skin = 0)
        {
            if (!model.IsValidModel())
            {
                return;
            }
            Model = model;
            SkinIndex = skin;
            OnUpdate();
        }
    }
}