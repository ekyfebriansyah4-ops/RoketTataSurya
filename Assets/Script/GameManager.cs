using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

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
    public int soalPerZona = 3;   // jumlah jawaban benar untuk pindah ke planet berikutnya
    public int nyawa = 3;
    public int nyawaMaks = 6;
    public int skor = 0;
    public int zonaSekarang = 0;
    public int beruntun = 0;
    public float lamaKebal = 1.5f;

    [Header("Bank soal per planet (isinya di BankSoal.cs)")]
    public Zona[] zona = BankSoal.SemuaZona();

    // Posisi-posisi cincin (diacak setiap soal)
    static readonly Vector2[] slot =
    {
        new Vector2(-2.2f, 0f), new Vector2(2.2f, 0f),
        new Vector2(0f, 1.3f), new Vector2(0f, -1.3f)
    };

    // Label UI yang menempel di tiap cincin
    class LabelCincin
    {
        public Transform cincin;
        public RectTransform label;
    }

    Transform gelombang;
    Soal soalAktif;
    bool menungguJawaban;
    bool selesai;
    float kecepatanAktif;
    float kebalSampai;
    int benarDiZona;
    readonly List<Soal> antrianSoal = new List<Soal>();
    Coroutine pesanCo;

    // Perisai
    bool perisaiAktif;
    Transform perisaiVisual;

    // UI progres, banner, dan label jawaban
    RectTransform isiProgres;
    RectTransform penandaRoket;
    float progresTampil;
    TextMeshProUGUI teksBanner;
    Coroutine bannerCo;
    RectTransform wadahLabel;
    readonly List<LabelCincin> labelCincin = new List<LabelCincin>();
    Camera kamera;

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
        nyawa = Mathf.Max(1, Pengaturan.NyawaAwal + DataRoket.Terpilih.bonusNyawa);
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

        soalPerZona = Mathf.Max(1, soalPerZona);
        IsiAntrianSoal();

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

        if (selesai || gelombang == null) return;

        // Gerakkan cincin mendekati roket
        gelombang.position += Vector3.back * kecepatanAktif * Time.deltaTime;

        // Cincin lewat tanpa ditembus: soal yang sama diulang
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

    void LateUpdate()
    {
        PerbaruiLabelJawaban();
    }

    // ====== Antrian soal (diacak, tidak berulang dalam satu zona) ======
    void IsiAntrianSoal()
    {
        antrianSoal.Clear();
        antrianSoal.AddRange(zona[zonaSekarang].daftarSoal);

        // Acak urutan (Fisher-Yates)
        for (int i = antrianSoal.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Soal tmp = antrianSoal[i];
            antrianSoal[i] = antrianSoal[j];
            antrianSoal[j] = tmp;
        }
    }

    Soal AmbilSoalBerikutnya()
    {
        if (antrianSoal.Count == 0) IsiAntrianSoal();
        Soal s = antrianSoal[0];
        antrianSoal.RemoveAt(0);
        return s;
    }

    IEnumerator MunculkanSoal(float jeda, Soal ulangi)
    {
        yield return new WaitForSeconds(jeda);
        if (selesai) yield break;

        kecepatanAktif = (kecepatanCincin + zonaSekarang * tambahanKecepatanPerZona) * Pengaturan.PengaliKecepatan;

        soalAktif = ulangi ?? AmbilSoalBerikutnya();
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

        // Piringan gelap dengan tepi bercahaya
        GameObject piringan = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(piringan.GetComponent<Collider>());
        piringan.transform.SetParent(cincin.transform, false);
        piringan.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        piringan.transform.localScale = new Vector3(1.6f, 0.03f, 1.6f);
        piringan.GetComponent<Renderer>().sharedMaterial = materialCincin;
        Visual.TambahTepiBercahaya(cincin.transform);

        // Trigger untuk mendeteksi roket
        BoxCollider col = cincin.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(1.4f, 1.4f, 0.8f);

        AnswerRing ar = cincin.AddComponent<AnswerRing>();
        ar.benar = benar;

        // Label jawaban yang selalu terbaca
                // Teks jawaban 3D yang menempel di cincin
        GameObject teks = new GameObject("Teks");
        teks.transform.SetParent(cincin.transform, false);
        teks.transform.localPosition = new Vector3(0f, 0f, -0.12f);
        TextMeshPro tmp = teks.AddComponent<TextMeshPro>();
        tmp.text = jawaban;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 1.5f;
        tmp.fontSizeMax = 5f;
        tmp.rectTransform.sizeDelta = new Vector2(1.5f, 1.2f);
        tmp.outlineWidth = 0.2f;
        tmp.outlineColor = new Color32(0, 0, 0, 255);
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
            benarDiZona++;
            string bonus = pengali > 1 ? "Beruntun x" + pengali + "! " : "";

            if (benarDiZona >= soalPerZona)
            {
                // Zona selesai, pindah ke planet berikutnya
                benarDiZona = 0;
                zonaSekarang++;
                if (zonaSekarang >= zona.Length)
                {
                    Selesai(true);
                    return;
                }
                IsiAntrianSoal();
                TampilkanBanner("Memasuki zona " + zona[zonaSekarang].namaPlanet, zonaSekarang);
            }

            Efek.SuaraBenar();
            TampilkanPesan("Benar! " + bonus + soalAktif.fakta, 3f);
        }
        else
        {
            beruntun = 0;
            nyawa--;
            antrianSoal.Add(soalAktif); // soal yang salah akan muncul lagi nanti
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

            Material m = Visual.Bercahaya(new Color32(133, 183, 235, 255), 2f);
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

    // ====== Akhir permainan ======
    void Selesai(bool menang)
    {
        selesai = true;
        StopAllCoroutines();
        HapusGelombang();
        MatikanPerisai();
        PerbaruiUI();
        teksSoal.text = "";
        teksPesan.text = "";
        if (teksBanner != null) teksBanner.alpha = 0f;

        if (menang)
        {
            Efek.SuaraMenang();
        }
        else
        {
            GameObject roket = GameObject.FindWithTag("Player");
            if (roket != null) roket.SetActive(false); // roket hancur
        }

        int tertinggi = PlayerPrefs.GetInt(KunciSkorTertinggi, 0);
        bool rekorBaru = skor > tertinggi;
        if (rekorBaru)
        {
            tertinggi = skor;
            PlayerPrefs.SetInt(KunciSkorTertinggi, skor);
            PlayerPrefs.Save();
        }

        TampilkanPanelSelesai(menang, rekorBaru, tertinggi);
    }

    void TampilkanPanelSelesai(bool menang, bool rekorBaru, int tertinggi)
    {
        Canvas canvas = MenuUtama.BuatCanvas("CanvasSelesai");
        canvas.sortingOrder = 20; // di atas UI game dan tombol Jeda

        GameObject panel = MenuUtama.BuatPanelLayar(canvas.transform, "PanelSelesai", new Color(0f, 0f, 0f, 0.75f));
        Vector2 tengah = new Vector2(0.5f, 0.5f);

        MenuUtama.BuatTeks(panel.transform, menang ? "Misi selesai!" : "Misi gagal", 80, tengah,
            new Vector2(0, 260), new Vector2(1400, 120),
            menang ? (Color)MenuUtama.Kuning : new Color32(240, 149, 149, 255));

        MenuUtama.BuatTeks(panel.transform,
            menang ? "Kamu berhasil sampai di Neptunus." : "Roket rusak sebelum mencapai Neptunus.",
            36, tengah, new Vector2(0, 170), new Vector2(1400, 60), MenuUtama.TeksBiruMuda);

        int zonaTercapai = menang ? zona.Length : zonaSekarang + 1;
        string namaZona = zona[Mathf.Clamp(zonaTercapai - 1, 0, zona.Length - 1)].namaPlanet;
        MenuUtama.BuatTeks(panel.transform,
            "Skor: " + skor + (rekorBaru ? "  (Rekor baru!)" : "") +
            "\nSkor tertinggi: " + tertinggi +
            "\nZona tercapai: " + zonaTercapai + "/" + zona.Length + " (" + namaZona + ")",
            40, tengah, new Vector2(0, 40), new Vector2(1200, 180), Color.white);

        MenuUtama.BuatTombol(panel.transform, "Main lagi", tengah, new Vector2(0, -130), new Vector2(520, 90),
            MenuUtama.Oranye, MenuUtama.TeksOranye, () => Transisi.Muat("Gameplay"));
        MenuUtama.BuatTombol(panel.transform, "Menu utama", tengah, new Vector2(0, -240), new Vector2(520, 90),
            MenuUtama.Biru, MenuUtama.TeksBiruMuda, () => Transisi.Muat("MainMenu"));
    }

    // ====== Label jawaban (UI yang mengikuti cincin) ======
    void BuatLabelJawaban(Transform cincin, string jawaban)
    {
        if (wadahLabel == null) return;

        GameObject g = new GameObject("Label_" + jawaban, typeof(RectTransform));
        g.transform.SetParent(wadahLabel, false);

        Image latar = g.AddComponent<Image>();
        latar.color = new Color32(6, 14, 34, 210);
        latar.raycastTarget = false;

        RectTransform rt = (RectTransform)g.transform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(300, 70);

        TextMeshProUGUI t = MenuUtama.BuatTeks(g.transform, jawaban, 38, new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(290, 70), Color.white);
        t.enableAutoSizing = true;
        t.fontSizeMin = 20f;
        t.fontSizeMax = 38f;

        labelCincin.Add(new LabelCincin { cincin = cincin, label = rt });
    }

    void PerbaruiLabelJawaban()
    {
        if (labelCincin.Count == 0 || wadahLabel == null) return;
        if (kamera == null) kamera = Camera.main;
        if (kamera == null) return;

        foreach (LabelCincin lc in labelCincin)
        {
            if (lc.cincin == null || lc.label == null) continue;

            Vector3 layar = kamera.WorldToScreenPoint(lc.cincin.position);
            bool terlihat = layar.z > 0.5f;
            lc.label.gameObject.SetActive(terlihat);
            if (!terlihat) continue;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(wadahLabel, layar, null, out Vector2 lokal);
            lc.label.anchoredPosition = lokal;

            float t = Mathf.InverseLerp(5f, jarakMuncul, lc.cincin.position.z);
            lc.label.localScale = Vector3.one * Mathf.Lerp(1.15f, 0.8f, t);
        }
    }

    void HapusGelombang()
    {
        foreach (LabelCincin lc in labelCincin)
            if (lc.label != null) Destroy(lc.label.gameObject);
        labelCincin.Clear();

        if (gelombang != null) Destroy(gelombang.gameObject);
        gelombang = null;
    }

    // ====== UI umum ======
    void PerbaruiUI()
    {
        int z = Mathf.Min(zonaSekarang, zona.Length - 1);
        teksZona.text = "Zona " + (z + 1) + "/" + zona.Length + ": " + zona[z].namaPlanet
            + " - Soal " + Mathf.Min(benarDiZona + 1, soalPerZona) + "/" + soalPerZona
            + " (" + Pengaturan.NamaTingkat + ")";

        string bonus = beruntun > 1 ? "  (x" + PengaliBeruntun + ")" : "";
        string perisai = perisaiAktif ? "   [Perisai]" : "";
        teksSkor.text = "Skor: " + skor + bonus + "   Nyawa: " + Mathf.Max(nyawa, 0) + perisai;
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

    // ====== Bar progres perjalanan dan banner zona ======
    void BuatProgresDanBanner()
    {
        Canvas canvas = teksSoal != null ? teksSoal.canvas : FindAnyObjectByType<Canvas>();
        if (canvas == null) return;
        Transform root = canvas.transform;

        // Wadah label jawaban (paling belakang supaya HUD tetap di atasnya)
        wadahLabel = new GameObject("WadahLabelJawaban", typeof(RectTransform)).GetComponent<RectTransform>();
        wadahLabel.SetParent(root, false);
        wadahLabel.anchorMin = Vector2.zero;
        wadahLabel.anchorMax = Vector2.one;
        wadahLabel.offsetMin = Vector2.zero;
        wadahLabel.offsetMax = Vector2.zero;
        wadahLabel.SetAsFirstSibling();

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
        // Progres ikut maju sedikit setiap jawaban benar di dalam zona
        float target = (zonaSekarang + (float)benarDiZona / soalPerZona) / zona.Length;
        progresTampil = Mathf.MoveTowards(progresTampil, Mathf.Clamp01(target), Time.deltaTime * 0.25f);
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
        AturTeks(teksZona, new Vector2(0f, 1f), new Vector2(30, -20), new Vector2(900, 60),
            32, TextAlignmentOptions.TopLeft, Color.white);
        AturTeks(teksSkor, new Vector2(1f, 1f), new Vector2(-30, -20), new Vector2(800, 60),
            32, TextAlignmentOptions.TopRight, Color.white);
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