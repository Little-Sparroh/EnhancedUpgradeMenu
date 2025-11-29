using BepInEx.Logging;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using Pigeon;
using BepInEx;

public class CompareHoverManager : MonoBehaviour
{
    private static ManualLogSource Logger => CompareHandling.Logger;

    public static CompareHoverManager Instance { get; private set; }

    public static HoverInfo currentComparisonInfo;
    public static HoverInfo currentHoverUpgrade;

    private static GameObject comparisonDisplayContainer;
    private static HoverInfoDisplay comparisonDisplay;
    private static HoverInfoDisplay mainDisplay;

    private static FileSystemWatcher configWatcher;

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SetupConfigWatcher();
    }

    private void SetupConfigWatcher()
    {
        try
        {
            string configDir = Paths.ConfigPath;
            string configFile = "sparroh.enhancedupgrademenu.cfg";

            if (!string.IsNullOrEmpty(configDir) && Directory.Exists(configDir))
            {
                configWatcher = new FileSystemWatcher(configDir, configFile);
                configWatcher.NotifyFilter = NotifyFilters.LastWrite;
                configWatcher.Created += OnConfigChanged;
                configWatcher.Changed += OnConfigChanged;
                configWatcher.EnableRaisingEvents = true;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to setup config watcher: {ex.Message}");
        }
    }

    private void OnConfigChanged(object sender, FileSystemEventArgs e)
    {
    }

    public static bool IsCompareKeyPressed()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return false;

        Key key = CompareHandling.CompareKey.Value;

        bool pressed = keyboard[key].wasPressedThisFrame;
        return pressed;
    }

    public static void SetComparisonUpgrade(HoverInfo info)
    {

        if (currentComparisonInfo == info)
        {
            return;
        }

        ClearComparisonUpgrade();

        currentComparisonInfo = info;

        if (info != null)
        {
            CreateComparisonDisplay(info);
        }
    }

    public static void ClearComparisonUpgrade()
    {
        if (currentComparisonInfo == null) return;

        currentComparisonInfo = null;

        if (comparisonDisplayContainer != null)
        {
            Destroy(comparisonDisplayContainer);
            comparisonDisplayContainer = null;
            comparisonDisplay = null;
        }
    }

    private static void CreateComparisonDisplay(HoverInfo info)
    {
        if (comparisonDisplayContainer != null) return;

        if (mainDisplay == null)
        {
            mainDisplay = HoverInfoDisplay.Instance;
        }
        var mainDisplayToClone = mainDisplay ?? HoverInfoDisplay.Instance;
        if (mainDisplayToClone == null)
        {
            return;
        }

        comparisonDisplayContainer = GameObject.Instantiate(mainDisplay.gameObject);
        comparisonDisplayContainer.name = "CompareHoverDisplay";
        comparisonDisplay = comparisonDisplayContainer.GetComponent<HoverInfoDisplay>();

        comparisonDisplayContainer.AddComponent<ComparisonDisplayMarker>();

        comparisonDisplayContainer.transform.SetParent(mainDisplay.transform.parent, false);

        var rectTransform = comparisonDisplayContainer.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.15f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.15f, 0.5f);
        rectTransform.pivot = new Vector2(0f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;

        comparisonDisplay.ShowInfo(info);
    }

    private void Update()
    {
        if (currentHoverUpgrade != null && IsCompareKeyPressed() && currentHoverUpgrade is HoverInfoUpgrade)
        {

            if (CompareHoverManager.currentComparisonInfo == currentHoverUpgrade)
            {
                CompareHoverManager.ClearComparisonUpgrade();
            }
            else
            {
                CompareHoverManager.SetComparisonUpgrade(currentHoverUpgrade);
            }
        }

        if (comparisonDisplayContainer != null)
        {
            if (!comparisonDisplayContainer.gameObject.activeSelf)
            {
                comparisonDisplayContainer.gameObject.SetActive(true);
            }

            var rectTransform = comparisonDisplayContainer.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.15f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.15f, 0.5f);
            rectTransform.pivot = new Vector2(0f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
        }
    }

    private void OnDestroy()
    {
        if (configWatcher != null)
        {
            configWatcher.Dispose();
        }
        ClearComparisonUpgrade();
    }
}
