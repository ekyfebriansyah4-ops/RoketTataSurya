using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

// Membangun tampilan Main Menu lewat kode. Dibuat otomatis di scene MainMenu.
public class MenuUtama : MonoBehaviour
{
    GameObject panelCaraBermain;

    static readonly Color32 Oranye = new Color32(239, 159, 39, 255);
    static readonly Color32 TeksOranye = new Color32(65, 36, 2, 255);
    static readonly Color32 Biru = new Color32(24, 95, 165, 255);
    static readonly Color32 TeksBiruMuda = new Color32(230, 241, 251, 255);

    // ====== Membuat objek ini otomatis di scene MainMenu ======
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
        if (scene.name == "MainMenu" && FindAnyObjectByType<MenuUtama>() == null)
        {
            new GameObject("MenuUtama").AddComponent<MenuUtama>();
        }
    }

    // ====== Membangun tampilan ======
    void Start()
    {
        SiapkanEventSystem();
        Transform root = BuatCanvas().transform;

        BuatTeks(root, "ROKET TATA SURYA", 96, new Vector2(0.5f, 1f), new Vector2(0, -150),
            new Vector2(1600, 140), Color.white);
        BuatTeks(root, "Terbangkan roketmu dari Merkurius sampai Neptunus\nsambil menjawab soal tentang tata surya",
            34, new Vector2(0.5f, 1f), new Vector2(0, -300), new Vector2(1500, 120), new Color32(181, 212, 244, 255));

                int tertinggi = PlayerPrefs.GetInt(GameManager.KunciSkorTertinggi, 0);
        BuatTeks(root, "Skor tertinggi: " + tertinggi, 36, new Vector2(0.5f, 0.5f), new Vector2(0, 60),
            new Vector2(800, 60), new Color32(250, 199, 117, 255));
        BuatTombol(root, "Cara bermain", new Vector2(0, -180), Biru, TeksBiruMuda, () => panelCaraBermain.SetActive(true));
        BuatTombol(root, "Keluar", new Vector2(0, -300), Biru, TeksBiruMuda, Keluar);

        BuatTeks(root, "Kelompok 6 - Pemrograman Mobile Lanjut", 24, new Vector2(0.5f, 0f), new Vector2(0, 30),
            new Vector2(1200, 50), new Color32(136, 135, 128, 255));

        // Dibuat terakhir supaya tampil paling atas saat dibuka
        panelCaraBermain = BuatPanelCaraBermain(root);
        panelCaraBermain.SetActive(false);
    }

    // ====== Aksi tombol ======
    void Mulai()
    {
        SceneManager.LoadScene("Gameplay");
    }

    void Keluar()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ====== Pembuat elemen UI ======
    void SiapkanEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;
        GameObject es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }

    Canvas BuatCanvas()
    {
        GameObject go = new GameObject("CanvasMenu");
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    TextMeshProUGUI BuatTeks(Transform parent, string isi, float ukuran, Vector2 anchor,
                             Vector2 posisi, Vector2 ukuranKotak, Color warna)
    {
        GameObject go = new GameObject("Teks", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.text = isi;
        t.fontSize = ukuran;
        t.color = warna;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;

        RectTransform rt = t.rectTransform;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = posisi;
        rt.sizeDelta = ukuranKotak;
        return t;
    }

    void BuatTombol(Transform parent, string label, Vector2 posisi, Color warnaLatar,
                    Color warnaTeks, UnityAction aksi)
    {
        GameObject go = new GameObject("Tombol_" + label, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        Image img = go.AddComponent<Image>();
        img.color = warnaLatar;

        Button tombol = go.AddComponent<Button>();
        tombol.targetGraphic = img;
        tombol.onClick.AddListener(aksi);

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = posisi;
        rt.sizeDelta = new Vector2(520, 100);

        BuatTeks(go.transform, label, 40, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 100), warnaTeks);
    }

    GameObject BuatPanelCaraBermain(Transform parent)
    {
        GameObject panel = new GameObject("PanelCaraBermain", typeof(RectTransform));
        panel.transform.SetParent(parent, false);

        Image latar = panel.AddComponent<Image>();
        latar.color = new Color32(6, 14, 34, 240);

        RectTransform rt = (RectTransform)panel.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        BuatTeks(panel.transform, "Cara bermain", 64, new Vector2(0.5f, 1f), new Vector2(0, -100),
            new Vector2(1200, 100), Color.white);

        TextMeshProUGUI isi = BuatTeks(panel.transform,
            "1. Sentuh dan geser layar untuk mengarahkan roket.\n" +
            "2. Baca soal yang muncul di bagian atas layar.\n" +
            "3. Terbangkan roket menembus cincin berisi jawaban yang benar.\n" +
            "4. Hindari meteor yang datang dari depan.\n" +
            "5. Jawaban benar membawamu ke planet berikutnya.\n" +
            "6. Salah menjawab atau menabrak meteor mengurangi nyawa.\n\n" +
            "Capai Neptunus untuk menyelesaikan misi!",
            34, new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(1300, 560), TeksBiruMuda);
        isi.alignment = TextAlignmentOptions.Left;

        BuatTombol(panel.transform, "Tutup", new Vector2(0, -400), Oranye, TeksOranye,
            () => panelCaraBermain.SetActive(false));

        return panel;
    }
}
