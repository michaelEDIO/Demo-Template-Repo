using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace EDIOptions.AppCenter.LocationReport
{
    [JsonObject(MemberSerialization.OptOut)]
    public class LocationReportDetails
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Type { get; set; }
        public List<string> Stores { get; set; }
        public List<string> BrandNames { get; set; }
        public List<string> AENames { get; set; }
        public string BOLQuery { get; set; }
        public string POQuery { get; set; }

        public LocationReportDetails()
        {
            StartDate = DateTime.Now;
            EndDate = DateTime.Now;
            Type = "Location";
            Stores = new List<string>();
            BrandNames = new List<string>();
            AENames = new List<string>();
            BOLQuery = "";
            POQuery = "";
        }
    }
}