using System.Collections;
using UnityEngine;

public class ExitButtonInteract : MonoBehaviour
{
    [Header("Interact")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private Collider interactTrigger; // trigger collider
    private bool playerInside;

    [Header("State")]
    [SerializeField] private bool unlocked = false;
    private bool used = false;

    [Header("Button Visual (press part)")]
    [SerializeField] private Transform pressPart; // pSphere1
    [SerializeField] private float pressDownY = -10f; // local y
    [SerializeField] private float pressDuration = 0.18f;
    [SerializeField] private AnimationCurve pressEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip confirmClip;      // buton onay sesi
    [SerializeField] private AudioClip accessDeniedClip; // Access_Denied
    [SerializeField, Range(0f, 1f)] private float confirmVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float denyVolume = 1f;

    [Header("Door")]
    [SerializeField] private SlidingDoorPair door;

    private void Awake()
    {
        if (interactTrigger != null) interactTrigger.isTrigger = true;
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
    }

    private void TryInteract()
    {
        if (used) return;

        if (!unlocked)
        {
            PlayOneShot(sfxSource, accessDeniedClip, denyVolume);
            return;
        }

        used = true;
        StartCoroutine(PressAndActivate());
    }

    private IEnumerator PressAndActivate()
    {
        if (pressPart != null)
        {
            Vector3 start = pressPart.localPosition;
            Vector3 down = new Vector3(start.x, pressDownY, start.z);

            yield return MoveLocal(pressPart, start, down, pressDuration, pressEase);
            yield return MoveLocal(pressPart, down, start, pressDuration, pressEase);
        }

        PlayOneShot(sfxSource, confirmClip, confirmVolume);

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
