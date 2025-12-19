# Changelog

## 1.2.0 (2025-12-18)

### Major Features
* **Gun Data Display**: Added comprehensive weapon stats preview window showing damage, fire rate, ammo capacity, recoil, spread, bullet data, and more when viewing gear details
* **Grid Clear Function**: Added "Clear Grid" button to instantly remove all upgrades from the gear grid with a single click
* **Loadout Previewing**: Added preview functionality that displays loadout contents when hovering over loadout buttons, supporting both text and visual preview modes

### Technical Updates
* **Clipboard API Improvement**: Replaced Windows API clipboard functions with Unity's built-in `GUIUtility.systemCopyBuffer` to remove kernel32.dll dependency and improve cross-platform compatibility

## 1.1.1 (2025-11-29)

### Loadout System Enhancements
* **Bidirectional Loadout Scrolling**: Independent left and right scrolling keys ("," and "." by default) replacing single toggle key
* **Configurable Rename Keybind**: Customizable loadout rename key (L by default) with hot reload support
* **Clean Tooltips**: Removed hardcoded key hint text from loadout tooltips for cleaner UI
* **Precise Rename Activation**: Loadout renaming now only activates when hovering over specific loadout slots (no fallback)
* **Enhanced Scrolling UX**: Loadout pages now reset to first page when opening any gear menu, with proper icon refresh

### Technical Updates
* **Loadout Persistence Fixes**: Improved name persistence logic to prevent interference between default and custom names
* **UI Refresh System**: Added automatic icon refresh when page offset resets for consistent visual state

## 1.1.0 (2025-11-29)

### Major Features
* **Grid Sharing System**: Copy complete gear build layouts to shareable codes and paste builds from others
* **Advanced Filter Panel**: Comprehensive filtering by rarity, favorites, and stat requirements with real-time updates
* **Priority Sort System**: Fully customizable sorting criteria with drag-and-drop reordering and persistent settings
* **Stat Display Formatting**: Reformats upgrade stats from "50 Damage" → "Damage: **50**"
* Re-enabled comparison and sorting systems (disabled in 1.0.0 due to incomplete implementation)

### Enhancements
* **Instant Scrapping**: Optional instant scrapping without confirmation timers
* **SharingButtons.cs**: New UI system for grid copy/paste operations
* **CompareHoverManager.cs**: Improved comparison mode with better hover handling
* **FilterHandling.cs**: Complete rewrite with advanced stat filtering capabilities
* **Sorting/Filtering Integration**: Seamless combination of filter panel and priority sorting

### Technical Updates
* Added clipboard integration (Windows API) for build sharing
* Binary encoding system for compact build codes
* Enhanced Harmony patches for UI modifications
* Improved error handling and compatibility checks
* Persistent configuration for sort priorities and filters

## 1.0.0 (2025-11-26)

### Features
* Upgrade comparison mode: Side-by-side comparison of two upgrades with tooltips
* Mass scrapping: Scrap marked or non-favorite upgrades with confirmation dialogs
* Filtering and sorting: UI filter panel for sorting and filtering upgrades
* Loadout expansion: Pagination for viewing expanded loadouts
* Configurable keybindings: Customizable keys for trash marking, page toggle, and comparison

### Technical
* Initial mod template setup with BepInEx framework
* Add MinVer for version management
* Add thunderstore.toml configuration for mod publishing
* Add LICENSE.md and CHANGELOG.md template files
* Basic plugin structure with HarmonyLib integration
* Placeholder for mod-specific functionality
