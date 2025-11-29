# Changelog

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
