#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ẩn Canvas UI trong Scene View để dễ vẽ Tilemap / không bị chữ to cạnh map.
/// UI vẫn hiện bình thường khi Play / Game View.
/// Menu: FarmSim → Ẩn/Hiện UI trong Scene View
/// </summary>
[InitializeOnLoad]
public static class HideUiInSceneView
{
    const string PrefKey = "FarmSim.HideUiInSceneView";
    const string FlagPath = "Assets/Editor/FORCE_HIDE_UI.flag";

    static HideUiInSceneView()
    {
        EditorSceneManager.sceneOpened += (_, __) => EditorApplication.delayCall += Apply;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.delayCall += Apply;
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        // Enter Play Mode Options (không reload domain) làm Canvas hiện lại sau khi Stop
        if (state == PlayModeStateChange.EnteredEditMode)
            EditorApplication.delayCall += Apply;
    }

    [MenuItem("FarmSim/Ẩn UI trong Scene View", false, 60)]
    public static void Hide()
    {
        EditorPrefs.SetBool(PrefKey, true);
        SetHidden(true);
        Debug.Log("[FarmSim] Đã ẩn Canvas trong Scene View. UI vẫn hiện khi Play.");
    }

    [MenuItem("FarmSim/Hiện UI trong Scene View", false, 61)]
    public static void Show()
    {
        EditorPrefs.SetBool(PrefKey, false);
        SetHidden(false);
        Debug.Log("[FarmSim] Đã hiện lại Canvas trong Scene View.");
    }

    static void Apply()
    {
        if (File.Exists(FlagPath))
        {
            try { File.Delete(FlagPath); } catch { /* ignore */ }
            EditorPrefs.SetBool(PrefKey, true);
            SetHidden(true);
            Debug.Log("[FarmSim] FORCE_HIDE_UI: đã ẩn Canvas trong Scene View.");
            return;
        }

        if (!EditorPrefs.GetBool(PrefKey, true)) return;
        SetHidden(true);
    }

    static void SetHidden(bool hide)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
                {
                    if (hide) SceneVisibilityManager.instance.Hide(canvas.gameObject, true);
                    else SceneVisibilityManager.instance.Show(canvas.gameObject, true);
                }
            }
        }
    }
}
#endif
