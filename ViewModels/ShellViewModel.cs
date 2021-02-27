using Caliburn.Micro;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Data;

namespace SPP_Config_Generator
{
	public class ShellViewModel : Screen, INotifyPropertyChanged
	{
		// Setup our public variables and such, many are saved within the general settings class, so we'll get/set from those
		public string AppTitle { get; set; } = $"SPP Config Generator v{Assembly.GetExecutingAssembly().GetName().Version}";

		public double WindowTop { get { return GeneralSettingsManager.GeneralSettings.WindowTop; } set { GeneralSettingsManager.GeneralSettings.WindowTop = value; } }
		public double WindowLeft { get { return GeneralSettingsManager.GeneralSettings.WindowLeft; } set { GeneralSettingsManager.GeneralSettings.WindowLeft = value; } }
		public double WindowHeight { get { return GeneralSettingsManager.GeneralSettings.WindowHeight; } set { GeneralSettingsManager.GeneralSettings.WindowHeight = value; } }
		public double WindowWidth { get { return GeneralSettingsManager.GeneralSettings.WindowWidth; } set { GeneralSettingsManager.GeneralSettings.WindowWidth = value; } }
		public string SPPFolderLocation { get { return GeneralSettingsManager.GeneralSettings.SPPFolderLocation; } set { GeneralSettingsManager.GeneralSettings.SPPFolderLocation = value; } }
		public string WOWConfigLocation { get { return GeneralSettingsManager.GeneralSettings.WOWConfigLocation; } set { GeneralSettingsManager.GeneralSettings.WOWConfigLocation = value; } }
		public string MySQLServer { get { return GeneralSettingsManager.GeneralSettings.MySQLServer; } set { GeneralSettingsManager.GeneralSettings.MySQLServer = value; } }
		public int MySQLPort { get { return GeneralSettingsManager.GeneralSettings.MySQLPort; } set { GeneralSettingsManager.GeneralSettings.MySQLPort = value; } }
		public string MySQLUser { get { return GeneralSettingsManager.GeneralSettings.MySQLUser; } set { GeneralSettingsManager.GeneralSettings.MySQLUser = value; } }
		public string MySQLPass { get { return GeneralSettingsManager.GeneralSettings.MySQLPass; } set { GeneralSettingsManager.GeneralSettings.MySQLPass = value; } }

		// These are the collections we'll be using, pulled from the Default Templates folder,
		// or from the existing WoW installation if the folder is defined
		public BindableCollection<ConfigEntry> WorldCollectionTemplate { get; set; } = new BindableCollection<ConfigEntry>();

		public BindableCollection<ConfigEntry> BnetCollectionTemplate { get; set; } = new BindableCollection<ConfigEntry>();
		public BindableCollection<ConfigEntry> WorldCollection { get; set; } = new BindableCollection<ConfigEntry>();
		public BindableCollection<ConfigEntry> BnetCollection { get; set; } = new BindableCollection<ConfigEntry>();

		// stores the filesystem path to the files
		public string WowConfigFile { get; set; } = string.Empty;

		public string BnetConfFile { get; set; } = string.Empty;
		public string WorldConfFile { get; set; } = string.Empty;

		// The statusbox is the status line displayed next to buttons
		public string StatusBox { get; set; }

		// This is the text for the log pane on the right side
		public string LogText { get; set; }

		// For search/filtering
		public ICollectionView worldView { get { return CollectionViewSource.GetDefaultView(WorldCollection); } }

		public ICollectionView bnetView { get { return CollectionViewSource.GetDefaultView(BnetCollection); } }
		private string _SearchBox = "";

		public string SearchBox
		{
			get { return _SearchBox; }
			set
			{
				if (_SearchBox != value)
				{
					_SearchBox = value;
				}
				RefreshViews();
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged(string propertyName)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		public ShellViewModel()
		{
			Log("App Initializing...");

			// Pull in saved settings
			Log("Loading settings");
			LoadSettings();

			// Alert if this variable is empty, it means we either have no saved settings
			// Or the SPP folder location was never set
			if (SPPFolderLocation == string.Empty)
				StatusBox = "Please set SPP Location in the General Settings tab";

			// If the window was last saved in position that is no longer
			// in view, then move it
			Log("Set Window position/width/height, moving into view");
			GeneralSettingsManager.MoveIntoView();

			worldView.Filter = new Predicate<object>(o => Filter(o as ConfigEntry));
			bnetView.Filter = new Predicate<object>(o => Filter(o as ConfigEntry));
		}

		// Refresh the ICollectionView whenever anything changes, for all collections
		private void RefreshViews()
		{
			NotifyPropertyChanged("SearchBox");
			worldView.Refresh();
			bnetView.Refresh();
		}

		private bool Filter(ConfigEntry entry)
		{
			return SearchBox == null
				|| entry.Name.IndexOf(SearchBox, StringComparison.OrdinalIgnoreCase) != -1
				|| entry.Value.IndexOf(SearchBox, StringComparison.OrdinalIgnoreCase) != -1;
		}

		// Pass in a collection, and which setting/value we want to change
		// then return back the updated collection
		public BindableCollection<ConfigEntry> UpdateConfigCollection(BindableCollection<ConfigEntry> collection, string entry, string value)
		{
			foreach (var item in collection)
			{
				if (string.Equals(item.Name, entry, StringComparison.OrdinalIgnoreCase))
				{
					// Update the value, then stop processing in case there's a duplicate.
					// We'll update the first, it's most likely the original/valid one
					item.Value = value;
					break;
				}
			}

			return collection;
		}

		// We want to set the external/hosting IP setting for the DB listing in the realm,
		// for the ExternalAddress setting in bnet, and in the WOW config portal entry
		public void SetIP()
		{
			// Check if there are valid targets for spp/wow config, sql - report if any missing
			// As long as we set Bnet REST IP first, then WoW config will be updated as well
			string tmp = "Enter the Listening/Hosted IP Address to set. If this is to be hosted for local network then use the LAN ipv4 address.\n\n";
			tmp += "If external hosting, use the WAN address. Note this entry will not be validated to accuracy.\n\n";
			tmp += "Note - this will not update the database realm entry until clicking save/export";
			string input = Microsoft.VisualBasic.Interaction.InputBox(tmp, "Set IP", "127.0.0.1");

			// If user hit didn't cancel or enter something stupid...
			// length > 6 is at least 4 numbers for an IP, and 3 . within an IP
			if (input.Length > 6)
				BnetCollection = UpdateConfigCollection(BnetCollection, "LoginREST.ExternalAddress", input);

			RefreshViews();
		}

		// We need the realm build entry, and both .conf build settings to be the same
		public void SetBuild()
		{
			// Grab the input
			string tmp = "Enter the 7.3.5 (xxxxx) build from your client. Available builds: 26124, 26365, 26654, 26822, 26899, or 26972\n\n";
			tmp += "Note - the build will not be updated in the database until clicking save/export";
			string input = Microsoft.VisualBasic.Interaction.InputBox(tmp, "Set Build", "26972");

			if (input == "26124" || input == "26365" || input == "26654" || input == "26822" || input == "26899" || input == "26972")
			{
				// Update Bnet entry
				BnetCollection = UpdateConfigCollection(BnetCollection, "Game.Build.Version", input);

				// Update World entry
				WorldCollection = UpdateConfigCollection(WorldCollection, "Game.Build.Version", input);
			}
			else // If not cancelled input, then alert to invalid entry
				if (input != "")
				MessageBox.Show("Build input invalid, ignoring");

			RefreshViews();
		}

		// This takes current settings in the default templates, and
		// overwrites our current settings with those defaults
		public void SetDefaults()
		{
			// Due to switching to CollectionView, we cannot simply replace the collection
			// with the template, but need to clear and re-use the existing, adding items in
			if (WorldCollectionTemplate == null)
				Log("World Template is null, cannot set defaults.");
			else
			{
				WorldCollection.Clear();
				foreach (var item in WorldCollectionTemplate)
				{
					WorldCollection.Add(item);
				}
			}

			if (BnetCollectionTemplate == null)
				Log("Bnet Template is null, cannot set defaults.");
			else
			{
				BnetCollection.Clear();
				foreach (var item in BnetCollectionTemplate)
				{
					BnetCollection.Add(item);
				}
			}

			// For whatever reason, this quick pause helps refresh visually as the collection changed
			Thread.Sleep(1);
			RefreshViews();
		}

		// We take the incoming collection, and search string, and go through each entry
		// to see if we have a match in the name/setting only. We don't care about a match
		// in the value or description
		public bool CheckCollectionForMatch(BindableCollection<ConfigEntry> collection, string searchValue)
		{
			bool match = false; // set our default

			foreach (var item in collection)
			{
				if (string.Equals(NormalizeString(item.Name), NormalizeString(searchValue), StringComparison.OrdinalIgnoreCase))
				{
					// Found a match, can stop checking in case there is a duplicate
					// and that can be checked by another method
					match = true;
					break;
				}
			}

			return match;
		}

		// Pass in our collection, and search value, and return any value for the matching setting
		// based on case-insensitive match
		public string GetValueFromCollection(BindableCollection<ConfigEntry> collection, string searchValue)
		{
			string result = string.Empty;

			// Populate from collection and check each entry as long as the
			// isn't empty. May no longer need to return a normalized string if the
			// parsing was correct when reading from file. May remove later...
			if (collection != null)
				foreach (var item in collection)
					if (string.Equals(NormalizeString(item.Name), NormalizeString(searchValue), StringComparison.OrdinalIgnoreCase))
						result = item.Value;

			return NormalizeString(result);
		}

		// Take a collection, and search value, and find if there's a matching setting.
		// If so, if that setting = 1 then return true. This is assuming that the one
		// being searched for is only for valid for 0/1 as the value
		public bool IsOptionEnabled(BindableCollection<ConfigEntry> collection, string searchValue)
		{
			bool result = false;

			if (collection != null)
				foreach (var item in collection)
					if (string.Equals(NormalizeString(item.Name), NormalizeString(searchValue), StringComparison.OrdinalIgnoreCase) && item.Value == "1")
						result = true;

			return result;
		}

		// strip out white space
		public string NormalizeString(string incoming)
		{
			return Regex.Replace(incoming, @"\s", "");
		}

		// Take the incoming collection, parse through and see if there are more than
		// 1 entry (case insensitive) for the setting name. Return the setting name(s)
		// if this happens.
		public string CheckCollectionForDuplicates(BindableCollection<ConfigEntry> collection)
		{
			string results = string.Empty;

			foreach (var item in collection)
			{
				int matches = 0;
				foreach (var item2 in collection)
				{
					if (string.Equals(item.Name, item2.Name, StringComparison.OrdinalIgnoreCase))
						matches++;
				}

				// There will naturally be 1 match as an entry matches itself. Anything more is a problem...
				// Only add to results if the match hasn't been added yet (will trigger twice for duplicate, we only want one notification)
				if (matches > 1 && !(results.IndexOf(item.Name, StringComparison.OrdinalIgnoreCase) >= 0))
					results += $"{item.Name}&";
			}

			return results;
		}

		public string CheckCommentsInValueField(BindableCollection<ConfigEntry> collection)
		{
			string result = string.Empty;

			foreach (var item in collection)
			{
				if (item.Value.Contains("#"))
					result += $"\nWarning - Entry [{item.Name}] has a \"#\" character in the value field. Best practices are to keep comments in their own line, separate from values.\n";
			}

			return result;
		}

		// From hitting the SPP Browse button in settings tab
		public void SPPFolderBrowse()
		{
			// If it's empty, then it was cancelled and we keep the old setting
			string tmp = BrowseFolder();
			if (tmp != string.Empty)
				SPPFolderLocation = tmp;
		}

		// From hitting the Wow browse button in settings tab
		public void WowConfigBrowse()
		{
			// If it's empty, then it was cancelled and we keep the old setting
			string tmp = BrowseFolder();
			if (tmp != string.Empty)
				WOWConfigLocation = tmp;
		}

		// Method to browse to a folder
		public string BrowseFolder()
		{
			const string baseFolder = @"C:\";
			string result = string.Empty;
			try
			{
				VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog();
				dialog.Description = "Please select a folder.";
				dialog.UseDescriptionForTitle = true; // This applies to the Vista style dialog only, not the old dialog.
				dialog.SelectedPath = baseFolder; // place to start search
				if ((bool)dialog.ShowDialog())
					result = dialog.SelectedPath;
			}
			catch { return string.Empty; }

			return result;
		}

		// Take a collection, parse it out and save to a file path
		public async void BuildConfFile(BindableCollection<ConfigEntry> collection, string path)
		{
			int count = 0;
			string tmpstr = string.Empty;

			foreach (var item in collection)
			{
				count++;

				// Update status every x entries, otherwise it slows down
				// too much if we update the status box every time
				if (count % 20 == 0)
				{
					StatusBox = $"Updating {path} row {count} of {collection.Count}";

					// Let our UI update
					await Task.Delay(1);
				}

				// Our description may be empty for this entry, so only process
				// it if it has something in it and add to the temp string
				if (item.Description.Length > 1)
					tmpstr += item.Description;

				// If we have data for a setting entry, then add it
				// to the temp string. Every setting = value entry
				// will end in a new line
				if (item.Name.Length > 1 && item.Value.Length > 0)
					tmpstr += $"{item.Name} = {item.Value}\n";
			}

			// flush to file, now that we've finished processing
			ExportToFile(path, tmpstr, false);

			// Clear our statusbox once we're done
			StatusBox = "";
		}

		// We're going to take the WOW config file and save, as well as
		// bnetserver.conf and worldserver.conf files based on our settings
		// in the current collections
		public void SaveConfig()
		{
			// Make sure our conf file locations are up to date in case folder changed in settings
			FindConfigPaths();

			// This should save general settings
			if (GeneralSettingsManager.GeneralSettings == null)
				Log("General Settings are empty, cannot save");
			else
				if (!GeneralSettingsManager.SaveSettings(GeneralSettingsManager.SettingsPath, GeneralSettingsManager.GeneralSettings))
				Log($"Exception saving file {GeneralSettingsManager.SettingsPath}");

			// Export to bnetserver.conf
			if (BnetConfFile == string.Empty)
				Log("BNET Export -> Config File cannot be found");
			else
			{
				if (BnetCollection == null || BnetCollection.Count == 0)
					Log("BNET Export -> Current settings are empty");
				else
				{
					// Wow config relies on bnet external address, so we only want to process
					//this if the bnet collection has something in it
					Log("Updating WoW Client config portal entry");
					UpdateWowConfig();
					BuildConfFile(BnetCollection, BnetConfFile);

					// Since we have a valid bnet collection, grab external address and
					// build, push to DB realm entry while we're here
					string clientBuild = GetValueFromCollection(BnetCollection, "Game.Build.Version");
					string realmAddress = GetValueFromCollection(BnetCollection, "LoginREST.ExternalAddress");

					Log("Updating Database Realm entry with build/IP from BNet config");
					var result = MySqlManager.MySQLQuery($"UPDATE realmlist SET address=\"{realmAddress}\",gamebuild={clientBuild} WHERE  id= 1", true);
					if (!result.Contains("ordinal"))  // I don't understand SQL, it works if this error pops up...
						Log(result);
				}
			}

			// Export to worldserver.conf
			if (WorldConfFile?.Length == 0)
				Log("WORLD Export -> Config File cannot be found");
			else
			{
				if (WorldCollection == null || WorldCollection.Count == 0)
					Log("WORLD Export -> Current settings are empty");
				else
					BuildConfFile(WorldCollection, WorldConfFile);
			}
		}

		// Take our wow config.wtf file and update the SET portal entry
		public void UpdateWowConfig()
		{
			string tmpstr = string.Empty;

			if (WowConfigFile == string.Empty)
				Log("WOW Config File cannot be found - cannot update SET portal entry");
			else
			{
				try
				{
					// Pull in our WOW config
					foreach (var item in File.ReadAllLines(WowConfigFile).ToList())
					{
						// If it's the portal entry, set it to the external address
						// and if there's something wrong with the file then nothing
						// would change anyways
						if (item.Contains("SET portal"))
							foreach (var entry in BnetCollection)
							{
								if (entry.Name.Contains("LoginREST.ExternalAddress"))
									tmpstr += $"SET portal \"{entry.Value}\"\n";
							}
						else
							// otherwise pass it along, dump blank lines
							if (item.Length > 2)
							tmpstr += item + "\n";
					}

					// flush the temp string to file, overwrite
					ExportToFile(WowConfigFile, tmpstr, false);
					StatusBox = "";
				}
				catch (Exception e)
				{
					string msg = $"Error accessing file {WowConfigFile},\nthere is a permissions problem.\nThe detailed exception is -\n{e.ToString()}";
					Log(msg);
					Alert(msg);
				}
			}
		}

		// Take our incoming file path, and the full formatted string (config)
		// that we want to save, and flush to the file
		public void ExportToFile(string path, string entry, bool append = true)
		{
			var permissionSet = new PermissionSet(PermissionState.None);
			var writePermission = new FileIOPermission(FileIOPermissionAccess.Write, path);
			permissionSet.AddPermission(writePermission);

			if (permissionSet.IsSubsetOf(AppDomain.CurrentDomain.PermissionSet))
			{
				try
				{
					// Determine filename and backup existing before overwrite
					string[] pathArray = path.Split('\\');

					// Format our backup file name with the date/time
					string backupFile = $"Backup Configs\\{DateTime.Now.ToString("yyyyMMdd_hhmmss")}.{pathArray[pathArray.Length - 1]}";
					Log($"Backing up {path} to {backupFile}");

					// Make a copy of the file we're overwriting,
					// to the backup file name we just set
					File.Copy(path, backupFile);
				}
				catch (Exception e)
				{
					string msg = $"Error backing up to {path}\n(permissions/attributes issue such as read-only)\nThe exception details are -\n{e.ToString()}";
					Log(msg);
					Alert(msg);
					return;
				}

				// Now we should have a backup, and take the incoming string entry
				// and flush it to the file path, overwriting
				try
				{
					using (StreamWriter stream = new StreamWriter(path, append))
					{
						// Clean up any double spaces to format a bit nicer, remove extra junk at end
						string tmp = entry.Replace("\n\n\n", "\n\n").TrimEnd('\n');
						stream.WriteLine(tmp);
						Log($"Wrote data to {path}");
					}
				}
				catch (Exception e)
				{
					string msg = $"Error writing to {path}\n(permissions or file attributes such as read-only)\nThe exception details -\n{e.ToString()}";
					Log(msg);
					Alert(msg);
				}
			}
			else
			{
				string msg = $"Issue with file {path}, canceling this operation. Please fix file/permission issues on this file/folder";
				Log(msg);
				Alert(msg);
			}
		}

		// Load in our saved settings (settings.json, SPP server config)
		public void LoadSettings()
		{
			// Pull in the saved settings, if any
			Log("Loading general settings");
			GeneralSettingsManager.LoadGeneralSettings();
			FindConfigPaths();

			// Pull in the default templates if they exist
			Log("Loading World/Bnet default templates");
			BnetCollectionTemplate = GeneralSettingsManager.CreateCollectionFromConfigFile("Default Templates\\bnetserver.conf");
			WorldCollectionTemplate = GeneralSettingsManager.CreateCollectionFromConfigFile("Default Templates\\worldserver.conf");

			// Pull in the SPP server configs, if the location is set correctly
			// in the general settings tab
			Log("Loading current World/Bnet config files");
			BnetCollection = GeneralSettingsManager.CreateCollectionFromConfigFile(BnetConfFile);
			WorldCollection = GeneralSettingsManager.CreateCollectionFromConfigFile(WorldConfFile);

			if (WorldCollectionTemplate.Count == 0)
				Log("WorldCollectionTemplate is empty, error loading file worldserver.conf");
			if (BnetCollectionTemplate.Count == 0)
				Log("BnetCollectionTemplate is empty, error loading file bnetserver.conf");
			if (WorldCollection.Count == 0)
				Log($"WorldConfig is empty, error loading file {WorldConfFile} -- if no configuration has been made, please hit the [Set Defaults] and [Save/Export]");
			if (BnetCollection.Count == 0)
				Log($"BnetConfig is empty, error loading file {BnetConfFile} -- if no configuration has been made, please hit the [Set Defaults] and [Save/Export]");

			// If the SPP folder or wow client location was empty, assume this is the first time running or
			// that something was deleted. Either way, user needs to know.
			if (SPPFolderLocation == string.Empty || WowConfigFile == string.Empty)
			{
				string tmp = "Hello! The location for either SPP folder or WOW config doesn't seem to be set, so if this is your first time running this app ";
				tmp += "then please go to the General App Settings tab and browse to the folder locations, then reload configs. From there you can ";
				tmp += "check the config and make any adjustments, then save/export when ready. Click the [Help/About] button for more details.";
				MessageBox.Show(tmp, "Settings Need Attention!");
			}
		}

		// Take the folder locations in settings, and try to determine the path for each config file
		public void FindConfigPaths()
		{
			// Find our world/bnet configs
			if (SPPFolderLocation?.Length == 0)
				Log("SPP Folder Location is empty, cannot find existing settings to parse.");
			else
			{
				if (File.Exists($"{SPPFolderLocation}\\worldserver.conf") || File.Exists($"{SPPFolderLocation}\\bnetserver.conf"))
				{
					WorldConfFile = $"{SPPFolderLocation}\\worldserver.conf";
					BnetConfFile = $"{SPPFolderLocation}\\bnetserver.conf";
				}
				else if (File.Exists($"{SPPFolderLocation}\\Servers\\worldserver.conf") || File.Exists($"{SPPFolderLocation}\\Servers\\bnetserver.conf") || (Directory.Exists($"{SPPFolderLocation}\\Servers")))
				{
					// Either we find the files themselves, or we found the Servers folder and we'll generate them here on saving
					// since this is the best guess given our saved path info
					WorldConfFile = $"{SPPFolderLocation}\\Servers\\worldserver.conf";
					BnetConfFile = $"{SPPFolderLocation}\\Servers\\bnetserver.conf";
				}
			}

			// Find our wow client config
			if (WOWConfigLocation?.Length == 0)
				Log("WOW Client Folder Location is empty, cannot find existing settings to parse.");
			else
			{
				if (File.Exists($"{WOWConfigLocation}\\config.wtf"))
					WowConfigFile = $"{WOWConfigLocation}\\config.wtf";
				else if (File.Exists($"{WOWConfigLocation}\\WTF\\config.wtf") || (Directory.Exists($"{WOWConfigLocation}\\WTF")))
					// Either we find the file, or we found the WTF folder and we'll assume this is it
					// since this is the best guess given our saved path info. Won't be anything to parse, though
					// if the file itself doesn't exist. Sad face...
					WowConfigFile = $"{WOWConfigLocation}\\WTF\\config.wtf";
			}
		}

		// take incoming string and append to the log. This will
		// auto update the log on the right side through xaml binding
		public void Log(string log)
		{
			LogText = ":> " + log + "\n" + LogText;
		}

		public void Alert(string message)
		{
			MessageBox.Show(message);
		}

		// If we're calling this, then we'll gather up info on settings that are related to
		// common issues, and see if there's a problem we can find
		public void CheckSPPConfig()
		{
			string result = string.Empty;

			// Prep our collections in case there's nothing in current settings
			FindConfigPaths();
			if (BnetCollection == null || BnetCollection.Count == 0)
			{
				BnetCollection = BnetCollectionTemplate;
				Log("Current Bnet settings were empty, applying defaults");
				result += "Current Bnet settings were empty, applying defaults\n";
			}
			if (WorldCollection == null || WorldCollection.Count == 0)
			{
				WorldCollection = WorldCollectionTemplate;
				Log("Current World settings were empty, applying defaults");
				result += "Current World settings were empty, applying defaults\n";
			}

			// Setup our values to test later
			string buildFromDB = MySqlManager.MySQLQuery(@"SELECT gamebuild FROM realmlist WHERE id = 1");
			string buildFromWorld = GetValueFromCollection(WorldCollection, "Game.Build.Version");
			string buildFromBnet = GetValueFromCollection(BnetCollection, "Game.Build.Version");
			string loginRESTExternalAddress = GetValueFromCollection(BnetCollection, "LoginREST.ExternalAddress");
			string loginRESTLocalAddress = GetValueFromCollection(BnetCollection, "LoginREST.LocalAddress");
			string addressFromDB = MySqlManager.MySQLQuery(@"SELECT address FROM realmlist WHERE id = 1");
			string localAddressFromDB = MySqlManager.MySQLQuery(@"SELECT localAddress FROM realmlist WHERE id = 1");
			string wowConfigPortal = string.Empty;
			string bnetBindIP = GetValueFromCollection(BnetCollection, "BindIP");
			string worldBindIP = GetValueFromCollection(WorldCollection, "BindIP");
			bool solocraft = IsOptionEnabled(WorldCollection, "Solocraft.Enable");
			bool flexcraftHealth = IsOptionEnabled(WorldCollection, "HealthCraft.Enable");
			bool flexcraftUnitMod = IsOptionEnabled(WorldCollection, "UnitModCraft.Enable");
			bool flexcraftCombatRating = IsOptionEnabled(WorldCollection, "Combat.Rating.Craft.Enable");
			bool bpay = IsOptionEnabled(WorldCollection, "Bpay.Enabled");
			bool purchaseShop = IsOptionEnabled(WorldCollection, "Purchase.Shop.Enabled");
			bool battleCoinVendor = IsOptionEnabled(WorldCollection, "Battle.Coin.Vendor.Enable");
			bool battleCoinVendorCustom = IsOptionEnabled(WorldCollection, "Battle.Coin.Vendor.Custom.Enable");
			bool gridUnload = IsOptionEnabled(WorldCollection, "GridUnload");
			bool baseMapLoadAllGrids = IsOptionEnabled(WorldCollection, "BaseMapLoadAllGrids");
			bool instanceMapLoadAllGrids = IsOptionEnabled(WorldCollection, "InstanceMapLoadAllGrids");
			bool disallowMultipleClients = IsOptionEnabled(WorldCollection, "Disallow.Multiple.Client");
			bool customHurtRealTime = IsOptionEnabled(WorldCollection, "Custom.HurtInRealTime");
			bool customNoCastTime = IsOptionEnabled(WorldCollection, "Custom.NoCastTime");
			bool worldChat = IsOptionEnabled(WorldCollection, "WorldChat.Enable");
			bool characterTemplate = IsOptionEnabled(WorldCollection, "Character.Template");

			// If we just applied defaults, and there's still nothing, then something went wrong... missing templates?
			if (BnetCollection.Count == 0 || WorldCollection.Count == 0)
				Log("Alert - There's an issue with collection(s) being empty.. possibly missing template files");
			else
			{
				// Compare bnet to default - any missing/extra items?
				foreach (var item in BnetCollectionTemplate)
				{
					if (CheckCollectionForMatch(BnetCollection, item.Name) == false)
					{
						result += $"Warning - [{item.Name}] exists in Bnet-Template, but not in current settings. Adding entry (will need to save/export afterwards to save)\n\n";
						BnetCollection.Add(item);
					}
				}

				// Check existing bnet entries, and see if the template has it. If not, could be an issue
				foreach (var item in BnetCollection)
					if (CheckCollectionForMatch(BnetCollectionTemplate, item.Name) == false)
						result += $"Warning - [{item.Name}] exists in current Bnet settings, but not in template. Please verify whether this entry is needed any longer.\n\n";

				// Compare world to default - any missing/extra items
				foreach (var item in WorldCollectionTemplate)
				{
					if (CheckCollectionForMatch(WorldCollection, item.Name) == false)
					{
						result += $"Warning - [{item.Name}] exists in World-Template, but not in current settings. Adding entry (will need to save/export afterwards to save)\n\n";
						WorldCollection.Add(item);
					}
				}

				// Check existing world entries, see if anything exists that isn't in the template.
				foreach (var item in WorldCollection)
					if (CheckCollectionForMatch(WorldCollectionTemplate, item.Name) == false)
						result += $"Warning - [{item.Name}] exists in current World settings, but not in template. Please verify whether this entry is needed any longer.\n\n";

				// Compare build# between bnet/world/realm
				if (buildFromBnet != buildFromDB || buildFromBnet != buildFromWorld)
				{
					result += $"Build from DB Realm - {buildFromDB}\n";
					result += $"Build from WorldConfig - {buildFromWorld}\n";
					result += $"Build from BnetConfig - {buildFromBnet}\n";
					result += "Alert - There is a [Game.Build.Version] mismatch between configs and database. Please use the \"Set Build\" button to fix, then save/export.\n\n";
				}
				else
					result += "Game.Build.Version numbers match, this is good!\n\n";

				// Compare IP bindings for listening - these really never need to change
				if (!worldBindIP.Contains("0.0.0.0") || !bnetBindIP.Contains("0.0.0.0"))
				{
					result += $"World BindIP - {worldBindIP}\n";
					result += $"Bnet BindIP - {bnetBindIP}\n";
					result += "Alert - Both World and Bnet BindIP setting should be \"0.0.0.0\"\n\n";
				}
				else
					result += "BindIP settings match and are set properly.\n\n";

				// Gather WoW portal IP from config.wtf
				if (File.Exists(WowConfigFile) == false)
				{
					Log("WOW Config File cannot be found - cannot parse SET portal entry");
					result += "Alert - WOW Config file not found, cannot check [SET portal] entry to compare.\n\n";
				}
				else
				{
					// Pull in our WOW config
					List<string> allLinesText = File.ReadAllLines(WowConfigFile).ToList();

					if (allLinesText.Count < 2)
						Log($"Warning - WoW Client config file [{WowConfigFile}] may be empty.");

					foreach (var item in allLinesText)
					{
						// If it's the portal entry, process further
						// split by " and 2nd item will be IP
						if (item.Contains("SET portal"))
						{
							string[] phrase = item.Split('"');
							wowConfigPortal = phrase[1];
						}
					}
				}

				// List our external/hosting IP settings
				if ((loginRESTExternalAddress != addressFromDB) || (loginRESTExternalAddress != wowConfigPortal))
				{
					result += $"LoginREST.ExternalAddress - {loginRESTExternalAddress}\n";
					result += $"Address from DB Realm - {addressFromDB}\n";
					result += $"Wow config PORTAL entry - {wowConfigPortal}\n";
					result += "Alert - All of these addresses should match. Use the \"Set IP\" button to set.\n\n";
				}
				else
					result += "IP settings for hosting all match, this is good!\n\n";

				// Check the local (not external hosting) IP settings. These don't need to change from 127.0.0.1 (localhost)
				if (!loginRESTLocalAddress.Contains("127.0.0.1") || !localAddressFromDB.Contains("127.0.0.1"))
				{
					result += $"LoginREST.LocalAddress - {loginRESTLocalAddress}\n";
					result += $"local Address from DB - {localAddressFromDB}\n";
					result += "Alert - both of these addresses should match, and probably both be set to 127.0.0.1\n\n";
				}

				// Check our solocraft settings compared to FlexCraft entries
				// If both are enabled, this is a problem
				if (solocraft)
				{
					if (flexcraftHealth)
						result += "Alert - Solocraft and HealthCraft are both enabled! This will cause conflicts. Disabling Solocraft recommended.\n\n";

					if (flexcraftUnitMod)
						result += "Alert - Solocraft and UnitModCraft are both enabled! This will cause conflicts. Disabling Solocraft recommended.\n\n";

					if (flexcraftCombatRating)
						result += "Alert - Solocraft and Combat.Rating.Craft are both enabled! This will cause conflicts. Disabling Solocraft recommended.\n\n";
				}

				// Check for battle shop entries
				if (bpay != purchaseShop)
					result += $"Alert - Bpay.Enabled and Purchase.Shop.Enabled should BOTH either be disabled or enabled together in the world config.\n\n";

				// check for both battlecoin.vendor.enable and battlecoin.vendor.custom.enable (should only be 1 enabled)
				if (battleCoinVendor && battleCoinVendorCustom)
					result += $"Alert - Battle.Coin.Vendor.Enable and Battle.Coin.Vendor.CUSTOM.Enable are both enabled - only one needs enabled in the world config.\n\n";

				// Character Template doesn't work with client build 26972
				if (characterTemplate && buildFromWorld == "26972")
					result += "Warning - Character Template does not work with client build 26972. Please set Character.Template to 0 if using this build for best results.\n\n";

				// World Chat, can crash if disabled
				if (!worldChat)
					result += "Warning - WorldChat.Enable = 0. You may want to enable this option, or it could crash the server when using any commands. This will also disable all commands, including for GM.\n\n";

				// Warn about grid related settings
				if (baseMapLoadAllGrids || instanceMapLoadAllGrids)
					result += "Warning - BaseMapLoadAllGrids and InstanceMapLoadAllGrids should be set to 0. If the worldserver crashes on loading maps or runs out of memory, this may be why.\n\n";
				if (gridUnload == false)
					result += $"Warning - GridUnload may need set to 1 to unload unused map grids and release memory. If the server runs out of memory, or crashes with high usage, this may be why.\n\n";

				// Notify if Disallow.Multiple.Client is enabled
				if (disallowMultipleClients)
				{
					result += "Warning - You have Disallow.Multiple.Client set to 1. This will disable multiple client connections from your local network, so if you plan on ";
					result += "playing multiple client sessions at once, or multiple users on the same network, then this needs set to 0.\n\n";
				}

				if (customHurtRealTime)
					result += "Note - You have Custom.HurtInRealTime = 1 and means you click every time to swing a weapon. To change to auto-attack, set this entry to 0\n\n";

				if (customNoCastTime)
					result += "Note - you have Custom.NoCastTime = 1 and may cause unintended effects when casting. Set entry to 0 if you need that to change\n\n";

				// Check collections for duplicate entries, and strip out the &
				// at the end of the string. This will leave the final as listing
				// [entry1&entry2&entry3] for the feedback
				string tmp1 = CheckCollectionForDuplicates(BnetCollection).TrimEnd('&');
				string tmp2 = CheckCollectionForDuplicates(WorldCollection).TrimEnd('&');

				// If there were duplicates, list them
				if (tmp1 != string.Empty)
					result += $"Alert - Duplicate entries found in [BnetConfig] for [{tmp1}]\n\n";
				if (tmp2 != string.Empty)
					result += $"Alert - Duplicate entries found in [WorldConfig] for [{tmp2}]\n\n";

				// Check if any settings have a value field containing comments. It won't break anything
				// but can definitely make it harder to parse through and is not a good practice
				tmp1 = CheckCommentsInValueField(BnetCollection);
				tmp2 = CheckCommentsInValueField(WorldCollection);

				if (tmp1 != string.Empty)
					result += tmp1;
				if (tmp2 != string.Empty)
					result += tmp2;

				// Build our final response based on any alert/warnings found
				if (result.Contains("Alert"))
					result += "\n\nAlert - Issues were found!";
				else if (result.Contains("Warning"))
					result += "\n\nWarnings were found, this could impact server stability or performance and those settings may need changed.\n\n";
				else
					result += "\n\nNo known problems were found!";

				// Take our final list of results and send to the user
				MessageBox.Show(result);
			}
		}
	}
}