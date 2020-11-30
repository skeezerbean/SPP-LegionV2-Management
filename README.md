# SPPConfigGenerator
## Build/Update config generator for SPP LegionV2

### Usage

Make sure the **SPP Database** engine is running.

Set the general settings with the location of your **SPP directory** (base directory OR the server folder with config, either works)

Set the general settings with the location of your **WOW Legion client** (base directory OR the WTF folder containing the config)

Load up the tool, run a check on the config, and save/export as needed to build/rebuild your SPP LegionV2 config files.

You can use the **[Set Build]** button to set the build # for the client you're using to match in both config files, and database realm entry.

You can use the **[Set IP]** button to set the hosting IP in the bnet config and database realm entry, as well as the WOW client config.wtf file to keep them the same.

### Changes -

**0.0.1.3** - search box added, enabled highlight of matching rows, and semi-working auto-scroll to first matches

**0.0.1.2** - introduced backups of existing configs to a backup folder (within app folder), pulls in existing bnet/worldserver.conf to check

**0.0.1.1** - introduced pulling/parsing template configs from worldserver.conf and bnetserver.conf default files, added/fixed some checks

**0.0.1.0** - base release, more proof of concept than anything

### About
This tool helps build working World and Bnet server config files without duplicate entries. This can also be used to check your configuration for any known issues. There are some things to be aware of -


The MySQL server will probably error connecting unless you're running this on the same server. You should **keep the MySQL server set to 127.0.0.1** and user/password should be left to defaults. Delete the settings.json file to reset them.


This tool can also update the **config.wtf** file in your WOW client configuration to make sure it matches with the rest of the configuration, assuming that this tool can access the folder/file. If you run your WOW Client from another PC, then you may need to set this manually to match the **[LoginREST.ExternalAddress]** from the **bnetserver.conf**, otherwise you may have trouble with your WOW client contacting the server.


This will also check for some common issues, such as having both **battle.coin.vendor** entries active (creates double speech in the vendor interaction). It also checks for having **Solocraft** and **Flexcraft** both enabled (can be a problem), and makes sure both entries for the bshop are both enabled/disabled together. It will also search for any duplicate entries in the config, which would cause an instant crash in the server startup if they were there.


This tool can also set and check the **[Game.Build.Version]** between both configs and the database realm entry, and warn of any issues. Use the **[Set Build]** button to set this entry if there is a discrepancy between them and your WOW client version. You can find the WOW client version by launching it and checking at the bottom-left of the client at the login screen.


Use the **[Set IP]** button to setup the external/lan/wan IP address in the Database entry for the realm, the Bnet config, and the WOW client (as much as it can access from the computer the tool is running from). This will update the Database Realm entry and WOW config immediately. The rest won't update until Save/Export. Use the **[Set Build]** button to set the **[Game.Build.Version]** in both configs, and the realm database entry.


If there is a problem, you can use the **[Set Defaults]** button to pull the wow config fresh from the local template files. **This will overwrite all previous settings** for the Bnet and World config files. You'd need to set the **[Game.Build.Version]** again, and possibly the IP if hosting outside of the local server.


Use the **[Check Config]** button to run through some quick problem checks for common issues. Note - this may give errors connecting to MySQL if this is running from another PC than the one the SPP Database Server runs on, and also make sure that the Database Server itself is running first. 


Otherwise this tool cannot connect to the Database to check/update any settings there. If the error says similar to **[not allowed to connect to this MySQL server]** then you're probably running this on a different computer or the database isn't running. Run it from the SPP server (while the database server is running).


Once you've finished making any changes, hit the **[Save/Export]** button to export the current settings to the bnetserver.conf and worldserver.conf files. Make sure to set the folders for your SPP LegionV2 folder, and your WOW Client folder in the **[General App Settings]** tab.
