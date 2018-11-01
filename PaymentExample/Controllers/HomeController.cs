using Newtonsoft.Json;
using PaymentExample.Models;
using PaymentExample.tr.com.ew.dmzws;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PaymentExample.Controllers
{
    public class HomeController : Controller
    {
        SqlConnection conn = new SqlConnection();
        ST_WS_Guvenlik sec = new ST_WS_Guvenlik();
        TurkPosWSTEST pos = new TurkPosWSTEST(); // TEST SERVİSİ OLDUĞU İÇİN TurkPosWSTEST INSTANCE'Sİ ALINDI. //

        #region Variables
        decimal productPrice = 1240.50m; // BU PROJEDEKİ ÖRNEKTE BORÇ OLARAK 1240TL 50 KURUŞ BELİRLENDİ. //
        int[] installmentArray = new int[] { 3, 6, 9, 12 }; // PARAMPOS'TA GENELDE 1-3-6-9-12 TAKSİT SEÇENEKLERİ ÇIKIYOR. O YÜZDEN BU ÖRNEKTE DE  3-6-9-12 SEÇENEKLERİ EKLENDİ. DEĞİŞTİREBİLİRSİNİZ. BURAYI DEĞİŞTİRMENİZ SONRASI GetInstallmentPlans.cshtml VİEW'INDAKİ "for (int i = insArray[0]; i <= insArray[insArray.Length - 1]; i += 3)"  DÖNGÜSÜNDEKİ i += 3 DEĞİŞTİRİLMELİDİR. //
        string[] installmentRate = new string[] { "1,00", "100", "1,50", "100", "100", "100", "100", "100", "100", "100", "100", "100" }; // TAKSİT ORANLARI GÜNCELLENECEKSE BU DİZİDEKİ ORANLARLA GÜNCELLENECEK. //
        bool specRatio;
        #endregion
        public HomeController()
        {
            sec = new ST_WS_Guvenlik() { CLIENT_CODE = TempHelper.param.CLIENT_CODE, CLIENT_PASSWORD = TempHelper.param.CLIENT_PASSWORD, CLIENT_USERNAME = TempHelper.param.CLIENT_USERNAME };
        }
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult Payment()
        {
            return View();
        }

        #region GET
        [HttpGet]
        public ActionResult GetInstallmentPlanSingle(string cardNumber, int? productID = 0)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cardNumber) || cardNumber.Length < 6)
                    return Json(new { Status = PaymentStatus.Error, Message = "Kart numarası hatalı veya eksik." });

                cardNumber = cardNumber.Replace(" ", "");

                // *** KARTA GÖRE SANAL POS BİLGİSİNİ ALIYORUZ. ***  BEGIN //
                ST_Genel_Sonuc result = pos.BIN_SanalPos(sec, cardNumber);
                int SanalPOS_ID = Convert.ToInt32(result.DT_Bilgi.Rows[0]["SanalPOS_ID"]);
                // *** KARTA GÖRE SANAL POS BİLGİSİNİ ALIYORUZ. *** END //
                
                // *** KOMİSYON ORANLARI ALINIYOR. *** BEGIN //
                ST_Genel_Sonuc rateResult = pos.TP_Ozel_Oran_SK_Liste(sec, TempHelper.param.GUID);
                DataTable rates = rateResult.DT_Bilgi;
                // *** KOMİSYON ORANLARI ALINIYOR. *** END //

                // *** TAKSİT BİLGİLERİNİ DÖNDÜRMEK İÇİN DATATABLE VE KOLONLAR OLUŞTURULUR. ***  BEGIN //
                DataTable installmentTable = new DataTable();
                #region DataTable Columns
                installmentTable.Columns.Add("InstallmentCr");
                installmentTable.Columns.Add("InstallmentNumber");
                installmentTable.Columns.Add("InstallmentRate");
                installmentTable.Columns.Add("InstallmentPrice");
                installmentTable.Columns.Add("InstallmentTotalPrice");
                installmentTable.Columns.Add("CreditCardBank");
                installmentTable.Columns.Add("CreditCardBankImage");
                installmentTable.Columns.Add("SanalPOS_ID");
                #endregion
                // *** TAKSİT BİLGİLERİNİ DÖNDÜRMEK İÇİN DATATABLE VE KOLONLAR OLUŞTURULUR. ***  END //

                // GİRDİĞİMİZ KREDİ KARTI BİLGİSİNE GÖRE BİZİ İLGİLENDİREN TAKSİT SEÇENEĞİNİ GETİRİYORUZ. ESASINDA TEK DATAROW'DA DÖNÜYORUZ. //
                foreach (DataRow item in rates.AsEnumerable().Where(x => x.ItemArray[2].ToString() == SanalPOS_ID.ToString()).ToList()) // ItemArray[2] ===> SANALPOSID //
                {
                    //TEK ÇEKİMDEKİ KOMİSYON KONTROL EDİLİYOR. VARSA EKLENİYOR.
                    if (Convert.ToDecimal(item["MO_01"]) > 0)
                    {
                        installmentTable.Rows.Add(Helper.Encrypt("1"), 1, item["MO_01"], Convert.ToDecimal((productPrice + ((productPrice * Convert.ToDecimal(item["MO_01"])) / 100))).ToString("f"), Convert.ToDecimal((productPrice + ((productPrice * Convert.ToDecimal(item["MO_01"])) / 100))).ToString("f"), item["Kredi_Karti_Banka"], item["Kredi_Karti_Banka_Gorsel"], Convert.ToInt32(item["SanalPOS_ID"]));
                    }
                    else
                    {
                        installmentTable.Rows.Add(Helper.Encrypt("1"), 1, item["MO_01"], Convert.ToDecimal(productPrice).ToString("f"), Convert.ToDecimal(productPrice).ToString("f"), item["Kredi_Karti_Banka"], item["Kredi_Karti_Banka_Gorsel"], Convert.ToInt32(item["SanalPOS_ID"]));
                    }


                    //  12 TAKSİT SEÇENEĞİ TEK TEK KONTROL EDİLİYOR.  i < 10 KONTROLÜNÜN SEBEBİ KOMİSYONLA ALAKALI BİLGİYİ ALDIĞIMIZ DATAROW'UN (item["MO_01"]) YANİ BAŞINDA 0 (SIFIR) LI ŞEKİLDE YAZMASI. //
                    for (int i = 2; i <= 12; i++)
                    {
                        if (i < 10)
                        {
                            //  TAKSİT SEÇENEĞİ MÜMKÜNSE KOMİSYON ORANI 0 'DAN BÜYÜK ÇIKIYOR. AKSİ HALDE -2 YADA -1 OLARAK GELİYOR. //
                            if (Convert.ToDecimal(item["MO_0" + i]) > 0)
                            {
                                installmentTable.Rows.Add(Helper.Encrypt(i.ToString()), i, Convert.ToDecimal(item["MO_0" + i]), Convert.ToDecimal(Convert.ToDecimal((productPrice) + (((productPrice) * Convert.ToDecimal(item["MO_0" + i])) / 100)) / i).ToString("f"), Convert.ToDecimal((productPrice) + (((productPrice) * Convert.ToDecimal(item["MO_0" + i])) / 100)).ToString("f"), item["Kredi_Karti_Banka"], item["Kredi_Karti_Banka_Gorsel"], Convert.ToInt32(item["SanalPOS_ID"]));
                            }
                            else if (Convert.ToDecimal(item["MO_0" + i]) == 0)
                            {
                                installmentTable.Rows.Add(Helper.Encrypt(i.ToString()), i, Convert.ToDecimal(item["MO_0" + i]), Convert.ToDecimal(Convert.ToDecimal((productPrice) + (((productPrice) * Convert.ToDecimal(item["MO_0" + i])) / 100)) / i).ToString("f"), Convert.ToDecimal(productPrice).ToString("f"), item["Kredi_Karti_Banka"], item["Kredi_Karti_Banka_Gorsel"], Convert.ToInt32(item["SanalPOS_ID"]));
                            }
                        }
                        else
                        {
                            //  TAKSİT SEÇENEĞİ MÜMKÜNSE KOMİSYON ORANI 0 'DAN BÜYÜK ÇIKIYOR. AKSİ HALDE -2 YADA -1 OLARAK GELİYOR. //
                            if (Convert.ToDecimal(item["MO_" + i]) > 0)
                            {
                                installmentTable.Rows.Add(Helper.Encrypt(i.ToString()), i, Convert.ToDecimal(item["MO_" + i]), Convert.ToDecimal(Convert.ToDecimal((productPrice) + (((productPrice) * Convert.ToDecimal(item["MO_" + i])) / 100)) / i).ToString("f"), Convert.ToDecimal((productPrice) + (((productPrice) * Convert.ToDecimal(item["MO_" + i])) / 100)).ToString("f"), item["Kredi_Karti_Banka"], item["Kredi_Karti_Banka_Gorsel"], Convert.ToInt32(item["SanalPOS_ID"]));
                            }
                            else if (Convert.ToDecimal(item["MO_" + i]) == 0)
                            {
                                installmentTable.Rows.Add(Helper.Encrypt(i.ToString()), i, Convert.ToDecimal(item["MO_" + i]), Convert.ToDecimal(Convert.ToDecimal((productPrice) + (((productPrice) * Convert.ToDecimal(item["MO_" + i])) / 100)) / i).ToString("f"), Convert.ToDecimal(productPrice).ToString("f"), item["Kredi_Karti_Banka"], item["Kredi_Karti_Banka_Gorsel"], Convert.ToInt32(item["SanalPOS_ID"]));
                            }
                        }
                    }
                }

                var list = JsonConvert.SerializeObject(installmentTable, Formatting.None, new JsonSerializerSettings()
                { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
                return Content(list, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { Data = ex.Message, ErrorDetail = ex.StackTrace, ErrorSource = ex.Source, Status = PaymentStatus.Fail }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public ActionResult GetInstallmentPlans(int? productID = 0)
        {
            ViewBag.Price = productPrice;
            ViewBag.InstallmentArray = installmentArray;
            ST_Genel_Sonuc dt = pos.TP_Ozel_Oran_SK_Liste(sec, TempHelper.param.GUID); // TÜM ORANLARI LİSTEDE GÖSTERMEK İÇİN ÇEKİYORUZ.
            return View(dt.DT_Bilgi);
        }
        #endregion

        #region POST
        [HttpPost]
        public ActionResult Payment(PaymentItem pay)
        {
            #region Validation
            if (string.IsNullOrWhiteSpace(pay.CardNumber) || pay.CardNumber.Replace(" ", "").Length > 16 || pay.CardNumber.Replace(" ", "").Length < 16)
            {
                return Json(new { Status = PaymentStatus.Error, Message = "Kart numarası hatalı veya eksik." });
            }
            if (string.IsNullOrWhiteSpace(pay.CardMonthYear) || pay.CardMonthYear.Contains("/") != true || pay.CardMonthYear.Replace("/", "").Length > 4 || pay.CardMonthYear.Replace("/", "").Length < 4)
            {
                return Json(new { Status = PaymentStatus.Error, Message = "Son kullanma tarihi hatalı." });
            }
            if (string.IsNullOrWhiteSpace(pay.CardOwner))
            {
                return Json(new { Status = PaymentStatus.Error, Message = "Kart sahibi girilmemiş." });
            }
            if (string.IsNullOrWhiteSpace(pay.SecurityCode) || pay.SecurityCode.Length > 3 || pay.SecurityCode.Length < 3)
            {
                return Json(new { Status = PaymentStatus.Error, Message = "Güvenlik kodu eksik ya da hatalı." });
            }
            if (string.IsNullOrWhiteSpace(pay.Installment))
            {
                return Json(new { Status = PaymentStatus.Error, Message = "Taksit seçeneği belirtilmemiş." });
            }
            #endregion

            try
            {
                pay.CardNumber = pay.CardNumber.Replace(" ", ""); // KART NUMARASINDAKİ BOŞLUKLAR ATILIYOR. //

                ST_Genel_Sonuc dt = pos.TP_Ozel_Oran_SK_Liste(sec, TempHelper.param.GUID); // KOMİSYON ORANLARINI BU METOT İLE ÇEKİYORUZ. //
                DataTable ratios = dt.DT_Bilgi;

                ST_Genel_Sonuc resultPos = pos.BIN_SanalPos(sec, pay.CardNumber.Substring(0, 6)); // İLGİLİ KART NUMARASINA AİT BIN KODUNA VE KARTLA ILGILI DIGER BILGILERE ERISEBILIRSINIZ. //
                int SanalPOS_ID = Convert.ToInt32(resultPos.DT_Bilgi.Rows[0]["SanalPOS_ID"]);

                decimal totalPrice = 0;
                string installmentNumberStr = Helper.Decrypt(pay.Installment); // ÖN TARAFTA TAKSİT SAYISINI ŞİFRELİ OLARAK TUTTUĞUMUZ İÇİN ARKADA ÇÖZÜYORUZ. //
                int installmentNum = int.Parse(installmentNumberStr);

                // BURADA AMAÇ ÖDEMEYİ GERÇEKLEŞTİREN MÜŞTERİ ESKAZA SİSTEM HATASIYLA OLMAYAN BİR TAKSİT SEÇENEĞİNİ ARKAYA GÖNDERMEYİ BAŞARDIYSA KONTROL ETMEK, GERÇEKTEN O KART İÇİN TAKSİT SEÇENEĞİ GEÇERLİ Mİ DİYE. AYRICA TOPLAM TUTAR BURADA YENİDEN HESAPLANIYOR. //
                foreach (DataRow item in ratios.AsEnumerable().Where(x => x.ItemArray[2].ToString() == SanalPOS_ID.ToString()).ToList())
                {
                    if (installmentNum >= 10)
                    {
                        if (Convert.ToInt32(item["MO_" + installmentNum]) < 0)
                        {
                            return Json(new { Status = PaymentStatus.Fail, Message = "Taksit seçeneği bulunamadı." });
                        }
                        else if (Convert.ToInt32(item["MO_" + installmentNum]) == 0)
                        {
                            totalPrice = productPrice;
                        }
                        else
                        {
                            totalPrice = productPrice + ((productPrice * ((decimal)item["MO_" + installmentNum])) / 100);
                        }
                    }
                    else
                    {
                        if (Convert.ToInt32(item["MO_0" + installmentNum]) <= 0)
                        {
                            return Json(new { Status = PaymentStatus.Fail, Message = "Taksit seçeneği bulunamadı." });
                        }
                        else if (Convert.ToInt32(item["MO_0" + installmentNum]) == 0)
                        {
                            totalPrice = productPrice;
                        }
                        else
                        {
                            totalPrice = productPrice + ((productPrice * ((decimal)item["MO_0" + installmentNum])) / 100);
                        }
                    }
                    pay.Ozel_Oran_SK_ID = Convert.ToInt32(item["Ozel_Oran_SK_ID"]);
                }

                string productPriceStr = productPrice.ToString("f"); // TP_Islem_Odeme METODUNDA TAHSILAT BEDELLERININ FORMATI 1240.50 ŞEKLİNDE OLMALI. -1240 TL 50 KURUŞ- 
                string totalPriceStr = totalPrice.ToString("f");// TP_Islem_Odeme METODUNDA TAHSILAT BEDELLERININ FORMATI 1240.50 ŞEKLİNDE OLMALI. -1240 TL 50 KURUŞ- 

                #region TP_Ozel_Oran_SK_Guncelle -- ORAN GÜNCELLEME YAPILACAK MI?
                specRatio = false; //TRUE = ÖZEL KOMİSYON ORANI.  FALSE = STANDART KOMİSYON ORANI //
                string specRatioResult = "";
                if (specRatio)
                {
                    #region TAKSİT KOMİSYON ORAN GÜNCELLEME
                    ST_Sonuc st = pos.TP_Ozel_Oran_SK_Guncelle(
                                sec, TempHelper.param.GUID,
                                pay.Ozel_Oran_SK_ID.ToString(),
                                installmentRate[0],
                                installmentRate[1],
                                installmentRate[2],
                                installmentRate[3],
                                installmentRate[4],
                                installmentRate[5],
                                installmentRate[6],
                                installmentRate[7],
                                installmentRate[8],
                                installmentRate[9],
                                installmentRate[10],
                                installmentRate[11]);
                    specRatioResult = st.Sonuc;

                    dt = pos.TP_Ozel_Oran_SK_Liste(sec, TempHelper.param.GUID);
                    ratios = dt.DT_Bilgi;
                    foreach (DataRow item in ratios.AsEnumerable().Where(x => x.ItemArray[2].ToString() == SanalPOS_ID.ToString()).ToList())
                    {
                        if (installmentNum >= 10)
                        {
                            if (Convert.ToInt32(item["MO_" + installmentNum]) < 0)
                            {
                                return Json(new { Status = PaymentStatus.Fail, Message = "Taksit seçeneği bulunamadı." });
                            }
                            else if (Convert.ToInt32(item["MO_" + installmentNum]) == 0)
                            {
                                totalPrice = productPrice;
                            }
                            else
                            {
                                totalPrice = productPrice + ((productPrice * ((decimal)item["MO_" + installmentNum])) / 100);
                            }
                        }
                        else
                        {
                            if (Convert.ToInt32(item["MO_0" + installmentNum]) <= 0)
                            {
                                return Json(new { Status = PaymentStatus.Fail, Message = "Taksit seçeneği bulunamadı." });
                            }
                            else if (Convert.ToInt32(item["MO_0" + installmentNum]) == 0)
                            {
                                totalPrice = productPrice;
                            }
                            else
                            {
                                totalPrice = productPrice + ((productPrice * ((decimal)item["MO_0" + installmentNum])) / 100);
                            }
                        }
                        pay.Ozel_Oran_SK_ID = Convert.ToInt32(item["Ozel_Oran_SK_ID"]);
                    }
                    #endregion
                }
                #endregion

                #region ŞİFRELEME VE ÖDEME İŞLEMİ
                //GÖNDERİLECEK DATALAR BURADA ŞİFRELENİYOR - HANGİ VERİLERİ YAZMAMIZ GEREKTİĞİ DÖKÜMANTASYONDA VAR. //
                string encrypted = pos.SHA2B64(
                    TempHelper.param.CLIENT_CODE +
                    TempHelper.param.GUID +
                    SanalPOS_ID +
                    installmentNumberStr + // TAKSİT SEÇENEĞİNİ STRING OLARAK VERİYORUZ. BURADA ÖNEMLİ HUSUS FORMAT 1240.50 ŞEKLİNDE OLMALI. VİRGÜL KULLANILMAMALIDIR.  //
                    productPriceStr +// ÖDEME TUTARINI STRING OLARAK VERİYORUZ. //
                    totalPriceStr +// KOMİSYON DAHİL ÖDEME TUTARINI STRING OLARAK VERİYORUZ. //
                    "YeniSiparis" +
                    "http://localhost:58125/Home/PaymentResult?Status=Error" +
                    "http://localhost:58125/Home/PaymentResult?Status=Success"
                    );

                ST_TP_Islem_Odeme paymentResult = pos.TP_Islem_Odeme(
                    sec, //  CONSTRUCTOR DA OLUŞTURDUĞUMUZ SEC NESNESİNİ VERİYORUZ. //
                    SanalPOS_ID, // KARTA AİT SANALPOS ID VERİLECEK. //
                    TempHelper.param.GUID, 
                    pay.CardOwner, // KART SAHİBİ YAZILMALI.
                    pay.CardNumber, // KART NUMARASINI BOŞLUKLARI SİLDİRİP YAZIYORUZ. //
                    pay.CardMonthYear.Split('/')[0], // AYI 2 HANELİ ŞEKİLDE YAZIYORUZ. TEK DE OLABİLİR. 1-2-3 GİBİ.
                    "20" + pay.CardMonthYear.Split('/')[1], //YILI 4 HANELİ OLACAK ŞEKİLDE YAZIYORUZ. //
                    pay.SecurityCode,
                    "05551112233",
                    "http://localhost:58125/Home/PaymentResult?Status=Error", // BURAYA EĞER ÖDEME İŞLEMİ HATALIYSA PARAMPOS'UN HANGİ ADRESE POST İŞLEMİ YAPACAĞINI BELİRTİYORUZ  - BEN 2 DURUMU AYNI METOTTA QUERYSTRING ILE AYIRDIM. //
                    "http://localhost:58125/Home/PaymentResult?Status=Success", // BURAYA EĞER ÖDEME İŞLEMİ BAŞARILIYLA PARAMPOS'UN HANGİ ADRESE POST İŞLEMİ YAPACAĞINI BELİRTİYORUZ //
                    "YeniSiparis", // SİSTEMİNİZDE BU ÖDEMENİN YAPILMASI İÇİN OLUŞTURDUĞUNUZ SİPARİŞ ID'Yİ BURAYA VEREBİLİRSİNİZ. //
                    "Ödeme",  // ÖDEME AÇIKLAMASI - "" İLE BOŞ GEÇİLEBİLİR. YUKARIDAKİ SHA2B64() METOTUNA VERİLEN AÇIKLAMA İLE AYNI OLMALIDIR. (HEPSİ İÇİN GEÇERLİ.) //
                    installmentNum, // TAKSİT SAYISINI VERİYORUZ. //
                    productPriceStr, // ÜRÜNÜNÜZÜN YA DA TOPLAM SİPARİŞİN ÜCRETİNİ BURAYA YAZIYORSUNUZ. KOMİSYON DAHİL OLMAYAN ÜCRETTİR. FORMAT 1240.50 ŞEKLİNDE OLMALI. VİRGÜL KULLANILMAMALIDIR. //
                    totalPriceStr,  // ÜRÜNÜNÜZÜN YA DA TOPLAM SİPARİŞİN KOMİSYON DAHİL TOPLAMI. FORMÜLÜ PARAMPOS APİ DOKUMANTASYONUNDA Toplam_Tutar = Islem_Tutar + ((Islem_Tutar x Komisyon Oran) / 100) OLARAK VERİLMİŞTİR. //
                    encrypted, // YUKARIDA ŞİFRELENEN NESNE //
                    "",
                    GeneralHelper.GetIPAddress(), // İŞLEMLERİN YAPILDIĞI SUNUCUNUN ADRESİ //
                    "http://localhost:58125/Home/Payment", // İŞLEMLERİN YAPILDIĞI ADRES - ÖDEME SAYFASI //
                    "", "", "", "", "" // BU PARAMETRELERİ PaymentResult METOTU İÇİN KULLANABİLİRİZ FAKAT İHTİYACIM YOK ŞU AN. BURAYA VERİLEN PARAMETRELERİ PaymentResult METOTUNDA TURKPOS_RETVAL_Ext_Data PARAMETRESİNDEN ALABİLİYORSUNUZ.  DETAYLI BİLGİ İÇİN PARAMPOS DOKÜMANTASYONUNA BAKABİLİRSİNİZ. http://dev.param.com.tr/tr/api/odeme SAYFANIN EN ALTINDAKİ PARAMETRE. //
                    );
                #endregion

                if (Convert.ToInt32(paymentResult.Sonuc) > 0)
                {
                    // İŞLEM BAŞARILIYSA SONUC "1" OLARAK DÖNÜYOR. NESNE OLARAK UCD_URL'İ ALIYORUZ. 3D İÇİN YÖNLENDİRMEMİZ GEREKEN SAYFANIN LİNKİ BU. //
                    string ucd_url = paymentResult.UCD_URL; //3D Yönlendirilecek URL
                    return Json(new { Status = PaymentStatus.Success, Message = "Ödeme işlemi için yönlendiriliyorsunuz... Beklemek istemiyorsanız tıklayınız.", URL = ucd_url });
                }
                else
                {
                    // HATA KODLARI İÇİN PARAMPOS DOKÜMANTASYONA BAKABİLİRSİNİZ. BEN BİR KISMINI BURADA YAZDIM. //
                    if (Convert.ToInt32(paymentResult.Sonuc) == -220)
                    {
                        return Json(new { Status = PaymentStatus.Error, Message = "Debit Kart ile taksitli işlem yapılamaz." });
                    }
                    else if (Convert.ToInt32(paymentResult.Sonuc) == -121)
                    {
                        return Json(new { Status = PaymentStatus.Error, Message = "Kredi Kartı sahibi bilgisini eksiksiz giriniz." });
                    }
                    else if (Convert.ToInt32(paymentResult.Sonuc) == -108)
                    {
                        return Json(new { Status = PaymentStatus.Error, Message = "Müşteri GSM no geçersiz." });
                    }
                    else if (Convert.ToInt32(paymentResult.Sonuc) == -110)
                    {
                        return Json(new { Status = PaymentStatus.Error, Message = "Taksit geçersiz." });
                    }
                    else if (Convert.ToInt32(paymentResult.Sonuc) == -113)
                    {
                        return Json(new { Status = PaymentStatus.Error, Message = "Tutar, 0'dan küçük veya eşit olmamalıdır." });
                    }
                    else
                    {
                        return Json(new { Status = PaymentStatus.Error, Message = "Sistemde bir hata meydana geldiği için ödeme işlemi gerçekleştirilemedi. Daha sonra tekrar deneyebilirsiniz. Hata Kodu:" + paymentResult.Sonuc });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { Status = PaymentStatus.Fail, Message = "İşlem yürütülürken bir hata meydana geldi." });
            }
        }
        [HttpPost]
        public ActionResult PaymentResult(string TURKPOS_RETVAL_Sonuc, string TURKPOS_RETVAL_Sonuc_Str, string TURKPOS_RETVAL_GUID, string TURKPOS_RETVAL_Islem_Tarih, string TURKPOS_RETVAL_Dekont_ID, string TURKPOS_RETVAL_Tahsilat_Tutari, string TURKPOS_RETVAL_Odeme_Tutari, string TURKPOS_RETVAL_Siparis_ID, string TURKPOS_RETVAL_Islem_ID, string TURKPOS_RETVAL_Ext_Data)
        {
            try
            {
                // BU METOTTA YUKARIDA YER VERİLEN PARAMETRELERE, API DOK. SAYFASINDA ULAŞABİLİRSİNİZ. http://dev.param.com.tr/tr/api/odeme SAYFANIN EN ALTI. //
                // ÖDEME İŞLEMİNİN DURUMUNU BU METOT İLE DE SORGULAYABİLİYORUZ. //
                ST_Genel_Sonuc rs = pos.TP_Islem_Sorgulama(sec, TempHelper.param.GUID, TURKPOS_RETVAL_Dekont_ID, TURKPOS_RETVAL_Siparis_ID, TURKPOS_RETVAL_Islem_ID);

                //3D SONRASI YÖNLENDİRİLECEK URL'İ BİZ VERMİŞTİK PARAMETRE OLARAK. URL AYNI QUERYSTRING'TEKİ STATUS BİLGİSİ İLE AYIRDIK.
                string result = Request.QueryString["Status"];
                if (result == "Success" && Convert.ToInt32(TURKPOS_RETVAL_Dekont_ID) > 0)
                {
                    return Redirect("/Home/Index?Status=Success");
                }
                else
                {
                    if (TURKPOS_RETVAL_Sonuc_Str == "Limit Yetersiz") return Redirect("/Home/Index?Status=NoLimit");
                    else return Redirect("/Home/Index?Status=PayError");

                }
            }
            catch (Exception)
            {
                return Redirect("/Home/Index?Status=ErrorException");
            }
        }
        #endregion
    }
}