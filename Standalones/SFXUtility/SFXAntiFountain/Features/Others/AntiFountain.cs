#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 AntiFountain.cs is part of SFXAntiFountain.

 SFXAntiFountain is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXAntiFountain is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXAntiFountain. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXLibrary;
using SFXLibrary.Logger;
using SFXAntiFountain.Classes;

#endregion

namespace SFXAntiFountain.Features.Others
{
    internal class AntiFountain : Child<App>
    {
        private const float FountainRange = 1450f;
        private Obj_AI_Turret _fountain;

        public AntiFountain(App parent) : base(parent)
        {
            OnLoad();
        }

        public override string Name
        {
            get { return Global.Lang.Get("F_AntiFountain"); }
        }

        protected override void OnEnable()
        {
            Obj_AI_Base.OnNewPath += OnObjAiBaseNewPath;

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            Obj_AI_Base.OnNewPath -= OnObjAiBaseNewPath;

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
                _fountain =
                    GameObjects.EnemyTurrets.FirstOrDefault(
                        t => t.IsEnemy && t.CharData.Name.ToLower().Contains("shrine"));
                if (_fountain == null)
                {
                    OnUnload(null, new UnloadEventArgs(true));
                }
                base.OnInitialize();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnObjAiBaseNewPath(Obj_AI_Base sender, GameObjectNewPathEventArgs args)
        {
            if (!sender.IsMe)
            {
                return;
            }
            if (args.Path.Any())
            {
                var last = args.Path.Last();
                if (last.Distance(_fountain.Position) < FountainRange)
                {
                    ObjectManager.Player.IssueOrder(
                        GameObjectOrder.MoveTo, _fountain.Position.Extend(last, FountainRange));
                }
            }
        }
    }
}