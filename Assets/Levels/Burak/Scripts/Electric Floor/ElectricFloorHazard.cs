using UnityEngine;

public class ElectricFloorHazard : MonoBehaviour
{
    [Header("Hit VFX (one-shot)")]
    public ParticleSystem hitVfxPrefab; // vfx_Lightning_02 prefab

    [Header("Hit SFX")]
    public AudioClip hitZapSfx;         // Zap
    [Range(0f, 1f)] public float hitZapVolume = 0.9f;

    [Header("Settings")]
    public float triggerCooldown = 0.75f;

    private bool onCooldown = false;

    private void OnTriggerEnter(Collider other) => TryKill(other);
    private void OnTriggerStay(Collider other)  => TryKill(other);

    private void TryKill(Collider other)
    {
        if (onCooldown) return;
        if (!other.CompareTag("Player")) return;

        var respawn = other.GetComponent<PlayerRespawn>();
        if (respawn == null) return;

        // VFX spawn
        if (hitVfxPrefab != null)
        {
            var vfx = Instantiate(hitVfxPrefab, other.transform.position, Quaternion.identity);
            vfx.Play();

            float life = vfx.main.duration + vfx.main.startLifetime.constantMax;
            Destroy(vfx.gameObject, Mathf.Max(0.2f, life));
        }

        // SFX
        if (hitZapSfx != null)
            AudioSource.PlayClipAtPoint(hitZapSfx, other.transform.position, hitZapVolume);

        onCooldown = true;

        // Death + fade
        respawn.Kill();

        Invoke(nameof(ResetCooldown), triggerCooldown);
    }

    private void ResetCooldown() => onCooldown = false;
}
