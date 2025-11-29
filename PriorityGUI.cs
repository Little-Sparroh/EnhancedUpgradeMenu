using System.Collections.Generic;
using UnityEngine;

public class PriorityGUI : MonoBehaviour
{
    private bool showWindow = false;
    private List<PriorityCriteria> currentOrder;
    private Rect windowRect = new Rect(100, 100, 300, 600);

    private int draggedIndex = -1;
    private float dragOffsetY;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        currentOrder = PriorityPatches.LoadPriorityOrder();
    }

    void OnGUI()
    {

        if (PriorityPatches.IsWindowOpen)
        {
            float buttonY = Screen.height / 2 - 25;
            float filterX = Screen.width * 0.75f;
            float priorityX = filterX - 160;
            if (GUI.Button(new Rect(priorityX, buttonY, 150, 50), "Priority Sort"))
            {
                showWindow = !showWindow;
                if (!showWindow)
                {
                    currentOrder = PriorityPatches.LoadPriorityOrder();
                }
            }
        }

        if (showWindow)
        {
            windowRect = GUI.Window(0, windowRect, DrawWindow, "Sort Priority");
        }
    }

    void DrawWindow(int id)
    {
        GUI.DragWindow(new Rect(0, 0, 300, 20));

        Event e = Event.current;

        for (int i = 0; i < currentOrder.Count; i++)
        {
            if (i == draggedIndex) continue;

            Rect itemRect = new Rect(10, 30 + i * 25, 270, 20);
            GUI.Label(itemRect, (i + 1) + ". " + GetCriteriaName(currentOrder[i]));

            if (e.type == EventType.MouseDown && itemRect.Contains(e.mousePosition))
            {
                draggedIndex = i;
                dragOffsetY = e.mousePosition.y - itemRect.y;
                e.Use();
            }
        }

        if (draggedIndex != -1)
        {
            if (e.type == EventType.MouseDrag)
            {
                float newY = e.mousePosition.y - dragOffsetY;
                int newIndex = Mathf.Clamp(Mathf.RoundToInt((newY - 30) / 25), 0, currentOrder.Count - 1);
                if (newIndex != draggedIndex)
                {
                    PriorityCriteria item = currentOrder[draggedIndex];
                    currentOrder.RemoveAt(draggedIndex);
                    currentOrder.Insert(newIndex, item);
                    draggedIndex = newIndex;
                }
                e.Use();
            }
            else if (e.type == EventType.MouseUp)
            {
                draggedIndex = -1;
                e.Use();
            }
        }

        if (draggedIndex != -1)
        {
            float drawY = e.mousePosition.y - dragOffsetY;
            Rect draggedRect = new Rect(10, drawY, 270, 20);
            GUI.skin.label.fontStyle = FontStyle.Bold;
            GUI.Label(draggedRect, "x. " + GetCriteriaName(currentOrder[draggedIndex]));
            GUI.skin.label.fontStyle = FontStyle.Normal;
        }

        if (GUI.Button(new Rect(10, 520, 70, 30), "Save"))
        {
            PriorityPatches.SavePriorityOrder(currentOrder);
            PriorityPatches.TriggerPrioritySort();
            showWindow = false;
        }

        if (GUI.Button(new Rect(90, 520, 70, 30), "Cancel"))
        {
            showWindow = false;
            currentOrder = PriorityPatches.LoadPriorityOrder();
        }

        if (GUI.Button(new Rect(170, 520, 70, 30), "Reset"))
        {
            currentOrder = new List<PriorityCriteria>
            {
                PriorityCriteria.Favorited, PriorityCriteria.NotFavorited, PriorityCriteria.Unlocked,
                PriorityCriteria.Locked, PriorityCriteria.RecentlyUsed, PriorityCriteria.RecentlyAcquired,
                PriorityCriteria.InstanceName, PriorityCriteria.Oddity, PriorityCriteria.Exotic,
                PriorityCriteria.Epic, PriorityCriteria.Rare, PriorityCriteria.Standard,
                PriorityCriteria.Turbocharged, PriorityCriteria.Trashed, PriorityCriteria.NotTurbocharged,
                PriorityCriteria.NotTrashed
            };
        }
    }

    void Swap(int a, int b)
    {
        (currentOrder[a], currentOrder[b]) = (currentOrder[b], currentOrder[a]);
    }

    string GetCriteriaName(PriorityCriteria criteria)
    {
        return criteria switch
        {
            PriorityCriteria.Favorited => "Favorited",
            PriorityCriteria.NotFavorited => "Not Favorited",
            PriorityCriteria.Unlocked => "Unlocked",
            PriorityCriteria.Locked => "Locked",
            PriorityCriteria.RecentlyUsed => "Recently Used",
            PriorityCriteria.RecentlyAcquired => "Recently Acquired",
            PriorityCriteria.InstanceName => "Upgrade Instance Name",
            PriorityCriteria.Oddity => "Oddity",
            PriorityCriteria.Exotic => "Exotic",
            PriorityCriteria.Epic => "Epic",
            PriorityCriteria.Rare => "Rare",
            PriorityCriteria.Standard => "Standard",
            PriorityCriteria.Turbocharged => "Turbocharged",
            PriorityCriteria.Trashed => "Trashed",
            PriorityCriteria.NotTurbocharged => "Not Turbocharged",
            PriorityCriteria.NotTrashed => "Not Trashed",
            _ => "Unknown"
        };
    }
}
