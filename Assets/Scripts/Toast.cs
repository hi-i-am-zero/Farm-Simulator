using System.Collections;
using TMPro;
using UnityEngine;

public class Toast : MonoBehaviour
{
    static Toast instance;
    [SerializeField] TMP_Text label;
    [SerializeField] float showTime = 1.5f;

    Coroutine routine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => instance = null;

    void Awake()
    {
        instance = this;
        ConfigureLabel();
        if (label != null) label.gameObject.SetActive(false);
    }

    public static void Show(string message)
    {
        if (instance != null) instance.Display(message);
    }

    void Display(string message)
    {
        if (label == null) return;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Run(message));
    }

    void ConfigureLabel()
    {
        if (label == null) return;
        // Toast chỉ để hiển thị thông báo, không chặn thao tác click nút bên dưới (như hàng đầu trong Shop)
        label.raycastTarget = false;
        if (label.font == null && TMP_Settings.defaultFontAsset != null)
            label.font = TMP_Settings.defaultFontAsset;
    }

    IEnumerator Run(string message)
    {
        ConfigureLabel();
        label.text = message;
        label.gameObject.SetActive(true);
        yield return new WaitForSecondsRealtime(showTime);
        if (label != null) label.gameObject.SetActive(false);
    }
}
