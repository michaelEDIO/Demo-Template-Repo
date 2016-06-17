using System;
using System.Security.Cryptography;

namespace EDIOptions.AppCenter.Security
{
    internal static class HashAlgorithmExtensions
    {
        public static void AddToDigest(this HashAlgorithm algorithm, byte[] block)
        {
            algorithm.AddToDigest(block, 0, block.Length);
        }

        public static void AddToDigest(this HashAlgorithm algorithm, byte[] block, int offset, int length)
        {
            algorithm.TransformBlock(block, offset, length, null, 0);
        }

        public static void AddToDigestBuffered(this HashAlgorithm algorithm, byte[] inputBuffer, int outputLength)
        {
            algorithm.AddToDigestBuffered(inputBuffer, 0, inputBuffer.Length, outputLength);
        }

        public static void AddToDigestBuffered(this HashAlgorithm algorithm, byte[] inputBuffer, int inputOffset, int inputLength, int outputLength)
        {
            for (int i = 0; i < outputLength; i += inputLength)
            {
                algorithm.AddToDigest(inputBuffer, inputOffset, Math.Min(inputLength, outputLength - i));
            }
        }

        public static byte[] FinalizeAndGetHash(this HashAlgorithm algorithm)
        {
            algorithm.TransformFinalBlock(new byte[0], 0, 0);
            byte[] ret = new byte[algorithm.Hash.Length];
            Array.ConstrainedCopy(algorithm.Hash, 0, ret, 0, ret.Length);
            return ret;
        }
    }
}