using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RampFakeMove : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Hareket edecek rampa objesi (boş bırakılırsa bu objenin kendisi kullanılır).")]
    public Transform ramp;

    [Header("Audio")]
    [Tooltip("Sesin çalacağı AudioSource. Boşsa bu objeden otomatik bulur/ekler.")]
    public AudioSource sfxSource;

    [Tooltip("Rampa kayarken çalacak ses (Woosh/Slide).")]
    public AudioClip slideClip;

    [Range(0f, 1f)]
    public float baseVolume = 0.7f;

    [Tooltip("Başlangıç pitch random aralığı.")]
    public Vector2 pitchRange = new Vector2(0.92f, 1.08f);

    [Tooltip("Rampa daha hızlı hareket ediyorsa pitch'e eklenecek miktar (hız etkisi).")]
    public float pitchBoostBySpeed = 0.015f;

    [Tooltip("Hareket bitince ses kaç saniyede fade-out yapsın?")]
    public float fadeOutTime = 0.2f;

    [Header("Movement Settings")]
    [Tooltip("Rampanın dünya uzayındaki hedef Z pozisyonu.")]
    public float targetWorldZ = -1.6f;

    [Tooltip("Saniyedeki hareket hızı.")]
    public float moveSpeed = 10f;

    [Tooltip("Oyuncu tetikleyince sadece bir kez mi hareket etsin?")]
    public bool moveOnlyOnce = true;

    [Header("Debug/Helpers")]
    public bool drawGizmos = true;

    private Vector3 targetPos;
    private bool shouldMove = false;
    private bool alreadyMoved = false;

    private Coroutine fadeRoutine;

    void Reset()
    {
        // Collider tetikleyici olsun diye küçük safety
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    void Start()
    {
        if (ramp == null)
            ramp = transform;

        // X ve Y aynı kalsın, sadece dünyanın Z'si değişsin
        Vector3 startPos = ramp.position;
        targetPos = new Vector3(startPos.x, startPos.y, targetWorldZ);

        // AudioSource yoksa bul / ekle
        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();

        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();

        // AudioSource ayarları (sen Inspector’dan değiştirsen de olur)
        sfxSource.playOnAwake = false;
        sfxSource.loop = true;
        sfxSource.spatialBlend = 1f; // 3D
        sfxSource.dopplerLevel = 0f;
        sfxSource.volume = 0f; // hareket başlayınca açacağız

        if (slideClip != null)
            sfxSource.clip = slideClip;
    }

    void Update()
    {
        if (!shouldMove) return;

        // Hareket
        ramp.position = Vector3.MoveTowards(
            ramp.position,
            targetPos,
            moveSpeed * Time.deltaTime
        );

        // Hareket esnasında pitch'i hafif hızdan etkileyelim
        if (sfxSource != null && sfxSource.isPlaying)
        {
            float boosted = sfxSource.pitch + (moveSpeed * pitchBoostBySpeed * Time.deltaTime);
            sfxSource.pitch = Mathf.Clamp(boosted, 0.5f, 2f);
        }

        // Hedefe ulaştıysa dur
        if ((ramp.position - targetPos).sqrMagnitude < 0.0001f)
        {
            ramp.position = targetPos;
            shouldMove = false;
            StopSlideSfx();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (moveOnlyOnce && alreadyMoved)
            return;

        alreadyMoved = true;
        shouldMove = true;

        StartSlideSfx();
    }

    private void StartSlideSfx()
    {
        if (sfxSource == null || slideClip == null) return;

        // Önceki fade varsa iptal
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        // Clip set + random pitch
        if (sfxSource.clip != slideClip) sfxSource.clip = slideClip;
        sfxSource.pitch = Random.Range(pitchRange.x, pitchRange.y);

        // Ses çalmıyorsa başlat
        if (!sfxSource.isPlaying)
            sfxSource.Play();

        // Direkt volume’u base’e çek
        sfxSource.volume = baseVolume;
    }

    private void StopSlideSfx()
    {
        if (sfxSource == null) return;
        if (!sfxSource.isPlaying) return;

        // Fade-out ile kapat
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeOutAndStop());
    }

    private IEnumerator FadeOutAndStop()
    {
        float startVol = sfxSource.volume;
        float t = 0f;

        float dur = Mathf.Max(0.01f, fadeOutTime);

        while (t < dur)
        {
            t += Time.deltaTime;
            float k = 1f - (t / dur);
            sfxSource.volume = startVol * k;
            yield return null;
        }

        sfxSource.volume = 0f;
        sfxSource.Stop();
        fadeRoutine = null;
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        if (ramp == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(ramp.position, new Vector3(ramp.position.x, ramp.position.y, targetWorldZ));
        Gizmos.DrawWireSphere(new Vector3(ramp.position.x, ramp.position.y, targetWorldZ), 0.15f);
    }
}
