using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NarratorAudioManager : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource source;

    [Range(0f, 1f)] public float baseVolume = 1f;
    public float gapAfterLine = 0.15f;

    private readonly Queue<AudioClip> queue = new Queue<AudioClip>();
    private readonly HashSet<AudioClip> playedOnce = new HashSet<AudioClip>();
    private bool isPlaying = false;

    void Awake()
    {
        if (source == null) source = GetComponent<AudioSource>();
        if (source == null) source = gameObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f; // 2D
        source.volume = baseVolume;
    }

    public void Enqueue(AudioClip clip, bool playOnlyOnce = true)
    {
        if (clip == null) return;
        if (playOnlyOnce && playedOnce.Contains(clip)) return;

        queue.Enqueue(clip);
        if (!isPlaying) StartCoroutine(PlayQueue());
    }

    private IEnumerator PlayQueue()
    {
        isPlaying = true;

        while (queue.Count > 0)
        {
            var clip = queue.Dequeue();
            playedOnce.Add(clip);

            source.clip = clip;
            source.volume = baseVolume;
            source.Play();

            yield return new WaitWhile(() => source.isPlaying);
            if (gapAfterLine > 0f) yield return new WaitForSeconds(gapAfterLine);
        }

        isPlaying = false;
    }
}
