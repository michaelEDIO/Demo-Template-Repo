using EDIOptions.AppCenter.Security;

namespace EDIOptions.AppCenter
{
    /// <summary>
    /// Helper string functions
    /// </summary>
    public static class StringHelper
    {
        private const string base36 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string alpha = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string numbers = "1234567890";

        /// <summary>
        /// Get random alpha-numeric string
        /// </summary>
        /// <param name="size">Size of string</param>
        /// <returns></returns>
        public static string GetRandomAlphaNumeric(int size)
        {
            return GetRandomString(size, base36);
        }

        /// <summary>
        /// Get random alpha only string
        /// </summary>
        /// <param name="size">Size of string</param>
        /// <returns></returns>
        public static string GetRandomAlpha(int size)
        {
            return GetRandomString(size, alpha);
        }

        /// <summary>
        /// Get random numberic only string
        /// </summary>
        /// <param name="size">Size of string</param>
        /// <returns></returns>
        public static string GetRandomNumeric(int size)
        {
            return GetRandomString(size, numbers);
        }

        /// <summary>
        /// Gets a random string using the charset provided
        /// </summary>
        /// <param name="size">Size of string</param>
        /// <param name="charset">Charset to use</param>
        /// <returns></returns>
        public static string GetRandom(int size, string charset)
        {
            return GetRandomString(size, charset);
        }

        private static string GetRandomString(int size, string charset)
        {
            string x = "";
            Rand rand = new Rand();
            for (int i = 0; i < size; i++)
            {
                x += charset[rand.Next(0, charset.Length - 1)];
            }
            return x;
        }

        /// <summary>
        /// Converts a long to a base 36 string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToBase36(this long value)
        {
            string result = "";
            do
            {
                result = base36[(int)(value % 36)] + result;
            } while ((value /= 36) > 0);

            return result;
        }
    }
}