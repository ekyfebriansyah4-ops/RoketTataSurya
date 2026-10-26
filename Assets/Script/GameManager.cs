using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[System.Serializable]
public class Soal
{
    public string pertanyaan;
    public string[] pilihan;
    public int indexBenar;
    public string fakta;

    public Soal(string p, string a, string b, string c, int benar, string f)
    {
        pertanyaan = p;
        pilihan = new[] { a, b, c };
        indexBenar = benar;
        fakta = f;
    }
}

[System.Serializable]
public class Zona
{
    public string namaPlanet;
    public Soal[] daftarSoal;

    public Zona(string nama, params Soal[] soal)
    {
        namaPlanet = nama;
        daftarSoal = soal;
    }
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public const string KunciSkorTertinggi = "SkorTertinggi";

    [Header("UI (otomatis dicari berdasarkan nama)")]
    public TMP_Text teksSoal;
    public TMP_Text teksZona;
    public TMP_Text teksSkor;
    public TMP_Text teksPesan;

    [Header("Cincin jawaban")]
    public Material materialCincin;
    public float jarakMuncul = 40f;
    public float kecepatanCincin = 12f;
    public float tambahanKecepatanPerZona = 1.5f;
    public float jedaAntarSoal = 1.5f;

    [Header("Status permainan")]
    public int nyawa = 3;
    public int nyawaMaks = 6;
    public int skor = 0;
    public int zonaSekarang = 0;
    public int beruntun = 0;
    public float lamaKebal = 1.5f;

    [Header("Bank soal per planet")]
    public Zona[] zona =
    {
        new Zona("Merkurius",
            new Soal("Planet yang paling dekat dengan Matahari adalah?", "Merkurius", "Venus", "Mars", 0,
                "Merkurius mengelilingi Matahari hanya dalam 88 hari."),
            new Soal("Merkurius adalah planet paling ... di tata surya", "Kecil", "Besar", "Dingin", 0,
                "Merkurius adalah planet terkecil di tata surya.")),
        new Zona("Venus",
            new Soal("Planet paling panas di tata surya adalah?", "Merkurius", "Venus", "Mars", 1,
                "Atmosfer tebal Venus memerangkap panas hingga lebih dari 460°C."),
            new Soal("Venus sering disebut sebagai?", "Bintang kejora", "Planet merah", "Planet cincin", 0,
                "Venus tampak sangat terang saat pagi atau senja.")),
        new Zona("Bumi",
            new Soal("Satelit alami Bumi bernama?", "Titan", "Bulan", "Phobos", 1,
                "Bulan mengelilingi Bumi sekitar 27 hari sekali."),
            new Soal("Bumi berada di urutan ke berapa dari Matahari?", "Ketiga", "Kedua", "Keempat", 0,
                "Bumi satu-satunya planet yang diketahui berpenghuni.")),
        new Zona("Mars",
            new Soal("Mars dijuluki sebagai planet?", "Merah", "Biru", "Cincin", 0,
                "Warna merah Mars berasal dari karat besi di permukaannya."),
            new Soal("Berapa jumlah bulan yang dimiliki Mars?", "Satu", "Dua", "Lima", 1,
                "Dua bulan Mars bernama Phobos dan Deimos.")),
        new Zona("Jupiter",
            new Soal("Planet terbesar di tata surya adalah?", "Saturnus", "Jupiter", "Neptunus", 1,
                "Jupiter cukup besar untuk memuat lebih dari 1.300 Bumi."),
            new Soal("Bintik Merah Besar di Jupiter adalah?", "Gunung", "Badai raksasa", "Kawah", 1,
                "Badai raksasa Jupiter sudah berlangsung ratusan tahun.")),
        new Zona("Saturnus",
            new Soal("Saturnus terkenal karena memiliki?", "Cincin", "Lautan", "Gunung api", 0,
                "Cincin Saturnus tersusun dari bongkahan es dan batuan."),
            new Soal("Saturnus termasuk jenis planet?", "Raksasa gas", "Batuan", "Kerdil", 0,
                "Massa jenis Saturnus lebih kecil daripada air.")),
        new Zona("Uranus",
            new Soal("Uranus berotasi dalam posisi?", "Tegak", "Berbaring", "Terbalik", 1,
                "Sumbu rotasi Uranus miring sekitar 98 derajat."),
            new Soal("Warna Uranus terlihat?", "Merah", "Biru kehijauan", "Kuning", 1,
                "Warna Uranus berasal dari gas metana di atmosfernya.")),
        new Zona("Neptunus",
            new Soal("Planet terjauh dari Matahari adalah?", "Uranus", "Neptunus", "Saturnus", 1,
                "Neptunus butuh sekitar 165 tahun untuk sekali mengelilingi Matahari."),
            new Soal("Angin di Neptunus terkenal sangat?", "Kencang", "Lemah", "Hangat", 0,
                "Angin di Neptunus bisa melebihi 2.000 km/jam.")),
    };

    // Posisi-posisi cincin (diacak setiap soal)
    static readonly Vector2[] slot =
    {
        new Vector2(-2.2f, 0f), new Vector2(2.2f, 0f),
        new Vector2(0f, 1.3f), new Vector2(0f, -1.3f)
    };

    Transform gelombang;
    Soal soalAktif;
    bool menungguJawaban;
    bool selesai;
    float waktuSelesai;
    float kecepatanAktif;
    float kebalSampai;
    Coroutine pesanCo;

    // Perisai
    bool perisaiAktif;
    Transform perisaiVisual;

    // UI progres dan banner
    RectTransform isiProgres;
    RectTransform penandaRoket;
    float progresTampil;
    TextMeshProUGUI teksBanner;
    Coroutine bannerCo;

    int PengaliBeruntun
    {
        get { return Mathf.Clamp(beruntun, 1, 3); }
    }

    // ====== Membuat GameManager otomatis saat scene Gameplay dimuat ======
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
        if (scene.name == "Gameplay" && FindAnyObjectByType<GameManager>() == null)
        {
            new GameObject("GameManager").AddComponent<GameManager>();
        }
    }

    // ====== Siklus permainan ======
    void Awake()
    {
        Instance = this;
        nyawa = Pengaturan.NyawaAwal;
        CariTeksOtomatis();
        AturTampilanUI();
        SiapkanMaterialCincin();
        BuatProgresDanBanner();
    }

    void Start()
    {
        if (teksSoal == null || teksZona == null || teksSkor == null || teksPesan == null)
        {
            Debug.LogError("Teks UI belum lengkap. Pastikan ada TeksSoal, TeksZona, TeksSkor, TeksPesan di Canvas.");
            enabled = false;
            return;
        }

        teksSoal.text = "";
        teksPesan.text = "";
        PerbaruiUI();
        TampilkanBanner("Memasuki zona " + zona[0].namaPlanet, 0);
        TampilkanPesan("Arahkan roket ke cincin jawaban yang benar!", 2.5f);
        StartCoroutine(MunculkanSoal(2.5f, null));
    }

    void Update()
    {
        PerbaruiProgres();

        // Perisai berputar mengelilingi roket
        if (perisaiAktif && perisaiVisual != null)
            perisaiVisual.Rotate(0f, 0f, 120f * Time.deltaTime);

        // Setelah game selesai, ketuk layar untuk kembali ke menu
        if (selesai)
        {
            if (Time.time - waktuSelesai > 1f && KetukBaru())
                Transisi.Muat("MainMenu");
            return;
        }

        if (gelombang == null) return;

        // Gerakkan cincin mendekati roket
        gelombang.position += Vector3.back * kecepatanAktif * Time.deltaTime;

        // Cincin lewat tanpa ditembus
        if (menungguJawaban && gelombang.position.z < -3f)
        {
            menungguJawaban = false;
            beruntun = 0;
            HapusGelombang();
            PerbaruiUI();
            TampilkanPesan("Cincin terlewat! Coba lagi.", 1.5f);
            StartCoroutine(MunculkanSoal(jedaAntarSoal, soalAktif));
        }
    }

    IEnumerator MunculkanSoal(float jeda, Soal ulangi)
    {
        yield return new WaitForSeconds(jeda);
        if (selesai) yield break;

        kecepatanAktif = (kecepatanCincin + zonaSekarang * tambahanKecepatanPerZona) * Pengaturan.PengaliKecepatan;

        Zona z = zona[zonaSekarang];
        soalAktif = ulangi ?? z.daftarSoal[Random.Range(0, z.daftarSoal.Length)];
        teksSoal.text = soalAktif.pertanyaan;
        BuatGelombang(soalAktif);
        menungguJawaban = true;
    }

    void BuatGelombang(Soal s)
    {
        gelombang = new GameObject("GelombangCincin").transform;
        gelombang.position = new Vector3(0f, 0f, jarakMuncul);

        List<Vector2> tersedia = new List<Vector2>(slot);
        for (int i = 0; i < 3; i++)
        {
            int k = Random.Range(0, tersedia.Count);
            BuatCincin(s.pilihan[i], i == s.indexBenar, tersedia[k]);
            tersedia.RemoveAt(k);
        }
    }

    void BuatCincin(string jawaban, bool benar, Vector2 posisi)
    {
        GameObject cincin = new GameObject("Cincin_" + jawaban);
        cincin.transform.SetParent(gelombang, false);
        cincin.transform.localPosition = new Vector3(posisi.x, posisi.y, 0f);

        GameObject piringan = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(piringan.GetComponent<Collider>());
        piringan.transform.SetParent(cincin.transform, false);
        piringan.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        piringan.transform.localScale = new Vector3(1.6f, 0.03f, 1.6f);
        piringan.GetComponent<Renderer>().sharedMaterial = materialCincin;
                  Visual.TambahTepiBercahaya(cincin.transform);

        GameObject teks = new GameObject("Teks");
        teks.transform.SetParent(cincin.transform, false);
        teks.transform.localPosition = new Vector3(0f, 0f, -0.1f);
        TextMeshPro tmp = teks.AddComponent<TextMeshPro>();
        tmp.text = jawaban;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 1f;
        tmp.fontSizeMax = 4f;
        tmp.rectTransform.sizeDelta = new Vector2(1.5f, 1f);

        BoxCollider col = cincin.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(1.4f, 1.4f, 0.8f);

        AnswerRing ar = cincin.AddComponent<AnswerRing>();
        ar.benar = benar;
    }

    public void PilihJawaban(bool benar)
    {
        if (!menungguJawaban || selesai) return;
        menungguJawaban = false;
        HapusGelombang();

        if (benar)
        {
            beruntun++;
            int pengali = PengaliBeruntun;
            skor += 10 * pengali;
            zonaSekarang++;
            if (zonaSekarang >= zona.Length)
            {
                Selesai(true);
                return;
            }
            Efek.SuaraBenar();
            string bonus = pengali > 1 ? "Beruntun x" + pengali + "! " : "";
            TampilkanPesan("Benar! " + bonus + soalAktif.fakta, 3f);
            TampilkanBanner("Memasuki zona " + zona[zonaSekarang].namaPlanet, zonaSekarang);
        }
        else
        {
            beruntun = 0;
            nyawa--;
            Efek.SuaraSalah();
            if (nyawa <= 0)
            {
                Selesai(false);
                return;
            }
            TampilkanPesan("Kurang tepat. Jawabannya: " + soalAktif.pilihan[soalAktif.indexBenar], 2.5f);
        }

        PerbaruiUI();
        teksSoal.text = "";
        StartCoroutine(MunculkanSoal(jedaAntarSoal + 1f, null));
    }

    // Dipanggil oleh meteor
    public void KenaMeteor()
    {
        if (selesai || Time.time < kebalSampai) return;

        // Perisai menahan satu tabrakan
        if (perisaiAktif)
        {
            MatikanPerisai();
            kebalSampai = Time.time + 0.5f;
            Efek.Getar(0.2f, 0.12f);
            PerbaruiUI();
            TampilkanPesan("Perisai menahan meteor!", 1.5f);
            return;
        }

        kebalSampai = Time.time + lamaKebal;
        nyawa--;
        Efek.KenaMeteor(lamaKebal);
        PerbaruiUI();
        if (nyawa <= 0) { Selesai(false); return; }
        TampilkanPesan("Tertabrak meteor!", 1.2f);
    }

    // Dipanggil oleh power-up
    public void AmbilPowerUp(JenisPowerUp jenis)
    {
        if (selesai) return;

        if (jenis == JenisPowerUp.Nyawa)
        {
            if (nyawa < nyawaMaks)
            {
                nyawa++;
                TampilkanPesan("+1 nyawa!", 1.5f);
            }
            else
            {
                skor += 5;
                TampilkanPesan("Nyawa sudah penuh, bonus +5 poin", 1.5f);
            }
        }
        else
        {
            AktifkanPerisai();
            TampilkanPesan("Perisai aktif! Menahan satu tabrakan meteor.", 2f);
        }
        PerbaruiUI();
    }

    void AktifkanPerisai()
    {
        perisaiAktif = true;
        if (perisaiVisual == null)
        {
            GameObject roket = GameObject.FindWithTag("Player");
            if (roket == null) return;

            perisaiVisual = new GameObject("Perisai").transform;
            perisaiVisual.SetParent(roket.transform, false);

            Material m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", new Color32(133, 183, 235, 255));

            for (int i = 0; i < 8; i++)
            {
                float sudut = i * Mathf.PI * 2f / 8f;
                GameObject bola = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(bola.GetComponent<Collider>());
                bola.transform.SetParent(perisaiVisual, false);
                bola.transform.localPosition = new Vector3(Mathf.Cos(sudut) * 1.1f, Mathf.Sin(sudut) * 0.8f, 0f);
                bola.transform.localScale = Vector3.one * 0.18f;
                bola.GetComponent<Renderer>().sharedMaterial = m;
            }
        }
        perisaiVisual.gameObject.SetActive(true);
    }

    void MatikanPerisai()
    {
        perisaiAktif = false;
        if (perisaiVisual != null) perisaiVisual.gameObject.SetActive(false);
    }

    void Selesai(bool menang)
    {
        selesai = true;
        waktuSelesai = Time.time;
        StopAllCoroutines();
        HapusGelombang();
        PerbaruiUI();
        teksSoal.text = "";
        if (teksBanner != null) teksBanner.alpha = 0f;

        if (menang) Efek.SuaraMenang();

        int tertinggi = PlayerPrefs.GetInt(KunciSkorTertinggi, 0);
        bool rekorBaru = skor > tertinggi;
        if (rekorBaru)
        {
            tertinggi = skor;
            PlayerPrefs.SetInt(KunciSkorTertinggi, skor);
            PlayerPrefs.Save();
        }

        teksPesan.text = (menang ? "Misi selesai! Kamu sampai di Neptunus." : "Roket rusak, misi gagal.")
            + "\nSkor akhir: " + skor + (rekorBaru ? "  (Rekor baru!)" : "  |  Skor tertinggi: " + tertinggi)
            + "\nKetuk layar untuk kembali ke menu";
    }

    void PerbaruiUI()
    {
        int z = Mathf.Min(zonaSekarang, zona.Length - 1);
        teksZona.text = "Zona " + (z + 1) + "/" + zona.Length + ": " + zona[z].namaPlanet
            + "  (" + Pengaturan.NamaTingkat + ")";

        string bonus = beruntun > 1 ? "  (x" + PengaliBeruntun + ")" : "";
        string perisai = perisaiAktif ? "   [Perisai]" : "";
        teksSkor.text = "Skor: " + skor + bonus + "   Nyawa: " + Mathf.Max(nyawa, 0) + perisai;
    }

    void HapusGelombang()
    {
        if (gelombang != null) Destroy(gelombang.gameObject);
        gelombang = null;
    }

    void TampilkanPesan(string isi, float durasi)
    {
        if (pesanCo != null) StopCoroutine(pesanCo);
        pesanCo = StartCoroutine(PesanSementara(isi, durasi));
    }

    IEnumerator PesanSementara(string isi, float durasi)
    {
        teksPesan.text = isi;
        yield return new WaitForSeconds(durasi);
        teksPesan.text = "";
    }

    bool KetukBaru()
    {
#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
        return false;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    // ====== Bar progres perjalanan dan banner zona ======
    void BuatProgresDanBanner()
    {
        Canvas canvas = teksSoal != null ? teksSoal.canvas : FindAnyObjectByType<Canvas>();
        if (canvas == null) return;
        Transform root = canvas.transform;

        RectTransform latar = BuatGambar(root, "BarProgres", new Color(1f, 1f, 1f, 0.15f), null);
        latar.anchorMin = new Vector2(0.5f, 0f);
        latar.anchorMax = new Vector2(0.5f, 0f);
        latar.pivot = new Vector2(0.5f, 0f);
        latar.anchoredPosition = new Vector2(0, 28);
        latar.sizeDelta = new Vector2(1200, 14);

        isiProgres = BuatGambar(latar, "Isi", MenuUtama.Oranye, null);
        isiProgres.anchorMin = Vector2.zero;
        isiProgres.anchorMax = new Vector2(0f, 1f);
        isiProgres.offsetMin = Vector2.zero;
        isiProgres.offsetMax = Vector2.zero;

        for (int i = 0; i < zona.Length; i++)
        {
            RectTransform titik = BuatGambar(latar, "Titik_" + zona[i].namaPlanet,
                LuarAngkasa.WarnaPlanet(i), MenuUtama.SpriteLingkaran());
            float x = (i + 1f) / zona.Length;
            titik.anchorMin = new Vector2(x, 0.5f);
            titik.anchorMax = new Vector2(x, 0.5f);
            titik.pivot = new Vector2(0.5f, 0.5f);
            titik.anchoredPosition = Vector2.zero;
            titik.sizeDelta = new Vector2(22, 22);
        }

        penandaRoket = BuatGambar(latar, "PenandaRoket", Color.white, MenuUtama.SpriteLingkaran());
        penandaRoket.anchorMin = new Vector2(0f, 0.5f);
        penandaRoket.anchorMax = new Vector2(0f, 0.5f);
        penandaRoket.pivot = new Vector2(0.5f, 0.5f);
        penandaRoket.anchoredPosition = Vector2.zero;
        penandaRoket.sizeDelta = new Vector2(30, 30);

        TextMeshProUGUI kiri = MenuUtama.BuatTeks(latar, "Matahari", 24, new Vector2(0f, 0.5f),
            new Vector2(-15, 0), new Vector2(200, 40), MenuUtama.Kuning);
        kiri.rectTransform.pivot = new Vector2(1f, 0.5f);
        kiri.alignment = TextAlignmentOptions.Right;

        TextMeshProUGUI kanan = MenuUtama.BuatTeks(latar, "Neptunus", 24, new Vector2(1f, 0.5f),
            new Vector2(15, 0), new Vector2(200, 40), new Color32(133, 183, 235, 255));
        kanan.rectTransform.pivot = new Vector2(0f, 0.5f);
        kanan.alignment = TextAlignmentOptions.Left;

        teksBanner = MenuUtama.BuatTeks(root, "", 72, new Vector2(0.5f, 0.5f), new Vector2(0, 120),
            new Vector2(1600, 120), Color.white);
        teksBanner.alpha = 0f;
    }

    RectTransform BuatGambar(Transform parent, string nama, Color warna, Sprite sprite)
    {
        GameObject g = new GameObject(nama, typeof(RectTransform));
        g.transform.SetParent(parent, false);
        Image img = g.AddComponent<Image>();
        img.color = warna;
        img.sprite = sprite;
        img.raycastTarget = false;
        return (RectTransform)g.transform;
    }

    void PerbaruiProgres()
    {
        if (isiProgres == null) return;
        float target = (float)zonaSekarang / zona.Length;
        progresTampil = Mathf.MoveTowards(progresTampil, target, Time.deltaTime * 0.25f);
        isiProgres.anchorMax = new Vector2(progresTampil, 1f);
        penandaRoket.anchorMin = new Vector2(progresTampil, 0.5f);
        penandaRoket.anchorMax = new Vector2(progresTampil, 0.5f);
    }

    void TampilkanBanner(string isi, int indexZona)
    {
        if (teksBanner == null) return;
        if (bannerCo != null) StopCoroutine(bannerCo);
        teksBanner.color = LuarAngkasa.WarnaPlanet(indexZona);
        bannerCo = StartCoroutine(AnimasiBanner(isi));
    }

    IEnumerator AnimasiBanner(string isi)
    {
        teksBanner.text = isi;
        float durasi = 2.2f;
        float t = 0f;
        while (t < durasi)
        {
            t += Time.deltaTime;
            teksBanner.alpha = t < 0.3f ? t / 0.3f : Mathf.Clamp01((durasi - t) / 0.7f);
            yield return null;
        }
        teksBanner.alpha = 0f;
    }

    // ====== Pengaturan otomatis ======
    void SiapkanMaterialCincin()
    {
        if (materialCincin != null) return;
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        materialCincin = new Material(shader);
        materialCincin.SetColor("_BaseColor", new Color32(10, 40, 45, 255));
    }

    void CariTeksOtomatis()
    {
        if (teksSoal == null) teksSoal = CariTeks("TeksSoal");
        if (teksZona == null) teksZona = CariTeks("TeksZona");
        if (teksSkor == null) teksSkor = CariTeks("TeksSkor");
        if (teksPesan == null) teksPesan = CariTeks("TeksPesan");
    }

    TMP_Text CariTeks(string nama)
    {
        GameObject obj = GameObject.Find(nama);
        if (obj == null)
        {
            Debug.LogWarning("Objek teks '" + nama + "' tidak ditemukan di Canvas.");
            return null;
        }
        return obj.GetComponent<TMP_Text>();
    }

    void AturTampilanUI()
    {
        CanvasScaler scaler = FindAnyObjectByType<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        AturTeks(teksSoal, new Vector2(0.5f, 1f), new Vector2(0, -90), new Vector2(1400, 120),
            48, TextAlignmentOptions.Center, Color.white);
        AturTeks(teksZona, new Vector2(0f, 1f), new Vector2(30, -20), new Vector2(800, 60),
            34, TextAlignmentOptions.TopLeft, Color.white);
        AturTeks(teksSkor, new Vector2(1f, 1f), new Vector2(-30, -20), new Vector2(900, 60),
            34, TextAlignmentOptions.TopRight, Color.white);
        AturTeks(teksPesan, new Vector2(0.5f, 0f), new Vector2(0, 60), new Vector2(1600, 160),
            38, TextAlignmentOptions.Center, new Color32(250, 199, 117, 255));
    }

    void AturTeks(TMP_Text teks, Vector2 anchor, Vector2 posisi, Vector2 ukuran,
                  float ukuranFont, TextAlignmentOptions rata, Color warna)
    {
        if (teks == null) return;
        RectTransform rt = teks.rectTransform;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = posisi;
        rt.sizeDelta = ukuran;
        teks.enableAutoSizing = false;
        teks.fontSize = ukuranFont;
        teks.alignment = rata;
        teks.color = warna;
    }
}