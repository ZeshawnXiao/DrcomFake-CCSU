using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DrcomFake
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public string username{ get; set;}
        public string password{get;set;}

        private System.Timers.Timer UITimer = null;
        public delegate void UpdateDelegate(List<string> l);

        private NotifyIcon notifyIcon;
        public MainWindow()
        {
            InitializeComponent();
            MainGrid.DataContext = this;
            string automin = Settings.GetSettingValue("automin");
            cbAutoMin.IsChecked = automin==null?false:bool.Parse(automin);
            string autologin = Settings.GetSettingValue("autologin");
            cbAutoLogin.IsChecked = autologin == null ? false : bool.Parse(autologin);
            UITimer = new System.Timers.Timer();
            UITimer.Interval = 1000;
            UITimer.Elapsed += GetUpdateInfo;
        }

        private void GetUpdateInfo(object sender, ElapsedEventArgs e)
        {
            List<string> l = DrcomWeb.GetLoginInfo();
            UpdateThread(l);
        }

        private void DragWindow(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
        public void InitTray()
        {
            this.notifyIcon = new NotifyIcon();
            this.notifyIcon.BalloonTipText = "长沙学院校园网客户端\n当前用户:" + username;
            this.notifyIcon.ShowBalloonTip(2000);
            this.notifyIcon.Text = "长沙学院校园网客户端\n当前用户:" + username;
            this.notifyIcon.Icon = new System.Drawing.Icon(App.GetResourceStream(new Uri("pack://application:,,,/icon.ico",UriKind.RelativeOrAbsolute)).Stream);
            this.notifyIcon.Visible = true;
            //打开菜单项
            System.Windows.Forms.MenuItem open = new System.Windows.Forms.MenuItem("显示主界面");
            open.Click += new EventHandler(Show);
            //退出菜单项
            System.Windows.Forms.MenuItem exit = new System.Windows.Forms.MenuItem("注销并退出");
            exit.Click += new EventHandler(Close);
            //关联托盘控件
            System.Windows.Forms.MenuItem[] childen = new System.Windows.Forms.MenuItem[] { open, exit };
            notifyIcon.ContextMenu = new System.Windows.Forms.ContextMenu(childen);

            this.notifyIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler((o, e) =>
            {
                if (e.Button == MouseButtons.Left) this.Show(o, e);
            });
            UITimer.Start();
        }

        private void SnackbarMessage_ActionClick(object sender, RoutedEventArgs e)
        {
            Snackbar.IsActive = false;
        }

        public void SetInfo()
        {

        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            notifyIcon.Dispose();
            Environment.Exit(0);
        }

        private void Show(object sender, EventArgs e)
        {
            this.Visibility = Visibility.Visible;
            this.ShowInTaskbar = true;
            this.Activate();
        }

        private void Hide(object sender, EventArgs e)
        {
            this.ShowInTaskbar = false;
            this.Visibility = Visibility.Collapsed;
        }

        private void Close(object sender, EventArgs e)
        {

            this.ShowInTaskbar = false;
            notifyIcon.Dispose();
            Environment.Exit(0);
        }

        private void CloseToTray_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void CbAutoMin_Click(object sender, RoutedEventArgs e)
        {
            Settings.SetSettingValue("automin", cbAutoMin.IsChecked.Value.ToString());

            Settings.SetSettingValue("autologin", cbAutoLogin.IsChecked.Value.ToString());
        }

        private void UpdateThread(List<string> l)
        {
            UpdateDelegate ud = new UpdateDelegate(updateUI);
            this.Dispatcher.Invoke(ud, l);
        }

        private void updateUI(List<string> l)
        {
            string a = l[0] + "分钟";
            a = a.PadLeft(10, ' ');
            CurrentTime.Text = "时长:" + a;
            int flow = int.Parse(l[1]);
            int flow0 = flow % 1024;int flow1=flow-flow0;flow0 *= 1000;flow0 -= flow0 % 1024;
            string b = (flow1 / 1024).ToString() + "." + (flow0 / 1024) + " MB";
            b = b.PadLeft(10, ' ');
            CurrentTraffic.Text = "流量:" + b;
        }
    }
}
