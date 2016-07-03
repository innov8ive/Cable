using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HMS
{
    public class UserSetting
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailID { get; set; }
        public int UserID { get; set; }
        public int UserType { get; set; }
        public bool IsActive { get; set; }
        public int OperatorID { get; set; }
        public string Password { get; set; }
        public string Mobile { get; set; }
        public string NetworkName { get; set; }
        public DateTime RegDate { get; set; }
        public string BackdoorLogin { get; set; }
        public bool SMSService { get; set; }
        public bool AdService { get; set; }
    }
    public class Delegates
    {
        public delegate void PopupChanged(object sender, EventArgs e);
    }
}