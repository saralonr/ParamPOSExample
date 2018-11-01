using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;

namespace PaymentExample
{
    public class GeneralHelper
    {
        public static string GetIPAddress()
        {
            return HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"] ?? HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
        }
        public static string conStr = ConfigurationManager.AppSettings["ConStr"];
    }
}