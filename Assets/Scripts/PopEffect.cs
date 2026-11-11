using TMPro;
using UnityEngine;

public class PopEffect : MonoBehaviour
{
    SpriteRenderer sr;
    TextMeshPro tmp;
    Vector3 start;
    float duration = 0.7f;
    float t;

    public static void Spawn(Sprite sprite, Vector3 position)
    {
        if (sprite == null) return;
        var go = new GameObject("Pop");
        go.transform.position = position + Vector3.up * 0.5f;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = "Crops";
        renderer.sortingOrder = 20;
        go.AddComponent<PopEffect>();
    }

    public static void SpawnText(string message, Vector3 position, Color? color = null)
    {
        if (string.IsNullOrEmpty(message)) return;
        var go = new GameObject("PopText");
        go.transform.position = position + Vector3.up * 0.85f;

        var text = go.AddComponent<TextMeshPro>();
        if (text.font == null && TMP_Settings.defaultFontAsset != null)
            text.font = TMP_Settings.defaultFontAsset;
        text.text = message;
        text.fontSize = 3.2f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.color = color ?? new Color(1f, 0.92f, 0.2f, 1f);
        var rend = go.GetComponent<Renderer>();
        if (rend != null && rend.sharedMaterial != null)
        {
            text.outlineWidth = 0.28f;
            text.outlineColor = new Color32(20, 15, 5, 255);
        }
        text.sortingLayerID = SortingLayer.NameToID("Player");
        text.sortingOrder = 30;

        var rt = go.GetComponent<RectTransform>();
        if (rt != null) rt.sizeDelta = new Vector2(5f, 1.5f);

        go.AddComponent<PopEffect>();
    }

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        tmp = GetComponent<TextMeshPro>();
        duration = tmp != null ? 1.05f : 0.7f;
    }

    void Start() => start = transform.position;

    void Update()
    {
        t += Time.deltaTime / duration;
        transform.position = start + Vector3.up * (t * 0.9f);
        float alpha = Mathf.Clamp01(1f - t);

        if (sr != null)
        {
            var c = sr.color;
            c.a = alpha;
            sr.color = c;
        }

        if (tmp != null)
        {
            var c = tmp.color;
            c.a = alpha;
            tmp.color = c;
        }

        if (t >= 1f) Destroy(gameObject);
    }
}
