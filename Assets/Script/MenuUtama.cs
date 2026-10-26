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
    public static readonly Color32 Oranye = new Color32(239, 159, 39, 255);
    public static readonly Color32 TeksOranye = new Color32(65, 36, 2, 255);
    public static readonly Color32 Biru = new Color32(24, 95, 165, 255);
    public static readonly Color32 TeksBiruMuda = new Color32(230, 241, 251, 255);
    public static readonly Color32 Kuning = new Color32(250, 199, 117, 255);

    static readonly Vector2 Atas = new Vector2(0.5f, 1f);
    static readonly Vector2 Tengah = new Vector2(0.5f, 0.5f);
    static readonly Vector2 Bawah = new Vector2(0.5f, 0f);

    struct InfoPlanet
    {
        public string nama;
        public Color32 warna;
        public string isi;
    }

    static readonly InfoPlanet[] dataPlanet =
    {
        new InfoPlanet { nama = "Merkurius", warna = new Color32(180, 178, 169, 255), isi =
            "• Planet terkecil dan paling dekat dengan Matahari.\n" +
            "• Hampir tidak punya atmosfer, sehingga siang sangat panas dan malam sangat dingin.\n" +
            "• Satu tahun di Merkurius hanya 88 hari Bumi.\n" +
            "• Tidak memiliki bulan." },
        new InfoPlanet { nama = "Venus", warna = new Color32(239, 159, 39, 255), isi =
            "• Planet paling panas, suhunya lebih dari 460°C.\n" +
            "• Atmosfer karbon dioksida yang tebal memerangkap panas Matahari.\n" +
            "• Berotasi berlawanan arah dengan kebanyakan planet.\n" +
            "• Tampak terang di langit pagi atau senja, disebut bintang kejora." },
        new InfoPlanet { nama = "Bumi", warna = new Color32(55, 138, 221, 255), isi =
            "• Planet ketiga dari Matahari.\n" +
            "• Satu-satunya planet yang diketahui memiliki kehidupan.\n" +
            "• Sekitar 71% permukaannya tertutup air.\n" +
            "• Memiliki satu satelit alami, yaitu Bulan." },
        new InfoPlanet { nama = "Mars", warna = new Color32(216, 90, 48, 255), isi =
            "• Dijuluki planet merah karena permukaannya mengandung karat besi.\n" +
            "• Memiliki Olympus Mons, gunung tertinggi di tata surya.\n" +
            "• Punya dua bulan kecil: Phobos dan Deimos." },
        new InfoPlanet { nama = "Jupiter", warna = new Color32(186, 117, 23, 255), isi =
            "• Planet terbesar di tata surya dan termasuk raksasa gas.\n" +
            "• Bintik Merah Besar adalah badai raksasa yang berlangsung ratusan tahun.\n" +
            "• Memiliki puluhan bulan, termasuk Ganymede, bulan terbesar di tata surya." },
        new InfoPlanet { nama = "Saturnus", warna = new Color32(250, 199, 117, 255), isi =
            "• Terkenal dengan cincinnya yang tersusun dari es dan batuan.\n" +
            "• Raksasa gas dengan massa jenis lebih kecil daripada air.\n" +
            "• Bulan terbesarnya, Titan, memiliki atmosfer tebal." },
        new InfoPlanet { nama = "Uranus", warna = new Color32(93, 202, 165, 255), isi =
            "• Raksasa es berwarna biru kehijauan karena gas metana.\n" +
            "• Sumbu rotasinya miring sekitar 98 derajat, seperti berguling.\n" +
            "• Ditemukan oleh William Herschel pada tahun 1781." },
        new InfoPlanet { nama = "Neptunus", warna = new Color32(24, 95, 165, 255), isi =
            "• Planet terjauh dari Matahari dan termasuk raksasa es.\n" +
            "• Memiliki angin tercepat di tata surya, lebih dari 2.000 km/jam.\n" +
            "• Satu tahun di Neptunus sekitar 165 tahun Bumi.\n" +
            "• Bulan terbesarnya bernama Triton." },
    };

    GameObject panelCaraBermain;
    GameObject panelBelajar;
    TextMeshProUGUI teksJudulBelajar;
    TextMeshProUGUI teksIsiBelajar;
    Image gambarPlanet;
    int indexPlanet;

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

    // ====== Membangun tampilan menu ======
    void Start()
    {
        Time.timeScale = 1f; // jaga-jaga jika sebelumnya game dijeda
        SiapkanEventSystem();
        Transform root = BuatCanvas("CanvasMenu").transform;

        BuatTeks(root, "ROKET TATA SURYA", 96, Atas, new Vector2(0, -130), new Vector2(1600, 140), Color.white);
        BuatTeks(root, "Terbangkan roketmu dari Merkurius sampai Neptunus\nsambil menjawab soal tentang tata surya",
            34, Atas, new Vector2(0, -270), new Vector2(1500, 120), new Color32(181, 212, 244, 255));

        int tertinggi = PlayerPrefs.GetInt(GameManager.KunciSkorTertinggi, 0);
        BuatTeks(root, "Skor tertinggi: " + tertinggi, 36, Tengah, new Vector2(0, 110), new Vector2(800, 60), Kuning);

        Vector2 ukuranTombol = new Vector2(520, 90);
        BuatTombol(root, "Mulai", Tengah, new Vector2(0, 10), ukuranTombol, Oranye, TeksOranye, Mulai);
        BuatTombol(root, "Belajar", Tengah, new Vector2(0, -100), ukuranTombol, Biru, TeksBiruMuda, BukaBelajar);
        BuatTombol(root, "Cara bermain", Tengah, new Vector2(0, -210), ukuranTombol, Biru, TeksBiruMuda,
            () => panelCaraBermain.SetActive(true));
        BuatTombol(root, "Keluar", Tengah, new Vector2(0, -320), ukuranTombol, Biru, TeksBiruMuda, Keluar);

        
        // Tombol pilihan tingkat kesulitan (berputar: Mudah -> Normal -> Sulit)
        Vector2 ukuranPengaturan = new Vector2(320, 70);
        Button tombolKesulitan = BuatTombol(root, "Kesulitan: " + Pengaturan.NamaTingkat, Tengah,
            new Vector2(-170, -410), ukuranPengaturan, Biru, TeksBiruMuda, () => { });
        TextMeshProUGUI labelKesulitan = tombolKesulitan.GetComponentInChildren<TextMeshProUGUI>();
        labelKesulitan.fontSize = 30;
        tombolKesulitan.onClick.AddListener(() =>
        {
            Pengaturan.Tingkat = (Kesulitan)(((int)Pengaturan.Tingkat + 1) % 3);
            labelKesulitan.text = "Kesulitan: " + Pengaturan.NamaTingkat;
        });

        // Tombol suara aktif/mati
        Button tombolSuara = BuatTombol(root, "Suara: " + (Pengaturan.SuaraAktif ? "Aktif" : "Mati"), Tengah,
            new Vector2(170, -410), ukuranPengaturan, Biru, TeksBiruMuda, () => { });
        TextMeshProUGUI labelSuara = tombolSuara.GetComponentInChildren<TextMeshProUGUI>();
        labelSuara.fontSize = 30;
        tombolSuara.onClick.AddListener(() =>
        {
            Pengaturan.SuaraAktif = !Pengaturan.SuaraAktif;
            labelSuara.text = "Suara: " + (Pengaturan.SuaraAktif ? "Aktif" : "Mati");
        });

        BuatTeks(root, "Kelompok 6 - Pemrograman Mobile Lanjut", 24, Bawah, new Vector2(0, 30),
            new Vector2(1200, 50), new Color32(136, 135, 128, 255));

        // Panel dibuat terakhir supaya tampil paling atas saat dibuka
        panelCaraBermain = BuatPanelCaraBermain(root);
        panelCaraBermain.SetActive(false);

        panelBelajar = BuatPanelBelajar(root);
        panelBelajar.SetActive(false);
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

    void BukaBelajar()
    {
        indexPlanet = 0;
        TampilkanPlanet();
        panelBelajar.SetActive(true);
    }

    void GeserPlanet(int arah)
    {
        indexPlanet = (indexPlanet + arah + dataPlanet.Length) % dataPlanet.Length;
        TampilkanPlanet();
    }

    void TampilkanPlanet()
    {
        InfoPlanet info = dataPlanet[indexPlanet];
        teksJudulBelajar.text = info.nama + "  (" + (indexPlanet + 1) + "/" + dataPlanet.Length + ")";
        gambarPlanet.color = info.warna;
        teksIsiBelajar.text = info.isi;
    }

    // ====== Panel-panel ======
    GameObject BuatPanelCaraBermain(Transform root)
    {
        GameObject panel = BuatPanelLayar(root, "PanelCaraBermain", new Color32(6, 14, 34, 245));

        BuatTeks(panel.transform, "Cara bermain", 64, Atas, new Vector2(0, -100), new Vector2(1200, 100), Color.white);

        TextMeshProUGUI isi = BuatTeks(panel.transform,
            "1. Sentuh dan geser layar untuk mengarahkan roket.\n" +
            "2. Baca soal yang muncul di bagian atas layar.\n" +
            "3. Terbangkan roket menembus cincin berisi jawaban yang benar.\n" +
            "4. Hindari meteor yang datang dari depan.\n" +
            "5. Jawaban benar membawamu ke planet berikutnya.\n" +
            "6. Salah menjawab atau menabrak meteor mengurangi nyawa.\n\n" +
            "Capai Neptunus untuk menyelesaikan misi!",
            34, Tengah, new Vector2(0, 40), new Vector2(1300, 560), TeksBiruMuda);
        isi.alignment = TextAlignmentOptions.Left;

        BuatTombol(panel.transform, "Tutup", Tengah, new Vector2(0, -400), new Vector2(520, 90), Oranye, TeksOranye,
            () => panelCaraBermain.SetActive(false));
        return panel;
    }

    GameObject BuatPanelBelajar(Transform root)
    {
        GameObject panel = BuatPanelLayar(root, "PanelBelajar", new Color32(6, 14, 34, 245));

        teksJudulBelajar = BuatTeks(panel.transform, "", 64, Atas, new Vector2(0, -90), new Vector2(1400, 100), Color.white);

        GameObject g = new GameObject("GambarPlanet", typeof(RectTransform));
        g.transform.SetParent(panel.transform, false);
        gambarPlanet = g.AddComponent<Image>();
        gambarPlanet.sprite = SpriteLingkaran();
        gambarPlanet.raycastTarget = false;
        RectTransform rt = (RectTransform)g.transform;
        rt.anchorMin = Tengah;
        rt.anchorMax = Tengah;
        rt.pivot = Tengah;
        rt.anchoredPosition = new Vector2(-520, 10);
        rt.sizeDelta = new Vector2(380, 380);

        teksIsiBelajar = BuatTeks(panel.transform, "", 34, Tengah, new Vector2(230, 10), new Vector2(1000, 560), TeksBiruMuda);
        teksIsiBelajar.alignment = TextAlignmentOptions.Left;

        Vector2 ukuranKecil = new Vector2(360, 90);
        BuatTombol(panel.transform, "Sebelumnya", Tengah, new Vector2(-420, -420), ukuranKecil, Biru, TeksBiruMuda,
            () => GeserPlanet(-1));
        BuatTombol(panel.transform, "Tutup", Tengah, new Vector2(0, -420), ukuranKecil, Oranye, TeksOranye,
            () => panelBelajar.SetActive(false));
        BuatTombol(panel.transform, "Berikutnya", Tengah, new Vector2(420, -420), ukuranKecil, Biru, TeksBiruMuda,
            () => GeserPlanet(1));
        return panel;
    }

    // ====== Pembuat elemen UI (dipakai juga oleh script lain) ======
    public static void SiapkanEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;
        GameObject es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }

    public static Canvas BuatCanvas(string nama)
    {
        GameObject go = new GameObject(nama);
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    public static TextMeshProUGUI BuatTeks(Transform parent, string isi, float ukuran, Vector2 anchor,
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

    public static Button BuatTombol(Transform parent, string label, Vector2 anchor, Vector2 posisi, Vector2 ukuran,
                                    Color warnaLatar, Color warnaTeks, UnityAction aksi)
    {
        GameObject go = new GameObject("Tombol_" + label, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        Image img = go.AddComponent<Image>();
        img.color = warnaLatar;

        Button tombol = go.AddComponent<Button>();
        tombol.targetGraphic = img;
        tombol.onClick.AddListener(aksi);

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = posisi;
        rt.sizeDelta = ukuran;

        TextMeshProUGUI t = BuatTeks(go.transform, label, 38, new Vector2(0.5f, 0.5f), Vector2.zero, ukuran, warnaTeks);
        return tombol;
    }

    public static GameObject BuatPanelLayar(Transform parent, string nama, Color warna)
    {
        GameObject panel = new GameObject(nama, typeof(RectTransform));
        panel.transform.SetParent(parent, false);

        Image latar = panel.AddComponent<Image>();
        latar.color = warna;

        RectTransform rt = (RectTransform)panel.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return panel;
    }

    // Sprite lingkaran dengan sedikit bayangan, dibuat dari kode
    static Sprite lingkaran;
    public static Sprite SpriteLingkaran()
    {
        if (lingkaran != null) return lingkaran;

        int n = 256;
        float r = n / 2f;
        Vector2 pusat = new Vector2(r, r);
        Vector2 titikTerang = new Vector2(n * 0.35f, n * 0.65f);
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        Color[] piksel = new Color[n * n];

        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float alpha = Mathf.Clamp01(r - Vector2.Distance(p, pusat));
                float terang = 1f - 0.45f * Mathf.Clamp01(Vector2.Distance(p, titikTerang) / n);
                piksel[y * n + x] = new Color(terang, terang, terang, alpha);
            }
        }

        tex.SetPixels(piksel);
        tex.Apply();
        lingkaran = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        return lingkaran;
    }
}