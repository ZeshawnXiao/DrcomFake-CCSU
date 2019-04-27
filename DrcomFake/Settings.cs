using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace DrcomFake
{
    public class Settings
    {
        public static void SetSettingValue(string key,string value)
        {
            Microsoft.Win32.RegistryKey registryKey;

            
            registryKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\DrcomFake");
            registryKey.SetValue(key, value);
            registryKey.Close();
        }

        public static string GetSettingValue(string key)
        {
            Microsoft.Win32.RegistryKey registryKey;
            registryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\DrcomFake");
            if (registryKey == null) return null;
            return (string)registryKey.GetValue(key);
        }
    }

}