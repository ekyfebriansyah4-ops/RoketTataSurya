using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Menangani tombol Back Android (sama dengan tombol Esc di keyboard). Dibuat otomatis.
public class TombolKembali : MonoBehaviour
{
    // Panel di menu yang ditutup saat tombol Back ditekan (urutan = prioritas)
    static readonly string[] panelMenu = { "PanelTentang", "PanelPilihRoket", "PanelBelajar", "PanelCaraBermain" };

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
        if ((scene.name == "Gameplay" || scene.name == "MainMenu") && FindAnyObjectByType<TombolKembali>() == null)
            new GameObject("TombolKembali").AddComponent<TombolKembali>();
    }

    void Update()
    {
        if (!TombolKembaliDitekan()) return;

        if (SceneManager.GetActiveScene().name == "Gameplay")
            KembaliDiGameplay();
        else
            KembaliDiMenu();
    }

    void KembaliDiGameplay()
    {
        // Game sudah selesai: kembali ke menu
        if (GameObject.Find("PanelSelesai") != null)
        {
            Transisi.Muat("MainMenu");
            return;
        }

        // Sedang dijeda: lanjutkan. Sedang bermain: jeda.
        if (GameObject.Find("PanelJeda") != null)
            KlikTombol("Tombol_Lanjutkan");
        else
            KlikTombol("Tombol_Jeda");
    }

    void KembaliDiMenu()
    {
        // Tutup panel yang sedang terbuka
        foreach (string nama in panelMenu)
        {
            GameObject panel = GameObject.Find(nama); // hanya menemukan yang aktif
            if (panel != null)
            {
                panel.SetActive(false);
                return;
            }
        }

        // Tidak ada panel terbuka: keluar aplikasi
#if UNITY_EDITOR
        Debug.Log("Tombol Back: aplikasi akan keluar (hanya berfungsi di HP).");
#else
        Application.Quit();
#endif
    }

    void KlikTombol(string nama)
    {
        GameObject g = GameObject.Find(nama);
        if (g == null) return;
        Button b = g.GetComponent<Button>();
        if (b != null) b.onClick.Invoke();
    }

    bool TombolKembaliDitekan()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }
}