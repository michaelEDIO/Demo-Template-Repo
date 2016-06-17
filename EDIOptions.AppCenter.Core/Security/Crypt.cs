using System;
using System.IO;
using System.Security.Cryptography;
using EDIOptions.AppCenter.Session;

namespace EDIOptions.AppCenter.Security
{
    public static class Crypt
    {
        private const int KeySizeBits = KeySizeBytes * 8;
        private const int IVSizeBytes = 16;
        private const int KeySizeBytes = 16;

        public static string Get128BitKey()
        {
            byte[] key1 = new byte[16];
            using (RNGCryptoServiceProvider rcsp = new RNGCryptoServiceProvider())
            {
                rcsp.GetBytes(key1);
            }
            return ByteArrayToString(key1);
        }

        public static string EncryptStreamToTempFile(User user, Stream diStream)
        {
            try
            {
                if (!diStream.CanRead)
                {
                    return "";
                }

                byte[] iv = new byte[IVSizeBytes];
                byte[] key = new byte[KeySizeBytes];
                using (RNGCryptoServiceProvider rcsp = new RNGCryptoServiceProvider())
                {
                    rcsp.GetBytes(iv);
                    rcsp.GetBytes(key);
                }
                string ivAsString = ByteArrayToString(iv);
                string keyAsString = ByteArrayToString(key);
                string oFileName = Path.Combine(SiteFileSystem.GetTempFileDirectory(), ivAsString);
                using (AesCryptoServiceProvider aesProvider = new AesCryptoServiceProvider())
                {
                    aesProvider.KeySize = KeySizeBits;
                    aesProvider.IV = iv;
                    aesProvider.Key = key;
                    using (FileStream eoStream = File.Create(oFileName))
                    using (CryptoStream cryptoStream = new CryptoStream(eoStream, aesProvider.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        diStream.CopyTo(cryptoStream);
                    }
                }
                return keyAsString + ivAsString;
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, "Crypt", "EncryptStreamToTempFile", e.Message);
                return "";
            }
        }

        public static string EncryptFileToFile(User user, string iFileName)
        {
            try
            {
                if (!File.Exists(iFileName))
                {
                    return "";
                }
                byte[] iv = new byte[IVSizeBytes];
                byte[] key = new byte[KeySizeBytes];
                using (RNGCryptoServiceProvider rcsp = new RNGCryptoServiceProvider())
                {
                    rcsp.GetBytes(iv);
                    rcsp.GetBytes(key);
                }
                string ivAsString = ByteArrayToString(iv);
                string keyAsString = ByteArrayToString(key);
                string oFileName = Path.Combine(new FileInfo(iFileName).Directory.FullName, ivAsString);
                using (AesCryptoServiceProvider aesProvider = new AesCryptoServiceProvider())
                {
                    aesProvider.KeySize = KeySizeBits;
                    aesProvider.IV = iv;
                    aesProvider.Key = key;
                    using (FileStream diStream = File.OpenRead(iFileName))
                    using (FileStream eoStream = File.Create(oFileName))
                    using (CryptoStream cryptoStream = new CryptoStream(eoStream, aesProvider.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        diStream.CopyTo(cryptoStream);
                    }
                }
                return keyAsString + ivAsString;
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, "Crypt", "EncryptFileToFile", e.Message);
                return "";
            }
        }

        public static byte[] DecryptFileToArray(User user, string iFileName, string token)
        {
            try
            {
                if (!File.Exists(iFileName) || !IsTokenGood(token))
                {
                    return new byte[0];
                }
                byte[] keyArr = StringToByteArray(token.Substring(0, 32));
                byte[] ivArr = StringToByteArray(token.Substring(32));
                if (!(keyArr.Length == ivArr.Length && keyArr.Length == 16))
                {
                    return new byte[0];
                }
                using (MemoryStream doStream = new MemoryStream())
                {
                    using (AesCryptoServiceProvider aesProvider = new AesCryptoServiceProvider())
                    {
                        aesProvider.KeySize = KeySizeBits;
                        aesProvider.IV = ivArr;
                        aesProvider.Key = keyArr;
                        using (FileStream eiStream = File.OpenRead(iFileName))
                        using (CryptoStream cryptoStream = new CryptoStream(eiStream, aesProvider.CreateDecryptor(), CryptoStreamMode.Read))
                        {
                            cryptoStream.CopyTo(doStream);
                        }
                    }
                    return doStream.ToArray();
                }
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, "Crypt", "DecryptFileToArray", e.Message);
                return new byte[0];
            }
        }

        public static bool DecryptFileToFile(User user, string iFileName, string oFileName, string token)
        {
            try
            {
                if (!File.Exists(iFileName) || File.Exists(oFileName) || !IsTokenGood(token))
                {
                    return false;
                }
                byte[] keyArr = StringToByteArray(token.Substring(0, 32));
                byte[] ivArr = StringToByteArray(token.Substring(32));
                if (!(keyArr.Length == ivArr.Length && keyArr.Length == 16))
                {
                    return false;
                }
                using (AesCryptoServiceProvider aesProvider = new AesCryptoServiceProvider())
                {
                    aesProvider.KeySize = KeySizeBits;
                    aesProvider.IV = ivArr;
                    aesProvider.Key = keyArr;
                    using (FileStream eiStream = File.OpenRead(iFileName))
                    using (FileStream doStream = File.Create(oFileName))
                    using (CryptoStream cryptoStream = new CryptoStream(eiStream, aesProvider.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        cryptoStream.CopyTo(doStream);
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, "Crypt", "DecryptFileToFile", e.Message);
                return false;
            }
        }

        public static bool DecryptTempFileToFile(User user, string oFileName, string token)
        {
            if (!IsTokenGood(token))
            {
                return false;
            }
            string iFile = Path.Combine(SiteFileSystem.GetTempFileDirectory(), token.Substring(32));
            return DecryptFileToFile(user, iFile, oFileName, token);
        }

        public static bool IsTokenGood(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }
            if (key.Length != 64)
            {
                return false;
            }
            foreach (char c in key.ToUpper())
            {
                if (!(char.IsDigit(c) || (c >= 'A' && c <= 'F')))
                {
                    return false;
                }
            }
            return true;
        }

        private static byte[] StringToByteArray(String hexString)
        {
            byte[] ret = new byte[hexString.Length >> 1];
            for (int i = 0; i < hexString.Length; i += 2)
            {
                ret[i >> 1] = Convert.ToByte(hexString.Substring(i, 2), 16);
            }
            return ret;
        }

        private static string ByteArrayToString(byte[] ba)
        {
            string hex = BitConverter.ToString(ba);
            return hex.Replace("-", "");
        }
    }
}