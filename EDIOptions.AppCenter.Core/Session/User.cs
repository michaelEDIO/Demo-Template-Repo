using System.Collections.Generic;

namespace EDIOptions.AppCenter.Session
{
    public class User
    {
        public User()
        {
            IsGuest = false;
        }

        public string UserName { get; set; }

        public string Email { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public int Level { get; set; }

        public string Customer { get; set; }

        public List<PartnerDetail> PartnerList { get; set; }

        public int PartnerIndex { get; set; }

        public string ActivePartner { get { return PartnerList[PartnerIndex].ID; } }

        public string ActivePartnerName { get { return PartnerList[PartnerIndex].FullName; } }

        public string CompanyName { get; set; }

        public string OCConnID { get; set; }

        public string NPConnID { get; set; }

        public string SHConnID { get; set; }

        public string SLConnID { get; set; }

        public bool IsGuest { get; set; }
    }
}