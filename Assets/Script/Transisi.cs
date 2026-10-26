using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Efek layar memudar hitam saat pindah scene.
// Cara pakai: Transisi.Muat("NamaScene");
public class Transisi : MonoBehaviour
{
    const float Durasi = 0.4f;

    static Transisi instance;
    Image tirai;
    bool sedangPindah;

    // Setiap game dimulai, layar memudar masuk dari hitam
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Siapkan()
    {
        Transisi t = Ambil();
        t.StartCoroutine(t.Pudar(1f, 0f));
    }

    static Transisi Ambil()
    {
        if (instance == null)
        {
            GameObject g = new GameObject("Transisi");
            DontDestroyOnLoad(g);
            instance = g.AddComponent<Transisi>();
            instance.BuatTirai();
        }
        return instance;
    }

    public static void Muat(string namaScene)
    {
        Transisi t = Ambil();
        if (t.sedangPindah) return; // cegah klik ganda
        t.StartCoroutine(t.Pindah(namaScene));
    }

    void BuatTirai()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // selalu paling atas
        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        GameObject g = new GameObject("Tirai", typeof(RectTransform));
        g.transform.SetParent(transform, false);
        tirai = g.AddComponent<Image>();
        tirai.color = Color.black;
        tirai.raycastTarget = false;

        RectTransform rt = (RectTransform)g.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    IEnumerator Pindah(string namaScene)
    {
        sedangPindah = true;
        tirai.raycastTarget = true; // blokir sentuhan selama transisi
        yield return Pudar(0f, 1f);

        Time.timeScale = 1f;
        SceneManager.LoadScene(namaScene);
        yield return null;

        yield return Pudar(1f, 0f);
        tirai.raycastTarget = false;
        sedangPindah = false;
    }

    IEnumerator Pudar(float dari, float ke)
    {
        float t = 0f;
        while (t < Durasi)
        {
            t += Time.unscaledDeltaTime; // tetap jalan walau game dijeda
            AturAlpha(Mathf.Lerp(dari, ke, t / Durasi));
            yield return null;
        }
        AturAlpha(ke);
    }

    void AturAlpha(float a)
    {
        Color c = tirai.color;
        c.a = a;
        tirai.color = c;
    }
}