using EDIOptions.AppCenter.Database;
using Newtonsoft.Json;
using System;

namespace EDIOptions.AppCenter.IntegrationManager
{
    [JsonObject(MemberSerialization.OptIn)]
    public class IntHeadRecord
    {
        [JsonProperty(PropertyName = "key")]
        public string Key { get; set; }

        [JsonProperty(PropertyName = "invoiceno")]
        public string InvoiceNumber { get; set; }

        [JsonProperty(PropertyName = "trxdate")]
        public string TransactionDate { get; set; }

        [JsonProperty(PropertyName = "shipdate")]
        public string ShipDate { get; set; }

        [JsonProperty(PropertyName = "ponumber")]
        public string PoNumber { get; set; }

        [JsonProperty(PropertyName = "bolnumber")]
        public string BolNumber { get; set; }

        [JsonProperty(PropertyName = "releasenum")]
        public string MasterPONumber { get; set; }

        [JsonProperty(PropertyName = "scaccode")]
        public string ScacCode { get; set; }

        [JsonProperty(PropertyName = "routing")]
        public string Routing { get; set; }

        [JsonProperty(PropertyName = "pltype")]
        public string PackingListType { get; set; }

        [JsonProperty(PropertyName = "stid")]
        public string StId { get; set; }

        [JsonProperty(PropertyName = "byid")]
        public string ById { get; set; }

        [JsonProperty(PropertyName = "msg")]
        public string Messages { get; set; }

        [JsonProperty(PropertyName = "xfertype")]
        public string TransferType { get; set; }

        [JsonProperty(PropertyName = "hprocessed")]
        public string HProcessed { get; set; }

        public IntHeadRecord()
        {
            Key = "";
            InvoiceNumber = "";
            TransactionDate = "";
            ShipDate = "";
            PoNumber = "";
            BolNumber = "";
            ScacCode = "";
            Routing = "";
            PackingListType = "";
            StId = "";
            ById = "";
            Messages = "";
            TransferType = "";
            HProcessed = "";
            MasterPONumber = "";
        }

        public IntHeadRecord(DBResult queryCurrent)
        {
            DateTime temp;
            string date = queryCurrent.FieldByName(_Column.ShipmentDate, "");
            if (!DateTime.TryParse(date, out temp))
            {
                ShipDate = date;
            }
            else
            {
                ShipDate = temp.ToString("MMM dd yyyy");
            }
            date = queryCurrent.FieldByName(_Column.TrxDate, "");
            if (!DateTime.TryParse(date, out temp))
            {
                TransactionDate = date;
            }
            else
            {
                TransactionDate = temp.ToString("MMM dd yyyy");
            }

            Key = queryCurrent.FieldByName(_Column.UniqueKey);
            InvoiceNumber = queryCurrent.FieldByName(_Column.InvoiceNo, "");
            BolNumber = queryCurrent.FieldByName(_Column.BOLNumber, "");
            PoNumber = queryCurrent.FieldByName(_Column.PONumber, "");
            MasterPONumber = queryCurrent.FieldByName(_Column.ReleaseNum, "");
            ScacCode = queryCurrent.FieldByName(_Column.SCACCode, "");
            Routing = queryCurrent.FieldByName(_Column.Routing, "");
            StId = queryCurrent.FieldByName(_Column.STId, "");
            ById = queryCurrent.FieldByName(_Column.BYId, "");
            Messages = queryCurrent.FieldByName(_Column.Msg, "");
            TransferType = queryCurrent.FieldByName(_Column.XferType, _InvoiceTransferType.Mixed);
            HProcessed = queryCurrent.FieldByName(_Column.HProcessed, "");
        }
    }
}