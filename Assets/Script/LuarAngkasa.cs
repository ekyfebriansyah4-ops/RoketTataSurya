using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Mengatur latar luar angkasa: bintang, planet latar, dan meteor.
// Dibuat otomatis saat scene Gameplay dimulai.
public class LuarAngkasa : MonoBehaviour
{
    [Header("Bintang")]
    public int jumlahBintang = 200;
    public float kecepatanBintang = 20f;

    [Header("Meteor")]
    public float jedaMinMeteor = 2.5f;
    public float jedaMaksMeteor = 4.5f;
    public float kecepatanMeteor = 15f;
    public float jarakMunculMeteor = 45f;

    [Header("Planet latar")]
    public Vector3 posisiPlanet = new Vector3(14f, 6f, 120f);
    public float ukuranPlanet = 30f;

    // Warna planet sesuai urutan zona: Merkurius sampai Neptunus
    static readonly Color32[] warnaPlanet =
    {
        new Color32(180, 178, 169, 255), // Merkurius
        new Color32(239, 159, 39, 255),  // Venus
        new Color32(55, 138, 221, 255),  // Bumi
        new Color32(216, 90, 48, 255),   // Mars
        new Color32(186, 117, 23, 255),  // Jupiter
        new Color32(250, 199, 117, 255), // Saturnus
        new Color32(93, 202, 165, 255),  // Uranus
        new Color32(24, 95, 165, 255),   // Neptunus
    };

    readonly List<Transform> bintang = new List<Transform>();
    Material matBintang, matMeteor, matPlanet;
    Transform planet;
    float timerMeteor;
    int zonaTerakhir = -1;

    // ====== Membuat objek ini otomatis di scene Gameplay ======
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
        if (scene.name == "Gameplay" && FindAnyObjectByType<LuarAngkasa>() == null)
        {
            new GameObject("LuarAngkasa").AddComponent<LuarAngkasa>();
        }
    }

    // ====== Siklus ======
    void Start()
    {
        matBintang = BuatMaterial("Universal Render Pipeline/Unlit", Color.white);
        matMeteor = BuatMaterial("Universal Render Pipeline/Lit", new Color32(110, 100, 90, 255));
        matPlanet = BuatMaterial("Universal Render Pipeline/Lit", warnaPlanet[0]);

        BuatBintang();
        BuatPlanet();
        timerMeteor = 4f; // meteor pertama muncul setelah 4 detik
    }

    void Update()
    {
        // Bintang melaju ke arah kamera, lalu didaur ulang ke depan
        float langkah = kecepatanBintang * Time.deltaTime;
        foreach (Transform b in bintang)
        {
            b.position += Vector3.back * langkah;
            if (b.position.z < -10f)
                b.position = PosisiBintangAcak(80f);
        }

        // Munculkan meteor secara berkala
        timerMeteor -= Time.deltaTime;
        if (timerMeteor <= 0f)
        {
            BuatMeteor();
            timerMeteor = Random.Range(jedaMinMeteor, jedaMaksMeteor);
        }

        // Planet berputar pelan dan warnanya mengikuti zona
        if (planet != null)
        {
            planet.Rotate(0f, 5f * Time.deltaTime, 0f);
            PerbaruiWarnaPlanet();
        }
    }

    // ====== Bintang ======
    void BuatBintang()
    {
        for (int i = 0; i < jumlahBintang; i++)
        {
            GameObject s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            s.name = "Bintang";
            Destroy(s.GetComponent<Collider>());
            s.transform.SetParent(transform, false);
            s.transform.localScale = Vector3.one * Random.Range(0.04f, 0.1f);
            s.transform.position = PosisiBintangAcak(Random.Range(0f, 80f));
            s.GetComponent<Renderer>().sharedMaterial = matBintang;
            bintang.Add(s.transform);
        }
    }

    Vector3 PosisiBintangAcak(float z)
    {
        return new Vector3(Random.Range(-30f, 30f), Random.Range(-18f, 18f), z);
    }

    // ====== Planet latar ======
    void BuatPlanet()
    {
        GameObject p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        p.name = "PlanetLatar";
        Destroy(p.GetComponent<Collider>());
        p.transform.SetParent(transform, false);
        p.transform.position = posisiPlanet;
        p.transform.localScale = Vector3.one * ukuranPlanet;
        p.GetComponent<Renderer>().sharedMaterial = matPlanet;
        planet = p.transform;
    }

    void PerbaruiWarnaPlanet()
    {
        if (GameManager.Instance == null) return;
        int z = Mathf.Clamp(GameManager.Instance.zonaSekarang, 0, warnaPlanet.Length - 1);
        if (z == zonaTerakhir) return;
        zonaTerakhir = z;
        matPlanet.SetColor("_BaseColor", warnaPlanet[z]);
    }

    // ====== Meteor ======
    void BuatMeteor()
    {
        GameObject m = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        m.name = "Meteor";
        m.transform.position = new Vector3(Random.Range(-3f, 3f), Random.Range(-1.8f, 1.8f), jarakMunculMeteor);

        float s = Random.Range(0.5f, 0.9f);
        m.transform.localScale = new Vector3(s, s * Random.Range(0.7f, 1f), s * Random.Range(0.8f, 1.1f));
        m.GetComponent<Renderer>().sharedMaterial = matMeteor;

        // Collider dijadikan trigger untuk mendeteksi tabrakan dengan roket
        SphereCollider col = m.GetComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.4f;

        Meteor mt = m.AddComponent<Meteor>();
        mt.kecepatan = kecepatanMeteor;
    }

    // ====== Utilitas ======
    Material BuatMaterial(string namaShader, Color warna)
    {
        Material mat = new Material(Shader.Find(namaShader));
        mat.SetColor("_BaseColor", warna);
        return mat;
    }
}

// Perilaku satu meteor: bergerak, berputar, dan mengurangi nyawa saat menabrak roket
public class Meteor : MonoBehaviour
{
    public float kecepatan = 15f;
    Vector3 putaran;

    void Start()
    {
        putaran = Random.insideUnitSphere * 120f;
    }

    void Update()
    {
        transform.position += Vector3.back * kecepatan * Time.deltaTime;
        transform.Rotate(putaran * Time.deltaTime);

        if (transform.position.z < -5f)
            Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Player"))
        {
            if (GameManager.Instance != null)
                GameManager.Instance.KenaMeteor();
            Destroy(gameObject);
        }
    }
}