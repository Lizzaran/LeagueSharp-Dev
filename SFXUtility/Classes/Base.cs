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

#region

using System;
using LeagueSharp.Common;
using SFXLibrary.Extensions.NET;
using SFXLibrary.Logger;

#endregion

namespace SFXUtility.Classes
{
    internal abstract class Base
    {
        protected Base(SFXUtility sfx)
        {
            BaseMenu = sfx.Menu;
            sfx.OnUnload += OnUnload;
        }

        public abstract bool Enabled { get; }
        public abstract string Name { get; }
        public bool Initialized { get; protected set; }
        public bool Unloaded { get; protected set; }
        public Menu Menu { get; set; }
        protected Menu BaseMenu { get; private set; }
        public event EventHandler OnInitialized;
        public event EventHandler OnEnabled;
        public event EventHandler OnDisabled;

        protected virtual void OnEnable()
        {
            try
            {
                if (Unloaded)
                {
                    return;
                }
                if (!Initialized)
                {
                    OnInitialize();
                }
                OnEnabled.RaiseEvent(null, null);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected virtual void OnInitialize()
        {
            try
            {
                if (Unloaded)
                {
                    return;
                }
                if (!Initialized)
                {
                    RaiseOnInitialized();
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected virtual void RaiseOnInitialized()
        {
            try
            {
                if (!Initialized && !Unloaded)
                {
                    Initialized = true;
                    OnInitialized.RaiseEvent(this, null);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected virtual void OnDisable()
        {
            try
            {
                if (Initialized && Enabled && !Unloaded)
                {
                    OnDisabled.RaiseEvent(null, null);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected virtual void OnUnload(object sender, UnloadEventArgs args)
        {
            if (Unloaded)
            {
                return;
            }
            OnDisable();
            if (args != null && args.Final)
            {
                Unloaded = true;
            }
        }
    }
}