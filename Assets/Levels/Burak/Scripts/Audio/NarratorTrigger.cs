using UnityEngine;

public class NarratorTrigger : MonoBehaviour
{
    [Header("References")]
    public NarratorAudioManager narrator;

    [Header("Line")]
    public AudioClip line;
    public bool playOnlyOnce = true;
    public float delay = 0f;

    private bool fired = false;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (playOnlyOnce && fired) return;

        fired = true;

        if (narrator == null)
            narrator = FindFirstObjectByType<NarratorAudioManager>();

        if (narrator == null || line == null) return;

        if (delay <= 0f)
            narrator.Enqueue(line, playOnlyOnce);
        else
            Invoke(nameof(PlayDelayed), delay);
    }

    private void PlayDelayed()
    {
        if (narrator != null && line != null)
            narrator.Enqueue(line, playOnlyOnce);
    }
}
