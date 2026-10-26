using UnityEngine;

// Ditempel otomatis ke setiap cincin jawaban oleh GameManager
public class AnswerRing : MonoBehaviour
{
    public bool benar;

    void OnTriggerEnter(Collider other)
    {
        // Cek apakah yang menembus cincin adalah roket (tag Player)
        if (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Player"))
        {
            GameManager.Instance.PilihJawaban(benar);
        }
    }
}