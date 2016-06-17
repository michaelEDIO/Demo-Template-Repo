using EDIOptions.AppCenter;
using EDIOptions.AppCenter.IntegrationManager;
using EDIOptions.AppCenter.Session;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;

namespace EDIOptCenterNet
{
    public struct youtubeVideo
    {
        [JsonProperty(PropertyName = "videolink")]
        string videolink { get; set; }
        [JsonProperty(PropertyName = "title")]
        string title { get; set; }
        


        public  youtubeVideo(string _videolink, string _title)
        {
            videolink = _videolink;
            title = _title;
        }

    }
    

    public partial class DemoTemplate : System.Web.UI.Page
    {
        

        
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        [WebMethod]
        public static string getYouTubeLinks(string version, string data)
        {

            youtubeVideo yt1 = new youtubeVideo("https://www.youtube.com/embed/NLA0DJU4U7w", "HomePage Video");
            youtubeVideo yt2 = new youtubeVideo("https://www.youtube.com/embed/z8Je3f4A2_A", "Preparing to Invoice");
            youtubeVideo yt3 = new youtubeVideo("https://www.youtube.com/embed/c61vy9opdP0", "Create a Packing List");


            List<youtubeVideo> youtubeVideos = new List<youtubeVideo>() { yt1,yt2,yt3 };

            return ApiResponse.JSONSuccess(new
            {
               videos = youtubeVideos
            });

        }
    }
}