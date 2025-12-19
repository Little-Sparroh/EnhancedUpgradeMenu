using System.Collections.Generic;
using Pigeon.UI;
using UnityEngine;
using UnityEngine.UI;

public class MiniHexPreview : MonoBehaviour
{
    [SerializeField]
    private RectTransform cellPrefab;

    [SerializeField]
    private RectTransform connectionPrefab;

    [SerializeField]
    private RectTransform slotPrefab;

    public class UpgradePlacement
    {
        public UpgradeInstance Upgrade;
        public int X;
        public int Y;
        public byte Rotation;
    }

    private List<RectTransform> slots = new List<RectTransform>();
    private List<RectTransform> cells = new List<RectTransform>();
    private List<RectTransform> connections = new List<RectTransform>();
    private HexMap hexMap;

    public static float Scale = 0.5f;
    private const int FixedWidth = 7;
    private const int FixedHeight = 6;

    public static float YCorrection = 0f;
    public static float OddColumnYCorrection = 0f;

    public void Setup(List<UpgradePlacement> placements)
    {
        if (hexMap == null)
            hexMap = new HexMap(FixedWidth, FixedHeight);

        Clear();

        CreateGrid();

        foreach (var placement in placements)
        {
            EquipUpgrade(placement);
        }
    }

    private void CreateGrid()
    {
        float num = slotPrefab.sizeDelta.x * Scale * 0.5f;
        float num2 = num * Mathf.Sqrt(3f);
        float num3 = num * 1.5f;
        float num4 = num2;

        for (int i = 0; i < FixedHeight; i++)
        {
            for (int j = 0; j < FixedWidth; j++)
            {
                RectTransform slot = Instantiate(slotPrefab, transform);
                slot.localScale = new Vector3(Scale, Scale, Scale);
                slot.anchoredPosition = new Vector2(j * num3, (float)i * (0f - num4) + ((j % 2 == 0) ? 0f : (num2 * -0.5f)));
                slots.Add(slot);
            }
        }

        ((RectTransform)transform).sizeDelta = new Vector2(
            FixedWidth * num3,
            FixedHeight * num4 + num2 * 0.5f);
    }

    private void EquipUpgrade(UpgradePlacement placement)
    {
        if (placement.Upgrade == null) return;

        HexMap modifiedMap = placement.Upgrade.GetPattern().GetModifiedMap(placement.Rotation);
        float num2 = slotPrefab.sizeDelta.x * 0.5f * Mathf.Sqrt(3f) * Scale;
        float num3 = cellPrefab.sizeDelta.x * 0.5f * Mathf.Sqrt(3f) * Scale;

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
        for (int l = 0; l < modifiedMap.Height; l++) {
            for (int k = 0; k < modifiedMap.Width; k++) {
                if (modifiedMap[k, l].enabled) {
                    int gx = offsetX + k;
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
        if (gxMax >= FixedWidth) adjustX -= Mathf.Max(0, gxMax - FixedWidth + 1);
        if (gyMin < 0) adjustY = -gyMin;
        if (gyMax >= FixedHeight) adjustY -= Mathf.Max(0, gyMax - FixedHeight + 1);
        offsetX += adjustX;
        offsetY += adjustY;

        HexMap.Enumerator activeCells = modifiedMap.GetActiveCells();
        while (activeCells.MoveNext())
        {
            int num4 = activeCells.X + offsetX;
            int num5 = activeCells.Y + offsetY;

            if (num4 < 0 || num5 < 0 || num4 >= FixedWidth || num5 >= FixedHeight) continue;

            hexMap[num4, num5].SetUpgrade(placement.Upgrade, activeCells.Current.connections);

            RectTransform cellGraphic = Instantiate(cellPrefab, transform);
            cellGraphic.localScale = new Vector3(Scale, Scale, Scale);

            foreach (RectTransform child in cellGraphic.GetComponentsInChildren<RectTransform>(true))
            {
                if (child != cellGraphic)
                {
                    child.anchoredPosition = child.anchoredPosition * Scale;
                    child.sizeDelta = child.sizeDelta * Scale;
                    AdjustChildTransforms(child, Scale);
                }
            }

            var slot = slots[FixedWidth * num5 + num4];
            cellGraphic.anchoredPosition = slot.anchoredPosition;

            float yOffset = YCorrection;
            if (num4 % 2 == 1)
                yOffset += OddColumnYCorrection;
            cellGraphic.anchoredPosition = new Vector2(
                cellGraphic.anchoredPosition.x,
                cellGraphic.anchoredPosition.y + yOffset
            );

            PolygonOutline component = cellGraphic.GetComponent<PolygonOutline>();
            Color color = placement.Upgrade.Upgrade.Color;
            component.color = color;
            component.thickness = (placement.Upgrade.Upgrade.UpgradeType == Upgrade.Type.OnlyOneOfThisType) ? 5f : 11f;

            for (int c = 0; c < component.transform.childCount; c++)
            {
                component.transform.GetChild(c).GetComponent<Graphic>().color = color;
            }

            cells.Add(cellGraphic);

            for (int i = 0; i < 6; i++)
            {
                if (hexMap[num4, num5].HasConnection(1 << i))
                {
                    RectTransform connection = Instantiate(connectionPrefab, cellGraphic);
                    connection.localScale = new Vector3(Scale, Scale, Scale);

                    foreach (RectTransform child in connection.GetComponentsInChildren<RectTransform>(true))
                    {
                        if (child != connection)
                        {
                            child.anchoredPosition = child.anchoredPosition * Scale;
                            child.sizeDelta = child.sizeDelta * Scale;
                            AdjustChildTransforms(child, Scale);
                        }
                    }
                    float num6 = (float)(-i) * 60f + 90f;
                    float num7 = num2 * 0.5f;
                    connection.anchoredPosition = new Vector2(Mathf.Cos(num6 * Mathf.PI / 180f) * (num7 - 1f), Mathf.Sin(num6 * Mathf.PI / 180f) * (num7 - 1f));
                    connection.sizeDelta = new Vector2(connection.sizeDelta.x, num7 - num3 * 0.25f - 1f);
                    connection.localRotation = Quaternion.Euler(0f, 0f, num6 - 90f);

                    var connGraphic = connection.GetComponent<Graphic>();
                    if (connGraphic != null) connGraphic.color = color;
                    for (int cc = 0; cc < connection.childCount; cc++)
                    {
                        var childGraphic = connection.GetChild(cc).GetComponent<Graphic>();
                        if (childGraphic != null) childGraphic.color = color;
                    }

                    connections.Add(connection);
                }
            }
        }
    }

    public void Clear()
    {
        foreach (var slot in slots)
        {
            if (slot != null) Destroy(slot.gameObject);
        }
        slots.Clear();

        foreach (var cell in cells)
        {
            if (cell != null) Destroy(cell.gameObject);
        }
        cells.Clear();

        foreach (var conn in connections)
        {
            if (conn != null) Destroy(conn.gameObject);
        }
        connections.Clear();

        for (int x = 0; x < FixedWidth; x++)
        {
            for (int y = 0; y < FixedHeight; y++)
            {
                hexMap[x, y].Disable();
            }
        }
    }

    private void OnDestroy()
    {
        Clear();
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
