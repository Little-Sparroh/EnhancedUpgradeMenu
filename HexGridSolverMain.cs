using System.Collections;
using UnityEngine;

public class UpgradeSolver
{
    public Coroutine? SolverCoroutine;

    internal static UpgradeSolver Instance;
    internal SolverUI SolverUI = new(null, null);

    public void Update()
    {
        SolverUI.Update();
    }

    internal void OnUpgradeWindowOpen(IUpgradeWindow window, IUpgradable gear, MonoBehaviour mono)
    {
        SolverUI.UpgradeWindow = window;
        SolverUI.CurrentGear = gear;
        SolverUI.OnWindowOpened();
        SolverUI.PatchUpgradeClick();
        mono.StartCoroutine(DelayPatch());
    }

    private IEnumerator DelayPatch()
    {
        yield return null;
        SolverUI.PatchUpgradeClick();
    }

    internal void OnUpgradeWindowClosed()
    {
        SolverUI.Close();
    }

    public void OnGUI()
    {
        SolverUI.OnGUI();
    }
}
