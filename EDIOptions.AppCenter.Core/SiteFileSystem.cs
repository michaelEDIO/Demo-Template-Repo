using System.IO;
using System.Web;
using EDIOptions.AppCenter.Session;

namespace EDIOptions.AppCenter
{
    public class SiteFileSystem
    {
        private static readonly string Root;
        private static readonly string TempDir;
        private const string folderTemp = "temp";
        private const string folderTest = "de";
        private const string folderLive = "pe";
        private const string folderDoc = "doc";
        private const string folderTransaction = "trx";
        private const string folderDownload = "download";
        private const string folderUpload = "upload";


        private const string folderTemplate = "template";
        private const string folderSpec = "spec";
        private const string folderOther = "other";

        private const string folderAll = "ALL";
        private const string folderCustomer = "CUST";
        private const string folderPartner = "PART";
        private const string folderBoth = "BOTH";

        static SiteFileSystem()
        {
            Root = HttpContext.Current.Server.MapPath("~/App_Data");
            TempDir = Path.Combine(Root, folderTemp);
        }

        #region File permission

        public static string GetContentType(string filename)
        {
            try
            {
                string ext = Path.GetExtension(filename);
                if (ext.Length < 2)
                {
                    return "";
                }
                switch (ext)
                {
                    case ".txt":
                        return "text/csv";
                    case ".csv":
                        return "text/csv";
                    case ".xls":
                        return "application/vnd.ms-excel";
                    case ".xlsx":
                        return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    case ".pdf":
                        return "application/pdf";
                    case ".zip":
                        return "application/octec-stream";
                    default:
                        return "";
                }
            }
            catch
            {
                return "";
            }
        }

        public static bool IsExtensionAllowed(string fileName)
        {
            try
            {
                string s = Path.GetExtension(fileName);
                if (string.IsNullOrWhiteSpace(s) || s.Length < 4)
                {
                    return false;
                }
                switch (s.ToLower())
                {
                    //case ".txt":
                    //case ".csv":
                    //case ".pdf":
                    //case ".zip":
                    case ".xls":
                    case ".xlsx":
                        return true;
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool IsContentTypeAllowed(string contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType))
            {
                return false;
            }
            switch (contentType.ToLower())
            {
                //case "text/plain":
                //case "text/csv":
                //case "application/pdf":
                //case "application/octec-stream":
                case "application/vnd.ms-excel":
                case "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet":
                    return true;
                default:
                    return false;
            }
        }

        #endregion File permission

        #region Upload fetch

        public static string GetUploadDirectory(User user, bool isTest)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.OCConnID))
            {
                return "";
            }
            return _CreateDirIfNotExist(Path.Combine(Root, isTest ? folderTest : folderLive, folderTransaction, user.OCConnID, folderUpload));
        }

        public static string GetUploadFileName(User user, bool isTest, string trxType, string extension)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.OCConnID) || string.IsNullOrWhiteSpace(trxType))
            {
                return "";
            }
            else
            {
                Rand r = new Rand();
                string fileName = string.Join("_", new string[] { user.NPConnID, user.Customer, user.ActivePartner, trxType, System.DateTime.Now.ToString("yyyyMMddHHmmss"), r.Next(65536).ToString().PadLeft(5, '0') });
                return Path.Combine(GetUploadDirectory(user, isTest), fileName) + extension;
            }
        }

        #endregion Upload fetch

        #region Temp fetch

        public static string GetTempFileDirectory()
        {
            return TempDir;
        }

        public static string GetTempFileName()
        {
            return Path.Combine(GetTempFileDirectory(), Path.GetRandomFileName());
        }

        #endregion Temp fetch

        #region Document fetch

        public static string GetSpecDocFilePath(User user, bool type, string sFileName)
        {
            return _GetDocFilePath(user, type, folderSpec, sFileName);
        }

        public static string GetTemplateDocFilePath(User user, bool type, string sFileName)
        {
            return _GetDocFilePath(user, type, folderTemplate, sFileName);
        }

        public static string GetGeneralDocFilePath(User user, bool type, string sFileName)
        {
            return _GetDocFilePath(user, type, folderOther, sFileName);
        }

        private static string _GetDocFilePath(User user, bool isTest, string folder, string sFileName)
        {
            string topLevelDir = Path.Combine(_GetDocFileDir(isTest), folderTemplate);
            string searchPath = Path.Combine(topLevelDir, folderBoth, user.Customer, user.ActivePartner, sFileName);
            if (File.Exists(searchPath))
            {
                return searchPath;
            }
            searchPath = Path.Combine(topLevelDir, folderPartner, user.ActivePartner, sFileName);
            if (File.Exists(searchPath))
            {
                return searchPath;
            }
            searchPath = Path.Combine(topLevelDir, folderCustomer, user.Customer, sFileName);
            if (File.Exists(searchPath))
            {
                return searchPath;
            }
            searchPath = Path.Combine(topLevelDir, folderAll, sFileName);
            if (File.Exists(searchPath))
            {
                return searchPath;
            }
            return "";
        }

        private static string _GetDocFileDir(bool isTest)
        {
            return Path.Combine(Root, isTest ? folderTest : folderLive, folderDoc);
        }

        #endregion Document fetch

        private static string _CreateDirIfNotExist(string dir)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return dir;
        }
    }
}