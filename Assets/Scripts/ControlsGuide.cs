using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Overlay hướng dẫn điều khiển khi bắt đầu mới / chơi lại từ đầu.
/// Set <see cref="Pending"/> trước khi LoadScene Main; bấm Tiếp tục để vào chơi.
/// </summary>
public class ControlsGuide : MonoBehaviour
{
    public static bool Pending;
    public static bool IsOpen { get; private set; }

    GameObject root;
    bool built;

    // AfterSceneLoad chi chay 1 lan luc boot — can sceneLoaded moi lan LoadScene
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void HookSceneLoaded()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!Application.isPlaying) return;
        if (!Pending) return;
        if (scene.name != "Main") return;
        if (FindAnyObjectByType<ControlsGuide>() != null) return;

        IsOpen = false;
        var go = new GameObject("ControlsGuide");
        go.AddComponent<ControlsGuide>();
    }

    void Awake()
    {
        // Goi som (truoc Start cua PauseMenu) khi spawn trong sceneLoaded
        if (Pending) Show();
    }

    void LateUpdate()
    {
        if (!IsOpen || root == null) return;
        // Dam bao cac Start() khac khong ghi de pause; giu overlay tren cung
        if (Time.timeScale != 0f)
            Time.timeScale = 0f;
        root.transform.SetAsLastSibling();
    }

    public void Show()
    {
        EnsureUi();
        if (root == null) return;

        Pending = false;
        IsOpen = true;
        root.SetActive(true);
        root.transform.SetAsLastSibling();
        Time.timeScale = 0f;
    }

    public void Continue()
    {
        IsOpen = false;
        if (root != null) root.SetActive(false);
        Time.timeScale = 1f;
    }

    void EnsureUi()
    {
        if (built && root != null) return;
        built = true;

        EnsureEventSystem();

        var canvasGo = GameObject.Find("Canvas");
        if (canvasGo == null)
        {
            canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        var overlay = CreateImage(canvasGo.transform, "ControlsGuideOverlay", new Color(0f, 0f, 0f, 0.62f));
        Stretch(overlay.rectTransform);
        overlay.raycastTarget = true;
        root = overlay.gameObject;

        var boxOuter = CreateImage(overlay.transform, "BoxOuter", new Color(0.45f, 0.26f, 0.10f, 1f));
        boxOuter.rectTransform.anchorMin = boxOuter.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        boxOuter.rectTransform.sizeDelta = new Vector2(640, 560);
        boxOuter.rectTransform.anchoredPosition = Vector2.zero;

        var boxInner = CreateImage(boxOuter.transform, "BoxInner", new Color(0.78f, 0.58f, 0.32f, 1f));
        Stretch(boxInner.rectTransform, 10);

        var face = CreateImage(boxInner.transform, "Face", new Color(0.93f, 0.86f, 0.70f, 1f));
        Stretch(face.rectTransform, 8);

        var title = CreateTmp(face.transform, "Title", "Huong dan dieu khien",
            new Vector2(0, 220), new Vector2(560, 56), 36, true, TextAlignmentOptions.Center);
        title.color = new Color(0.28f, 0.16f, 0.06f, 1f);
        title.raycastTarget = false;

        const string body =
            "<b>WASD</b> / mui ten — Di chuyen\n" +
            "<b>E</b> / Space — Tuong tac (o dat, NPC)\n" +
            "<b>1</b>–<b>6</b> / cuon chuot — Chon cong cu\n" +
            "<b>I</b> — Tui do\n" +
            "<b>O</b> — Don hang\n" +
            "<b>Esc</b> — Tam dung / dong panel\n" +
            "<b>Q</b> — Dong cua hang NPC";

        var list = CreateTmp(face.transform, "ControlsList", body,
            new Vector2(0, 20), new Vector2(540, 340), 26, false, TextAlignmentOptions.Left);
        list.color = new Color(0.28f, 0.16f, 0.06f, 1f);
        list.lineSpacing = 12f;
        list.richText = true;
        list.raycastTarget = false;

        var continueBtn = CreateButton(face.transform, "ContinueButton", "Tiep tuc",
            new Vector2(0, -210), new Vector2(280, 64), new Color(0.22f, 0.55f, 0.30f, 1f));
        continueBtn.onClick.AddListener(Continue);

        root.SetActive(false);
    }

    static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        if (es.GetComponent<InputSystemUIInputModule>() == null)
            es.AddComponent<InputSystemUIInputModule>();
    }

    static Button CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        go.GetComponent<Image>().color = color;

        var tmp = CreateTmp(go.transform, "Text", label, Vector2.zero, Vector2.zero, 30, true, TextAlignmentOptions.Center);
        Stretch(tmp.rectTransform);
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return go.GetComponent<Button>();
    }

    static Image CreateImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        return img;
    }

    static TMP_Text CreateTmp(Transform parent, string name, string text, Vector2 pos, Vector2 size,
        float fontSize, bool bold, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.color = Color.white;
        if (bold) tmp.fontStyle = FontStyles.Bold;
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        return tmp;
    }

    static void Stretch(RectTransform rt, float inset = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
        rt.pivot = new Vector2(0.5f, 0.5f);
    }
}
