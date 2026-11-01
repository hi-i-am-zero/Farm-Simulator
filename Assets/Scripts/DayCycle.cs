using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DayCycle : MonoBehaviour
{
    public static DayCycle Instance { get; private set; }

    [SerializeField] float dayLengthSeconds = 120f;
    [SerializeField] Image nightOverlay;
    [SerializeField] TMP_Text dayText;
    [SerializeField] Color nightColor = new Color(0.04f, 0.07f, 0.25f, 1f);
    [SerializeField, Range(0f, 1f)] float maxNightAlpha = 0.55f;

    public int Day { get; set; } = 1;
    public float TimeOfDay { get; set; } // 0..1

    void Awake() => Instance = this;

    void Update()
    {
        TimeOfDay += Time.deltaTime / dayLengthSeconds;
        if (TimeOfDay >= 1f) { TimeOfDay -= 1f; Day++; }

        // Tối dần ở 55%-75% ngày, sáng dần ở 85%-95%
        float fadeIn = Mathf.InverseLerp(0.55f, 0.75f, TimeOfDay);
        float fadeOut = 1f - Mathf.InverseLerp(0.85f, 0.95f, TimeOfDay);
        float alpha = Mathf.Min(fadeIn, fadeOut) * maxNightAlpha;

        var c = nightColor;
        c.a = alpha;
        nightOverlay.color = c;

        dayText.text = $"Ngày {Day}";
    }
}
