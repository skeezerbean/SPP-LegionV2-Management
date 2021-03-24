using MahApps.Metro.Controls.Dialogs;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace SPP_LegionV2_Management
{
	public class ShellViewModel : BaseViewModel
	{
		private DateTime lastUpdate = DateTime.Now;

		// Setup our public variables and such, many are saved within the general settings class, so we'll get/set from those
		public string AppTitle { get; set; } = $"SPP LegionV2 Management v{Assembly.GetExecutingAssembly().GetName().Version}";

		// declare individual VMs, lets us always show the same one as we switch tabs
		public ConfigGeneratorViewModel ConfigGeneratorVM = new ConfigGeneratorViewModel(DialogCoordinator.Instance);

		public AccountManagerViewModel AccountManagerVM = new AccountManagerViewModel(DialogCoordinator.Instance);
		public SettingsViewModel SettingsVM = new SettingsViewModel(DialogCoordinator.Instance);

		// This holds the values for the window position/size to be pulled from saved settings
		public double WindowTop { get { return GeneralSettingsManager.GeneralSettings.WindowTop; } set { GeneralSettingsManager.GeneralSettings.WindowTop = value; } }

		public double WindowLeft { get { return GeneralSettingsManager.GeneralSettings.WindowLeft; } set { GeneralSettingsManager.GeneralSettings.WindowLeft = value; } }
		public double WindowHeight { get { return GeneralSettingsManager.GeneralSettings.WindowHeight; } set { GeneralSettingsManager.GeneralSettings.WindowHeight = value; } }
		public double WindowWidth { get { return GeneralSettingsManager.GeneralSettings.WindowWidth; } set { GeneralSettingsManager.GeneralSettings.WindowWidth = value; } }

		// Status display at the top section of the app
		public string ServerConfigStatus { get; set; } = "⚠";

		public string ClientConfigStatus { get; set; } = "⚠";
		public string SQLConnectionStatus { get; set; } = "⚠";

		public ShellViewModel()
		{
			GeneralSettingsManager.MoveIntoView();
			UpdateStatus();
		}

		// This will constantly run to update the status
		private async void UpdateStatus()
		{
			// Keep this running always
			while (1 == 1)
			{
				// Every 2 seconds we want to check for updates in the even the Database Server status or file locations change
				if (lastUpdate.AddSeconds(2) < DateTime.Now)
				{
					if ((File.Exists($"{ GeneralSettingsManager.GeneralSettings.SPPFolderLocation}\\worldserver.conf") && File.Exists($"{ GeneralSettingsManager.GeneralSettings.SPPFolderLocation}\\bnetserver.conf"))
						|| (File.Exists($"{ GeneralSettingsManager.GeneralSettings.SPPFolderLocation}\\Servers\\worldserver.conf") && File.Exists($"{ GeneralSettingsManager.GeneralSettings.SPPFolderLocation}\\Servers\\bnetserver.conf")))
						ServerConfigStatus = "✓";
					else
						ServerConfigStatus = "⚠";

					if (File.Exists($"{GeneralSettingsManager.GeneralSettings.WOWConfigLocation}\\config.wtf")
						|| File.Exists($"{GeneralSettingsManager.GeneralSettings.WOWConfigLocation}\\WTF\\config.wtf"))
						ClientConfigStatus = "✓";
					else
						ClientConfigStatus = "⚠";

					if (await CheckSQLStatus())
					{
						SQLConnectionStatus = "✓";
						GeneralSettingsManager.IsMySQLRunning = true;
					}
					else
					{
						SQLConnectionStatus = "⚠";
						GeneralSettingsManager.IsMySQLRunning = false;
					}

					lastUpdate = DateTime.Now;
				}

				await Task.Delay(1);
			}
		}

		private async Task<bool> CheckSQLStatus()
		{
			string result = string.Empty;

			await Task.Run(() =>
			{
				result = MySqlManager.MySQLQueryToString(@"SELECT '1'");
			});

			return result == "1";
		}

		public void LoadPageConfigGenerator()
		{
			ActivateItem(ConfigGeneratorVM);
		}

		public void LoadPageAccountManager()
		{
			ActivateItem(AccountManagerVM);
		}

		public void LoadPageSettings()
		{
			ActivateItem(SettingsVM);
		}
	}
}