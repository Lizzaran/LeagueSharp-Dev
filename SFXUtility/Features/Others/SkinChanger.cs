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

namespace SFXUtility.Features.Others
{
    #region

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Net;
    using System.Web.Script.Serialization;
    using Classes;
    using LeagueSharp;
    using LeagueSharp.Common;
    using SFXLibrary;
    using SFXLibrary.Logger;

    #endregion

    internal class SkinChanger : Base
    {
        private readonly List<HeroSkin> _heroSkins = new List<HeroSkin>();
        private readonly List<Skins> _skins = new List<Skins>();
        private Others _parent;

        public override bool Enabled
        {
            get { return !Unloaded && _parent != null && _parent.Enabled && Menu != null && Menu.Item(Name + "Enabled").GetValue<bool>(); }
        }

        public override string Name
        {
            get { return Language.Get("F_SkinChanger"); }
        }

        protected override void OnEnable()
        {
            GameObject.OnFloatPropertyChange += OnGameObjectFloatPropertyChange;

            foreach (var hero in _heroSkins)
            {
                hero.SetSkin();
            }

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            GameObject.OnFloatPropertyChange -= OnGameObjectFloatPropertyChange;
            base.OnDisable();
        }

        protected override void OnGameLoad(EventArgs args)
        {
            try
            {
                if (Global.IoC.IsRegistered<Others>())
                {
                    _parent = Global.IoC.Resolve<Others>();
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

                foreach (var hero in HeroManager.AllHeroes)
                {
                    _heroSkins.Add(new HeroSkin(hero));
                }

                Menu = new Menu(Name, Name);

                using (var bw = new BackgroundWorker())
                {
                    bw.DoWork += delegate
                    {
                        try
                        {
                            using (var client = new WebClient())
                            {
                                var versionJson = client.DownloadString("http://ddragon.leagueoflegends.com/realms/na.json");
                                var gameVersion =
                                    (String)
                                        ((Dictionary<String, Object>)
                                            new JavaScriptSerializer().Deserialize<Dictionary<String, Object>>(versionJson)["n"])["champion"];

                                foreach (var hero in HeroManager.AllHeroes)
                                {
                                    try
                                    {
                                        var champJson =
                                            client.DownloadString("http://ddragon.leagueoflegends.com/cdn/" + gameVersion + "/data/en_US/champion/" +
                                                                  hero.ChampionName + ".json");
                                        var skins =
                                            (ArrayList)
                                                ((Dictionary<String, Object>)
                                                    ((Dictionary<String, Object>)
                                                        new JavaScriptSerializer().Deserialize<Dictionary<String, Object>>(champJson)["data"])[
                                                            hero.ChampionName])["skins"];

                                        var tmpSkins = new Skins(hero);
                                        foreach (Dictionary<string, object> skin in skins)
                                        {
                                            try
                                            {
                                                var skinName = skin["name"].ToString();
                                                if (skinName.Equals("default"))
                                                {
                                                    skinName = hero.ChampionName;
                                                }
                                                if (!string.IsNullOrWhiteSpace(skinName))
                                                {
                                                    tmpSkins.List[skinName] = (int) skin["num"];
                                                }
                                            }
                                            catch
                                            {
                                            }
                                        }
                                        if (tmpSkins.List.Count > 0)
                                        {
                                            _skins.Add(tmpSkins);
                                        }
                                    }
                                    catch
                                    {
                                    }
                                }
                            }
                        }
                        catch
                        {
                        }
                    };
                    bw.RunWorkerCompleted += delegate
                    {
                        foreach (var hero in HeroManager.AllHeroes.OrderBy(h => h.ChampionName))
                        {
                            var champMenu = new Menu(hero.ChampionName, Name + hero.ChampionName);
                            var localHero = hero;
                            foreach (var skin in _skins.Where(h => h.Hero.NetworkId.Equals(localHero.NetworkId)).SelectMany(h => h.List))
                            {
                                var heroSkin = _heroSkins.FirstOrDefault(h => h.Hero.NetworkId.Equals(localHero.NetworkId));
                                if (heroSkin == null || string.IsNullOrWhiteSpace(skin.Key))
                                    continue;

                                var localSkin = skin;
                                champMenu.AddItem(new MenuItem(champMenu.Name + skin.Key, skin.Key).SetValue(false)).ValueChanged +=
                                    (s, e) =>
                                    {
                                        if (e.GetNewValue<bool>())
                                        {
                                            champMenu.Items.ForEach(p =>
                                            {
                                                if (p.GetValue<bool>() && p.Name != skin.Key)
                                                {
                                                    p.SetValue(false);
                                                }
                                            });
                                            heroSkin.CurrentSkin = localSkin.Value;
                                            if (Enabled && !localHero.IsDead)
                                            {
                                                heroSkin.SetSkin();
                                            }
                                        }
                                    };
                                if (champMenu.Item(champMenu.Name + skin.Key).GetValue<bool>())
                                {
                                    heroSkin.CurrentSkin = localSkin.Value;
                                    heroSkin.SetSkin();
                                }
                            }
                            Menu.AddSubMenu(champMenu);
                        }
                    };
                    bw.RunWorkerAsync();
                }

                Menu.AddItem(new MenuItem(Name + "Enabled", Language.Get("G_Enabled")).SetValue(false));

                _parent.Menu.AddSubMenu(Menu);

                HandleEvents(_parent);
                RaiseOnInitialized();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnGameObjectFloatPropertyChange(GameObject sender, GameObjectFloatPropertyChangeEventArgs args)
        {
            try
            {
                if (args.Property != "mHP")
                    return;
                var hero = sender as Obj_AI_Hero;
                if (hero != null)
                {
                    var heroSkin = _heroSkins.FirstOrDefault(h => h.Hero.NetworkId == hero.NetworkId);
                    if (heroSkin != null && args.OldValue.Equals(args.NewValue) && args.NewValue.Equals(hero.MaxHealth) && !hero.IsDead)
                    {
                        heroSkin.SetSkin();
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }

    internal class Skins
    {
        public Dictionary<string, int> List = new Dictionary<string, int>();

        public Skins(Obj_AI_Hero hero)
        {
            Hero = hero;
        }

        public Obj_AI_Hero Hero { get; set; }
    }

    internal class HeroSkin
    {
        private readonly string _defaultModel;

        public HeroSkin(Obj_AI_Hero hero)
        {
            Hero = hero;
            _defaultModel = hero.BaseSkinName;
            CurrentSkin = hero.BaseSkinId;
        }

        public Obj_AI_Hero Hero { get; set; }
        public int CurrentSkin { get; set; }

        public void SetSkin()
        {
            try
            {
                Hero.SetSkin(_defaultModel, CurrentSkin);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }
}