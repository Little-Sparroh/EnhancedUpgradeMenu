using System.Collections;
using UnityEngine;

public class UpgradeSolver
{
    public Coroutine? SolverCoroutine;

    internal static UpgradeSolver Instance;
    internal SolverUI SolverUI = new(null);

    public void Update()
    {
        SolverUI.Update();
    }

    internal void OnGearDetailsWindowOpen(GearDetailsWindow window, MonoBehaviour mono)
    {
        SolverUI.GearDetailsWindow = window;
        SolverUI.OnWindowOpened();
        SolverUI.PatchUpgradeClick();
        mono.StartCoroutine(DelayPatch());
    }

    private IEnumerator DelayPatch()
    {
        yield return null;
        SolverUI.PatchUpgradeClick();
    }

    internal void OnGearDetailsWindowClosed()
    {
        SolverUI.Close();
    }

    public void OnGUI()
    {
        SolverUI.OnGUI();
    }
}
