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
    using LeagueSharp.CommonEx.Core.Events;
    using LeagueSharp.CommonEx.Core.Extensions.SharpDX;
    using SFXLibrary.Extensions.NET;
    using SFXLibrary.IoCContainer;
    using SFXLibrary.Logger;
    using SharpDX;
    using ObjectHandler = LeagueSharp.CommonEx.Core.ObjectHandler;

    #endregion

    internal class TurnAround : Base
    {
        private readonly List<SpellInfoStruct> _spellInfos = new List<SpellInfoStruct>
        {
            new SpellInfoStruct("Cassiopeia", "CassiopeiaPetrifyingGaze", 750f, false, true),
            new SpellInfoStruct("Shaco", "TwoShivPoison", 625f, true, false),
            new SpellInfoStruct("Tryndamere", "MockingShout", 850f, false, false)
        };

        private float _blockMovementTime;
        private Others _parent;

        public TurnAround(IContainer container)
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
            get { return "Turn Around"; }
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
                }
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
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
                if (sender == null || sender.Team == ObjectManager.Player.Team || ObjectManager.Player.IsDead ||
                    !ObjectManager.Player.IsTargetable)
                    return;

                var spellInfo =
                    _spellInfos.FirstOrDefault(i => args.SData.Name.Contains(i.Name, StringComparison.OrdinalIgnoreCase));

                if (!Equals(spellInfo, default(SpellInfoStruct)))
                {
                    if (spellInfo.Target && args.Target == ObjectManager.Player ||
                        ObjectManager.Player.ServerPosition.Distance(sender.ServerPosition) <= spellInfo.Range)
                    {
                        _blockMovementTime = Game.Time + args.SData.SpellCastTime;
                        ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, new Vector2(
                            ObjectManager.Player.ServerPosition.X +
                            (sender.ServerPosition.X - ObjectManager.Player.ServerPosition.X)*
                            (spellInfo.TurnOpposite ? 100 : -100)/
                            ObjectManager.Player.ServerPosition.Distance(sender.ServerPosition),
                            ObjectManager.Player.ServerPosition.Y +
                            (sender.ServerPosition.Y - ObjectManager.Player.ServerPosition.Y)*
                            (spellInfo.TurnOpposite ? 100 : -100)/
                            ObjectManager.Player.ServerPosition.Distance(sender.ServerPosition)).ToVector3());
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
            }
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

                if (!ObjectHandler.EnemyHeroes.Any(h => _spellInfos.Any(i => i.Owner == h.ChampionName)))
                    return;

                HandleEvents(_parent);
                RaiseOnInitialized();
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
            }
        }

        private struct SpellInfoStruct
        {
            public SpellInfoStruct(string owner, string name, float range, bool target, bool turnOpposite)
                : this()
            {
                Owner = owner;
                Name = name;
                Range = range;
                Target = target;
                TurnOpposite = turnOpposite;
            }

            public string Owner { get; private set; }
            public string Name { get; private set; }
            public float Range { get; private set; }
            public bool Target { get; private set; }
            public bool TurnOpposite { get; private set; }
        }
    }
}