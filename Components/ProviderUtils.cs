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

namespace OS_PayBox
{
    public class ProviderUtils
    {
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
            rPost.Add("PBX_RETOUR", info.GetXmlProperty("genxml/textbox/pbxretour"));
            rPost.Add("PBX_EFFECTUE", pbxeffectue);
            rPost.Add("PBX_REFUSE", pbxrefuse);
            rPost.Add("PBX_ANNULE", pbxrefuse);
            rPost.Add("PBX_REPONDRE_A", Utils.ToAbsoluteUrl("/DesktopModules/NBright/NBrightPayBox/notify.ashx"));
            rPost.Add("PBX_HASH", "SHA512");
            rPost.Add("PBX_TIME", DateTime.UtcNow.ToString("o"));

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

    }
}
