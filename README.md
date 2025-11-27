# Enhanced Upgrade Menu

A BepInEx mod for MycoPunk that enhances the upgrade management experience with additional functionality and improved usability.

## Description

Enhanced Upgrade Menu adds powerful features to help you manage your gear upgrades more efficiently:

- **Upgrade Comparison Mode**: Compare two upgrades side-by-side by holding an upgrade and pressing the compare key (default C). Displays tooltips simultaneously for easy comparison.
- **Scrapping Functionality**: Scrap all marked upgrades or all non-favorite upgrades with confirmation dialogs. Mark upgrades for scrapping by pressing the trash mark key (default T).
- **Filtering and Sorting**: Access a filter panel to sort and filter upgrades in the details window.
- **Loadout Expansion**: Expand your loadout view with pagination (default '.' key) to see more gear options at once.

## Getting Started

### Dependencies

* MycoPunk (base game)
* [BepInEx](https://github.com/BepInEx/BepInEx) - Version 5.4.2403 or compatible
* .NET Framework 4.8
* [HarmonyLib](https://github.com/pardeike/Harmony) (included via NuGet)

### Building/Compiling

1. Clone this repository and customize the following:
   - Rename namespace and class names appropriately
   - Modify PluginGUID to be unique (format: "author.modname")
   - Update PluginName and PluginVersion
   - Add your specific Harmony patches and functionality

2. Add any additional NuGet packages or references needed for your mod

3. Open the solution file in Visual Studio, Rider, or your preferred C# IDE

4. Build the project in Release mode to generate the .dll file

Alternatively, use dotnet CLI:
```bash
dotnet build --configuration Release
```

### Installing

**For distribution as a completed mod:**

**Option 1: Via Thunderstore (Recommended)**
1. Update `thunderstore.toml` with your mod's specific information
2. Publish using Thunderstore CLI or mod manager
3. Users download and install via Thunderstore Mod Manager

**Option 2: Manual Distribution**
1. Package the built .dll, any config files, and README
2. Users place the .dll in their `<MycoPunk Directory>/BepInEx/plugins/` folder

**Note:** This template is not meant to be installed directly - customize it first for your specific mod functionality.

### Executing program

Once customized and built, the mod will automatically load through BepInEx when the game starts. Check the BepInEx console for loading confirmation messages.

### Mod Development Structure

- **Plugin.cs:** Main plugin class with Awake method and Harmony initialization
- **thunderstore.toml:** Publishing configuration for Thunderstore
- **CSPROJECT.csproj:** Build configuration with proper references
- **Resources:** Icon and documentation placeholders

## Help

* **First time modding?** Check BepInEx documentation and MycoPunk modding resources
* **Harmony patches failing?** Ensure method signatures match the game's IL
* **Dependency issues?** Update NuGet packages and verify .NET runtime version
* **Thunderstore publishing?** Update all metadata in thunderstore.toml before publishing
* **Plugin not loading?** Check BepInEx logs for errors and verify GUID uniqueness

## Authors

* Sparroh (MycoPunk mod collection maintainer)
* Coloron (wrote the LoadoutScrolling extension for this mod)
* funlennysub (original BepInEx template)
* [@DomPizzie](https://twitter.com/dompizzie) (README template)

## License

* This project is licensed under the MIT License - see the LICENSE.md file for details
