using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Portal : MonoBehaviour
{
    [Header("Nereye ışınlanacak?")]
    public Transform destination;

    [Header("Rotasyonu hizala")]
    public bool alignRotation = true;

    [Header("Audio")]
    [Tooltip("Giriş portalında çalacak SFX kaynağı. Boşsa bu objeden AudioSource arar.")]
    public AudioSource sfxSource;
    [Tooltip("Çıkış portalında çalacak SFX kaynağı. Boşsa destination objesinden AudioSource arar.")]
    public AudioSource destinationSfxSource;

    public AudioClip teleportClip;
    [Range(0f, 1f)] public float teleportVolume = 0.9f;

    [Header("Anti-Spam")]
    public float cooldown = 0.35f;
    private float lastTeleportTime = -999f;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void Awake()
    {
        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();

        // Destination audio source otomatik bul
        if (destinationSfxSource == null && destination != null)
            destinationSfxSource = destination.GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (destination == null) return;

        if (Time.time < lastTeleportTime + cooldown) return;
        lastTeleportTime = Time.time;

        // 1) Giriş portalında teleport sesi
        if (sfxSource != null && teleportClip != null)
            sfxSource.PlayOneShot(teleportClip, teleportVolume);

        // CharacterController geçici kapat
        CharacterController cc = other.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // Pozisyonu taşı
        other.transform.position = destination.position;

        // Rotasyonu hizala
        if (alignRotation)
        {
            Vector3 euler = destination.rotation.eulerAngles;
            other.transform.rotation = Quaternion.Euler(0, euler.y, 0);
        }

        if (cc != null) cc.enabled = true;

        // 2) Çıkış portalında da teleport sesi (oyuncu artık orada)
        // DestinationSfxSource yoksa burada tekrar dene:
        if (destinationSfxSource == null)
            destinationSfxSource = destination.GetComponent<AudioSource>();

        if (destinationSfxSource != null && teleportClip != null)
            destinationSfxSource.PlayOneShot(teleportClip, teleportVolume);
    }
}
