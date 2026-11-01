using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] SaveManager saveManager;
    [SerializeField] string menuScene = "MainMenu";

    public bool IsPaused { get; private set; }
    DateTime pausedAt;

    void Start()
    {
        Time.timeScale = 1f;
        panel.SetActive(false);
    }

    void Update()
    {
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
        panel.SetActive(true);
        saveManager.Save();
    }

    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;
        // Cây và gà tính theo giờ thật, nên trừ đi thời gian đã tạm dừng
        float paused = (float)(DateTime.UtcNow - pausedAt).TotalSeconds;
        Growables.Shift(-paused);
        Time.timeScale = 1f;
        panel.SetActive(false);
    }

    public void BackToMenu()
    {
        Resume();
        saveManager.Save();
        SceneManager.LoadScene(menuScene);
    }

    public void Quit()
    {
        Resume();
        saveManager.Save();
        Application.Quit();
    }
}
