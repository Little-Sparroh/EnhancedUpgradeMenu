using HarmonyLib;

[HarmonyPatch(typeof(GearDetailsWindow), "Awake")]
public static class GearDetailsWindow_Patch
{
    [HarmonyPostfix]
    static void Postfix(GearDetailsWindow __instance)
    {
        var comp = __instance.gameObject.AddComponent<SharingButtons>();
        comp.window = __instance;
    }
}

[HarmonyPatch(typeof(ModuleEquipSlots), "EquipModule")]
public static class EquipModule_Patch
{
    [HarmonyPrefix]
    static void Prefix(object target, object upgrade, int offsetX, int offsetY, byte rotation, bool sort)
    {
        var ug = upgrade as UpgradeInstance;
    }

    [HarmonyPostfix]
    static void Postfix(bool __result, object target, object upgrade, int offsetX, int offsetY, byte rotation, bool sort)
    {
        var ug = upgrade as UpgradeInstance;
    }
}
