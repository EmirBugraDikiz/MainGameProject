using UnityEngine;

[RequireComponent(typeof(Collider))]
public class OneShotTriggerAudio : MonoBehaviour
{
    [Header("Settings")]
    public string playerTag = "Player";
    public bool playOnce = true;

    [Header("Clip")]
    public AudioClip clip;

    [Header("Delay")]
    public float playDelay = 0f;

    private bool played;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (playOnce && played) return;
        if (!other.CompareTag(playerTag)) return;

        if (!clip)
        {
            Debug.LogWarning($"{name}: Clip eksik.");
            return;
        }

        if (NarratorAudioQueue.Instance == null)
        {
            Debug.LogWarning($"{name}: NarratorAudioQueue sahnede yok! (Instance null)");
            return;
        }

        played = true;
        NarratorAudioQueue.Instance.Enqueue(clip, playDelay);
    }
}
