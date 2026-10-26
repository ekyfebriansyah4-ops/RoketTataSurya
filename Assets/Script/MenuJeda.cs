using UnityEngine;
using UnityEngine.SceneManagement;

// Tombol Jeda dan panel jeda di Gameplay. Dibuat otomatis.
public class MenuJeda : MonoBehaviour
{
    GameObject panelJeda;

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
        if (scene.name == "Gameplay" && FindAnyObjectByType<MenuJeda>() == null)
        {
            new GameObject("MenuJeda").AddComponent<MenuJeda>();
        }
    }

    void Start()
    {
        MenuUtama.SiapkanEventSystem();

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) canvas = MenuUtama.BuatCanvas("CanvasJeda");
        Transform root = canvas.transform;

        // Tombol Jeda di kanan atas, di bawah tulisan skor
        MenuUtama.BuatTombol(root, "Jeda", new Vector2(1f, 1f), new Vector2(-30, -90), new Vector2(180, 70),
            MenuUtama.Biru, MenuUtama.TeksBiruMuda, Jeda);

        // Panel jeda
        panelJeda = MenuUtama.BuatPanelLayar(root, "PanelJeda", new Color(0f, 0f, 0f, 0.75f));
        Vector2 tengah = new Vector2(0.5f, 0.5f);
        MenuUtama.BuatTeks(panelJeda.transform, "Permainan dijeda", 64, tengah, new Vector2(0, 160),
            new Vector2(1000, 100), Color.white);
        MenuUtama.BuatTombol(panelJeda.transform, "Lanjutkan", tengah, new Vector2(0, 20), new Vector2(520, 90),
            MenuUtama.Oranye, MenuUtama.TeksOranye, Lanjutkan);
        MenuUtama.BuatTombol(panelJeda.transform, "Menu utama", tengah, new Vector2(0, -100), new Vector2(520, 90),
            MenuUtama.Biru, MenuUtama.TeksBiruMuda, KeMenu);
        panelJeda.SetActive(false);
    }

    void Jeda()
    {
        Time.timeScale = 0f; // semua gerakan berbasis waktu berhenti
        panelJeda.SetActive(true);
    }

    void Lanjutkan()
    {
        Time.timeScale = 1f;
        panelJeda.SetActive(false);
    }

    void KeMenu()
    {
        Time.timeScale = 1f;
        Transisi.Muat("MainMenu");
    }

    void OnDestroy()
    {
        Time.timeScale = 1f; // pastikan waktu normal kembali saat pindah scene
    }
}