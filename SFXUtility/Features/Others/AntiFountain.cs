#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 AntiFountain.cs is part of SFXUtility.

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
    using System.Linq;
    using Classes;
    using LeagueSharp;
    using LeagueSharp.Common;
    using SFXLibrary;
    using SFXLibrary.Extensions.NET;
    using SFXLibrary.Extensions.SharpDX;
    using SFXLibrary.Logger;

    #endregion

    internal class AntiFountain : Base
    {
        private const float FountainRange = 1450f;
        private Obj_AI_Turret _fountainTurret;
        private Others _parent;

        public override bool Enabled
        {
            get { return !Unloaded && _parent != null && _parent.Enabled && Menu != null && Menu.Item(Name + "Enabled").GetValue<bool>(); }
        }

        public override string Name
        {
            get { return Language.Get("F_AntiFountain"); }
        }

        protected override void OnEnable()
        {
            Obj_AI_Base.OnNewPath += OnObjAiBaseNewPath;
            Spellbook.OnCastSpell += OnSpellbookCastSpell;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            Obj_AI_Base.OnNewPath -= OnObjAiBaseNewPath;
            Spellbook.OnCastSpell -= OnSpellbookCastSpell;
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

                Menu = new Menu(Name, Name);

                Menu.AddItem(new MenuItem(Name + "Enabled", Language.Get("G_Enabled")).SetValue(false));

                _parent.Menu.AddSubMenu(Menu);

                _fountainTurret =
                    ObjectManager.Get<Obj_AI_Turret>()
                        .FirstOrDefault(
                            s => s != null && s.IsValid && s.IsEnemy && s.Name.Contains("TurretShrine", StringComparison.OrdinalIgnoreCase));

                if (_fountainTurret == null)
                    return;

                HandleEvents(_parent);
                RaiseOnInitialized();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnSpellbookCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            try
            {
                if (!sender.Owner.IsMe || _fountainTurret.Distance(ObjectManager.Player.ServerPosition) > 2000f + FountainRange)
                    return;

                if (AntiGapcloser.Spells.Any(a => a.SpellName == sender.GetSpell(args.Slot).Name))
                {
                    var intersection = args.StartPosition.To2D()
                        .Intersects(args.EndPosition.To2D(), _fountainTurret.ServerPosition.To2D(), FountainRange/2);
                    if (intersection.Intersects)
                    {
                        args.Process = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnObjAiBaseNewPath(Obj_AI_Base sender, GameObjectNewPathEventArgs args)
        {
            try
            {
                if (!sender.IsMe || _fountainTurret.Distance(ObjectManager.Player.ServerPosition) > 3000f + FountainRange)
                    return;

                for (int i = 0, l = args.Path.Length - 1; i < l; i++)
                {
                    var intersection = args.Path[i].To2D().Intersects(args.Path[i + 1].To2D(), _fountainTurret.Position.To2D(), FountainRange);
                    if (intersection.Intersects)
                    {
                        ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, intersection.Point.To3D());
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }
    }
}