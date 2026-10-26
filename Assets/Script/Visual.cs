using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Pusat tampilan realistis: post-processing, pencahayaan, material bercahaya,
// tekstur planet prosedural, mesh asteroid, dan mesh cincin. Dibuat otomatis.
public class Visual : MonoBehaviour
{
    const string NamaTemplateUnlit = "MatUnlit";

#if UNITY_EDITOR
    // Membuat template material Unlit di folder Resources supaya shader-nya ikut ter-build ke Android
    [UnityEditor.InitializeOnLoadMethod]
    static void PastikanTemplateMaterial()
    {
        UnityEditor.EditorApplication.delayCall += () =>
        {
            string path = "Assets/Resources/" + NamaTemplateUnlit + ".mat";
            if (UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
            Shader s = Shader.Find("Universal Render Pipeline/Unlit");
            if (s == null) return;
            if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Resources"))
                UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
            UnityEditor.AssetDatabase.CreateAsset(new Material(s), path);
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log("Template material Unlit dibuat di " + path);
        };
    }
#endif

    // ====== Dibuat otomatis di Gameplay dan MainMenu ======
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
        if ((scene.name == "Gameplay" || scene.name == "MainMenu") && FindAnyObjectByType<Visual>() == null)
            new GameObject("Visual").AddComponent<Visual>();
    }

    void Start()
    {
        AturKamera();
        AturPencahayaan();
        BuatPostProcessing();
    }

    // ====== Kamera, cahaya, post-processing ======
    void AturKamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color32(2, 4, 12, 255);

        UniversalAdditionalCameraData data = cam.GetUniversalAdditionalCameraData();
        data.renderPostProcessing = true;
        data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
    }

    void AturPencahayaan()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color32(22, 28, 48, 255);

        foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.type != LightType.Directional) continue;
            l.color = new Color32(255, 240, 220, 255); // cahaya matahari hangat
            l.intensity = 1.4f;
            l.shadows = LightShadows.Soft;
        }
    }

    void BuatPostProcessing()
    {
        GameObject g = new GameObject("PostProcessing");
        Volume volume = g.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10;

        VolumeProfile profil = ScriptableObject.CreateInstance<VolumeProfile>();

        Bloom bloom = profil.Add<Bloom>(true);
        bloom.intensity.Override(1.2f);
        bloom.threshold.Override(0.9f);
        bloom.scatter.Override(0.7f);

        Tonemapping tonemap = profil.Add<Tonemapping>(true);
        tonemap.mode.Override(TonemappingMode.ACES);

        Vignette vignette = profil.Add<Vignette>(true);
        vignette.intensity.Override(0.35f);
        vignette.smoothness.Override(0.4f);

        ColorAdjustments warna = profil.Add<ColorAdjustments>(true);
        warna.postExposure.Override(0.3f);
        warna.contrast.Override(12f);
        warna.saturation.Override(10f);

        volume.profile = profil;
    }

    // ====== Material ======
    static Material templateUnlit;

    public static Material Lit(Color warna, float metalik = 0f, float halus = 0.5f, Texture tekstur = null)
    {
        Material m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", warna);
        m.SetFloat("_Metallic", metalik);
        m.SetFloat("_Smoothness", halus);
        if (tekstur != null) m.SetTexture("_BaseMap", tekstur);
        return m;
    }

    // Material yang berpendar (warna di atas 1 akan ditangkap efek bloom)
    public static Material Bercahaya(Color warna, float intensitas = 2f, Texture tekstur = null)
    {
        if (templateUnlit == null) templateUnlit = Resources.Load<Material>(NamaTemplateUnlit);
        Material m = templateUnlit != null
            ? new Material(templateUnlit)
            : new Material(Shader.Find("Universal Render Pipeline/Unlit"));

        Color c = warna * intensitas;
        c.a = 1f;
        m.SetColor("_BaseColor", c);
        if (tekstur != null) m.SetTexture("_BaseMap", tekstur);
        return m;
    }

    // ====== Tepi bercahaya untuk cincin jawaban ======
    static Mesh meshTepi;
    static Material matTepi;

    public static void TambahTepiBercahaya(Transform induk)
    {
        if (meshTepi == null) meshTepi = MeshCincin(0.72f, 0.86f, 64);
        if (matTepi == null) matTepi = Bercahaya(new Color32(93, 202, 165, 255), 3f);

        GameObject tepi = new GameObject("TepiBercahaya");
        tepi.transform.SetParent(induk, false);
        tepi.transform.localPosition = new Vector3(0f, 0f, -0.05f);
        tepi.AddComponent<MeshFilter>().sharedMesh = meshTepi;
        tepi.AddComponent<MeshRenderer>().sharedMaterial = matTepi;
    }

    // ====== Mesh cincin (dua sisi) di bidang XY ======
    public static Mesh MeshCincin(float dalam, float luar, int segmen)
    {
        int n = segmen + 1;
        Vector3[] v = new Vector3[n * 4];
        Vector2[] uv = new Vector2[n * 4];
        int[] tri = new int[segmen * 12];

        for (int i = 0; i <= segmen; i++)
        {
            float a = i * Mathf.PI * 2f / segmen;
            float c = Mathf.Cos(a), s = Mathf.Sin(a);
            Vector3 pDalam = new Vector3(c * dalam, s * dalam, 0f);
            Vector3 pLuar = new Vector3(c * luar, s * luar, 0f);
            float u = (float)i / segmen;

            v[i * 2] = pDalam; v[i * 2 + 1] = pLuar;
            v[n * 2 + i * 2] = pDalam; v[n * 2 + i * 2 + 1] = pLuar;
            uv[i * 2] = new Vector2(u, 0f); uv[i * 2 + 1] = new Vector2(u, 1f);
            uv[n * 2 + i * 2] = new Vector2(u, 0f); uv[n * 2 + i * 2 + 1] = new Vector2(u, 1f);
        }

        int t = 0;
        int o = n * 2;
        for (int i = 0; i < segmen; i++)
        {
            int a0 = i * 2, a1 = i * 2 + 1, b0 = a0 + 2, b1 = a1 + 2;
            // sisi depan
            tri[t++] = a0; tri[t++] = b0; tri[t++] = a1;
            tri[t++] = a1; tri[t++] = b0; tri[t++] = b1;
            // sisi belakang (urutan dibalik)
            tri[t++] = a0 + o; tri[t++] = a1 + o; tri[t++] = b0 + o;
            tri[t++] = a1 + o; tri[t++] = b1 + o; tri[t++] = b0 + o;
        }

        Mesh mesh = new Mesh { name = "Cincin" };
        mesh.vertices = v;
        mesh.uv = uv;
        mesh.triangles = tri;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.hideFlags = HideFlags.DontUnloadUnusedAsset;
        return mesh;
    }

    // ====== Mesh asteroid (bola yang permukaannya dibuat tidak rata) ======
    static Mesh meshBola;
    static readonly Mesh[] cacheAsteroid = new Mesh[6];

    public static Mesh MeshAsteroid(int varian)
    {
        varian = Mathf.Clamp(varian, 0, cacheAsteroid.Length - 1);
        if (cacheAsteroid[varian] != null) return cacheAsteroid[varian];

        if (meshBola == null)
        {
            GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            meshBola = tmp.GetComponent<MeshFilter>().sharedMesh;
            Destroy(tmp);
        }

        Vector3[] v = meshBola.vertices;
        float o = varian * 13.37f + 3f;
        for (int i = 0; i < v.Length; i++)
        {
            Vector3 n = v[i].normalized;
            float d = 1f
                + 0.35f * (Mathf.PerlinNoise(n.x * 1.7f + o, n.y * 1.7f + o) - 0.5f)
                + 0.15f * (Mathf.PerlinNoise(n.z * 3.1f + o, n.x * 3.1f - o) - 0.5f);
            v[i] = n * 0.5f * d;
        }

        Mesh mesh = new Mesh { name = "Asteroid" + varian };
        mesh.vertices = v;
        mesh.triangles = meshBola.triangles;
        mesh.uv = meshBola.uv;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.hideFlags = HideFlags.DontUnloadUnusedAsset;
        cacheAsteroid[varian] = mesh;
        return mesh;
    }

    // ====== Noise ======
    // Noise yang menyambung mulus di arah horizontal (untuk tekstur bola)
    static float Fbm(float u, float v, float skala, float seed, int oktaf = 4)
    {
        float total = 0f, amp = 0.5f, frek = 1f, norm = 0f;
        float sudut = u * Mathf.PI * 2f;
        for (int i = 0; i < oktaf; i++)
        {
            float r = skala * frek / (Mathf.PI * 2f);
            float nx = Mathf.Cos(sudut) * r + seed;
            float nz = Mathf.Sin(sudut) * r + seed * 1.7f;
            float ny = v * skala * frek * 0.5f + seed * 0.3f;
            float n = (Mathf.PerlinNoise(nx, ny) + Mathf.PerlinNoise(nz, ny + 31.7f)) * 0.5f;
            total += n * amp;
            norm += amp;
            amp *= 0.5f;
            frek *= 2f;
        }
        return Mathf.Clamp01((total / norm - 0.5f) * 2.2f + 0.5f);
    }

    static float Fbm2D(float x, float y, float seed, int oktaf = 5)
    {
        float total = 0f, amp = 0.5f, frek = 1f, norm = 0f;
        for (int i = 0; i < oktaf; i++)
        {
            total += Mathf.PerlinNoise(x * frek + seed, y * frek + seed * 0.7f) * amp;
            norm += amp;
            amp *= 0.5f;
            frek *= 2f;
        }
        return total / norm;
    }

    static float K(float x) { return x * x; }

    static Texture2D BuatTekstur(int w, int h, Color[] piksel, TextureWrapMode wrapU)
    {
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, true);
        t.wrapModeU = wrapU;
        t.wrapModeV = TextureWrapMode.Clamp;
        t.SetPixels(piksel);
        t.Apply(true);
        t.hideFlags = HideFlags.DontUnloadUnusedAsset;
        return t;
    }

    // ====== Tekstur planet prosedural ======
    static readonly Texture2D[] cachePlanet = new Texture2D[8];

    public static Texture2D TeksturPlanet(int index)
    {
        index = Mathf.Clamp(index, 0, 7);
        if (cachePlanet[index] != null) return cachePlanet[index];

        int w = 512, h = 256;
        Color[] px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[y * w + x] = WarnaPermukaan(index, (x + 0.5f) / w, (y + 0.5f) / h);

        cachePlanet[index] = BuatTekstur(w, h, px, TextureWrapMode.Repeat);
        return cachePlanet[index];
    }

    static Color WarnaPermukaan(int i, float u, float v)
    {
        float lintang = Mathf.Abs(v - 0.5f) * 2f; // 0 = khatulistiwa, 1 = kutub

        switch (i)
        {
            case 0: // Merkurius: abu-abu berkawah
            {
                float n = Fbm(u, v, 8f, 1f);
                float kawah = Fbm(u, v, 24f, 5f, 3);
                Color c = Color.Lerp(new Color(0.35f, 0.33f, 0.31f), new Color(0.70f, 0.68f, 0.64f), n);
                if (kawah > 0.68f) c *= 0.7f;
                return c;
            }
            case 1: // Venus: awan kuning berpusar
            {
                float n = Fbm(u, v, 6f, 2f);
                float pita = Mathf.Sin(v * 18f + n * 6f) * 0.5f + 0.5f;
                return Color.Lerp(new Color(0.85f, 0.68f, 0.40f), new Color(0.98f, 0.88f, 0.65f), pita * 0.7f + n * 0.3f);
            }
            case 2: // Bumi: laut, benua, es kutub, awan
            {
                float daratan = Fbm(u, v, 5f, 3f, 5);
                float detail = Fbm(u, v, 14f, 9f, 3);
                Color c = daratan > 0.56f
                    ? Color.Lerp(new Color(0.20f, 0.42f, 0.16f), new Color(0.55f, 0.45f, 0.28f), detail)
                    : Color.Lerp(new Color(0.04f, 0.14f, 0.40f), new Color(0.08f, 0.30f, 0.60f), daratan / 0.56f);
                if (lintang > 0.84f) c = new Color(0.92f, 0.94f, 0.96f);
                float awan = Fbm(u, v, 7f, 13f, 4);
                if (awan > 0.6f) c = Color.Lerp(c, Color.white, Mathf.Clamp01((awan - 0.6f) * 3f) * 0.8f);
                return c;
            }
            case 3: // Mars: merah karat dengan kutub putih
            {
                float n = Fbm(u, v, 7f, 4f, 5);
                Color c = Color.Lerp(new Color(0.45f, 0.18f, 0.08f), new Color(0.82f, 0.42f, 0.22f), n);
                if (lintang > 0.88f) c = Color.Lerp(c, new Color(0.95f, 0.93f, 0.90f), 0.85f);
                return c;
            }
            case 4: // Jupiter: pita-pita dan Bintik Merah Besar
            {
                float n = Fbm(u, v, 5f, 5f);
                float pita = Mathf.Sin(v * 34f + n * 4f) * 0.5f + 0.5f;
                Color c = Color.Lerp(new Color(0.62f, 0.42f, 0.26f), new Color(0.93f, 0.86f, 0.74f), pita);
                float bintik = K((u - 0.3f) / 0.06f) + K((v - 0.38f) / 0.04f);
                if (bintik < 1f) c = Color.Lerp(c, new Color(0.75f, 0.32f, 0.18f), 1f - bintik);
                return c;
            }
            case 5: // Saturnus: pita lembut kekuningan
            {
                float n = Fbm(u, v, 4f, 6f);
                float pita = Mathf.Sin(v * 26f + n * 2f) * 0.5f + 0.5f;
                return Color.Lerp(new Color(0.78f, 0.66f, 0.44f), new Color(0.95f, 0.88f, 0.68f), pita);
            }
            case 6: // Uranus: biru kehijauan halus
            {
                float n = Fbm(u, v, 4f, 7f);
                float pita = Mathf.Sin(v * 14f + n) * 0.5f + 0.5f;
                return Color.Lerp(new Color(0.55f, 0.80f, 0.84f), new Color(0.70f, 0.90f, 0.92f), pita * 0.5f + n * 0.3f);
            }
            default: // Neptunus: biru tua dengan bintik gelap
            {
                float n = Fbm(u, v, 5f, 8f);
                float pita = Mathf.Sin(v * 20f + n * 3f) * 0.5f + 0.5f;
                Color c = Color.Lerp(new Color(0.12f, 0.24f, 0.62f), new Color(0.30f, 0.50f, 0.90f), pita);
                float bintik = K((u - 0.6f) / 0.05f) + K((v - 0.42f) / 0.035f);
                if (bintik < 1f) c = Color.Lerp(c, new Color(0.05f, 0.10f, 0.30f), 1f - bintik);
                return c;
            }
        }
    }

    // ====== Tekstur lain ======
    static Texture2D cacheBatu, cacheNebula, cacheCincinSaturnus;

    public static Texture2D TeksturBatu()
    {
        if (cacheBatu != null) return cacheBatu;
        int w = 128, h = 64;
        Color[] px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = (x + 0.5f) / w, v = (y + 0.5f) / h;
                float n = Fbm(u, v, 10f, 21f, 5);
                float bintik = Fbm(u, v, 30f, 37f, 2);
                Color c = Color.Lerp(new Color(0.22f, 0.19f, 0.17f), new Color(0.55f, 0.50f, 0.45f), n);
                if (bintik > 0.7f) c *= 0.6f;
                px[y * w + x] = c;
            }
        cacheBatu = BuatTekstur(w, h, px, TextureWrapMode.Repeat);
        return cacheBatu;
    }

    public static Texture2D TeksturNebula()
    {
        if (cacheNebula != null) return cacheNebula;
        int w = 512, h = 256;
        Color[] px = new Color[w * h];
        System.Random acak = new System.Random(11);

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float fx = (float)x / w, fy = (float)y / h;
                float n1 = Fbm2D(fx * 4f, fy * 2f, 1.3f);
                float n2 = Fbm2D(fx * 3f + 5f, fy * 1.5f + 3f, 7.1f);
                float tepi = 1f - Mathf.Clamp01(K(fx - 0.5f) / 0.22f + K(fy - 0.5f) / 0.28f);
                float massa = Mathf.Clamp01((n1 - 0.42f) * 2.5f) * tepi;
                Color c = Color.Lerp(new Color(0.28f, 0.10f, 0.45f), new Color(0.08f, 0.35f, 0.55f), n2) * massa * 0.8f;
                c.a = 1f;
                px[y * w + x] = c;
            }

        // bintik bintang kecil di latar
        for (int i = 0; i < 600; i++)
        {
            int idx = acak.Next(0, w * h);
            float b = (float)acak.NextDouble() * 0.6f + 0.4f;
            px[idx] = new Color(b, b, b, 1f);
        }

        cacheNebula = BuatTekstur(w, h, px, TextureWrapMode.Clamp);
        return cacheNebula;
    }

    public static Texture2D TeksturCincinSaturnus()
    {
        if (cacheCincinSaturnus != null) return cacheCincinSaturnus;
        int w = 8, h = 128;
        Color[] px = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            float r = (float)y / h; // 0 = sisi dalam, 1 = sisi luar
            float pita = Mathf.PerlinNoise(r * 40f, 0.5f);
            float celah = (r > 0.62f && r < 0.68f) ? 0.25f : 1f; // celah Cassini
            Color c = Color.Lerp(new Color(0.55f, 0.48f, 0.36f), new Color(0.92f, 0.86f, 0.70f), pita) * celah;
            c.a = 1f;
            for (int x = 0; x < w; x++) px[y * w + x] = c;
        }
        cacheCincinSaturnus = BuatTekstur(w, h, px, TextureWrapMode.Repeat);
        return cacheCincinSaturnus;
    }
}