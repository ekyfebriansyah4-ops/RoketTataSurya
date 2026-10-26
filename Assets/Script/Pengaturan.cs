using UnityEngine;

public enum Kesulitan { Mudah, Normal, Sulit }

// Menyimpan pengaturan pemain (tingkat kesulitan dan suara) di perangkat.
public static class Pengaturan
{
    const string KunciKesulitan = "Kesulitan";
    const string KunciSuara = "SuaraAktif";

    // Terapkan pengaturan suara setiap kali game dijalankan
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void TerapkanSaatMulai()
    {
        AudioListener.volume = SuaraAktif ? 1f : 0f;
    }

    public static Kesulitan Tingkat
    {
        get { return (Kesulitan)PlayerPrefs.GetInt(KunciKesulitan, (int)Kesulitan.Normal); }
        set { PlayerPrefs.SetInt(KunciKesulitan, (int)value); PlayerPrefs.Save(); }
    }

    public static bool SuaraAktif
    {
        get { return PlayerPrefs.GetInt(KunciSuara, 1) == 1; }
        set
        {
            PlayerPrefs.SetInt(KunciSuara, value ? 1 : 0);
            PlayerPrefs.Save();
            AudioListener.volume = value ? 1f : 0f;
        }
    }

    public static string NamaTingkat
    {
        get { return Tingkat.ToString(); }
    }

    public static int NyawaAwal
    {
        get
        {
            switch (Tingkat)
            {
                case Kesulitan.Mudah: return 5;
                case Kesulitan.Sulit: return 2;
                default: return 3;
            }
        }
    }

    // Pengali kecepatan cincin dan meteor
    public static float PengaliKecepatan
    {
        get
        {
            switch (Tingkat)
            {
                case Kesulitan.Mudah: return 0.8f;
                case Kesulitan.Sulit: return 1.3f;
                default: return 1f;
            }
        }
    }

    // Pengali jeda antar meteor (lebih besar = meteor lebih jarang)
    public static float PengaliJedaMeteor
    {
        get
        {
            switch (Tingkat)
            {
                case Kesulitan.Mudah: return 1.6f;
                case Kesulitan.Sulit: return 0.6f;
                default: return 1f;
            }
        }
    }
}