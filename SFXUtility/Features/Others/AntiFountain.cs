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
    using LeagueSharp.CommonEx.Core.Events;
    using LeagueSharp.CommonEx.Core.Extensions.SharpDX;
    using SFXLibrary.Extensions.NET;
    using SFXLibrary.Extensions.SharpDX;
    using SFXLibrary.IoCContainer;
    using SFXLibrary.Logger;
    using Gapcloser = LeagueSharp.CommonEx.Core.Events.Gapcloser;
    using ObjectHandler = LeagueSharp.CommonEx.Core.ObjectHandler;

    #endregion

    internal class AntiFountain : Base
    {
        private const float FountainRange = 1450f;
        private Obj_AI_Turret _fountainTurret;
        private Others _parent;

        public AntiFountain(IContainer container)
            : base(container)
        {
            Load.OnLoad += OnLoad;
        }

        public override bool Enabled
        {
            get
            {
                return _parent != null && _parent.Enabled && Menu != null && Menu.Item(Name + "Enabled").GetValue<bool>();
            }
        }

        public override string Name
        {
            get { return "Anti Fountain"; }
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

        private void OnLoad(EventArgs args)
        {
            try
            {
                if (IoC.IsRegistered<Others>())
                {
                    _parent = IoC.Resolve<Others>();
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

                Menu = new Menu(Name, BaseName + Name);

                Menu.AddItem(new MenuItem(Name + "Enabled", "Enabled").SetValue(false));

                _parent.Menu.AddSubMenu(Menu);

                _fountainTurret =
                    ObjectHandler.GetFast<Obj_AI_Turret>()
                        .FirstOrDefault(
                            s =>
                                s != null && s.IsValid && s.Team != ObjectManager.Player.Team &&
                                s.Name.Contains("TurretShrine", StringComparison.OrdinalIgnoreCase));

                if (_fountainTurret == null || _fountainTurret.Equals(default(Obj_AI_Turret)))
                    return;

                HandleEvents(_parent);
                RaiseOnInitialized();
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
            }
        }

        private void OnSpellbookCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (!sender.Owner.IsMe)
                return;
            if (Gapcloser.Spells.Any(a => a.SpellName == sender.GetSpell(args.Slot).Name))
            {
                var intersections = args.StartPosition.ToVector2()
                    .FindLineCircleIntersections(args.EndPosition.ToVector2(),
                        _fountainTurret.ServerPosition.ToVector2(),
                        FountainRange/2);
                if (intersections.Count > 0)
                {
                    args.Process = false;
                }
            }
        }

        private void OnObjAiBaseNewPath(Obj_AI_Base sender, GameObjectNewPathEventArgs args)
        {
            try
            {
                if (!sender.IsMe)
                    return;

                for (int i = 0, l = args.Path.Length - 1; i < l; i++)
                {
                    var intersections = args.Path[i].ToVector2()
                        .FindLineCircleIntersections(args.Path[i + 1].ToVector2(),
                            _fountainTurret.ServerPosition.ToVector2(),
                            FountainRange/2);
                    if (intersections.Count > 0)
                    {
                        ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo,
                            intersections.ClosestIntersection(args.Path[i].ToVector2()).ToVector3());
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
            }
        }
    }
}