#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 ILogger.cs is part of SFXLibrary.

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

    #endregion

    public interface ILogger
    {
        string Prefix { get; set; }
        void Write(Exception ex);
        void WriteBlock(Exception ex);
        void WriteLine(Exception ex);
    }
}