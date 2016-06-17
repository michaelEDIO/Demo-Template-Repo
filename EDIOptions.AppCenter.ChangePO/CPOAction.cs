using Newtonsoft.Json;

namespace EDIOptions.AppCenter.ChangePO
{
    public enum ActionType
    {
        Cancel,
        Apply
    }

    [JsonObject(MemberSerialization.OptOut)]
    public class CPOAction
    {
        public ActionType Action { get; set; }
        public string Key { get; set; }
    }
}