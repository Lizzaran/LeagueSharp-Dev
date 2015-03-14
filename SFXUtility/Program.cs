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
    using System.Linq;
    using System.Reflection;
    using Classes;
    using SFXLibrary;
    using SFXLibrary.Data;
    using SFXLibrary.IoCContainer;
    using SFXLibrary.JSON;
    using SFXLibrary.Logger;

    #endregion

    /*
     * TODO: Thickness from circles option
     * TODO: Time Format mm:ss | ss option
     * TODO: Add comments?
     * TODO: Logger: L# Version, directory etc.
     */

    internal class Program
    {
        // ReSharper disable once UnusedParameter.Local
        private static void Main(string[] args)
        {
            var container = new Container();

            container.Register(typeof (ILogger),
                delegate
                {
                    var logger = (ExceptionLogger)Activator.CreateInstance(typeof (ExceptionLogger), AppDomain.CurrentDomain.BaseDirectory);
                    logger.FilterSensitiveData = true;
                    logger.LogLevel = LogLevel.High;
                    logger.SensitiveData = Sensitive.Data.ToArray();
                    return logger;
                }, true);

            AppDomain.CurrentDomain.UnhandledException +=
                delegate(object sender, UnhandledExceptionEventArgs eventArgs)
                {
                    var ex = sender as Exception ?? new NotSupportedException("Unhandled exception doesn't derive from System.Exception: " + sender);
                    container.Resolve<ILogger>().AddItem(new LogItem(ex));
                };

            container.Register<Mediator, Mediator>(true);

            container.Register(typeof (SFXUtility), () => Activator.CreateInstance(typeof (SFXUtility), container), true,
                true);

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
                    container.Register(type, () => Activator.CreateInstance(tmpType, container), true, true);
                }
                catch (Exception ex)
                {
                    container.Resolve<ILogger>().AddItem(new LogItem(ex));
                }
            }
        }
    }
}