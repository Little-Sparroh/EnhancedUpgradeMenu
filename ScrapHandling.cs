using System;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public static class ScrapHandlingMod
{

    private static bool isScrapping = false;
    public static bool IsScrapping => isScrapping;
    private static bool wasScrappingSkins = false;
    private const float HOLD_DURATION = 1.0f;
    private const int BATCH_SIZE = 1000000;
    private const float BATCH_INTERVAL = 0f;
    private const float SPECIAL_REWARD_CHANCE = 0.02f;

public static Key currentTrashKey;
    public static System.Action ScrapMarkedAction;
    public static System.Action ScrapNonFavoriteAction;
    public static UnityEngine.Sprite starSprite;

    public static IEnumerator ScrapMarkedUpgrades()
    {
        bool setupSuccess = false;
        bool inSkinMode = false;
        bool isSkinsMode = false;
        List<UpgradeInstance> toScrap = null;
        var totalResources = new System.Collections.Generic.Dictionary<PlayerResource, int>();
        int scrappedCount = 0;

        try
        {
            if (isScrapping)
            {
                SparrohPlugin.Logger.LogWarning("ScrapMarkedUpgrades: Already scrapping, aborting.");
                yield break;
            }

            isScrapping = true;
            SparrohPlugin.Logger.LogInfo("Starting ScrapMarkedUpgrades operation.");

            var gearWindow = UnityEngine.Object.FindObjectOfType<GearDetailsWindow>();
            if (gearWindow == null)
            {
                SparrohPlugin.Logger.LogError("ScrapMarkedUpgrades: GearDetailsWindow not found.");
                isScrapping = false;
                yield break;
            }

            var gear = gearWindow.UpgradablePrefab;
            if (gear == null)
            {
                SparrohPlugin.Logger.LogError("ScrapMarkedUpgrades: UpgradablePrefab is null.");
                isScrapping = false;
                yield break;
            }

            var inSkinModeField = AccessTools.Field(typeof(GearDetailsWindow), "inSkinMode");
            inSkinMode = (bool)inSkinModeField.GetValue(gearWindow);
            isSkinsMode = inSkinMode;

            IEnumerable<UpgradeInfo> all = inSkinMode ? PlayerData.GetAllSkins(gear) : PlayerData.GetAllUpgrades(gear);

            int totalCount = 0;
            foreach (var info in all)
            {
                if (info?.Instances == null) continue;
                totalCount += info.Instances.Count;
            }

            toScrap = new List<UpgradeInstance>(totalCount);
            foreach (var info in all)
            {
                if (info?.Instances == null) continue;
                foreach (var inst in info.Instances)
                {
                    if (inst != null && IsTrashMarked(inst))
                    {
                        toScrap.Add(inst);
                    }
                }
            }

            if (toScrap.Count == 0)
            {
                SparrohPlugin.Logger.LogInfo("ScrapMarkedUpgrades: No marked upgrades found.");
                isScrapping = false;
                yield break;
            }

            SparrohPlugin.Logger.LogInfo($"ScrapMarkedUpgrades: Processing {toScrap.Count} upgrades.");

            var grouped = toScrap.GroupBy(inst => inst.Upgrade.Rarity).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var group in grouped)
            {
                var rarityKey = group.Key;
                var count = group.Value.Count;
                ref RarityData rarity = ref Global.GetRarity(rarityKey);
                int scrip = rarity.upgradeScripCost / 6;
                totalResources.TryGetValue(Global.Instance.ScripResource, out int existingScrip);
                totalResources[Global.Instance.ScripResource] = existingScrip + scrip * count;

                if (!isSkinsMode)
                {
                    totalResources.TryGetValue(rarity.scrapResource, out int existingScrap);
                    totalResources[rarity.scrapResource] = existingScrap + 2 * count;

                    if (PlayerResource.TryGetResource("strange_comp", out var strangeRes))
                    {
                        int strangeCount = Mathf.FloorToInt(count * SPECIAL_REWARD_CHANCE);
                        totalResources.TryGetValue(strangeRes, out int existingStrange);
                        totalResources[strangeRes] = existingStrange + strangeCount;
                    }
                }
                else
                {
                    if (PlayerResource.TryGetResource("oyster", out var oysterRes))
                    {
                        int oysterCount = Mathf.FloorToInt(count * SPECIAL_REWARD_CHANCE);
                        totalResources.TryGetValue(oysterRes, out int existingOyster);
                        totalResources[oysterRes] = existingOyster + oysterCount;
                    }
                }
            }

            setupSuccess = true;
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"ScrapMarkedUpgrades: Setup failed: {ex.Message}");
            isScrapping = false;
            yield break;
        }

        if (setupSuccess && toScrap != null)
        {
            for (int i = 0; i < toScrap.Count; i += BATCH_SIZE)
            {
                int batchEnd = Mathf.Min(i + BATCH_SIZE, toScrap.Count);
                for (int j = i; j < batchEnd; j++)
                {
                    var inst = toScrap[j];
                    if (inst == null || inst.Upgrade == null)
                    {
                        continue;
                    }

                    inst.Destroy();
                    scrappedCount++;
                }
                if (BATCH_INTERVAL > 0f)
                {
                    yield return new WaitForSeconds(BATCH_INTERVAL);
                }
            }
        }

        try
        {
            foreach (var kvp in totalResources)
            {
                PlayerData.Instance.AddResource(kvp.Key, kvp.Value);
            }
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"ScrapMarkedUpgrades: Failed to add resources: {ex.Message}");
        }

        if (scrappedCount > 0)
        {
            try
            {
                wasScrappingSkins = isSkinsMode;
                RefreshOpenWindows();
            }
            catch (Exception ex)
            {
                SparrohPlugin.Logger.LogError($"ScrapMarkedUpgrades: Failed to refresh windows: {ex.Message}");
            }
        }

        isScrapping = false;
    }

    public static bool IsFavorite(UpgradeInstance instance)
    {
        if (instance == null) return false;
        var flagsField = AccessTools.Field(typeof(UpgradeInstance), "flags");
        byte flags = (byte)flagsField.GetValue(instance);
        return (flags & 0x01) != 0;
    }

    public static bool IsTrashMarked(UpgradeInstance instance)
    {
        if (instance == null) return false;
        var flagsField = AccessTools.Field(typeof(UpgradeInstance), "flags");
        byte flags = (byte)flagsField.GetValue(instance);
        return (flags & 0x30) != 0;
    }

    public static void SetTrashMark(UpgradeInstance instance, bool marked)
    {
        if (instance == null) return;
        var flagsField = AccessTools.Field(typeof(UpgradeInstance), "flags");
        byte flags = (byte)flagsField.GetValue(instance);
        if (marked)
        {
            flags |= 0x30;
            flags &= 0xFE;
        }
        else
        {
            flags &= 0xCF;
        }
        flagsField.SetValue(instance, flags);
    }

    public static void SetFavorite(UpgradeInstance instance, bool favorite)
    {
        if (instance == null) return;
        var flagsField = AccessTools.Field(typeof(UpgradeInstance), "flags");
        byte flags = (byte)flagsField.GetValue(instance);
        if (favorite)
        {
            flags |= 0x01;
            flags &= 0xFD;
        }
        else
        {
            flags &= 0xFE;
        }
        flagsField.SetValue(instance, flags);
    }

public static void TryScrapMarkedUpgrades(MonoBehaviour owner)
    {
        var gearWindow = UnityEngine.Object.FindObjectOfType<GearDetailsWindow>();
        if (gearWindow == null) return;
        var gear = gearWindow.UpgradablePrefab;
        if (gear == null) return;
        var inSkinModeField = AccessTools.Field(typeof(GearDetailsWindow), "inSkinMode");
        bool inSkinMode = (bool)inSkinModeField.GetValue(gearWindow);
        var all = inSkinMode ? PlayerData.GetAllSkins(gear) : PlayerData.GetAllUpgrades(gear);
        bool hasMarked = false;
        foreach (var info in all)
        {
            if (info?.Instances == null) continue;
            if (info.Instances.Any(inst => inst != null && ScrapHandlingMod.IsTrashMarked(inst)))
            {
                hasMarked = true;
                break;
            }
        }
        if (hasMarked)
        {
            owner.StartCoroutine(ScrapMarkedUpgrades());
        }
    }

    public static IEnumerator ScrapNonFavoriteUpgrades()
    {
        bool setupSuccess = false;
        bool inSkinMode = false;
        bool isSkinsMode = false;
        List<UpgradeInstance> toScrap = null;
        var totalResources = new System.Collections.Generic.Dictionary<PlayerResource, int>();
        int scrappedCount = 0;

        try
        {
            if (isScrapping)
            {
                SparrohPlugin.Logger.LogWarning("ScrapNonFavoriteUpgrades: Already scrapping, aborting.");
                yield break;
            }

            isScrapping = true;
            SparrohPlugin.Logger.LogInfo("Starting ScrapNonFavoriteUpgrades operation.");

            var gearWindow = UnityEngine.Object.FindObjectOfType<GearDetailsWindow>();
            if (gearWindow == null)
            {
                SparrohPlugin.Logger.LogError("ScrapNonFavoriteUpgrades: GearDetailsWindow not found.");
                isScrapping = false;
                yield break;
            }

            var gear = gearWindow.UpgradablePrefab;
            if (gear == null)
            {
                SparrohPlugin.Logger.LogError("ScrapNonFavoriteUpgrades: UpgradablePrefab is null.");
                isScrapping = false;
                yield break;
            }

            var inSkinModeField = AccessTools.Field(typeof(GearDetailsWindow), "inSkinMode");
            inSkinMode = (bool)inSkinModeField.GetValue(gearWindow);
            isSkinsMode = inSkinMode;

            IEnumerable<UpgradeInfo> all = inSkinMode ? PlayerData.GetAllSkins(gear) : PlayerData.GetAllUpgrades(gear);

            int totalCount = 0;
            foreach (var info in all)
            {
                if (info?.Instances == null) continue;
                totalCount += info.Instances.Count;
            }

            toScrap = new List<UpgradeInstance>(totalCount);
            foreach (var info in all)
            {
                if (info?.Instances == null) continue;
                foreach (var inst in info.Instances)
                {
                    if (inst != null && !IsFavorite(inst))
                    {
                        toScrap.Add(inst);
                    }
                }
            }

            if (toScrap.Count == 0)
            {
                SparrohPlugin.Logger.LogInfo("ScrapNonFavoriteUpgrades: No non-favorite upgrades found.");
                isScrapping = false;
                yield break;
            }

            SparrohPlugin.Logger.LogInfo($"ScrapNonFavoriteUpgrades: Processing {toScrap.Count} upgrades.");

            var grouped = toScrap.GroupBy(inst => inst.Upgrade.Rarity).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var group in grouped)
            {
                var rarityKey = group.Key;
                var count = group.Value.Count;
                ref RarityData rarity = ref Global.GetRarity(rarityKey);
                int scrip = rarity.upgradeScripCost / 6;
                totalResources.TryGetValue(Global.Instance.ScripResource, out int existingScrip);
                totalResources[Global.Instance.ScripResource] = existingScrip + scrip * count;

                if (!isSkinsMode)
                {
                    totalResources.TryGetValue(rarity.scrapResource, out int existingScrap);
                    totalResources[rarity.scrapResource] = existingScrap + 2 * count;

                    if (PlayerResource.TryGetResource("strange_comp", out var strangeRes))
                    {
                        int strangeCount = Mathf.FloorToInt(count * SPECIAL_REWARD_CHANCE);
                        totalResources.TryGetValue(strangeRes, out int existingStrange);
                        totalResources[strangeRes] = existingStrange + strangeCount;
                    }
                }
                else
                {
                    if (PlayerResource.TryGetResource("oyster", out var oysterRes))
                    {
                        int oysterCount = Mathf.FloorToInt(count * SPECIAL_REWARD_CHANCE);
                        totalResources.TryGetValue(oysterRes, out int existingOyster);
                        totalResources[oysterRes] = existingOyster + oysterCount;
                    }
                }
            }

            setupSuccess = true;
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"ScrapNonFavoriteUpgrades: Setup failed: {ex.Message}");
            isScrapping = false;
            yield break;
        }

        if (setupSuccess && toScrap != null)
        {
            for (int i = 0; i < toScrap.Count; i += BATCH_SIZE)
            {
                int batchEnd = Mathf.Min(i + BATCH_SIZE, toScrap.Count);
                for (int j = i; j < batchEnd; j++)
                {
                    var inst = toScrap[j];
                    if (inst == null || inst.Upgrade == null)
                    {
                        continue;
                    }

                    inst.Destroy();
                    scrappedCount++;
                }
                if (BATCH_INTERVAL > 0f)
                {
                    yield return new WaitForSeconds(BATCH_INTERVAL);
                }
            }
        }

        try
        {
            foreach (var kvp in totalResources)
            {
                PlayerData.Instance.AddResource(kvp.Key, kvp.Value);
            }
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"ScrapNonFavoriteUpgrades: Failed to add resources: {ex.Message}");
        }

        if (scrappedCount > 0)
        {
            try
            {
                wasScrappingSkins = isSkinsMode;
                RefreshOpenWindows();
            }
            catch (Exception ex)
            {
                SparrohPlugin.Logger.LogError($"ScrapNonFavoriteUpgrades: Failed to refresh windows: {ex.Message}");
            }
        }

        isScrapping = false;
    }

    public static void TryScrapNonFavoriteUpgrades(MonoBehaviour owner)
    {
        var gearWindow = UnityEngine.Object.FindObjectOfType<GearDetailsWindow>();
        if (gearWindow == null) return;
        var gear = gearWindow.UpgradablePrefab;
        if (gear == null) return;
        var inSkinModeField = AccessTools.Field(typeof(GearDetailsWindow), "inSkinMode");
        bool inSkinMode = (bool)inSkinModeField.GetValue(gearWindow);
        var all = inSkinMode ? PlayerData.GetAllSkins(gear) : PlayerData.GetAllUpgrades(gear);
        bool hasNonFavorite = false;
        foreach (var info in all)
        {
            if (info?.Instances == null) continue;
            if (info.Instances.Any(inst => inst != null && !ScrapHandlingMod.IsFavorite(inst)))
            {
                hasNonFavorite = true;
                break;
            }
        }
        if (hasNonFavorite)
        {
            owner.StartCoroutine(ScrapNonFavoriteUpgrades());
        }
    }

    private static void RefreshOpenWindows()
    {
        if (Menu.Instance != null && Menu.Instance.IsOpen)
        {
            var top = Menu.Instance.WindowSystem.GetTop();
            if (top != null)
            {
                top.OnOpen(Menu.Instance.WindowSystem);
                if (wasScrappingSkins)
                {
                    GearDetailsWindow gearWindow = top as GearDetailsWindow;
                    if (gearWindow != null)
                    {
                        var inSkinModeField = AccessTools.Field(typeof(GearDetailsWindow), "inSkinMode");
                        inSkinModeField.SetValue(gearWindow, true);
                    }
                    wasScrappingSkins = false;
                }
            }
        }
    }
}

public class Patches
{

    private static void AddScrapButton(GearDetailsWindow window)
    {

        var oldButtonMarked = window.transform.Find("ModScrapButtonMarked");
        if (oldButtonMarked != null)
        {
            UnityEngine.Object.DestroyImmediate(oldButtonMarked.gameObject);
        }
        var oldButtonNonFavorite = window.transform.Find("ModScrapButtonNonFavorite");
        if (oldButtonNonFavorite != null)
        {
            UnityEngine.Object.DestroyImmediate(oldButtonNonFavorite.gameObject);
        }

        var parentRect = window.transform.GetComponent<RectTransform>();

        var markedButtonGO = new GameObject("ModScrapButtonMarked");
        markedButtonGO.transform.SetParent(window.transform, false);
        var rectMarked = markedButtonGO.AddComponent<RectTransform>();
        rectMarked.sizeDelta = new Vector2(200, 50);
        rectMarked.anchorMin = new Vector2(1, 0);
        rectMarked.anchorMax = new Vector2(1, 0);
        rectMarked.pivot = new Vector2(1, 0);
        rectMarked.anchoredPosition = new Vector2(-parentRect.rect.width * 0.25f, 10);

        var imageMarked = markedButtonGO.AddComponent<Image>();
        imageMarked.color = Color.gray;
        imageMarked.raycastTarget = true;

        var buttonMarked = markedButtonGO.AddComponent<Button>();
        buttonMarked.transition = Button.Transition.ColorTint;
        var colorsMarked = buttonMarked.colors;
        colorsMarked.normalColor = Color.gray;
        colorsMarked.highlightedColor = Color.white;
        colorsMarked.pressedColor = Color.blue;
        buttonMarked.colors = colorsMarked;

        var textChildMarked = new GameObject("Text");
        textChildMarked.transform.SetParent(markedButtonGO.transform, false);
        var textRectMarked = textChildMarked.AddComponent<RectTransform>();
        textRectMarked.sizeDelta = rectMarked.sizeDelta;
        var tmproMarked = textChildMarked.AddComponent<TMPro.TextMeshProUGUI>();
        tmproMarked.text = "Scrap Marked";
        tmproMarked.alignment = TMPro.TextAlignmentOptions.Center;
        tmproMarked.color = Color.white;
        tmproMarked.fontSize = 24;

        var handlerMarked = markedButtonGO.AddComponent<HoldButtonHandler>();
        handlerMarked.onHoldComplete = ScrapHandlingMod.ScrapMarkedAction;

        var nonFavButtonGO = new GameObject("ModScrapButtonNonFavorite");
        nonFavButtonGO.transform.SetParent(window.transform, false);
        var rectNonFav = nonFavButtonGO.AddComponent<RectTransform>();
        rectNonFav.sizeDelta = new Vector2(250, 50);
        rectNonFav.anchorMin = new Vector2(1, 0);
        rectNonFav.anchorMax = new Vector2(1, 0);
        rectNonFav.pivot = new Vector2(1, 0);
        rectNonFav.anchoredPosition = new Vector2(rectMarked.anchoredPosition.x + 210, 50);

        var imageNonFav = nonFavButtonGO.AddComponent<Image>();
        imageNonFav.color = Color.red;
        imageNonFav.raycastTarget = true;

        var buttonNonFav = nonFavButtonGO.AddComponent<Button>();
        buttonNonFav.transition = Button.Transition.ColorTint;
        var colorsNonFav = buttonNonFav.colors;
        colorsNonFav.normalColor = Color.red;
        colorsNonFav.highlightedColor = Color.white;
        colorsNonFav.pressedColor = Color.blue;
        buttonNonFav.colors = colorsNonFav;

        var textChildNonFav = new GameObject("Text");
        textChildNonFav.transform.SetParent(nonFavButtonGO.transform, false);
        var textRectNonFav = textChildNonFav.AddComponent<RectTransform>();
        textRectNonFav.sizeDelta = rectNonFav.sizeDelta;
        var tmproNonFav = textChildNonFav.AddComponent<TMPro.TextMeshProUGUI>();
        tmproNonFav.text = "Scrap All Non-Favorite";
        tmproNonFav.alignment = TMPro.TextAlignmentOptions.Center;
        tmproNonFav.color = Color.white;
        tmproNonFav.fontSize = 20;

        var handlerNonFav = nonFavButtonGO.AddComponent<HoldButtonHandler>();
        handlerNonFav.onHoldComplete = ScrapHandlingMod.ScrapNonFavoriteAction;
    }

    public static void UpdateFavoriteIconPostfix(GearUpgradeUI __instance)
    {
        if (ScrapHandlingMod.IsScrapping) return;

        var favoriteIconField = AccessTools.Field(typeof(GearUpgradeUI), "favoriteIcon");
        var favoriteIcon = (Image)favoriteIconField.GetValue(__instance);
        if (favoriteIcon != null && __instance.Upgrade != null)
        {
            bool favorite = ScrapHandlingMod.IsFavorite(__instance.Upgrade);
            bool trash = ScrapHandlingMod.IsTrashMarked(__instance.Upgrade);
            if (favorite)
            {
                ScrapHandlingMod.starSprite = favoriteIcon.sprite;
                favoriteIcon.gameObject.SetActive(true);
                favoriteIcon.color = Color.white;
            }
            else if (trash)
            {
                favoriteIcon.sprite = ScrapHandlingMod.starSprite ?? Resources.Load<Sprite>("favorite star");
                favoriteIcon.gameObject.SetActive(true);
                favoriteIcon.color = Color.red;
                UnityEngine.Canvas.ForceUpdateCanvases();
            }
            else
            {
                favoriteIcon.gameObject.SetActive(false);
            }
        }
    }

    public static void OnAdditionalActionPostfix(GearUpgradeUI __instance, int index, ref bool refreshUI)
    {
        if (index == 0 && __instance.Upgrade != null) {
            ScrapHandlingMod.SetFavorite(__instance.Upgrade, __instance.Upgrade.Favorite);
        }
    }

    public static void EnableGridViewPostfix(GearUpgradeUI __instance, bool grid)
    {
        if (ScrapHandlingMod.IsScrapping) return;
        if (__instance.Upgrade == null) return;
        var favoriteIconField = AccessTools.Field(typeof(GearUpgradeUI), "favoriteIcon");
        var favoriteIcon = (Image)favoriteIconField.GetValue(__instance);
        if (favoriteIcon.gameObject.activeSelf)
        {
            favoriteIcon.color = ScrapHandlingMod.IsFavorite(__instance.Upgrade) ? Color.white : Color.red;
        }
    }

    public static void UpdatePrefix(GearDetailsWindow __instance)
    {
        if (ScrapHandlingMod.IsScrapping) return;
        if (Keyboard.current != null)
        {
            bool isKeyPressed = Keyboard.current[ScrapHandlingMod.currentTrashKey].isPressed;

            if (!isKeyPressed)
            {
                if (toggledThisSession.Count > 0)
                {
                    toggledThisSession.Clear();
                }
                return;
            }

            GearUpgradeUI hoveredUI = null;
            if (UIRaycaster.RaycastForComponent<GearUpgradeUI>(out hoveredUI))
            {
                var upgrade = hoveredUI.Upgrade;
                if (upgrade != null && !ScrapHandlingMod.IsFavorite(upgrade) && !toggledThisSession.Contains(upgrade))
                {
                    bool wasMarked = ScrapHandlingMod.IsTrashMarked(upgrade);
                    ScrapHandlingMod.SetTrashMark(upgrade, !wasMarked);
                    toggledThisSession.Add(upgrade);
                    var favoriteIconField2 = AccessTools.Field(typeof(GearUpgradeUI), "favoriteIcon");
                    var favoriteIcon2 = (Image)favoriteIconField2?.GetValue(hoveredUI);
                    if (favoriteIcon2 != null)
                    {
                        if (ScrapHandlingMod.IsFavorite(upgrade))
                        {
                            favoriteIcon2.sprite = ScrapHandlingMod.starSprite;
                            favoriteIcon2.gameObject.SetActive(true);
                            favoriteIcon2.color = Color.white;
                        }
                        else if (ScrapHandlingMod.IsTrashMarked(upgrade))
                        {
                            favoriteIcon2.sprite = ScrapHandlingMod.starSprite;
                            favoriteIcon2.gameObject.SetActive(true);
                            favoriteIcon2.color = Color.red;
                        }
                        else
                        {
                            favoriteIcon2.gameObject.SetActive(false);
                        }
                        UnityEngine.Canvas.ForceUpdateCanvases();
                    }
                }
            }
        }
    }

    private static HashSet<UpgradeInstance> toggledThisSession = new HashSet<UpgradeInstance>();
}

public class HoldButtonHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private bool isHolding = false;
    private float holdTimer = 0f;
    private const float HOLD_DURATION = 1.0f;

    public System.Action onHoldComplete;

    public void OnPointerDown(PointerEventData eventData)
    {
        isHolding = true;
        holdTimer = 0f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isHolding = false;
    }

    void Update()
    {
        if (isHolding)
        {
            holdTimer += Time.deltaTime;
            if (holdTimer >= HOLD_DURATION)
            {
                onHoldComplete?.Invoke();
                isHolding = false;
            }
        }
    }
}
