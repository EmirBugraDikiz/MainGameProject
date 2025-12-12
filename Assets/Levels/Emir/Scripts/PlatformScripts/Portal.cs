using UnityEngine;

public class Portal : MonoBehaviour
{
    [Header("Nereye ışınlanacak?")]
    public Transform destination;

    [Header("Rotasyonu hizala")]
    public bool alignRotation = true;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Player'ın CharacterController'ını geçici olarak kapat
        CharacterController cc = other.GetComponent<CharacterController>();
        if (cc != null)
            cc.enabled = false;

        // Pozisyonu taşı
        other.transform.position = destination.position;

        // Yönünü portal çıkışına göre ayarla (isteğe bağlı)
        if (alignRotation)
        {
            Vector3 euler = destination.rotation.eulerAngles;
            other.transform.rotation = Quaternion.Euler(0, euler.y, 0);
        }

        // Tekrar aç
        if (cc != null)
            cc.enabled = true;
    }
}
