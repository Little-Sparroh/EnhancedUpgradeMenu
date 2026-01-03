using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class SolverUI
{
    private static FieldInfo? GetPrivateField(string name, System.Type type)
    {
        try
        {
            return type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        }
        catch
        {
            return null;
        }
    }

    private readonly Color _defaultSecondaryColor = new(0.9434f, 0.9434f, 0.9434f, 1);
    private readonly Color _grayedOutColor = new(0.9434f, 0.9434f, 0.9434f, 0.1484f);

    internal IUpgradeWindow? UpgradeWindow;
    internal IUpgradable? CurrentGear;

    private readonly Dictionary<int, UnityEvent> _originalOnHoverEnters = new();
    private Dictionary<int, GearUpgradeUI> _selectedUpgrades = new();
    private GearUpgradeUI? _hoveredUpgrade;
    private bool _showSolveButton;
    private bool _solveButtonEnabled;
    private readonly HashSet<UpgradeInstance> _toggledThisSession = new();

    private readonly InputActionMap _solverControls;
    private readonly InputAction _addForSolve;

    public SolverUI(IUpgradeWindow? upgradeWindow, IUpgradable? currentGear)
    {
        UpgradeWindow = upgradeWindow;
        CurrentGear = currentGear;

        _solverControls = new InputActionMap("SolverControls");
        _addForSolve = _solverControls.AddAction("AddForSolve");
        _addForSolve.AddBinding("<Keyboard>/n");
    }

    internal void OnWindowOpened()
    {
        _showSolveButton = true;
        _solveButtonEnabled = false;
    }

    private void SelectUpgrade()
    {
        if (_hoveredUpgrade == null || !_hoveredUpgrade.Upgrade.IsUnlocked)
            return;

        var upgrade = _hoveredUpgrade.Upgrade;
        var rarity = Global.Instance.Rarities[(int)upgrade.Upgrade.Rarity];

        var buttonField = GetPrivateField("button", typeof(GearUpgradeUI));
        var idField = GetPrivateField("upgradeID", typeof(Upgrade));
        var hoverColorField = GetPrivateField("hoverColor", typeof(Pigeon.UI.DefaultButton));

        if (buttonField == null)
        {
            return;
        }

        if (_selectedUpgrades.TryGetValue(upgrade.InstanceID, out var selectedUI))
        {
            var btn = buttonField.GetValue(selectedUI) as Pigeon.UI.DefaultButton;
            if (btn != null) btn.SetDefaultColor(rarity.backgroundColor);
            _selectedUpgrades.Remove(upgrade.InstanceID);
            _solveButtonEnabled = _selectedUpgrades.Count > 0;
            return;
        }

        if (!upgrade.CanStack)
        {
            var conflictingUpgrade = _selectedUpgrades.Values
                .FirstOrDefault(u =>
                    (!upgrade.CanStack && idField != null && (int)idField.GetValue(u.Upgrade) == (int)idField.GetValue(upgrade.Upgrade)));

            if (conflictingUpgrade != null)
            {
                var backgroundColor = Global.Instance.Rarities[(int)conflictingUpgrade.Upgrade.Upgrade.Rarity]
                    .backgroundColor;
                var btn = buttonField.GetValue(conflictingUpgrade) as Pigeon.UI.DefaultButton;
                if (btn != null) btn.SetDefaultColor(backgroundColor);
                _selectedUpgrades.Remove(conflictingUpgrade.Upgrade.InstanceID);
            }
        }

        var btn2 = buttonField.GetValue(_hoveredUpgrade) as Pigeon.UI.DefaultButton;
        if (btn2 != null && hoverColorField != null)
        {
            var hoverColor = hoverColorField.GetValue(btn2) as Color?;
            if (hoverColor.HasValue) btn2.SetDefaultColor(hoverColor.Value);
        }
        _selectedUpgrades[upgrade.InstanceID] = _hoveredUpgrade;
        _solveButtonEnabled = true;
    }

    internal void RebuildSelectedUpgrades()
    {
        if (UpgradeWindow is null) return;

        var upgradeListParentField = GetPrivateField("upgradeListParent", UpgradeWindow.GetType());
        var buttonField = GetPrivateField("button", typeof(GearUpgradeUI));
        var hoverColorField = GetPrivateField("hoverColor", typeof(Pigeon.UI.DefaultButton));

        if (upgradeListParentField == null || buttonField == null) return;

        var upgradeListParent = upgradeListParentField.GetValue(UpgradeWindow) as Transform;
        if (upgradeListParent == null) return;

        var upgradeUIs = upgradeListParent.GetComponentsInChildren<GearUpgradeUI>().Where(x => _selectedUpgrades.ContainsKey(x.Upgrade.InstanceID)).ToList();

        _selectedUpgrades = upgradeUIs.ToDictionary(ui => ui.Upgrade.InstanceID);

        foreach (var selectedUpgradeKv in _selectedUpgrades)
        {
            var btn = buttonField.GetValue(selectedUpgradeKv.Value) as Pigeon.UI.DefaultButton;
            var hoverColor = hoverColorField?.GetValue(btn) as Color?;
            if (btn != null && hoverColor.HasValue) btn.SetDefaultColor(hoverColor.Value);
        }

        _solveButtonEnabled = _selectedUpgrades.Count > 0;
    }

    internal void Close()
    {
        _solverControls.Disable();
        _showSolveButton = false;

        _originalOnHoverEnters.Clear();
        _selectedUpgrades.Clear();
        var coroutine = UpgradeSolver.Instance.SolverCoroutine;
        if (coroutine is not null) SparrohPlugin.Instance.StopCoroutine(coroutine);
    }

    internal void Update()
    {
        if (Keyboard.current != null)
        {
            bool isKeyPressed = Keyboard.current.nKey.isPressed;

            if (!isKeyPressed)
            {
                if (_toggledThisSession.Count > 0)
                {
                    _toggledThisSession.Clear();
                }
                return;
            }

            GearUpgradeUI hoveredUI = null;
            if (UIRaycaster.RaycastForComponent<GearUpgradeUI>(out hoveredUI))
            {
                var upgrade = hoveredUI.Upgrade;
                if (upgrade != null && !_toggledThisSession.Contains(upgrade))
                {
                    _hoveredUpgrade = hoveredUI;
                    SelectUpgrade();
                    _toggledThisSession.Add(upgrade);
                }
            }
        }
    }

    public void OnGUI()
    {
        if (UpgradeWindow == null || !((UnityEngine.Component)UpgradeWindow).gameObject.activeInHierarchy) return;
        if (!_showSolveButton) return;

        float buttonX = 10f;
        float buttonY = Screen.height / 2 - 25;
        float buttonWidth = 150;
        float buttonHeight = 50;
        float margin = 10;

        GUI.enabled = _solveButtonEnabled && UpgradeSolver.Instance.SolverCoroutine == null;
        if (GUI.Button(new UnityEngine.Rect(buttonX, buttonY, buttonWidth, buttonHeight), "Solve"))
        {
            foreach (var selectedUpgrade in _selectedUpgrades.Values)
                SparrohPlugin.Logger.LogInfo($"\t • {FormatUpgrade(selectedUpgrade)}");

            var upgrades = _selectedUpgrades
                .Select(u => u.Value.Upgrade)
                .OrderByDescending(u => u.Upgrade.Name == "Boundary Incursion" ? int.MaxValue : u.GetPattern().GetCellCount())
                .ToList();

            var solver = new Solver(UpgradeWindow, CurrentGear, upgrades);
            if (!solver.CanFitAll())
            {
                ShowError("Solve Error", "The selected upgrades cannot fit in the available space.");
            }
            else
            {
                solver.TrySolve(success =>
                {
                    SparrohPlugin.Logger.LogInfo(success ? "Found a solution" : "No solution");
                    foreach (var upgradeUI in _selectedUpgrades.Values)
                    {
                        var rarity = Global.Instance.Rarities[(int)upgradeUI.Upgrade.Upgrade.Rarity];
                        var buttonField = GetPrivateField("button", typeof(GearUpgradeUI));
                        var btn = buttonField.GetValue(upgradeUI) as Pigeon.UI.DefaultButton;
                        if (btn != null) btn.SetDefaultColor(rarity.backgroundColor);
                    }
                    _selectedUpgrades.Clear();
                    _solveButtonEnabled = false;
                    UpgradeSolver.Instance.SolverCoroutine = null;
                });
            }
        }
        GUI.enabled = true;

        GUI.enabled = UpgradeSolver.Instance.SolverCoroutine != null;
        if (GUI.Button(new UnityEngine.Rect(buttonX, buttonY + buttonHeight + margin, buttonWidth, buttonHeight), "Cancel"))
        {
            SparrohPlugin.Instance.StopCoroutine(UpgradeSolver.Instance.SolverCoroutine);
            UpgradeSolver.Instance.SolverCoroutine = null;
        }
        GUI.enabled = true;

        GUI.enabled = _selectedUpgrades.Count > 0;
        if (GUI.Button(new UnityEngine.Rect(buttonX, buttonY + buttonHeight + margin + buttonHeight + margin, buttonWidth, buttonHeight), "Clear"))
        {
            foreach (var upgradeUI in _selectedUpgrades.Values)
            {
                var rarity = Global.Instance.Rarities[(int)upgradeUI.Upgrade.Upgrade.Rarity];
                var buttonField = GetPrivateField("button", typeof(GearUpgradeUI));
                var btn = buttonField.GetValue(upgradeUI) as Pigeon.UI.DefaultButton;
                if (btn != null) btn.SetDefaultColor(rarity.backgroundColor);
            }
            _selectedUpgrades.Clear();
            _solveButtonEnabled = false;
        }
    }

    private void ShowError(string title, string message)
    {
    }

    private static string FormatUpgrade(GearUpgradeUI upgrade)
    {
        var upgradeInstance = upgrade.Upgrade;
        return $"[{upgradeInstance.Upgrade.RarityName}] {upgradeInstance.Upgrade.Name} ({upgradeInstance.InstanceID})";
    }

    internal void PatchUpgradeClick()
    {
        if (UpgradeWindow is null) return;

        _solverControls.Enable();

        var upgradeListParentField = GetPrivateField("upgradeListParent", UpgradeWindow.GetType());
        var buttonField = GetPrivateField("button", typeof(GearUpgradeUI));

        if (upgradeListParentField == null || buttonField == null)
        {
            return;
        }

        var upgradeListParent = upgradeListParentField.GetValue(UpgradeWindow) as Transform;
        if (upgradeListParent == null)
        {
            return;
        }

        var upgradeUIs = upgradeListParent.GetComponentsInChildren<GearUpgradeUI>().Where(x => x.gameObject.activeSelf).ToList();

        foreach (var upgradeUI in upgradeUIs)
        {
            var btn = buttonField.GetValue(upgradeUI) as Pigeon.UI.DefaultButton;
            if (btn is null)
            {
                continue;
            }

            var instanceID = upgradeUI.Upgrade.InstanceID;
            if (_originalOnHoverEnters.ContainsKey(instanceID)) continue;

            var originalOnHoverEnter = btn.OnHoverEnter;
            _originalOnHoverEnters[instanceID] = originalOnHoverEnter;

            btn.OnHoverExit.AddListener(() => _hoveredUpgrade = null);
            btn.OnHoverEnter.AddListener(() => _hoveredUpgrade = upgradeUI);
        }
    }
}
