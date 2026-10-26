using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Data satu jenis roket
public struct JenisRoket
{
    public string nama;
    public string deskripsi;
    public Color32 badan;
    public Color32 aksen;
    public Color32 api;
    public float kecepatanGerak;  // makin besar makin gesit
    public int nilaiKelincahan;   // untuk tampilan (1-5)
    public int bonusNyawa;        // tambahan/pengurangan nyawa awal
}

// Daftar roket dan roket yang sedang dipilih (tersimpan di perangkat)
public static class DataRoket
{
    const string Kunci = "RoketDipilih";

    public static readonly JenisRoket[] Semua =
    {
        new JenisRoket
        {
            nama = "Elang", deskripsi = "Seimbang, cocok untuk pemula.",
            badan = new Color32(235, 237, 242, 255), aksen = new Color32(200, 55, 50, 255),
            api = new Color32(255, 120, 30, 255),
            kecepatanGerak = 8f, nilaiKelincahan = 3, bonusNyawa = 0
        },
        new JenisRoket
        {
            nama = "Kilat", deskripsi = "Sangat gesit, tapi lebih rapuh.",
            badan = new Color32(40, 60, 110, 255), aksen = new Color32(93, 202, 165, 255),
            api = new Color32(90, 170, 255, 255),
            kecepatanGerak = 12f, nilaiKelincahan = 5, bonusNyawa = -1
        },
        new JenisRoket
        {
            nama = "Banteng", deskripsi = "Lambat, tapi tangguh.",
            badan = new Color32(95, 110, 70, 255), aksen = new Color32(250, 199, 117, 255),
            api = new Color32(255, 200, 80, 255),
            kecepatanGerak = 5.5f, nilaiKelincahan = 2, bonusNyawa = 1
        },
    };

    public static int IndexTerpilih
    {
        get { return Mathf.Clamp(PlayerPrefs.GetInt(Kunci, 0), 0, Semua.Length - 1); }
        set { PlayerPrefs.SetInt(Kunci, value); PlayerPrefs.Save(); }
    }

    public static JenisRoket Terpilih
    {
        get { return Semua[IndexTerpilih]; }
    }
}

// Panel pilihan roket di Main Menu
public static class PilihanRoket
{
    public static GameObject BuatPanel(Transform root, Action<string> saatDipilih)
    {
        GameObject panel = MenuUtama.BuatPanelLayar(root, "PanelPilihRoket", new Color32(6, 14, 34, 245));
        Vector2 atas = new Vector2(0.5f, 1f);
        Vector2 tengah = new Vector2(0.5f, 0.5f);

        MenuUtama.BuatTeks(panel.transform, "Pilih roket", 64, atas, new Vector2(0, -90),
            new Vector2(1200, 100), Color.white);

        int n = DataRoket.Semua.Length;
        Outline[] bingkai = new Outline[n];
        TextMeshProUGUI[] labelTombol = new TextMeshProUGUI[n];

        for (int i = 0; i < n; i++)
        {
            int index = i;
            JenisRoket r = DataRoket.Semua[i];
            float x = (i - (n - 1) / 2f) * 480f;

            // Kartu
            GameObject kartu = new GameObject("Kartu_" + r.nama, typeof(RectTransform));
            kartu.transform.SetParent(panel.transform, false);
            Image latarKartu = kartu.AddComponent<Image>();
            latarKartu.color = new Color32(16, 32, 64, 255);
            RectTransform rt = (RectTransform)kartu.transform;
            rt.anchorMin = tengah;
            rt.anchorMax = tengah;
            rt.pivot = tengah;
            rt.anchoredPosition = new Vector2(x, 10);
            rt.sizeDelta = new Vector2(420, 520);

            bingkai[i] = kartu.AddComponent<Outline>();
            bingkai[i].effectColor = MenuUtama.Oranye;
            bingkai[i].effectDistance = new Vector2(6, -6);

            // Gambar roket sederhana (dilihat dari belakang): api, sayap, badan, jendela
            BuatBentuk(kartu.transform, "Api", r.api, MenuUtama.SpriteLingkaran(), new Vector2(0, 80), new Vector2(70, 70));
            BuatBentuk(kartu.transform, "Sayap", r.aksen, null, new Vector2(0, 150), new Vector2(250, 30));
            BuatBentuk(kartu.transform, "Badan", r.badan, MenuUtama.SpriteLingkaran(), new Vector2(0, 150), new Vector2(150, 150));
            BuatBentuk(kartu.transform, "Jendela", new Color32(90, 150, 220, 255), MenuUtama.SpriteLingkaran(),
                new Vector2(0, 185), new Vector2(40, 40));

            // Teks
            MenuUtama.BuatTeks(kartu.transform, r.nama, 48, tengah, new Vector2(0, 20), new Vector2(380, 70), Color.white);
            MenuUtama.BuatTeks(kartu.transform, r.deskripsi, 26, tengah, new Vector2(0, -35), new Vector2(380, 50),
                MenuUtama.TeksBiruMuda);
            string teksNyawa = r.bonusNyawa >= 0 ? "+" + r.bonusNyawa : r.bonusNyawa.ToString();
            MenuUtama.BuatTeks(kartu.transform, "Kelincahan: " + r.nilaiKelincahan + "/5\nNyawa: " + teksNyawa,
                30, tengah, new Vector2(0, -110), new Vector2(380, 90), MenuUtama.Kuning);

            // Tombol pilih
            Button tombol = MenuUtama.BuatTombol(kartu.transform, "Pilih", tengah, new Vector2(0, -205),
                new Vector2(300, 70), MenuUtama.Oranye, MenuUtama.TeksOranye, () => { });
            labelTombol[i] = tombol.GetComponentInChildren<TextMeshProUGUI>();
            tombol.onClick.AddListener(() =>
            {
                DataRoket.IndexTerpilih = index;
                Segarkan(bingkai, labelTombol);
                if (saatDipilih != null) saatDipilih(DataRoket.Semua[index].nama);
            });
        }

        Segarkan(bingkai, labelTombol);

        MenuUtama.BuatTombol(panel.transform, "Tutup", tengah, new Vector2(0, -420), new Vector2(520, 90),
            MenuUtama.Biru, MenuUtama.TeksBiruMuda, () => panel.SetActive(false));

        return panel;
    }

    static void Segarkan(Outline[] bingkai, TextMeshProUGUI[] labelTombol)
    {
        int terpilih = DataRoket.IndexTerpilih;
        for (int i = 0; i < bingkai.Length; i++)
        {
            bingkai[i].enabled = i == terpilih;
            labelTombol[i].text = i == terpilih ? "Dipakai" : "Pilih";
        }
    }

    static void BuatBentuk(Transform induk, string nama, Color warna, Sprite sprite, Vector2 posisi, Vector2 ukuran)
    {
        GameObject g = new GameObject(nama, typeof(RectTransform));
        g.transform.SetParent(induk, false);
        Image img = g.AddComponent<Image>();
        img.color = warna;
        img.sprite = sprite;
        img.raycastTarget = false;
        RectTransform rt = (RectTransform)g.transform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = posisi;
        rt.sizeDelta = ukuran;
    }
}