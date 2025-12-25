using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NarratorAudioQueue : MonoBehaviour
{
    public static NarratorAudioQueue Instance { get; private set; }

    [Header("Output")]
    public AudioSource source;

    [Header("Debug")]
    public bool logPlays = false;

    private readonly Queue<QueueItem> queue = new();
    private Coroutine runner;

    private struct QueueItem
    {
        public AudioClip clip;
        public float delay;
        public Action onComplete;

        public QueueItem(AudioClip clip, float delay, Action onComplete)
        {
            this.clip = clip;
            this.delay = delay;
            this.onComplete = onComplete;
        }
    }

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (!source) source = GetComponent<AudioSource>();
        if (!source) source = gameObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f; // 2D narrator
    }

    public void Enqueue(AudioClip clip, float delay = 0f, Action onComplete = null)
    {
        if (!clip) return;

        queue.Enqueue(new QueueItem(clip, Mathf.Max(0f, delay), onComplete));

        if (runner == null)
            runner = StartCoroutine(Run());
    }

    public void Clear(bool stopCurrent = true)
    {
        queue.Clear();
        if (stopCurrent && source) source.Stop();

        if (runner != null)
        {
            StopCoroutine(runner);
            runner = null;
        }
    }

    public bool IsBusy()
    {
        return (source && source.isPlaying) || queue.Count > 0;
    }

    private IEnumerator Run()
    {
        while (queue.Count > 0)
        {
            var item = queue.Dequeue();

            if (item.delay > 0f)
                yield return new WaitForSeconds(item.delay);

            if (!source || !item.clip) continue;

            source.clip = item.clip;
            source.Play();

            if (logPlays) Debug.Log($"[NarratorQueue] Playing: {item.clip.name}");

            // bitene kadar bekle
            while (source.isPlaying)
                yield return null;

            item.onComplete?.Invoke();
        }

        runner = null;
    }
}
