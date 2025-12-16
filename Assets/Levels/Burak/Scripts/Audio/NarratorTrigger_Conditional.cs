using System.Collections;
using UnityEngine;

public class NarratorTrigger_Conditional : MonoBehaviour
{
    [Header("References")]
    public NarratorAudioManager narrator;

    [Header("Line")]
    public AudioClip line;
    public float delay = 0f;

    [Header("Conditions")]
    public bool requireFakeFinishTried = true;
    public bool playOnlyOnce = true;

    private bool localPlayed = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Şart: fake finish denendiyse
        if (requireFakeFinishTried && !Level2NarratorFlags.FakeFinishTried)
            return;

        // Global olarak da tek sefer çalsın (respawn sonrası tekrar tetiklenmesin)
        if (playOnlyOnce && Level2NarratorFlags.RealParkourIntroPlayed)
            return;

        // Extra güvenlik
        if (playOnlyOnce && localPlayed) return;

        localPlayed = true;
        Level2NarratorFlags.RealParkourIntroPlayed = true;

        if (narrator == null)
            narrator = FindFirstObjectByType<NarratorAudioManager>();

        if (narrator == null || line == null) return;

        if (delay <= 0f) narrator.Enqueue(line, true);
        else StartCoroutine(PlayDelayed());
    }

    private IEnumerator PlayDelayed()
    {
        yield return new WaitForSeconds(delay);

        if (narrator == null)
            narrator = FindFirstObjectByType<NarratorAudioManager>();

        if (narrator != null && line != null)
            narrator.Enqueue(line, true);
    }
}
