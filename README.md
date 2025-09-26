![](https://github.com/skeezerbean/SPP-LegionV2-Management/blob/main/Resources/SPP-Config-Splash.png)

Use of this software in any way must be in agreement with the license listed in the license.txt file

# SPP-LegionV2-Management
## Configuration and Management for SPP LegionV2 Servers

THANK YOU to Serithul of SPP discord for helping with SQL. This project wouldn't be half of what it is without you :)
Also special thanks for m1#2698 for some app images!

------------------------

### Backing up your database - use [THIS!](https://github.com/skeezerbean/SPP-LegionV2-Management/blob/main/Resources/How_to_use_HeidiSQL_to_backup_your_databases.pdf) for directions how to do that

------------------------

### Usage

This application requires .net framework 4.8. 

### Updating

To update your current app to the latest, you just download the latest release and extract. Overwrite your existing app. The release doesn't contain a settings file, so it won't overwrite your existing settings or your backups.

### About
This tool helps build working World and Bnet server config files without duplicate entries, check your configuration for any potential issues, manage accounts/characters as well as potentially clean up the database of orphaned characters/objects. There are some things to be aware of -


The MySQL server will probably error connecting unless you're running this on the same server. You should **keep the MySQL server set to 127.0.0.1** and user/password should be left to defaults in the config file. Delete the settings.json file to reset them.


This tool can also update the config file in your WOW client configuration to make sure it matches with the rest of the configuration, assuming that this tool can access the folder/file. If you run your WOW Client from another PC, then you may need to set this manually to match the **[LoginREST.ExternalAddress]** from the **bnetserver.conf**, otherwise you may have trouble with your WOW client contacting the server. These addresses can be changed by using the **[Set IP]** button, and it can update the entries automatically. If you have your wow client on a machine other than the server, you can copy a 'fake' wow config.wtf to the server and set the location in the tool to bypass any errors for it, or browse via UNC path as long as the tool has rights to access across the network. Just make sure you update your wow config manually if the portal address needs changed if it doesn't point to your actual config. When using the **[Set IP]** button, it sets the hosting IP in the bnet config and database realm entry, as well as the WOW client config file to keep them the same. This is the IP you'd set for hosting the WoW server on the local LAN, or internet, depending on your setup and helps to keep those settings synced


If there is a problem, you can use the **[Set Defaults]** button to pull the wow config fresh from the local template files. **This will overwrite all previous settings** for the Bnet and World config files. 


If you see an error similar to **[not allowed to connect to this MySQL server]** then you're probably running this on a different computer or the database isn't running. Run it from the SPP server.


To begin - **Make sure** the SPP Database is running

![](https://github.com/skeezerbean/SPP-LegionV2-Management/blob/main/Resources/database-started.PNG)


### Config Generator


Click on the **Settings** button

![](https://github.com/skeezerbean/SPP-LegionV2-Management/blob/main/Resources/Settings-Tab.PNG)


When browsing for the SPP folder, it will be the one that has these items where you extracted your SPP LegionV2 Server files. Note that changing a folder automatically saves the new locations, as well as the window position/size

![](https://github.com/skeezerbean/SPP-LegionV2-Management/blob/main/Resources/SPP-server-folder.PNG)


Browsing for the Wow Config Location will be your WoW Legion **Client** folder, which will look more or less like this. Go to the WTF folder and choose the config file. Please note that you need to run the client at least once to generate the config file so that the app can find it. No need to login, just run the client and then exit

![](https://github.com/skeezerbean/SPP-LegionV2-Management/blob/main/Resources/wow-client-folder.PNG)


Once you've set the folder locations, click these buttons

![](https://github.com/skeezerbean/SPP-LegionV2-Management/blob/main/Resources/Config-Generator-Button.PNG)

![](https://github.com/skeezerbean/SPP-LegionV2-Management/blob/main/Resources/Reload-Configs-Button.PNG)


This will pull in data from your current config and populate the World/Bnet tabs. Click on the World tab to see the config

![](https://github.com/skeezerbean/SPP-LegionV2-Management/blob/main/Resources/World-Config-Loaded.PNG)


Click the "Check Config" button

![](https://github.com/skeezerbean/SPP-LegionV2-Management/blob/main/Resources/Check-Config-Button.PNG)


This will show any potential issues with your setup. The picture below shows an issue with duplicate entry, as well as a warning for the value having a comment in the line

![](https://github.com/skeezerbean/SPP-LegionV2-Management/blob/main/Resources/Check-Config-Errors-Duplicates.PNG)


Use the search tool to find the entry. This will filter results as you type

![](https://github.com/skeezerbean/SPP-LegionV2-Management/blob/main/Resources/Search-entry-duplicate.PNG)


Click the entry, and you'll see that it's highlighted in blue in that row

![](https://github.com/skeezerbean/SPP-LegionV2-Management/blob/main/Resources/Select-Line-To-Delete.PNG)


Press Delete on your keyboard, and the line will be gone

![](https://github.com/skeezerbean/SPP-LegionV2-Management/blob/main/Resources/After-Delete.PNG)


You can now Check Config again, and you can see that the alert is now gone

![](https://github.com/skeezerbean/SPP-LegionV2-Management/blob/main/Resources/Check-Config-Success.PNG)


If you are missing some entries, such as new lines added from one of the updates, this will alert you when you **Check Config** (and automatically add them and their default values to your config)

![](https://github.com/skeezerbean/SPP-LegionV2-Management/blob/main/Resources/Check-Config-Missing-Entries.PNG)


The **[Game.Build.Version]** must be the same as whatever client you're using. You can find this by launching the Legion client, and checking the build # at the bottom-left

![](https://github.com/skeezerbean/SPP-LegionV2-Management/blob/main/Resources/Legion-Client-Build.PNG)


If this is different than what your config has set, then use the **Set Build** button to update. You'll enter the build number from your legion client, which may be different than the example above

![](https://github.com/skeezerbean/SPP-LegionV2-Management/blob/main/Resources/Set-Build.PNG)


Once you're ready, click on **Export** to save your updated configs to disk. This will auto-backup existing configs into the SPP Config Generator's backup folder before saving new ones

![](https://github.com/skeezerbean/SPP-LegionV2-Management/blob/main/Resources/Save-Export-Backup.PNG)


Once the config has been exported then you can start/restart your bnet/world servers to use the updated configuration.


### Account Manager


To use this, click on the **Account Manager** button

![](https://github.com/skeezerbean/SPP-LegionV2-Management/blob/main/Resources/Account-Manager-Button.PNG)


On the Account Manager screen, in the **Accounts** tab - 

**1** - Pulls the list of accounts from the database

**2** - You can select an account and see/change the settings on the right side

**3** - Change settings here for an account. If the BattleNet ID or Login is -1, then chances are that BattleNet account doesn't exist. Leave it alone. The account may have not been fully created. Otherwise you can change the number of Battle Coins for an account, change the BattleNet Email or Account Username. **Note - if you change the BattleNet Email for login, you will need to enter a password as well as they are tied together in how the password hash is created (TrinityCore). This can be the original password for this account, or you can set a new one.**  You can also check the box to upgrade all BattleNet pets for the selected account to be rare quality (may break some pets if not all can be upgraded), as well as set their current xp amount to 2000 which should set them all ready for level up

**4** - Click this when done editing characters and it will push changes to the database. 

![](https://github.com/skeezerbean/SPP-LegionV2-Management/blob/main/Resources/Account-Manager-Accounts-Screen.PNG)


If you changed any account information and applied, it will push those updates to the database and refresh the account list. If you tried submitting a value in fields that was not acceptable (alphanumeric in number fields, etc) then those are ignored. You'll also get feedback on changes

![](https://github.com/skeezerbean/SPP-LegionV2-Management/blob/main/Resources/Account-Manager-Accounts-Update.PNG)


You can also list and change some character information on the **Characters** tab. 

**1** - Gathers the list of characters in the database

**2** - Select a character, and it will populate settings on the right

**3** - Change settings here. You can rename a character (length is limited in the database up to 12 characters), or change the account it's attached to. Make sure you don't change this to a non-existant account or the character will be orphaned. If that's the case, you'll be able to make these changes to change it back on that tab

**4** - Click this when done, and it will push changes to the database

![](https://github.com/skeezerbean/SPP-LegionV2-Management/blob/main/Resources/Account-Manager-Characters.PNG)


If you changed any information then the app will notify you

![](https://github.com/skeezerbean/SPP-LegionV2-Management/blob/main/Resources/Account-Manager-Characters-Update.PNG)


You can change some character information, and remove orphaned characters, on the **Orphaned Characters** tab. 

**1** - Gathers the list of characters in the database, or select to remove all. Example shows removing all and as it shows progress. You'll have to close the app to get it to stop removing, or wait until the task is finished if you clicked to remove all orphaned characters

**2** - Select a character, and it will populate settings on the right. This will let you change the account to associate with a legit account if needed

**3** - Change settings here. You can rename a character (length is limited in the database up to 12 characters), or change the account it's attached to, such as moving it back to a legit account

**4** - Click this when done, and it will push changes to the database

![](https://github.com/skeezerbean/SPP-LegionV2-Management/blob/main/Resources/Account-Manager-OrphanedCharacters.PNG)


Use this to search for and remove orphaned objects from the database. These are items belonging to a character that no longer exists in the database, and adds a lot of extra info to be stored that isn't needed. At the time of writing this, it also includes massive amounts of items from the AH Bot, as it doesn't remove items from the item_instance table when auctions expire, or when the AHBot buys items if that setting is enabled. On a test machine this was 1.9 million orphaned objects. On the Aurora partner server, it showed over 10 million objects orphaned. This should be corrected in a future patch (to stop orphaning items), but in the mean time you can use this to potentially clean up your server and reduce memory usage. Note, if you have a lot of items it will take a LONG WHILE to finish.

This tab has also been updated so that you can limit the number of rows being processed in case the query is taking too long (very possible on non-SSD drives). You can continue to re-run the removal of orphaned objects process another set of rows based on your limit. This may be easier to just run some over time to help clean up the database. The default 100,000 should be mostly ok to run, but if your drive/system is slow then this may take a while and need set to something lower.

![](https://github.com/skeezerbean/SPP-LegionV2-Management/blob/main/Resources/Clean-Orphaned-Objects.PNG)
