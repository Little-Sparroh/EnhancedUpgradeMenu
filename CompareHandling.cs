using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine.InputSystem;

public static class CompareHandling
{
    internal static ManualLogSource Logger;
    public static ConfigEntry<Key> CompareKey;

    public static void Initialize(ManualLogSource logger)
    {
        Logger = logger;

        Logger.LogInfo("CompareHoverPatches and manager initialized successfully.");
    }

    public static void ApplyPatches(Harmony harmony)
    {
        CompareHoverPatches.ApplyPatches(harmony);
    }
}
