#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Models.cs is part of SFXTrinket.

 SFXTrinket is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXTrinket is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXTrinket. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Web.Script.Serialization;
using SFXLibrary.Logger;

#endregion

namespace SFXTrinket.Data
{
    // Credits: Tree
    internal static class ModelManager
    {
        private const string DataDragonBase = "http://ddragon.leagueoflegends.com/";
        private static readonly string GameVersion;

        #region ModelList

        public static List<string> ModelList = new List<string>
        {
            "Aatrox",
            "Ahri",
            "Akali",
            "Alistar",
            "Amumu",
            "AncientGolem",
            "Anivia",
            "AniviaEgg",
            "AniviaIceblock",
            "Annie",
            "AnnieTibbers",
            "ARAMChaosNexus",
            "ARAMChaosTurretFront",
            "ARAMChaosTurretInhib",
            "ARAMChaosTurretNexus",
            "ARAMChaosTurretShrine",
            "ARAMOrderNexus",
            "ARAMOrderTurretFront",
            "ARAMOrderTurretInhib",
            "ARAMOrderTurretNexus",
            "ARAMOrderTurretShrine",
            "AramSpeedShrine",
            "AscRelic",
            "AscWarpIcon",
            "AscXerath",
            "Ashe",
            "Azir",
            "AzirSoldier",
            "AzirSunDisc",
            "AzirTowerClicker",
            "AzirUltSoldier",
            "Bard",
            "BardFollower",
            "BardHealthShrine",
            "BardPickup",
            "BardPickupNoIcon",
            "Blitzcrank",
            "Blue_Minion_Basic",
            "Blue_Minion_MechCannon",
            "Blue_Minion_MechMelee",
            "Blue_Minion_Wizard",
            "BlueTrinket",
            "Brand",
            "Braum",
            "brush_CS_A",
            "brush_CS_B",
            "brush_CS_C",
            "brush_CS_D",
            "brush_CS_E",
            "brush_CS_F",
            "brush_CS_G",
            "brush_CS_H",
            "brush_CS_I",
            "brush_CS_J",
            "brush_HA_A",
            "brush_HA_B",
            "brush_HA_C",
            "brush_HA_D",
            "brush_HA_E",
            "brush_HA_F",
            "brush_HA_G",
            "brush_HA_H",
            "brush_HA_I",
            "brush_HA_J",
            "brush_SRU_A",
            "brush_SRU_B",
            "brush_SRU_C",
            "brush_SRU_D",
            "brush_SRU_E",
            "brush_SRU_F",
            "brush_SRU_G",
            "brush_SRU_H",
            "brush_SRU_I",
            "brush_SRU_J",
            "brush_TT_A",
            "brush_TT_B",
            "brush_TT_C",
            "brush_TT_D",
            "brush_TT_E",
            "brush_TT_F",
            "brush_TT_G",
            "brush_TT_H",
            "brush_TT_I",
            "brush_TT_J",
            "brush_TT_K",
            "brush_TT_L",
            "brush_TT_M",
            "brush_TT_N",
            "brush_TT_O",
            "brush_TT_P",
            "brush_TT_Q",
            "brush_TT_R",
            "brush_TT_S",
            "brush_TT_T",
            "brush_TT_U",
            "Caitlyn",
            "CaitlynTrap",
            "Cassiopeia",
            "Cassiopeia_Death",
            "ChaosInhibitor",
            "ChaosInhibitor_D",
            "ChaosNexus",
            "ChaosTurretGiant",
            "ChaosTurretNormal",
            "ChaosTurretShrine",
            "ChaosTurretTutorial",
            "ChaosTurretWorm",
            "ChaosTurretWorm2",
            "Chogath",
            "Corki",
            "crystal_platform",
            "Darius",
            "DestroyedInhibitor",
            "DestroyedNexus",
            "DestroyedTower",
            "Diana",
            "Dragon",
            "Draven",
            "DrMundo",
            "Ekko",
            "Elise",
            "EliseSpider",
            "EliseSpiderling",
            "Evelynn",
            "Ezreal",
            "Ezreal_cyber_1",
            "Ezreal_cyber_2",
            "Ezreal_cyber_3",
            "FiddleSticks",
            "Fiora",
            "Fizz",
            "FizzBait",
            "FizzShark",
            "Galio",
            "Gangplank",
            "Garen",
            "GhostWard",
            "GiantWolf",
            "Gnar",
            "GnarBig",
            "Golem",
            "GolemODIN",
            "Gragas",
            "Graves",
            "GreatWraith",
            "HA_AP_BannerMidBridge",
            "HA_AP_BridgeLaneStatue",
            "HA_AP_Chains",
            "HA_AP_Chains_Long",
            "HA_AP_ChaosTurret",
            "HA_AP_ChaosTurret2",
            "HA_AP_ChaosTurret3",
            "HA_AP_ChaosTurretRubble",
            "HA_AP_ChaosTurretShrine",
            "HA_AP_ChaosTurretTutorial",
            "HA_AP_Cutaway",
            "HA_AP_HealthRelic",
            "HA_AP_Hermit",
            "HA_AP_Hermit_Robot",
            "HA_AP_HeroTower",
            "HA_AP_OrderCloth",
            "HA_AP_OrderShrineTurret",
            "HA_AP_OrderTurret",
            "HA_AP_OrderTurret2",
            "HA_AP_OrderTurret3",
            "HA_AP_OrderTurretRubble",
            "HA_AP_OrderTurretTutorial",
            "HA_AP_PeriphBridge",
            "HA_AP_Poro",
            "HA_AP_PoroSpawner",
            "HA_AP_ShpNorth",
            "HA_AP_ShpSouth",
            "HA_AP_Viking",
            "HA_ChaosMinionMelee",
            "HA_ChaosMinionRanged",
            "HA_ChaosMinionSiege",
            "HA_ChaosMinionSuper",
            "HA_FB_HealthRelic",
            "HA_OrderMinionMelee",
            "HA_OrderMinionRanged",
            "HA_OrderMinionSiege",
            "HA_OrderMinionSuper",
            "Hecarim",
            "Heimerdinger",
            "HeimerTBlue",
            "HeimerTYellow",
            "Irelia",
            "Janna",
            "JarvanIV",
            "JarvanIVStandard",
            "JarvanIVWall",
            "Jax",
            "Jayce",
            "Jinx",
            "JinxMine",
            "Kalista",
            "KalistaAltar",
            "KalistaSpawn",
            "Karma",
            "Karthus",
            "Kassadin",
            "Katarina",
            "Kayle",
            "Kennen",
            "Khazix",
            "KingPoro",
            "KINGPORO_HiddenUnit",
            "KINGPORO_PoroFollower",
            "KogMaw",
            "KogMawDead",
            "Leblanc",
            "LeeSin",
            "Leona",
            "LesserWraith",
            "Lissandra",
            "Lizard",
            "LizardElder",
            "Lucian",
            "Lulu",
            "LuluCupcake",
            "LuluDragon",
            "LuluFaerie",
            "LuluKitty",
            "LuluLadybug",
            "LuluPig",
            "LuluSnowman",
            "LuluSquill",
            "Lux",
            "Malphite",
            "Malzahar",
            "MalzaharVoidling",
            "Maokai",
            "MaokaiSproutling",
            "MasterYi",
            "MissFortune",
            "MonkeyKing",
            "MonkeyKingClone",
            "MonkeyKingFlying",
            "Mordekaiser",
            "Morgana",
            "Nami",
            "Nasus",
            "NasusUlt",
            "Nautilus",
            "Nidalee",
            "Nidalee_Cougar",
            "Nidalee_Spear",
            "Nocturne",
            "Nunu",
            "Odin_Blue_Minion_Caster",
            "Odin_Drill",
            "Odin_Lifts_Buckets",
            "Odin_Lifts_Crystal",
            "Odin_Minecart",
            "Odin_Red_Minion_Caster",
            "Odin_skeleton",
            "Odin_SoG_Chaos",
            "Odin_SOG_Chaos_Crystal",
            "Odin_SoG_Order",
            "Odin_SOG_Order_Crystal",
            "Odin_Windmill_Gears",
            "Odin_Windmill_Propellers",
            "OdinBlueSuperminion",
            "OdinCenterRelic",
            "OdinChaosTurretShrine",
            "OdinClaw",
            "OdinCrane",
            "OdinMinionGraveyardPortal",
            "OdinMinionSpawnPortal",
            "OdinNeutralGuardian",
            "OdinOpeningBarrier",
            "OdinOrderTurretShrine",
            "OdinQuestBuff",
            "OdinQuestIndicator",
            "OdinRedSuperminion",
            "OdinRockSaw",
            "OdinShieldRelic",
            "OdinSpeedShrine",
            "OdinTestCubeRender",
            "Olaf",
            "OlafAxe",
            "OrderInhibitor",
            "OrderInhibitor_D",
            "OrderNexus",
            "OrderTurretAngel",
            "OrderTurretDragon",
            "OrderTurretNormal",
            "OrderTurretNormal2",
            "OrderTurretShrine",
            "OrderTurretTutorial",
            "Orianna",
            "OriannaBall",
            "OriannaNoBall",
            "Pantheon",
            "Poppy",
            "Quinn",
            "QuinnValor",
            "Rammus",
            "RammusDBC",
            "RammusPB",
            "Red_Minion_Basic",
            "Red_Minion_MechCannon",
            "Red_Minion_MechMelee",
            "Red_Minion_Wizard",
            "redDragon",
            "RekSai",
            "RekSaiTunnel",
            "Renekton",
            "Rengar",
            "Riven",
            "Rumble",
            "Ryze",
            "Sejuani",
            "Shaco",
            "ShacoBox",
            "Shen",
            "Shop",
            "ShopKeeper",
            "ShopMale",
            "Shyvana",
            "ShyvanaDragon",
            "SightWard",
            "Singed",
            "Sion",
            "Sivir",
            "Skarner",
            "SmallGolem",
            "Sona",
            "SonaDJGenre01",
            "SonaDJGenre02",
            "SonaDJGenre03",
            "Soraka",
            "SpellBook1",
            "Sru_Antlermouse",
            "SRU_Baron",
            "SRU_BaronSpawn",
            "SRU_Bird",
            "SRU_Blue",
            "SRU_BlueMini",
            "SRU_BlueMini2",
            "Sru_Butterfly",
            "SRU_ChaosMinionMelee",
            "SRU_ChaosMinionRanged",
            "SRU_ChaosMinionSiege",
            "SRU_ChaosMinionSuper",
            "Sru_Crab",
            "Sru_CrabWard",
            "SRU_Dragon",
            "sru_dragon_prop",
            "Sru_Dragonfly",
            "Sru_Duck",
            "Sru_Duckie",
            "SRU_Es_Banner",
            "Sru_Es_Bannerplatform_Chaos",
            "Sru_Es_Bannerplatform_Order",
            "Sru_Es_Bannerwall_Chaos",
            "Sru_Es_Bannerwall_Order",
            "SRU_Gromp",
            "Sru_Gromp_Prop",
            "SRU_Krug",
            "SRU_KrugMini",
            "Sru_Lizard",
            "SRU_Murkwolf",
            "SRU_MurkwolfMini",
            "SRU_OrderMinionMelee",
            "SRU_OrderMinionRanged",
            "SRU_OrderMinionSiege",
            "SRU_OrderMinionSuper",
            "Sru_Porowl",
            "SRU_Razorbeak",
            "SRU_RazorbeakMini",
            "SRU_Red",
            "SRU_RedMini",
            "SRU_RiverDummy",
            "Sru_Snail",
            "SRU_SnailSpawner",
            "SRU_Spiritwolf",
            "sru_storekeepernorth",
            "sru_storekeepersouth",
            "SRU_WallVisionBearer",
            "SRUAP_Building",
            "SRUAP_ChaosInhibitor",
            "sruap_chaosinhibitor_rubble",
            "SRUAP_ChaosNexus",
            "Sruap_Chaosnexus_Rubble",
            "Sruap_Esports_Banner",
            "sruap_flag",
            "sruap_mage_vines",
            "SRUAP_MageCrystal",
            "SRUAP_OrderInhibitor",
            "sruap_orderinhibitor_rubble",
            "SRUAP_OrderNexus",
            "Sruap_Ordernexus_Rubble",
            "Sruap_Pali_Statue_Banner",
            "SRUAP_Turret_Chaos1",
            "sruap_turret_chaos1_rubble",
            "SRUAP_Turret_Chaos2",
            "SRUAP_Turret_Chaos3",
            "SRUAP_Turret_Chaos3_Test",
            "SRUAP_Turret_Chaos4",
            "SRUAP_Turret_Chaos5",
            "SRUAP_Turret_Order1",
            "SRUAP_Turret_Order1_Rubble",
            "SRUAP_Turret_Order2",
            "SRUAP_Turret_Order3",
            "SRUAP_Turret_Order3_Test",
            "SRUAP_Turret_Order4",
            "SRUAP_Turret_Order5",
            "Summoner_Rider_Chaos",
            "Summoner_Rider_Order",
            "SummonerBeacon",
            "Swain",
            "SwainBeam",
            "SwainNoBird",
            "SwainRaven",
            "Syndra",
            "SyndraOrbs",
            "SyndraSphere",
            "Talon",
            "Taric",
            "Teemo",
            "TeemoMushroom",
            "TestCube",
            "TestCubeRender",
            "TestCubeRender10Vision",
            "TestCubeRenderwCollision",
            "Thresh",
            "ThreshLantern",
            "Tristana",
            "Trundle",
            "TrundleWall",
            "Tryndamere",
            "TT_Brazier",
            "TT_Buffplat_L",
            "TT_Buffplat_R",
            "TT_Chains_Bot_Lane",
            "TT_Chains_Order_Base",
            "TT_Chains_Order_Periph",
            "TT_Chains_Xaos_Base",
            "TT_ChaosInhibitor",
            "TT_ChaosInhibitor_D",
            "TT_ChaosTurret1",
            "TT_ChaosTurret2",
            "TT_ChaosTurret3",
            "TT_ChaosTurret4",
            "TT_ChaosTurret5",
            "TT_DummyPusher",
            "TT_Flytrap_A",
            "TT_Nexus_Gears",
            "TT_NGolem",
            "TT_NGolem2",
            "TT_NWolf",
            "TT_NWolf2",
            "TT_NWraith",
            "TT_NWraith2",
            "TT_OrderInhibitor",
            "TT_OrderInhibitor_D",
            "TT_OrderTurret1",
            "TT_OrderTurret2",
            "TT_OrderTurret3",
            "TT_OrderTurret4",
            "TT_OrderTurret5",
            "TT_Relic",
            "TT_Shopkeeper",
            "TT_Shroom_A",
            "TT_SpeedShrine",
            "TT_Speedshrine_Gears",
            "TT_Spiderboss",
            "TT_SpiderLayer_Web",
            "TT_Tree_A",
            "TT_Tree1",
            "Tutorial_Blue_Minion_Basic",
            "Tutorial_Blue_Minion_Wizard",
            "Tutorial_Red_Minion_Basic",
            "Tutorial_Red_Minion_Wizard",
            "TwistedFate",
            "Twitch",
            "Udyr",
            "UdyrPhoenix",
            "UdyrPhoenixUlt",
            "UdyrTiger",
            "UdyrTigerUlt",
            "UdyrTurtle",
            "UdyrTurtleUlt",
            "UdyrUlt",
            "Urf",
            "Urgot",
            "Varus",
            "Vayne",
            "Veigar",
            "Velkoz",
            "Vi",
            "Viktor",
            "ViktorSingularity",
            "VisionWard",
            "Vladimir",
            "VoidGate",
            "VoidSpawn",
            "VoidSpawnTracer",
            "Volibear",
            "Warwick",
            "wolf",
            "Worm",
            "Wraith",
            "Xerath",
            "XerathArcaneBarrageLauncher",
            "XinZhao",
            "Yasuo",
            "YellowTrinket",
            "YellowTrinketUpgrade",
            "Yonkey",
            "Yorick",
            "YorickDecayedGhoul",
            "YorickRavenousGhoul",
            "YorickSpectralGhoul",
            "YoungLizard",
            "Zac",
            "ZacRebirthBloblet",
            "Zed",
            "ZedShadow",
            "Ziggs",
            "Zilean",
            "Zyra",
            "ZyraGraspingPlant",
            "ZyraPassive",
            "ZyraSeed",
            "ZyraThornPlant"
        };

        #endregion

        static ModelManager()
        {
            try
            {
                using (var client = new WebClient())
                {
                    var versionJson = client.DownloadString(DataDragonBase + "realms/na.json");
                    GameVersion =
                        (string)
                            ((Dictionary<string, object>)
                                new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(versionJson)["n"])[
                                    "champion"];
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static bool IsValidModel(this string model)
        {
            return !string.IsNullOrWhiteSpace(model) && ModelList.Contains(model);
        }

        public static string GetValidModel(this string model)
        {
            var index = ModelList.FindIndex(x => x.Equals(model, StringComparison.OrdinalIgnoreCase));
            return index == -1 ? "" : ModelList[index];
        }

        public static ArrayList GetSkins(string model)
        {
            try
            {
                using (var client = new WebClient())
                {
                    var champJson =
                        client.DownloadString(
                            DataDragonBase + "cdn/" + GameVersion + "/data/en_US/champion/" + model + ".json");
                    return
                        (ArrayList)
                            ((Dictionary<string, object>)
                                ((Dictionary<string, object>)
                                    new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(champJson)["data"
                                        ])[model])["skins"];
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return new ArrayList();
        }
    }
}