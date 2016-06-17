using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace EDIOptions.AppCenter.Security
{
    /// <summary>
    /// Provides a default implementation of the MD5-crypt algorithm.
    /// </summary>
    public static class MD5Crypt
    {
        private const string itoa64 = "./0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

        /// <summary>
        /// Hashes a given password with a given salt.
        /// </summary>
        /// <param name="inPassword">The password to hash.</param>
        /// <param name="inSalt">The salt to use.</param>
        /// <returns>The full MD5 hash of the password and salt.</returns>
        public static string Hash(string inPassword, string inSalt)
        {
            if (inPassword == null)
            {
                inPassword = "";
            }
            if (inSalt == null)
            {
                inSalt = "";
            }
            var md5csp = MD5.Create();
            byte[] password = Encoding.UTF8.GetBytes(inPassword);
            byte[] salt = Encoding.UTF8.GetBytes(_GetRawSalt(inSalt));
            byte[] magic = { (byte)'$', (byte)'1', (byte)'$' };

            byte[] altSum = alternateSum(md5csp, password, salt);
            byte[] intSum = intermediateSum(md5csp, password, magic, salt, altSum);

            for (int i = 0; i < 1000; i++)
            {
                intermediateCalc(md5csp, password, salt, intSum, i);
            }

            MemoryStream output = new MemoryStream();
            output.Write(magic, 0, magic.Length);
            output.Write(salt, 0, salt.Length);
            output.WriteByte((byte)'$');

            EncodeHash(output, intSum);

            return Encoding.UTF8.GetString(output.ToArray());
        }

        /// <summary>
        /// Checks if a given password hashes to the given hash.
        /// </summary>
        /// <param name="inPassword">The password to check.</param>
        /// <param name="inHash">The hash to check against.</param>
        /// <returns>True if the hash matches, false otherwise.</returns>
        public static bool Verify(string inPassword, string inHash)
        {
            Regex r = new Regex(@"^\$1\$(.{0,8})\$([A-Za-z0-9/.]{22})$");
            if (string.IsNullOrEmpty(inHash))
            {
                return false;
            }
            var match = r.Match(inHash);
            if (!match.Success)
            {
                return false;
            }
            string salt = match.Groups[0].Value;
            string hash = match.Groups[1].Value;
            if (inPassword == null)
            {
                inPassword = "";
            }
            string check = Hash(inPassword, salt);
            return check == inHash;
        }

        private static string _GetRawSalt(string salt)
        {
            if (string.IsNullOrEmpty(salt))
            {
                return "";
            }
            int saltStart = 0;
            if (salt.StartsWith("$1$"))
            {
                saltStart += 3;
            }
            int saltEnd = saltStart;
            while (++saltEnd < salt.Length && salt[saltEnd] != '$')
            {
            }

            return salt.Substring(saltStart, Math.Min(saltEnd - saltStart, 8));
        }

        private static void EncodeHash(MemoryStream output, byte[] hashBuf)
        {
            int temp = 0;
            temp = (hashBuf[0] << 16) | (hashBuf[6] << 8) | hashBuf[12]; to64(output, temp, 4);
            temp = (hashBuf[1] << 16) | (hashBuf[7] << 8) | hashBuf[13]; to64(output, temp, 4);
            temp = (hashBuf[2] << 16) | (hashBuf[8] << 8) | hashBuf[14]; to64(output, temp, 4);
            temp = (hashBuf[3] << 16) | (hashBuf[9] << 8) | hashBuf[15]; to64(output, temp, 4);
            temp = (hashBuf[4] << 16) | (hashBuf[10] << 8) | hashBuf[5]; to64(output, temp, 4);
            temp = hashBuf[11]; to64(output, temp, 2);
        }

        private static void to64(MemoryStream s, int v, int n)
        {
            while (--n >= 0)
            {
                s.WriteByte((byte)itoa64[v & 0x3f]);
                v >>= 6;
            }
        }

        private static byte[] alternateSum(HashAlgorithm algorithm, byte[] password, byte[] salt)
        {
            algorithm.Initialize();
            algorithm.AddToDigest(password);
            algorithm.AddToDigest(salt);
            algorithm.AddToDigest(password);
            return algorithm.FinalizeAndGetHash();
        }

        private static byte[] intermediateSum(HashAlgorithm algorithm, byte[] password, byte[] magic, byte[] salt, byte[] alternateSum)
        {
            algorithm.Initialize();
            algorithm.AddToDigest(password);
            algorithm.AddToDigest(magic);
            algorithm.AddToDigest(salt);
            algorithm.AddToDigestBuffered(alternateSum, password.Length);
            byte[] temp = new byte[1];
            for (int i = password.Length; i != 0; i >>= 1)
            {
                if ((i & 1) != 0)
                {
                    temp[0] = 0;
                }
                else
                {
                    temp[0] = password[0];
                }
                algorithm.AddToDigest(temp);
            }
            return algorithm.FinalizeAndGetHash();
        }

        private static void intermediateCalc(HashAlgorithm algorithm, byte[] password, byte[] salt, byte[] intermediateSum, int i)
        {
            algorithm.Initialize();
            if ((i & 1) != 0)
            {
                algorithm.AddToDigest(password);
            }
            else
            {
                algorithm.AddToDigest(intermediateSum);
            }
            if ((i % 3) != 0)
            {
                algorithm.AddToDigest(salt);
            }

            if ((i % 7) != 0)
            {
                algorithm.AddToDigest(password);
            }

            if ((i & 1) != 0)
            {
                algorithm.AddToDigest(intermediateSum);
            }
            else
            {
                algorithm.AddToDigest(password);
            }
            Array.ConstrainedCopy(algorithm.FinalizeAndGetHash(), 0, intermediateSum, 0, intermediateSum.Length);
        }
    }
}