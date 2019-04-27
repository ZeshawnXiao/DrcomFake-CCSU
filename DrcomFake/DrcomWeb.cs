using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DrcomFake
{
    class DrcomWeb
    {
        private static string GetHtml(string url)
        {
            string strHTML = "";
            WebClient myWebClient = new WebClient();
            Stream myStream = myWebClient.OpenRead(url);
            StreamReader sr = new StreamReader(myStream, Encoding.GetEncoding("utf-8"));
            strHTML = sr.ReadToEnd();
            myStream.Close();
            return strHTML;
        }

        public static List<string> GetLoginInfo()
        {
            List<string> l = new List<string>();
            string a = GetHtml("http://10.1.1.254");
            int b = a.IndexOf("time=");
            string t = a.Substring(b, 17);
            t = t.Replace("time=", "");
            t = t.Replace("'", "");
            t = t.Trim(' ');
            l.Add(t);
            int i = a.IndexOf("flow=");
            a = a.Substring(i, 17);
            a = a.Replace("flow=", "");
            a = a.Replace("'", "");
            l.Add(a);
            return l;
        }

    }
}
