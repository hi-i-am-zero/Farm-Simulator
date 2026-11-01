using UnityEngine;
using UnityEngine.UI;

public class RainEffect : MonoBehaviour
{
    [SerializeField] int dropCount = 90;
    [SerializeField] Vector2 dropSize = new Vector2(3f, 30f);
    [SerializeField] float speed = 1500f;
    [SerializeField] Color dropColor = new Color(0.7f, 0.8f, 1f, 0.55f);

    RectTransform area;
    RectTransform[] drops;
    bool raining;

    Vector2 AreaSize => area.rect.width > 1f ? area.rect.size : new Vector2(1920f, 1080f);

    void Awake()
    {
        area = (RectTransform)transform;
        drops = new RectTransform[dropCount];

        for (int i = 0; i < dropCount; i++)
        {
            var go = new GameObject("Drop", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(area, false);
            rt.sizeDelta = dropSize;
            rt.localRotation = Quaternion.Euler(0f, 0f, 8f);

            var img = go.GetComponent<Image>();
            img.color = dropColor;
            img.raycastTarget = false;

            go.SetActive(false);
            drops[i] = rt;
        }
    }

    public void SetRaining(bool on)
    {
        raining = on;
        foreach (var d in drops) d.gameObject.SetActive(on);
        if (on) Scatter();
    }

    void Scatter()
    {
        var s = AreaSize;
        foreach (var d in drops)
            d.anchoredPosition = new Vector2(Random.Range(-s.x / 2f, s.x / 2f),
                                             Random.Range(-s.y / 2f, s.y / 2f));
    }

    void Update()
    {
        if (!raining) return;

        var s = AreaSize;
        float dy = speed * Time.deltaTime;

        foreach (var d in drops)
        {
            var p = d.anchoredPosition;
            p.y -= dy;
            p.x -= dy * 0.14f;

            if (p.y < -s.y / 2f)
            {
                p.y = s.y / 2f;
                p.x = Random.Range(-s.x / 2f, s.x / 2f);
            }
            if (p.x < -s.x / 2f) p.x += s.x;

            d.anchoredPosition = p;
        }
    }
}
