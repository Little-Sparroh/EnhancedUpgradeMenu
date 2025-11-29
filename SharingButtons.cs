using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

public class SharingButtons : MonoBehaviour
{
    public GearDetailsWindow window;
    private ModuleEquipSlots equipSlots;

    private const uint CF_TEXT = 1;
    private const uint GMEM_MOVEABLE = 0x2;

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetClipboardData(uint uFormat);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr data);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(IntPtr hMem);

    private static string Clipboard_GetText()
    {
        if (!OpenClipboard(IntPtr.Zero))
            return null;

        try
        {
            IntPtr ptr = GetClipboardData(CF_TEXT);
            if (ptr == IntPtr.Zero)
                return null;

            IntPtr locked = GlobalLock(ptr);
            if (locked == IntPtr.Zero)
                return null;

            try
            {
                return Marshal.PtrToStringAnsi(locked);
            }
            finally
            {
                GlobalUnlock(ptr);
            }
        }
        finally
        {
            CloseClipboard();
        }
    }

    private static void Clipboard_SetText(string text)
    {
        if (!OpenClipboard(IntPtr.Zero))
            return;

        try
        {
            EmptyClipboard();
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text + '\0');
            IntPtr hMem = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)bytes.Length);
            if (hMem == IntPtr.Zero)
                return;

            IntPtr locked = GlobalLock(hMem);
            if (locked != IntPtr.Zero)
            {
                try
                {
                    Marshal.Copy(bytes, 0, locked, bytes.Length);
                }
                finally
                {
                    GlobalUnlock(hMem);
                }
                SetClipboardData(CF_TEXT, hMem);
            }
        }
        finally
        {
            CloseClipboard();
        }
    }

    void OnGUI()
    {
        if (window == null || window.UpgradablePrefab == null) return;
        if (equipSlots == null)
        {
            equipSlots = (ModuleEquipSlots)typeof(GearDetailsWindow).GetField("equipSlots", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(window);
        }

        float x = 10;
        float y = 10;
        if (GUI.Button(new Rect(x, y, 100, 30), "Copy Grid"))
        {
            CopyGridToClipboard();
        }
        if (GUI.Button(new Rect(x + 110, y, 100, 30), "Paste Code"))
        {
            PasteCodeFromClipboard();
        }
    }

    void CopyGridToClipboard()
    {
        var upgrades = GetCurrentEquippedUpgrades();
        var code = upgrades.ToString();
        Clipboard_SetText(code);
    }

    void PasteCodeFromClipboard()
    {
        string code = Clipboard_GetText();
        if (string.IsNullOrEmpty(code)) return;
        var equippedUpgrades = new EquippedUpgrades(code);
        ApplyUpgradesToGrid(equippedUpgrades.Upgrades);
    }

    EquippedUpgrades GetCurrentEquippedUpgrades()
    {
        var gearId = window.UpgradablePrefab.Info.ID;
        var list = new List<EquippedUpgrade>();
        var hexMap = equipSlots.HexMap;
        var upgradePositions = new Dictionary<UpgradeInstance, List<(int, int)>>();

        for (int i = 0; i < hexMap.Height; i++)
        {
            for (int j = 0; j < hexMap.Width; j++)
            {
                var node = hexMap[j, i];
                if (node.enabled && node.upgrade != null)
                {
                    if (!upgradePositions.ContainsKey(node.upgrade))
                        upgradePositions[node.upgrade] = new List<(int, int)>();
                    upgradePositions[node.upgrade].Add((j, i));
                }
            }
        }

        foreach (var kvp in upgradePositions)
        {
            var upgrade = kvp.Key;
            var positions = kvp.Value;
            byte rotation = upgrade.GetRotation(window.UpgradablePrefab);
            int id = upgrade.Upgrade.ID;
            int minX = int.MaxValue, minY = int.MaxValue;
            float sumX = 0, sumY = 0;
            foreach (var pos in positions)
            {
                sumX += pos.Item1;
                sumY += pos.Item2;
                if (pos.Item1 < minX || (pos.Item1 == minX && pos.Item2 < minY))
                {
                    minX = pos.Item1;
                    minY = pos.Item2;
                }
            }
            list.Add(new EquippedUpgrade(minX, minY, rotation, id));
        }
        return new EquippedUpgrades(gearId, list);
    }

    void ApplyUpgradesToGrid(List<EquippedUpgrade> upgrades)
    {
        var hexMap = equipSlots.HexMap;
        for (int i = 0; i < hexMap.Height; i++)
        {
            for (int j = 0; j < hexMap.Width; j++)
            {
                var node = hexMap[j, i];
                if (node.upgrade != null)
                {
                    equipSlots.Unequip(window.UpgradablePrefab, node.upgrade);
                }
            }
        }

        var groupedUpgrades = upgrades.GroupBy(up => up.ID).ToDictionary(g => g.Key, g => g.ToList());
        var availableInstances = GetAvailableUpgradeInstances();

        foreach (var kvp in groupedUpgrades)
        {
            var id = kvp.Key;
            var upgradeList = kvp.Value;
            if (!availableInstances.ContainsKey(id))
            {
                continue;
            }
            var instances = availableInstances[id];
            var usedInstances = new List<UpgradeInstance>();
            foreach (var up in upgradeList)
            {
                if (instances.Count > 0)
                {
                    var instance = instances[0];
                    instances.RemoveAt(0);
                   EquipUpgrade(up, instance, !usedInstances.Contains(instance));
                    usedInstances.Add(instance);
                }
                else
                {
                    break;
                }
            }
        }
    }

    Dictionary<int, List<UpgradeInstance>> GetAvailableUpgradeInstances()
    {
        var dict = new Dictionary<int, List<UpgradeInstance>>();
        var upgradable = window.UpgradablePrefab;

        var enumerator = new PlayerData.UpgradeEnumerator(upgradable);
        while (enumerator.MoveNext())
        {
            var id = enumerator.Upgrade.Upgrade.ID;
            if (!dict.ContainsKey(id)) dict[id] = new List<UpgradeInstance>();
            dict[id].Add(enumerator.Upgrade);
        }

        enumerator = new PlayerData.UpgradeEnumerator(Global.Instance);
        while (enumerator.MoveNext())
        {
            var id = enumerator.Upgrade.Upgrade.ID;
            if (!dict.ContainsKey(id)) dict[id] = new List<UpgradeInstance>();
            dict[id].Add(enumerator.Upgrade);
        }

        return dict;
    }

    UpgradeInstance FindUpgradeInstance(int id)
    {
        var upgradable = window.UpgradablePrefab;
        PlayerData.UpgradeEnumerator enumerator = new PlayerData.UpgradeEnumerator(upgradable);
        while (enumerator.MoveNext())
        {
            if (enumerator.Upgrade.Upgrade.ID == id)
            {
                return enumerator.Upgrade;
            }
        }
        if (upgradable is Character)
        {
            enumerator = new PlayerData.UpgradeEnumerator(Global.Instance);
            while (enumerator.MoveNext())
            {
                if (enumerator.Upgrade.Upgrade.ID == id)
                {
                    return enumerator.Upgrade;
                }
            }
        }
        return null;
    }

    void EquipUpgrade(EquippedUpgrade up, UpgradeInstance upgrade, bool checkOffset = true)
    {
        var hexMap = upgrade.GetPattern().GetModifiedMap(up.Rotation);
        int minK = int.MaxValue, minL = int.MaxValue;
        for (int l = 0; l < hexMap.Height; l++)
        {
            for (int k = 0; k < hexMap.Width; k++)
            {
                if (hexMap[k, l].enabled)
                {
                    if (k < minK || (k == minK && l < minL))
                    {
                        minK = k;
                        minL = l;
                    }
                }
            }
        }
        if (minK == int.MaxValue) minK = 0;
        if (minL == int.MaxValue) minL = 0;
        int offsetX = up.X - minK;
        int offsetY = up.Y - minL;
        int gxMin = int.MaxValue, gxMax = int.MinValue;
        int gyMin = int.MaxValue, gyMax = int.MinValue;
        for (int l = 0; l < hexMap.Height; l++)
        {
            for (int k = 0; k < hexMap.Width; k++)
            {
                if (hexMap[k, l].enabled)
                {
                    int gx = offsetX + k;
                    int gy = offsetY + l;
                    if (gx < gxMin) gxMin = gx;
                    if (gx > gxMax) gxMax = gx;
                    if (gy < gyMin) gyMin = gy;
                    if (gy > gyMax) gyMax = gy;
                }
            }
        }
        int adjustX = 0;
        int adjustY = 0;
        if (gxMin < 0) adjustX = -gxMin;
        if (gxMax >= equipSlots.Width) adjustX -= Mathf.Max(0, gxMax - equipSlots.Width + 1);
        if (gyMin < 0) adjustY = -gyMin;
        if (gyMax >= equipSlots.Height) adjustY -= Mathf.Max(0, gyMax - equipSlots.Height + 1);
        offsetX += adjustX;
        offsetY += adjustY;
        equipSlots.Unequip(window.UpgradablePrefab, upgrade);
        bool flag = equipSlots.Unequip(window.UpgradablePrefab, upgrade);
        bool result = equipSlots.EquipModule(window.UpgradablePrefab, upgrade, offsetX, offsetY, up.Rotation);
        if (!(result || flag))
        {
            equipSlots.Unequip(window.UpgradablePrefab, upgrade);
            flag = equipSlots.Unequip(window.UpgradablePrefab, upgrade);
            result = equipSlots.EquipModule(window.UpgradablePrefab, upgrade, offsetX, offsetY - 1, up.Rotation);
            if (!(result || flag))
            {
            }
            else
            {
                offsetY -= 1;
            }
        }

        if (checkOffset && result)
        {
            var actualMin = GetActualUpgradePosition(upgrade);
            if (actualMin.HasValue)
            {
                int actualX = actualMin.Value.Item1;
                int actualY = actualMin.Value.Item2;
                int deltaX = up.X - actualX;
                int deltaY = up.Y - actualY;
                if (deltaX != 0 || deltaY != 0)
                {
                    equipSlots.Unequip(window.UpgradablePrefab, upgrade);
                    bool adjustedResult = equipSlots.EquipModule(window.UpgradablePrefab, upgrade, offsetX + deltaX, offsetY + deltaY, up.Rotation);
                    if (!adjustedResult)
                    {
                    }
                }
            }
        }
    }

    (int, int)? GetActualUpgradePosition(UpgradeInstance upgrade)
    {
        var hexMap = equipSlots.HexMap;
        int? minX = null, minY = null;
        for (int i = 0; i < hexMap.Height; i++)
        {
            for (int j = 0; j < hexMap.Width; j++)
            {
                var node = hexMap[j, i];
                if (node.enabled && node.upgrade == upgrade)
                {
                    if (minX == null || (j < minX) || (j == minX && i < minY))
                    {
                        minX = j;
                        minY = i;
                    }
                }
            }
        }
        if (minX.HasValue) return (minX.Value, minY.Value);
        return null;
    }
}
