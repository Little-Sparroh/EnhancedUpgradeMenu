using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System;
using System.Reflection;
using System.Linq;
using Pigeon;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[MycoMod(null, ModFlags.IsClientSide)]
public class SparrohPlugin : BaseUnityPlugin
{
    public const string PluginGUID = "sparroh.enhancedupgrademenu";
    public const string PluginName = "EnhancedUpgradeMenu";
    public const string PluginVersion = "1.0.0";

    private ConfigEntry<Key> TrashMarkKey;
    private ConfigEntry<Key> ScrollerToggleKey;
    private ConfigEntry<Key> CompareKey;

    private void Awake()
    {
        var harmony = new Harmony(PluginGUID);

        LoadoutHandlingMod.Logger = Logger;
        ScrapHandlingMod.Logger = Logger;
        CompareHandlingMod.Logger = Logger;
        SortHandlingMod.Logger = Logger;
        LoadoutExpanderMod.LoggerSource = Logger;

// Temporarily disabled - comparison mode broken
        //CompareHandlingMod.Init();

        LoadoutExpanderMod._loadoutButtonsField = AccessTools.Field(typeof(GearDetailsWindow), "loadoutButtons");
        LoadoutExpanderMod._upgradableField = AccessTools.Field(typeof(GearDetailsWindow), "upgradable");
        LoadoutExpanderMod._updateIconMethod = AccessTools.Method(typeof(GearDetailsWindow), "UpdateLoadoutIcon");

        TrashMarkKey = Config.Bind("Keybinds", "TrashMarkKey", Key.T, "Key to toggle trash mark on upgrades");
        ScrapHandlingMod.currentTrashKey = TrashMarkKey.Value;
        TrashMarkKey.SettingChanged += (sender, args) => { ScrapHandlingMod.currentTrashKey = TrashMarkKey.Value; };
        ScrapHandlingMod.ScrapMarkedAction = () => ScrapHandlingMod.TryScrapMarkedUpgrades(this);
        ScrapHandlingMod.ScrapNonFavoriteAction = () => ScrapHandlingMod.TryScrapNonFavoriteUpgrades(this);

        ScrollerToggleKey = Config.Bind("Keybinds", "PageToggleKey", Key.Period, "Key to toggle between loadout pages");
        LoadoutExpanderMod.ToggleHotkey = ScrollerToggleKey.Value;
        ScrollerToggleKey.SettingChanged += (sender, args) => { LoadoutExpanderMod.ToggleHotkey = ScrollerToggleKey.Value; };

// Temporarily disabled - comparison mode broken
        //CompareKey = Config.Bind("Keybinds", "CompareKey", Key.C, "Key to toggle compare mode for upgrades");
        //CompareHandlingMod.CompareKey = (UnityEngine.InputSystem.Key)CompareKey.Value;
        //CompareKey.SettingChanged += (sender, args) => { CompareHandlingMod.CompareKey = (UnityEngine.InputSystem.Key)CompareKey.Value; };

        harmony.PatchAll();

        var setupMethod = AccessTools.Method(typeof(GearDetailsWindow), "Setup", new[] { typeof(IUpgradable) });
        if (setupMethod != null)
        {
            harmony.Patch(setupMethod, postfix: new HarmonyMethod(typeof(Patches), "SetupPostfix"));
        }

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

        var gameManagerUpdateMethod = AccessTools.Method(typeof(GameManager), "Update");
        if (gameManagerUpdateMethod != null)
        {
            harmony.Patch(gameManagerUpdateMethod, postfix: new HarmonyMethod(typeof(CompareHandlingMod), "GameManagerUpdatePostfix"));
        }

        Logger.LogInfo($"{PluginName} loaded successfully.");
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current[LoadoutExpanderMod.ToggleHotkey].wasPressedThisFrame)
        {
            LoadoutExpanderMod.TogglePage();
        }


    }

    private void OnGUI()
    {
        if (showPopup)
        {
            Rect popupRect = new Rect(Screen.width / 2 - 150, Screen.height / 2 - 50, 300, 100);
            GUI.Window(0, popupRect, DrawPopup, "Confirm Action");
        }
        else if (Menu.Instance != null && Menu.Instance.IsOpen && Menu.Instance.WindowSystem.GetTop() is GearDetailsWindow)
        {
            DrawButtons();
        }
    }

    private static void DrawButtons()
    {
        float buttonY = Screen.height - 60;
        float buttonX = Screen.width * 0.75f;
        float buttonWidth = 150f;
        float buttonHeight = 50f;
        float spacing = 10f;

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
}
