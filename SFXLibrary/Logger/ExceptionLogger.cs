#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 ExceptionLogger.cs is part of SFXLibrary.

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
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Extensions.NET;
    using JSON;

    #endregion

    public class ExceptionLogger : ProducerConsumer<ExceptionLogger.Item>, ILogger<ExceptionLogger.Item>
    {
        private readonly HashSet<string> _cache = new HashSet<string>();
        private readonly string _fileName;
        private readonly string _logDir;

        public ExceptionLogger(string logDir, string fileName = "{1}_{0}.txt")
        {
            _logDir = logDir;
            _fileName = fileName;
        }

        public JSONParameters JSONParams { get; set; }
        public LogLevel LogLevel { get; set; }

        public new void AddItem(Item item)
        {
            if (LogLevel == LogLevel.None || item == null || string.IsNullOrEmpty(item.Exception))
                return;

            var cache = item.Exception.ToBase64();
            if (!_cache.Contains(cache))
            {
                _cache.Add(cache);
                base.AddItem(item);
            }
        }

        protected override void ProcessItem(Item item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Exception))
                return;

            try
            {
                var fname = string.Format(_fileName,
                    (item.Exception + item.AdditionalInformation.ToDebugString()).ToMd5Hash(),
                    LogLevel.ToString().ToLower());

                if (File.Exists(Path.Combine(_logDir, fname)))
                    return;

                Directory.CreateDirectory(_logDir);

                using (var fs = new FileStream(fname, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, true))
                {
                    var log = string.Empty;
                    switch (LogLevel)
                    {
                        case LogLevel.Low:
                            log = JSON.ToJSON(item.Exception, JSONParams);
                            break;
                        case LogLevel.Medium:
                        case LogLevel.High:
                            log = JSON.ToJSON(item, JSONParams);
                            break;
                    }
                    var logByte = new UTF8Encoding(true).GetBytes(log);
                    fs.Write(logByte, 0, logByte.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public class Item
        {
            public readonly string Exception;
            public Dictionary<string, string> AdditionalInformation;

            public Item(Exception exception)
            {
                if (exception != null)
                    Exception = exception.ToString();
            }

            public object Object { get; set; }
        }
    }
}