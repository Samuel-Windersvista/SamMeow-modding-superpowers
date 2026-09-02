# Skipper

A BepInEx plugin for SPT-AKI that allows you to skip quests in-game.

### How to install

1. Download the latest release here: [link](https://dev.sp-tarkov.com/Terkoiz/Skipper/releases) -OR- build from source (instructions below)
2. Simply extract the zip file contents into your root SPT-AKI folder (where EscapeFromTarkov.exe is)
3. Your `BepInEx/plugins` folder should now contain a `Terkoiz.Skipper.dll` file inside

### How to build from source

1. Download/clone this repository
2. Open your current SPT directory and copy all files from `\EscapeFromTarkov_Data\Managed` into this solution's `\References\EFT_Managed` folder.
3. Rebuild the project in the Release configuration.
4. Grab the `Terkoiz.Skipper.dll` file from the `bin/Release` folder and use it wherever. Refer to the "How to install" section if you need help here.