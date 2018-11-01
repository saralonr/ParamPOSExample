using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace PaymentExample
{
    public class TempHelper
    {
        public static UserInfo user
        {
            get { return HttpContext.Current.Session["UserInfo"] as UserInfo; }
            set { HttpContext.Current.Session["UserInfo"] = value; }
        }
        public static ParamPosItem param
        {
            get { return HttpContext.Current.Session["ParamPosItem"] as ParamPosItem; }
            set { HttpContext.Current.Session["ParamPosItem"] = value; }
        }
        internal static void CreateTemp()
        {
            ParamPosItem item = new ParamPosItem();
            item.CLIENT_CODE = ConfigurationManager.AppSettings["CLIENT_CODE"].ToString();
            item.CLIENT_USERNAME = ConfigurationManager.AppSettings["CLIENT_USERNAME"].ToString();
            item.CLIENT_PASSWORD = ConfigurationManager.AppSettings["CLIENT_PASSWORD"].ToString();
            item.GUID = ConfigurationManager.AppSettings["GUID"].ToString();
            param = item;

            UserInfo inf = new UserInfo();
            inf.ID = 12345;
            inf.Firstname = "Jon";
            inf.Lastname = "Snow";
            inf.Phone = "05551111111";
            inf.Debt = 100.5m;
            user = inf;
        }
    }
    public class ParamPosItem
    {
        public string CLIENT_CODE { get; set; }
        public string CLIENT_USERNAME { get; set; }
        public string CLIENT_PASSWORD { get; set; }
        public string GUID { get; set; }
    }

    public class UserInfo
    {
        public int ID { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Phone { get; set; }
        public decimal Debt { get; set; }
    }
}