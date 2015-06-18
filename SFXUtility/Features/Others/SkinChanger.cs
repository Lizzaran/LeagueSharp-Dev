#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 SkinChanger.cs is part of SFXUtility.

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
using SFXUtility.Data;

#endregion

namespace SFXUtility.Features.Others
{
    // Credits: Tree
    internal class SkinChanger : Child<Others>
    {
        private const float CheckInterval = 300f;
        private float _lastCheck;
        private ModelUnit _player;
        private List<ModelUnit> _playerList;
        public SkinChanger(SFXUtility sfx) : base(sfx) {}

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

        protected override void OnLoad()
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
            _playerList = new List<ModelUnit>();
            _lastCheck = Environment.TickCount;


            foreach (var hero in GameObjects.Heroes)
            {
                var localHero = hero;
                var champMenu = new Menu(
                    (hero.IsAlly ? "A: " : "E: ") + hero.ChampionName, (hero.IsAlly ? "a" : "e") + hero.ChampionName);
                var modelUnit = new ModelUnit(hero);

                _playerList.Add(modelUnit);

                if (hero.IsMe)
                {
                    _player = modelUnit;
                }

                foreach (Dictionary<string, object> skin in ModelManager.GetSkins(hero.ChampionName))
                {
                    var localSkin = skin;
                    var skinName = skin["name"].ToString().Equals("default")
                        ? hero.ChampionName
                        : skin["name"].ToString();
                    var changeSkin = champMenu.AddItem(new MenuItem(skinName, skinName).SetValue(false));
                    if (changeSkin.IsActive())
                    {
                        modelUnit.SetModel(hero.BaseSkinName, (int) skin["num"]);
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
                Menu.AddSubMenu(champMenu);
            }

            base.OnInitialize();
        }

        private void OnGameInput(GameInputEventArgs args)
        {
            if (args.Input.StartsWith("/model"))
            {
                args.Process = false;
                var modelName = args.Input.Replace("/model ", string.Empty);
                var model = modelName.GetValidModel();

                if (model == "" || !model.IsValidModel())
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
                _player.SetModel(_player.Unit.BaseSkinName, skin);
            }
        }

        private void OnGameUpdate(EventArgs args)
        {
            if (_lastCheck + CheckInterval > Environment.TickCount)
            {
                return;
            }

            _lastCheck = Environment.TickCount;

            foreach (var unit in _playerList)
            {
                unit.OnUpdate();
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
            Model = unit.BaseSkinName;
            SkinIndex = unit.BaseSkinId;
            UpdateAdditionalObjects();
        }

        #region UpdateAdditionalObjects

        private void UpdateAdditionalObjects()
        {
            var championName = Unit.ChampionName;
            if (championName.Equals("Lulu"))
            {
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
                AdditionalObjects.Add("RammusDBC");
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

            if (championName.Equals("Anivia"))
            {
                IgnoredModels.Add("AniviaEgg");
                AdditionalObjects.Add("AniviaIceblock");
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

        private void UpdateSpawnedUnits()
        {
            SpawnedUnits.RemoveAll(obj => !obj.IsValid);

            var unit = ObjectManager.GetUnitByNetworkId<GameObject>(Unit.AI_LastPetSpawnedID);

            if (unit != null && unit.IsValid && !SpawnedUnits.Contains(unit) && !(unit is Obj_LampBulb))
            {
                SpawnedUnits.Add((Obj_AI_Base) unit);
            }
        }

        public void OnUpdate()
        {
            UpdateSpawnedUnits();

            if (Unit.IsDead)
            {
                return;
            }

            var model = IgnoredModels.Contains(Unit.BaseSkinName) ? Unit.BaseSkinName : Model;
            var skin = SkinIndex;

            if (!Unit.BaseSkinName.Equals(model) || !Unit.BaseSkinId.Equals(skin))
            {
                Unit.SetSkin(model, skin);
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