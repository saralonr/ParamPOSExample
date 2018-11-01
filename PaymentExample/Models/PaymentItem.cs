using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PaymentExample.Models
{
    public class PaymentItem
    {
        public string CardNumber { get; set; }
        public string CardMonthYear { get; set; }
        public string SecurityCode { get; set; }
        public string CardOwner { get; set; }
        public string Installment { get; set; }
        public int Ozel_Oran_SK_ID { get; set; }
    }
}