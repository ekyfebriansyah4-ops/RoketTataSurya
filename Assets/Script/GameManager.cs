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
    public int skor = 0;
    public int zonaSekarang = 0;

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
    Coroutine pesanCo;

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
        CariTeksOtomatis();
        AturTampilanUI();
        SiapkanMaterialCincin();
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
        TampilkanPesan("Arahkan roket ke cincin jawaban yang benar!", 2.5f);
        StartCoroutine(MunculkanSoal(2f, null));
    }

    void Update()
    {
        // Setelah game selesai, ketuk layar untuk kembali ke menu
        if (selesai)
        {
            if (Time.time - waktuSelesai > 1f && KetukBaru())
                SceneManager.LoadScene("MainMenu");
            return;
        }

        if (gelombang == null) return;

        // Gerakkan cincin mendekati roket
        gelombang.position += Vector3.back * kecepatanAktif * Time.deltaTime;

        // Cincin lewat tanpa ditembus
        if (menungguJawaban && gelombang.position.z < -3f)
        {
            menungguJawaban = false;
            HapusGelombang();
            TampilkanPesan("Cincin terlewat! Coba lagi.", 1.5f);
            StartCoroutine(MunculkanSoal(jedaAntarSoal, soalAktif));
        }
    }

    IEnumerator MunculkanSoal(float jeda, Soal ulangi)
    {
        yield return new WaitForSeconds(jeda);
        if (selesai) yield break;

        // Makin jauh planetnya, makin cepat cincin datang
        kecepatanAktif = kecepatanCincin + zonaSekarang * tambahanKecepatanPerZona;

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
        // Wadah cincin
        GameObject cincin = new GameObject("Cincin_" + jawaban);
        cincin.transform.SetParent(gelombang, false);
        cincin.transform.localPosition = new Vector3(posisi.x, posisi.y, 0f);

        // Piringan (Cylinder yang dipipihkan)
        GameObject piringan = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(piringan.GetComponent<Collider>());
        piringan.transform.SetParent(cincin.transform, false);
        piringan.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        piringan.transform.localScale = new Vector3(1.6f, 0.03f, 1.6f);
        piringan.GetComponent<Renderer>().sharedMaterial = materialCincin;

        // Teks jawaban 3D
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

        // Trigger untuk mendeteksi roket
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
            skor += 10;
            zonaSekarang++;
            if (zonaSekarang >= zona.Length)
            {
                Selesai(true);
                return;
            }
            Efek.SuaraBenar();
            TampilkanPesan("Benar! " + soalAktif.fakta, 3f);
        }
        else
        {
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
        if (selesai) return;
        nyawa--;
        Efek.KenaMeteor();
        PerbaruiUI();
        if (nyawa <= 0) { Selesai(false); return; }
        TampilkanPesan("Tertabrak meteor!", 1.2f);
    }

    void Selesai(bool menang)
    {
        selesai = true;
        waktuSelesai = Time.time;
        StopAllCoroutines();
        HapusGelombang();
        PerbaruiUI();
        teksSoal.text = "";

        if (menang) Efek.SuaraMenang();

        // Simpan skor tertinggi di perangkat
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
        teksZona.text = "Zona " + (z + 1) + "/" + zona.Length + ": " + zona[z].namaPlanet;
        teksSkor.text = "Skor: " + skor + "   Nyawa: " + Mathf.Max(nyawa, 0);
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

    // ====== Pengaturan otomatis ======

    void SiapkanMaterialCincin()
    {
        if (materialCincin != null) return;
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        materialCincin = new Material(shader);
        materialCincin.SetColor("_BaseColor", new Color32(15, 110, 86, 255));
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

        // teks, anchor, posisi, ukuran, font, alignment, warna
        AturTeks(teksSoal, new Vector2(0.5f, 1f), new Vector2(0, -90), new Vector2(1400, 120),
            48, TextAlignmentOptions.Center, Color.white);
        AturTeks(teksZona, new Vector2(0f, 1f), new Vector2(30, -20), new Vector2(700, 60),
            34, TextAlignmentOptions.TopLeft, Color.white);
        AturTeks(teksSkor, new Vector2(1f, 1f), new Vector2(-30, -20), new Vector2(600, 60),
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