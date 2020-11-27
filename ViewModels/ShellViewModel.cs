using Caliburn.Micro;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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
		public string HelpAbout { get; set; } = string.Empty;
		public string StatusBox { get; set; }
		public string LogText { get; set; }

		// To-Do 
		// Check bnet/world collections for duplicate entries

		public ShellViewModel()
		{
			Log("App Initializing...");
			// Pull in saved settings, adjust window position if needed
			Log("Loading settings");
			LoadSettings();
			PopulateHelp();
			Log("Set Window position/width/height, moving into view");
			GeneralSettingsManager.MoveIntoView();
		}

		public BindableCollection<ConfigEntry> UpdateConfigCollection(BindableCollection<ConfigEntry> collection, string entry, string value)
		{
			foreach (var item in collection)
			{
				if (string.Equals(item.Name, entry, StringComparison.OrdinalIgnoreCase))
				{
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
			// length > 7 is 4 at least 4 numbers for an IP, and 3 . within an IP
			if (input.Length > 7)
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
			foreach (var item in collection)
				if (string.Equals(NormalizeString(item.Name), NormalizeString(searchValue), StringComparison.OrdinalIgnoreCase))
					result = item.Value;

			return NormalizeString(result);
		}

		public bool IsOptionEnabled(BindableCollection<ConfigEntry> collection, string searchValue)
		{
			bool result = false;

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

				// There will naturally be 1 match as an entry matches itself. Anything more is a problem
				// only add if the match has been added (will trigger twice for duplicate, we only want one notification)
				if (matches > 1)
					if (!results.Contains(item.Name))
						results += $"{item.Name}&";
			}

			return results;
		}

		public void CheckSPPConfig()
		{
			string buildFromDB = MySqlManager.MySQLQuery(@"SELECT gamebuild FROM realmlist WHERE id = 1");
			string buildFromWorld = GetValueFromCollection(WorldCollection, "Game.Build.Version");
			string buildFromBnet = GetValueFromCollection(BnetCollection, "Game.Build.Version");
			string loginRESTExternalAddress = GetValueFromCollection(BnetCollection, "LoginREST.ExternalAddress");
			string loginRESTLocalAddress = GetValueFromCollection(BnetCollection, "LoginREST.LocalAddress");
			string addressFromDB = MySqlManager.MySQLQuery(@"SELECT address FROM realmlist WHERE id = 1");
			string localAddressFromDB = MySqlManager.MySQLQuery(@"SELECT localAddress FROM realmlist WHERE id = 1");
			string wowConfigPortal = string.Empty;
			string wowConfigFile = GetWowConfigFile();
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

			// If we just applied defaults, and there's still nothing, then something went wrong... missing templates?
			if ((BnetCollection == null || BnetCollection.Count == 0) || (WorldCollection == null || WorldCollection.Count == 0))
				Log("Alert - There's an issue with collection(s) bring empty.. possibly missing template files");
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

				if (wowConfigFile == string.Empty)
					Log("WOW Config File cannot be found - cannot parse SET portal entry");
				else
				{
					// Pull in our WOW config
					List<string> allLinesText = File.ReadAllLines(wowConfigFile).ToList();

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
			SPPFolderLocation = BrowseFolder();
		}

		public void WowConfigBrowse()
		{
			WOWConfigLocation = BrowseFolder();
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

		public async void ExportToConfFile(BindableCollection<ConfigEntry> collection, string path)
		{
			int count = 0;
			string tmpstr = string.Empty;

			foreach (var item in collection)
			{
				count++;
				StatusBox = $"Updating BNET row {count} of {collection.Count}";

				// Let our UI update
				await Task.Delay(1);

				if (item.Description.Length > 1)
					tmpstr += item.Description;
				if (item.Name.Length > 1 && item.Value.Length > 0)
					tmpstr += $"{item.Name} = {item.Value}\n";
			}

			// flush to file
			ExportToConfig(path, tmpstr, false);
		}

		public void SaveConfig()
		{
			string worldConfigFile = string.Empty;
			string bnetConfigFile = string.Empty;

			// This should save general settings, and also current configs for world/bnet
			if (GeneralSettingsManager.GeneralSettings == null)
				Log("General Settings are empty, cannot save");
			else
				if (!GeneralSettingsManager.SaveSettings(GeneralSettingsManager.SettingsPath, GeneralSettingsManager.GeneralSettings))
				Log($"Exception saving file {GeneralSettingsManager.SettingsPath}");

			if (WorldCollection == null || WorldCollection.Count == 0)
				Log("Cannot save WorldConfig, current settings are empty");
			else
				if (!GeneralSettingsManager.SaveSettings(GeneralSettingsManager.WorldConfigPath, WorldCollection))
				Log($"Exception saving file {GeneralSettingsManager.WorldConfigPath}");

			if (BnetCollection == null || BnetCollection.Count == 0)
				Log("Cannot save BnetConfig, current settings are empty");
			else
				if (!GeneralSettingsManager.SaveSettings(GeneralSettingsManager.BNetConfigPath, BnetCollection))
				Log($"Exception saving file {GeneralSettingsManager.BNetConfigPath}");

			// Run config checks function (will report non-matching build, IPs, etc)

			// Find our actual config folder for SPP
			if (File.Exists($"{SPPFolderLocation}\\Servers\\bnetserver.conf"))
				bnetConfigFile = $"{SPPFolderLocation}\\Servers\\bnetserver.conf";
			if (File.Exists($"{SPPFolderLocation}\\bnetserver.conf"))
				bnetConfigFile = $"{SPPFolderLocation}\\bnetserver.conf";
			if (File.Exists($"{SPPFolderLocation}\\Servers\\worldserver.conf"))
				worldConfigFile = $"{SPPFolderLocation}\\Servers\\worldserver.conf";
			if (File.Exists($"{SPPFolderLocation}\\worldserver.conf"))
				worldConfigFile = $"{SPPFolderLocation}\\worldserver.conf";

			// Export to BNET
			if (bnetConfigFile == string.Empty)
				Log("BNET Export -> Config File cannot be found");
			else
				ExportToConfFile(BnetCollection, bnetConfigFile);

			// Export to World - config starts with [worldserver]
			if (worldConfigFile == string.Empty)
				Log("WORLD Export -> Config File cannot be found");
			else
				ExportToConfFile(WorldCollection, worldConfigFile);

			UpdateWowConfig();
			StatusBox = "Export Complete";
		}

		public string GetWowConfigFile()
		{
			string wowConfigFile = string.Empty;

			// Find the exact file we need
			if (File.Exists($"{WOWConfigLocation}\\WTF\\config.wtf"))
				wowConfigFile = $"{WOWConfigLocation}\\WTF\\config.wtf";

			if (File.Exists($"{WOWConfigLocation}\\config.wtf"))
				wowConfigFile = $"{WOWConfigLocation}\\config.wtf";

			return wowConfigFile;
		}

		public async void UpdateWowConfig()
		{
			string tmpstr = string.Empty;
			string wowConfigFile = GetWowConfigFile();
			int count = 0;

			if (wowConfigFile == string.Empty)
				Log("WOW Config File cannot be found - cannot update SET portal entry");
			else
			{
				// Pull in our WOW config
				List<string> allLinesText = File.ReadAllLines(wowConfigFile).ToList();

				foreach (var item in allLinesText)
				{
					count++;
					StatusBox = $"Updating WOWCONFIG row {count} of {allLinesText.Count}";

					// Let our UI update
					await Task.Delay(1);

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
				ExportToConfig(wowConfigFile, tmpstr, false);
			}
		}

		public void ExportToConfig(string path, string entry, bool append = true)
		{
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

		public void LoadSettings()
		{
			// Pull in the saved settings, if any
			Log("Loading general settings");
			GeneralSettingsManager.LoadGeneralSettings();

			Log("Loading World/Bnet default templates from .conf files");
			BnetCollectionTemplate = GeneralSettingsManager.CreateDefaultTemplateFromFile("bnetserver.conf");
			WorldCollectionTemplate = GeneralSettingsManager.CreateDefaultTemplateFromFile("worldserver.conf");

			Log("Loading World/Bnet saved settings");
			WorldCollection = GeneralSettingsManager.LoadSettings(GeneralSettingsManager.WorldConfigPath);
			BnetCollection = GeneralSettingsManager.LoadSettings(GeneralSettingsManager.BNetConfigPath);

			if (WorldCollectionTemplate == null)
				Log("WorldCollectionTemplate is null, error loading file worldserver.conf");
			if (BnetCollectionTemplate == null)
				Log("BnetCollectionTemplate is null, error loading file bnetserver.conf");
			if (WorldCollection == null)
				Log($"WorldConfig is null, error loading file {GeneralSettingsManager.WorldConfigPath} -- if no configuration has been made, please hit the [Set Defaults] and [Save/Export]");
			if (BnetCollection == null)
				Log($"BnetConfig is null, error loading file {GeneralSettingsManager.BNetConfigPath} -- if no configuration has been made, please hit the [Set Defaults] and [Save/Export]");
		}

		public void Log(string log) { LogText = ":> " + log + "\n" + LogText; }

		public void PopulateHelp()
		{
			HelpAbout += "This tool helps build working World and Bnet server config files without duplicate entries. This can also be used to check your configuration ";
			HelpAbout += "for any known issues. There are some things to be aware of -\n";
			HelpAbout += "The MySQL server will probably error connecting unless you're running this on the same server. You should keep the MySQL server set to 127.0.0.1 and user/password ";
			HelpAbout += "should be left to defaults. Delete the settings.json file to reset them.\n";
			HelpAbout += "This tool can also update the config.wtf file in your WOW client configuration to make sure it matches with the rest of the configuration, assuming that ";
			HelpAbout += "this tool can access the folder/file. If you run your WOW Client from another PC, then you may need to set this manually to match the [LoginREST.ExternalAddress] ";
			HelpAbout += "from the Bnet Config, otherwise you may have trouble with your WOW client contacting the server. You can find this entry in your Bnet Config.\n";
			HelpAbout += "This tool can also set and check the [Game.Build.Version] between both configs and the database realm entry, and warn of any issues. Use the [Set Build] button ";
			HelpAbout += "to set this entry if there is a discrepancy between them and your WOW client version. You can find the WOW client version by launching it and checking at the ";
			HelpAbout += "bottom-left of the client at the login screen.\n";
			HelpAbout += "Use the [Set IP] button to setup the external/lan/wan IP address in the Database entry for the realm, the Bnet config, and the WOW client (as much as it can ";
			HelpAbout += "access from the computer the tool is running from). This will update the Database Realm entry and WOW config immediately. The rest won't update until Save/Export.\n";
			HelpAbout += "Use the [Set Build] button to set the [Game.Build.Version] in both configs, and the realm database entry.\n";
			HelpAbout += "If there is a problem, you can use the [Set Defaults] button to pull the wow config fresh from the local template files. This will overwrite all ";
			HelpAbout += "previous settings for the Bnet and World config files. You'd need to set the [Game.Build.Version] again, and possibly the IP if hosting outside ";
			HelpAbout += "of the local server.\n";
			HelpAbout += "Use the [Check Config] button to run through some quick problem checks for common issues. Note - this may give errors connecting to MySQL ";
			HelpAbout += "if this is running from another PC than the one the SPP Database Server runs on, and also make sure that the Database Server itself is running first. ";
			HelpAbout += "Otherwise this tool cannot connect to the Database to check/update any settings there. If the error says similar to [not allowed to connect to this MySQL server] ";
			HelpAbout += "then you're probably running this on a different computer. Run it from the SPP server (while the database server is running).\n";
			HelpAbout += "Once you've finished making any changes, hit the [Save/Export] button to export the current settings to the bnetserver.conf and worldserver.conf files.\n";
			HelpAbout += "Make sure to set the folders for your SPP LegionV2 folder, and your WOW Client folder in the [General App Settings] tab.";
		}
	}
}
