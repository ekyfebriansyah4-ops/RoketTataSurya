using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Latar luar angkasa realistis: nebula, bintang berpendar, planet bertekstur, dan asteroid.
// Dibuat otomatis di scene Gameplay dan MainMenu.
public class LuarAngkasa : MonoBehaviour
{
    [Header("Bintang")]
    public int jumlahBintang = 300;
    public float kecepatanBintang = 20f;

    [Header("Meteor")]
    public float jedaMinMeteor = 2.5f;
    public float jedaMaksMeteor = 4.5f;
    public float kecepatanMeteor = 15f;
    public float jarakMunculMeteor = 45f;

    [Header("Planet latar")]
    public Vector3 posisiPlanet = new Vector3(14f, 6f, 120f);
    public float ukuranPlanet = 30f;

    // Warna khas tiap planet (dipakai juga oleh UI)
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

    public static Color32 WarnaPlanet(int index)
    {
        return warnaPlanet[Mathf.Clamp(index, 0, warnaPlanet.Length - 1)];
    }

    readonly List<Transform> bintang = new List<Transform>();
    Material[] matBintang;
    Material matMeteor, matPlanet;
    Transform planet;
    GameObject cincinSaturnus;
    float timerMeteor;
    int zonaTerakhir = -1;

    // ====== Membuat objek ini otomatis ======
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
        if ((scene.name == "Gameplay" || scene.name == "MainMenu") && FindAnyObjectByType<LuarAngkasa>() == null)
            new GameObject("LuarAngkasa").AddComponent<LuarAngkasa>();
    }

    // ====== Siklus ======
    void Start()
    {
        matBintang = new[]
        {
            Visual.Bercahaya(new Color(1f, 1f, 1f), 2.5f),
            Visual.Bercahaya(new Color(0.75f, 0.85f, 1f), 3f),  // kebiruan
            Visual.Bercahaya(new Color(1f, 0.9f, 0.75f), 2f),   // kekuningan
        };
        matMeteor = Visual.Lit(Color.white, 0f, 0.15f, Visual.TeksturBatu());
        matPlanet = Visual.Lit(Color.white, 0f, 0.25f, Visual.TeksturPlanet(0));

        BuatNebula();
        BuatBintang();
        BuatPlanet();
        BuatCincinSaturnus();
        PerbaruiPlanet();
        timerMeteor = 4f;
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

        // Meteor hanya muncul saat bermain
        if (SceneManager.GetActiveScene().name == "Gameplay")
        {
            timerMeteor -= Time.deltaTime;
            if (timerMeteor <= 0f)
            {
                BuatMeteor();
                timerMeteor = Random.Range(jedaMinMeteor, jedaMaksMeteor) * Pengaturan.PengaliJedaMeteor;
            }
        }

        // Planet berotasi pelan dan teksturnya mengikuti zona
        if (planet != null)
        {
            planet.Rotate(Vector3.up, 4f * Time.deltaTime, Space.Self);
            PerbaruiPlanet();
        }
    }

    // ====== Nebula (latar jauh) ======
    void BuatNebula()
    {
        GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = "Nebula";
        Destroy(q.GetComponent<Collider>());
        q.transform.SetParent(transform, false);
        q.transform.position = new Vector3(0f, 0f, 160f);
        q.transform.localScale = new Vector3(420f, 236f, 1f);
        q.GetComponent<Renderer>().sharedMaterial = Visual.Bercahaya(Color.white, 1f, Visual.TeksturNebula());
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
            s.transform.localScale = Vector3.one * Random.Range(0.03f, 0.09f);
            s.transform.position = PosisiBintangAcak(Random.Range(0f, 80f));
            s.GetComponent<Renderer>().sharedMaterial = matBintang[Random.Range(0, matBintang.Length)];
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
        p.transform.rotation = Quaternion.Euler(0f, 0f, -15f); // sumbu sedikit miring
        p.transform.localScale = Vector3.one * ukuranPlanet;
        p.GetComponent<Renderer>().sharedMaterial = matPlanet;
        planet = p.transform;
    }

    void BuatCincinSaturnus()
    {
        cincinSaturnus = new GameObject("CincinSaturnus");
        cincinSaturnus.transform.SetParent(transform, false);
        cincinSaturnus.transform.position = posisiPlanet;
        cincinSaturnus.transform.rotation = Quaternion.Euler(70f, 0f, -15f);
        cincinSaturnus.transform.localScale = Vector3.one * ukuranPlanet;
        cincinSaturnus.AddComponent<MeshFilter>().sharedMesh = Visual.MeshCincin(0.65f, 1.2f, 96);
        cincinSaturnus.AddComponent<MeshRenderer>().sharedMaterial =
            Visual.Lit(Color.white, 0f, 0.3f, Visual.TeksturCincinSaturnus());
        cincinSaturnus.SetActive(false);
    }

    void PerbaruiPlanet()
    {
        int z = GameManager.Instance != null ? Mathf.Clamp(GameManager.Instance.zonaSekarang, 0, 7) : 0;
        if (z == zonaTerakhir) return;
        zonaTerakhir = z;
        matPlanet.SetTexture("_BaseMap", Visual.TeksturPlanet(z));
        if (cincinSaturnus != null) cincinSaturnus.SetActive(z == 5);
    }

    // ====== Meteor (asteroid) ======
    void BuatMeteor()
    {
        GameObject m = new GameObject("Meteor");
        m.transform.position = new Vector3(Random.Range(-3f, 3f), Random.Range(-1.8f, 1.8f), jarakMunculMeteor);
        m.transform.rotation = Random.rotation;
        m.transform.localScale = Vector3.one * Random.Range(0.6f, 1f);

        m.AddComponent<MeshFilter>().sharedMesh = Visual.MeshAsteroid(Random.Range(0, 6));
        m.AddComponent<MeshRenderer>().sharedMaterial = matMeteor;

        SphereCollider col = m.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.4f;

        Meteor mt = m.AddComponent<Meteor>();
        mt.kecepatan = kecepatanMeteor * Pengaturan.PengaliKecepatan;
    }
}

// Perilaku satu meteor: bergerak, berputar, dan mengurangi nyawa saat menabrak roket
public class Meteor : MonoBehaviour
{
    public float kecepatan = 15f;
    Vector3 putaran;

    void Start()
    {
        putaran = Random.insideUnitSphere * 90f;
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