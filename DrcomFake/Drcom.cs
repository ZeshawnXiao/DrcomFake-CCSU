using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Net.NetworkInformation;
using System.Management;
using System.Threading;

namespace DrcomFake
{
    class Drcom
    {
        private const string server = "10.1.1.254";
        private const string CONTROLCHECKSTATUS = "\x20";
        private const string ADAPTERNUM = "\x07";
        private const string IPDOG = "\x01";
        private const string PRIMARY_DNS = "218.196.40.9";
        private const string dhcp_server = "218.196.40.8";
        private const string AUTH_VERSION = "\x27\x00";
        private const string KEEP_ALIVE_VERSION = "\xd8\x02";
        private const string host_name = "fuyumi";
        private const string host_os = "Windows 10";
        private static string host_ip;
        private string MAC;
        private string username;
        private string password;
        private string SALT;
        private string AUTH_INFO;

        public delegate void isDialFinish(int status);
        public event isDialFinish idl;

        private Socket s = null;

        public Drcom()
        {
            
            
            
            s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            s.ReceiveBufferSize = 1024;
            
            s.Bind(new IPEndPoint(IPAddress.Any, 61440));
            s.ReceiveTimeout = 30;
        }

        public void SetAuthInfo(string username,string password)
        {
            this.username = username;
            this.password = password;
        }

        private static string GetMD5Sum(string s)
        {
            s = ConvertStringToHex(s);
            string res = "";
            MD5 m = new MD5CryptoServiceProvider();
            byte[] a = m.ComputeHash(strToToHexByte(s));

            for (int i = 0; i < a.Length; i++)
            {
                res += a[i].ToString("x2");
            }
            return ConvertHexToString(res);
        }

        private static string Dump(long n)
        {
            string hex = n.ToString("x");
            if ((hex.Length & 1) == 1)
            {
                hex = "0" + hex;
            }
            return ConvertHexToString(hex);
        }

        private static string mkpkt(string salt, string username, string password, long mac)
        {
            string data = "\x03\x01\x00" + Convert.ToChar((username.Length + 20));

            data += GetMD5Sum("\x03\x01" + salt + password);
            data += username.PadRight(36, '\x00');
            data += CONTROLCHECKSTATUS;
            data += ADAPTERNUM;
            string d4 = data.Substring(4, 6);
            d4 = ConvertStringToHex(d4);
            long a = Convert.ToInt64(d4, 16);
            data += Dump(a ^ mac).PadLeft(6, '\x00');
            data += GetMD5Sum("\x01" + password + salt + "\x00\x00\x00\x00");
            data += "\x01";
            foreach (string h in host_ip.Split('.'))
            {
                data += ConvertHexToString(Convert.ToInt32(h).ToString("x2"));
            }
            data += "\x00\x00\x00\x00" +
                "\x00\x00\x00\x00" +
                "\x00\x00\x00\x00";
            data += GetMD5Sum(data + "\x14\x00\x07\x0b").Substring(0, 8);
            data += IPDOG;
            data += "\x00\x00\x00\x00";

            data += host_name.PadRight(32, '\x00');
            foreach (string h in PRIMARY_DNS.Split('.'))
            {
                data += ConvertHexToString(Convert.ToInt32(h).ToString("x2"));
            }
            foreach (string h in dhcp_server.Split('.'))
            {
                data += ConvertHexToString(Convert.ToInt32(h).ToString("x2"));
            }
            data += "\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00";
            data += "\x94\x00\x00\x00" +
                "\x05\x00\x00\x00" +
                "\x01\x00\x00\x00" +
                "\x25\x00\x00\x00" +
                "\x02\x00\x00\x00";
            data += host_os.PadRight(32, '\x00');
            for (int i = 0; i < 96; i++)
            {
                data += "\x00";
            }
            data += AUTH_VERSION;
            data += "\x00";
            data += Convert.ToChar(password.Length);
            data += ROR(GetMD5Sum("\x03\x01" + salt + password), password);
            data += "\x02\x0c";
            data += checksum(data + "\x01\x26\x07\x11\x00\x00" + Dump(mac));
            data += "\x00\x00";
            data += Dump(mac);
            for (int i = 0; i < 8 - password.Length; i++)
            {
                data += "\x00";
            }
            if (password.Length % 2 == 1)
            {
                data += "\x00";
            }
            data += "\xe9\x13";
            return data;
        }

        public static string ConvertStringToHex(string strASCII, string separator = null)
        {
            StringBuilder sbHex = new StringBuilder();
            foreach (char chr in strASCII)
            {
                sbHex.Append(String.Format("{0:X2}", Convert.ToInt32(chr)));
                sbHex.Append(separator ?? string.Empty);
            }
            return sbHex.ToString();
        }
        public static string ConvertHexToString(string HexValue, string separator = null)
        {
            HexValue = string.IsNullOrEmpty(separator) ? HexValue : HexValue.Replace(string.Empty, separator);
            StringBuilder sbStrValue = new StringBuilder();

            while (HexValue.Length > 0)
            {
                sbStrValue.Append(Convert.ToChar(Convert.ToUInt32(HexValue.Substring(0, 2), 16)).ToString());
                HexValue = HexValue.Substring(2);
            }
            return sbStrValue.ToString();
        }

        private string GetMAC()
        {
            ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection nics = mc.GetInstances();

            foreach (ManagementObject nic in nics)
            {
                if (Convert.ToBoolean(nic["ipEnabled"]) == true)
                {
                    string ip = (nic["IPAddress"] as String[])[0];
                    string mac = nic["MacAddress"].ToString();
                    string gateway = (nic["DefaultIPGateway"] as String[])[0];
                    if (gateway.Contains("254") && gateway.Contains("172.") && ip.Contains("172."))
                    {
                        host_ip = ip;
                        return mac.Replace(":", "");
                    }
                }
            }


            return "NOTFOUND";
        }

        private static byte[] strToToHexByte(string hexString)
        {
            hexString = hexString.Replace(" ", "");
            if ((hexString.Length % 2) != 0)
                hexString = hexString.Insert(hexString.Length - 1, 0.ToString());
            byte[] returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            return returnBytes;
        }

        private static string ROR(string md5, string pwd)
        {
            string res = "";
            for (int i = 0; i < pwd.Length; i++)
            {
                int x = Convert.ToInt32(md5[i]) ^ Convert.ToInt32(pwd[i]);
                res += Convert.ToChar(((x << 3) & 0xff) + (x >> 5));
            }
            return res;
        }

        private static string checksum(string s)
        {
            ulong ret = 1234;
            for (int i = 0; i < s.Length; i += 4)
            {

                string t = i + 4 >= s.Length ? s.Substring(i).PadRight(4, '\x00') : s.Substring(i, 4);
                char[] temp = t.ToCharArray();
                Array.Reverse(temp);
                t = new string(temp);
                string cs = ConvertStringToHex(t);
                ret ^= Convert.ToUInt64(cs, 16);
            }
            ret = (1968 * ret) & 0xffffffff;
            string res = "";
            byte[] Bytes = BitConverter.GetBytes(Convert.ToUInt32(ret));
            foreach (byte b in Bytes)
            {
                res += b.ToString("x2");
            }
            res = ConvertHexToString(res);
            return res;
        }


        public void dial()
        {
            try
            {
                this.MAC = GetMAC();

            }
            catch (NullReferenceException)
            {
                idl?.Invoke(1);
                return;
            }
            Log.log("auth server:" + server + " username:" + username + " password:" + password + " mac:" + MAC);
            while (true)
            {
                string package_tail;
                try
                {
                    package_tail = login(username, password, server);

                }
                catch (LoginException)
                {
                    return;
                }
                Log.log("package_tail:" + ConvertStringToHex(package_tail));
                HeartBeat_1(SALT, package_tail, password, server);
                HeartBeat_2(SALT, package_tail, password, server);
                
            }
        }

        private string login(string usr,string pwd,string svr)
        {
            int i = 0,j=0;
            byte[] sendData, data = new byte[1024];
            IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(svr), 61440);
            EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
            string recvData;
            while (true) {
                TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
                long a = Convert.ToInt64(ts.TotalSeconds);
                Random r = new Random();
                a += r.Next(0xf, 0xff);
                string salt;
                try
                {
                   salt = challenge((int)a);
                }
                catch (ChallengeException)
                {
                    continue;
                }
                SALT = salt;
                long mac = Convert.ToInt64(MAC, 16);
                string packet = mkpkt(salt, usr, pwd, mac);
                Log.log("Login send " + ConvertStringToHex(packet));
                sendData = StrToBytes(packet);
                Log.log("Login packet sent ");
                
                
                s.SendTo(sendData,SocketFlags.None,ipep);
                try
                {
                    s.ReceiveFrom(data,SocketFlags.None, ref ep);                   
                }
                catch (SocketException)
                {
                    Log.log("challenge timeout retrying");
                    if (j >= 5)
                    {
                        Log.log("challenge timeout retrying over 5 times");
                        idl?.Invoke(0);
                        throw new LoginException();
                    }
                    continue;
                }

                recvData = BytesToString(data);
                Log.log("recive " + ConvertStringToHex(recvData));
                if(ep.ToString() == "10.1.1.254:61440")
                {
                    if (data[0] == '\x04')
                    {
                        Log.log("loged in");
                        AUTH_INFO = recvData.Substring(46,32);
                        break;
                    }
                    else
                    {
                        Log.log("login failed");
                        Thread.Sleep(30);
                        continue;
                    }
                }
                else
                {
                    if (i >= 5)
                    {
                        idl?.Invoke(0);
                        throw new LoginException();
                    }
                    else
                    {
                        i++;
                    }
                }
            }
            Log.log("login sent");
            return recvData.Substring(46,32);
        }

        public string challenge(int ran)
        {
            byte[] sendData, data = new byte[1024];
            IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(server), 61440);
            EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
            int h = ran % 0xffff;
            string t = h.ToString("x4");
            t = t.Substring(2, 2) + t.Substring(0, 2);
            t = "0102" + t + "09";
            for (int i = 0; i < 15; i++) t = t + "00";
            t = ConvertHexToString(t);
            sendData = Encoding.UTF8.GetBytes(t);

            while (true)
            {
                s.SendTo(sendData, ipep);
                Log.log("Challenge send " + ConvertStringToHex(t));
                try
                {
                    s.ReceiveFrom(data,SocketFlags.None,ref ep);
                }
                catch (SocketException)
                {
                    Log.log("challenge timeout retrying");
                    continue;
                }
                
                if (ep.ToString() == "10.1.1.254:61440")
                {
                    break;
                }
            }
            if (data[0] != '\x02')
            {
                throw new ChallengeException();
            }
            string res = BytesToString(data);
            Log.log("Challenge pacakage sent");
            return res.Substring(4, 4);
        }

        private string BytesToString(byte[] data)
        {
            string res = "";
            foreach (byte b in data)
            {
                res += b.ToString("x2");
            }
            return res;
        }

        private void HeartBeat_1(string salt,string tail,string pwd,string svr)
        {
            byte[] sendData, recvdata = new byte[1024];
            IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(server), 61440);
            EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            ushort a = (ushort)(Convert.ToUInt32(ts.TotalSeconds) % 0xffff);
            byte[] b = BitConverter.GetBytes(a);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(b);
            }
            string foo = BytesToString(b);
            string data = "\xff" + GetMD5Sum("\x03x01" + salt + pwd) + "\x00\x00\x00";
            data += ConvertHexToString(tail);
            data += foo + "\x00\x00\x00\x00";
            Log.log("send" + ConvertStringToHex(data));
            sendData = StrToBytes(data);
            s.SendTo(sendData, ipep);
            while (true)
            {
                try
                {
                   s.ReceiveFrom(recvdata, SocketFlags.None, ref ep);
                }
                catch (SocketException)
                {
                    continue;
                }
                
                if (recvdata[0] == '\x07')
                {
                    break;
                }
                else
                {
                    Log.log("recv not expected : " + BytesToString(recvdata));
                }
            }
            Log.log("recv " + BytesToString(recvdata));
        }
        private string HearBeat_PackageBuilder(int number,string tail,int type=1,bool first = false)
        {
            string data = "\x07" + Convert.ToChar(number) + "\x28\x00\x0b" + Convert.ToChar(type);
            if (first)
            {
                data += "\x0f\x27";
            }
            else
            {
                data += KEEP_ALIVE_VERSION;
            }
            data += "\x2f\x12\x00\x00\x00\x00\x00\x00";
            data += tail;
            data += "\x00\x00\x00\x00";
            if (type == 3)
            {
                string foo = "";
                foreach (string h in host_ip.Split('.'))
                {
                    foo += ConvertHexToString(Convert.ToInt32(h).ToString("x2"));
                }
                data += "\x00\x00\x00\x00" + foo + "\x00\x00\x00\x00\x00\x00\x00\x00";
            }
            else
            {
                data += "\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00";
            }
            return data;
        }

        private void HeartBeat_2(string salt, string taill, string pwd, string svr)
        {
            byte[] sendData, recvdata = new byte[1024];
            IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(server), 61440);
            EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
            Random random = new Random();
            int ran = random.Next(0, 0xffff);
            ran += random.Next(1, 10);
            int svr_num = 0;
            string packet = HearBeat_PackageBuilder(svr_num, "\x00\x00\x00\x00", 1, true);
            while (true)
            {
                Log.log("send1 " + ConvertStringToHex(packet));
                sendData = Encoding.UTF8.GetBytes(packet);
                s.SendTo(sendData, ipep);
                s.ReceiveFrom(recvdata, SocketFlags.None, ref ep);
                Log.log("recv1 "+ BytesToString(recvdata));
                string recv = Encoding.UTF8.GetString(recvdata);
                if(recv.StartsWith("\x07\x00\x28\x00") || recv.StartsWith("\x07" + Convert.ToChar(svr_num) + "\x285\x00"))
                {
                    break;
                }
                else if(recv[0] == '\x07' && recv[2] == '\x10')
                {
                    Log.log("recv file, resending..");
                    svr_num++;
                    break;
                }
                else
                {
                    Log.log("recv1 not expected : " + BytesToString(recvdata));
                }
            }
            Log.log("recv1 " + BytesToString(recvdata));

            ran += random.Next(1, 10);
            packet = HearBeat_PackageBuilder(svr_num, "\x00\x00\x00\x00", 1, false);
            Log.log("send2 " + ConvertStringToHex(packet));
            sendData = Encoding.UTF8.GetBytes(packet);
            s.SendTo(sendData, ipep);
            while (true)
            {
                s.ReceiveFrom(recvdata, SocketFlags.None, ref ep);
                if (recvdata[0] == '\x07')
                {
                    svr_num++;
                    break;
                }
                else
                {
                    Log.log("recv2 not expected : " + BytesToString(recvdata));
                }
            }
            Log.log("recv2 " + BytesToString(recvdata));
            string tail = Encoding.UTF8.GetString(recvdata).Substring(16, 4);

            ran += random.Next(1, 10);
            packet = HearBeat_PackageBuilder(svr_num, tail, 3, false);
            Log.log("send3 " + ConvertStringToHex(packet));
            sendData = Encoding.UTF8.GetBytes(packet);
            s.SendTo(sendData, ipep);
            while (true)
            {
                s.ReceiveFrom(recvdata, SocketFlags.None, ref ep);
                if (recvdata[0] == '\x07')
                {
                    svr_num++;
                    break;
                }
                else
                {
                    Log.log("recv3 not expected : " + BytesToString(recvdata));
                }
            }
            Log.log("recv3 " + BytesToString(recvdata));
            if (Global.status != 2)
            {
                idl?.Invoke(2);
                Global.status = 2;
            }
            tail = Encoding.UTF8.GetString(recvdata).Substring(16, 4);
            int i = svr_num;
            while (true)
            {
                try
                {
                    Thread.Sleep(20000);
                    HeartBeat_1(salt, taill, pwd, svr);
                    ran += random.Next(1, 10);
                    packet = HearBeat_PackageBuilder(i, tail, 1, false);
                    Log.log("send" + Convert.ToString(i)+" " + ConvertStringToHex(packet));
                    sendData = Encoding.UTF8.GetBytes(packet);
                    s.SendTo(sendData, ipep);
                    s.ReceiveFrom(recvdata, SocketFlags.None, ref ep);
                    Log.log("recv" + BytesToString(recvdata));
                    tail = Encoding.UTF8.GetString(recvdata).Substring(16, 4);

                    ran += random.Next(1, 10);
                    packet = HearBeat_PackageBuilder(i+1, tail, 3, false);
                    Log.log("send" + Convert.ToString(i+1) + " " + ConvertStringToHex(packet));
                    sendData = Encoding.UTF8.GetBytes(packet);
                    s.SendTo(sendData, ipep);
                    s.ReceiveFrom(recvdata, SocketFlags.None, ref ep);
                    Log.log("recv " + BytesToString(recvdata));
                    tail = Encoding.UTF8.GetString(recvdata).Substring(16, 4);

                    i = (i + 2) % 0xff;

                }
                catch
                {
                    break;
                }
            }
        }

        private byte[] StrToBytes(string str)
        {
            byte[] b = new byte[str.Length];
            int i = 0;
            foreach(char c in str)
            {
                b[i++]=Convert.ToByte(c);
            }
            return b;
        }

        public void logout()
        {
            IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(server), 61440);
            EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            long a = Convert.ToInt64(ts.TotalSeconds);
            Random r = new Random();
            a += r.Next(0xf, 0xff);
            string salt = challenge((int)a);
            if (salt != "")
            {
                string data = "\x06x\01\x00" + Convert.ToChar((username.Length + 20));
                data += GetMD5Sum("\x03\x01" + salt + password);
                data += username.PadRight(36, '\x00');
                data += CONTROLCHECKSTATUS;
                data += ADAPTERNUM;

                string d4 = data.Substring(4, 6);
                d4 = ConvertStringToHex(d4);
                long t = Convert.ToInt64(d4, 16);
                long mac = Convert.ToInt64(MAC, 16);
                data += Dump(t ^ mac).PadLeft(6,'\x00');
                data += AUTH_INFO;
                byte[] sendData = StrToBytes(data),recv = new byte[1024];
                s.SendTo(sendData, ipep);
                while (true)
                {
                    try
                    {
                        s.ReceiveFrom(recv, ref ep);
                    }
                    catch (SocketException)
                    {
                        continue;
                    }
                    if (recv[0] == '\x04')
                    {
                        Log.log("loged out");
                    }
                }
            }
        }
    }
}