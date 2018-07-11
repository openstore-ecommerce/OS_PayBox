using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace OS_PayBox
{

    public class RemotePost
    {
        private System.Collections.Specialized.NameValueCollection Inputs = new System.Collections.Specialized.NameValueCollection();
        public string Url = "";
        public string Method = "post";
        public string FormName = "form";
        public void Add(string name, string value)
        {
            Inputs.Add(name, value);
        }

        public string GetHmac1(string hmacKey)
        {
            var message = "";
            int i = 0;
            for (i = 0; i <= Inputs.Keys.Count - 1; i += 1)
            {
                message += Inputs.Keys[i] + "=" + Inputs[Inputs.Keys[i]] + "&";
            }

            message = message.TrimEnd('&');

            var strhmac = HashHMACHex(hmacKey, message);

            return strhmac;
        }

        public string GetHmac(string hmacKey)
        {
            var message = "";
            int i = 0;
            for (i = 0; i <= Inputs.Keys.Count - 1; i += 1)
            {
                message += Inputs.Keys[i] + "=" + Inputs[Inputs.Keys[i]] + "&";
            }

            message = message.TrimEnd('&');

            // Si la clé est en ASCII, On la transforme en binaire
            Encoding encoding = Encoding.ASCII;
            byte[] keyByte = PackH(hmacKey);

            // On calcule l’empreinte (à renseigner dans le paramètre PBX_HMAC) grâce à la fonction de hash et
            // la clé binaire
            // On envoi via la variable PBX_HASH l'algorithme de hachage qui a été utilisé (SHA512 dans ce cas)
            // Pour afficher la liste des algorithmes disponibles sur votre environnement, décommentez la ligne 
            // suivante

            HMACSHA512 hmacsha512 = new HMACSHA512(keyByte);

            byte[] messageBytes = encoding.GetBytes(message);
            byte[] hashmessage = hmacsha512.ComputeHash(messageBytes);

            string hash_mac = ByteToString(hashmessage);

            return hash_mac;
        }

        private static byte[] PackH(string hex)
        {
            if ((hex.Length % 2) == 1) hex += '0';
            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }

        private static string ByteToString(byte[] buff)
        {
            string sbinary = "";

            for (int i = 0; i < buff.Length; i++)
            {
                sbinary += buff[i].ToString("X2"); // hex format
            }
            return (sbinary);
        }

        private static byte[] HashHMAC(byte[] key, byte[] message)
        {
            var hash = new HMACSHA512(key);
            return hash.ComputeHash(message);
        }

        private static byte[] StringEncode(string text)
        {
            var encoding = new ASCIIEncoding();
            return encoding.GetBytes(text);
        }

        private static string HashEncode(byte[] hash)
        {
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        private static string HashHMACHex(string keyHex, string message)
        {
            byte[] hash = HashHMAC(StringEncode(keyHex), StringEncode(message));
            return HashEncode(hash);
        }


        public string GetPostHtml(string loadingImageUrl)
        {
            string sipsHtml = "";

            sipsHtml = "<html><head>";
            sipsHtml += "</head><body onload=\"document." + FormName + ".submit()\">";
            sipsHtml += "<form name=\"" + FormName + "\" method=\"" + Method + "\" action=\"" + Url + "\">";
            int i = 0;
            for (i = 0; i <= Inputs.Keys.Count - 1; i += 1)
            {
                sipsHtml += "<input type=\"hidden\" name=\"" + Inputs.Keys[i] + "\" value=\"" + Inputs[Inputs.Keys[i]] + "\" />";
            }
            sipsHtml += "</form>";

            sipsHtml += "  <table border=\"0\" cellspacing=\"0\" cellpadding=\"0\" width=\"100%\" height=\"100%\">";
            sipsHtml += "<tr><td width=\"100%\" height=\"100%\" valign=\"middle\" align=\"center\">";
            sipsHtml += "<font style=\"font-family: Trebuchet MS, Verdana, Helvetica;font-size: 14px;letter-spacing: 1px;font-weight: bold;\">";
            sipsHtml += "Processing...";
            sipsHtml += "</font><br /><br /><img src=\"" + loadingImageUrl + "\" />     ";
            sipsHtml += "</td></tr>";
            sipsHtml += "</table>";

            sipsHtml += "</body></html>";

            return sipsHtml;

        }

    }

}
