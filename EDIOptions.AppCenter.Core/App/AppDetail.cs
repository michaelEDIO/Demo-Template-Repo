namespace EDIOptions.AppCenter
{
    public class AppDetail
    {
        public readonly string appName;

        public readonly string appURL;

        public AppDetail(string name, string url)
        {
            appName = name;
            appURL = url;
        }
    }
}