using BepInEx.Configuration;
using HarmonyLib;
using System.IO;

public static class InstantScrapping
{
    internal static ConfigEntry<bool> EnableFixedTimer;
    internal static ConfigEntry<float> FixedTimerDuration;
    internal static ConfigEntry<bool> EnableInstantScrapping;

    internal static bool s_EnableInstantScrapping;
    internal static bool s_EnableFixedTimer;
    internal static float s_FixedTimerDuration;

    private static FileSystemWatcher configWatcher;
    private static Harmony _harmony;

    public static void Initialize(ConfigEntry<bool> enableFixed, ConfigEntry<float> fixedDur, ConfigEntry<bool> enableInst)
    {
        EnableFixedTimer = enableFixed;
        FixedTimerDuration = fixedDur;
        EnableInstantScrapping = enableInst;

        s_EnableInstantScrapping = EnableInstantScrapping.Value;
        s_EnableFixedTimer = EnableFixedTimer.Value;
        s_FixedTimerDuration = FixedTimerDuration.Value;

        SetupFileWatchers();

        _harmony = new Harmony("sparroh.instantscrapping");

        var unlockActionParamsType = AccessTools.TypeByName("UnlockActionParams") ?? AccessTools.TypeByName("Pigeon.UnlockActionParams");
        if (unlockActionParamsType == null)
        {
            return;
        }
        var hasUnlockActionMethod = AccessTools.Method(typeof(GearUpgradeUI), "HasUnlockAction", new[] { unlockActionParamsType.MakeByRefType() });
        if (hasUnlockActionMethod != null)
        {
            _harmony.Patch(hasUnlockActionMethod, postfix: new HarmonyMethod(typeof(InstantScrapPatches), "HasUnlockActionPostfix"));
        }
    }

    private static void SetupFileWatchers()
    {
        var configPath = BepInEx.Paths.ConfigPath;
        configWatcher = new FileSystemWatcher(configPath, $"sparroh.enhancedupgrademenu.cfg");
        configWatcher.Changed += (s, e) =>
        {
            EnableInstantScrapping.ConfigFile.Reload();
            s_EnableInstantScrapping = EnableInstantScrapping.Value;
            s_EnableFixedTimer = EnableFixedTimer.Value;
            s_FixedTimerDuration = FixedTimerDuration.Value;
        };
        configWatcher.EnableRaisingEvents = true;
    }

    public static void Destroy()
    {
        if (configWatcher != null)
        {
            configWatcher.EnableRaisingEvents = false;
            configWatcher.Dispose();
        }
    }
}

public static class InstantScrapPatches
{
    public static void HasUnlockActionPostfix(ref object data)
    {
        if (data == null)
        {
            return;
        }

        var onSecondaryCompleteField = data.GetType().GetField("OnSecondaryComplete");
        if (onSecondaryCompleteField == null)
        {
            return;
        }

        var onSecondary = onSecondaryCompleteField.GetValue(data);
        if (onSecondary == null)
        {
            return;
        }

        var durationField = data.GetType().GetField("SecondaryDuration");
        if (durationField == null)
        {
            return;
        }

        float currentDuration = (float)durationField.GetValue(data);

        if (InstantScrapping.EnableInstantScrapping.Value)
        {
            durationField.SetValue(data, 0.001f);
        }
        else if (InstantScrapping.EnableFixedTimer.Value)
        {
            durationField.SetValue(data, InstantScrapping.FixedTimerDuration.Value);
        }
        else
        {
            return;
        }

        float newDuration = (float)durationField.GetValue(data);
    }
}
