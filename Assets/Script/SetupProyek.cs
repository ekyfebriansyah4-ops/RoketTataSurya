#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Script editor: membuat scene MainMenu dan mendaftarkan scene ke build.
// Jalankan dari menu atas Unity: Roket Tata Surya > Siapkan Scene Main Menu
public static class SetupProyek
{
    const string PathMenu = "Assets/Scenes/MainMenu.unity";
    const string PathGameplay = "Assets/Scenes/Gameplay.unity";

    [MenuItem("Roket Tata Surya/Siapkan Scene Main Menu")]
    static void SiapkanSceneMenu()
    {
        // Simpan dulu scene yang sedang terbuka
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (!System.IO.File.Exists(PathMenu))
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color32(6, 14, 34, 255);
            }

            EditorSceneManager.SaveScene(scene, PathMenu);
        }

        // Urutan build: MainMenu dulu, lalu Gameplay
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(PathMenu, true),
            new EditorBuildSettingsScene(PathGameplay, true)
        };

        EditorSceneManager.OpenScene(PathMenu);
        Debug.Log("Scene MainMenu siap dan sudah terdaftar di build bersama Gameplay.");
    }
}
#endif