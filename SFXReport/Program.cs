#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Program.cs is part of SFXReport.

 SFXReport is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXReport is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXReport. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

namespace SFXReport
{
    #region

    using System;
    using System.Collections.Generic;
    using System.IO;
    using SFXLibrary.IoCContainer;
    using SFXLibrary.JSON;
    using SFXLibrary.Logger;

    #endregion

    internal class Program
    {
        // ReSharper disable once UnusedParameter.Local
        private static void Main(string[] args)
        {
            //var sensitiveData = new List<string>
            //{
            //    Game.IP, Game.Region, Game.Id.ToString()
            //};
            //sensitiveData.AddRange(ObjectManager.Get<Obj_AI_Hero>().Select(hero => hero.Name));

            var container = new Container();
            container.Register(typeof (ILogger),
                () =>
                    Activator.CreateInstance(typeof (ExceptionLogger), AppDomain.CurrentDomain.BaseDirectory,
                        "{1}_{0}.txt", new JSONParameters {FilterSensitiveData = true}, LogLevel.High));

            var logger = container.Resolve<ILogger>();
            var exceptionLogger = logger as ExceptionLogger;

            var asd = new int[1];
            for (var i = 0; 10 > i; i++)
            {
                try
                {
                    var a = asd[3];
                }
                catch (Exception ex)
                {
                    if (exceptionLogger != null)
                    {
                        var eLogger = exceptionLogger;
                        eLogger.AddItem(new LogItem(ex) {Object = new Test()});
                    }
                }
            }

            Console.ReadKey();
        }
    }

    public class Test
    {
        public enum TEnum
        {
            One = 1,
            Two = 2
        }

        public const string cConstString = "cConstString";
        private BinaryReader binaryReader = new BinaryReader(new MemoryStream());
        public char cChar = 'a';
        public DateTime dateTime = DateTime.Now;

        private Dictionary<int, string> dictionary = new Dictionary<int, string>
        {
            {1, "1"},
            {2, "2"},
            {3, "3"}
        };

        private Dictionary<int, MemoryStream> dictionary2 = new Dictionary<int, MemoryStream>
        {
            {1, new MemoryStream()},
            {2, new MemoryStream()}
        };

        private int[] iArray = {1, 2, 3};

        public List<Test2> lList = new List<Test2>
        {
            new Test2 {Id = 1, Name = "1"},
            new Test2 {Id = 2, Name = "2"},
            new Test2 {Id = 3, Name = "3"}
        };

        public MemoryStream mStream = new MemoryStream();
        public bool? pBoolNull = null;
        public TEnum pEnum = TEnum.One;
        private int pInt = 1;
        private string pString = "Test";
        public string pStringEmpty = string.Empty;
        public string pStringNull = null;
    }

    public class Test2
    {
        public Test3 Test3 = new Test3 {Rofl = "test"};
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class Test3
    {
        public string Rofl = "Rofl";
    }
}