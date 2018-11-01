using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PaymentExample.Models
{
    public class InstallmentPlan
    {
        public long Ozel_Oran_SK_ID { get; set; }
        public Guid GUID { get; set; }
        public int SanalPOS_ID { get; set; }
        public string Kredi_Karti_Banka { get; set; }
        public string Kredi_Karti_Banka_Gorsel { get; set; }
        public decimal? MO_01 { get; set; }
        public decimal? MO_02 { get; set; }
        public decimal? MO_03 { get; set; }
        public decimal? MO_04 { get; set; }
        public decimal? MO_05 { get; set; }
        public decimal? MO_06 { get; set; }
        public decimal? MO_07 { get; set; }
        public decimal? MO_08 { get; set; }
        public decimal? MO_09 { get; set; }
        public decimal? MO_10 { get; set; }
        public decimal? MO_11 { get; set; }
        public decimal? MO_12 { get; set; }

        public decimal? MO_01_Price { get; set; }
        public decimal? MO_02_Price { get; set; }
        public decimal? MO_03_Price { get; set; }
        public decimal? MO_04_Price { get; set; }
        public decimal? MO_05_Price { get; set; }
        public decimal? MO_06_Price { get; set; }
        public decimal? MO_07_Price { get; set; }
        public decimal? MO_08_Price { get; set; }
        public decimal? MO_09_Price { get; set; }
        public decimal? MO_10_Price { get; set; }
        public decimal? MO_11_Price { get; set; }
        public decimal? MO_12_Price { get; set; }
    }
}