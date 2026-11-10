using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] SaveManager saveManager;
    [SerializeField] string menuScene = "MainMenu";
    [SerializeField] Button resumeButton;
    [SerializeField] Button restartButton;
    [SerializeField] Button menuButton;
    [SerializeField] Button quitButton;
    [SerializeField] GameObject confirmPanel;
    [SerializeField] Button confirmYesButton;
    [SerializeField] Button confirmNoButton;

    public bool IsPaused { get; private set; }
    DateTime pausedAt;

    void Start()
    {
        Time.timeScale = 1f;
        if (panel != null) panel.SetActive(false);
        SetupPauseUi();
        HideConfirm();
    }

    void Update()
    {
        // Dang noi chuyen NPC: Esc chi dong shop, khong mo Pause
        if (ShopUI.IsOpen) return;
        if (Input.GetKeyDown(KeyCode.Escape)) Toggle();
    }

    public void Toggle()
    {
        if (IsPaused) Resume(); else Pause();
    }

    public void Pause()
    {
        if (IsPaused) return;
        IsPaused = true;
        pausedAt = DateTime.UtcNow;
        Time.timeScale = 0f;
        if (panel != null) panel.SetActive(true);
        HideConfirm();
        if (saveManager != null) saveManager.Save();
    }

    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;
        float paused = (float)(DateTime.UtcNow - pausedAt).TotalSeconds;
        Growables.Shift(-paused);
        Time.timeScale = 1f;
        HideConfirm();
        if (panel != null) panel.SetActive(false);
    }

    public void BackToMenu()
    {
        Resume();
        if (saveManager != null) saveManager.Save();
        if (Application.CanStreamedLevelBeLoaded(menuScene))
            SceneManager.LoadScene(menuScene);
        else
            Debug.LogError($"PauseMenu: scene '{menuScene}' chua co trong Build Settings.");
    }

    public void OnRestartClicked()
    {
        EnsureConfirmPanel();
        if (confirmPanel != null)
            confirmPanel.SetActive(true);
        else
            ConfirmRestart();
    }

    public void ConfirmRestart()
    {
        HideConfirm();
        Time.timeScale = 1f;
        IsPaused = false;
        if (saveManager != null)
            saveManager.ResetSave();
        else
        {
            SaveSystem.Delete();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    public void CancelRestart() => HideConfirm();

    // Giữ method de scene/setup cu khong loi compile (nut Thoat da an)
    public void Quit()
    {
        Resume();
        if (saveManager != null) saveManager.Save();
        Application.Quit();
    }

    void HideConfirm()
    {
        if (confirmPanel != null)
            confirmPanel.SetActive(false);
    }

    void SetupPauseUi()
    {
        if (panel == null) return;

        if (resumeButton == null)
        {
            var t = panel.transform.Find("ResumeButton");
            if (t != null) resumeButton = t.GetComponent<Button>();
        }
        if (menuButton == null)
        {
            var t = panel.transform.Find("MenuButton");
            if (t != null) menuButton = t.GetComponent<Button>();
        }
        if (quitButton == null)
        {
            var t = panel.transform.Find("QuitButton");
            if (t != null) quitButton = t.GetComponent<Button>();
        }

        // Xoa / an nut Thoat trong pause
        if (quitButton != null)
            quitButton.gameObject.SetActive(false);

        // Tao nut Choi lai ngay duoi Tiep tuc
        if (restartButton == null)
        {
            var existing = panel.transform.Find("RestartButton");
            if (existing != null)
                restartButton = existing.GetComponent<Button>();
            else if (resumeButton != null)
                restartButton = CloneButton(resumeButton, "RestartButton", "Choi lai", new Vector2(0, -40));
        }

        // Day "Ve menu" xuong duoi Choi lai
        if (menuButton != null)
        {
            var rt = menuButton.GetComponent<RectTransform>();
            if (rt != null) rt.anchoredPosition = new Vector2(0, -120);
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(Resume);
        }
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(OnRestartClicked);
            restartButton.gameObject.SetActive(true);
        }
        if (menuButton != null)
        {
            menuButton.onClick.RemoveAllListeners();
            menuButton.onClick.AddListener(BackToMenu);
        }

        EnsureConfirmPanel();
        WireConfirmButtons();
    }

    Button CloneButton(Button source, string name, string label, Vector2 anchoredPos)
    {
        var go = Instantiate(source.gameObject, panel.transform);
        go.name = name;
        var rt = go.GetComponent<RectTransform>();
        if (rt != null) rt.anchoredPosition = anchoredPos;

        var tmp = go.GetComponentInChildren<TMP_Text>();
        if (tmp != null)
        {
            tmp.text = label;
            tmp.raycastTarget = false;
        }

        var btn = go.GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        return btn;
    }

    void EnsureConfirmPanel()
    {
        if (confirmPanel != null)
        {
            WireConfirmButtons();
            return;
        }

        // Dat tren Canvas (khong nam trong PausePanel dang scale time) — van OK khi timeScale=0
        var canvas = panel != null ? panel.transform.root : null;
        if (canvas == null) return;

        var overlayGo = new GameObject("PauseRestartConfirm", typeof(RectTransform), typeof(Image));
        overlayGo.transform.SetParent(canvas, false);
        var overlayRt = overlayGo.GetComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;
        var overlayImg = overlayGo.GetComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.55f);
        overlayImg.raycastTarget = true;
        confirmPanel = overlayGo;

        var box = new GameObject("Box", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(overlayGo.transform, false);
        var boxRt = box.GetComponent<RectTransform>();
        boxRt.anchorMin = boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        boxRt.sizeDelta = new Vector2(520, 280);
        box.GetComponent<Image>().color = new Color(0.45f, 0.26f, 0.10f, 1f);

        var face = new GameObject("Face", typeof(RectTransform), typeof(Image));
        face.transform.SetParent(box.transform, false);
        var faceRt = face.GetComponent<RectTransform>();
        faceRt.anchorMin = Vector2.zero;
        faceRt.anchorMax = Vector2.one;
        faceRt.offsetMin = new Vector2(8, 8);
        faceRt.offsetMax = new Vector2(-8, -8);
        face.GetComponent<Image>().color = new Color(0.93f, 0.86f, 0.70f, 1f);

        var msg = CreateTmp(face.transform, "Message",
            "Choi lai se mat het du lieu.\nBan co muon tiep tuc?",
            new Vector2(0, 50), new Vector2(460, 120), 28);
        msg.color = new Color(0.28f, 0.16f, 0.06f, 1f);
        msg.raycastTarget = false;

        confirmYesButton = CreateButton(face.transform, "ConfirmYes", "Co",
            new Vector2(-110, -70), new Vector2(160, 56), new Color(0.55f, 0.22f, 0.18f, 1f));
        confirmNoButton = CreateButton(face.transform, "ConfirmNo", "Khong",
            new Vector2(110, -70), new Vector2(160, 56), new Color(0.22f, 0.55f, 0.30f, 1f));

        WireConfirmButtons();
        confirmPanel.SetActive(false);
    }

    void WireConfirmButtons()
    {
        if (confirmPanel != null && (confirmYesButton == null || confirmNoButton == null))
        {
            foreach (var b in confirmPanel.GetComponentsInChildren<Button>(true))
            {
                if (b.name == "ConfirmYes") confirmYesButton = b;
                else if (b.name == "ConfirmNo") confirmNoButton = b;
            }
        }

        if (confirmYesButton != null)
        {
            confirmYesButton.onClick.RemoveAllListeners();
            confirmYesButton.onClick.AddListener(ConfirmRestart);
        }
        if (confirmNoButton != null)
        {
            confirmNoButton.onClick.RemoveAllListeners();
            confirmNoButton.onClick.AddListener(CancelRestart);
        }
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
        var tmp = CreateTmp(go.transform, "Text", label, Vector2.zero, Vector2.zero, 28);
        tmp.rectTransform.anchorMin = Vector2.zero;
        tmp.rectTransform.anchorMax = Vector2.one;
        tmp.rectTransform.offsetMin = Vector2.zero;
        tmp.rectTransform.offsetMax = Vector2.zero;
        tmp.raycastTarget = false;
        return go.GetComponent<Button>();
    }

    static TMP_Text CreateTmp(Transform parent, string name, string text, Vector2 pos, Vector2 size, float fontSize)
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
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        return tmp;
    }
}
