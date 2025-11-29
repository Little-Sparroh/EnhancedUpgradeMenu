# Enhanced Upgrade Menu

A comprehensive BepInEx mod for MycoPunk that massively enhances the upgrade management experience with powerful new functionality and improved usability.

## Description

Enhanced Upgrade Menu transforms the gear upgrade interface with a suite of advanced tools designed to help you manage your gear more efficiently and share build configurations with the community.

## Features

### 🏗️ Build Management & Sharing

**Grid Sharing System**
- Copy complete gear build layouts (positions, rotations, upgrade IDs) to compact shareable codes
- Paste build codes from other players to instantly recreate their gear configurations
- Perfect for sharing optimized builds within the MycoPunk community
- Auto-adjusts positioning if grid space conflicts occur

**Upgrade Comparison**
- Compare two upgrades side-by-side with simultaneous tooltips
- Hold any upgrade and press **C** to enter comparison mode
- Displays stats and details for easy decision making

**Smart Scrapping**
- Mark upgrades for scrapping with **T** key
- Mass scrap all marked upgrades or all non-favorite upgrades
- Confirmation dialogs prevent accidental deletion
- Instant scrapping option (configurable) skips confirmation timers

### 🔍 Advanced Filtering & Sorting

**Comprehensive Filter Panel**
- Toggle filtering by upgrade rarity (Standard, Rare, Epic, Exotic, Oddity)
- Show/hide favorites or show only favorited upgrades
- Filter by stat requirements (upgrades must have ALL selected properties)
- Clears all filters with one click
- Real-time updates as you adjust filters

**Priority Sort System**
- Customizable sorting criteria with drag-and-drop reordering
- Sort by: Favorited status, Lock status, Rarity, Turbocharged state, Recently used/acquired, Instance name
- Save and load your preferred sort priorities
- Persistent settings across game sessions

### 🎨 Quality of Life Improvements

**UI Enhancements**
- Configurable hotkeys for all major functions
- Loadout expansion/pagination (press **.** to cycle pages)
- Stat display reformatting: "50 Damage" → "Damage: **50**"
- Clean, intuitive interface with color-coded buttons

**Configuration Options**
- Hotkey customization (trash marking, comparison, page toggle)
- Instant scrapping toggle
- Fixed scrap timer duration
- Stat formatting on/off
- All settings persist between game sessions

## Getting Started

### Requirements
- **MycoPunk** (base game)
- **[BepInEx 5.4.2403](https://github.com/BepInEx/BepInEx)** or compatible version
- **.NET Framework 4.8**
- **[HarmonyLib](https://github.com/pardeike/Harmony)** (included via NuGet)

### Installation
1. Download from Thunderstore Mod Manager or install BepInEx manually
2. Place `EnhancedUpgradeMenu.dll` in `<MycoPunk Directory>/BepInEx/plugins/`
3. Launch MycoPunk - the mod loads automatically through BepInEx

### First Time Setup
- Access the gear details window for any piece of equipment
- New buttons appear: **Priority Sort**, **Filter**, **Scrap Marked**, **Scrap Non Favorite**
- **Copy Grid** / **Paste Code** buttons appear for sharing builds
- Configure hotkeys in the BepInEx config file if desired

## Usage Guide

### Grid Sharing
1. Arrange your upgrades on the gear grid as desired
2. Click **Copy Grid** to copy a shareable code to clipboard
3. Share the code with others or save it for later
4. Click **Paste Code** to instantly recreate any shared build

### Filtering Upgrades
1. Click the **Filter** button to open the advanced filter panel
2. Toggle rarities on/off, set favorite filters, select required stats
3. Only upgrades matching ALL criteria remain visible
4. Use **Clear All Filters** to reset

### Custom Sorting
1. Click **Priority Sort** to open the customization window
2. Drag criteria up/down to change sort priority order
3. Click **Save** to apply your preferences
4. The **Reset** button restores defaults

### Hotkeys
- **T**: Toggle trash mark on hovered upgrade
- **C**: Toggle upgrade comparison mode
- **.**: Cycle loadout pages

## Development

### Building from Source
1. Clone this repository
2. Ensure .NET Framework 4.8 SDK is installed
3. Run `dotnet build --configuration Release`
4. Find the compiled DLL in `bin/Release/net48/`

### Project Structure
- `Plugin.cs` - Main plugin entry point and configuration
- `CompareHandling.cs` - Side-by-side upgrade comparison
- `ScrapHandling.cs` - Mass scrapping functionality
- `FilterHandling.cs` - Advanced filtering panel
- `SortHandling.cs` - Priority-based sorting system
- `SharingButtons.cs` - Grid copy/paste functionality
- Various patches for UI integration and harmony modifications

### Publishing
- Update version in `Plugin.cs` and `thunderstore.toml`
- Build in Release mode
- Use Thunderstore CLI for publishing

## Contributing

We welcome contributions! This mod is developed for the MycoPunk community by players who want to enhance their upgrade management experience.

## Authors
- **Sparroh** - Main developer and maintainer
- **Coloron** - Original LoadoutScrolling extension integration
- **funlennysub** - Initial BepInEx template

## License
This project is licensed under the MIT License - see the LICENSE.md file for details

---

*Transform your gear upgrading workflow with Enhanced Upgrade Menu - where efficiency meets elegance.*
