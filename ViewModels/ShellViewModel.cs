using Caliburn.Micro;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SPP_Config_Generator
{
	public class ShellViewModel : Screen
	{
		public string AppTitle { get; set; } = $"SPP Config Generator v{Assembly.GetExecutingAssembly().GetName().Version.ToString()}";
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
		public BindableCollection<ConfigEntry> WorldCollectionTemplate { get; set; } = new BindableCollection<ConfigEntry>();
		public BindableCollection<ConfigEntry> BnetCollectionTemplate { get; set; } = new BindableCollection<ConfigEntry>();
		public BindableCollection<ConfigEntry> WorldCollection { get; set; } = new BindableCollection<ConfigEntry>();
		public BindableCollection<ConfigEntry> BnetCollection { get; set; } = new BindableCollection<ConfigEntry>();
		public string WowConfigFile { get; set; } = string.Empty;
		public string BnetConfFile { get; set; } = string.Empty;
		public string WorldConfFile { get; set; } = string.Empty;
		public string StatusBox { get; set; }
		public string LogText { get; set; }

		public ShellViewModel()
		{
			Log("App Initializing...");
			
			// Pull in saved settings, adjust window position if needed
			Log("Loading settings");
			LoadSettings();
			
			if (SPPFolderLocation == string.Empty)
				StatusBox = "Please set SPP Location in the General Settings tab";

			Log("Set Window position/width/height, moving into view");
			GeneralSettingsManager.MoveIntoView();
		}

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

		public void SetIP()
		{
			string input = string.Empty;
			// Check if there are valid targets for spp/wow config, sql - report if any missing
			// As long as we set Bnet REST IP first, then WoW config will be updated as well
			input = Microsoft.VisualBasic.Interaction.InputBox("Enter the Listening/Hosted IP Address to set. If this is to be hosted for local network then use the LAN ipv4 address. If external hosting, use the WAN address. Note this entry will not be validated to accuracy.", "Set IP", "127.0.0.1");

			// If user hit didn't cancel or enter something stupid...
			// length > 6 is at least 4 numbers for an IP, and 3 . within an IP
			if (input.Length > 6)
			{
				// Update Bnet entry
				BnetCollection = UpdateConfigCollection(BnetCollection, "LoginREST.ExternalAddress", input);

				// Update database realm entry
				var result = MySqlManager.MySQLQuery($"UPDATE realmlist SET address = '{input}' WHERE id = 1");
				if (!result.Contains("ordinal"))  // I don't understand SQL, it works if this error pops up...
					Log(result);

				// Update Wow config portal entry
				UpdateWowConfig();
			}
		}

		public void SetBuild()
		{
			string input = string.Empty;

			// Grab the input
			input = Microsoft.VisualBasic.Interaction.InputBox("Enter the 7.3.5 (xxxxx) build from your client. Available builds: 26124, 26365, 26654, 26822, 26899, or 26972", "Set Build", "26972");

			// If user hit didn't cancel or enter something stupid...
			// all build numbers are 5 total chars
			if (input.Length == 5)
			{
				// Update Bnet entry
				BnetCollection = UpdateConfigCollection(BnetCollection, "Game.Build.Version", input);

				// Update World entry
				WorldCollection = UpdateConfigCollection(WorldCollection, "Game.Build.Version", input);

				// Update database realm entry
				var result = MySqlManager.MySQLQuery($"UPDATE realmlist SET gamebuild = '{input}' WHERE id = 1");
				if (!result.Contains("ordinal"))  // I don't understand SQL, it works if this error pops up...
					Log(result);
			}
		}

		public void SetDefaults()
		{
			// Do we do anything other than drop template collection onto world/bnet saved ones?
			if (WorldCollectionTemplate == null)
				Log("World Template is null, cannot set defaults.");
			else
				WorldCollection = WorldCollectionTemplate;

			if (BnetCollectionTemplate == null)
				Log("Bnet Template is null, cannot set defaults.");
			else
				BnetCollection = BnetCollectionTemplate;
		}

		public bool CheckCollectionForMatch(BindableCollection<ConfigEntry> collection, string searchValue)
		{
			bool match = false;

			foreach (var item in collection)
			{
				if (string.Equals(NormalizeString(item.Name), NormalizeString(searchValue), StringComparison.OrdinalIgnoreCase))
				{
					// Found a match, can stop checking this round
					match = true;
					break;
				}
			}

			return match;
		}

		public string GetValueFromCollection(BindableCollection<ConfigEntry> collection, string searchValue)
		{
			string result = string.Empty;

			// Populate from Bnet collection
			if (collection != null)
				foreach (var item in collection)
					if (string.Equals(NormalizeString(item.Name), NormalizeString(searchValue), StringComparison.OrdinalIgnoreCase))
						result = item.Value;

			return NormalizeString(result);
		}

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

		public string CheckCollectionForDuplicates(BindableCollection<ConfigEntry> collection)
		{
			string results = string.Empty;

			foreach (var item in collection)
			{
				int matches = 0;
				foreach (var item2 in collection)
				{
					if (item.Name == item2.Name)
						matches++;
				}

				// There will naturally be 1 match as an entry matches itself. Anything more is a problem...
				// Only add to results if the match hasn't been added yet (will trigger twice for duplicate, we only want one notification)
				if (matches > 1)
					if (!results.Contains(item.Name))
						results += $"{item.Name}&";
			}

			return results;
		}

		public void CheckSPPConfig()
		{
			// Prep our collections in case there's nothing in current settings
			FindConfigPaths();
			if (BnetCollection == null || BnetCollection.Count == 0)
			{
				BnetCollection = BnetCollectionTemplate;
				Log("Current Bnet settings were empty, applying defaults");
			}
			if (WorldCollection == null || WorldCollection.Count == 0)
			{
				WorldCollection = WorldCollectionTemplate;
				Log("Current World settings were empty, applying defaults");
			}

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
			string result = string.Empty;
			bool solocraft = IsOptionEnabled(WorldCollection, "Solocraft.Enable");
			bool flexcraftHealth = IsOptionEnabled(WorldCollection, "HealthCraft.Enable");
			bool flexcraftUnitMod = IsOptionEnabled(WorldCollection, "UnitModCraft.Enable");
			bool flexcraftCombatRating = IsOptionEnabled(WorldCollection, "Combat.Rating.Craft.Enable");
			bool bpay = IsOptionEnabled(WorldCollection, "Bpay.Enabled");
			bool purchaseShop = IsOptionEnabled(WorldCollection, "Purchase.Shop.Enabled");
			bool battleCoinVendor = IsOptionEnabled(WorldCollection, "Battle.Coin.Vendor.Enable");
			bool battleCoinVendorCustom = IsOptionEnabled(WorldCollection, "Battle.Coin.Vendor.Custom.Enable");

			// If we just applied defaults, and there's still nothing, then something went wrong... missing templates?
			if (BnetCollection.Count == 0 || WorldCollection.Count == 0)
				Log("Alert - There's an issue with collection(s) being empty.. possibly missing template files");
			else
			{
				// Compare bnet to default - any missing/extra items?
				result += "\nChecking Bnet config compared to template...\n";

				foreach (var item in BnetCollectionTemplate)
				{
					if (CheckCollectionForMatch(BnetCollection, item.Name) == false)
					{
						result += $"Alert - [{item.Name}] exists in Bnet-Template, but not in current settings. Adding entry (will need to save/export afterwards to save)\n";
						BnetCollection.Add(item);
					}
				}

				foreach (var item in BnetCollection)
					if (CheckCollectionForMatch(BnetCollectionTemplate, item.Name) == false)
						result += $"Alert - [{item.Name}] exists in current Bnet settings, but not in template. Please verify whether this entry is needed any longer.\n";

				// Compare world to default - any missing/extra items?
				result += "\nChecking World config compared to template...\n";

				foreach (var item in WorldCollectionTemplate)
				{
					if (CheckCollectionForMatch(WorldCollection, item.Name) == false)
					{
						result += $"Alert - [{item.Name}] exists in World-Template, but not in current settings. Adding entry (will need to save/export afterwards to save)\n";
						WorldCollection.Add(item);
					}
				}

				foreach (var item in WorldCollection)
					if (CheckCollectionForMatch(WorldCollectionTemplate, item.Name) == false)
						result += $"Alert - [{item.Name}] exists in current World settings, but not in template. Please verify whether this entry is needed any longer.\n";

				// Compare build# between bnet/world/realm
				result += $"\nBuild from DB Realm - {buildFromDB}\n";
				result += $"Build from WorldConfig - {buildFromWorld}\n";
				result += $"Build from BnetConfig - {buildFromBnet}\n";
				if (buildFromBnet != buildFromDB || buildFromBnet != buildFromWorld)
					result += "Alert - There is a [Game.Build.Version] mismatch between configs and database. Please use the \"Set Build\" button to fix, then save/export.\n";

				// Compare IP bindings
				result += $"\nWorld BindIP - {worldBindIP}\n";
				result += $"Bnet BindIP - {bnetBindIP}\n";
				if (!worldBindIP.Contains("0.0.0.0") || !bnetBindIP.Contains("0.0.0.0"))
					result += "Alert - Both World and Bnet BindIP setting should be \"0.0.0.0\"\n";

				// Compare listening IPs between bnet/world/realm/wow config
				result += $"\nLoginREST.ExternalAddress - {loginRESTExternalAddress}\n";
				result += $"Address from DB Realm - {addressFromDB}\n";

				// Gather WoW portal IP from config.wtf
				if (File.Exists(WowConfigFile) == false)
				{
					Log("WOW Config File cannot be found - cannot parse SET portal entry");
					result += "Alert - WOW Config file not found, cannot check [SET portal] entry to compare.\n";
				}
				else
				{
					// Pull in our WOW config
					List<string> allLinesText = File.ReadAllLines(WowConfigFile).ToList();

					if (allLinesText.Count < 2)
						Log($"Alert - WoW Client config file [{WowConfigFile}] may be empty.");

					foreach (var item in allLinesText)
					{
						// If it's the portal entry, process further
						// split by " and 2nd item will be IP
						if (item.Contains("SET portal"))
						{
							string[] phrase = item.Split('"');
							wowConfigPortal = phrase[1];
							result += $"WoW config.wtf Set Portal IP - {wowConfigPortal}\n";
						}
					}
				}

				if (loginRESTExternalAddress != addressFromDB || loginRESTExternalAddress != wowConfigPortal)
					result += "Alert - All of these addresses should match. Set these to the Local/LAN/WAN IP depending on hosting goals.\n";

				result += $"\nLoginREST.LocalAddress - {loginRESTLocalAddress}\n";
				result += $"local Address from DB - {localAddressFromDB}\n";
				if (!loginRESTLocalAddress.Contains("127.0.0.1") || !localAddressFromDB.Contains("127.0.0.1"))
					result += "Alert - both of these addresses should match, and probably both be set to 127.0.0.1\n";

				if (solocraft)
				{
					if (flexcraftHealth)
						result += "\nAlert - Solocraft and HealthCraft are both enabled! This will cause conflicts. Disabling Solocraft recommended.\n";

					if (flexcraftUnitMod)
						result += "\nAlert - Solocraft and UnitModCraft are both enabled! This will cause conflicts. Disabling Solocraft recommended.\n";

					if (flexcraftCombatRating)
						result += "\nAlert - Solocraft and Combat.Rating.Craft are both enabled! This will cause conflicts. Disabling Solocraft recommended.\n";
				}

				// Check for battle shop entries
				if (bpay != purchaseShop)
					result += $"\nAlert - Bpay.Enabled is {bpay}, and Purchase.Shop.Enabled is {purchaseShop} - both should either be disabled or enabled together.\n";

				// check for both battlecoin.vendor.enable and battlecoin.vendor.custom.enable (should only be 1 enabled)
				if (battleCoinVendor && battleCoinVendorCustom)
					result += $"\nAlert - Battle.Coin.Vendor.Enable is {battleCoinVendor}, and Battle.Coin.Vendor.CUSTOM.Enable is {battleCoinVendorCustom} - only one needs enabled.\n";

				// Check collections for duplicate entries
				result += "\nChecking for duplicates in world/bnet config\n";
				string tmp1 = CheckCollectionForDuplicates(BnetCollection).TrimEnd('&');
				string tmp2 = CheckCollectionForDuplicates(WorldCollection).TrimEnd('&');

				if (tmp1 != string.Empty)
					result += $"\nAlert - Duplicate entries found in [BnetConfig] for [{tmp1}]";
				if (tmp2 != string.Empty)
					result += $"\nAlert - Duplicate entries found in [WorldConfig] for [{tmp2}]";

				// Anything else?
				if (result.Contains("Alert"))
					result += "\n\nAlert - Issues were found!";
				else
					result += "\n\nNo known problems were found!";
				MessageBox.Show(result);
			}
		}

		public void SPPFolderBrowse()
		{
			// If it's empty, then it was cancelled and we keep the old setting
			string tmp = BrowseFolder();
			if (tmp != string.Empty)
				SPPFolderLocation = tmp;
		}

		public void WowConfigBrowse()
		{
			// If it's empty, then it was cancelled and we keep the old setting
			string tmp = BrowseFolder();
			if (tmp != string.Empty)
				WOWConfigLocation = tmp;
		}

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

		public async void BuildConfFile(BindableCollection<ConfigEntry> collection, string path)
		{
			int count = 0;
			string tmpstr = string.Empty;

			foreach (var item in collection)
			{
				count++;

				// Update status every x entries
				if (count % 5 == 0)
				{
					StatusBox = $"Updating {path} row {count} of {collection.Count}";

					// Let our UI update
					await Task.Delay(1);
				}

				if (item.Description.Length > 1)
					tmpstr += item.Description;
				if (item.Name.Length > 1 && item.Value.Length > 0)
					tmpstr += $"{item.Name} = {item.Value}\n";
			}

			// flush to file
			ExportToFile(path, tmpstr, false);
			StatusBox = "";
		}

		public void SaveConfig()
		{
			// Make sure our conf file locations are up to date in case folder changed in settings
			FindConfigPaths();

			// This should save general settings, and also current configs for world/bnet
			if (GeneralSettingsManager.GeneralSettings == null)
				Log("General Settings are empty, cannot save");
			else
				if (!GeneralSettingsManager.SaveSettings(GeneralSettingsManager.SettingsPath, GeneralSettingsManager.GeneralSettings))
				Log($"Exception saving file {GeneralSettingsManager.SettingsPath}");

			// Export to BNET
			if (BnetConfFile == string.Empty)
				Log("BNET Export -> Config File cannot be found");
			else
			{
				if (BnetCollection == null || BnetCollection.Count == 0)
					Log("BNET Export -> Current settings are empty");
				else
				{
					// Wow config relies on bnet external address
					UpdateWowConfig();
					BuildConfFile(BnetCollection, BnetConfFile);
				}
			}

			// Export to World - config starts with [worldserver]
			if (WorldConfFile == string.Empty)
				Log("WORLD Export -> Config File cannot be found");
			else
			{
				if (WorldCollection == null || WorldCollection.Count == 0)
					Log("WORLD Export -> Current settings are empty");
				else
					BuildConfFile(WorldCollection, WorldConfFile);
			}
		}

		public void UpdateWowConfig()
		{
			string tmpstr = string.Empty;

			if (WowConfigFile == string.Empty)
				Log("WOW Config File cannot be found - cannot update SET portal entry");
			else
			{
				// Pull in our WOW config
				List<string> allLinesText = File.ReadAllLines(WowConfigFile).ToList();

				foreach (var item in allLinesText)
				{
					// If it's the portal entry, set it to the external address
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
		}

		public void ExportToFile(string path, string entry, bool append = true)
		{
			try
			{
				// Determine filename and backup existing before overwrite
				string[] pathArray = path.Split('\\');
				string backupFile = $"Backup Configs\\{DateTime.Now.ToString("yyyyMMdd_hhmmss")}.{pathArray[pathArray.Length - 1]}";
				Log($"Backing up {path} to {backupFile}");
				File.Copy(path, backupFile);
			}
			catch (Exception e) { Log($"Error backing up to {path}, exception {e.ToString()}"); }

			using (StreamWriter stream = new StreamWriter(path, append))
			{
				try
				{
					stream.WriteLine(entry);
					Log($"Wrote data to {path}");
				}
				catch (Exception e) { Log($"Error writing to {path}, exception {e.ToString()}"); }
			}
		}

		public async void LoadSettings()
		{
			StatusBox = "Please wait, loading general settings...";
			// Pull in the saved settings, if any
			Log("Loading general settings");
			GeneralSettingsManager.LoadGeneralSettings();

			// This await should let the GUI size/position settings apply before moving forward
			await Task.Delay(1);
			FindConfigPaths();

			Log("Loading World/Bnet default templates");
			StatusBox = "Please wait, loading bnet template...";
			await Task.Delay(1);
			BnetCollectionTemplate = GeneralSettingsManager.CreateCollectionFromConfigFile("Default Templates\\bnetserver.conf");

			StatusBox = "Please wait, loading world template...";
			await Task.Delay(1);
			WorldCollectionTemplate = GeneralSettingsManager.CreateCollectionFromConfigFile("Default Templates\\worldserver.conf");

			Log("Loading current World/Bnet config files");
			StatusBox = "Please wait, loading current bnetserver.conf...";
			await Task.Delay(1);
			BnetCollection = GeneralSettingsManager.CreateCollectionFromConfigFile(BnetConfFile);

			StatusBox = "Please wait, loading current worldserver.conf...";
			await Task.Delay(1);
			WorldCollection = GeneralSettingsManager.CreateCollectionFromConfigFile(WorldConfFile);

			StatusBox = "";
			if (WorldCollectionTemplate.Count == 0)
				Log("WorldCollectionTemplate is empty, error loading file worldserver.conf");
			if (BnetCollectionTemplate.Count == 0)
				Log("BnetCollectionTemplate is empty, error loading file bnetserver.conf");
			if (WorldCollection.Count == 0)
				Log($"WorldConfig is empty, error loading file {WorldConfFile} -- if no configuration has been made, please hit the [Set Defaults] and [Save/Export]");
			if (BnetCollection.Count == 0)
				Log($"BnetConfig is empty, error loading file {BnetConfFile} -- if no configuration has been made, please hit the [Set Defaults] and [Save/Export]");

			if (SPPFolderLocation == string.Empty || WowConfigFile == string.Empty)
				MessageBox.Show("Hello! The location for either SPP folder or WOW config doesn't seem to exist, so if this is your first time running this app then please go to the General App Settings tab and set the folder locations, then save/export.", "Settings Need Attention!");
		}

		// Take the folder locations in settings, and try to determine the path for each config file
		public void FindConfigPaths()
		{
			// Find our world/bnet configs
			if (SPPFolderLocation == string.Empty)
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
			if (WOWConfigLocation == string.Empty)
				Log("WOW Client Folder Location is empty, cannot find existing settings to parse.");
			else
			{
				if (File.Exists($"{WOWConfigLocation}\\config.wtf"))
					WowConfigFile = $"{WOWConfigLocation}\\config.wtf";
				else if (File.Exists($"{WOWConfigLocation}\\WTF\\config.wtf") || (Directory.Exists($"{WOWConfigLocation}\\WTF")))
					// Either we find the file, or we found the WTF folder and we'll assume this is it
					// since this is the best guess given our saved path info. Won't be anything to parse, though
					WowConfigFile = $"{WOWConfigLocation}\\WTF\\config.wtf";
			}
		}

		public void Log(string log)
		{
			LogText = ":> " + log + "\n" + LogText;
		}
	}
}