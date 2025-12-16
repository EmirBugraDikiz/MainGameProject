using System.Collections;
using UnityEngine;

public class NarratorWaitTrigger : MonoBehaviour
{
    public NarratorAudioManager narrator;
    public AudioClip line;

    [Tooltip("Oyuncu bu alana girince şu kadar saniye bekle, sonra şart sağlanıyorsa konuş.")]
    public float waitSeconds = 4f;

    [Tooltip("Sadece 1 kez çalışsın.")]
    public bool playOnlyOnce = true;

    private bool fired = false;
    private Coroutine routine;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (playOnlyOnce && fired) return;

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(WaitAndPlay());
    }

    private IEnumerator WaitAndPlay()
    {
        yield return new WaitForSeconds(waitSeconds);

        // Double jump alınmadıysa trip at
        if (!PotionPickup.DoubleJumpCollected)
        {
            if (narrator == null) narrator = FindFirstObjectByType<NarratorAudioManager>();
            if (narrator != null && line != null)
            {
                narrator.Enqueue(line, true);
                fired = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Oyuncu uzaklaştıysa timer’ı iptal et (spam engel)
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
    }
}
