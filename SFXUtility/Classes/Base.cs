#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Base.cs is part of SFXUtility.

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

namespace SFXUtility.Classes
{
    #region

    using System;
    using LeagueSharp.Common;
    using SFXLibrary.Extensions.NET;
    using SFXLibrary.IoCContainer;
    using SFXLibrary.Logger;

    #endregion

    internal abstract class Base
    {
        /// <exception cref="InvalidOperationException">SFXUtility</exception>
        protected Base(IContainer container)
        {
            if (!container.IsRegistered<SFXUtility>())
                throw new InvalidOperationException("SFXUtility");
            if (!container.IsRegistered<ILogger>())
                throw new InvalidOperationException("ILogger");

            IoC = container;

            var sfx = IoC.Resolve<SFXUtility>();

            Logger = IoC.Resolve<ILogger>();
            BaseMenu = sfx.Menu;
            BaseName = sfx.Name;
        }

        public abstract bool Enabled { get; }
        public abstract string Name { get; }
        public bool Initialized { get; protected set; }
        public Menu Menu { get; set; }
        protected Menu BaseMenu { get; private set; }
        protected string BaseName { get; private set; }
        protected IContainer IoC { get; private set; }
        protected ILogger Logger { get; set; }
        public event EventHandler OnInitialized;
        public event EventHandler OnEnabled;
        public event EventHandler OnDisabled;

        protected virtual void OnEnable()
        {
            try
            {
                OnEnabled.RaiseEvent(null, null);
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
            }
        }

        protected virtual void OnDisable()
        {
            try
            {
                OnDisabled.RaiseEvent(null, null);
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
            }
        }

        protected virtual void RaiseOnInitialized()
        {
            try
            {
                Initialized = true;
                OnInitialized.RaiseEvent(this, null);
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
            }
        }

        protected virtual void HandleEvents(Base parent)
        {
            try
            {
                parent.Menu.Item(parent.Name + "Enabled").ValueChanged += delegate(object sender, OnValueChangeEventArgs args)
                {
                    if (args.GetNewValue<bool>())
                    {
                        if (Menu != null && Menu.Item(Name + "Enabled").GetValue<bool>())
                        {
                            OnEnable();
                        }
                    }
                    else
                    {
                        OnDisable();
                    }
                };
                Menu.Item(Name + "Enabled").ValueChanged += delegate(object sender, OnValueChangeEventArgs args)
                {
                    if (args.GetNewValue<bool>())
                    {
                        if (parent.Menu != null && parent.Menu.Item(parent.Name + "Enabled").GetValue<bool>())
                        {
                            OnEnable();
                        }
                    }
                    else
                    {
                        OnDisable();
                    }
                };

                if (Enabled)
                {
                    OnEnable();
                }
            }
            catch (Exception ex)
            {
                Logger.AddItem(new LogItem(ex) {Object = this});
            }
        }
    }
}