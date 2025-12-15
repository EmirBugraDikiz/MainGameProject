using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class PlayerDeathEffect : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Ölüm sırasında ağırlığını açıp kapatacağımız Volume")]
    public Volume postProcessVolume;

    [Tooltip("Kameranın bağlı olduğu root (PlayerCameraRoot)")]
    public Transform cameraRoot;

    [Tooltip("Sesleri çalmak için AudioSource (Player üstünde olsun)")]
    public AudioSource audioSource;

    private PlayerAbilitiesController abilities;
    private PlayerInput playerInput;

    [Header("Audio Clips")]
    public AudioClip deathClip;      // Death_Sound.flac
    public AudioClip respawnClip;    // Return_By_Death.mp3

    [Header("Timings")]
    [Tooltip("Tamamen kararması ne kadar sürsün? (saniye)")]
    public float deathFadeDuration = 0.4f;      // 0.3–0.5 arası ideal

    [Tooltip("Respawn sonrası karanlıktan normale dönme süresi")]
    public float respawnFadeDuration = 1.0f;

    [Tooltip("Fade eğrisi (başta hızlı, sonra yavaş vs.)")]
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private float defaultWeight = 0f;
    private bool  isPlaying    = false;
    private Quaternion defaultCamLocalRot;

    void Awake()
    {
        abilities   = GetComponent<PlayerAbilitiesController>();
        playerInput = GetComponent<PlayerInput>();

        if (postProcessVolume != null)
            defaultWeight = postProcessVolume.weight;

        if (cameraRoot != null)
            defaultCamLocalRot = cameraRoot.localRotation;
    }

    /// <summary>
    /// PlayerRespawn burayı çağıracak.
    /// </summary>
    public void StartDeathSequence(PlayerRespawn respawn)
    {
        if (!isPlaying && respawn != null)
        {
            StartCoroutine(DeathRoutine(respawn));
        }
    }

    private IEnumerator DeathRoutine(PlayerRespawn respawn)
    {
        isPlaying = true;

        // Zamanı garanti normale çek
        Time.timeScale = 1f;

        // Input kilidi
        if (abilities != null) abilities.controlsEnabled = false;
        if (playerInput != null) playerInput.enabled = false;

        // Volume başlangıçta tamamen kapalı
        if (postProcessVolume != null)
            postProcessVolume.weight = 0f;

        // Ölüm sesi
        if (audioSource != null && deathClip != null)
            audioSource.PlayOneShot(deathClip);

        // Kamera düşme animasyonu
        Quaternion startRot = cameraRoot != null ? cameraRoot.localRotation : Quaternion.identity;
        Quaternion fallRot  = startRot * Quaternion.Euler(25f, 0f, Random.Range(-10f, 10f));

        // 1) KAPANMA (0 → tam siyah)
        float t = 0f;
        while (t < deathFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float kNorm = Mathf.Clamp01(t / deathFadeDuration);
            float k     = fadeCurve != null ? fadeCurve.Evaluate(kNorm) : kNorm;

            if (postProcessVolume != null)
                postProcessVolume.weight = Mathf.Lerp(0f, 1f, k);

            if (cameraRoot != null)
                cameraRoot.localRotation = Quaternion.Slerp(startRot, fallRot, k);

            yield return null;
        }

        // Göz TAM kapalı olsun
        if (postProcessVolume != null)
            postProcessVolume.weight = 1f;

        if (cameraRoot != null)
            cameraRoot.localRotation = fallRot;

        // Ekran tamamen kapalı, oyuncu kıpırdayamıyor.
        // Şimdi respawn et
        respawn.Respawn();

        // Kamera rotasyonunu resetle (hala karanlıkta)
        if (cameraRoot != null)
            cameraRoot.localRotation = defaultCamLocalRot;

        // Respawn sesi
        if (audioSource != null && respawnClip != null)
            audioSource.PlayOneShot(respawnClip);

        // 2) AÇILMA (tam siyah → normal)
        t = 0f;
        while (t < respawnFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float kNorm = Mathf.Clamp01(t / respawnFadeDuration);
            float k     = fadeCurve != null ? fadeCurve.Evaluate(kNorm) : kNorm;

            if (postProcessVolume != null)
                postProcessVolume.weight = Mathf.Lerp(1f, 0f, k);

            yield return null;
        }

        if (postProcessVolume != null)
            postProcessVolume.weight = defaultWeight;

        // Inputu geri aç
        if (abilities != null) abilities.controlsEnabled = true;
        if (playerInput != null) playerInput.enabled = true;

        isPlaying = false;
    }
    public IEnumerator FadeToBlackOnly(float duration)
    {
        // Başlangıçta volume 0 olsun
        if (postProcessVolume != null)
            postProcessVolume.weight = 0f;

        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float kNorm = Mathf.Clamp01(t / duration);
            float k     = fadeCurve != null ? fadeCurve.Evaluate(kNorm) : kNorm;

            if (postProcessVolume != null)
                postProcessVolume.weight = Mathf.Lerp(0f, 1f, k);

            yield return null;
        }

        if (postProcessVolume != null)
            postProcessVolume.weight = 1f;
    }
}
