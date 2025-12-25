using System.Collections;
using UnityEngine;

public class ExitButtonInteract : MonoBehaviour
{
    [Header("Interact")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private Collider interactTrigger;
    private bool playerInside;

    [Header("State")]
    [SerializeField] private bool unlocked = false;
    private bool used = false;
    public bool IsUnlocked => unlocked;

    [Header("Button Visual (press part)")]
    [SerializeField] private Transform pressPart;
    [SerializeField] private float pressDownY = -10f;
    [SerializeField] private float pressDuration = 0.18f;
    [SerializeField] private AnimationCurve pressEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip confirmClip;
    [SerializeField] private AudioClip accessDeniedClip;
    [SerializeField, Range(0f, 1f)] private float confirmVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float denyVolume = 1f;

    [Header("Narrator VO")]
    [Tooltip("Erken basınca çalan VO (L4_22)")]
    [SerializeField] private AudioClip earlyPressNarratorClip;

    [Tooltip("Buton aktifken basınca çalan FINAL VO (L4_24)")]
    [SerializeField] private AudioClip finishNarratorClip;

    [SerializeField] private bool earlyPressNarratorOnce = true;
    private bool earlyPressNarratorPlayed = false;

    [Tooltip("NarratorSequenceController (cutscene ile aynı olan)")]
    [SerializeField] private NarratorSequenceController narratorSequence;

    [Header("Door")]
    [SerializeField] private SlidingDoorPair door;

    private void Awake()
    {
        if (interactTrigger != null) interactTrigger.isTrigger = true;

        if (narratorSequence == null)
            narratorSequence = FindFirstObjectByType<NarratorSequenceController>();

        if (sfxSource != null) sfxSource.playOnAwake = false;
    }

    private void Update()
    {
        if (!playerInside) return;
        if (Input.GetKeyDown(interactKey))
            TryInteract();
    }

    public void Unlock()
    {
        unlocked = true;
        Debug.Log("[ExitButtonInteract] EXIT BUTTON UNLOCKED!");
    }

    private void TryInteract()
    {
        if (used) return;

        if (!unlocked)
        {
            PlayOneShot(sfxSource, accessDeniedClip, denyVolume);
            TryQueueEarlyNarratorVO();
            return;
        }

        used = true;
        StartCoroutine(PressFinishSequence());
    }

    private void TryQueueEarlyNarratorVO()
    {
        if (earlyPressNarratorClip == null) return;
        if (earlyPressNarratorOnce && earlyPressNarratorPlayed) return;
        earlyPressNarratorPlayed = true;

        if (narratorSequence != null && narratorSequence.IsRunning)
        {
            narratorSequence.EnqueueInjectedAsTalk(earlyPressNarratorClip);
            return;
        }

        if (NarratorAudioQueue.Instance != null)
            NarratorAudioQueue.Instance.Enqueue(earlyPressNarratorClip);
    }

    private IEnumerator PressFinishSequence()
    {
        // 1) Buton animasyonu
        if (pressPart != null)
        {
            Vector3 start = pressPart.localPosition;
            Vector3 down = new Vector3(start.x, pressDownY, start.z);

            yield return MoveLocal(pressPart, start, down, pressDuration, pressEase);
            yield return MoveLocal(pressPart, down, start, pressDuration, pressEase);
        }

        // 2) Confirm SFX
        PlayOneShot(sfxSource, confirmClip, confirmVolume);

        // 3) FINAL narrator konuşması (L4_24) -> narrator voiceSource'tan ÇAL ve BEKLE
        if (finishNarratorClip != null)
        {
            if (narratorSequence != null)
            {
                // Sequence çalışsa da çalışmasa da doğru davranır:
                yield return narratorSequence.PlayStandalone(finishNarratorClip, asTalk: true);
            }
            else if (NarratorAudioQueue.Instance != null)
            {
                NarratorAudioQueue.Instance.Enqueue(finishNarratorClip);
                yield return new WaitForSeconds(finishNarratorClip.length);
            }
        }

        // 4) Kapıyı aç
        if (door != null)
            door.Open();
    }

    private IEnumerator MoveLocal(Transform tr, Vector3 from, Vector3 to, float duration, AnimationCurve curve)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / duration);
            float e = curve.Evaluate(n);
            tr.localPosition = Vector3.Lerp(from, to, e);
            yield return null;
        }
        tr.localPosition = to;
    }

    private void PlayOneShot(AudioSource src, AudioClip clip, float vol = 1f)
    {
        if (src == null || clip == null) return;
        src.PlayOneShot(clip, vol);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = false;
    }
}
