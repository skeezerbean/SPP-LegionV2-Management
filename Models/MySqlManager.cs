using MySql.Data.MySqlClient;
using System;

namespace SPP_Config_Generator
{
	public static class MySqlManager
	{
		// https://stackoverflow.com/questions/21618015/how-to-connect-to-mysql-database

		static string server;
		static string database = "legion_auth";
		static string user;
		static string password;
		static int port;
		static string connectionString;

		private static void UpdateConnectionInfo()
		{
			// Refresh these every time in case settings have been changed/updated
			server = GeneralSettingsManager.GeneralSettings.MySQLServer;
			user = GeneralSettingsManager.GeneralSettings.MySQLUser;
			password = GeneralSettingsManager.GeneralSettings.MySQLPass;
			port = GeneralSettingsManager.GeneralSettings.MySQLPort;
			connectionString = String.Format("server={0};port={1};user id={2}; password={3}; database={4};", server, port, user, password, database);
		}

		public static string MySQLQuery(string query)
		{
			UpdateConnectionInfo();
			string response = string.Empty;

			try
			{
				using (MySqlConnection connection = new MySqlConnection(connectionString))
				{
					connection.Open();
					Console.WriteLine("connect");
					using (var cmd = connection.CreateCommand())
					{
						cmd.CommandText = query;
						using (var reader = cmd.ExecuteReader())
						{
							while (reader.Read()) { }
							response = reader.GetString(0);
						}
					}
				}
			}
			catch (Exception ex) { return ex.Message.ToString(); }

			return response;
		}
	}
}
