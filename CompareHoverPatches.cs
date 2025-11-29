using HarmonyLib;
using UnityEngine;
using Pigeon;

[HarmonyPatch]
public class CompareHoverPatches
{

    public static void ApplyPatches(Harmony harmony)
    {
        harmony.PatchAll();
    }

    [HarmonyPatch(typeof(HoverInfoDisplay), "OnHoverEnter")]
    [HarmonyPostfix]
    public static void OnHoverEnter_Postfix(HoverInfo info)
    {

        if (CompareHoverManager.Instance == null)
        {
            var managerObject = new GameObject("CompareHoverManager");
            UnityEngine.Object.DontDestroyOnLoad(managerObject);
            managerObject.AddComponent<CompareHoverManager>();
        }

        if (CompareHoverManager.Instance == null)
        {
            return;
        }

        if (info is HoverInfoUpgrade)
        {
            CompareHoverManager.currentHoverUpgrade = info;
        }
        else
        {
            CompareHoverManager.currentHoverUpgrade = null;
        }
    }

    [HarmonyPatch(typeof(HoverInfoDisplay), "OnHoverExit")]
    [HarmonyPostfix]
    public static void OnHoverExit_Postfix(HoverInfo info)
    {
        CompareHoverManager.currentHoverUpgrade = null;
    }

    [HarmonyPatch(typeof(HoverInfoDisplay), "Update")]
    [HarmonyPrefix]
    public static bool Update_Prefix(HoverInfoDisplay __instance)
    {
        if (__instance.gameObject.GetComponent<ComparisonDisplayMarker>() != null)
        {
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(HoverInfoDisplay), "ShowInfo")]
    [HarmonyPrefix]
    public static bool ShowInfo_Prefix(HoverInfoDisplay __instance, HoverInfo info)
    {
        if (__instance.gameObject.GetComponent<ComparisonDisplayMarker>() != null)
        {

            if (CompareHoverManager.currentComparisonInfo != null && info != CompareHoverManager.currentComparisonInfo)
            {
            }
        }
        return true;
    }

    [HarmonyPatch(typeof(HoverInfoDisplay), "Activate")]
    [HarmonyPrefix]
    public static bool Activate_Prefix(HoverInfoDisplay __instance, HoverInfo info, bool resetPosition)
    {
        if (__instance.gameObject.GetComponent<ComparisonDisplayMarker>() != null)
        {
            if (info == CompareHoverManager.currentComparisonInfo)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        return true;
    }

    [HarmonyPatch(typeof(HoverInfoDisplay), "Deactivate")]
    [HarmonyPrefix]
    public static bool Deactivate_Prefix(HoverInfoDisplay __instance)
    {
        if (__instance.gameObject.GetComponent<ComparisonDisplayMarker>() != null)
        {
            return false;
        }
        return true;
    }
}
