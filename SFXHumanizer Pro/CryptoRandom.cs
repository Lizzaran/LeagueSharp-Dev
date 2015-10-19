#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 CryptoRandom.cs is part of SFXHumanizer Pro.

 SFXHumanizer Pro is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXHumanizer Pro is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXHumanizer Pro. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Security.Cryptography;

#endregion

namespace SFXHumanizer_Pro
{
    public class CryptoRandom : Random
    {
        private readonly RNGCryptoServiceProvider _rng = new RNGCryptoServiceProvider();
        private byte[] _buffer;
        private int _bufferPosition;

        private void InitBuffer()
        {
            if (_buffer == null || _buffer.Length != 512)
            {
                _buffer = new byte[512];
            }

            _rng.GetBytes(_buffer);
            _bufferPosition = 0;
        }

        public override int Next()
        {
            return (int) GetRandomUInt32() & 0x7FFFFFFF;
        }

        /// <exception cref="ArgumentOutOfRangeException">maxValue</exception>
        public override int Next(int maxValue)
        {
            if (maxValue < 0)
            {
                throw new ArgumentOutOfRangeException("maxValue");
            }

            return Next(0, maxValue);
        }

        /// <exception cref="ArgumentOutOfRangeException">minValue</exception>
        public override int Next(int minValue, int maxValue)
        {
            if (minValue > maxValue)
            {
                throw new ArgumentOutOfRangeException("minValue");
            }

            if (minValue == maxValue)
            {
                return minValue;
            }

            long diff = maxValue - minValue;

            while (true)
            {
                var rand = GetRandomUInt32();

                var max = 1 + (long) uint.MaxValue;
                var remainder = max % diff;

                if (rand < max - remainder)
                {
                    return (int) (minValue + (rand % diff));
                }
            }
        }

        public override double NextDouble()
        {
            return GetRandomUInt32() / (1.0 + uint.MaxValue);
        }

        private uint GetRandomUInt32()
        {
            lock (this)
            {
                EnsureRandomBuffer(4);

                var rand = BitConverter.ToUInt32(_buffer, _bufferPosition);

                _bufferPosition += 4;

                return rand;
            }
        }

        private void EnsureRandomBuffer(int requiredBytes)
        {
            if (_buffer == null)
            {
                InitBuffer();
            }

            if (_buffer == null)
            {
                return;
            }

            if (requiredBytes > _buffer.Length)
            {
                throw new ArgumentOutOfRangeException("requiredBytes", "can't be greater than buffer");
            }

            if ((_buffer.Length - _bufferPosition) < requiredBytes)
            {
                InitBuffer();
            }
        }
    }
}