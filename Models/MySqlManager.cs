using MySql.Data.MySqlClient;
using System;
using System.Web.UI.WebControls;

namespace SPP_LegionV2_Management
{
	public static class MySqlManager
	{
		// https://stackoverflow.com/questions/21618015/how-to-connect-to-mysql-database

		private static string server;
		private static string user;
		private static string password;
		private static int port;
		private static string connectionString;

		private static void UpdateConnectionInfo()
		{
			// Refresh these every time in case settings have been changed/updated
			server = GeneralSettingsManager.GeneralSettings.MySQLServer;
			user = GeneralSettingsManager.GeneralSettings.MySQLUser;
			password = GeneralSettingsManager.GeneralSettings.MySQLPass;
			port = GeneralSettingsManager.GeneralSettings.MySQLPort;
			connectionString = String.Format("server={0};port={1};user id={2}; password={3};default command timeout=3600;", server, port, user, password);
		}

		public static MySqlDataReader MySQLQuery(string query, Action<MySqlDataReader> loader)
		{
			UpdateConnectionInfo();

			try
			{
				using (MySqlConnection connection = new MySqlConnection(connectionString))
				{
					connection.Open();

					using (MySqlCommand cmd = connection.CreateCommand())
					{
						cmd.CommandText = query;
						using (var reader = cmd.ExecuteReader())
						{
							while (reader.Read())
							{
								loader.Invoke(reader);
							}
						}
					}
				}
			}
			catch (Exception e) { Console.WriteLine($"SQL Exception {e.Message}\nQuery: {query}"); }

			return null;
		}

		public static string MySQLQueryToString(string query, bool update = false)
		{
			UpdateConnectionInfo();
			string response = string.Empty;

			try
			{
				// create a MySQL command and set the SQL statement with parameters
				MySqlConnection connection = new MySqlConnection(connectionString);
				connection.Open();
				MySqlCommand myCommand = new MySqlCommand();
				myCommand.Connection = connection;
				myCommand.CommandText = query;

				// exe(cute the command and read the results
				if (update)
				{
					response = $"{myCommand.ExecuteNonQuery().ToString()} rows affected";
				}
				else
				{
					using (var reader = myCommand.ExecuteReader())
					{
						while (reader.Read()) { }
						if (!reader.IsDBNull(0))
							response = reader.GetValue(0).ToString();
					}
				}
				connection.Close();
			}
			catch (Exception e)
			{
				Console.WriteLine($"SQL exception, query = {query}");
				return e.Message;
			}

			return response;
		}
	}
}