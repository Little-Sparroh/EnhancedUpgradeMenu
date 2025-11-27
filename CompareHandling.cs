using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using Pigeon;
using UnityEngine.InputSystem;
using BepInEx.Logging;

public static class CompareHandlingMod
{
    internal static ManualLogSource Logger;

    public static UnityEngine.InputSystem.Key CompareKey = UnityEngine.InputSystem.Key.C;

    public static void Init()
    {

    }

    private static GearUpgradeUI LockedUpgrade = null;
    private static GearUpgradeUI HoveredUpgrade = null;
    private static GearUpgradeUI LastDisplayedUpgrade = null;
    private static HoverInfoDisplay LockedDisplayInstance = null;
    private static bool IsComparisonModeActive = false;
    private static bool IsDisplayCreated = false;

    private static bool JustCalledShowInfo = false;

    [HarmonyPatch(typeof(HoverInfoDisplay), "Awake")]
    [HarmonyPrefix]
    private static bool HoverInfoDisplayAwakePrefix(HoverInfoDisplay __instance)
    {
        if (HoverInfoDisplay.Instance == null)
        {
            typeof(HoverInfoDisplay).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.SetValue(null, __instance);
            return true;
        }
        else
        {
            return false;
        }
    }

    [HarmonyPatch(typeof(HoverInfoDisplay), "Activate", typeof(HoverInfo))]
    [HarmonyPostfix]
    private static void HoverInfoDisplayActivatePostfix(HoverInfo info)
    {
        if (info is GearUpgradeUI gearUpgradeUI)
        {
            HoveredUpgrade = gearUpgradeUI;
            HandleComparisonLogic();
        if (IsComparisonModeActive && LockedDisplayInstance != null && LockedUpgrade != null)
        {
            LockedDisplayInstance.ShowInfo(LockedUpgrade as HoverInfo);
            LockedDisplayInstance.gameObject.SetActive(true);
            PositionLockedDisplaySynchronously();
            LockedDisplayInstance.transform.SetAsLastSibling();
        }
        }
    }

    [HarmonyPatch(typeof(HoverInfoDisplay), "ShowInfo")]
    [HarmonyPrefix]
    private static void HoverInfoDisplayShowInfoPrefix(HoverInfoDisplay __instance, HoverInfo info)
    {
        if (__instance == LockedDisplayInstance)
        {
            JustCalledShowInfo = true;
        }
    }

    [HarmonyPatch(typeof(HoverInfoDisplay), "ShowInfo")]
    [HarmonyPostfix]
    private static void HoverInfoDisplayShowInfoPostfix(HoverInfoDisplay __instance, HoverInfo info)
    {
        if (__instance == LockedDisplayInstance)
        {
            JustCalledShowInfo = false;
        }
    }

    [HarmonyPatch(typeof(HoverInfoDisplay), "UpdatePosition", typeof(bool))]
    [HarmonyPostfix]
    private static void HoverInfoDisplayUpdatePositionPostfix(HoverInfoDisplay __instance, bool setInitialPos)
    {
        if (__instance == LockedDisplayInstance && IsComparisonModeActive)
        {
            __instance.RectTransform.anchoredPosition = new Vector2(-600, 0);
        }
        if (__instance == HoverInfoDisplay.Instance && IsComparisonModeActive)
        {
            __instance.RectTransform.anchoredPosition = new Vector2(600, 0);
        }
    }

    [HarmonyPatch(typeof(HoverInfoDisplay), "Deactivate")]
    [HarmonyPrefix]
    private static bool HoverInfoDisplayDeactivatePrefix(HoverInfoDisplay __instance)
    {
        if ((__instance == HoverInfoDisplay.Instance || __instance == LockedDisplayInstance) && IsComparisonModeActive)
        {
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(GearUpgradeUI), "OnPointerEnter")]
    [HarmonyPostfix]
    private static void GearUpgradeUIOnPointerEnterPostfix2(GearUpgradeUI __instance)
    {
        HandleComparisonLogic();
    }

    [HarmonyPatch(typeof(GameManager), "Update")]
    [HarmonyPostfix]
    private static void GameManagerUpdatePostfix()
    {
        bool keyPressed = Menu.Instance != null &&
                           Menu.Instance.WindowSystem.GetTop() is GearDetailsWindow &&
                           Keyboard.current != null &&
                           Keyboard.current[CompareKey].wasPressedThisFrame;

        if (keyPressed)
        {
            HandleCompareToggle((Menu.Instance.WindowSystem.GetTop() as GearDetailsWindow));
        }
    }

    private static void HandleCompareToggle(GearDetailsWindow gearDetailsWindow)
    {
        GearUpgradeUI currentlyHovered = GetCurrentlyHoveredUpgrade(gearDetailsWindow);

        if (currentlyHovered != null)
        {
            if (currentlyHovered == LockedUpgrade)
            {
                LockedUpgrade = null;
                IsComparisonModeActive = false;
            }
            else
            {
                LockedUpgrade = currentlyHovered;
                IsComparisonModeActive = true;

                if (HoverInfoDisplay.Instance != null && !IsDisplayCreated)
                {
                    var temp = HoverInfoDisplay.Instance;
                    var parentTransform = Menu.Instance != null ? Menu.Instance.transform : GameManager.Instance.WindowSystem.transform;
                    LockedDisplayInstance = GameObject.Instantiate(HoverInfoDisplay.Instance, parentTransform);
                    typeof(HoverInfoDisplay).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.SetValue(null, temp);

                    LockedDisplayInstance.ShowInfo(LockedUpgrade as HoverInfo);
                    LockedDisplayInstance.gameObject.SetActive(true);
                    IsDisplayCreated = true;
                    PositionLockedDisplayCompanion();
                }
            }
        }
    }

    private static GearUpgradeUI GetCurrentlyHoveredUpgrade(GearDetailsWindow gearDetailsWindow)
    {
        if (UIRaycaster.RaycastForComponent<GearUpgradeUI>(out var hoveredUpgrade))
        {
            return hoveredUpgrade;
        }
        return null;
    }

    private static void UpdateCurrentlyHoveredUpgrade(GearDetailsWindow gearDetailsWindow)
    {
        HoveredUpgrade = GetCurrentlyHoveredUpgrade(gearDetailsWindow);
    }

    private static void HandleComparisonLogic()
    {
        bool shouldActivateComparison = LockedUpgrade != null && HoveredUpgrade != null && LockedUpgrade != HoveredUpgrade && !IsComparisonModeActive;
        bool shouldDeactivateComparison = LockedUpgrade == null && IsComparisonModeActive;

        if (shouldActivateComparison)
        {
            IsComparisonModeActive = true;
        }
        else if (shouldDeactivateComparison)
        {
            IsComparisonModeActive = false;
            if (LockedDisplayInstance != null)
            {
                LockedDisplayInstance.gameObject.SetActive(false);
            }
            LastDisplayedUpgrade = null;
        }

        if (IsComparisonModeActive)
        {
            if (HoveredUpgrade != null)
            {
                LastDisplayedUpgrade = HoveredUpgrade;
            }
            if (LastDisplayedUpgrade != null)
            {
                HoverInfoDisplay.Instance.ShowInfo(LastDisplayedUpgrade as HoverInfo);
                HoverInfoDisplay.Instance.gameObject.SetActive(true);
                HoverInfoDisplay.Instance.GetComponent<RectTransform>().anchoredPosition = new Vector2(600, 0);
            }
        }
    }

    private static void PositionLockedDisplayCompanion()
    {
        var mainRect = HoverInfoDisplay.Instance.RectTransform;
        var canvasRect = mainRect.parent as RectTransform;

        if (canvasRect == null)
        {
            LockedDisplayInstance.GetComponent<RectTransform>().anchoredPosition = new Vector2(-960f, 0f); // fallback
            return;
        }

        Vector2 canvasSize = canvasRect.rect.size;

        Vector2 position = new Vector2(-600f, 0f);

        LockedDisplayInstance.GetComponent<RectTransform>().anchoredPosition = position;
        LockedDisplayInstance.transform.SetAsLastSibling();
    }

    private static bool IsCompanionPositionValid(Vector2 position, Vector2 size, Vector2 canvasSize, RectTransform mainRect)
    {
        if (position.x < -canvasSize.x / 2f || position.x + size.x > canvasSize.x / 2f ||
            position.y - size.y < -canvasSize.y / 2f || position.y > canvasSize.y / 2f)
        {
            return false;
        }

        Rect lockedRect = new Rect(position - size / 2f, size);
        Rect mainRectBounds = new Rect(mainRect.anchoredPosition - mainRect.sizeDelta / 2f, mainRect.sizeDelta);
        mainRectBounds = new Rect(mainRectBounds.xMin - 30f, mainRectBounds.yMin - 30f,
                                 mainRectBounds.width + 60f, mainRectBounds.height + 60f);

        return !lockedRect.Overlaps(mainRectBounds);
    }

    private static Vector2 ClampToScreenBoundsCompanion(Vector2 position, Vector2 size, Vector2 canvasSize, RectTransform mainRect)
    {
        float halfCanvasX = canvasSize.x / 2f;
        float halfCanvasY = canvasSize.y / 2f;

        Vector2 mainCenter = mainRect.anchoredPosition;

        position.x = Mathf.Clamp(position.x, -halfCanvasX, halfCanvasX - size.x);
        position.y = Mathf.Clamp(position.y, -halfCanvasY + size.y, halfCanvasY);

        if (Mathf.Abs(position.x - mainCenter.x) > 200f)
        {
            if (mainCenter.y + size.y + 100f <= halfCanvasY)
            {
                position.y = mainCenter.y + 100f;
            }
            else if (mainCenter.y - size.y - 100f >= -halfCanvasY)
            {
                position.y = mainCenter.y - 100f;
            }
        }

        return position;
    }

    private static void PositionLockedDisplaySynchronously()
    {
        var canvasRect = HoverInfoDisplay.Instance.RectTransform.parent as RectTransform;

        if (canvasRect == null)
        {
            return;
        }

        Vector2 canvasSize = canvasRect.rect.size;

        Vector2 position = Vector2.zero;

        LockedDisplayInstance.GetComponent<RectTransform>().anchoredPosition = position;
    }

    private static bool IsCompanionPositionValidSync(Vector2 position, Vector2 size, Vector2 canvasSize, Vector2 mainDisplayPos, Vector2 mainDisplaySize)
    {
        if (position.x < -canvasSize.x / 2f || position.x + size.x > canvasSize.x / 2f ||
            position.y - size.y < -canvasSize.y / 2f || position.y > canvasSize.y / 2f)
        {
            return false;
        }

        Rect companionRect = new Rect(position - size / 2f, size);
        Rect mainRectBounds = new Rect(mainDisplayPos - mainDisplaySize / 2f, mainDisplaySize);
        mainRectBounds = new Rect(mainRectBounds.xMin - 30f, mainRectBounds.yMin - 30f,
                                 mainRectBounds.width + 60f, mainRectBounds.height + 60f);

        return !companionRect.Overlaps(mainRectBounds);
    }

    private static Vector2 ClampToScreenBoundsSync(Vector2 position, Vector2 size, Vector2 canvasSize, Vector2 mainDisplayPos, Vector2 mainDisplaySize)
    {
        float halfCanvasX = canvasSize.x / 2f;
        float halfCanvasY = canvasSize.y / 2f;

        position.x = Mathf.Clamp(position.x, -halfCanvasX, halfCanvasX - size.x);
        position.y = Mathf.Clamp(position.y, -halfCanvasY + size.y, halfCanvasY);

        if (Mathf.Abs(position.x - mainDisplayPos.x) > 150f)
        {
            if (mainDisplayPos.y + mainDisplaySize.y + size.y + 100f <= halfCanvasY)
            {
                position.y = mainDisplayPos.y + mainDisplaySize.y + 50f;
                position.x = Mathf.Clamp(position.x, -halfCanvasX, halfCanvasX - size.x);
            }
            else if (mainDisplayPos.y - size.y - 100f >= -halfCanvasY)
            {
                position.y = mainDisplayPos.y - size.y - 50f;
                position.x = Mathf.Clamp(position.x, -halfCanvasX, halfCanvasX - size.x);
            }
        }

        return position;
    }

    [HarmonyPatch(typeof(GearDetailsWindow), "Setup")]
    [HarmonyPostfix]
    private static void GearDetailsWindowSetupPostfix()
    {
        LockedUpgrade = null;
        IsComparisonModeActive = false;
        if (LockedDisplayInstance != null)
        {
            LockedDisplayInstance.gameObject.SetActive(false);
            LockedDisplayInstance = null;
        }
        IsDisplayCreated = false;
    }

    [HarmonyPatch(typeof(GearDetailsWindow), "OnCloseCallback")]
    [HarmonyPostfix]
    private static void GearDetailsWindowOnCloseCallbackPostfix()
    {
        LockedUpgrade = null;
        IsComparisonModeActive = false;
        if (LockedDisplayInstance != null)
        {
            LockedDisplayInstance.gameObject.SetActive(false);
            LockedDisplayInstance = null;
        }
        IsDisplayCreated = false;
    }

    [HarmonyPatch(typeof(GearUpgradeUI), "OnPointerEnter")]
    [HarmonyPostfix]
    private static void GearUpgradeUIOnPointerEnterPostfix(GearUpgradeUI __instance)
    {
        HoveredUpgrade = __instance;
    }

    [HarmonyPatch(typeof(GearUpgradeUI), "OnPointerExit")]
    [HarmonyPostfix]
    private static void GearUpgradeUIOnPointerExitPostfix()
    {
        HoveredUpgrade = null;
        HandleComparisonLogic();
    }
}
