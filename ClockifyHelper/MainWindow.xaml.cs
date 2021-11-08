using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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

namespace ClockifyHelper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private System.Windows.Forms.NotifyIcon notifyIcon;
        private readonly ApplicationSettings applicationSettings;

        private bool forceExit;

        public MainWindow()
        {
            InitializeComponent();

            var configurtion = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.local.json", optional: true)
                .Build();

            applicationSettings = new ApplicationSettings()
            {
                ApiKey = configurtion["ApplicationSettings:ApiKey"],
                DefaultProjectName = configurtion["ApplicationSettings:DefaultProjectName"],
                IdleThresholdMinutes = int.Parse(configurtion["ApplicationSettings:IdleThresholdMinutes"]),
                MinimizeOnClose = bool.Parse(configurtion["ApplicationSettings:MinimizeOnClose"]),
                ShowInSystemTray = bool.Parse(configurtion["ApplicationSettings:ShowInSystemTray"])
            };

            if (applicationSettings.MinimizeOnClose && !applicationSettings.ShowInSystemTray)
            {
                MessageBox.Show("MinimizeOnClose is set to true while ShowInSystemTray is set to False." +
                    " There will be no way to shutdown the application. The setting for System Tray will be overriden", "Configuration Error", MessageBoxButton.OK);

                applicationSettings.ShowInSystemTray = true;
            }

            DataContext = new ViewModel(applicationSettings);

            if (applicationSettings.ShowInSystemTray)
            {
                System.ComponentModel.IContainer container = new System.ComponentModel.Container();
                notifyIcon = new System.Windows.Forms.NotifyIcon(container);
                notifyIcon.Icon = new System.Drawing.Icon("./Assets/Diflexmo_logo.ico");
                notifyIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(MyNotifyIcon_MouseDoubleClick);

                var contextMenu = new System.Windows.Forms.ContextMenuStrip();

                var menuItem = new System.Windows.Forms.ToolStripMenuItem();
                menuItem.Text = "E&xit";
                menuItem.Click += ExitClick;
                contextMenu.Items.AddRange(new System.Windows.Forms.ToolStripMenuItem[] { menuItem });

                notifyIcon.ContextMenuStrip = contextMenu;

                notifyIcon.Visible = true;
            }
        }

        private void ExitClick(object sender, EventArgs e)
        {
            forceExit = true;
            Close();
        }

        private void MyNotifyIcon_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            WindowState = WindowState.Normal;
            Activate();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (applicationSettings.ShowInSystemTray)
            {
                if (WindowState == WindowState.Minimized)
                {
                    ShowInTaskbar = false;
                }
                else if (WindowState == WindowState.Normal)
                {
                    ShowInTaskbar = true;
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (applicationSettings.MinimizeOnClose && !forceExit)
            {
                e.Cancel = true;
                WindowState = WindowState.Minimized;
            }
            else
            {
                if (notifyIcon != null)
                {
                    notifyIcon.Visible = false;
                }
            }
        }
    }
}
