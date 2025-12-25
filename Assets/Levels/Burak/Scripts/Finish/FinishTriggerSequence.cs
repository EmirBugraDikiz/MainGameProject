using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class FinishTriggerSequence : MonoBehaviour
{
    [Header("UI Fade")]
    [Tooltip("Canvas üstündeki full-screen beyaz Image (alpha 0 başlayacak)")]
    public Image whiteFadeImage;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip flashBackClip;

    [Header("Scene")]
    public string finishSceneName = "FinishScene";

    [Header("Options")]
    [Tooltip("Sadece Player tag'li obje tetiklesin")]
    public bool requirePlayerTag = true;

    private bool triggered = false;

    private void Reset()
    {
        // Inspector’da Is Trigger zaten açık olsun ama garanti:
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D
    }

    private void Start()
    {
        // Başta beyaz overlay şeffaf olsun
        if (whiteFadeImage != null)
        {
            var c = whiteFadeImage.color;
            c.a = 0f;
            whiteFadeImage.color = c;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        if (requirePlayerTag && !other.CompareTag("Player"))
            return;

        triggered = true;
        StartCoroutine(Sequence());
    }

    private IEnumerator Sequence()
    {
        // Clip’i ayarla ve çal
        if (flashBackClip != null)
            audioSource.clip = flashBackClip;

        float duration = (audioSource.clip != null) ? audioSource.clip.length : 4.7f;

        audioSource.Play();

        // Ses süresi boyunca alpha 0 -> 1
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / duration);

            if (whiteFadeImage != null)
            {
                var c = whiteFadeImage.color;
                c.a = a;
                whiteFadeImage.color = c;
            }

            yield return null;
        }

        // Full beyaz olduktan sonra scene değiştir
        SceneManager.LoadScene(finishSceneName);
    }
}
