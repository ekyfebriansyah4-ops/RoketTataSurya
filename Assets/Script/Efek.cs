using System.Collections;
using UnityEngine;

// Efek suara, getaran, ledakan pecahan, dan kedip roket. Semua suara dibuat dari kode.
public class Efek : MonoBehaviour
{
    static Efek instance;

    AudioSource sumber;
    AudioClip klipBenar, klipSalah, klipMenang, klipMeteor;
    Material matPecahan, matPercikan;
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

        matPecahan = BuatMaterial(new Color32(130, 120, 110, 255));
        matPercikan = BuatMaterial(new Color32(239, 140, 39, 255));
    }

    // ====== Dipanggil dari script lain ======
    public static void SuaraBenar()
    {
        Efek e = Ambil();
        e.sumber.PlayOneShot(e.klipBenar);
    }

    public static void SuaraSalah()
    {
        Efek e = Ambil();
        e.sumber.PlayOneShot(e.klipSalah);
        Getar(0.15f, 0.1f);
    }

    public static void SuaraMenang()
    {
        Efek e = Ambil();
        e.sumber.PlayOneShot(e.klipMenang);
    }

    public static void Getar(float durasi, float kekuatan)
    {
        Efek e = Ambil();
        if (!e.sedangGetar) e.StartCoroutine(e.GetarKamera(durasi, kekuatan));
    }

    public static void KenaMeteor(float lamaKebal)
    {
        Efek e = Ambil();
        e.sumber.PlayOneShot(e.klipMeteor);
        Getar(0.3f, 0.25f);

        GameObject roket = GameObject.FindWithTag("Player");
        if (roket != null)
        {
            e.Ledakan(roket.transform.position + Vector3.forward * 0.8f);
            e.StartCoroutine(e.KedipRoket(roket, lamaKebal));
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        Handheld.Vibrate(); // HP ikut bergetar
#endif
    }

    // ====== Ledakan pecahan (memakai Rigidbody) ======
    void Ledakan(Vector3 posisi)
    {
        for (int i = 0; i < 14; i++)
        {
            GameObject p = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p.name = "Pecahan";
            Destroy(p.GetComponent<Collider>());
            p.transform.position = posisi;
            p.transform.rotation = Random.rotation;
            p.transform.localScale = Vector3.one * Random.Range(0.08f, 0.2f);
            p.GetComponent<Renderer>().sharedMaterial = (i % 3 == 0) ? matPercikan : matPecahan;

            Rigidbody rb = p.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.linearDamping = 1f;
            rb.AddForce(Random.onUnitSphere * Random.Range(3f, 6f) + Vector3.back * 4f, ForceMode.VelocityChange);
            rb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.VelocityChange);

            p.AddComponent<Pecahan>();
        }
    }

    // ====== Roket berkedip selama kebal ======
    IEnumerator KedipRoket(GameObject roket, float lama)
    {
        Renderer[] semua = roket.GetComponentsInChildren<Renderer>();
        float t = 0f;
        bool tampil = true;

        while (t < lama && roket != null)
        {
            tampil = !tampil;
            foreach (Renderer r in semua) if (r != null) r.enabled = tampil;
            yield return new WaitForSeconds(0.1f);
            t += 0.1f;
        }
        foreach (Renderer r in semua) if (r != null) r.enabled = true;
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
                float volume = 1f - (float)i / perNada;
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
            halus = Mathf.Lerp(halus, Random.Range(-1f, 1f), 0.08f);
            data[i] = Mathf.Clamp(halus * volume * 3f, -1f, 1f);
        }

        AudioClip klip = AudioClip.Create("Meteor", panjang, 1, rate, false);
        klip.SetData(data, 0);
        return klip;
    }

    Material BuatMaterial(Color warna)
    {
        Material m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", warna);
        return m;
    }
}

// Satu pecahan ledakan: mengecil lalu hilang
public class Pecahan : MonoBehaviour
{
    const float Umur = 0.8f;
    float t;
    Vector3 skalaAwal;

    void Start()
    {
        skalaAwal = transform.localScale;
    }

    void Update()
    {
        t += Time.deltaTime;
        transform.localScale = skalaAwal * Mathf.Max(0f, 1f - t / Umur);
        if (t >= Umur) Destroy(gameObject);
    }
}

// Musik latar yang dibuat dari kode, terus berjalan saat pindah scene
public class Musik : MonoBehaviour
{
    static Musik instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void MulaiMusik()
    {
        if (instance != null) return;
        GameObject g = new GameObject("Musik");
        DontDestroyOnLoad(g);
        instance = g.AddComponent<Musik>();
    }

    void Awake()
    {
        AudioSource a = gameObject.AddComponent<AudioSource>();
        a.clip = BuatMusik();
        a.loop = true;
        a.volume = 0.3f;
        a.Play();
    }

    // 4 akor (Am - F - C - G), masing-masing 2 detik, dengan pad lembut dan arpeggio
    AudioClip BuatMusik()
    {
        int rate = 44100;
        float durasiAkor = 2f;
        float[][] akor =
        {
            new[] { 220f, 261.63f, 329.63f },  // Am
            new[] { 174.61f, 220f, 261.63f },  // F
            new[] { 261.63f, 329.63f, 392f },  // C
            new[] { 196f, 246.94f, 293.66f },  // G
        };

        int perAkor = (int)(rate * durasiAkor);
        float[] data = new float[perAkor * akor.Length];
        float duaPi = 2f * Mathf.PI;

        for (int a = 0; a < akor.Length; a++)
        {
            for (int i = 0; i < perAkor; i++)
            {
                float t = (float)i / rate;

                // Pad: masuk dan keluar perlahan
                float envPad = Mathf.Min(1f, t / 0.4f) * Mathf.Min(1f, (durasiAkor - t) / 0.4f);
                float pad = 0f;
                foreach (float f in akor[a]) pad += Mathf.Sin(duaPi * f * t);
                pad *= 0.06f * envPad;

                // Arpeggio: 8 nada pendek per akor
                int k = (int)(t / 0.25f);
                float tn = t - k * 0.25f;
                float fArp = akor[a][k % 3] * 2f;
                float envArp = Mathf.Min(1f, tn / 0.005f) * Mathf.Exp(-tn * 8f);
                float arp = Mathf.Sin(duaPi * fArp * tn) * envArp * 0.1f;

                data[a * perAkor + i] = pad + arp;
            }
        }

        AudioClip klip = AudioClip.Create("MusikLatar", data.Length, 1, rate, false);
        klip.SetData(data, 0);
        return klip;
    }
}