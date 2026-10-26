using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public enum JenisPowerUp { Perisai, Nyawa }

// Memunculkan power-up secara berkala di Gameplay. Dibuat otomatis.
public class PengaturPowerUp : MonoBehaviour
{
    public float jedaMin = 12f;
    public float jedaMaks = 18f;
    public float kecepatan = 12f;
    public float jarakMuncul = 45f;

    static PengaturPowerUp instance;
    AudioSource sumber;
    AudioClip klipAmbil;
    Material matPerisai, matNyawa;
    float timer;

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
        if (scene.name == "Gameplay" && FindAnyObjectByType<PengaturPowerUp>() == null)
        {
            new GameObject("PengaturPowerUp").AddComponent<PengaturPowerUp>();
        }
    }

    public static void MainkanSuara()
    {
        if (instance != null) instance.sumber.PlayOneShot(instance.klipAmbil);
    }

    void Awake()
    {
        instance = this;
        sumber = gameObject.AddComponent<AudioSource>();
        sumber.playOnAwake = false;
        klipAmbil = BuatNadaNaik();

        matPerisai = BuatMaterial(new Color32(55, 138, 221, 255));
        matNyawa = BuatMaterial(new Color32(226, 75, 74, 255));
    }

    void Start()
    {
        timer = 10f; // power-up pertama setelah 10 detik
    }

    void Update()
    {
        if (GameManager.Instance == null) return;
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            BuatPowerUp();
            timer = Random.Range(jedaMin, jedaMaks);
        }
    }

    void BuatPowerUp()
    {
        JenisPowerUp jenis = Random.value < 0.5f ? JenisPowerUp.Perisai : JenisPowerUp.Nyawa;

        GameObject g = new GameObject("PowerUp_" + jenis);
        g.transform.position = new Vector3(Random.Range(-2.5f, 2.5f), Random.Range(-1.5f, 1.5f), jarakMuncul);

        // Bentuk utama
        PrimitiveType bentuk = jenis == JenisPowerUp.Perisai ? PrimitiveType.Sphere : PrimitiveType.Cube;
        GameObject inti = GameObject.CreatePrimitive(bentuk);
        Destroy(inti.GetComponent<Collider>());
        inti.transform.SetParent(g.transform, false);
        inti.transform.localScale = Vector3.one * 0.5f;
        inti.GetComponent<Renderer>().sharedMaterial = jenis == JenisPowerUp.Perisai ? matPerisai : matNyawa;

        // Label di atasnya
        GameObject label = new GameObject("Label");
        label.transform.SetParent(g.transform, false);
        label.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        TextMeshPro tmp = label.AddComponent<TextMeshPro>();
        tmp.text = jenis == JenisPowerUp.Perisai ? "PERISAI" : "+1 NYAWA";
        tmp.fontSize = 3f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = jenis == JenisPowerUp.Perisai ? new Color32(181, 212, 244, 255) : new Color32(247, 193, 193, 255);
        tmp.rectTransform.sizeDelta = new Vector2(3f, 1f);

        // Trigger untuk diambil roket
        SphereCollider col = g.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.6f;

        PowerUp p = g.AddComponent<PowerUp>();
        p.jenis = jenis;
        p.kecepatan = kecepatan * Pengaturan.PengaliKecepatan;
        p.inti = inti.transform;
    }

    AudioClip BuatNadaNaik()
    {
        float[] frek = { 523f, 784f, 1047f };
        int rate = 44100;
        int perNada = (int)(rate * 0.08f);
        float[] data = new float[perNada * frek.Length];
        for (int n = 0; n < frek.Length; n++)
            for (int i = 0; i < perNada; i++)
            {
                float t = (float)i / rate;
                data[n * perNada + i] = Mathf.Sin(2f * Mathf.PI * frek[n] * t) * (1f - (float)i / perNada) * 0.35f;
            }
        AudioClip klip = AudioClip.Create("PowerUp", data.Length, 1, rate, false);
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

// Satu power-up: melaju, berputar, dan diambil saat menyentuh roket
public class PowerUp : MonoBehaviour
{
    public JenisPowerUp jenis;
    public float kecepatan = 12f;
    public Transform inti;

    void Update()
    {
        transform.position += Vector3.back * kecepatan * Time.deltaTime;
        if (inti != null) inti.Rotate(60f * Time.deltaTime, 120f * Time.deltaTime, 0f);
        if (transform.position.z < -5f) Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Player"))
        {
            PengaturPowerUp.MainkanSuara();
            if (GameManager.Instance != null) GameManager.Instance.AmbilPowerUp(jenis);
            Destroy(gameObject);
        }
    }
}