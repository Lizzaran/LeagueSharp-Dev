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
    using System.IO.Compression;
    using System.Text;
    using Extensions.NET;
    using JSON;

    #endregion

    public class ExceptionLogger : ProducerConsumer<LogItem>, ILogger
    {
        private readonly bool _compression;
        private readonly string _fileName;
        private readonly string _logDir;
        private readonly HashSet<string> _unique = new HashSet<string>();

        public ExceptionLogger(string logDir, string fileName = "{1}_{0}.gz", bool compression = true,
            LogLevel logLevel = LogLevel.High)
        {
            _logDir = logDir;
            _fileName = fileName;
            _compression = compression;
            LogLevel = logLevel;
        }

        public JSONParameters JSONParams { get; set; }
        public Dictionary<string, string> AdditionalData { get; set; }
        public bool FilterSensitiveData { get; set; }
        public string[] SensitiveData { get; set; }
        public LogLevel LogLevel { get; set; }

        public new void AddItem(LogItem item)
        {
            if (LogLevel == LogLevel.None || item == null || string.IsNullOrEmpty(item.Exception))
                return;

            var uniqueValue = (item.Exception + AdditionalData.ToDebugString()).Trim();
            if (!_unique.Contains(uniqueValue))
            {
                _unique.Add(uniqueValue);
                base.AddItem(item);
            }
        }

        protected override void ProcessItem(LogItem item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Exception))
                return;

            try
            {
                var fname = string.Format(_fileName,
                    (item.Exception + AdditionalData.ToDebugString()).ToMd5Hash(),
                    LogLevel.ToString().ToLower());

                if (File.Exists(Path.Combine(_logDir, fname)))
                    return;

                using (
                    var fileStream = new FileStream(fname, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096,
                        true))
                using (Stream gzStream = new GZipStream(fileStream, CompressionMode.Compress, false))
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

                    if (FilterSensitiveData)
                        log = FilterData(log, SensitiveData);

                    var logByte = new UTF8Encoding(true).GetBytes(log);

                    if (!string.IsNullOrWhiteSpace(log))
                    {
                        if (_compression)
                            gzStream.Write(logByte, 0, logByte.Length);
                        else
                            fileStream.Write(logByte, 0, logByte.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private string FilterData(string data, string[] sensitiveData)
        {
            return data.Replace(sensitiveData, "[filtered]");
        }
    }
}