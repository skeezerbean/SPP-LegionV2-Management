using Caliburn.Micro;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace SPP_Config_Generator
{
	/// <summary>
	/// This class handles the general settings, save/load XML
	/// </summary>
	public static class GeneralSettingsManager
	{
		public static GeneralSettings GeneralSettings { get; set; } = new GeneralSettings();
		public static JObject SettingsJSON { get; set; }
		public static JObject WorldTemplateJSON { get; set; }
		public static JObject BnetTemplateJSON { get; set; }
		public static JObject WorldConfigJSON { get; set; }
		public static JObject BnetConfigJSON { get; set; }
		public static string SettingsPath = "Settings.json";
		public static string WorldTemplatePath = "default-config-world.json";
		public static string BNetTemplatePath = "default-config-bnet.json";
		public static string WorldConfigPath = "saved-config-world.json";
		public static string BNetConfigPath = "saved-config-bnet.json";
		public static string LogText { get; set; }

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
		public static bool SaveSettings(string path, object item)
		{
			try { File.WriteAllText(path, JsonConvert.SerializeObject(item, Formatting.Indented)); }
			catch { return false; }
			return true;
		}

		// This is for reading in the world/bnet template/saved files
		// Look into dumping this and general load into a generic
		public static BindableCollection<ConfigEntry> LoadSettings(string path)
		{
			BindableCollection<ConfigEntry> loadresult = new BindableCollection<ConfigEntry>();

			try
			{
				var json = JToken.Parse(File.ReadAllText(path));
				loadresult = JsonConvert.DeserializeObject<BindableCollection<ConfigEntry>>(json.ToString());
			}
			catch { return null; } // failed to load the file

			return loadresult;
		}

		public static BindableCollection<ConfigEntry> CreateDefaultTemplateFromFile(string inFile)
		{
			string tmpDescription = string.Empty;
			string tmpName = string.Empty;
			string tmpValue = string.Empty;
			BindableCollection<ConfigEntry> tempCollection = new BindableCollection<ConfigEntry>();
			int count = 0;

			try
			{
				// Pull in our config
				List<string> allLinesText = File.ReadAllLines(inFile).ToList();

				foreach (var item in allLinesText)
				{
					count++;

					// Check if comment or not
					if (item.Contains("=") && item.Contains("#") == false)
					{
						// Split based on = sign
						string[] strArray = item.Split('=');

						// Now we should have 2 parts, the entry[0] and the value[1]
						ConfigEntry entry = new ConfigEntry();
						entry.Name = strArray[0];
						entry.Value = strArray[1];
						entry.Description = tmpDescription;
						tempCollection.Add(entry);
						tmpDescription = string.Empty; // reset our description
					}
					else
					{
						// If we're here, then the line doesn't have =, so it's description or other
						// so we'll keep adding to the tmpDescription until it's used/flushed to the collection
						tmpDescription += item + "\n";
					}
				}
			}
			catch { return null; }

			return tempCollection;
		}

		// If the saved window settings are out of bounds (resolution change, multiple monitors change)
		// then we move into view
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

		public GeneralSettings() { }
	}

	// This is the class for representing any single configuration entry for the WoW config
	// A group of them will be put into a BindableCollection
	public class ConfigEntry
	{
		public string Name { get; set; } = string.Empty;
		public string Value { get; set; } = string.Empty;
		public string Description { get; set; } = string.Empty;

		public ConfigEntry() { }
	}
}