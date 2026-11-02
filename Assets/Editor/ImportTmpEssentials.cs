#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class ImportTmpEssentials
{
    const string Flag = "Assets/Editor/IMPORT_TMP.flag";

    [InitializeOnLoadMethod]
    static void AutoImport()
    {
        if (!File.Exists(Flag)) return;

        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(Flag)) return;
            try { File.Delete(Flag); } catch { /* ignore */ }

            if (File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset"))
            {
                Debug.Log("[FarmSim] TMP Essential Resources đã có sẵn.");
                return;
            }

            Debug.Log("[FarmSim] Đang import TMP Essential Resources...");
            TMP_PackageResourceImporter.ImportResources(true, false, false);
            Debug.Log("[FarmSim] Đã gọi import TMP Essential Resources. Chờ Unity import xong rồi Play lại.");
        };
    }

    [MenuItem("FarmSim/Import TMP Essential Resources", false, 50)]
    public static void ImportFromMenu()
    {
        TMP_PackageResourceImporter.ImportResources(true, false, false);
        EditorUtility.DisplayDialog("TMP", "Đã import TMP Essential Resources.\nĐợi Unity xử lý xong rồi bấm Play.", "OK");
    }
}
#endif
