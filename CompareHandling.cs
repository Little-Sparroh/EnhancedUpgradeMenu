using System;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine.InputSystem;

public static class CompareHandling
{
    public static ConfigEntry<Key> CompareKey;

    public static void Initialize()
    {
        try
        {
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Failed to initialize CompareHandling: {ex.Message}");
        }
    }

    public static void ApplyPatches(Harmony harmony)
    {
        try
        {
            CompareHoverPatches.ApplyPatches(harmony);
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Failed to apply CompareHandling patches: {ex.Message}");
        }
    }
}
