using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Menyesuaikan UI dengan safe area layar HP (menghindari poni/lekukan layar).
// Isi setiap Canvas (kecuali panel layar penuh) dipindah ke wadah "AreaAman". Dibuat otomatis.
public class AreaAman : MonoBehaviour
{
    readonly List<RectTransform> daftarWadah = new List<RectTransform>();
    Rect areaTerakhir;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void DaftarOtomatis()
    {
        SceneManager.sceneLoaded -= SaatSceneDimuat;
        SceneManager.sceneLoaded += SaatSceneDimuat;
        BuatJikaPerlu(SceneManager.GetActiveScene());
    }

    static void SaatSceneDimuat(Scene scene, LoadSceneMode mode)
    {
        BuatJikaPerlu(scene);
    }

    static void BuatJikaPerlu(Scene scene)
    {
        if ((scene.name == "Gameplay" || scene.name == "MainMenu") && FindAnyObjectByType<AreaAman>() == null)
            new GameObject("AreaAman").AddComponent<AreaAman>();
    }

    IEnumerator Start()
    {
        // Tunggu dua frame supaya semua UI lain selesai dibuat dulu
        yield return null;
        yield return null;

        Scene aktif = SceneManager.GetActiveScene();
        foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            // Lewati canvas milik scene lain (misalnya tirai Transisi) dan canvas bersarang
            if (c.gameObject.scene != aktif || !c.isRootCanvas) continue;
            BungkusCanvas(c);
        }
        Terapkan();
    }

    void Update()
    {
        // Sesuaikan ulang jika area aman berubah (misalnya layar diputar)
        if (daftarWadah.Count > 0 && Screen.safeArea != areaTerakhir) Terapkan();
    }

    void BungkusCanvas(Canvas canvas)
    {
        Transform root = canvas.transform;

        RectTransform wadah = new GameObject("WadahAreaAman", typeof(RectTransform)).GetComponent<RectTransform>();
        wadah.SetParent(root, false);

        // Pindahkan semua isi canvas ke wadah, kecuali panel layar penuh (nama diawali "Panel")
        List<Transform> anak = new List<Transform>();
        foreach (Transform t in root)
            if (t != wadah && !t.name.StartsWith("Panel")) anak.Add(t);
        foreach (Transform t in anak)
            t.SetParent(wadah, false);

        wadah.SetAsFirstSibling(); // panel tetap tampil di atas
        daftarWadah.Add(wadah);
    }

    void Terapkan()
    {
        Rect area = Screen.safeArea;
        areaTerakhir = area;
        if (Screen.width <= 0 || Screen.height <= 0) return;

        Vector2 min = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
        Vector2 maks = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);

        foreach (RectTransform w in daftarWadah)
        {
            if (w == null) continue;
            w.anchorMin = min;
            w.anchorMax = maks;
            w.offsetMin = Vector2.zero;
            w.offsetMax = Vector2.zero;
        }
    }
}