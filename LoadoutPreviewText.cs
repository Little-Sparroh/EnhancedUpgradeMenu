using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TextPreview : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    private List<TextMeshProUGUI> textElements = new List<TextMeshProUGUI>();

    private void Awake()
    {
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0.9f;
        gameObject.AddComponent<VerticalLayoutGroup>();
        gameObject.AddComponent<ContentSizeFitter>();
        Image bg = gameObject.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.7f);
    }

    public void Setup(List<MiniHexPreview.UpgradePlacement> placements)
    {
        Clear();

        VerticalLayoutGroup vlg = GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.spacing = 5;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = GetComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Image bg = GetComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.7f);

        foreach (var placement in placements)
        {
            if (placement.Upgrade == null) continue;

            GameObject textGO = new GameObject("TextElement");
            textGO.transform.SetParent(transform, false);

            TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = placement.Upgrade.Upgrade.Name;
            text.fontSize = 14;
            text.color = placement.Upgrade.Upgrade.Color;
            text.alignment = TextAlignmentOptions.Left;
            text.enableWordWrapping = false;

            textElements.Add(text);
        }
    }

    private void Clear()
    {
        foreach (var text in textElements)
        {
            if (text != null) Destroy(text.gameObject);
        }
        textElements.Clear();
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
}
