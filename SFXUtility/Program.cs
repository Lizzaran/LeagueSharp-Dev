#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Program.cs is part of SFXUtility.

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

namespace SFXUtility
{
    #region

    using System;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using Classes;
    using SFXLibrary;
    using SFXLibrary.IoCContainer;
    using SFXLibrary.Logger;

    #endregion

    /*
     * TODO: Thickness from circles option
     * TODO: Time Format mm:ss | ss option
     * TODO: Logger: L# Version, directory etc.
     * TODO: Simple usage tracker.. which features are enabled/disabled?!
     */

    internal class Program
    {
        // ReSharper disable once UnusedParameter.Local
        private static void Main(string[] args)
        {
            const BindingFlags bFlags = BindingFlags.CreateInstance | BindingFlags.Public | BindingFlags.Instance |
                                        BindingFlags.OptionalParamBinding;

            var container = new Container();

            AppDomain.CurrentDomain.UnhandledException +=
                delegate(object sender, UnhandledExceptionEventArgs eventArgs)
                {
                    if (container.IsRegistered<ILogger>())
                    {
                        var ex = sender as Exception ??
                                 new NotSupportedException(
                                     "Unhandled exception doesn't derive from System.Exception: " +
                                     sender);
                        container.Resolve<ILogger>().AddItem(new LogItem(ex));
                    }
                };

            container.Register(typeof (ILogger),
                () =>
                    Activator.CreateInstance(typeof (ExceptionLogger), bFlags, null,
                        new object[] {AppDomain.CurrentDomain.BaseDirectory}, CultureInfo.CurrentCulture), true);

            container.Register<Mediator, Mediator>(true);

            container.Register(typeof (SFXUtility),
                () =>
                    Activator.CreateInstance(typeof (SFXUtility), bFlags, null, new object[] {container},
                        CultureInfo.CurrentCulture), true, true);

            var bType = typeof (Base);
            foreach (
                var type in
                    Assembly.GetAssembly(bType)
                        .GetTypes()
                        .OrderBy(type => type.Name)
                        .Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(bType)))
            {
                try
                {
                    var tmpType = type;
                    container.Register(tmpType,
                        () =>
                            Activator.CreateInstance(tmpType, bFlags, null, new object[] {container},
                                CultureInfo.CurrentCulture), true, true);
                }
                catch (Exception ex)
                {
                    container.Resolve<ILogger>().AddItem(new LogItem(ex));
                }
            }
        }
    }
}