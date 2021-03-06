using Caliburn.Micro;
using MahApps.Metro.Controls.Dialogs;
using MySql.Data.MySqlClient;
using System;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SPP_LegionV2_Management
{
	public class AccountManagerViewModel : Screen, INotifyPropertyChanged
	{
		// IDialogCoordinator is for metro message boxes
		private readonly IDialogCoordinator _dialogCoordinator;

		// block parallel tasks running of the same type
		private bool _accountRetrieveRunning = false;
		private bool _characterRetrieveRunning = false;
		private bool _deleteAccountRunning = false;
		private bool _deleteCharacterRunning = false;

		public BindableCollection<Account> Accounts { get; set; } = new BindableCollection<Account>();
		public BindableCollection<Character> Characters { get; set; } = new BindableCollection<Character>();
		public BindableCollection<Character> OrphanedCharacters { get; set; } = new BindableCollection<Character>();
		public Character SelectedCharacter { get; set; } = new Character();
		public Character OrphanedSelectedCharacter { get; set; } = new Character();
		public Account SelectedAccount { get; set; }

		// Accounts
		public int AccountsTotal { get; set; }
		public int CurrentID { get { return SelectedAccount.ID; } }
		public int CurrentBattleNetAccount { get { return SelectedAccount.BattleNetAccount; } }
		public string CurrentBattleNetEmail { get { return SelectedAccount.BattleNetEmail; } set { SelectedAccount.BattleNetEmail = value; } }
		public string CurrentUsername { get { return SelectedAccount.Username; } set { SelectedAccount.Username = value; } }
		public int CurrentBattleCoins { get { return SelectedAccount.BattleCoins; } set { SelectedAccount.BattleCoins = value; } }
		public int CurrentGMLevel { get { return SelectedAccount.GMLevel; } set { SelectedAccount.GMLevel = value; } }
		public int CurrentBattleNetID { get { return SelectedAccount.BattleNetAccount; } }
		public string CurrentPassword { private get { return SelectedAccount.Password; } set { SelectedAccount.Password = value; } }

		// Characters
		public int CharactersTotal { get; set; }
		public int CurrentCharacterGUID { get { return SelectedCharacter.Guid; } }
		public int CurrentCharacterAccountID { get { return SelectedCharacter.Account; } set { SelectedCharacter.Account = value; } }
		public string CurrentCharacterName { get { return SelectedCharacter.Name; } set { SelectedCharacter.Name = value; } }

		// Orphaned Characters
		public int OrphanedCharactersTotal { get; set; }
		public int OrphanedOrphanedCharactersTotal { get; set; }
		public int OrphanedCurrentCharacterGUID { get { return OrphanedSelectedCharacter.Guid; } }
		public int OrphanedCurrentCharacterAccountID { get { return OrphanedSelectedCharacter.Account; } set { OrphanedSelectedCharacter.Account = value; } }
		public string OrphanedCurrentCharacterName { get { return OrphanedSelectedCharacter.Name; } set { OrphanedSelectedCharacter.Name = value; } }

		// IDialogCoordinator is part of Metro, for dialog handling in the view model
		public AccountManagerViewModel(IDialogCoordinator instance)
		{
			_dialogCoordinator = instance;
		}

		// This is used to compute a new SHA256 password hash for the bnet account, and
		// is REQUIRED if the bnet account name changed, as the 2 are tied together
		private string sha256_hash(string randomString, bool reverse = false)
		{
			var crypt = new SHA256Managed();
			var hash = new System.Text.StringBuilder();
			byte[] crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(randomString));

			foreach (byte theByte in crypto)
				hash.Append(theByte.ToString("x2"));

			// the 2nd time hashing the password needs the string reversed
			if (reverse)
			{
				string a = hash.ToString();
				char[] ca = a.ToCharArray();
				StringBuilder sb = new StringBuilder(a.Length);
				for (int i = 0; i < a.Length; i += 2)
					sb.Insert(0, ca, i, 2);

				return sb.ToString();
			}

			return hash.ToString();
		}

		public void ApplyAccountChanges()
		{
			foreach (var account in Accounts)
			{
				string userFromDB = MySqlManager.MySQLQueryToString($"SELECT `username` FROM `legion_auth`.`account` WHERE `id` = '{account.ID}'");
				string battlenetLoginFromDB = MySqlManager.MySQLQueryToString($"SELECT IFNULL((SELECT `email` FROM `legion_auth`.`battlenet_accounts` WHERE `id` = '{account.BattleNetAccount}'),\"-1\")");
				int battlenetCoinsFromDB = Int32.Parse(MySqlManager.MySQLQueryToString($"SELECT IFNULL((SELECT `donate` FROM `legion_auth`.`battlenet_accounts` WHERE `id` = '{account.BattleNetAccount}'),\"-1\")"));

				// Update our username
				if (account.Username != userFromDB)
					MessageBox.Show($"Updating Username from [{userFromDB}] to [{account.Username}] - "
						+ MySqlManager.MySQLQueryToString($"UPDATE `legion_auth`.`account` SET `username`='{account.Username}' WHERE `id`='{account.ID}'", true));

				// If this was -1 then the account didn't exist, so skip. Otherwise update the login/battlecoins
				if (account.BattleNetAccount != -1)
				{
					bool _updatedPassword = false;
					string passHash;

					if (battlenetLoginFromDB != account.BattleNetEmail)
					{
						// Changing BattleNet Email means the password hash MUST be updated, or this account will no longer be able
						// to login. The password box on the account page needed filled out. If not, we alert and ignore the 
						// BattleNet Email change.
						if (account.Password != null && account.Password.Length > 0)
						{
							// the WoW Bnet password hash is built by getting uppercase hash of the email login, then combining
							// that hash + ":" and the password, then reverse order, uppercase the result and shove in the DB
							passHash = sha256_hash(sha256_hash(account.BattleNetEmail).ToUpper() + ":" + account.Password, true).ToUpper();
							MessageBox.Show($"Changing BattleNet Login for [{battlenetLoginFromDB}] to [{account.BattleNetEmail}] - "
							+ MySqlManager.MySQLQueryToString($"UPDATE `legion_auth`.`battlenet_accounts` SET `email`='{account.BattleNetEmail}',`sha_pass_hash`='{passHash}' WHERE `id`='{account.BattleNetAccount}'", true));

							_updatedPassword = true;
						}
						else
						{
							string tmp = "ALERT - You're trying to change your BattleNet Email, but you didn't enter your account password. ";
							tmp += "This is REQUIRED, as the SHA256 password hash MUST be updated in order to login afterwards. Note, this password ";
							tmp += "can be either an old or new one. Update the BattleNet Email change again, AND the password on this account, and apply again.";
							MessageBox.Show(tmp);
						}
					}

					// In case they entered a password (to change account pass only, not the login)
					// see above notes about hash/passwords
					if (account.Password != null && !_updatedPassword && account.Password.Length > 0)
					{
						passHash = sha256_hash(sha256_hash(account.BattleNetEmail).ToUpper() + ":" + account.Password, true).ToUpper();
						MessageBox.Show($"Changing BattleNet Password for [{account.BattleNetEmail}] - "
							+ MySqlManager.MySQLQueryToString($"UPDATE `legion_auth`.`battlenet_accounts` SET `sha_pass_hash`='{passHash}' WHERE `id`='{account.BattleNetAccount}'", true));
					}

					if (battlenetCoinsFromDB != account.BattleCoins)
						MessageBox.Show($"Setting BattleNet Coins for [{account.BattleNetAccount}:{account.BattleNetEmail}] - "
						+ MySqlManager.MySQLQueryToString($"UPDATE `legion_auth`.`battlenet_accounts` SET `email`='{account.BattleNetEmail}',`donate`='{account.BattleCoins}' WHERE `id`='{account.BattleNetAccount}'", true));
				}

				// If this account is not GM, it should resolve to -1
				string response = MySqlManager.MySQLQueryToString($"SELECT IFNULL((SELECT `gmlevel` FROM `legion_auth`.`account_access` WHERE `id` = '{account.ID}'), \"-1\")");

				// If the value entered on the form was out of scope....
				if (account.GMLevel < -1 || account.GMLevel > 6)
					MessageBox.Show($"Account [{account.ID}({account.Username})] - entry for GMLevel must be from 1 to 6 only, or set to -1 or 0 to remove GM status");

				// If they're NOT a GM, and the GM level entered is higher than 0 then we need to add them
				else if (response == "-1" && account.GMLevel > 0)
				{
					// perform insert to new gm access entry if default GMlevel also changed
					MessageBox.Show($"Account [{account.ID}({account.Username})] Adding GM Entry - " 
						+ MySqlManager.MySQLQueryToString($"INSERT INTO `legion_auth`.`account_access` (`id`,`gmlevel`,`RealmID`) VALUES ('{account.ID}','{account.GMLevel}','-1')", true));
				}
				// If they ARE GM (response not -1), and the new value declares they shouldn't be...
				else if (response != "-1" && (account.GMLevel == -1 || account.GMLevel == 0))
					MessageBox.Show($"Account [{account.ID}({account.Username})] Removing from GM status - " 
						+ MySqlManager.MySQLQueryToString($"DELETE FROM `legion_auth`.`account_access` WHERE `id`='{account.ID}'", true));
				// In case GM Level was changed for existing account
				else if (response != "-1" && (account.GMLevel != Int32.Parse(response)))
					MessageBox.Show($"Account [{account.ID}({account.Username})] Changing GM status from {response} to {account.GMLevel} - " 
						+ MySqlManager.MySQLQueryToString($"UPDATE `legion_auth`.`account_access` SET `gmlevel`='{account.GMLevel}' WHERE `id`='{account.ID}'", true));
			}

			// now that things have been updated, refresh our list
			RetrieveAccounts();
		}

		// Button for Character changes calls this
		public void ApplyCharacterChanges() { ApplyCharacterChangesProcess(Characters); }

		// Button for Orphaned Character changes calls this
		public void ApplyOrphanedCharacterChanges() { ApplyCharacterChangesProcess(OrphanedCharacters); }

		// Work to actually change character settings
		public void ApplyCharacterChangesProcess(BindableCollection<Character> characters)
		{
			string tmp = string.Empty;

			// For either name or account, if they've changed then update the entry for them
			foreach (var character in characters)
			{
				int accountIDFromDB = Int32.Parse(MySqlManager.MySQLQueryToString($"SELECT `account` FROM `legion_characters`.`characters` WHERE `guid`='{character.Guid}'"));
				string nameFromDB = MySqlManager.MySQLQueryToString($"SELECT `name` FROM `legion_characters`.`characters` WHERE `guid`='{character.Guid}'");

				if (character.Name != nameFromDB)
					MessageBox.Show($"Changing name for character [{nameFromDB}] to [{character.Name}] - "
						+ MySqlManager.MySQLQueryToString($"UPDATE `legion_characters`.`characters` SET `name`='{character.Name}' WHERE `guid`='{character.Guid}'", true));

				if (character.Account != accountIDFromDB)
					MessageBox.Show($"Changing account for character [{character.Name}] from [{accountIDFromDB}] to [{character.Account}] - "
						+ MySqlManager.MySQLQueryToString($"UPDATE `legion_characters`.`characters` SET `account`='{character.Account}' WHERE `guid`='{character.Guid}'", true));
			}

			RetrieveCharacters();
		}

		// This checks the status of the constant SQL check in the ShellViewModel
		public bool CheckSQL()
		{
			if (!GeneralSettingsManager.IsMySQLRunning)
			{
				MessageBox.Show("Database Engine is not running, cannot continue");
				return false;
			}

			return true;
		}

		// This creates a 'reader' to pull account data from the database, using GetAccountsFromSQL as it's processing function
		public async void RetrieveAccounts()
		{
			if (!CheckSQL() || _accountRetrieveRunning)
				return;

			_accountRetrieveRunning = true;

			// Clear the collection since we're re-using
			Accounts.Clear();

			// wrap this in a task, waiting for it to complete. This will let the UI update through progress
			Task t = Task.Run(() =>
			{
				MySqlDataReader reader = MySqlManager.MySQLQuery("SELECT `id`,`username`,IFNULL(`battlenet_account`,\"-1\") FROM `legion_auth`.`account`", GetAccountsFromSQL);
			});

			// Loop until it completes, awaiting each iteration to let the UI update
			while (!t.IsCompleted) { await Task.Delay(100); }

			AccountsTotal = Accounts.Count;
			_accountRetrieveRunning = false;
		}

		public void AddAccount()
		{
			if (!CheckSQL())
				return;

			// This should run through the process of adding an account with relevant info, then add bnet,
			// link them, add GM status if needed, as well as set the bnet password hash
		}

		// Called from button, pass to actual character deletion
		public async void DeleteSelectedCharacter()
		{
			Task t = Task.Run(() =>
			{
				DeleteCharacter(SelectedCharacter);
			});

			while (!t.IsCompleted) { await Task.Delay(100); }
		}

		// Delete specified character passed in, either from deleting selected character, or
		// running through all characters in an account being deleted
		private async Task DeleteCharacter(Character character)
		{
			if (!CheckSQL() || _deleteCharacterRunning)
				return;

			_deleteCharacterRunning = true;

			// Gather related character items/stats from DB, cycle through deleting those


			// then finally delete the character itself

			_deleteCharacterRunning = false;
		}

		public async Task DeleteSelectedAccount()
		{
			if (!CheckSQL() || _deleteAccountRunning)
				return;

			// For now, return until this can be finished.
			return;

			_deleteAccountRunning = true;

			// Get a fresh character list, or populate a new one
			Task t = Task.Run(() =>
			{
				RetrieveCharacters();
			});

			// give some time to let the character retrieve start
			await Task.Delay(100);

			// Loop until task completes, awaiting each iteration to let the UI update
			while (_characterRetrieveRunning) { await Task.Delay(1); }

			BindableCollection<Character> charactersToDelete = new BindableCollection<Character>();
			charactersToDelete.Clear();
			string msg = "ARE YOU SURE??\n\nCharacters in this account to be deleted:\n";

			// Getting our list of characters for this account
			foreach (var character in Characters)
			{
				if (character.Account == SelectedAccount.ID)
				{
					charactersToDelete.Add(character);
					msg += $"--{character.Name}\n";
				}
			}

			// Prompt with character list, verify to continue
			MessageBoxResult mbr = MessageBox.Show(msg, $"Deleting Account: {SelectedAccount.BattleNetAccount}", MessageBoxButton.YesNo);

			if (mbr.ToString() == "Yes")
			{
				// Cycle through all characters for this account, removing all data for each character.
				// then delete the characters themselves
				foreach (var character in charactersToDelete)
				{
					t = Task.Run(() =>
					{
						DeleteCharacter(character);
					});

					// Loop until it completes, awaiting each iteration to let the UI update
					while (!t.IsCompleted) { await Task.Delay(100); }
				}

				// Delete account, bnet, gm entry if exists from SQL then retrieve accounts/characters again to refresh from DB directly, verify account is gone


				RetrieveAccounts();
				RetrieveCharacters();
			}

			_deleteAccountRunning = false;
		}

		// Only here for the sake of button unique name
		public void RetrieveOrphanedCharacters()
		{
			RetrieveCharacters();
		}

		// This creates a 'reader' to pull character data from the database, using GetCharactersFromSQL as it's processing function
		public async void RetrieveCharacters()
		{
			if (!CheckSQL() || _characterRetrieveRunning)
				return;

			_characterRetrieveRunning = true;

			//  We're re-using these, so clear before populating again
			Characters.Clear();
			OrphanedCharacters.Clear();

			// wrap this in a task, waiting for it to complete. This will let the UI update through progress
			Task t = Task.Run(() =>
			{
				MySqlDataReader reader = MySqlManager.MySQLQuery("SELECT `guid`,`account`,`name` FROM `legion_characters`.`characters`", GetCharactersFromSQL);
			});

			// Loop until it completes, awaiting each iteration to let the UI update
			while (!t.IsCompleted) { await Task.Delay(100); }

			CharactersTotal = Characters.Count;
			OrphanedCharactersTotal = OrphanedCharacters.Count;
			_characterRetrieveRunning = false;
		}

		// Incoming reader is from the RetrieveAccounts task
		private async void GetAccountsFromSQL(MySqlDataReader reader)
		{
			Account account = new Account();

			try
			{
				account.ID = reader.GetInt32(0);
				account.Username = reader.GetString(1);
				account.BattleNetAccount = reader.GetInt32(2);
				account.BattleNetEmail = MySqlManager.MySQLQueryToString($"SELECT IFNULL((SELECT `email` FROM `legion_auth`.`battlenet_accounts` WHERE `id` = '{account.BattleNetAccount}'),\"-1\")");

				// Any account without matching battlenet will be -1 here
				if (account.BattleNetAccount != -1)
					account.BattleCoins = Int32.Parse(MySqlManager.MySQLQueryToString($"SELECT `donate` FROM `legion_auth`.`battlenet_accounts` WHERE `id` = '{account.BattleNetAccount}'"));

				// This should return -1 if there's no GM row for this account
				account.GMLevel = Int32.Parse(MySqlManager.MySQLQueryToString($"SELECT IFNULL((SELECT `gmlevel` FROM `legion_auth`.`account_access` WHERE `id` = '{account.ID}'), \"-1\")")) ;

				// This should only add if there wasn't an exception causing a problem
				Accounts.Add(account);
			}
			catch (Exception e) { Console.WriteLine($"Account: {account.BattleNetEmail}\n{e.Message}"); }

			// Let the UI update
			await Task.Delay(1);
		}

		// Incoming reader is from the RetrieveCharacters task
		private async void GetCharactersFromSQL(MySqlDataReader reader)
		{
			try
			{
				Character character = new Character();
				character.Guid = reader.GetInt32(0);
				character.Account = reader.GetInt32(1);
				character.Name = reader.GetString(2);

				// This should return -1 for character account without a match in the account table
				string response = MySqlManager.MySQLQueryToString($"SELECT IFNULL((SELECT `id` FROM `legion_auth`.`account` WHERE `id`='{character.Account}'),\"-1\")");

				if (response == "-1")
					OrphanedCharacters.Add(character);
				else
					Characters.Add(character);
			}
			catch (Exception e) { Console.WriteLine($"Error retrieving character info - {e.Message}"); }

			// This checks if the count is divisible by this number, so every xx then it will let the UI update with count totals
			// instead of every time. In the event of large number of accounts this cuts down on how often the UI pauses to update
			if ((OrphanedCharacters.Count % 200 == 0 && OrphanedCharacters.Count > 0)
				|| (Characters.Count % 200 == 0 && Characters.Count > 0))
			{
				CharactersTotal = Characters.Count;
				OrphanedCharactersTotal = OrphanedCharacters.Count;
				await Task.Delay(1);
			}
		}
	}

	// This class handles adding dependency properties for password boxes,
	// allowing me to actually clear the box and handle password hashing easier.
	public static class PasswordBoxAssistant
	{
		public static readonly DependencyProperty BoundPassword =
			DependencyProperty.RegisterAttached("BoundPassword", typeof(string), typeof(PasswordBoxAssistant), new PropertyMetadata(string.Empty, OnBoundPasswordChanged));

		public static readonly DependencyProperty BindPassword = DependencyProperty.RegisterAttached(
			"BindPassword", typeof(bool), typeof(PasswordBoxAssistant), new PropertyMetadata(false, OnBindPasswordChanged));

		private static readonly DependencyProperty UpdatingPassword =
			DependencyProperty.RegisterAttached("UpdatingPassword", typeof(bool), typeof(PasswordBoxAssistant), new PropertyMetadata(false));

		private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			PasswordBox box = d as PasswordBox;

			// only handle this event when the property is attached to a PasswordBox
			// and when the BindPassword attached property has been set to true
			if (d == null || !GetBindPassword(d))
			{
				return;
			}

			// avoid recursive updating by ignoring the box's changed event
			box.PasswordChanged -= HandlePasswordChanged;

			string newPassword = (string)e.NewValue;

			if (!GetUpdatingPassword(box))
			{
				box.Password = newPassword;
			}

			box.PasswordChanged += HandlePasswordChanged;
		}

		private static void OnBindPasswordChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
		{
			// when the BindPassword attached property is set on a PasswordBox,
			// start listening to its PasswordChanged event

			PasswordBox box = dp as PasswordBox;

			if (box == null)
			{
				return;
			}

			bool wasBound = (bool)(e.OldValue);
			bool needToBind = (bool)(e.NewValue);

			if (wasBound)
			{
				box.PasswordChanged -= HandlePasswordChanged;
			}

			if (needToBind)
			{
				box.PasswordChanged += HandlePasswordChanged;
			}
		}

		private static void HandlePasswordChanged(object sender, RoutedEventArgs e)
		{
			PasswordBox box = sender as PasswordBox;

			// set a flag to indicate that we're updating the password
			SetUpdatingPassword(box, true);
			// push the new password into the BoundPassword property
			SetBoundPassword(box, box.Password);
			SetUpdatingPassword(box, false);
		}

		public static void SetBindPassword(DependencyObject dp, bool value)
		{
			dp.SetValue(BindPassword, value);
		}

		public static bool GetBindPassword(DependencyObject dp)
		{
			return (bool)dp.GetValue(BindPassword);
		}

		public static string GetBoundPassword(DependencyObject dp)
		{
			return (string)dp.GetValue(BoundPassword);
		}

		public static void SetBoundPassword(DependencyObject dp, string value)
		{
			dp.SetValue(BoundPassword, value);
		}

		private static bool GetUpdatingPassword(DependencyObject dp)
		{
			return (bool)dp.GetValue(UpdatingPassword);
		}

		private static void SetUpdatingPassword(DependencyObject dp, bool value)
		{
			dp.SetValue(UpdatingPassword, value);
		}
	}
}
