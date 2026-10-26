#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Script editor. Jalankan dari menu atas Unity: Roket Tata Surya
public static class SetupProyek
{
    const string PathMenu = "Assets/Scenes/MainMenu.unity";
    const string PathGameplay = "Assets/Scenes/Gameplay.unity";
    const string FolderIkon = "Assets/Ikon";
    const string PathIkon = "Assets/Ikon/IkonAplikasi.png";

    [MenuItem("Roket Tata Surya/Siapkan Scene Main Menu")]
    static void SiapkanSceneMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (!File.Exists(PathMenu))
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

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(PathMenu, true),
            new EditorBuildSettingsScene(PathGameplay, true)
        };

        EditorSceneManager.OpenScene(PathMenu);
        Debug.Log("Scene MainMenu siap dan sudah terdaftar di build bersama Gameplay.");
    }

    [MenuItem("Roket Tata Surya/Atur Ikon dan Identitas Aplikasi")]
    static void AturIkonDanIdentitas()
    {
        // 1. Gambar ikon lewat kode, simpan sebagai PNG
        Directory.CreateDirectory(FolderIkon);
        Texture2D gambar = GambarIkon(512);
        File.WriteAllBytes(PathIkon, gambar.EncodeToPNG());
        Object.DestroyImmediate(gambar);
        AssetDatabase.ImportAsset(PathIkon);
        Texture2D ikon = AssetDatabase.LoadAssetAtPath<Texture2D>(PathIkon);

        // 2. Identitas aplikasi
        PlayerSettings.companyName = "Kelompok 6";
        PlayerSettings.productName = "Roket Tata Surya";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.kelompok6.rokettatasurya");

        // 3. Pasang ikon sebagai ikon bawaan aplikasi
        PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { ikon }, IconKind.Any);

        AssetDatabase.SaveAssets();
        EditorGUIUtility.PingObject(ikon);
        Debug.Log("Ikon, nama aplikasi, dan package name sudah diatur.");
    }

    // Planet oranye bercincin di langit malam berbintang
    static Texture2D GambarIkon(int n)
    {
        Color latar = new Color32(6, 14, 34, 255);
        Color warnaPlanet = new Color32(239, 159, 39, 255);
        Color warnaCincin = new Color32(250, 220, 160, 255);

        Vector2 pusat = new Vector2(n * 0.5f, n * 0.48f);
        float r = n * 0.24f;
        Vector2 titikTerang = pusat + new Vector2(-r * 0.4f, r * 0.4f);
        float sudut = -20f * Mathf.Deg2Rad;
        float rx = r * 1.9f;
        float ry = r * 0.55f;

        Color[] piksel = new Color[n * n];
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                Vector2 d = p - pusat;

                // Koordinat cincin yang dimiringkan
                float u = d.x * Mathf.Cos(sudut) - d.y * Mathf.Sin(sudut);
                float v = d.x * Mathf.Sin(sudut) + d.y * Mathf.Cos(sudut);
                float e = (u * u) / (rx * rx) + (v * v) / (ry * ry);
                bool diCincin = e > 0.72f && e < 1f;

                Color c = latar;
                if (diCincin && v > 0f) c = warnaCincin; // cincin bagian belakang

                if (d.magnitude < r)
                {
                    float terang = 1f - 0.45f * Mathf.Clamp01(Vector2.Distance(p, titikTerang) / (r * 2f));
                    c = warnaPlanet * terang;
                    c.a = 1f;
                }

                if (diCincin && v <= 0f) c = warnaCincin; // cincin bagian depan

                piksel[y * n + x] = c;
            }
        }

        // Bintang-bintang kecil
        System.Random acak = new System.Random(7);
        for (int i = 0; i < 45; i++)
        {
            int sx = acak.Next(4, n - 4);
            int sy = acak.Next(4, n - 4);
            int ukuran = acak.Next(1, 3);
            for (int oy = -ukuran; oy <= ukuran; oy++)
                for (int ox = -ukuran; ox <= ukuran; ox++)
                {
                    int idx = (sy + oy) * n + (sx + ox);
                    if (piksel[idx] == latar) piksel[idx] = Color.white;
                }
        }

        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.SetPixels(piksel);
        tex.Apply();
        return tex;
    }
}
#endif