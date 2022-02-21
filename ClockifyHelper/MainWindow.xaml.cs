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
		private System.Windows.Forms.ToolStripMenuItem toggleActivatetMenuItem;

		public MainWindow()
		{
			InitializeComponent();
			applicationSettings = InitializeApplicationSettings();
			DataContext = new ViewModel(applicationSettings, ShowNotification, ChangeToggleActivationButtonText);

			if (applicationSettings.MinimizeOnClose && !applicationSettings.ShowInSystemTray)
			{
				MessageBox.Show("MinimizeOnClose is set to true while ShowInSystemTray is set to False." +
					" There will be no way to shutdown the application. The setting for showing System Tray Icon will be overriden", "Configuration Error", MessageBoxButton.OK);

				applicationSettings.ShowInSystemTray = true;
			}

			if (applicationSettings.EnableNotifications && !applicationSettings.ShowInSystemTray)
			{
				MessageBox.Show("Notifications only work if the System Tray icon is also enabled", "Configuration Error", MessageBoxButton.OK);

				applicationSettings.EnableNotifications = false;
			}

			if (applicationSettings.ShowInSystemTray)
			{
				ConfigureSystemTrayIcon();
			}
		}

		private ApplicationSettings InitializeApplicationSettings()
		{
			var configurtion = new ConfigurationBuilder()
				.AddJsonFile("appsettings.json")
				.AddJsonFile("appsettings.local.json", optional: true)
				.Build();

			return new ApplicationSettings()
			{
				ApiKey = configurtion["ApplicationSettings:ApiKey"],
				DefaultProjectName = configurtion["ApplicationSettings:DefaultProjectName"],
				IdleThresholdMinutes = int.Parse(configurtion["ApplicationSettings:IdleThresholdMinutes"]),
				MinimizeOnClose = bool.Parse(configurtion["ApplicationSettings:MinimizeOnClose"]),
				ShowInSystemTray = bool.Parse(configurtion["ApplicationSettings:ShowInSystemTray"]),
				EnableNotifications = bool.Parse(configurtion["ApplicationSettings:EnableNotifications"]),
			};
		}

		private void ConfigureSystemTrayIcon()
		{
			notifyIcon = new System.Windows.Forms.NotifyIcon();
			notifyIcon.Icon = new System.Drawing.Icon("./Assets/favicon.ico");
			notifyIcon.MouseDoubleClick += NotifyIcon_MouseDoubleClick;

			notifyIcon.ContextMenuStrip = ConfigureContextMenuStrip();

			notifyIcon.Visible = true;
		}

		private System.Windows.Forms.ContextMenuStrip ConfigureContextMenuStrip()
		{
			var contextMenu = new System.Windows.Forms.ContextMenuStrip();

			toggleActivatetMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			toggleActivatetMenuItem.Text = "A&ctivate";
			toggleActivatetMenuItem.Click += ToggleActivateClicked;

			var exitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			exitMenuItem.Text = "E&xit";
			exitMenuItem.Click += ExitClicked;

			contextMenu.Items.AddRange(new System.Windows.Forms.ToolStripMenuItem[] { toggleActivatetMenuItem, exitMenuItem });

			return contextMenu;
		}

		private void ToggleActivateClicked(object sender, EventArgs e)
		{
			ViewModel.Instance?.StartCommand?.Execute(false);
		}

		private void ExitClicked(object sender, EventArgs e)
		{
			forceExit = true;
			Close();
		}

		private void NotifyIcon_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
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

		private void ShowNotification(string text)
		{
			Application.Current.Dispatcher.Invoke(() =>
			{
				if (applicationSettings.EnableNotifications && notifyIcon != null)
				{
					notifyIcon.ShowBalloonTip(1500, "Time Tracking", text, System.Windows.Forms.ToolTipIcon.Info);
				}
			});
		}

		private void ChangeToggleActivationButtonText(bool isStarted)
		{
			Application.Current.Dispatcher.Invoke(() =>
			{
				toggleActivatetMenuItem.Text = isStarted ? "S&top" : "A&ctivate";
				notifyIcon.Icon = isStarted ? new System.Drawing.Icon("./Assets/favicon-active.ico") : new System.Drawing.Icon("./Assets/favicon.ico");
			});
		}
	}
}