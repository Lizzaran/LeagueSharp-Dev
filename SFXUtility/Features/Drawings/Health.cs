#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Health.cs is part of SFXUtility.

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

namespace SFXUtility.Features.Drawings
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Classes;
    using LeagueSharp;
    using LeagueSharp.Common;
    using SFXLibrary;
    using SFXLibrary.Logger;
    using SharpDX;

    #endregion

    internal class Health : Base
    {
        private readonly List<InhibitorObject> _inhibs = new List<InhibitorObject>();
        private readonly List<TurretObject> _turrets = new List<TurretObject>();
        private Drawings _parent;

        public override bool Enabled
        {
            get { return _parent != null && _parent.Enabled && Menu != null && Menu.Item(Name + "Enabled").GetValue<bool>(); }
        }

        public override string Name
        {
            get { return Language.Get("F_Health"); }
        }

        protected override void OnEnable()
        {
            foreach (var turret in _turrets)
            {
                turret.Active = Menu.Item(Name + "Turret").GetValue<bool>();
            }
            foreach (var inhib in _inhibs)
            {
                inhib.Active = Menu.Item(Name + "Inhibitor").GetValue<bool>();
            }
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            foreach (var turret in _turrets)
            {
                turret.Active = false;
            }
            foreach (var inhib in _inhibs)
            {
                inhib.Active = false;
            }
            base.OnEnable();
        }

        private void OnParentInitialized(object sender, EventArgs eventArgs)
        {
            try
            {
                if (_parent.Menu == null)
                    return;

                Menu = new Menu(Name, Name);

                var drawingMenu = new Menu(Language.Get("G_Drawing"), Name + "Drawing");
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + "Percentage", Language.Get("G_Percent")).SetValue(false));
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + "FontSize", Language.Get("G_FontSize")).SetValue(new Slider(13, 3, 30)));

                Menu.AddSubMenu(drawingMenu);

                Menu.AddItem(new MenuItem(Name + "Turret", Language.Get("G_Turret")).SetValue(false));
                Menu.AddItem(new MenuItem(Name + "Inhibitor", Language.Get("G_Inhibitor")).SetValue(false));

                Menu.AddItem(new MenuItem(Name + "Enabled", Language.Get("G_Enabled")).SetValue(false));

                Menu.Item(Name + "Enabled").ValueChanged += delegate(object o, OnValueChangeEventArgs args)
                {
                    foreach (var turret in _turrets)
                    {
                        turret.Active = _parent.Enabled && args.GetNewValue<bool>() && Menu.Item(Name + "Turret").GetValue<bool>();
                    }
                    foreach (var inhib in _inhibs)
                    {
                        inhib.Active = _parent.Enabled && args.GetNewValue<bool>() && Menu.Item(Name + "Inhibitor").GetValue<bool>();
                    }
                };

                Menu.Item(Name + "Turret").ValueChanged += delegate(object o, OnValueChangeEventArgs args)
                {
                    foreach (var turret in _turrets)
                    {
                        turret.Active = Enabled && args.GetNewValue<bool>();
                    }
                };

                Menu.Item(Name + "Inhibitor").ValueChanged += delegate(object o, OnValueChangeEventArgs args)
                {
                    foreach (var inhib in _inhibs)
                    {
                        inhib.Active = Enabled && args.GetNewValue<bool>();
                    }
                };

                Menu.Item(Name + "DrawingPercentage").ValueChanged += delegate(object o, OnValueChangeEventArgs args)
                {
                    foreach (var turret in _turrets)
                    {
                        turret.Percentage = args.GetNewValue<bool>();
                    }
                    foreach (var inhib in _inhibs)
                    {
                        inhib.Percentage = args.GetNewValue<bool>();
                    }
                };

                _parent.Menu.AddSubMenu(Menu);

                foreach (var turret in ObjectManager.Get<Obj_AI_Turret>().Where(t => t.IsValid && !t.IsDead && t.Health > 1f && t.Health < 9999f))
                {
                    _turrets.Add(new TurretObject(turret, Menu.Item(Name + "DrawingFontSize").GetValue<Slider>().Value)
                    {
                        Active = Enabled && Menu.Item(Name + "Turret").GetValue<bool>(),
                        Percentage = Menu.Item(Name + "DrawingPercentage").GetValue<bool>()
                    });
                }
                foreach (var inhib in ObjectManager.Get<Obj_BarracksDampener>().Where(i => i.IsValid))
                {
                    _inhibs.Add(new InhibitorObject(inhib, Menu.Item(Name + "DrawingFontSize").GetValue<Slider>().Value)
                    {
                        Active = Enabled && Menu.Item(Name + "Inhibitor").GetValue<bool>(),
                        Percentage = Menu.Item(Name + "DrawingPercentage").GetValue<bool>()
                    });
                }

                if (!_turrets.Any() || !_inhibs.Any())
                    return;

                HandleEvents(_parent);
                RaiseOnInitialized();
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
                if (Global.IoC.IsRegistered<Drawings>())
                {
                    _parent = Global.IoC.Resolve<Drawings>();
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

        private class TurretObject
        {
            private bool _active;
            private bool _added;
            private Render.Text _text;

            public TurretObject(Obj_AI_Turret turret, int fontSize)
            {
                _text = new Render.Text(Drawing.WorldToMinimap(turret.Position), string.Empty, fontSize, Color.White)
                {
                    OutLined = true,
                    Centered = true,
                    VisibleCondition = delegate
                    {
                        try
                        {
                            return Active;
                        }
                        catch (Exception ex)
                        {
                            Global.Logger.AddItem(new LogItem(ex));
                            return false;
                        }
                    },
                    TextUpdate = delegate
                    {
                        try
                        {
                            if (turret.IsDead)
                                Dispose();
                            var percent = Convert.ToInt32((turret.Health/turret.MaxHealth)*100);
                            return Percentage ? (percent == 0 ? 1 : percent).ToString() : ((int) turret.Health).ToString();
                        }
                        catch (Exception ex)
                        {
                            Global.Logger.AddItem(new LogItem(ex));
                            return string.Empty;
                        }
                    }
                };
            }

            public bool Percentage { private get; set; }

            public bool Active
            {
                private get { return _active; }
                set
                {
                    _active = value;
                    Update();
                }
            }

            private void Dispose()
            {
                if (_text != null)
                {
                    Active = false;
                    _text = null;
                }
            }

            private void Update()
            {
                if (_active && !_added)
                {
                    _text.Add(0);
                    _added = true;
                }
                else if (!_active && _added)
                {
                    _text.Remove();
                    _added = false;
                }
            }
        }

        private class InhibitorObject
        {
            private readonly Render.Text _text;
            private bool _active;
            private bool _added;

            public InhibitorObject(Obj_BarracksDampener inhib, int fontSize)
            {
                _text = new Render.Text(Drawing.WorldToMinimap(inhib.Position), string.Empty, fontSize, Color.White)
                {
                    OutLined = true,
                    Centered = true,
                    VisibleCondition = delegate
                    {
                        try
                        {
                            return Active && !inhib.IsDead && inhib.Health > 1f;
                        }
                        catch (Exception ex)
                        {
                            Global.Logger.AddItem(new LogItem(ex));
                            return false;
                        }
                    },
                    TextUpdate = delegate
                    {
                        try
                        {
                            var percent = Convert.ToInt32((inhib.Health/inhib.MaxHealth)*100);
                            return Percentage ? (percent == 0 ? 1 : percent).ToString() : ((int) inhib.Health).ToString();
                        }
                        catch (Exception ex)
                        {
                            Global.Logger.AddItem(new LogItem(ex));
                            return string.Empty;
                        }
                    }
                };
            }

            public bool Percentage { private get; set; }

            public bool Active
            {
                private get { return _active; }
                set
                {
                    _active = value;
                    Update();
                }
            }

            private void Update()
            {
                if (_active && !_added)
                {
                    _text.Add(0);
                    _added = true;
                }
                else if (!_active && _added)
                {
                    _text.Remove();
                    _added = false;
                }
            }
        }
    }
}