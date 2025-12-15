using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelEndTrigger : MonoBehaviour
{
    [Header("Gas Settings")]
    [Tooltip("Uyku gazı particle objesi (başlangıçta kapalı olsun).")]
    public GameObject gasEffect;

    [Header("Fade & End")]
    [Tooltip("Player üzerindeki PlayerDeathEffect referansı.")]
    public PlayerDeathEffect deathEffect;

    [Tooltip("Ekranın tamamen kararmasının süresi.")]
    public float fadeOutDuration = 1.5f;

    [Header("Sleep SFX")]
    [Tooltip("Yawn sesi (Yawn_wav).")]
    public AudioSource sleepAudioSource;

    [Tooltip("Trigger'a girdikten kaç saniye sonra Yawn + Fade başlasın?")]
    public float yawnAndFadeDelay = 1.0f;

    [Header("Next Level")]
    [Tooltip("Geçilecek sahnenin adı (Build Settings'te ekli olmalı).")]
    public string nextSceneName = "Room3";

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;

        // 1) GAZ ANINDA AÇILSIN
        if (gasEffect != null)
            gasEffect.SetActive(true);

        // 2) Geri kalan sekans coroutine'de
        StartCoroutine(LevelEndSequence());
    }

    private System.Collections.IEnumerator LevelEndSequence()
    {
        // --- GAZ ÇIKIYOR, BİRAZ BEKLE ---
        if (yawnAndFadeDelay > 0f)
            yield return new WaitForSeconds(yawnAndFadeDelay);

        // --- YAWN + FADE AYNI ANDA BAŞLASIN ---

        // Yawn sesi
        if (sleepAudioSource != null)
            sleepAudioSource.Play();

        // Fade to black
        if (deathEffect != null)
        {
            // PlayerDeathEffect içindeki FadeToBlackOnly kullanılıyor
            yield return deathEffect.FadeToBlackOnly(fadeOutDuration);
        }

        // --- LEVEL GEÇİŞİ ---
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }
}
