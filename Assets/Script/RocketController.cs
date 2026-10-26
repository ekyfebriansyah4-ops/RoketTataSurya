using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class RocketController : MonoBehaviour
{
    [Header("Batas gerak roket")]
    public float batasX = 3f;
    public float batasY = 1.8f;

    [Header("Kehalusan gerak")]
    public float kecepatan = 8f;
    public float kemiringanMaks = 30f;

    private Vector3 target;

    void Start()
    {
               kecepatan = DataRoket.Terpilih.kecepatanGerak;
    }

    void Update()
    {
        // Ambil posisi jari/mouse, ubah ke rentang -1..1, lalu ke posisi dunia
        if (AmbilPosisiPointer(out Vector2 layar))
        {
            float nx = (layar.x / Screen.width) * 2f - 1f;
            float ny = (layar.y / Screen.height) * 2f - 1f;
            target = new Vector3(nx * batasX, ny * batasY, transform.position.z);
        }

        // Gerak halus menuju target
        Vector3 posisiLama = transform.position;
        transform.position = Vector3.Lerp(transform.position, target, kecepatan * Time.deltaTime);

        // Roket miring sesuai arah gerak ke kiri/kanan
        float kecepatanX = (transform.position.x - posisiLama.x) / Mathf.Max(Time.deltaTime, 0.0001f);
        float miring = Mathf.Clamp(-kecepatanX * 4f, -kemiringanMaks, kemiringanMaks);
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, 0, miring), 10f * Time.deltaTime);
    }

    bool AmbilPosisiPointer(out Vector2 posisi)
    {
#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            posisi = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            posisi = Mouse.current.position.ReadValue();
            return true;
        }
#else
        if (Input.touchCount > 0)
        {
            posisi = Input.GetTouch(0).position;
            return true;
        }
        if (Input.GetMouseButton(0))
        {
            posisi = Input.mousePosition;
            return true;
        }
#endif
        posisi = Vector2.zero;
        return false;
    }
}