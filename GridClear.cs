using BepInEx.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class GridClearMod
{
    private static readonly PropertyInfo _upgradablePrefabProperty = typeof(GearDetailsWindow).GetProperty("UpgradablePrefab", BindingFlags.Public | BindingFlags.Instance);
    private static readonly FieldInfo _selectedGearField = typeof(OuroGearWindow).GetField("selectedGear", BindingFlags.NonPublic | BindingFlags.Instance);

    public static void Initialize()
    {
    }

    public static void DrawClearButton()
    {
        if (Menu.Instance != null && Menu.Instance.IsOpen)
        {
            var topWindow = Menu.Instance.WindowSystem.GetTop();
            IUpgradeWindow upgradeWindow = topWindow as GearDetailsWindow;
            IUpgradable gear = null;
            if (upgradeWindow != null)
            {
                gear = _upgradablePrefabProperty?.GetValue(upgradeWindow) as IUpgradable;
            }
            else
            {
                upgradeWindow = topWindow as OuroGearWindow;
                if (upgradeWindow != null)
                {
                    gear = _selectedGearField?.GetValue(upgradeWindow) as IUpgradable;
                }
            }
            if (upgradeWindow != null && gear != null)
            {
                float buttonX = 10f;
                float buttonY = Screen.height - 60f;
                float buttonWidth = 100f;
                float buttonHeight = 50f;
                if (GUI.Button(new Rect(buttonX, buttonY, buttonWidth, buttonHeight), "Clear Grid"))
                {
                    ClearAllUpgrades(upgradeWindow, gear);
                }
            }
        }
    }

    private static void ClearAllUpgrades(IUpgradeWindow window, IUpgradable gear)
    {
        var equipSlots = GetEquipSlots(window);
        if (equipSlots == null) return;
        var hexMap = equipSlots.HexMap;
        var upgradesToClear = new List<UpgradeInstance>();
        for (int i = 0; i < hexMap.Height; i++)
        {
            for (int j = 0; j < hexMap.Width; j++)
            {
                var node = hexMap[j, i];
                if (node.upgrade != null)
                {
                    upgradesToClear.Add(node.upgrade);
                }
            }
        }
        var sortedUpgrades = upgradesToClear.OrderBy(u => u.Upgrade.Name == "Boundary Incursion" ? 1 : 0).ToList();
        foreach (var upgrade in sortedUpgrades)
        {
            equipSlots.Unequip(gear, upgrade);
        }
    }

    private static ModuleEquipSlots GetEquipSlots(IUpgradeWindow window)
    {
        return (ModuleEquipSlots)window.GetType().GetField("equipSlots", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(window);
    }
}
