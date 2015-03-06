#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 ConsoleLogger.cs is part of SFXLibrary.

 SFXLibrary is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXLibrary is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXLibrary. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

namespace SFXLibrary.Logger
{
    #region

    using System;
    using System.Diagnostics.CodeAnalysis;

    #endregion

    [SuppressMessage("ReSharper", "ExceptionNotDocumented")]
    public class ConsoleLogger : ILogger
    {
        public ConsoleLogger()
            : this(ConsoleColor.Yellow)
        {
        }

        public ConsoleLogger(ConsoleColor consoleColor)
        {
            ConsoleColor = consoleColor;
        }

        public ConsoleColor ConsoleColor { get; set; }
        public string Prefix { get; set; }

        public void Write(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor;
            Console.Write(string.IsNullOrWhiteSpace(Prefix)
                ? ex.StackTrace
                : string.Format("{0}: {1}", Prefix, ex.StackTrace));
            Console.ResetColor();
        }

        public void WriteBlock(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor;
            Console.WriteLine(string.IsNullOrWhiteSpace(Prefix)
                ? ex.Message
                : string.Format("{0}: {1}", Prefix, ex.Message));
            Console.ResetColor();
            Console.WriteLine("--------------------");
            Console.ForegroundColor = ConsoleColor;
            Console.WriteLine(ex.StackTrace);
            Console.ResetColor();
            Console.WriteLine("--------------------");
            Console.WriteLine(string.Empty);
        }

        public void WriteLine(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor;
            Console.Write(string.IsNullOrWhiteSpace(Prefix)
                ? ex.StackTrace
                : string.Format("{0}: {1}{2}", Prefix, ex.StackTrace, Environment.NewLine));
            Console.ResetColor();
        }
    }
}