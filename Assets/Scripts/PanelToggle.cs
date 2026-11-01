using UnityEngine;

public class PanelToggle : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] KeyCode key = KeyCode.None;

    public void Toggle() => panel.SetActive(!panel.activeSelf);

    void Update()
    {
        if (key != KeyCode.None && Input.GetKeyDown(key)) Toggle();
    }
}
