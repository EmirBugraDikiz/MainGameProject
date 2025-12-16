using UnityEngine;

public enum PotionType
{
    DoubleJump,
    Kronos
}

public class PotionPickup : MonoBehaviour
{
    // ==== Global State (Narrator / kapı / kontrol mekanikleri için) ====
    public static bool DoubleJumpCollected = false;
    public static bool KronosCollected = false;

    [Header("Settings")]
    public PotionType potionType;
    public float destroyDelay = 0.1f;

    [Header("Audio (Plays fully even if potion is destroyed)")]
    public AudioClip pickupClip;
    [Range(0f, 1f)] public float pickupVolume = 0.8f;
    public Vector2 pitchRange = new Vector2(0.95f, 1.05f);

    [Header("Narrator (Optional)")]
    [Tooltip("Boş bırakırsan sahneden otomatik bulmayı dener.")]
    public NarratorAudioManager narrator;

    [Tooltip("Potion alınca çalınacak narrator repliği (L2_05_PotionPickUp_CLEAN)")]
    public AudioClip narratorLineOnPickup;

    [Tooltip("Potion SFX’ten sonra narrator kaç saniye sonra girsin?")]
    public float narratorDelay = 0.15f;

    private bool isCollected = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;
        if (!other.CompareTag("Player")) return;

        PlayerAbilitiesController abilities = other.GetComponent<PlayerAbilitiesController>();
        if (abilities == null) return;

        isCollected = true;

        // Yeteneği ver + state’i işaretle
        switch (potionType)
        {
            case PotionType.DoubleJump:
                abilities.GrantDoubleJump();
                DoubleJumpCollected = true;
                break;

            case PotionType.Kronos:
                abilities.GrantKronos();
                KronosCollected = true;
                break;
        }

        // 1) Potion pickup SFX (potion yok olsa bile tam çalar)
        PlayPickupSoundFull();

        // 2) Narrator repliği
        PlayNarratorLine();

        // Görsel + collider kapat (spam olmasın, estetik)
        DisableVisuals();

        Destroy(gameObject, destroyDelay);
    }

    private void PlayPickupSoundFull()
    {
        if (pickupClip == null) return;

        GameObject temp = new GameObject("PotionPickupSFX");
        temp.transform.position = transform.position;

        AudioSource a = temp.AddComponent<AudioSource>();
        a.clip = pickupClip;
        a.volume = pickupVolume;
        a.pitch = Random.Range(pitchRange.x, pitchRange.y);
        a.spatialBlend = 1f;   // 3D
        a.dopplerLevel = 0f;
        a.playOnAwake = false;

        a.Play();
        Destroy(temp, pickupClip.length / Mathf.Max(0.01f, a.pitch));
    }

    private void PlayNarratorLine()
    {
        if (narratorLineOnPickup == null) return;

        if (narrator == null)
            narrator = FindFirstObjectByType<NarratorAudioManager>();

        if (narrator == null) return;

        if (narratorDelay <= 0f)
        {
            narrator.Enqueue(narratorLineOnPickup, true);
        }
        else
        {
            Invoke(nameof(PlayNarratorDelayed), narratorDelay);
        }
    }

    private void PlayNarratorDelayed()
    {
        if (narrator != null && narratorLineOnPickup != null)
            narrator.Enqueue(narratorLineOnPickup, true);
    }

    private void DisableVisuals()
    {
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = false;

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;
    }
}
