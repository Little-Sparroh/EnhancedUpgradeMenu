using HarmonyLib;

public static class UpgradeSolverPatches
{
    [HarmonyPatch(typeof(GearDetailsWindow), "OnOpen")]
    public static class GearDetailsWindowPatch
    {
        [HarmonyPostfix]
        public static void Postfix(GearDetailsWindow __instance)
        {
            UpgradeSolver.Instance.OnGearDetailsWindowOpen(__instance, SparrohPlugin.Instance);
        }
    }

    [HarmonyPatch(typeof(GearDetailsWindow), "OnCloseCallback")]
    public static class GearDetailsWindowClosePatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            UpgradeSolver.Instance.OnGearDetailsWindowClosed();
        }
    }
}
