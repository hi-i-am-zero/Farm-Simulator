using UnityEngine;

public class PanelToggle : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] KeyCode key = KeyCode.None;
    [SerializeField] GameObject[] showWithPanel;

    void Start() => SyncExtras();

    public void Toggle()
    {
        if (panel == null) return;
        panel.SetActive(!panel.activeSelf);
        SyncExtras();
    }

    void Update()
    {
        if (ControlsGuide.IsOpen) return;
        if (key != KeyCode.None && Input.GetKeyDown(key))
            Toggle();
    }

    void SyncExtras()
    {
        if (panel == null || showWithPanel == null) return;
        bool on = panel.activeSelf;
        foreach (var go in showWithPanel)
            if (go != null) go.SetActive(on);
    }
}
