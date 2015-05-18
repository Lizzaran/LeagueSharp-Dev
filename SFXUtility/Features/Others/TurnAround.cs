#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 TurnAround.cs is part of SFXUtility.

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
    using System.Collections.Generic;
    using System.Linq;
    using Classes;
    using LeagueSharp;
    using LeagueSharp.Common;
    using SFXLibrary.Extensions.NET;
    using SFXLibrary.Logger;
    using SharpDX;

    #endregion

    internal class TurnAround : Base
    {
        private readonly List<SpellInfoStruct> _spellInfos = new List<SpellInfoStruct>
        {
            new SpellInfoStruct("Cassiopeia", "CassiopeiaPetrifyingGaze", 750f, false, true),
            new SpellInfoStruct("Tryndamere", "MockingShout", 850f, false, false)
        };

        private float _blockMovementTime;
        private Vector2 _lastWaypoint = Vector2.Zero;
        private Others _parent;

        public override bool Enabled
        {
            get { return !Unloaded && _parent != null && _parent.Enabled && Menu != null && Menu.Item(Name + "Enabled").GetValue<bool>(); }
        }

        public override string Name
        {
            get { return Global.Lang.Get("F_TurnAround"); }
        }

        protected override void OnEnable()
        {
            Obj_AI_Base.OnProcessSpellCast += OnObjAiBaseProcessSpellCast;
            Obj_AI_Base.OnIssueOrder += OnObjAiBaseIssueOrder;
            base.OnEnable();
        }

        private void OnObjAiBaseIssueOrder(Obj_AI_Base sender, GameObjectIssueOrderEventArgs args)
        {
            try
            {
                if (sender.IsMe && args.Order == GameObjectOrder.MoveTo)
                {
                    if (_blockMovementTime > Game.Time)
                    {
                        args.Process = false;
                    }
                    else if (_lastWaypoint != Vector2.Zero && _lastWaypoint.IsValid())
                    {
                        ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, _lastWaypoint.To3D());
                        _lastWaypoint = Vector2.Zero;
                        ;
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected override void OnDisable()
        {
            Obj_AI_Base.OnProcessSpellCast -= OnObjAiBaseProcessSpellCast;
            base.OnDisable();
        }

        private void OnObjAiBaseProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            try
            {
                if (sender == null || sender.Team == ObjectManager.Player.Team || ObjectManager.Player.IsDead || !ObjectManager.Player.IsTargetable)
                    return;
                var spellInfo = _spellInfos.FirstOrDefault(i => args.SData.Name.Contains(i.Name, StringComparison.OrdinalIgnoreCase));

                if (!spellInfo.Equals(default(SpellInfoStruct)))
                {
                    if ((spellInfo.Target && args.Target == ObjectManager.Player) ||
                        ObjectManager.Player.ServerPosition.Distance(sender.ServerPosition) <= spellInfo.Range)
                    {
                        _lastWaypoint = ObjectManager.Player.GetWaypoints().Last();
                        ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo,
                            sender.ServerPosition.Extend(ObjectManager.Player.ServerPosition,
                                ObjectManager.Player.ServerPosition.Distance(sender.ServerPosition) + (spellInfo.TurnOpposite ? 100 : -100)));
                        _blockMovementTime = Game.Time + args.SData.SpellCastTime;
                    }
                }
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

                Menu.AddItem(new MenuItem(Name + "Enabled", Global.Lang.Get("G_Enabled")).SetValue(false));

                _parent.Menu.AddSubMenu(Menu);

                if (!HeroManager.Enemies.Any(h => _spellInfos.Any(i => i.Owner == h.ChampionName)))
                    return;

                HandleEvents(_parent);
                RaiseOnInitialized();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private struct SpellInfoStruct
        {
            public readonly string Name;
            public readonly string Owner;
            public readonly float Range;
            public readonly bool Target;
            public readonly bool TurnOpposite;

            public SpellInfoStruct(string owner, string name, float range, bool target, bool turnOpposite)
            {
                Owner = owner;
                Name = name;
                Range = range;
                Target = target;
                TurnOpposite = turnOpposite;
            }
        }
    }
}