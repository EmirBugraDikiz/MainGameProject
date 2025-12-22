using System.Collections;
using UnityEngine;

public class NarratorEntrySequence : MonoBehaviour
{
    [Header("Doors")]
    public Transform doorLeft;
    public Transform doorRight;

    [Tooltip("Door_Left: z 2 -> 0")]
    public float leftCloseZ = 0f;

    [Tooltip("Door_Right: z -3.3 -> -1.433132")]
    public float rightCloseZ = -1.433132f;

    [Header("Door Slam")]
    public float doorSlamDuration = 0.35f;

    // Zıbamm hissi: ilk %15’te %85 kapanma
    public AnimationCurve slamCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.15f, 0.85f),
        new Keyframe(1f, 1f)
    );

    [Header("Door Audio (Optional)")]
    public AudioSource sfxSource;
    public AudioClip doorSlamClip;
    [Range(0f, 1f)] public float doorSlamVolume = 0.9f;
    public Vector2 slamPitchRange = new Vector2(0.95f, 1.05f);

    [Header("Narrator Platform Lift (TheNARRATOR root)")]
    public Transform narratorRoot;   // TheNARRATOR objesi (root)
    public float startY = -30f;
    public float endY = 0f;
    public float liftDuration = 8f;
    public AnimationCurve liftCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Lift Audio (Optional)")]
    public AudioSource liftLoopSource;
    public AudioClip liftLoopClip;
    [Range(0f, 1f)] public float liftLoopVolume = 0.6f;

    [Header("Music Switch (Optional)")]
    [Tooltip("BackgroundMusic objesindeki MusicSwitcher componenti buraya.")]
    public MusicSwitcher music;
    [Tooltip("Trigger sonrası geçilecek gerici müzik / ambience (Low Hum 2).")]
    public AudioClip lowHum2Clip;
    [Tooltip("-1 bırakırsan mevcut müzik volume'u korunur.")]
    public float lowHum2TargetVolume = -1f;
    public bool switchMusicOnTrigger = true;

    [Header("Timing")]
    public float liftStartDelay = 0.12f;

    [Header("Narrator Lines (Start After Lift Begins)")]
    public NarratorSequenceController narratorSequence;
    public float narratorStartAfterLiftBegins = 0.6f;

    private bool triggered;

    void Awake()
    {
        // TheNARRATOR başlangıçta belirlediğin Y'e çekilsin (sadece Y)
        if (narratorRoot != null)
        {
            Vector3 p = narratorRoot.position;
            narratorRoot.position = new Vector3(p.x, startY, p.z);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;

        // Trigger bir daha çalışmasın diye colliderı kapat
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Müzik switch (fade)
        if (switchMusicOnTrigger && music != null && lowHum2Clip != null)
        {
            music.SwitchTo(lowHum2Clip, lowHum2TargetVolume);
        }

        StartCoroutine(SequenceRoutine());
    }

    private IEnumerator SequenceRoutine()
    {
        // 1) Kapıları slam kapat
        yield return StartCoroutine(SlamCloseDoors());

        // 2) minicik nefes
        if (liftStartDelay > 0f)
            yield return new WaitForSeconds(liftStartDelay);

        // 3) Lift başlarken (gecikmeli) narrator replik akışını başlat
        if (narratorSequence != null)
            StartCoroutine(StartNarratorAfterDelay(narratorStartAfterLiftBegins));

        // 4) TheNARRATOR'ı yükselt
        yield return StartCoroutine(LiftNarratorRoot());
    }

    private IEnumerator StartNarratorAfterDelay(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        narratorSequence?.StartSequence();
    }

    private IEnumerator SlamCloseDoors()
    {
        if (doorLeft == null || doorRight == null)
        {
            Debug.LogWarning("NarratorEntrySequence: Door refs missing.");
            yield break;
        }

        // Slam SFX
        if (sfxSource != null && doorSlamClip != null)
        {
            float oldPitch = sfxSource.pitch;
            sfxSource.pitch = Random.Range(slamPitchRange.x, slamPitchRange.y);
            sfxSource.PlayOneShot(doorSlamClip, doorSlamVolume);
            sfxSource.pitch = oldPitch;
        }

        Vector3 leftStart = doorLeft.localPosition;
        Vector3 rightStart = doorRight.localPosition;

        Vector3 leftEnd = new Vector3(leftStart.x, leftStart.y, leftCloseZ);
        Vector3 rightEnd = new Vector3(rightStart.x, rightStart.y, rightCloseZ);

        float t = 0f;
        while (t < doorSlamDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / Mathf.Max(0.0001f, doorSlamDuration));
            float eased = slamCurve != null ? slamCurve.Evaluate(k) : k;

            doorLeft.localPosition = Vector3.LerpUnclamped(leftStart, leftEnd, eased);
            doorRight.localPosition = Vector3.LerpUnclamped(rightStart, rightEnd, eased);

            yield return null;
        }

        doorLeft.localPosition = leftEnd;
        doorRight.localPosition = rightEnd;
    }

    private IEnumerator LiftNarratorRoot()
    {
        if (narratorRoot == null)
        {
            Debug.LogWarning("NarratorEntrySequence: narratorRoot missing.");
            yield break;
        }

        // Lift loop SFX
        if (liftLoopSource != null && liftLoopClip != null)
        {
            liftLoopSource.clip = liftLoopClip;
            liftLoopSource.loop = true;
            liftLoopSource.volume = liftLoopVolume;
            if (!liftLoopSource.isPlaying) liftLoopSource.Play();
        }

        Vector3 startPos = narratorRoot.position;
        Vector3 endPos = new Vector3(startPos.x, endY, startPos.z);

        float t = 0f;
        while (t < liftDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / Mathf.Max(0.0001f, liftDuration));
            float eased = liftCurve != null ? liftCurve.Evaluate(k) : k;

            narratorRoot.position = Vector3.Lerp(startPos, endPos, eased);
            yield return null;
        }

        narratorRoot.position = endPos;

        if (liftLoopSource != null && liftLoopSource.isPlaying)
        {
            liftLoopSource.Stop();
        }
    }
}
