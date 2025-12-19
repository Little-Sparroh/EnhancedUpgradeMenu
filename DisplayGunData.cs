using BepInEx.Logging;
using Pigeon;
using Pigeon.Math;
using Pigeon.Movement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Text;

public class DisplayGunDataMod
{
    internal ManualLogSource Logger;

    private bool enableGearDetailsStatsWindow;

    private IWeapon currentWeapon;
    private GearDetailsWindow currentWindow;
    private bool showStatsWindow = false;
    private Rect windowRect = new Rect(20, 20, 350, 400);
    private Vector2 scrollPosition = Vector2.zero;
    private bool statsWindowVisible = true;

    private readonly Color sky = new Color(0.529f, 0.808f, 0.922f);
    private readonly Color orchid = new Color(0.855f, 0.439f, 0.839f);
    private readonly Color rose = new Color(0.8901960784313725f, 0.1411764705882353f, 0.16862745098039217f);
    private readonly Color macaroon = new Color(0.9764705882352941f, 0.8784313725490196f, 0.4627450980392157f);
    private readonly Color shamrock = new Color(0.011764705882352941f, 0.6745098039215687f, 0.07450980392156863f);

    private const int NUM_STAT_LINES = 25;
    private const float UpdateInterval = 0.5f;
    private float updateTimer = 0f;
    private List<string> statLines = new List<string>();

    public DisplayGunDataMod(ManualLogSource logger, bool enable)
    {
        Logger = logger;
        enableGearDetailsStatsWindow = enable;
        Logger.LogInfo("DisplayGunData integrated successfully.");
    }

    public void SetEnable(bool enable)
    {
        enableGearDetailsStatsWindow = enable;
    }



    public void Update()
    {
        if (!enableGearDetailsStatsWindow) return;

        if (Menu.Instance != null && LevelData.CanModifyGear)
        {
            var topWindow = Menu.Instance.WindowSystem.GetTop();
            if (topWindow is GearDetailsWindow gearWindow && !gearWindow.InSkinMode)
            {
                if (gearWindow.UpgradablePrefab is IWeapon weapon)
                {
                    if (currentWeapon != weapon)
                    {
                        currentWeapon = weapon;
                        currentWindow = gearWindow;
                        showStatsWindow = true;
                        updateTimer = 0;
                        UpdateGearDetailsStats();
                    }
                }
                else
                {
                    currentWeapon = null;
                    currentWindow = null;
                    showStatsWindow = false;
                }
            }
            else
            {
                currentWeapon = null;
                currentWindow = null;
                showStatsWindow = false;
            }
        }
        else
        {
            currentWeapon = null;
            showStatsWindow = false;
        }

        if (showStatsWindow && currentWeapon != null)
        {
            updateTimer += Time.deltaTime;
            if (updateTimer >= UpdateInterval)
            {
                updateTimer = 0f;
                UpdateGearDetailsStats();
            }
        }
    }

    private void UpdateGearDetailsStats()
    {
        if (currentWeapon == null) return;

        GunData originalGunData = currentWeapon.GunData;

        UpgradeStatChanges statChanges = new UpgradeStatChanges();

        if (currentWindow != null)
        {
            var equipSlotsField = typeof(GearDetailsWindow).GetField("equipSlots", BindingFlags.NonPublic | BindingFlags.Instance);
            if (equipSlotsField != null)
            {
                object equipSlotsObj = equipSlotsField.GetValue(currentWindow);
                if (equipSlotsObj != null)
                {
                }
                if (equipSlotsObj is ModuleEquipSlots equipSlots)
                {
                    var hexMap = equipSlots.HexMap;
                    if (hexMap != null)
                    {
                        var upgrades = new List<UpgradeInstance>();
                        for (int x = 0; x < hexMap.Width; x++)
                        {
                            for (int y = 0; y < hexMap.Height; y++)
                            {
                                var node = hexMap[x, y];
                                if (node.enabled && node.upgrade != null && !upgrades.Contains(node.upgrade))
                                {
                                    upgrades.Add(node.upgrade);
                                }
                            }
                        }
                        foreach (UpgradeInstance upgrade in upgrades)
                        {
                            try
                            {
                                ((GearUpgrade)upgrade.Upgrade).Apply((IGear)currentWeapon, upgrade);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogWarning($"Error applying upgrade {upgrade.Upgrade.Name}: {ex}");
                            }
                        }
                    }
                }
                else
                {
                    Logger.LogInfo("Cast to ModuleEquipSlots failed");
                }
            }
        }

        ref GunData data = ref currentWeapon.GunData;

        Dictionary<string, StatInfo> primaryStats = new Dictionary<string, StatInfo>();
        var primaryEnum = currentWeapon.EnumeratePrimaryStats(statChanges);
        while (primaryEnum.MoveNext())
        {
            primaryStats[primaryEnum.Current.name] = primaryEnum.Current;
        }

        Dictionary<string, StatInfo> secondaryStats = new Dictionary<string, StatInfo>();
        var secondaryEnum = currentWeapon.EnumerateSecondaryStats(statChanges);
        while (secondaryEnum.MoveNext())
        {
            if (secondaryEnum.Current.name != "Aim Zoom")
            {
                secondaryStats[secondaryEnum.Current.name] = secondaryEnum.Current;
            }
        }

        statLines.Clear();
        statLines.Add("Weapon Stats Preview:");

        if (primaryStats.TryGetValue("Damage Type", out var dmgType))
        {
            statLines.Add($"{dmgType.name}: <color=#{ColorUtility.ToHtmlStringRGB(dmgType.color)}>{dmgType.value}</color>");
        }

        AddStatFromDict(ref statLines, secondaryStats, "Damage");
        AddStatFromDict(ref statLines, primaryStats, "Damage Type");
        AddStatFromDict(ref statLines, secondaryStats, "Fire Rate");
        statLines.Add($"Burst Size: <color=#{ColorUtility.ToHtmlStringRGB(macaroon)}>{data.burstSize}</color>");
        statLines.Add($"Burst Interval: <color=#{ColorUtility.ToHtmlStringRGB(macaroon)}>{data.burstFireInterval.ToString("F2")}</color>");
        AddStatFromDict(ref statLines, secondaryStats, "Ammo Capacity");
        statLines.Add($"Ammo Capacity: <color=#{ColorUtility.ToHtmlStringRGB(sky)}>{data.ammoCapacity}</color>");
        AddStatFromDict(ref statLines, secondaryStats, "Reload Duration");
        AddStatFromDict(ref statLines, secondaryStats, "Charge Duration");
        statLines.Add($"Explosion Size: <color=#{ColorUtility.ToHtmlStringRGB(orchid)}>{Mathf.Round(data.hitForce)}</color>");
        AddStatFromDict(ref statLines, secondaryStats, "Range");
        statLines.Add($"Recoil: <color=#{ColorUtility.ToHtmlStringRGB(shamrock)}>X({Mathf.Round(data.recoilData.recoilX.x)}, {Mathf.Round(data.recoilData.recoilX.y)}) Y({Mathf.Round(data.recoilData.recoilY.x)}, {Mathf.Round(data.recoilData.recoilY.y)})</color>");
        statLines.Add($"Spread: <color=#{ColorUtility.ToHtmlStringRGB(shamrock)}>Size({Mathf.Round(data.spreadData.spreadSize.x)}, {Mathf.Round(data.spreadData.spreadSize.y)})</color>");
        statLines.Add($"Fire Mode: <color=#{ColorUtility.ToHtmlStringRGB(macaroon)}>{(data.automatic == 1 ? "Automatic" : "Semi Automatic")}</color>");

        BulletData shotData = default(BulletData);
        try
        {
            Vector3 dummyPos = Vector3.zero;
            Quaternion dummyRot = Quaternion.identity;
            shotData = data.GetBulletData(ref dummyPos, ref dummyRot);
            statLines.Add($"Bullet Speed: <color=#FFFFFF>{shotData.speed:F1}</color>");
            statLines.Add($"Bullet Force: <color=#FFFFFF>{shotData.force:F1}</color>");
            statLines.Add($"Bullet Damage: <color=#FFFFFF>{shotData.damage:F1}</color>");
            statLines.Add($"Bullet Range: <color=#FFFFFF>{shotData.range.falloffEndDistance:F0}</color>");
            statLines.Add($"Bullet Gravity: <color=#FFFFFF>{shotData.gravity:F2}</color>");
            if (shotData.damageEffect > EffectType.Normal)
            {
                StatusEffectData effect = Global.GetEffect(shotData.damageEffect);
                statLines.Add($"Bullet Effect: <color=#{ColorUtility.ToHtmlStringRGB(effect.iconColor)}>{effect.EffectName} x{shotData.damageEffectAmount}</color>");
            }
        }
        catch (System.Exception ex)
        {
            Logger.LogWarning($"Error getting BulletData: {ex.Message}");
        }

        currentWeapon.GunData = originalGunData;
    }

    private void AddStatFromDict(ref List<string> lines, Dictionary<string, StatInfo> stats, string statName)
    {
        if (stats.TryGetValue(statName, out var stat))
        {
            string label = stat.name + ":";
            string value = stat.value;
            if (statName == "Fire Rate" && float.TryParse(stat.value, out float rpm))
            {
                value = (rpm / 60f).ToString("F1") + " /s";
            }
            Color valueColor = (statName == "Damage Type") ? stat.color : GetStatValueColor(statName);
            lines.Add($"{label} <color=#{ColorUtility.ToHtmlStringRGB(valueColor)}>{value}</color>");
        }
    }

    private Color GetStatValueColor(string statName)
    {
        return statName switch
        {
            "Damage" => rose,
            "Fire Rate" => macaroon,
            "Ammo Capacity" => sky,
            "Reload Duration" => sky,
            "Charge Duration" => sky,
            "Range" => shamrock,
            _ => Color.white
        };
    }

    public void OnGUI()
    {
        if (!enableGearDetailsStatsWindow || !showStatsWindow || currentWeapon == null) return;

        float buttonWidth = 150f;
        float buttonHeight = 30f;
        Rect toggleRect = new Rect(Screen.width - buttonWidth - 20, Screen.height - buttonHeight - 20, buttonWidth, buttonHeight);

        if (GUI.Button(toggleRect, statsWindowVisible ? "Hide Gun Stats" : "Show Gun Stats"))
        {
            statsWindowVisible = !statsWindowVisible;
        }

        if (statsWindowVisible)
        {
            windowRect = GUI.Window(0, windowRect, DoStatsWindow, "Gun Stats Preview");
        }
    }

    private void DoStatsWindow(int windowID)
    {
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        foreach (var line in statLines)
        {
            GUILayout.Label(line, new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true });
        }

        GUILayout.EndScrollView();

        GUI.DragWindow(new Rect(0, 0, 10000, 20));
    }


}
