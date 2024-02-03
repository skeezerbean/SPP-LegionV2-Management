using Caliburn.Micro;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;

namespace SPP_LegionV2_Management
{
	public class SettingsViewModel : Screen
	{
		// IDialogCoordinator is for metro message boxes
		private readonly IDialogCoordinator _dialogCoordinator;

		public string SPPFolderLocation
		{ get { return GeneralSettingsManager.GeneralSettings.SPPFolderLocation; } set { GeneralSettingsManager.GeneralSettings.SPPFolderLocation = value; } }
		public string WOWConfigLocation
		{ get { return GeneralSettingsManager.GeneralSettings.WOWConfigLocation; } set { GeneralSettingsManager.GeneralSettings.WOWConfigLocation = value; } }

		// IDialogCoordinator is part of Metro, for dialog handling in the view model
		public SettingsViewModel(IDialogCoordinator instance)
		{
			_dialogCoordinator = instance;
		}

		// From hitting the SPP Browse button in settings tab
		public void SPPFolderBrowse()
		{
			// If it's empty, then it was cancelled and we keep the old setting
			string tmp = BrowseFolder();
			if (tmp != string.Empty)
			{
				SPPFolderLocation = tmp;
				GeneralSettingsManager.SaveSettings(GeneralSettingsManager.SettingsPath, GeneralSettingsManager.GeneralSettings);
			}
		}

		// From hitting the Wow browse button in settings tab
		public void WowConfigBrowse()
		{
			// If it's empty, then it was cancelled and we keep the old setting
			string tmp = BrowseFolder();
			if (tmp != string.Empty)
			{
				WOWConfigLocation = tmp;
				GeneralSettingsManager.SaveSettings(GeneralSettingsManager.SettingsPath, GeneralSettingsManager.GeneralSettings);
			}
		}

		// Method to browse to a folder
		public string BrowseFolder()
		{
			string result = string.Empty;

			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "wtf files (*.wtf)|*.wtf";
			openFileDialog.InitialDirectory = @"C:\";
			openFileDialog.Title = "Please select a WoW client config file.";
			if (openFileDialog.ShowDialog() == true)
				result = openFileDialog.FileName;

			return result;
		}
	}
}