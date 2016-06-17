using EDIOptions.AppCenter;
using EDIOptions.AppCenter.ItemDistributionManager;
using EDIOptions.AppCenter.Session;
using Newtonsoft.Json;
using System;
using System.Web;
using System.Web.Services;
using System.Web.SessionState;
using System.Web.UI;

namespace EDIOptCenterNet
{
    public partial class IDM : System.Web.UI.Page
    {
        protected delegate void fetchItemsDelegate(HttpSessionState state);

        protected fetchItemsDelegate _fetchList;

        protected static JsonSerializerSettings serializerSettings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };

        protected void Page_Load(object sender, EventArgs e)
        {
            SiteMaster.SetAppID(AppID.ItemDistributionManager);
            _fetchList = (state) =>
            {
                var user = Session[SKeys.User] as User;
                var distroDays = ItemTable.GetDistributionDays(user);

                distroBox.Text = distroDays.ToString();
                sunCheckbox.Checked = distroDays.IsSunday;
                monCheckbox.Checked = distroDays.IsMonday;
                tueCheckbox.Checked = distroDays.IsTuesday;
                wedCheckbox.Checked = distroDays.IsWednesday;
                thuCheckbox.Checked = distroDays.IsThursday;
                friCheckbox.Checked = distroDays.IsFriday;
                satCheckbox.Checked = distroDays.IsSaturday;

                minDollarBox.Value = minDollarLabel.InnerText = distroDays.MinDollars.ToString();
                dayRangeBox.Value = dayRangeLabel.InnerText = distroDays.DayRange.ToString();

                var itemData = ItemTable.GetItemData(user);

                editGrid.DataSource = itemData;
                editGrid.DataBind();
                editGrid.HeaderRow.TableSection = System.Web.UI.WebControls.TableRowSection.TableHeader;

                itemGrid.DataSource = itemData;
                itemGrid.DataBind();
                itemGrid.HeaderRow.TableSection = System.Web.UI.WebControls.TableRowSection.TableHeader;
            };
            Page.RegisterAsyncTask(new PageAsyncTask((sndr, args, callback, extraData) => { return _fetchList.BeginInvoke(HttpContext.Current.Session, callback, extraData); }, empty => { }, empty => { }, null));
        }

        [WebMethod]
        public static string UpdateData(string version, string data)
        {
            if (SiteMaster.VerifyRequest(HttpContext.Current.Session))
            {
                var user = HttpContext.Current.Session[SKeys.User] as User;
                var updateData = JsonConvert.DeserializeObject<UpdateData>(data);
                try
                {
                    ItemTable.DoChanges(user, updateData);
                }
                catch
                {
                    return JsonConvert.SerializeObject(ApiResponse.Error(ResponseType.ErrorIDMUnknown), serializerSettings);
                }
                return JsonConvert.SerializeObject(ApiResponse.Success(), serializerSettings);
            }
            else
            {
                return JsonConvert.SerializeObject(ApiResponse.Error(ResponseType.ErrorAuth), serializerSettings);
            }
        }
    }
}