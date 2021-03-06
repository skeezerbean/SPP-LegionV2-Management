# SPP-LegionV2-Management
## Configuration and Management for SPP LegionV2 Servers

THANK YOU to Serithul of SPP discord for helping with SQL. This project wouldn't be half of what it is without you :)

### Usage

This application requires .net framework 4.8. 

(screenshots for setup and usage at the bottom) - Make sure the **SPP Database** engine is running.

You can use the **[Set IP]** button to set the hosting IP in the bnet config and database realm entry, as well as the WOW client config.wtf file to keep them the same. This is the IP you'd set for hosting the WoW server on the local LAN, or internet, depending on your setup and helps to keep those settings synced

### Updating

To update your current app to the latest, you just download the latest release and extract. Overwrite your existing app. The release doesn't contain a settings file, so it won't overwrite your existing settings or your backups.

### Changes -

**0.0.2.3** - Fixed bug in setting passwords, adding account creation logic

**0.0.2.2** - Added ability to set BattleNet password, which is also required if changing BattleNet login because of how the password hash is created in TrinityCore

**0.0.2.1** - **PLEASE NOTE** The .exe file name has changed and the old one should be deleted. Revamped GUI, added some account/character management

-- Working - listing/changing current account/character information

-- Not working - creating new account, cleanup/deletion of accounts/characters/orphaned objects

**0.0.2.0** - Prep work for adding additional modules for management, renaming project

**0.0.1.8** - Massive internal changes to virtualize datagrid -> loading/filtering should be MUCH faster, code cleanup

**0.0.1.7** - building/exporting the .conf files will be quicker, clean up the alert box for "Check Config", added check for WorldChat.Enable and also Character.Template when using client build 26972

**0.0.1.6** - Added few more config checks, fixed an sql issue and some code cleanup

**0.0.1.5** - Changed some parsing to better check for issues, including values with comments on the same line as a configuration entry (and give a warning)

**0.0.1.4** - Extra checks, moved description to tooltips for each row (shorter launch/import time), general cleanup, added button to reload config so that you don't have to close and open the app again to reload if any mistakes were made.

**0.0.1.3** - search box added, enabled highlight of matching rows, and semi-working auto-scroll to first matches

**0.0.1.2** - introduced backups of existing configs to a backup folder (within app folder), pulls in existing bnet/worldserver.conf to check

**0.0.1.1** - introduced pulling/parsing template configs from worldserver.conf and bnetserver.conf default files, added/fixed some checks

**0.0.1.0** - base release, more proof of concept than anything

### About
This tool helps build working World and Bnet server config files without duplicate entries, check your configuration for any potential issues, manage accounts/characters as well as potentially clean up the database of orphaned characters/objects. There are some things to be aware of -


The MySQL server will probably error connecting unless you're running this on the same server. You should **keep the MySQL server set to 127.0.0.1** and user/password should be left to defaults in the config file. Delete the settings.json file to reset them.


This tool can also update the **config.wtf** file in your WOW client configuration to make sure it matches with the rest of the configuration, assuming that this tool can access the folder/file. If you run your WOW Client from another PC, then you may need to set this manually to match the **[LoginREST.ExternalAddress]** from the **bnetserver.conf**, otherwise you may have trouble with your WOW client contacting the server. These addresses can be changed by using the **[Set IP]** button, and it can update the entries automatically. If you have your wow client on a machine other than the server, you can copy a 'fake' wow config.wtf to the server and set the location in the tool to bypass any errors for it. Just make sure you update your wow config manually if the portal address needs changed.


If there is a problem, you can use the **[Set Defaults]** button to pull the wow config fresh from the local template files. **This will overwrite all previous settings** for the Bnet and World config files. 


If you see an error similar to **[not allowed to connect to this MySQL server]** then you're probably running this on a different computer or the database isn't running. Run it from the SPP server (while the database server is running).


To begin - Make sure the SPP Database is running

![](https://github.com/skeezerbean/SPP-LegionV2-Management/blob/main/Resources/database-started.PNG)


### Config Generator


Click on the **Settings** button

![](https://github.com/skeezerbean/SPP-LegionV2-Management/blob/main/Resources/Settings-Tab.PNG)


When browsing for the SPP folder, it will be the one that has these items where you extracted your SPP LegionV2 Server files. Note that changing a folder automatically saves the new locations, as well as the window position/size

![](https://github.com/skeezerbean/SPP-LegionV2-Management/blob/main/Resources/SPP-server-folder.PNG)


Browsing for the Wow Config Location will be your WoW Legion **Client** folder, which will look more or less like this

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

**1** - Pulls the list of accounts from the database.

**2** - You can select an account and see/change the settings on the right side

**3** - Change settings here for an account. If the BattleNet ID or Login is -1, then chances are that BattleNet account doesn't exist. Leave it alone. The account may have not been fully created. Otherwise you can change the number of Battle Coins for an account, change the BattleNet Email or Account Username. **Note - if you change the BattleNet Email for login, you will need to enter a password as well as they are tied together in how the password hash is created (TrinityCore). This can be the original password for this account, or you can set a new one.**

**4** - Click this when done editing characters and it will push changes to the database. 

![](https://github.com/skeezerbean/SPP-LegionV2-Management/blob/main/Resources/Account-Manager-Accounts-Screen.PNG)


If you changed any account information and applied, it will push those updates to the database and refresh the account list. If you tried submitting a value in fields that was not acceptable (alphanumeric in number fields, etc) then those are ignored. You'll also get feedback on changes

![](https://github.com/skeezerbean/SPP-LegionV2-Management/blob/main/Resources/Account-Manager-Accounts-Update.PNG)


You can also list and change some character information on the **Characters** or **Orphaned Characters** tab. 

**1** - Gathers the list of characters in the database

**2** - Select a character, and it will populate settings on the right

**3** - Change settings here. You can rename a character (length is limited in the database up to 12 characters), or change the account it's attached to. Make sure you don't change this to a non-existant account or the character will be orphaned. If that's the case, you'll be able to make these changes to change it back on that tab

**4** - Click this when done, and it will push changes to the database

![](https://github.com/skeezerbean/SPP-LegionV2-Management/blob/main/Resources/Account-Manager-Characters.PNG)


If you changed any information then the app will notify you

![](https://github.com/skeezerbean/SPP-LegionV2-Management/blob/main/Resources/Account-Manager-Characters-Update.PNG)


