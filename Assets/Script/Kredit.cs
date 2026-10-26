using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// Tombol "Tentang" dan panel kredit di Main Menu. Dibuat otomatis.
public class Kredit : MonoBehaviour
{
    // ====== Lengkapi bagian bertanda [ ... ] ======
    const string IsiKredit =
        "<b>Roket Tata Surya</b>  versi 1.0\n" +
        "Game edukasi 3D tentang planet-planet di tata surya.\n\n" +
        "<b>Dikembangkan oleh Kelompok 6</b>\n" +
        "Rafael (2303040005), Anteng (2303040053), Fahmi (2303040100)\n" +
        "Eky (2303040141), Albab (2303040138), Ibrahim (2303040175)\n\n" +
        "<b>Mata kuliah</b>  Pemrograman Mobile Lanjut\n" +
        "<b>Dosen</b>  Achmad Fauzan, S.Kom., M.Cs.\n" +
        "Teknik Informatika, Fakultas Teknik dan Sains\n" +
        "Universitas Muhammadiyah Purwokerto, 2026\n\n" +
        "<b>Dibuat dengan</b>  Unity 6 dan C#. Model 3D, tekstur, dan suara dibuat lewat kode.\n" +
        "<b>Sumber materi</b>  NASA Science - Solar System (science.nasa.gov/solar-system)\n" +
        "<b>Bantuan AI</b>  Claude (Anthropic) dipakai sebagai alat bantu menyusun dan memahami kode.";

    GameObject panelTentang;

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
        if (scene.name == "MainMenu" && FindAnyObjectByType<Kredit>() == null)
            new GameObject("Kredit").AddComponent<Kredit>();
    }

    IEnumerator Start()
    {
        yield return null; // tunggu Main Menu selesai dibuat

        GameObject canvasMenu = GameObject.Find("CanvasMenu");
        if (canvasMenu == null) yield break;
        Transform root = canvasMenu.transform;

        // Tombol Tentang di pojok kanan atas
        Vector2 kananAtas = new Vector2(1f, 1f);
        MenuUtama.BuatTombol(root, "Tentang", kananAtas, new Vector2(-30, -30), new Vector2(220, 70),
            MenuUtama.Biru, MenuUtama.TeksBiruMuda, BukaPanel);

        // Panel kredit
        panelTentang = MenuUtama.BuatPanelLayar(root, "PanelTentang", new Color32(6, 14, 34, 245));
        Vector2 atas = new Vector2(0.5f, 1f);
        Vector2 tengah = new Vector2(0.5f, 0.5f);

        MenuUtama.BuatTeks(panelTentang.transform, "Tentang aplikasi", 64, atas, new Vector2(0, -80),
            new Vector2(1200, 100), Color.white);

        TextMeshProUGUI isi = MenuUtama.BuatTeks(panelTentang.transform, IsiKredit, 30, tengah,
            new Vector2(0, 20), new Vector2(1500, 640), MenuUtama.TeksBiruMuda);
        isi.alignment = TextAlignmentOptions.Left;
        isi.richText = true;

        MenuUtama.BuatTombol(panelTentang.transform, "Tutup", tengah, new Vector2(0, -420), new Vector2(520, 90),
            MenuUtama.Oranye, MenuUtama.TeksOranye, () => panelTentang.SetActive(false));

        panelTentang.SetActive(false);
    }

    void BukaPanel()
    {
        panelTentang.SetActive(true);
        panelTentang.transform.SetAsLastSibling(); // tampil paling atas
    }
}