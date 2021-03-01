using MahApps.Metro.Controls.Dialogs;
using System.Reflection;

namespace SPP_LegionV2_Management
{
	public class ShellViewModel : BaseViewModel
	{
		// Setup our public variables and such, many are saved within the general settings class, so we'll get/set from those
		public string AppTitle { get; set; } = $"SPP Config Generator v{Assembly.GetExecutingAssembly().GetName().Version}";

		// declare individual VMs, lets us always show the same one as we switch tabs
		public ConfigGeneratorViewModel ConfigGeneratorVM = new ConfigGeneratorViewModel(DialogCoordinator.Instance);
		public AccountManagerViewModel AccountManagerVM = new AccountManagerViewModel(DialogCoordinator.Instance);

		public double WindowTop { get { return GeneralSettingsManager.GeneralSettings.WindowTop; } set { GeneralSettingsManager.GeneralSettings.WindowTop = value; } }
		public double WindowLeft { get { return GeneralSettingsManager.GeneralSettings.WindowLeft; } set { GeneralSettingsManager.GeneralSettings.WindowLeft = value; } }
		public double WindowHeight { get { return GeneralSettingsManager.GeneralSettings.WindowHeight; } set { GeneralSettingsManager.GeneralSettings.WindowHeight = value; } }
		public double WindowWidth { get { return GeneralSettingsManager.GeneralSettings.WindowWidth; } set { GeneralSettingsManager.GeneralSettings.WindowWidth = value; } }

		public ShellViewModel()
		{
			// Load our default child view, and pull in saved settings
			LoadPageConfigGenerator();

			GeneralSettingsManager.MoveIntoView();
		}

		public void LoadPageConfigGenerator() { ActivateItem(ConfigGeneratorVM); }

		public void LoadPageAccountManager() { ActivateItem(AccountManagerVM); }
	}
}