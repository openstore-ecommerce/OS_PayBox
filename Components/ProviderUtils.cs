using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using DotNetNuke.Common;
using DotNetNuke.Entities.Portals;
using NBrightCore.common;
using NBrightDNN;
using Nevoweb.DNN.NBrightBuy.Components;
using DotNetNuke.Common.Utilities;
using NBrightCore.images;
using System.Globalization;

namespace OS_PayBox
{
    public class ProviderUtils
    {
        private static Dictionary<string, string> _countryCode;
        public static NBrightInfo GetProviderSettings()
        {
            var objCtrl = new NBrightBuyController();
            var info = objCtrl.GetPluginSinglePageData("OS_PayBoxpayment", "OS_PayBoxPAYMENT", Utils.GetCurrentCulture());
            return info;
        }


        public static String GetBankRemotePost(OrderData orderData)
        {
            var rPost = new RemotePost();

            var objCtrl = new NBrightBuyController();
            var info = objCtrl.GetPluginSinglePageData("OS_PayBoxpayment", "OS_PayBoxPAYMENT", orderData.Lang);

            var param = new string[3];
            param[0] = "orderid=" + orderData.PurchaseInfo.ItemID.ToString("");
            param[1] = "status=1";
            var pbxeffectue = Globals.NavigateURL(StoreSettings.Current.PaymentTabId, "", param);
            param[0] = "orderid=" + orderData.PurchaseInfo.ItemID.ToString("");
            param[1] = "status=0";
            var pbxrefuse = Globals.NavigateURL(StoreSettings.Current.PaymentTabId, "", param);
            var appliedtotal = orderData.PurchaseInfo.GetXmlPropertyDouble("genxml/appliedtotal").ToString("0.00").Replace(",", "").Replace(".", ""); ;
            var postUrl = info.GetXmlProperty("genxml/textbox/mainurl");

            WebRequest request = WebRequest.Create("https://tpeweb.paybox.com/load.html");
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            if (response == null || response.StatusCode != HttpStatusCode.OK) postUrl = info.GetXmlProperty("genxml/textbox/backupurl");

            if (info.GetXmlPropertyBool("genxml/checkbox/preproduction"))
            {
                postUrl = info.GetXmlProperty("genxml/textbox/preprodurl");
            }

            rPost.Url = postUrl;

            rPost.Add("PBX_SITE", info.GetXmlProperty("genxml/textbox/pbxsite"));
            rPost.Add("PBX_RANG", info.GetXmlProperty("genxml/textbox/pbxrang"));
            rPost.Add("PBX_DEVISE", info.GetXmlProperty("genxml/textbox/pbxdevise"));
            rPost.Add("PBX_TOTAL", appliedtotal);
            rPost.Add("PBX_IDENTIFIANT", info.GetXmlProperty("genxml/textbox/pbxidentifiant"));
            rPost.Add("PBX_CMD", orderData.PurchaseInfo.ItemID.ToString(""));
            rPost.Add("PBX_PORTEUR", orderData.GetClientEmail());
            rPost.Add("PBX_EFFECTUE", pbxeffectue);
            rPost.Add("PBX_REFUSE", pbxrefuse);
            rPost.Add("PBX_ANNULE", pbxrefuse);
            rPost.Add("PBX_REPONDRE_A", Utils.ToAbsoluteUrl("/DesktopModules/NBright/OS_PayBox/notify.ashx"));
            rPost.Add("PBX_HASH", "SHA512");
            rPost.Add("PBX_TIME", DateTime.UtcNow.ToString("o"));

            if (info.GetXmlPropertyBool("genxml/checkbox/pbxautoseule"))
            {
                rPost.Add("PBX_AUTOSEULE", "O");
                rPost.Add("PBX_RETOUR", info.GetXmlProperty("genxml/textbox/pbxretour").TrimEnd(';') + ";call:T;trans:S");
            }
            else
            {
                rPost.Add("PBX_RETOUR", info.GetXmlProperty("genxml/textbox/pbxretour"));
            }

            //REQUIRED FOR 3DS

            rPost.Add("PBX_SHOPPINGCART", "<?xml version='1.0' encoding='utf-8'?><shoppingcart><total><totalQuantity>" + orderData.GetInfo().GetXmlPropertyInt("genxml/totalqty") + "</totalQuantity></total></shoppingcart>");

            PopulateCountryCode();
            var billAddrInfo = orderData.GetBillingAddress();
            var countryCode = billAddrInfo.GetXmlProperty("genxml/dropdownlist/country");
            var cCode = "250";
            if (_countryCode.ContainsKey(countryCode)) cCode = _countryCode[countryCode];

            var xmlBilling = "<?xml version='1.0' encoding='utf-8'?><Billing><Address><FirstName>" + billAddrInfo.GetXmlProperty("genxml/textbox/firstname") + "</FirstName><LastName>" + billAddrInfo.GetXmlProperty("genxml/textbox/lastname") + "</LastName><Address1>" + billAddrInfo.GetXmlProperty("genxml/textbox/unit") + " " + billAddrInfo.GetXmlProperty("genxml/textbox/street") + "</Address1><ZipCode>" + billAddrInfo.GetXmlProperty("genxml/textbox/postalcode") + "</ZipCode><City>" + billAddrInfo.GetXmlProperty("genxml/textbox/city") + "</City><CountryCode>" + cCode + "</CountryCode></Address></Billing>";
            xmlBilling = StripAccents(xmlBilling);
            rPost.Add("PBX_BILLING", xmlBilling);

            // Calc HMAC
            rPost.Add("PBX_HMAC", rPost.GetHmac(info.GetXmlProperty("genxml/textbox/hmackey")).ToUpper());


            //Build the re-direct html 
            var rtnStr = "";
            rtnStr = rPost.GetPostHtml("/DesktopModules/NBright/OS_PayBox/Themes/config/img/" + info.GetXmlProperty("genxml/dropdownlist/imagelogo"));

            if (info.GetXmlPropertyBool("genxml/checkbox/debugmode"))
            {
                File.WriteAllText(PortalSettings.Current.HomeDirectoryMapPath + "\\debug_OS_PayBox.html", rtnStr);
            }
            return rtnStr;
        }
        private static string StripAccents(string s)
        {
            var sb = new StringBuilder();
            foreach (char c in s.Normalize(NormalizationForm.FormKD))
                switch (CharUnicodeInfo.GetUnicodeCategory(c))
                {
                    case UnicodeCategory.NonSpacingMark:
                    case UnicodeCategory.SpacingCombiningMark:
                    case UnicodeCategory.EnclosingMark:
                        break;

                    default:
                        sb.Append(c);
                        break;
                }
            return sb.ToString();
        }

        private static void PopulateCountryCode()
        {
            _countryCode = new Dictionary<string, string>();
            _countryCode.Add("AF", "004");
            _countryCode.Add("AL", "008");
            _countryCode.Add("DZ", "012");
            _countryCode.Add("AS", "016");
            _countryCode.Add("AD", "020");
            _countryCode.Add("AO", "024");
            _countryCode.Add("AI", "660");
            _countryCode.Add("AQ", "010");
            _countryCode.Add("AG", "028");
            _countryCode.Add("AR", "032");
            _countryCode.Add("AM", "051");
            _countryCode.Add("AW", "533");
            _countryCode.Add("AU", "036");
            _countryCode.Add("AT", "040");
            _countryCode.Add("AZ", "031");
            _countryCode.Add("BS", "044");
            _countryCode.Add("BH", "048");
            _countryCode.Add("BD", "050");
            _countryCode.Add("BB", "052");
            _countryCode.Add("BY", "112");
            _countryCode.Add("BE", "056");
            _countryCode.Add("BZ", "084");
            _countryCode.Add("BJ", "204");
            _countryCode.Add("BM", "060");
            _countryCode.Add("BT", "064");
            _countryCode.Add("BO", "068");
            _countryCode.Add("BQ", "535");
            _countryCode.Add("BA", "070");
            _countryCode.Add("BW", "072");
            _countryCode.Add("BV", "074");
            _countryCode.Add("BR", "076");
            _countryCode.Add("IO", "086");
            _countryCode.Add("BN", "096");
            _countryCode.Add("BG", "100");
            _countryCode.Add("BF", "854");
            _countryCode.Add("BI", "108");
            _countryCode.Add("CV", "132");
            _countryCode.Add("KH", "116");
            _countryCode.Add("CM", "120");
            _countryCode.Add("CA", "124");
            _countryCode.Add("KY", "136");
            _countryCode.Add("CF", "140");
            _countryCode.Add("TD", "148");
            _countryCode.Add("CL", "152");
            _countryCode.Add("CN", "156");
            _countryCode.Add("CX", "162");
            _countryCode.Add("CC", "166");
            _countryCode.Add("CO", "170");
            _countryCode.Add("KM", "174");
            _countryCode.Add("CD", "180");
            _countryCode.Add("CG", "178");
            _countryCode.Add("CK", "184");
            _countryCode.Add("CR", "188");
            _countryCode.Add("HR", "191");
            _countryCode.Add("CU", "192");
            _countryCode.Add("CW", "531");
            _countryCode.Add("CY", "196");
            _countryCode.Add("CZ", "203");
            _countryCode.Add("CI", "384");
            _countryCode.Add("DK", "208");
            _countryCode.Add("DJ", "262");
            _countryCode.Add("DM", "212");
            _countryCode.Add("DO", "214");
            _countryCode.Add("EC", "218");
            _countryCode.Add("EG", "818");
            _countryCode.Add("SV", "222");
            _countryCode.Add("GQ", "226");
            _countryCode.Add("ER", "232");
            _countryCode.Add("EE", "233");
            _countryCode.Add("SZ", "748");
            _countryCode.Add("ET", "231");
            _countryCode.Add("FK", "238");
            _countryCode.Add("FO", "234");
            _countryCode.Add("FJ", "242");
            _countryCode.Add("FI", "246");
            _countryCode.Add("FR", "250");
            _countryCode.Add("GF", "254");
            _countryCode.Add("PF", "258");
            _countryCode.Add("TF", "260");
            _countryCode.Add("GA", "266");
            _countryCode.Add("GM", "270");
            _countryCode.Add("GE", "268");
            _countryCode.Add("DE", "276");
            _countryCode.Add("GH", "288");
            _countryCode.Add("GI", "292");
            _countryCode.Add("GR", "300");
            _countryCode.Add("GL", "304");
            _countryCode.Add("GD", "308");
            _countryCode.Add("GP", "312");
            _countryCode.Add("GU", "316");
            _countryCode.Add("GT", "320");
            _countryCode.Add("GG", "831");
            _countryCode.Add("GN", "324");
            _countryCode.Add("GW", "624");
            _countryCode.Add("GY", "328");
            _countryCode.Add("HT", "332");
            _countryCode.Add("HM", "334");
            _countryCode.Add("VA", "336");
            _countryCode.Add("HN", "340");
            _countryCode.Add("HK", "344");
            _countryCode.Add("HU", "348");
            _countryCode.Add("IS", "352");
            _countryCode.Add("IN", "356");
            _countryCode.Add("ID", "360");
            _countryCode.Add("IR", "364");
            _countryCode.Add("IQ", "368");
            _countryCode.Add("IE", "372");
            _countryCode.Add("IM", "833");
            _countryCode.Add("IL", "376");
            _countryCode.Add("IT", "380");
            _countryCode.Add("JM", "388");
            _countryCode.Add("JP", "392");
            _countryCode.Add("JE", "832");
            _countryCode.Add("JO", "400");
            _countryCode.Add("KZ", "398");
            _countryCode.Add("KE", "404");
            _countryCode.Add("KI", "296");
            _countryCode.Add("KP", "408");
            _countryCode.Add("KR", "410");
            _countryCode.Add("KW", "414");
            _countryCode.Add("KG", "417");
            _countryCode.Add("LA", "418");
            _countryCode.Add("LV", "428");
            _countryCode.Add("LB", "422");
            _countryCode.Add("LS", "426");
            _countryCode.Add("LR", "430");
            _countryCode.Add("LY", "434");
            _countryCode.Add("LI", "438");
            _countryCode.Add("LT", "440");
            _countryCode.Add("LU", "442");
            _countryCode.Add("MO", "446");
            _countryCode.Add("MG", "450");
            _countryCode.Add("MW", "454");
            _countryCode.Add("MY", "458");
            _countryCode.Add("MV", "462");
            _countryCode.Add("ML", "466");
            _countryCode.Add("MT", "470");
            _countryCode.Add("MH", "584");
            _countryCode.Add("MQ", "474");
            _countryCode.Add("MR", "478");
            _countryCode.Add("MU", "480");
            _countryCode.Add("YT", "175");
            _countryCode.Add("MX", "484");
            _countryCode.Add("FM", "583");
            _countryCode.Add("MD", "498");
            _countryCode.Add("MC", "492");
            _countryCode.Add("MN", "496");
            _countryCode.Add("ME", "499");
            _countryCode.Add("MS", "500");
            _countryCode.Add("MA", "504");
            _countryCode.Add("MZ", "508");
            _countryCode.Add("MM", "104");
            _countryCode.Add("NA", "516");
            _countryCode.Add("NR", "520");
            _countryCode.Add("NP", "524");
            _countryCode.Add("NL", "528");
            _countryCode.Add("NC", "540");
            _countryCode.Add("NZ", "554");
            _countryCode.Add("NI", "558");
            _countryCode.Add("NE", "562");
            _countryCode.Add("NG", "566");
            _countryCode.Add("NU", "570");
            _countryCode.Add("NF", "574");
            _countryCode.Add("MP", "580");
            _countryCode.Add("NO", "578");
            _countryCode.Add("OM", "512");
            _countryCode.Add("PK", "586");
            _countryCode.Add("PW", "585");
            _countryCode.Add("PS", "275");
            _countryCode.Add("PA", "591");
            _countryCode.Add("PG", "598");
            _countryCode.Add("PY", "600");
            _countryCode.Add("PE", "604");
            _countryCode.Add("PH", "608");
            _countryCode.Add("PN", "612");
            _countryCode.Add("PL", "616");
            _countryCode.Add("PT", "620");
            _countryCode.Add("PR", "630");
            _countryCode.Add("QA", "634");
            _countryCode.Add("MK", "807");
            _countryCode.Add("RO", "642");
            _countryCode.Add("RU", "643");
            _countryCode.Add("RW", "646");
            _countryCode.Add("RE", "638");
            _countryCode.Add("BL", "652");
            _countryCode.Add("SH", "654");
            _countryCode.Add("KN", "659");
            _countryCode.Add("LC", "662");
            _countryCode.Add("MF", "663");
            _countryCode.Add("PM", "666");
            _countryCode.Add("VC", "670");
            _countryCode.Add("WS", "882");
            _countryCode.Add("SM", "674");
            _countryCode.Add("ST", "678");
            _countryCode.Add("SA", "682");
            _countryCode.Add("SN", "686");
            _countryCode.Add("RS", "688");
            _countryCode.Add("SC", "690");
            _countryCode.Add("SL", "694");
            _countryCode.Add("SG", "702");
            _countryCode.Add("SX", "534");
            _countryCode.Add("SK", "703");
            _countryCode.Add("SI", "705");
            _countryCode.Add("SB", "090");
            _countryCode.Add("SO", "706");
            _countryCode.Add("ZA", "710");
            _countryCode.Add("GS", "239");
            _countryCode.Add("SS", "728");
            _countryCode.Add("ES", "724");
            _countryCode.Add("LK", "144");
            _countryCode.Add("SD", "729");
            _countryCode.Add("SR", "740");
            _countryCode.Add("SJ", "744");
            _countryCode.Add("SE", "752");
            _countryCode.Add("CH", "756");
            _countryCode.Add("SY", "760");
            _countryCode.Add("TW", "158");
            _countryCode.Add("TJ", "762");
            _countryCode.Add("TZ", "834");
            _countryCode.Add("TH", "764");
            _countryCode.Add("TL", "626");
            _countryCode.Add("TG", "768");
            _countryCode.Add("TK", "772");
            _countryCode.Add("TO", "776");
            _countryCode.Add("TT", "780");
            _countryCode.Add("TN", "788");
            _countryCode.Add("TR", "792");
            _countryCode.Add("TM", "795");
            _countryCode.Add("TC", "796");
            _countryCode.Add("TV", "798");
            _countryCode.Add("UG", "800");
            _countryCode.Add("UA", "804");
            _countryCode.Add("AE", "784");
            _countryCode.Add("GB", "826");
            _countryCode.Add("UM", "581");
            _countryCode.Add("US", "840");
            _countryCode.Add("UY", "858");
            _countryCode.Add("UZ", "860");
            _countryCode.Add("VU", "548");
            _countryCode.Add("VE", "862");
            _countryCode.Add("VN", "704");
            _countryCode.Add("VG", "092");
            _countryCode.Add("VI", "850");
            _countryCode.Add("WF", "876");
            _countryCode.Add("EH", "732");
            _countryCode.Add("YE", "887");
            _countryCode.Add("ZM", "894");
            _countryCode.Add("ZW", "716");
            _countryCode.Add("AX", "248");
        }


    }
}
