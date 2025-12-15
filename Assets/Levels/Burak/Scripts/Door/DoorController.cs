using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("References")]
    public Transform door;            // Kapı mesh'i
    public AudioSource audioSource;   // Kapıya eklenmiş AudioSource

    [Header("Audio Clips")]
    public AudioClip openClip;        // Kapı açılma sesi
    public AudioClip closeClip;       // Kapı kapanma sesi

    [Header("Settings")]
    public float openOffsetZ = 1.2f;    // Açılınca Z'de ne kadar kayacak
    public float speed = 3f;          // Smooth hız

    private Vector3 closedPos;
    private Vector3 openPos;
    private bool isPlayerNear = false;

    void Start()
    {
        // Kapının kapalı pozisyonunu kaydet
        closedPos = door.localPosition;
        openPos = closedPos + new Vector3(0f, 0f, openOffsetZ);
    }

    void Update()
    {
        // Hedef pozisyon: oyuncu yakınsa open, değilse closed
        Vector3 targetPos = isPlayerNear ? openPos : closedPos;

        // Smooth geçiş
        door.localPosition = Vector3.Lerp(
            door.localPosition,
            targetPos,
            Time.deltaTime * speed
        );
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;

            // Açılma sesi çal
            if (audioSource != null && openClip != null)
            {
                audioSource.pitch = Random.Range(0.95f, 1.05f); // küçük varyasyon
                audioSource.PlayOneShot(openClip);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;

            // Kapanma sesi çal
            if (audioSource != null && closeClip != null)
            {
                audioSource.pitch = Random.Range(0.95f, 1.05f); // küçük varyasyon
                audioSource.PlayOneShot(closeClip);
            }
        }
    }
}
