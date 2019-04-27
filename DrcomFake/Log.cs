using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrcomFake
{
    class Log
    {

        public static void log(string content)
        {
            return;
            try
            {
                
                System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace();
                string s = "[" + DateTime.Now.ToString() + "] [" + st.GetFrame(1).GetMethod().Name + "] : " + content;
                FileStream fs = new FileStream("login_log.log", FileMode.Append);

                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine(s);
                sw.Close();
            }
            catch (Exception)
            {
                return;
            }
        }
    }
}
