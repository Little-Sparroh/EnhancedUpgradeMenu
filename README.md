# Enhanced Upgrade Menu

A BepInEx mod for MycoPunk that enhances the upgrade management experience with advanced features for sorting, filtering, comparing, and optimizing gear builds.

## Features

### Core Functionality
- **Hex Grid Solver**: Automatic upgrade placement system that finds optimal arrangements for selected upgrades on the hex grid. Select upgrades by hovering and pressing 'N', then use the "Solve" button to automatically place them with support for rotation and boundary expansion.
- **Gun Data Display**: Comprehensive weapon stats preview window showing damage, fire rate, ammo capacity, recoil, spread, bullet data, and more when viewing gear details.
- **Grid Sharing System**: Copy complete gear build layouts to shareable codes and paste builds from others using binary encoding for compact build codes.
- **Advanced Filter Panel**: Comprehensive filtering by rarity, favorites, and stat requirements with real-time updates.
- **Priority Sort System**: Fully customizable sorting criteria with drag-and-drop reordering and persistent settings.
- **Loadout Management**: Expanded loadouts with pagination, preview functionality (text and visual modes), and configurable renaming.

### UI Enhancements
- **Stat Display Formatting**: Reformats upgrade stats from "50 Damage" to "Damage: **50**" for improved readability (does not affect directive window hover information).
- **Comparison Mode**: Side-by-side comparison of two upgrades with detailed tooltips.
- **Mass Scrapping**: Scrap marked or non-favorite upgrades with confirmation dialogs, including instant scrapping option.
- **Grid Clear Function**: Instantly remove all upgrades from the gear grid with a single click, unequipping Boundary Incursion upgrades last for better compatibility.
- **Loadout Scrolling**: Bidirectional scrolling with independent left and right keys for navigating loadout pages.

### Configuration
- **Configurable Keybindings**: Customizable keys for trash marking, loadout scrolling, renaming, and comparison with hot reload support.
- **Persistent Settings**: Settings for sort priorities, filters, and UI options are saved and restored between sessions.

## Getting Started

### Dependencies

* MycoPunk (base game)
* [BepInEx](https://github.com/BepInEx/BepInEx) - Version 5.4.2403 or compatible
* .NET Framework 4.8
* [HarmonyLib](https://github.com/pardeike/Harmony) (included via NuGet)

### Building/Compiling

1. Clone this repository
2. Open the solution file in Visual Studio, Rider, or your preferred C# IDE
3. Build the project in Release mode to generate the .dll file

Alternatively, use dotnet CLI:
```bash
dotnet build --configuration Release
```

### Installing

**Via Thunderstore (Recommended)**:
1. Download and install via Thunderstore Mod Manager
2. The mod will be automatically installed to the correct directory

**Manual Installation**:
1. Place the built `EnhancedUpgradeMenu.dll` in your `<MycoPunk Directory>/BepInEx/plugins/` folder

### Executing program

The mod loads automatically through BepInEx when the game starts. Check the BepInEx console for loading confirmation messages.

## Configuration

Access mod settings through the BepInEx configuration file at `<MycoPunk Directory>/BepInEx/config/sparroh.enhancedupgrademenu.cfg`. Key options include:

- Keybinds for various actions (trash marking, scrolling, renaming, comparison)
- Toggle for instant scrapping
- Enable/disable stat reformatting and gun data display
- Loadout preview settings

## Help

* **Mod not loading?** Verify BepInEx is installed correctly and check console logs for errors
* **Keybinds not working?** Ensure no conflicts with other mods or game settings
* **Grid solver issues?** Make sure upgrades are selected before solving (hover and press 'N')
* **Sharing not working?** Check clipboard permissions and ensure the build code is valid
* **UI elements missing?** Confirm mod version compatibility and verify no other mods are interfering

## Authors

- Sparroh
- Coloron (Loadout Expander)
- funlennysub (BepInEx template // Hex Grid Solver)
- Generally Break (Efficient encoding)
- [@DomPizzie](https://twitter.com/dompizzie) (README template)

## License

This project is licensed under the MIT License - see the LICENSE file for details
