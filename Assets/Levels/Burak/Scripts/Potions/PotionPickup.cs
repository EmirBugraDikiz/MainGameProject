using UnityEngine;

public enum PotionType
{
    DoubleJump,
    Kronos
}

public class PotionPickup : MonoBehaviour
{
    [Header("Settings")]
    public PotionType potionType;
    public float destroyDelay = 0.1f;

    [Header("Audio (Plays fully even if potion is destroyed)")]
    public AudioClip pickupClip;
    [Range(0f, 1f)] public float pickupVolume = 0.8f;
    public Vector2 pitchRange = new Vector2(0.95f, 1.05f);

    private bool isCollected = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        if (!other.CompareTag("Player")) return;

        PlayerAbilitiesController abilities = other.GetComponent<PlayerAbilitiesController>();
        if (abilities == null) return;

        isCollected = true;

        // Yeteneği ver
        switch (potionType)
        {
            case PotionType.DoubleJump:
                abilities.GrantDoubleJump();
                break;

            case PotionType.Kronos:
                abilities.GrantKronos();
                break;
        }

        // Sesi potion yok olsa bile tam çal
        PlayPickupSoundFull();

        // Görsel + collider kapat (spam olmasın, estetik)
        DisableVisuals();

        Destroy(gameObject, destroyDelay);
    }

    private void PlayPickupSoundFull()
    {
        if (pickupClip == null) return;

        // PlayClipAtPoint yeni bir GameObject + AudioSource oluşturur, clip bitince kendi yok olur
        // Pitch random için küçük trick: geçici objeyi kendimiz yaratıp pitch set etmek
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

    private void DisableVisuals()
    {
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = false;

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;
    }
}
