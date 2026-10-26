using UnityEngine;
using UnityEngine.SceneManagement;

// Detail roket realistis: material logam, api berpendar, dan lampu mesin. Dibuat otomatis.
public class TampilanRoket : MonoBehaviour
{
    Transform apiDalam;
    Transform apiLuar;
    Light lampuMesin;

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
        if (scene.name == "Gameplay" && FindAnyObjectByType<TampilanRoket>() == null)
            new GameObject("TampilanRoket").AddComponent<TampilanRoket>();
    }

    void Start()
    {
        GameObject roket = GameObject.FindWithTag("Player");
        if (roket == null)
        {
            Debug.LogWarning("Roket dengan tag Player tidak ditemukan.");
            return;
        }
        Transform r = roket.transform;

        Material logamPutih = Visual.Lit(new Color(0.92f, 0.93f, 0.95f), 0.6f, 0.85f);
        Material logamMerah = Visual.Lit(new Color32(200, 55, 50, 255), 0.4f, 0.7f);
        Material logamGelap = Visual.Lit(new Color32(45, 45, 48, 255), 0.9f, 0.6f);
        Material kaca = Visual.Lit(new Color32(90, 150, 220, 255), 0.9f, 0.95f);
        Material apiOranye = Visual.Bercahaya(new Color32(255, 120, 30, 255), 4f);
        Material apiKuning = Visual.Bercahaya(new Color32(255, 220, 120, 255), 6f);

        // Ganti material bagian yang sudah ada dengan material logam
        GantiMaterial(r, "Badan", logamPutih);
        GantiMaterial(r, "SayapKiri", logamMerah);
        GantiMaterial(r, "SayapKanan", logamMerah);

        // Bagian tambahan
        BuatBagian(r, PrimitiveType.Sphere, "Hidung", new Vector3(0f, 0f, 0.75f), Vector3.zero,
            new Vector3(0.42f, 0.42f, 0.55f), logamMerah);
        BuatBagian(r, PrimitiveType.Sphere, "Jendela", new Vector3(0f, 0.2f, 0.25f), Vector3.zero,
            new Vector3(0.22f, 0.22f, 0.22f), kaca);
        BuatBagian(r, PrimitiveType.Cube, "SiripAtas", new Vector3(0f, 0.32f, -0.45f), Vector3.zero,
            new Vector3(0.06f, 0.35f, 0.35f), logamMerah);
        BuatBagian(r, PrimitiveType.Cylinder, "Mesin", new Vector3(0f, 0f, -0.82f), new Vector3(90f, 0f, 0f),
            new Vector3(0.34f, 0.08f, 0.34f), logamGelap);

        // Api berpendar (ditangkap efek bloom)
        apiLuar = BuatBagian(r, PrimitiveType.Sphere, "ApiLuar", new Vector3(0f, 0f, -0.95f), Vector3.zero,
            new Vector3(0.32f, 0.32f, 0.35f), apiOranye);
        apiDalam = BuatBagian(r, PrimitiveType.Sphere, "ApiDalam", new Vector3(0f, 0f, -1.05f), Vector3.zero,
            new Vector3(0.2f, 0.2f, 0.6f), apiKuning);

        // Lampu sungguhan dari mesin yang menerangi roket
        GameObject g = new GameObject("LampuMesin");
        g.transform.SetParent(r, false);
        g.transform.localPosition = new Vector3(0f, 0f, -1.2f);
        lampuMesin = g.AddComponent<Light>();
        lampuMesin.type = LightType.Point;
        lampuMesin.color = new Color32(255, 140, 50, 255);
        lampuMesin.range = 5f;
        lampuMesin.intensity = 2.5f;
    }

    void Update()
    {
        if (apiDalam == null || apiLuar == null) return;

        // Api dan lampu berkedip dengan pola acak yang halus
        float f = 0.85f + 0.3f * Mathf.PerlinNoise(Time.time * 15f, 0f);
        apiDalam.localScale = new Vector3(0.2f, 0.2f, 0.6f * f);
        apiLuar.localScale = new Vector3(0.32f * f, 0.32f * f, 0.35f);
        if (lampuMesin != null) lampuMesin.intensity = 2f + 1.5f * (f - 0.85f) / 0.3f;
    }

    void GantiMaterial(Transform induk, string nama, Material mat)
    {
        Transform t = induk.Find(nama);
        if (t != null) t.GetComponent<Renderer>().sharedMaterial = mat;
    }

    Transform BuatBagian(Transform induk, PrimitiveType bentuk, string nama, Vector3 posisi,
                         Vector3 rotasi, Vector3 skala, Material mat)
    {
        GameObject g = GameObject.CreatePrimitive(bentuk);
        g.name = nama;
        Destroy(g.GetComponent<Collider>()); // hanya hiasan
        g.transform.SetParent(induk, false);
        g.transform.localPosition = posisi;
        g.transform.localEulerAngles = rotasi;
        g.transform.localScale = skala;
        g.GetComponent<Renderer>().sharedMaterial = mat;
        return g.transform;
    }
}