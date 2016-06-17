namespace EDIOptions.AppCenter.Session
{
    /// <summary>
    /// A class used to hold session keys.
    /// </summary>
    public static class SKeys
    {
        /// <summary>
        /// The User object.
        /// </summary>
        public const string User = "User";

        /// <summary>
        /// Indicates if the environment is a test environment.
        /// </summary>
        public const string IsTest = "IsTest";

        /// <summary>
        /// The landing page URL.
        /// </summary>
        public const string LandingPg = "LandingPg";

        /// <summary>
        /// The current session ID.
        /// </summary>
        public const string SessionID = "ID";

        /// <summary>
        /// Keeps track of tokens.
        /// </summary>
        public const string TokenSet = "TokenDict";

        /// <summary>
        /// Holds the result from recent uploads.
        /// </summary>
        public const string UploadResponse = "UploadResponse";

        /// <summary>
        /// A dictionary of various transaction types, and a description of each one.
        /// </summary>
        public const string TrxDict = "TrxDict";

        /// <summary>
        /// True if the session is maintained by the OptCenter site.
        /// </summary>
        public const string IsOCSession = "IsOCSession";
    }
}