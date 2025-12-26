using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HarmonyLib;
using Pigeon.UI;

public class EquipSlotsPreview : MonoBehaviour
{
    private GameObject clonedEquipSlots;
    private Component equipSlotsComponent;
    private CanvasGroup canvasGroup;

    private float SlotSize => (Traverse.Create(equipSlotsComponent).Field("slotPrefab").GetValue() as RectTransform)?.sizeDelta.x ?? 100f;

    public static float YCorrection = 0f;
    public static float OddColumnYCorrection = 0f;

    public void Setup(List<MiniHexPreview.UpgradePlacement> placements)
    {
        if (equipSlotsComponent != null)
        {
            foreach (var placement in placements)
            {
                if (placement.Upgrade == null) continue;

                try
                {
                    int childCountBefore = clonedEquipSlots.transform.childCount;

                    HexMap modifiedMap = placement.Upgrade.GetPattern().GetModifiedMap(placement.Rotation);

                    int minK = int.MaxValue, minL = int.MaxValue;
                    int maxK = int.MinValue, maxL = int.MinValue;
                    for (int l = 0; l < modifiedMap.Height; l++)
                    {
                        for (int k = 0; k < modifiedMap.Width; k++)
                        {
                            if (modifiedMap[k, l].enabled)
                            {
                                if (k < minK) minK = k;
                                if (l < minL) minL = l;
                                if (k > maxK) maxK = k;
                                if (l > maxL) maxL = l;
                            }
                        }
                    }

                    if (minK == int.MaxValue) minK = 0;
                    if (minL == int.MaxValue) minL = 0;
                    int offsetX = placement.X - minK;
                    int offsetY = placement.Y - minL;

                    int gxMin = int.MaxValue, gxMax = int.MinValue;
                    int gyMin = int.MaxValue, gyMax = int.MinValue;
                    for (int l = 0; l < modifiedMap.Height; l++)
                    {
                        for (int k1 = 0; k1 < modifiedMap.Width; k1++)
                        {
                            if (modifiedMap[k1, l].enabled)
                            {
                                int gx = offsetX + k1;
                                int gy = offsetY + l;
                                if (gx < gxMin) gxMin = gx;
                                if (gx > gxMax) gxMax = gx;
                                if (gy < gyMin) gyMin = gy;
                                if (gy > gyMax) gyMax = gy;
                            }
                        }
                    }

                    int adjustX = 0, adjustY = 0;
                    if (gxMin < 0) adjustX = -gxMin;
                    if (gxMax >= 7) adjustX -= Mathf.Max(0, gxMax - 7 + 1);
                    if (gyMin < 0) adjustY = -gyMin;
                    if (gyMax >= 6) adjustY -= Mathf.Max(0, gyMax - 6 + 1);
                    offsetX += adjustX;
                    offsetY += adjustY;

                    HexMap.Enumerator activeCells = modifiedMap.GetActiveCells();
                    while (activeCells.MoveNext())
                    {
                        int num4 = activeCells.X + offsetX;
                        int num5 = activeCells.Y + offsetY;

                        if (num4 < 0 || num5 < 0 || num4 >= 7 || num5 >= 6) continue;

                        RectTransform cellPrefab = Traverse.Create(equipSlotsComponent).Field("cellPrefab").GetValue() as RectTransform;
                        if (cellPrefab == null)
                        {
                            continue;
                        }

                        RectTransform cellGraphic = Instantiate(cellPrefab, clonedEquipSlots.transform);
                        cellGraphic.localScale = Vector3.one;

                        float num = SlotSize * 1 * 0.5f;
                        float num2 = num * Mathf.Sqrt(3f);
                        float num3 = num * 1.5f;
                        float num4Floating = num2;

                        cellGraphic.anchoredPosition = new Vector2(num4 * num3,
                            (float)num5 * (0f - num4Floating) + ((num4 % 2 == 0) ? 0f : (num2 * -0.5f)));

                        float yOffset = EquipSlotsPreview.YCorrection;
                        if (num4 % 2 == 1)
                            yOffset += EquipSlotsPreview.OddColumnYCorrection;
                        cellGraphic.anchoredPosition = new Vector2(
                            cellGraphic.anchoredPosition.x,
                            cellGraphic.anchoredPosition.y + yOffset
                        );

                        var component = cellGraphic.GetComponent<PolygonOutline>();
                        Color color = placement.Upgrade.Upgrade.Color;
                        if (component != null)
                        {
                            component.color = color;
                            component.thickness = (placement.Upgrade.Upgrade.UpgradeType == Upgrade.Type.OnlyOneOfThisType) ? 5f : 11f;
                        }

                        for (int c = 0; c < cellGraphic.childCount; c++)
                        {
                            var graphic = cellGraphic.GetChild(c).GetComponent<Graphic>();
                            if (graphic != null)
                                graphic.color = color;
                        }

                        int childCountAfter = clonedEquipSlots.transform.childCount;
                    }
                }
                catch (Exception ex)
                {
                    SparrohPlugin.Logger.LogError($"EquipSlotsPreview.Setup: Failed to equip upgrade: {ex.Message}");
                }
            }
        }
        clonedEquipSlots.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
    }

    public void SetPosition(Vector2 position)
    {
        ((RectTransform)transform).anchoredPosition = position;
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Initialize(GameObject clonedGO, Type equipSlotsType)
    {
        clonedEquipSlots = clonedGO;
        equipSlotsComponent = clonedGO.GetComponent(equipSlotsType);
        clonedGO.transform.SetParent(transform, false);

        ClearExistingUpgrades(clonedGO);

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0.9f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        var buttons = clonedGO.GetComponentsInChildren<UnityEngine.UI.Button>();
        foreach (var btn in buttons) btn.interactable = false;

        var rect = GetComponent<RectTransform>();
        if (rect == null) rect = gameObject.AddComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(10000, 10000);
        gameObject.SetActive(false);
    }

    private void ClearExistingUpgrades(GameObject clonedGO)
    {
        var cells = clonedGO.GetComponentsInChildren<PolygonOutline>();
        foreach (var cell in cells)
        {
            if (cell != null && cell.gameObject.name.Contains("PatternCell(Clone)"))
            {
                Destroy(cell.gameObject);
            }
        }

        var connections = clonedGO.GetComponentsInChildren<UnityEngine.UI.Image>();
        int destroyedConns = 0;
        foreach (var conn in connections)
        {
            var parent = conn.transform.parent;
            if (parent != null && parent.name.Contains("PatternCell(Clone)"))
            {
                Destroy(conn.gameObject);
                destroyedConns++;
            }
            else if (conn.gameObject.name.ToLower().Contains("slot") || conn.gameObject.name.ToLower().Contains("grid"))
            {
            }
            else
            {
            }
        }
    }

    private void AdjustChildTransforms(RectTransform parent, float scale)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            RectTransform child = parent.GetChild(i) as RectTransform;
            if (child != null)
            {
                child.anchoredPosition = child.anchoredPosition * scale;
                child.sizeDelta = child.sizeDelta * scale;
                AdjustChildTransforms(child, scale);
            }
        }
    }
}
