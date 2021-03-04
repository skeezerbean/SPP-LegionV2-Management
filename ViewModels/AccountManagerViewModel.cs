using Caliburn.Micro;
using MahApps.Metro.Controls.Dialogs;
using MySql.Data.MySqlClient;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;

namespace SPP_LegionV2_Management
{
	public class AccountManagerViewModel : Screen, INotifyPropertyChanged
	{
		// IDialogCoordinator is for metro message boxes
		private readonly IDialogCoordinator _dialogCoordinator;
		private BindableCollection<Character> tempCharacters = new BindableCollection<Character>();
		private BindableCollection<Character> tempOrphanedCharacters = new BindableCollection<Character>();

		public BindableCollection<Account> Accounts { get; set; } = new BindableCollection<Account>();
		public BindableCollection<Character> Characters { get; set; } = new BindableCollection<Character>();
		public BindableCollection<Character> OrphanedCharacters { get; set; } = new BindableCollection<Character>();
		public Account SelectedAccount { get; set; } = new Account();
		public Character SelectedCharacter { get; set; } = new Character();

		// Accounts
		public int AccountsTotal { get; set; }
		public int CurrentID { get { return SelectedAccount.ID; } }
		public int CurrentBattleNetAccount { get { return SelectedAccount.BattleNetAccount; } }
		public string CurrentBattleNetEmail { get { return SelectedAccount.BattleNetEmail; } set { SelectedAccount.BattleNetEmail = value; NotifyOfPropertyChange("Accounts"); } }
		public string CurrentUsername { get { return SelectedAccount.Username; } set { SelectedAccount.Username = value; } }
		public int CurrentBattleCoins { get { return SelectedAccount.BattleCoins; } set { SelectedAccount.BattleCoins = value; } }
		public int CurrentGMLevel { get { return SelectedAccount.GMLevel; } set { SelectedAccount.GMLevel = value; } }

		// Characters
		public int CharactersTotal { get; set; }
		public int OrphanedCharactersTotal { get; set; }
		public int CurrentCharacterGUID { get { return SelectedCharacter.Guid; } }
		public int CurrentCharacterAccountID { get { return SelectedCharacter.Account; } set { SelectedCharacter.Account = value; } }
		public string CurrentCharacterName { get { return SelectedCharacter.Name; } set { SelectedCharacter.Name = value; } }

		public override event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged(string propertyName)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		// IDialogCoordinator is part of Metro, for dialog handling in the view model
		public AccountManagerViewModel(IDialogCoordinator instance)
		{
			_dialogCoordinator = instance;
		}

		public void ApplyAccountChanges()
		{
			string tmp = string.Empty;

			foreach (var account in Accounts)
			{
				string response = string.Empty;

				// Update our username
				MySqlManager.MySQLQueryToString($"UPDATE legion_auth.account SET username=\"{account.Username}\" WHERE id={account.ID}", true);

				// If this was -1 then the account didn't exist, so skip. Otherwise update the login/battlecoins
				if (account.BattleNetAccount != -1)
					MySqlManager.MySQLQueryToString($"UPDATE legion_auth.battlenet_accounts SET email=\"{account.BattleNetEmail}\",donate={account.BattleCoins} WHERE id={account.BattleNetAccount}", true);


				response = MySqlManager.MySQLQueryToString($"SELECT IFNULL((SELECT gmlevel FROM legion_auth.account_access WHERE id = {account.ID}), \"-1\")");

				if (account.GMLevel < -1 || account.GMLevel > 6)
					MessageBox.Show($"Account ID {account.ID}({account.Username}) - entry for GMLevel must be from 1 to 6 only, or set to -1 or 0 to remove GM status");

				else if (response == "-1" && account.GMLevel > 0)
				{
						// perform insert to new gm access entry if default GMlevel also changed
						Console.WriteLine($" {account.ID}({account.Username}) Adding GM Entry - " + MySqlManager.MySQLQueryToString($"INSERT INTO legion_auth.account_access (id,gmlevel,RealmID) VALUES ({account.ID},{account.GMLevel},-1)", true));
				}
				// remove the entry if exists
				else if (response != "-1" && (account.GMLevel == -1 || account.GMLevel == 0))
					Console.WriteLine($" {account.ID}({account.Username}) Removing from GM status - " + MySqlManager.MySQLQueryToString($"DELETE FROM legion_auth.account_access WHERE id={account.ID}", true));
			}

			// now that things have been updated, refresh our list
			RetrieveAccounts();
		}

		public void ApplyCharacterChanges()
		{
			string tmp = string.Empty;

			foreach (var character in Characters)
			{
				int accountIDFromDB = Int32.Parse(MySqlManager.MySQLQueryToString($"SELECT account FROM legion_characters.characters WHERE guid={character.Guid}"));
				string nameFromDB = MySqlManager.MySQLQueryToString($"SELECT name FROM legion_characters.characters WHERE guid={character.Guid}");

				if (character.Name != nameFromDB)
					MessageBox.Show($"Changing name for character {nameFromDB} to {character.Name} - " + MySqlManager.MySQLQueryToString($"UPDATE legion_characters.characters SET name=\"{character.Name}\" WHERE guid={character.Guid}", true));

				if (character.Account != accountIDFromDB)
					MessageBox.Show($"Changing account for character {character.Name} from {accountIDFromDB} to {character.Account} - " + MySqlManager.MySQLQueryToString($"UPDATE legion_characters.characters SET account={character.Account} WHERE guid={character.Guid}", true));
			}

			RetrieveCharacters();
		}

		public bool CheckSQL()
		{
			if (!GeneralSettingsManager.IsMySQLRunning)
			{
				MessageBox.Show("Database Engine is not running, cannot continue");
				return false;
			}

			return true;
		}

		public async void RetrieveAccounts()
		{
			if (!CheckSQL())
				return;

			Accounts.Clear();
			MySqlDataReader reader = MySqlManager.MySQLQuery("SELECT id,username,IFNULL(battlenet_account,\"-1\") FROM legion_auth.account", GetAccountsFromSQL);
			AccountsTotal = Accounts.Count;
			await Task.Delay(1);
		}

		public void AddAccount()
		{
			if (!CheckSQL())
				return;
		}

		public void DeleteSelectedAccount()
		{
			if (!CheckSQL())
				return;

			// Query characters associated with this account, prompt to continue and post characters in window to verify

			// Cycle through all characters for this account, removing all data for each character.
			// then delete the characters themselves
			// Delete account, bnet, gm entry if exists from SQL then retrieve accounts again to refresh from DB directly, verify account is gone
		}

		// Only here for the sake of button unique name
		public void RetrieveOrphanedCharacters()
		{
			RetrieveCharacters();
		}

		public async void RetrieveCharacters()
		{
			if (!CheckSQL())
				return;

			Characters.Clear();
			OrphanedCharacters.Clear();
			tempCharacters.Clear();
			tempOrphanedCharacters.Clear();
			MySqlDataReader reader = MySqlManager.MySQLQuery("SELECT guid,account,name FROM legion_characters.characters", GetCharactersFromSQL);
			await Task.Delay(1);

			Characters = tempCharacters;
			OrphanedCharacters = tempOrphanedCharacters;
			CharactersTotal = Characters.Count;
			OrphanedCharactersTotal = OrphanedCharacters.Count;
		}

		private async void GetAccountsFromSQL(MySqlDataReader reader)
		{
			Account account = new Account();
			string tmp = string.Empty;
			int num = -1;

			try
			{
				account.ID = reader.GetInt32(0);
				account.Username = reader.GetString(1);
				account.BattleNetAccount = reader.GetInt32(2);
				account.BattleNetEmail = MySqlManager.MySQLQueryToString($"SELECT IFNULL((SELECT email FROM legion_auth.battlenet_accounts WHERE id = {account.BattleNetAccount}),\"-1\")");

				if (account.BattleNetAccount != -1)
				{
					tmp = MySqlManager.MySQLQueryToString($"SELECT donate FROM legion_auth.battlenet_accounts WHERE id = {account.BattleNetAccount}");
					Int32.TryParse(tmp, out num);
				}
				account.BattleNetAccount = num;

				num = -1;
				tmp = string.Empty;
				tmp = MySqlManager.MySQLQueryToString($"SELECT IFNULL((SELECT gmlevel FROM legion_auth.account_access WHERE id = {account.ID}), \"-1\")");

				// If there was an uncaught exception/failure somewhere
				if (!tmp.Contains("Invalid attempt to access a field before calling Read()"))
				{
					Int32.TryParse(tmp, out num);
					account.GMLevel = num;
				}
			}
			catch (Exception e) { Console.WriteLine($"Account: {account.BattleNetEmail}\n{e.Message}"); }

			Accounts.Add(account);
			await Task.Delay(1);
		}

		private async void GetCharactersFromSQL(MySqlDataReader reader)
		{
			try
			{
				Character character = new Character();
				character.Guid = reader.GetInt32(0);
				character.Account = reader.GetInt32(1);
				character.Name = reader.GetString(2);

				// This should return -1 for characters missing an account
				string response = MySqlManager.MySQLQueryToString($"SELECT IFNULL((SELECT id FROM legion_auth.account WHERE id={character.Account}),\"-1\")");

				if (response == "-1")
					tempOrphanedCharacters.Add(character);
				else
					tempCharacters.Add(character);
			}
			catch (Exception e) { Console.WriteLine($"Error retrieving character info - {e.Message}"); }

			if (OrphanedCharacters.Count % 200 == 0)
			{
				CharactersTotal = tempCharacters.Count;
				OrphanedCharactersTotal = tempOrphanedCharacters.Count;
				NotifyOfPropertyChange("CharactersTotal");
				NotifyOfPropertyChange("OrphanedCharactersTotal");
				await Task.Delay(1);
			}
		}
	}
}
