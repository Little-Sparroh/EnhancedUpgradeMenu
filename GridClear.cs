using BepInEx.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class GridClearMod
{
    public static void Initialize()
    {
    }

    public static void DrawClearButton()
    {
        if (Menu.Instance != null && Menu.Instance.IsOpen)
        {
            var topWindow = Menu.Instance.WindowSystem.GetTop() as GearDetailsWindow;
            if (topWindow != null)
            {
                float buttonX = 10f;
                float buttonY = Screen.height - 60f;
                float buttonWidth = 100f;
                float buttonHeight = 50f;
                if (GUI.Button(new Rect(buttonX, buttonY, buttonWidth, buttonHeight), "Clear Grid"))
                {
                    ClearAllUpgrades(topWindow);
                }
            }
        }
    }

    private static void ClearAllUpgrades(GearDetailsWindow window)
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
            equipSlots.Unequip(window.UpgradablePrefab, upgrade);
        }
    }

    private static ModuleEquipSlots GetEquipSlots(GearDetailsWindow window)
    {
        return (ModuleEquipSlots)typeof(GearDetailsWindow).GetField("equipSlots", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(window);
    }
}
