using System;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[MycoMod(null, ModFlags.IsClientSide)]
public class SparrohPlugin : BaseUnityPlugin
{
    public const string PluginGUID = "sparroh.enhancedupgrademenu";
    public const string PluginName = "EnhancedUpgradeMenu";
    public const string PluginVersion = "1.2.0";

    private ConfigEntry<Key> TrashMarkKey;
    private ConfigEntry<Key> ScrollLeftKey;
    private ConfigEntry<Key> ScrollRightKey;
    private ConfigEntry<Key> LoadoutRenameKey;
    private ConfigEntry<Key> CompareKey;
    private ConfigEntry<bool> EnableInstantScrapping;
    private ConfigEntry<bool> EnableFixedTimer;
    private ConfigEntry<float> FixedTimerDuration;
    private ConfigEntry<bool> EnableStatReformat;
    private ConfigEntry<bool> EnableDisplayGunData;
    private DisplayGunDataMod displayGunDataMod;
    internal static ManualLogSource Logger;
    public static SparrohPlugin Instance;
    public Coroutine SolverCoroutine;
    private UpgradeSolver upgradeSolver;

    private void Awake()
    {
        try
        {
            Logger = base.Logger;
            Instance = this;

            var harmony = new Harmony(PluginGUID);

            try
            {
                LoadoutExpanderMod._loadoutButtonsField = AccessTools.Field(typeof(GearDetailsWindow), "loadoutButtons");
                LoadoutExpanderMod._updateIconMethod = AccessTools.Method(typeof(GearDetailsWindow), "UpdateLoadoutIcon");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to setup LoadoutExpander reflection: {ex.Message}");
            }

            try
            {
                TrashMarkKey = Config.Bind("Keybinds", "Mark for Trash", Key.T, "Key to toggle trash mark on upgrades");
                ScrapHandlingMod.currentTrashKey = TrashMarkKey.Value;
                TrashMarkKey.SettingChanged += (sender, args) => { ScrapHandlingMod.currentTrashKey = TrashMarkKey.Value; };
                ScrapHandlingMod.ScrapMarkedAction = () => ScrapHandlingMod.TryScrapMarkedUpgrades(this);
                ScrapHandlingMod.ScrapNonFavoriteAction = () => ScrapHandlingMod.TryScrapNonFavoriteUpgrades(this);

                ScrollLeftKey = Config.Bind("Keybinds", "Scroll Loadout Left", Key.Comma, "Key to scroll to the left loadout page");
                LoadoutExpanderMod.ScrollLeftKey = ScrollLeftKey.Value;
                ScrollLeftKey.SettingChanged += (sender, args) => { LoadoutExpanderMod.ScrollLeftKey = ScrollLeftKey.Value; };

                ScrollRightKey = Config.Bind("Keybinds", "Scroll Loadout Right", Key.Period, "Key to scroll to the right loadout page");
                LoadoutExpanderMod.ScrollRightKey = ScrollRightKey.Value;
                ScrollRightKey.SettingChanged += (sender, args) => { LoadoutExpanderMod.ScrollRightKey = ScrollRightKey.Value; };

                LoadoutRenameKey = Config.Bind("Keybinds", "Rename Loadout", Key.L, "Key to rename the hovered loadout");
                LoadoutHoverInfoPatches.RenameKey = LoadoutRenameKey.Value;
                LoadoutRenameKey.SettingChanged += (sender, args) => { LoadoutHoverInfoPatches.RenameKey = LoadoutRenameKey.Value; };

                CompareKey = Config.Bind("Keybinds", "Toggle Compare", Key.C, "Key to compare upgrades");
                CompareHandling.CompareKey = CompareKey;
                CompareKey.SettingChanged += (sender, args) => { CompareHandling.CompareKey = CompareKey; };

                EnableInstantScrapping = Config.Bind("General", "Instant Scrap", false, "Enable instant scrapping without timer");
                EnableFixedTimer = Config.Bind("General", "Fixed Scrap Time", false, "Use fixed scrap timer instead of default");
                FixedTimerDuration = Config.Bind("General", "Scrap Duration", 1f, "Duration in seconds for fixed scrap timer");

                EnableStatReformat = Config.Bind("General", "Reformat Statistics", true, "Force Key: Value stat format");
                FormatHandling.enableStatReformat = EnableStatReformat.Value;
                EnableStatReformat.SettingChanged += (sender, args) => { FormatHandling.enableStatReformat = EnableStatReformat.Value; };

                EnableDisplayGunData = Config.Bind("General", "Display Gun Stats", false, "Show gun stats window when editing weapons in Gear Details");
                EnableDisplayGunData.SettingChanged += (sender, args) => { if (displayGunDataMod != null) displayGunDataMod.SetEnable(EnableDisplayGunData.Value); };

                LoadoutPreviewMod.enableTextMode = Config.Bind("General", "Loadout Preview", false, "If true, show upgrade list on hover");
                LoadoutPreviewMod.enableTextMode.SettingChanged += (sender, args) => LoadoutPreviewMod.OnConfigChanged();
                LoadoutPreviewMod.UpdatePreviewMode();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to setup configuration bindings: {ex.Message}");
            }

            try
            {
                upgradeSolver = new UpgradeSolver();
                UpgradeSolver.Instance = upgradeSolver;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to initialize UpgradeSolver: {ex.Message}");
                upgradeSolver = null;
            }

            try
            {
                CompareHandling.Initialize();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to initialize CompareHandling: {ex.Message}");
            }

            try
            {
                InstantScrapping.Initialize(EnableFixedTimer, FixedTimerDuration, EnableInstantScrapping);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to initialize InstantScrapping: {ex.Message}");
            }

            try
            {
                FormatHandling.Initialize();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to initialize FormatHandling: {ex.Message}");
            }

            try
            {
                GridClearMod.Initialize();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to initialize GridClearMod: {ex.Message}");
            }

            try
            {
                displayGunDataMod = new DisplayGunDataMod(EnableDisplayGunData.Value);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to initialize DisplayGunDataMod: {ex.Message}");
                displayGunDataMod = null;
            }

            try
            {
                CompareHandling.ApplyPatches(harmony);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to apply CompareHandling patches: {ex.Message}");
            }

            try
            {
                LoadoutPreviewMod.ApplyPatches(harmony);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to apply LoadoutPreviewMod patches: {ex.Message}");
            }

            try
            {
                priorityCurrentOrder = PriorityPatches.LoadPriorityOrder();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to load priority order: {ex.Message}");
                priorityCurrentOrder = new List<PriorityCriteria>();
            }

            try
            {
                harmony.PatchAll();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to apply Harmony patches: {ex.Message}");
            }

            try
            {
                PriorityPatches.Patch(harmony);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to apply PriorityPatches: {ex.Message}");
            }

            try
            {
                var updateMethod = AccessTools.Method(typeof(GearDetailsWindow), "Update");
                if (updateMethod != null)
                {
                    harmony.Patch(updateMethod, prefix: new HarmonyMethod(typeof(Patches), "UpdatePrefix"));
                }

                var updateFavIconMethod = AccessTools.Method(typeof(GearUpgradeUI), "UpdateFavoriteIcon");
                if (updateFavIconMethod != null)
                {
                    harmony.Patch(updateFavIconMethod, postfix: new HarmonyMethod(typeof(Patches), "UpdateFavoriteIconPostfix"));
                }

                var onAdditionalActionMethod = AccessTools.Method(typeof(GearUpgradeUI), "OnAdditionalAction", new[] { typeof(int), typeof(bool).MakeByRefType() });
                if (onAdditionalActionMethod != null)
                {
                    harmony.Patch(onAdditionalActionMethod, postfix: new HarmonyMethod(typeof(Patches), "OnAdditionalActionPostfix"));
                }

                var enableGridViewMethod = AccessTools.Method(typeof(GearUpgradeUI), "EnableGridView", new[] { typeof(bool) });
                if (enableGridViewMethod != null)
                {
                    harmony.Patch(enableGridViewMethod, postfix: new HarmonyMethod(typeof(Patches), "EnableGridViewPostfix"));
                }

                var onOpenMethod = AccessTools.Method(typeof(GearDetailsWindow), "OnOpen");
                if (onOpenMethod != null)
                {
                    harmony.Patch(onOpenMethod, postfix: new HarmonyMethod(typeof(UpgradeSolverPatches.GearDetailsWindowPatch), "Postfix"));
                }

                var onCloseCallbackMethod = AccessTools.Method(typeof(GearDetailsWindow), "OnCloseCallback");
                if (onCloseCallbackMethod != null)
                {
                    harmony.Patch(onCloseCallbackMethod, postfix: new HarmonyMethod(typeof(UpgradeSolverPatches.GearDetailsWindowClosePatch), "Postfix"));
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to apply additional patches: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Critical error during mod initialization: {ex.Message}\n{ex.StackTrace}");
        }
        Logger.LogInfo($"{PluginName} loaded successfully.");
    }

    private void OnDestroy()
    {
        try
        {
            Logger.LogInfo("Shutting down EnhancedUpgradeMenu mod...");

            try
            {
                InstantScrapping.Destroy();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to destroy InstantScrapping: {ex.Message}");
            }

            try
            {
                LoadoutPreviewMod.Destroy();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to destroy LoadoutPreviewMod: {ex.Message}");
            }

            try
            {
                if (upgradeSolver?.SolverUI != null)
                {
                    upgradeSolver.SolverUI.Close();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to close UpgradeSolver UI: {ex.Message}");
            }

            Logger.LogInfo("EnhancedUpgradeMenu mod shutdown completed.");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Critical error during mod shutdown: {ex.Message}");
        }
    }

    private void Update()
    {
        try
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current[LoadoutExpanderMod.ScrollRightKey].wasPressedThisFrame)
                {
                    LoadoutExpanderMod.ScrollRight();
                }
                else if (Keyboard.current[LoadoutExpanderMod.ScrollLeftKey].wasPressedThisFrame)
                {
                    LoadoutExpanderMod.ScrollLeft();
                }
            }

            if (displayGunDataMod != null)
            {
                try
                {
                    displayGunDataMod.SetEnable(EnableDisplayGunData.Value);
                    displayGunDataMod.Update();
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error updating DisplayGunDataMod: {ex.Message}");
                }
            }

            if (upgradeSolver != null)
            {
                try
                {
                    upgradeSolver.Update();
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error updating UpgradeSolver: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Critical error in Update(): {ex.Message}");
        }
    }

    private void OnGUI()
    {

        if (showPopup)
        {
            Rect popupRect = new Rect(Screen.width / 2 - 150, Screen.height / 2 - 50, 300, 100);
            GUI.Window(0, popupRect, DrawPopup, "Confirm Action");
        }
        else if (Menu.Instance != null && Menu.Instance.IsOpen)
        {
            var topWindow = Menu.Instance.WindowSystem.GetTop() as GearDetailsWindow;
            if (topWindow != null)
            {
                DrawButtons();
            }
        }

        if (showPriorityWindow)
        {
            priorityWindowRect = GUI.Window(0, priorityWindowRect, DrawPriorityWindow, "Sort Priority");
        }

        GridClearMod.DrawClearButton();
        displayGunDataMod.OnGUI();
        upgradeSolver.OnGUI();
    }

    private IEnumerator DelayPatch()
    {
        yield return null;
        upgradeSolver.SolverUI.PatchUpgradeClick();
    }

    private static void DrawButtons()
    {
        float buttonY = Screen.height - 60;
        float buttonX = Screen.width * 0.75f;
        float buttonWidth = 150f;
        float buttonHeight = 50f;
        float spacing = 10f;

        float priorityX = buttonX - 160;
        if (GUI.Button(new Rect(priorityX, buttonY, 150, 50), "Priority Sort"))
        {
            showPriorityWindow = !showPriorityWindow;
            if (showPriorityWindow)
            {
                priorityWindowRect = new Rect((Screen.width - 300) / 2, (Screen.height - 600) / 2, 300, 600);
            }
            else
            {
                priorityCurrentOrder = PriorityPatches.LoadPriorityOrder();
            }
        }

        Rect filterRect = new Rect(buttonX, buttonY, buttonWidth, buttonHeight);
        if (GUI.Button(filterRect, "Filter"))
        {
            UpgradeSortingPlugin.FilterPanel.Toggle();
        }

        buttonX += buttonWidth + spacing;

        Rect scrapMarkedRect = new Rect(buttonX, buttonY, buttonWidth, buttonHeight);
        if (GUI.Button(scrapMarkedRect, "Scrap Marked"))
        {
            showPopup = true;
            popupAction = "ScrapMarked";
            popupMessage = "Are you sure you want to scrap all marked upgrades?";
        }

        buttonX += buttonWidth + spacing;

        buttonWidth = 200f;
        Rect scrapNonFavRect = new Rect(buttonX, buttonY, buttonWidth, buttonHeight);
        if (GUI.Button(scrapNonFavRect, "Scrap Non Favorite"))
        {
            showPopup = true;
            popupAction = "ScrapNonFavorite";
            popupMessage = "Are you sure you want to scrap all non-favorite upgrades?";
        }
    }

    private static void DrawPriorityWindow(int id)
    {
        GUI.DragWindow(new Rect(0, 0, 300, 20));

        Event e = Event.current;

        for (int i = 0; i < priorityCurrentOrder.Count; i++)
        {
            if (i == priorityDraggedIndex) continue;

            Rect itemRect = new Rect(10, 30 + i * 25, 270, 20);
            GUI.Label(itemRect, (i + 1) + ". " + GetPriorityCriteriaName(priorityCurrentOrder[i]));

            if (e.type == EventType.MouseDown && itemRect.Contains(e.mousePosition))
            {
                priorityDraggedIndex = i;
                priorityDragOffsetY = e.mousePosition.y - itemRect.y;
                e.Use();
            }
        }

        if (priorityDraggedIndex != -1)
        {
            if (e.type == EventType.MouseDrag)
            {
                float newY = e.mousePosition.y - priorityDragOffsetY;
                int newIndex = Mathf.Clamp(Mathf.RoundToInt((newY - 30) / 25), 0, priorityCurrentOrder.Count - 1);
                if (newIndex != priorityDraggedIndex)
                {
                    var item = priorityCurrentOrder[priorityDraggedIndex];
                    priorityCurrentOrder.RemoveAt(priorityDraggedIndex);
                    priorityCurrentOrder.Insert(newIndex, item);
                    priorityDraggedIndex = newIndex;
                }
                e.Use();
            }
            else if (e.type == EventType.MouseUp)
            {
                priorityDraggedIndex = -1;
                e.Use();
            }
        }

        if (priorityDraggedIndex != -1)
        {
            float drawY = e.mousePosition.y - priorityDragOffsetY;
            Rect draggedRect = new Rect(10, drawY, 270, 20);
            GUI.skin.label.fontStyle = FontStyle.Bold;
            GUI.Label(draggedRect, "x. " + GetPriorityCriteriaName(priorityCurrentOrder[priorityDraggedIndex]));
            GUI.skin.label.fontStyle = FontStyle.Normal;
        }

        if (GUI.Button(new Rect(10, 520, 70, 30), "Save"))
        {
            PriorityPatches.SavePriorityOrder(priorityCurrentOrder);
            PriorityPatches.TriggerPrioritySort();
            showPriorityWindow = false;

        }

        if (GUI.Button(new Rect(90, 520, 70, 30), "Cancel"))
        {
            showPriorityWindow = false;
            priorityCurrentOrder = PriorityPatches.LoadPriorityOrder();

        }

        if (GUI.Button(new Rect(170, 520, 70, 30), "Reset"))
        {
            priorityCurrentOrder = new List<PriorityCriteria>
            {
                PriorityCriteria.Favorited, PriorityCriteria.NotFavorited, PriorityCriteria.Unlocked,
                PriorityCriteria.Locked, PriorityCriteria.RecentlyUsed, PriorityCriteria.RecentlyAcquired,
                PriorityCriteria.InstanceName, PriorityCriteria.Oddity, PriorityCriteria.Exotic,
                PriorityCriteria.Epic, PriorityCriteria.Rare, PriorityCriteria.Standard,
                PriorityCriteria.Turbocharged, PriorityCriteria.Trashed, PriorityCriteria.NotTurbocharged,
                PriorityCriteria.NotTrashed
            };

        }
    }

    private static string GetPriorityCriteriaName(PriorityCriteria criteria)
    {
        return criteria switch
        {
            PriorityCriteria.Favorited => "Favorited",
            PriorityCriteria.NotFavorited => "Not Favorited",
            PriorityCriteria.Unlocked => "Unlocked",
            PriorityCriteria.Locked => "Locked",
            PriorityCriteria.RecentlyUsed => "Recently Used",
            PriorityCriteria.RecentlyAcquired => "Recently Acquired",
            PriorityCriteria.InstanceName => "Upgrade Instance Name",
            PriorityCriteria.Oddity => "Oddity",
            PriorityCriteria.Exotic => "Exotic",
            PriorityCriteria.Epic => "Epic",
            PriorityCriteria.Rare => "Rare",
            PriorityCriteria.Standard => "Standard",
            PriorityCriteria.Turbocharged => "Turbocharged",
            PriorityCriteria.Trashed => "Trashed",
            PriorityCriteria.NotTurbocharged => "Not Turbocharged",
            PriorityCriteria.NotTrashed => "Not Trashed",
            _ => "Unknown"
        };
    }

    private static void DrawPopup(int windowID)
    {
        GUI.Label(new Rect(10, 20, 280, 40), popupMessage);
        if (GUI.Button(new Rect(20, 60, 80, 30), "Yes"))
        {
            if (popupAction == "ScrapMarked")
            {
                ScrapHandlingMod.ScrapMarkedAction?.Invoke();
            }
            else if (popupAction == "ScrapNonFavorite")
            {
                ScrapHandlingMod.ScrapNonFavoriteAction?.Invoke();
            }
            showPopup = false;
        }
        if (GUI.Button(new Rect(200, 60, 80, 30), "No"))
        {
            showPopup = false;
        }
    }

    private static bool showPopup = false;
    private static string popupAction = "";
    private static string popupMessage = "";
    private static bool showPriorityWindow = false;
    private static Rect priorityWindowRect = new Rect(0, 0, 300, 600);
    private static List<PriorityCriteria> priorityCurrentOrder;
    private static int priorityDraggedIndex = -1;
    private static float priorityDragOffsetY;
}
