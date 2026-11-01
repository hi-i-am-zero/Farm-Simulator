using UnityEngine;

public class PopEffect : MonoBehaviour
{
    SpriteRenderer sr;
    Vector3 start;
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

    void Awake() => sr = GetComponent<SpriteRenderer>();
    void Start() => start = transform.position;

    void Update()
    {
        t += Time.deltaTime / 0.7f;
        transform.position = start + Vector3.up * (t * 0.9f);
        var c = sr.color;
        c.a = 1f - t;
        sr.color = c;
        if (t >= 1f) Destroy(gameObject);
    }
}
