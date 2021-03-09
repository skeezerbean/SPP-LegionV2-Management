using Caliburn.Micro;
using MahApps.Metro.Controls.Dialogs;
using MySql.Data.MySqlClient;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Security;

namespace SPP_LegionV2_Management
{
	public class AccountManagerViewModel : Screen
	{
		// IDialogCoordinator is for metro message boxes
		private readonly IDialogCoordinator _dialogCoordinator;
		private DateTime _lastUpdate = DateTime.Now;
		private int _tempObjectsOrphanedTotal;

		// block parallel tasks running of the same type
		private bool _accountRetrieveRunning = false;
		private bool _characterRetrieveRunning = false;
		private bool _deleteAccountRunning = false;
		private bool _deleteCharacterRunning = false;
		private bool _deleteObjectsRunning = false;
		private bool _removingObjects = false;
		private bool _retrievingObjects = false;

		public BindableCollection<Account> Accounts { get; set; } = new BindableCollection<Account>();
		public BindableCollection<Character> Characters { get; set; } = new BindableCollection<Character>();
		public BindableCollection<Character> OrphanedCharacters { get; set; } = new BindableCollection<Character>();
		public Character SelectedCharacter { get; set; } = new Character();
		public Character OrphanedSelectedCharacter { get; set; } = new Character();
		public Account SelectedAccount { get; set; }

		// Accounts
		public int AccountsTotal { get; set; }
		public int CurrentID { get { return (SelectedAccount == null) ? -1 : SelectedAccount.ID; } }
		public int CurrentBattleNetAccount { get { return (SelectedAccount == null) ? -1 : SelectedAccount.BattleNetAccount; } }
		public string CurrentBattleNetEmail { get { return (SelectedAccount == null) ? string.Empty : SelectedAccount.BattleNetEmail; } set { SelectedAccount.BattleNetEmail = value; } }
		public string CurrentUsername { get { return (SelectedAccount == null) ? string.Empty : SelectedAccount.Username; } set { SelectedAccount.Username = value; } }
		public int CurrentBattleCoins { get { return (SelectedAccount == null) ? -1 : SelectedAccount.BattleCoins; } set { SelectedAccount.BattleCoins = value; } }
		public int CurrentGMLevel { get { return (SelectedAccount == null) ? -1 : SelectedAccount.GMLevel; } set { SelectedAccount.GMLevel = value; } }
		public int CurrentBattleNetID { get { return (SelectedAccount == null) ? -1 : SelectedAccount.BattleNetAccount; } }
		public SecureString SecurePassword { get { return (SelectedAccount == null) ? new SecureString() : SelectedAccount.SecurePassword; } set { SelectedAccount.SecurePassword = value; } }

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

		// Orphaned Objects
		public int OrphanedObjectsTotal { get; set; }
		public BindableCollection<int> OrphanedIDs = new BindableCollection<int>();
		public int OrphanedIDsTotal { get; set; }
		public string OrphanedObjectsStatus { get; set; }

		// IDialogCoordinator is part of Metro, for dialog handling in the view model
		public AccountManagerViewModel(IDialogCoordinator instance)
		{
			_dialogCoordinator = instance;
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

					if (battlenetLoginFromDB != account.BattleNetEmail.ToUpper())
					{
						// Changing BattleNet Email means the password hash MUST be updated, or this account will no longer be able
						// to login. The password box on the account page needed filled out. If not, we alert and ignore the 
						// BattleNet Email change. With the Controls/Externsions classes, we're able to keep the password in a
						// SecureString until passing directly into the generation of the hash
						if (account.SecurePassword != null && account.SecurePassword.Length > 0)
						{
							// the WoW Bnet password hash is built by getting uppercase hash of the (uppercase) email login, then combining
							// that hash + ":" and the password, then reverse order, uppercase the result and shove in the DB
							passHash = Extensions.SecureStringExtensions.sha256_hash(Extensions.SecureStringExtensions.sha256_hash(account.BattleNetEmail.ToUpper()).ToUpper()
								+ ":" + Extensions.SecureStringExtensions.ToUnsecuredString(account.SecurePassword).ToUpper(), true).ToUpper();
							MessageBox.Show($"Changing BattleNet Login for [{battlenetLoginFromDB}] to [{account.BattleNetEmail.ToUpper()}] - "
							+ MySqlManager.MySQLQueryToString($"UPDATE `legion_auth`.`battlenet_accounts` SET `email`='{account.BattleNetEmail.ToUpper()}',`sha_pass_hash`='{passHash}' WHERE `id`='{account.BattleNetAccount}'", true));

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
					if (account.SecurePassword != null && !_updatedPassword && account.SecurePassword.Length > 0)
					{
						passHash = Extensions.SecureStringExtensions.sha256_hash(Extensions.SecureStringExtensions.sha256_hash(account.BattleNetEmail.ToUpper()).ToUpper()
							+ ":" + Extensions.SecureStringExtensions.ToUnsecuredString(account.SecurePassword).ToUpper(), true).ToUpper();
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

			// This should run through the process of adding an account with the login name,
			// to uppercase, the rest happens at account list screen to manage password/GM level
			string loginName = Microsoft.VisualBasic.Interaction.InputBox("Enter the login name for the account", "Login Account Name").ToUpper();

			// If they hit cancel
			if (loginName.Length == 0)
				return;

			// Create the DB entries, battle_net first, then we can pull the ID it creates and add to account table after
			MySqlManager.MySQLQueryToString($"INSERT INTO `legion_auth`.`battlenet_accounts` (`email`) VALUES ('{loginName}')", true);
			int battlenetID = Int32.Parse(MySqlManager.MySQLQueryToString($"SELECT `id` FROM `legion_auth`.`battlenet_accounts` WHERE `email`='{loginName}'"));
			MySqlManager.MySQLQueryToString($"INSERT INTO `legion_auth`.`account` (`username`,`email`,`battlenet_account`) VALUES ('{loginName}','{loginName}','{battlenetID}')", true);

			// Pop up to let user know to create a password on the account page once the list is refreshed.
			// this lets us used the existing more-secure method than simply taking text input
			RetrieveAccounts();
			MessageBox.Show("Note - once the account list refreshes, you will need to set a password for this account before it can be used.", "Note");
		}

		// Called from button, pass to actual character deletion
		public async void DeleteSelectedCharacter()
		{
			if (!CheckSQL() || _deleteCharacterRunning)
				return;

			Task t = Task.Run(() =>
			{
				Console.WriteLine($"Deleting character: {SelectedCharacter}");
				DeleteCharacter(SelectedCharacter);
			});

			// Give it a moment to start before checking, lets the bool check get set
			Task.Delay(100);
			while (!t.IsCompleted) { await Task.Delay(100); }
		}

		// called from button, pass to actual character deletion
		public async void DeleteOrphanedCharacters()
		{
			if (!CheckSQL() || _deleteCharacterRunning)
				return;

			// Refresh or make sure we have updated Orphaned character info
			RetrieveCharacters();

			foreach (var character in OrphanedCharacters)
			{
				// Run the deletion for each
				Task t = Task.Run(() =>
				{
					Console.WriteLine($"Deleting orphaned character: {character}");
					DeleteCharacter(character);
				});

				// Give it a moment to start before checking, lets the bool check get set
				Task.Delay(100);
				while (!t.IsCompleted) { await Task.Delay(100); }
			}

			// Refresh list of characters
			RetrieveCharacters();
		}

		// Delete specified character passed in, either from deleting selected character, or
		// running through all characters in an account being deleted
		private async Task DeleteCharacter(Character character)
		{
			if (!CheckSQL() || _deleteCharacterRunning)
				return;

			_deleteCharacterRunning = true;

			// Run the deletion for each
			Task t = Task.Run(() =>
			{
				Console.WriteLine($"Deleting objects for character: {character}");
				// remove rows with info for this character
				RemoveObjectsByID(character.Guid);
			});

			// Give it a moment to start before checking, lets the bool check get set
			Task.Delay(100);
			while (!t.IsCompleted) { await Task.Delay(100); }

			// then finally delete the character itself
			MySqlManager.MySQLQueryToString($"DELETE FROM `legion_characters`.`characters` WHERE `guid`='{character.Guid}'", true);

			// reset the flag and update the lists
			_deleteCharacterRunning = false;
			RetrieveCharacters();
		}

		public async Task DeleteSelectedAccount()
		{
			if (!CheckSQL() || _deleteAccountRunning)
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
				// Cycle through all characters and issue delete for them
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
				MySqlManager.MySQLQueryToString($"DELETE FROM `legion_auth`.`account_access` WHERE `id`='{SelectedAccount.ID}'", true);
				MySqlManager.MySQLQueryToString($"DELETE FROM `legion_auth`.`battlenet_accounts` WHERE `id`='{SelectedAccount.BattleNetAccount}'", true);
				MySqlManager.MySQLQueryToString($"DELETE FROM `legion_auth`.`account` WHERE `id`='{SelectedAccount.ID}'", true);

				RetrieveAccounts();
				RetrieveCharacters();
			}

			_deleteAccountRunning = false;
		}

		public async void RemoveOrphanedItems()
		{
			if (!CheckSQL() || _removingObjects)
				return;

			_retrievingObjects = true;
			_removingObjects = true;
			_tempObjectsOrphanedTotal = 0;
			OrphanedIDs.Clear();
			OrphanedObjectsTotal = 0;
			OrphanedObjectsStatus = "Gathering Data...";

			Task t = Task.Run(() =>
			{
				RetrieveOrphanedItems();
			});

			// Loop until it completes, awaiting each iteration to let the UI update
			while (_retrievingObjects) { await Task.Delay(100); }

			OrphanedIDsTotal = OrphanedIDs.Count;
			OrphanedObjectsTotal = _tempObjectsOrphanedTotal;
			OrphanedObjectsStatus = "Gathering Complete, removing objects";

			if (OrphanedIDs.Count > 0)
			{
				int completedItems = 0;
				foreach (var id in OrphanedIDs)
				{
					t = Task.Run(() =>
					{
						OrphanedObjectsStatus = $"{(int)(0.5f + ((100f * completedItems) / OrphanedIDs.Count))}% - Removing objects for ID {id}";
						RemoveObjectsByID(id);
					});

					// Loop until it completes, awaiting each iteration to let the UI update
					while (!t.IsCompleted) { await Task.Delay(1); }
					completedItems++;
				}
			}

			OrphanedObjectsStatus = "Finished removing orphaned items";
			_removingObjects = false;
		}

		public async Task RemoveObjectsByID(int guid)
		{
			// Each table we gathered from, we'll remove rows with corresponding IDs
			await Task.Delay(1000);
		}

		public async Task RetrieveOrphanedItems()
		{
			// cycle through our tables and gather guid's for characters
			// wrap this in a task, waiting for it to complete. This will let the UI update through progress
			// Can each be run in sequence inside a single task?
			Task t = Task.Run(() =>
			{
				MySqlDataReader reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_account_data`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_achievement`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_achievement_progress`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_action`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_adventure_quest`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_achievement`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_archaeology`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_archaeology_finds`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_army_training_info`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_artifact`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_aura`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_aura_effect`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_banned`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_battleground_data`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_battleground_random`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_achievement`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_battle_pet_journal`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_achievement`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_chat_logos`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_cheat_common`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_cuf_profiles`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_currency`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_custom_event_reapeter`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_custom_reward`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_custom_server_rewards_reapeter`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_deathmatch`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_deathmatch_store`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_declinedname`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `owner_guid` FROM `legion_characters`.`character_donate`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_donate_service`", GetOrphanedIDsFromSQL);
				// Unsure about this one
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_equipmentsets`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_feed_log`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `CharacterGuid` FROM `legion_characters`.`character_garrison`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_garrison_blueprints`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_garrison_buildings`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_garrison_followers`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_garrison_missions`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_garrison_shipment`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_garrison_talents`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_gifts`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_glyphs`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_homebind`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `Guid` FROM `legion_characters`.`character_honor`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_instance`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_inventory`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_kill`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_lfg_cooldown`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_loot_cooldown`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `owner` FROM `legion_characters`.`character_pet`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `owner` FROM `legion_characters`.`character_pet_declinedname`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_pvp_talent`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_queststatus`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_queststatus_daily`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_queststatus_objectives`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_queststatus_rewarded`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_queststatus_seasonal`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_queststatus_weekly`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_queststatus_world`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_quests_ignored`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_rates`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_rename`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_reputation`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_reward`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_share`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_skills`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_social`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_spell`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_spell_cooldown`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_stats`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_stat_kill_creature`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_talent`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_transmogs`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_transmog_outfits`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_visuals`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`character_visual_enchant`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `playerGuid` FROM `legion_characters`.`character_void_storage`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`gm_tickets`", GetOrphanedIDsFromSQL);
				// Need to figure out handling guilds if leader removed - table character.guild - leaderguid
				// same with guild_eventlog, PlayerGuid1, PlayerGuid2

				reader = MySqlManager.MySQLQuery("SELECT `playerGuid` FROM `legion_characters`.`guild_finder_applicant`", GetOrphanedIDsFromSQL);

				// where rank > 0 ??? to not delete leader
				reader = MySqlManager.MySQLQuery("SELECT `guid` FROM `legion_characters`.`guild_member`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `PlayerGuid` FROM `legion_characters`.`guild_newslog`", GetOrphanedIDsFromSQL);

				// This may be the really large query
				reader = MySqlManager.MySQLQuery("SELECT `owner_guid` FROM `legion_characters`.`item_instance`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `char_guid` FROM `legion_characters`.`item_instance_artifact`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `char_guid` FROM `legion_characters`.`item_instance_artifact_powers`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `char_guid` FROM `legion_characters`.`item_instance_relics`", GetOrphanedIDsFromSQL);
				reader = MySqlManager.MySQLQuery("SELECT `player_guid` FROM `legion_characters`.`item_refund_instance`", GetOrphanedIDsFromSQL);

				// How to handle mail, queue, mail_items?

			});

			// Loop until it completes, awaiting each iteration to let the UI update
			while (!t.IsCompleted)
			{
				// Let the UI update
				await Task.Delay(1000);
				OrphanedIDsTotal = OrphanedIDs.Count;
				OrphanedObjectsTotal = _tempObjectsOrphanedTotal;
			}

			_retrievingObjects = false;
		}

		// Only here for the sake of button unique name
		public void RetrieveOrphanedCharacters() { RetrieveCharacters(); }

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

		// Incoming reader is from the RetrieveOrphanedItems task
		private async void GetOrphanedIDsFromSQL(MySqlDataReader reader)
		{
			try
			{
				bool match;
				int guid = reader.GetInt32(0);
				string existingChar = MySqlManager.MySQLQueryToString($"SELECT IFNULL((SELECT `guid` FROM `legion_characters`.`characters` WHERE `guid` = '{guid}'), \"-1\")");

				// will be -1 if the guid doesn't exist
				if (existingChar == "-1")
				{
					match = false;

					foreach (var id in OrphanedIDs)
						if (id == guid)
							match = true;

					// If we don't have this ID already, add to the list
					if (!match)
						OrphanedIDs.Add(guid);

					_tempObjectsOrphanedTotal++;
				}
			}
			catch (Exception e) { Console.WriteLine($"Getting OrphanedObject: {e.Message}"); }
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
}
