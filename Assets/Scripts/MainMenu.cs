using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] string gameScene = "Main";
    [SerializeField] Button continueButton;
    [SerializeField] TMP_Text newGameLabel;

    bool confirming;

    void Start()
    {
        Time.timeScale = 1f;
        continueButton.interactable = SaveSystem.Exists;
    }

    public void Continue() => SceneManager.LoadScene(gameScene);

    // Bấm lần 1 để xác nhận, lần 2 mới xoá save
    public void NewGame()
    {
        if (SaveSystem.Exists && !confirming)
        {
            confirming = true;
            if (newGameLabel != null) newGameLabel.text = "Xoá save? Bấm lần nữa";
            return;
        }

        SaveSystem.Delete();
        SceneManager.LoadScene(gameScene);
    }

    public void Quit() => Application.Quit(); // không có tác dụng trong Editor và WebGL
}
