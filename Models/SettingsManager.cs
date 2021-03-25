using Caliburn.Micro;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;
using System.Windows;

namespace SPP_LegionV2_Management
{
	/// <summary>
	/// This class handles the general settings, save/load with files
	/// </summary>
	public static class GeneralSettingsManager
	{
		public static GeneralSettings GeneralSettings { get; set; } = new GeneralSettings();
		public static JObject SettingsJSON { get; set; }
		public static bool IsMySQLRunning { get; set; }
		public static string SettingsPath = "Settings.json";

		// If the file doesn't exist, it will resort to defaults listed in the class itself
		public static void LoadGeneralSettings()
		{
			// Let's try to pull in settings, keep default if fails
			try
			{
				var fileinfo = new FileInfo(SettingsPath);
				SettingsJSON = JObject.Parse(File.ReadAllText(SettingsPath));
				GeneralSettings = JsonConvert.DeserializeObject<GeneralSettings>(SettingsJSON.ToString());
			}
			catch
			{
				// Setting defaults...
				string jsonString = JsonConvert.SerializeObject(GeneralSettings);
				GeneralSettings = JsonConvert.DeserializeObject<GeneralSettings>(jsonString);
				SettingsJSON = (JObject)JsonConvert.DeserializeObject(jsonString);
			}
		}

		// Take an incoming class and convert to json to save to disk
		// as well as implementing indentation for human-reading
		public static bool SaveSettings(string path, object item)
		{
			try { File.WriteAllText(path, JsonConvert.SerializeObject(item, Formatting.Indented)); }
			catch { return false; }
			return true;
		}

		// This takes in a file path to a bnet or worldserver.conf file, then parses it to store
		// into a collection of <ConfigEntry> objects to return back to the caller
		public static BindableCollection<ConfigEntry> CreateCollectionFromConfigFile(string inFile)
		{
			string tmpDescription = string.Empty;
			BindableCollection<ConfigEntry> tempCollection = new BindableCollection<ConfigEntry>();

			try
			{
				// Pull in our config, process every text line in the file
				foreach (var item in File.ReadAllLines(inFile).ToList())
				{
					// Check if comment or not
					if (item.Contains("=") && !item.StartsWith("#"))
					{
						// Split based on = sign
						string[] strArray = item.Split('=');

						// Now we should have 2 parts, the setting[0] and the value[1] and strip janky whitespace from the start/end
						// so that we're left with a value and setting without extra space. This helps both comparing strings later,
						// as well as the export of this collection back to disk
						ConfigEntry entry = new ConfigEntry
						{
							// Set our name (setting) without the whitespace
							Name = strArray[0].TrimStart(' ').TrimEnd(' '),

							// Set our value of the name/setting without whitespace
							Value = strArray[1].TrimStart(' ').TrimEnd(' '),

							// Remove any extra whitespace
							Description = tmpDescription.TrimStart(' ').TrimEnd(' ')
						};

						// Squirt our new entry into our temp collection
						tempCollection.Add(entry);
						tmpDescription = string.Empty; // reset our description
					}
					else
					{
						// If we're here, then the line is part of the description or other line
						// so we'll keep adding to the tmpDescription until it's used/flushed to the collection
						// when it matches a config entry. Since each time this happens, add a \n at the end
						// so it will maintain visual continuity with the original .conf file when it exports
						tmpDescription += item + "\n";
					}
				}
				// Grab the last entry, which may be just a description
				if (tmpDescription?.Length > 0)
					tempCollection.Add(new ConfigEntry { Name = "", Value = "", Description = tmpDescription });
			}
			catch { return new BindableCollection<ConfigEntry>(); } // if something failed, then return a default blank collection

			return tempCollection;
		}

		// If the saved window settings are out of bounds (resolution change, multiple monitors change)
		// then we move into view so that it isn't off screen somewhere
		public static void MoveIntoView()
		{
			GeneralSettingsManager.GeneralSettings.WindowWidth = System.Math.Min(GeneralSettingsManager.GeneralSettings.WindowWidth, SystemParameters.VirtualScreenWidth);
			GeneralSettingsManager.GeneralSettings.WindowHeight = System.Math.Min(GeneralSettingsManager.GeneralSettings.WindowHeight, SystemParameters.VirtualScreenHeight);
			GeneralSettingsManager.GeneralSettings.WindowLeft = System.Math.Min(System.Math.Max(GeneralSettingsManager.GeneralSettings.WindowLeft, SystemParameters.VirtualScreenLeft),
				SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - GeneralSettingsManager.GeneralSettings.WindowWidth);
			GeneralSettingsManager.GeneralSettings.WindowTop = System.Math.Min(System.Math.Max(GeneralSettingsManager.GeneralSettings.WindowTop, SystemParameters.VirtualScreenTop),
				SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - GeneralSettingsManager.GeneralSettings.WindowHeight);
		}
	}

	/// <summary>
	/// This class stores the overall general settings for the app
	/// </summary>
	public class GeneralSettings
	{
		public string SPPFolderLocation { get; set; } = string.Empty;
		public string WOWConfigLocation { get; set; } = string.Empty;
		public string MySQLServer { get; set; } = "127.0.0.1";
		public int MySQLPort { get; set; } = 3310;
		public string MySQLUser { get; set; } = "spp_user";
		public string MySQLPass { get; set; } = "123456";
		public double WindowTop { get; set; } = 100;
		public double WindowLeft { get; set; } = 100;
		public double WindowHeight { get; set; } = 500;
		public double WindowWidth { get; set; } = 800;

		public GeneralSettings()
		{
		}
	}

	// This is the class for representing any single configuration entry for the WoW config
	// A group of them will be put into a List
	public class ConfigEntry
	{
		public string Name { get; set; } = string.Empty;
		public string Value { get; set; } = string.Empty;
		public string Description { get; set; } = string.Empty;

		public ConfigEntry()
		{
		}
	}
}