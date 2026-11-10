using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[ExecuteAlways]
public class MainMenu : MonoBehaviour
{
    [SerializeField] string gameScene = "Main";
    [SerializeField] Button continueButton;
    [SerializeField] Button newGameButton;
    [SerializeField] Button quitButton;
    [SerializeField] TMP_Text newGameLabel;
    [SerializeField] Sprite backgroundSprite;
    [SerializeField] GameObject confirmPanel;
    [SerializeField] Button confirmYesButton;
    [SerializeField] Button confirmNoButton;

    bool built;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoSpawnIfNeeded()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.name != "MainMenu") return;
        if (FindAnyObjectByType<MainMenu>() != null) return;

        var root = new GameObject("MainMenuRoot");
        var menu = root.AddComponent<MainMenu>();
        menu.BuildDefaultUi();
    }

    public void Configure(Button continueBtn, TMP_Text newGameText, string sceneName)
    {
        continueButton = continueBtn;
        newGameLabel = newGameText;
        if (!string.IsNullOrEmpty(sceneName)) gameScene = sceneName;
        RefreshContinue();
    }

    void Awake()
    {
        if (continueButton == null)
            BuildDefaultUi();
        EnsureConfirmPanel();
        HideConfirm();
    }

    void OnEnable()
    {
        if (Application.isPlaying)
        {
            RefreshContinue();
            return;
        }

        // Edit mode: build UI neu thieu
        if (continueButton == null)
            BuildDefaultUi();
    }

    void Start()
    {
        Time.timeScale = 1f;
        WireButtons();
        RefreshContinue();
        HideConfirm();
    }

    void RefreshContinue()
    {
        if (continueButton == null) return;

        bool hasSave = SaveSystem.Exists;

        // Lan dau: "Bat dau choi"; da choi: "Tiep tuc"
        continueButton.gameObject.SetActive(true);
        continueButton.interactable = true;
        var label = continueButton.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            label.text = hasSave ? "Tiep tuc" : "Bat dau choi";

        // Lan dau khong can "Choi moi" (chua co du lieu de xoa)
        if (newGameButton != null)
            newGameButton.gameObject.SetActive(hasSave);
    }

    void WireButtons()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(Continue);
        }
        if (newGameButton != null)
        {
            newGameButton.onClick.RemoveAllListeners();
            newGameButton.onClick.AddListener(OnNewGameClicked);
        }
        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(Quit);
        }

        WireConfirmButtons();
    }

    void WireConfirmButtons()
    {
        // includeInactive: panel thuong dang tat, GameObject.Find se miss
        if (confirmPanel != null)
        {
            if (confirmYesButton == null || confirmNoButton == null)
            {
                var buttons = confirmPanel.GetComponentsInChildren<Button>(true);
                foreach (var b in buttons)
                {
                    if (b.name == "ConfirmYes") confirmYesButton = b;
                    else if (b.name == "ConfirmNo") confirmNoButton = b;
                }
            }
        }

        if (confirmYesButton != null)
        {
            confirmYesButton.onClick.RemoveAllListeners();
            confirmYesButton.onClick.AddListener(ConfirmNewGame);
        }
        if (confirmNoButton != null)
        {
            confirmNoButton.onClick.RemoveAllListeners();
            confirmNoButton.onClick.AddListener(CancelNewGame);
        }
    }

    public void Continue() => SceneManager.LoadScene(gameScene);

    public void OnNewGameClicked()
    {
        if (!SaveSystem.Exists)
        {
            StartNewGame();
            return;
        }

        EnsureConfirmPanel();
        if (confirmPanel != null)
            confirmPanel.SetActive(true);
        else
            StartNewGame();
    }

    public void ConfirmNewGame() => StartNewGame();

    public void CancelNewGame() => HideConfirm();

    void StartNewGame()
    {
        HideConfirm();
        SaveSystem.Delete();
        SceneManager.LoadScene(gameScene);
    }

    void HideConfirm()
    {
        if (confirmPanel != null)
            confirmPanel.SetActive(false);
    }

    public void Quit() => Application.Quit();

    public void BuildDefaultUi()
    {
        if (built) return;
        built = true;

        EnsureEventSystem();

        var cam = Camera.main;
        if (cam != null)
        {
            cam.orthographic = true;
            cam.backgroundColor = new Color(0.45f, 0.72f, 0.95f, 1f);
            cam.clearFlags = CameraClearFlags.SolidColor;
        }

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

        var canvasTf = canvasGo.transform;

        var bg = CreateImage(canvasTf, "Background", new Color(0.55f, 0.78f, 0.42f, 1f));
        Stretch(bg.rectTransform);
        if (backgroundSprite != null)
        {
            bg.sprite = backgroundSprite;
            bg.type = Image.Type.Simple;
            bg.preserveAspect = false;
            bg.color = Color.white;
        }
        else
        {
            bg.color = new Color(0.55f, 0.78f, 0.42f, 1f);
            var sky = CreateImage(canvasTf, "SkyBand", new Color(0.55f, 0.78f, 0.95f, 1f));
            sky.rectTransform.anchorMin = new Vector2(0, 0.45f);
            sky.rectTransform.anchorMax = Vector2.one;
            sky.rectTransform.offsetMin = Vector2.zero;
            sky.rectTransform.offsetMax = Vector2.zero;
            sky.raycastTarget = false;
        }

        var dim = CreateImage(canvasTf, "Dim", new Color(0f, 0f, 0f, 0.28f));
        Stretch(dim.rectTransform);
        dim.raycastTarget = false;

        var panelOuter = CreateImage(canvasTf, "PanelOuter", new Color(0.45f, 0.26f, 0.10f, 0.98f));
        panelOuter.rectTransform.anchorMin = panelOuter.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        panelOuter.rectTransform.sizeDelta = new Vector2(560, 480);
        panelOuter.rectTransform.anchoredPosition = new Vector2(0, -20);

        var panelInner = CreateImage(panelOuter.transform, "PanelInner", new Color(0.78f, 0.58f, 0.32f, 1f));
        Stretch(panelInner.rectTransform, 10);

        var panelFace = CreateImage(panelInner.transform, "PanelFace", new Color(0.93f, 0.86f, 0.70f, 1f));
        Stretch(panelFace.rectTransform, 8);

        var title = CreateTmp(panelFace.transform, "Title", "Farm Simulator",
            new Vector2(0, 160), new Vector2(480, 80), 52, true);
        title.color = new Color(0.28f, 0.16f, 0.06f, 1f);

        var subtitle = CreateTmp(panelFace.transform, "Subtitle", "Nong trai nho 2D",
            new Vector2(0, 105), new Vector2(480, 40), 24, false);
        subtitle.color = new Color(0.40f, 0.28f, 0.14f, 1f);

        continueButton = CreateMenuButton(panelFace.transform, "ContinueButton", "Bat dau choi",
            new Vector2(0, 30), new Color(0.22f, 0.55f, 0.30f, 1f));
        newGameButton = CreateMenuButton(panelFace.transform, "NewGameButton", "Choi moi",
            new Vector2(0, -55), new Color(0.22f, 0.55f, 0.30f, 1f));
        quitButton = CreateMenuButton(panelFace.transform, "QuitButton", "Thoat",
            new Vector2(0, -140), new Color(0.55f, 0.32f, 0.18f, 1f));

        newGameLabel = newGameButton.GetComponentInChildren<TMP_Text>();
        EnsureConfirmPanel();
        RefreshContinue();
        WireButtons();
    }

    void EnsureConfirmPanel()
    {
        if (confirmPanel == null)
        {
            // Tim ca object dang inactive
            var canvas = GameObject.Find("Canvas");
            if (canvas != null)
            {
                var t = canvas.transform.Find("NewGameConfirm");
                if (t != null) confirmPanel = t.gameObject;
            }
        }

        if (confirmPanel != null)
        {
            WireConfirmButtons();
            return;
        }

        var canvasGo = GameObject.Find("Canvas");
        if (canvasGo == null) return;

        // Full-screen blocker
        var overlay = CreateImage(canvasGo.transform, "NewGameConfirm", new Color(0f, 0f, 0f, 0.55f));
        Stretch(overlay.rectTransform);
        overlay.raycastTarget = true;
        confirmPanel = overlay.gameObject;

        var boxOuter = CreateImage(overlay.transform, "BoxOuter", new Color(0.45f, 0.26f, 0.10f, 1f));
        boxOuter.rectTransform.anchorMin = boxOuter.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        boxOuter.rectTransform.sizeDelta = new Vector2(520, 280);
        boxOuter.rectTransform.anchoredPosition = Vector2.zero;

        var boxInner = CreateImage(boxOuter.transform, "BoxInner", new Color(0.93f, 0.86f, 0.70f, 1f));
        Stretch(boxInner.rectTransform, 8);

        var msg = CreateTmp(boxInner.transform, "Message",
            "Choi moi se mat het du lieu.\nBan co muon tiep tuc?",
            new Vector2(0, 50), new Vector2(460, 120), 28, true);
        msg.color = new Color(0.28f, 0.16f, 0.06f, 1f);
        msg.raycastTarget = false;

        confirmYesButton = CreateMenuButton(boxInner.transform, "ConfirmYes", "Co",
            new Vector2(-110, -70), new Color(0.55f, 0.22f, 0.18f, 1f));
        confirmYesButton.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 56);

        confirmNoButton = CreateMenuButton(boxInner.transform, "ConfirmNo", "Khong",
            new Vector2(110, -70), new Color(0.22f, 0.55f, 0.30f, 1f));
        confirmNoButton.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 56);

        WireConfirmButtons();
        confirmPanel.SetActive(false);
    }

    static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        if (es.GetComponent<InputSystemUIInputModule>() == null)
            es.AddComponent<InputSystemUIInputModule>();
    }

    static Button CreateMenuButton(Transform parent, string name, string label, Vector2 pos, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(360, 64);
        go.GetComponent<Image>().color = color;
        var tmp = CreateTmp(go.transform, "Text", label, Vector2.zero, Vector2.zero, 30, true);
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

    static TMP_Text CreateTmp(Transform parent, string name, string text, Vector2 pos, Vector2 size, float fontSize, bool bold)
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
        tmp.alignment = TextAlignmentOptions.Center;
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
