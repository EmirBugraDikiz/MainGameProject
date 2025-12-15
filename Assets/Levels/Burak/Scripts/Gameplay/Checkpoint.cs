using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Bu checkpoint sadece bir kez mi aktif olsun? (Sadece ilk sefer sayılır.)")]
    public bool oneTimeUse = false;

    [Header("Feedback (Optional)")]
    public GameObject activateVfx;
    public AudioSource activateAudio;  // Checkpoint.wav için

    [Header("Checkpoint Light (Optional)")]
    [Tooltip("Checkpoint alındığında kapanmasını istediğin ışık veya görsel objesi.")]
    public GameObject checkpointLight;

    private bool isActivated    = false; // oneTimeUse için
    private bool feedbackPlayed = false; // ses / vfx / ışık sadece 1 kez

    private void Reset()
    {
        // Yeni eklediğinde collider'ı otomatik trigger yapmaya çalışsın
        var col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Sadece Player tag'li objeyi kabul et
        if (!other.CompareTag("Player"))
            return;

        var respawn = other.GetComponent<PlayerRespawn>();
        if (respawn == null)
            respawn = other.GetComponentInParent<PlayerRespawn>();

        if (respawn == null)
            return;

        // Tamamen tek kullanımlık checkpoint ise
        if (oneTimeUse && isActivated)
            return;

        // Her girişte respawn noktasını güncellemek sorun değil
        respawn.SetCheckpoint(transform);
        Debug.Log($"[Checkpoint] Yeni respawn noktası: {name}");

        // Feedback (ses, vfx, ışık) sadece İLK sefer çalışsın
        if (!feedbackPlayed)
        {
            feedbackPlayed = true;

            if (activateVfx != null)
                activateVfx.SetActive(true);

            if (activateAudio != null)
                activateAudio.Play();

            if (checkpointLight != null)
                checkpointLight.SetActive(false);
        }

        // oneTimeUse açıksa, bundan sonra bu checkpoint tamamen devre dışı
        if (oneTimeUse)
            isActivated = true;
    }
}
