using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DrcomFake
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LoginWindow : Window
    {
        private bool savepass,autologin;
        Drcom dr = null;
        private delegate void CallBackFun(int status);
        MainWindow mw = null;
        public LoginWindow()
        {
            InitializeComponent();
            
            txtUsername.Text = Settings.GetSettingValue("username")==null?"": Settings.GetSettingValue("username");
            txtPasswd.Password = Settings.GetSettingValue("password")==null?"": Settings.GetSettingValue("password");
            cbSavePass.IsChecked = Settings.GetSettingValue("savepass")==null?false:bool.Parse(Settings.GetSettingValue("savepass"));
            cbAutoLogin.IsChecked = Settings.GetSettingValue("autologin")==null?false:bool.Parse(Settings.GetSettingValue("autologin")); ;
            Global.status = 0;
            dr = new Drcom();
            mw = new MainWindow();
        }

        private void DragWindow(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void Cb_Click(object sender, RoutedEventArgs e)
        {            
            autologin = cbAutoLogin.IsChecked.Value;
            savepass = cbSavePass.IsChecked.Value || cbAutoLogin.IsChecked.Value;
            cbSavePass.IsChecked = savepass;
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            login();
        }
        private void login()
        {
            if (!isLoginInfoValid())
            {
                SnackMessage.Content = "用户名或密码不能为空。";
                Snackbar.IsActive = true;

                return;
            }

            Settings.SetSettingValue("username", txtUsername.Text);
            Settings.SetSettingValue("password", txtPasswd.Password);
            Settings.SetSettingValue("autologin", cbAutoLogin.IsChecked.Value.ToString());
            Settings.SetSettingValue("savepass", cbSavePass.IsChecked.Value.ToString());
            ChangeUIWhenLogin(true);

            string usr = txtUsername.Text + "@unicom";
            string pwd = txtPasswd.Password;
            dr.SetAuthInfo(usr, pwd);
            dr.idl += LoginSuccess;
            Global.DrThread = new Thread(new ThreadStart(dr.dial));
            Global.DrThread.Start();
        }

        private void LoginSuccess(int status)
        {
            
            UpdateStatusThread(status);
            
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (btnLogin.IsEnabled)
                Application.Current.Shutdown();
            else {
                ChangeUIWhenLogin(false);
                try
                {
                    Global.DrThread.Abort();
                }
                catch (ThreadAbortException)
                {

                }
                
            }
        }

        private void SnackbarMessage_ActionClick(object sender, RoutedEventArgs e)
        {
            Snackbar.IsActive = false;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
           // FileStream fs = new FileStream("login_log.log", FileMode.Create);
            //fs.Close();
            Log.log("window has opened");
            if (cbAutoLogin.IsChecked.Value)
            {
                login();
            }

        }

        private bool isLoginInfoValid()
        {
            return txtPasswd.Password.Length != 0 && txtUsername.Text.Length != 0;
        }

        private void ChangeUIWhenLogin(bool b)
        {
            btnLogin.IsEnabled =
            txtUsername.IsEnabled = txtPasswd.IsEnabled =
            cbAutoLogin.IsEnabled = cbSavePass.IsEnabled =
            lblPwd.IsEnabled = lblUsr.IsEnabled = !b;
            prgBar.IsIndeterminate = b;
            btnLogin.Content = !b ? "登录" : "正在登录";
                
            
        }

        private void UpdateStatus(int status)
        {
            switch (status)
            {
                case 0:
                    SnackMessage.Content = "登录超时。";
                    Snackbar.IsActive = true;
                    ChangeUIWhenLogin(false);
                    break;
                case 1:
                    SnackMessage.Content = "网络没有联通。";
                    Snackbar.IsActive = true;
                    ChangeUIWhenLogin(false);
                    break;
                case 2:
                    mw.tbUsr.Text = txtUsername.Text;
                    mw.username = txtUsername.Text;
                    if(!mw.cbAutoMin.IsChecked.Value)
                        mw.Show();
                    mw.InitTray();
                    this.Visibility = Visibility.Collapsed ;
                    break;
            }
        }

        private void UpdateStatusThread(int status)
        {
            CallBackFun c = new CallBackFun(UpdateStatus);
            this.Dispatcher.Invoke(c, status);
        }
    }
}
