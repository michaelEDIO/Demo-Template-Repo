using System;
using System.Security.Cryptography;

namespace EDIOptions.AppCenter
{
    public class Rand
    {
        private readonly RNGCryptoServiceProvider rcsp;

        public Rand()
        {
            rcsp = new RNGCryptoServiceProvider();
        }

        public Rand(int seed)
            : this()
        {
        }

        /// <summary>
        /// Returns a nonnegative random number.
        /// </summary>
        /// <returns>A 32-bit signed integer that is greater than or equal to 0 and less than <see cref="Int32.MaxValue"/></returns>
        public int Next()
        {
            return Next(0, int.MaxValue);
        }

        /// <summary>
        /// Returns a nonnegative random number less than the specified maximum.
        /// </summary>
        /// <param name="maxValue">The exclusive upper bound of the random number to be generated. maxValue must be greater than or equal to zero.</param>
        /// <returns>A 32-bit signed integer that is greater than or equal to 0, and less than maxValue; that is, the rangeOutput of return values ordinarily includes 0 but not maxValue. However, if maxValue equals 0, maxValue is returned.</returns>
        public int Next(int maxValue)
        {
            if (maxValue < 0)
            {
                throw new ArgumentOutOfRangeException("maxValue");
            }
            return Next(0, maxValue);
        }

        /// <summary>
        /// Returns a random number within a specified rangeOutput.
        /// </summary>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">The exclusive upper bound of the random number returned. maxValue must be greater than or equal to minValue.</param>
        /// <returns>A 32-bit signed integer greater than or equal to minValue and less than maxValue; that is, the rangeOutput of return values includes minValue but not maxValue. If minValue equals maxValue, minValue is returned.</returns>
        public int Next(int minValue, int maxValue)
        {
            if (minValue > maxValue)
            {
                int temp = minValue;
                minValue = maxValue;
                maxValue = temp;
            }
            else if (minValue == maxValue)
            {
                return minValue;
            }

            ulong cap = 1UL << 31;
            ulong rangeOutput = (ulong)maxValue - (ulong)minValue;
            ulong rangeInput = (cap / rangeOutput) * rangeOutput;
            ulong tempRand = 0;
            while ((tempRand = (NextULong() & 0xFFFFFFFF)) >= rangeInput) { }
            return (int)(tempRand % rangeOutput) + minValue;
        }

        /// <summary>
        /// Returns a random floating-point number that is greater than or equal to 0.0, and less than 1.0.
        /// </summary>
        /// <returns>A double-precision floating point number that is greater than or equal to 0.0, and less than 1.0.</returns>
        public double NextDouble()
        {
            ulong ul = NextULong() / (1 << 11);
            return ul / (double)(1UL << 53);
        }

        /// <summary>
        /// Fills the elements of a specified array of bytes with random numbers.
        /// </summary>
        /// <param name="buffer">An array of bytes to contain random numbers.</param>
        public void NextBytes(byte[] buffer)
        {
            rcsp.GetBytes(buffer);
        }

        /// <summary>
        /// Returns a random number.
        /// </summary>
        /// <returns>A 64-bit unsigned integer.</returns>
        public ulong NextULong()
        {
            byte[] temp = new byte[8];
            rcsp.GetBytes(temp);
            return BitConverter.ToUInt64(temp, 0);
        }
    }
}