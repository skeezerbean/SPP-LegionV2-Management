using Caliburn.Micro;
using MahApps.Metro.Controls.Dialogs;
using MySql.Data.MySqlClient;
using System;
using System.Security;
using System.Threading.Tasks;
using System.Windows;

namespace SPP_LegionV2_Management
{
	public class AccountManagerViewModel : Screen
	{
		// IDialogCoordinator is for metro message boxes
		private readonly IDialogCoordinator _dialogCoordinator;

		// block parallel tasks running of the same type
		private bool _accountRetrieveRunning = false;
		private bool _characterRetrieveRunning = false;
		private bool _deleteAccountRunning = false;
		private bool _deleteCharacterRunning = false;
		private bool _removingObjects = false;
		private bool _retrievingObjects = false;
		private BindableCollection<int> _guildMasters = new BindableCollection<int>();

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
		public string AccountStatus { get { return (CharacterStatus == null) ? string.Empty : CharacterStatus; } set { CharacterStatus = value; } }

		// Characters
		public int CharactersTotal { get; set; }
		public int CurrentCharacterGUID { get { return (SelectedCharacter == null) ? -1 : SelectedCharacter.Guid; } }
		public int CurrentCharacterAccountID { get { return (SelectedCharacter == null) ? -1 : SelectedCharacter.Account; } set { SelectedCharacter.Account = value; } }
		public string CurrentCharacterName { get { return (SelectedCharacter == null) ? string.Empty : SelectedCharacter.Name; } set { SelectedCharacter.Name = value; } }
		public string CharacterStatus { get; set; }

		// Orphaned Characters
		public int OrphanedCharactersTotal { get; set; }
		public int OrphanedOrphanedCharactersTotal { get; set; }
		public int OrphanedCurrentCharacterGUID { get { return (OrphanedSelectedCharacter == null) ? -1 : OrphanedSelectedCharacter.Guid; } }
		public int OrphanedCurrentCharacterAccountID { get { return (OrphanedSelectedCharacter == null) ? -1 : OrphanedSelectedCharacter.Account; } set { OrphanedSelectedCharacter.Account = value; } }
		public string OrphanedCurrentCharacterName { get { return (OrphanedSelectedCharacter == null) ? string.Empty : OrphanedSelectedCharacter.Name; } set { OrphanedSelectedCharacter.Name = value; } }
		public string OrphanedCharacterStatus { get { return (CharacterStatus == null) ? string.Empty : CharacterStatus; } set { CharacterStatus = value; } }

		// Orphaned Objects
		public int OrphanedObjectsTotal { get; set; }
		public BindableCollection<int> OrphanedIDs = new BindableCollection<int>();
		public int OrphanedIDsTotal { get; set; }
		public string OrphanedObjectsStatus { get { return (CharacterStatus == null) ? string.Empty : CharacterStatus; } set { CharacterStatus = value; } }

		// IDialogCoordinator is part of Metro, for dialog handling in the view model
		public AccountManagerViewModel(IDialogCoordinator instance)
		{
			_dialogCoordinator = instance;
		}

		public async void ApplyAccountChanges()
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

			// Sometimes screen updates too quickly before DB changes applies, this should help
			await Task.Delay(500);
			// now that things have been updated, refresh our list
			_ = RetrieveAccounts();
		}

		// Button for Character changes calls this
		public void ApplyCharacterChanges() { ApplyCharacterChangesProcess(Characters); }

		// Button for Orphaned Character changes calls this
		public void ApplyOrphanedCharacterChanges() { ApplyCharacterChangesProcess(OrphanedCharacters); }

		// Work to actually change character settings
		public async void ApplyCharacterChangesProcess(BindableCollection<Character> characters)
		{
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

			// Sometimes screen updates too quickly before DB changes applies, this should help
			await Task.Delay(500);
			_ = RetrieveCharacters();
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
		public async Task RetrieveAccounts()
		{
			if (!CheckSQL() || _accountRetrieveRunning || _deleteAccountRunning || _deleteCharacterRunning)
				return;

			_accountRetrieveRunning = true;
			CharacterStatus = "Collecting Account Info...";
			// Clear the collection since we're re-using
			Accounts.Clear();

			// wrap this in a task, waiting for it to complete. This will let the UI update through progress
			Task t = Task.Run(() =>
			{
				MySqlDataReader reader = MySqlManager.MySQLQuery("SELECT `id`,`username`,IFNULL(`battlenet_account`,\"-1\") FROM `legion_auth`.`account`", GetAccountsFromSQL);
			});

			// Loop until it completes, awaiting each iteration to let the UI update
			while (!t.IsCompleted) { await Task.Delay(1); }

			CharacterStatus = "Account Retrieval Finished...";
			AccountsTotal = Accounts.Count;
			_accountRetrieveRunning = false;
		}

		public void AddAccount()
		{
			if (!CheckSQL())
				return;

			string result = string.Empty;
			string loginName = string.Empty;

			while (result != "-1")
			{
				// This should run through the process of adding an account with the login name,
				// to uppercase, the rest happens at account list screen to manage password/GM level
				loginName = Microsoft.VisualBasic.Interaction.InputBox("Enter the login name for the account", "Login Account Name").ToUpper();

				// If they hit cancel
				if (loginName.Length == 0)
					return;

				result = MySqlManager.MySQLQueryToString($"SELECT IFNULL((SELECT `email` FROM `legion_auth`.`battlenet_accounts` WHERE `email` = '{loginName}'), \"-1\")");
				if (result != "-1")
					MessageBox.Show($"BattleNet Login \"{loginName}\" already exists. Please try again.");
			}

			// Create the DB entries, battle_net first, then we can pull the ID it creates and add to account table after
			MySqlManager.MySQLQueryToString($"INSERT INTO `legion_auth`.`battlenet_accounts` (`email`) VALUES ('{loginName}')", true);
			int battlenetID = Int32.Parse(MySqlManager.MySQLQueryToString($"SELECT `id` FROM `legion_auth`.`battlenet_accounts` WHERE `email`='{loginName}'"));
			MySqlManager.MySQLQueryToString($"INSERT INTO `legion_auth`.`account` (`username`,`email`,`battlenet_account`) VALUES ('{loginName}','{loginName}','{battlenetID}')", true);

			// Pop up to let user know to create a password on the account page once the list is refreshed.
			// this lets us used the existing more-secure method than simply taking text input
			MessageBox.Show("Note - once the account list refreshes, you will need to set a password for this account before it can be used.", "Note");
			_ = RetrieveAccounts();
		}

		// Called from button, pass to actual character deletion
		public async void DeleteSelectedCharacter()
		{
			if (!CheckSQL() || _deleteCharacterRunning || _removingObjects || _deleteAccountRunning)
				return;

			CharacterStatus = $"Deleting Character {SelectedCharacter.Name}";
			Task t = Task.Run(() =>
			{
				DeleteCharacter(SelectedCharacter);
			});

			// Give it a moment to start before checking, lets the bool check get set
			await Task.Delay(100);
			while (_deleteCharacterRunning) { await Task.Delay(1); }

			CharacterStatus = $"{SelectedCharacter.Name} has been removed.";

			// Sometimes screen updates too quickly before DB changes applies, this should help
			await Task.Delay(500);
			RetrieveCharacters();
		}

		// called from button, pass to actual character deletion
		public async void DeleteOrphanedCharacters()
		{
			if (!CheckSQL() || _deleteCharacterRunning || _removingObjects || _deleteAccountRunning)
				return;

			// Refresh if we don't know of any orphaned characters
			if (OrphanedCharacters.Count == 0)
			{
				Task t = Task.Run(() =>
				{
					// Refresh or make sure we have updated Orphaned character info
					RetrieveCharacters();
				});

				// Give it a moment to start before checking, lets the bool check get set
				await Task.Delay(1);
				while (_characterRetrieveRunning) { await Task.Delay(1); }
			}

			// Sometimes at the right moment the character list may refresh when someone clicks a button. hopefully this keeps from crashing
			try
			{
				foreach (var character in OrphanedCharacters)
				{
					// Run the deletion for each
					Task t = Task.Run(() =>
					{
						DeleteCharacter(character);
					});

					// Give it a moment to start before checking, lets the bool check get set
					await Task.Delay(100);
					while (_deleteCharacterRunning) { await Task.Delay(1); }
				}
			}
			catch { }
			// Sometimes screen updates too quickly before DB changes applies, this should help
			CharacterStatus = "Orphaned characters removal finished";
			await Task.Delay(500);
			// Refresh list of characters
			_ = RetrieveCharacters();
		}

		// Delete specified character passed in, either from deleting selected character, or
		// running through all characters in an account being deleted
		private async Task DeleteCharacter(Character character)
		{
			if (!CheckSQL() || _deleteCharacterRunning || _removingObjects)
				return;

			_deleteCharacterRunning = true;

			// Run the deletion for each
			Task t = Task.Run(() =>
			{
				// remove rows with info for this character
				foreach (var entry in CharacterTableField.CharacterTableFields)
				{
					//MySqlManager.MySQLQueryToString($"DELETE FROM `{entry.table}` WHERE `{entry.field}` = '{character.Guid}'", true);
					RemoveObjectRows(entry.table, entry.field, character.Guid);
				}
			});
			Task.Delay(100);
			while (_removingObjects) { await Task.Delay(1); }

			// then finally delete the character itself
			CharacterStatus = $"{character.Name} removed";
			MySqlManager.MySQLQueryToString($"DELETE FROM `legion_characters`.`characters` WHERE `guid`='{character.Guid}'", true);

			// reset the flag and update the lists
			_deleteCharacterRunning = false;
		}

		public async Task DeleteSelectedAccount()
		{
			if (!CheckSQL() || _deleteAccountRunning || _deleteCharacterRunning || _removingObjects)
				return;

			CharacterStatus = $"Collecting character info for Account:{SelectedAccount.BattleNetAccount}";

			// Get a fresh character list, or populate a new one
			Task t = Task.Run(() =>
			{
				RetrieveCharacters();
			});

			// give some time to let the character retrieve start
			await Task.Delay(100);

			// Loop until task completes, awaiting each iteration to let the UI update
			while (_characterRetrieveRunning) { await Task.Delay(1); }

			_deleteAccountRunning = true;
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
						CharacterStatus = $"Removing character {character.Name}";
						DeleteCharacter(character);
					});

					await Task.Delay(100);
					// Loop until it completes, awaiting each iteration to let the UI update
					while (_deleteCharacterRunning) { await Task.Delay(1); }
				}

				// Delete account, bnet, gm entry if exists from SQL then retrieve accounts/characters again to refresh from DB directly, verify account is gone
				MySqlManager.MySQLQueryToString($"DELETE FROM `legion_auth`.`account_access` WHERE `id`='{SelectedAccount.ID}'", true);
				MySqlManager.MySQLQueryToString($"DELETE FROM `legion_auth`.`battlenet_accounts` WHERE `id`='{SelectedAccount.BattleNetAccount}'", true);
				MySqlManager.MySQLQueryToString($"DELETE FROM `legion_auth`.`account` WHERE `id`='{SelectedAccount.ID}'", true);
				CharacterStatus = $"Removal of Account:{SelectedAccount.BattleNetEmail} completed";

				t = Task.Run(async () =>
				{
					await RetrieveCharacters();
					await RetrieveAccounts();
				});
			}

			_deleteAccountRunning = false;
		}

		// Check the DB against our list, see if any orphaned objects exist. If so, remove
		public async void RemoveOrphanedItems()
		{
			if (!CheckSQL() || _removingObjects || _retrievingObjects || _deleteAccountRunning || _deleteCharacterRunning)
				return;

			int completedItems = 0;

			_retrievingObjects = true;
			_removingObjects = true;
			OrphanedObjectsTotal = 0;
			CharacterStatus = "Gathering Data...";

			foreach (var entry in CharacterTableField.CharacterTableFields)
			{
				OrphanedObjectsTotal += GetOrphanedTotalRows(entry.table, entry.field);
			}

			_retrievingObjects = false;

			// If none, skip out
			if (OrphanedObjectsTotal == 0)
			{
				CharacterStatus = "No Orphaned Objects found";
				_removingObjects = false;
				return;
			}

			// Setup a task to run through each entry, update the GUI with completion %
			foreach (var entry in CharacterTableField.CharacterTableFields)
			{
				Task t = Task.Run(() =>
				{
					CharacterStatus = $"{(int)(0.1f + ((100f * completedItems) / CharacterTableField.CharacterTableFields.Count))}% - Removing objects from table `{entry.table}`";
					RemoveObjectRows(entry.table, entry.field);
					completedItems++;
				});

				// Loop until it completes, awaiting each iteration to let the UI update
				while (!t.IsCompleted) { await Task.Delay(1); }
			}

			CharacterStatus = "Finished removing orphaned items";
			_removingObjects = false;
		}

		// Get our of orphaned objects, anything belonging to a character ID that doesn't exist in the
		// characters table, and isn't part of the AH auctions, return the matches to add to our total
		private int GetOrphanedTotalRows(string table, string field)
		{
			// If the orphaned items in item_instance match an item in the auctionhouse, then we need to ignore
			if (table == "legion_characters`.`item_instance")
				return Int32.Parse(MySqlManager.MySQLQueryToString($"SELECT COUNT(*) FROM `{table}` WHERE `{field}` "
					+ "NOT IN (SELECT `guid` FROM `legion_characters`.`characters`) AND `guid` NOT IN (SELECT `itemguid` FROM `legion_characters`.`auctionhouse`)"));

			return Int32.Parse(MySqlManager.MySQLQueryToString($"SELECT COUNT(*) FROM `{table}` WHERE `{field}` NOT IN (SELECT `guid` FROM `legion_characters`.`characters`)"));
		}

		// If our guid is -1 then it's removing orphaned objects. Otherwise it's targetting specific guid to remove
		private void RemoveObjectRows(string table, string field, int guid = -1)
		{
			string defaultQuery = $"DELETE FROM `{table}` WHERE `{((guid == -1) ? $"{field}` NOT IN (SELECT `guid` FROM `legion_characters`.`characters`)" : $"{field}` = '{guid}'")}";

			// If the character ID matches a guild master, then we need to check if there's a replacement guild member available
			// There could be multiple matches and need handled 1 at a time. This character ID will get removed from the guild_member
			// table whenever it gets to that object anyways, so no need to handle that part here
			if (table == "legion_characters`.`guild")
			{
				// this will be our holder of guildmasters without a matching character ID
				_guildMasters.Clear();

				// Either grabbing all orphaned guildmasters, or checking for specific one
				MySqlDataReader reader = MySqlManager.MySQLQuery("SELECT IFNULL((SELECT `leaderguid` FROM `legion_characters`.`guild` WHERE `leaderguid` "
					+ $"{((guid == -1) ? "NOT IN(SELECT `guid` FROM `legion_characters`.`characters`)" : $"= '{guid}'")}), \"-1\")", GetGuildMasters);

				// now our _guildMasters collection is populated
				if (_guildMasters.Count > 0)
				{
					foreach (var guildmaster in _guildMasters)
					{
						// Get a count of members for this guild, minus guildmaster, and has a match in characters table (non-orphaned)
						int guildID = Int32.Parse(MySqlManager.MySQLQueryToString($"SELECT IFNULL((SELECT `guildid` FROM `legion_characters`.`guild` WHERE `leaderguid` = '{guildmaster}'), \"-1\")"));
						int membercount = Int32.Parse(MySqlManager.MySQLQueryToString($"SELECT COUNT(*) FROM `legion_characters`.`guild_member` WHERE `guildid` = '{guildID}' AND NOT 'guid' = '{guildmaster}' AND `guid` IN (SELECT `guid` FROM `legion_characters`.`characters`)"));

						if (membercount > 0)
						{
							// We need to find a replacement guildmaster, and this will return the first match for this guild ID
							// in ranking order, so whatever guid returns first as the highest rank, congrats. You're the leader
							// and Uncle Ben will lecture you about great power and responsbility.
							int newGM = Int32.Parse(MySqlManager.MySQLQueryToString($"SELECT IFNULL((SELECT `guid` FROM `legion_characters`.`guild_member` WHERE `guildid` = '{guildID}' AND NOT `guid` = '{guildmaster}' ORDER BY `rank` LIMIT 1), \"-1\")"));

							// Now update the guild leaderguid record with the new guildmaster
							if (newGM == -1)
								// This means there were no valid guild members, so run the default
								MySqlManager.MySQLQueryToString(defaultQuery, true);
							else
							{
								// Set the new guildmaster, and update guild_member rank
								MySqlManager.MySQLQueryToString($"UPDATE `legion_characters`.`guild` SET `leaderguid` = '{newGM}' WHERE `leaderguid` = '{guildmaster}'", true);
								MySqlManager.MySQLQueryToString($"UPDATE `legion_characters`.`guild_member` SET `rank` = '0' WHERE `guid` = '{newGM}'", true);
							}
						}
						else
						{
							// This means guildmaster was the only entry, so let's remove
							MySqlManager.MySQLQueryToString(defaultQuery, true);
						}
					}
				}
			}
			else if (table == "legion_characters`.`item_instance")
			{
				// The guid may be the same as the auctionhouse, so we need to make sure we're not removing any items that
				// are listed in the auctionhouse table, otherwise they may be orphaned auctions that the AH code didn't clean up
				MySqlManager.MySQLQueryToString("DELETE FROM `legion_characters`.`item_instance` WHERE `owner_guid` "
					+ $"{((guid == -1) ? "NOT IN (SELECT `guid` FROM `legion_characters`.`characters`) AND `owner_guid` NOT IN (SELECT `itemguid` FROM `legion_characters`.`auctionhouse`)" : $"= '{guid}'")}", true);
				// optimize the table. Not supported in InnoDB, but will perform the same sort of thing and shrink the table to release the now-empty space
				MySqlManager.MySQLQueryToString("OPTIMIZE TABLE `legion_characters`.`item_instance`");
			}
			else
			{
				// Nothing else has matched, so squirt out our default
				MySqlManager.MySQLQueryToString(defaultQuery, true);
			}
		}

		// Only here for the sake of button unique name
		public void RetrieveOrphanedCharacters() { RetrieveCharacters(); }

		// Incoming reader is from the RemoveOrphanedRows function
		private void GetGuildMasters(MySqlDataReader reader)
		{
			try
			{
				// Add each guild master that matches the query into the collection
				_guildMasters.Add(reader.GetInt32(0));
			}
			catch (Exception e) { Console.WriteLine($"GetOrphanedGuildMasters: {e.Message}"); }
		}

		// This creates a 'reader' to pull character data from the database, called from multiple functions
		public async Task RetrieveCharacters()
		{
			if (!CheckSQL() || _characterRetrieveRunning || _deleteAccountRunning || _deleteCharacterRunning)
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
			while (!t.IsCompleted) { await Task.Delay(1); }

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
				account.GMLevel = Int32.Parse(MySqlManager.MySQLQueryToString($"SELECT IFNULL((SELECT `gmlevel` FROM `legion_auth`.`account_access` WHERE `id` = '{account.ID}'), \"-1\")"));

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