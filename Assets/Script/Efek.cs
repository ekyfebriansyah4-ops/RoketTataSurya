using System.Collections;
using UnityEngine;

// Efek suara (dibuat dari kode, tanpa file audio) dan getaran kamera/HP.
// Cara pakai dari script lain: Efek.SuaraBenar(), Efek.SuaraSalah(), Efek.KenaMeteor(), dst.
public class Efek : MonoBehaviour
{
    static Efek instance;

    AudioSource sumber;
    AudioClip klipBenar, klipSalah, klipMenang, klipMeteor;
    bool sedangGetar;

    static Efek Ambil()
    {
        if (instance == null)
            instance = new GameObject("Efek").AddComponent<Efek>();
        return instance;
    }

    void Awake()
    {
        sumber = gameObject.AddComponent<AudioSource>();
        sumber.playOnAwake = false;

        klipBenar = BuatNada("Benar", new[] { 660f, 880f }, 0.12f);
        klipSalah = BuatNada("Salah", new[] { 330f, 220f }, 0.18f);
        klipMenang = BuatNada("Menang", new[] { 523f, 659f, 784f, 1047f }, 0.15f);
        klipMeteor = BuatDentuman();
    }

    // ====== Dipanggil dari script lain ======
    public static void SuaraBenar() { Efek e = Ambil(); e.sumber.PlayOneShot(e.klipBenar); }
    public static void SuaraSalah() { Efek e = Ambil(); e.sumber.PlayOneShot(e.klipSalah); }
    public static void SuaraMenang() { Efek e = Ambil(); e.sumber.PlayOneShot(e.klipMenang); }

    public static void KenaMeteor()
    {
        Efek e = Ambil();
        e.sumber.PlayOneShot(e.klipMeteor);
        if (!e.sedangGetar) e.StartCoroutine(e.GetarKamera(0.3f, 0.25f));
#if UNITY_ANDROID && !UNITY_EDITOR
        Handheld.Vibrate(); // HP ikut bergetar
#endif
    }

    // ====== Getaran kamera ======
    IEnumerator GetarKamera(float durasi, float kekuatan)
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;

        sedangGetar = true;
        Vector3 posisiAwal = cam.transform.localPosition;
        float t = 0f;
        while (t < durasi)
        {
            Vector2 acak = Random.insideUnitCircle * kekuatan * (1f - t / durasi);
            cam.transform.localPosition = posisiAwal + new Vector3(acak.x, acak.y, 0f);
            t += Time.deltaTime;
            yield return null;
        }
        cam.transform.localPosition = posisiAwal;
        sedangGetar = false;
    }

    // ====== Pembuat suara ======
    AudioClip BuatNada(string nama, float[] frekuensi, float durasiNada)
    {
        int rate = 44100;
        int perNada = (int)(rate * durasiNada);
        float[] data = new float[perNada * frekuensi.Length];

        for (int n = 0; n < frekuensi.Length; n++)
        {
            for (int i = 0; i < perNada; i++)
            {
                float t = (float)i / rate;
                float volume = 1f - (float)i / perNada; // memudar
                data[n * perNada + i] = Mathf.Sin(2f * Mathf.PI * frekuensi[n] * t) * volume * 0.4f;
            }
        }

        AudioClip klip = AudioClip.Create(nama, data.Length, 1, rate, false);
        klip.SetData(data, 0);
        return klip;
    }

    AudioClip BuatDentuman()
    {
        int rate = 44100;
        int panjang = (int)(rate * 0.35f);
        float[] data = new float[panjang];
        float halus = 0f;

        for (int i = 0; i < panjang; i++)
        {
            float volume = Mathf.Pow(1f - (float)i / panjang, 2f);
            halus = Mathf.Lerp(halus, Random.Range(-1f, 1f), 0.08f); // derau yang diredam = suara "dum"
            data[i] = Mathf.Clamp(halus * volume * 3f, -1f, 1f);
        }

        AudioClip klip = AudioClip.Create("Meteor", panjang, 1, rate, false);
        klip.SetData(data, 0);
        return klip;
    }
}