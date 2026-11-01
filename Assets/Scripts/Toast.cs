using System.Collections;
using TMPro;
using UnityEngine;

public class Toast : MonoBehaviour
{
    static Toast instance;
    [SerializeField] TMP_Text label;
    [SerializeField] float showTime = 1.5f;

    Coroutine routine;

    void Awake()
    {
        instance = this;
        label.gameObject.SetActive(false);
    }

    public static void Show(string message)
    {
        if (instance != null) instance.Display(message);
    }

    void Display(string message)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Run(message));
    }

    IEnumerator Run(string message)
    {
        label.text = message;
        label.gameObject.SetActive(true);
        yield return new WaitForSeconds(showTime);
        label.gameObject.SetActive(false);
    }
}
