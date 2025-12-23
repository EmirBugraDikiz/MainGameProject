using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class L1NarrationManager : MonoBehaviour
{
    public static L1NarrationManager I;

    [Header("Audio")]
    public AudioSource source;

    [Header("Behavior")]
    [Tooltip("Yeni bir line gelince eskisini kesip yenisini hemen başlatır. (false = sıraya alır)")]
    public bool interruptInsteadOfQueue = false;

    [Header("Startup")]
    [Tooltip("Wakeup sesi trigger delay=0 olsa bile bu kadar gecikmeli başlar.")]
    public float wakeupExtraDelay = 0.8f;

    [Header("Clips")]
    public AudioClip L1_01_Wakeup;
    public AudioClip L1_02_jump1;
    public AudioClip L1_03_Fake_Platform;
    public AudioClip L1_04_Fake_Platform_After_Death;
    public AudioClip L1_05_Fake_Parkour;
    public AudioClip L1_06_New_Parkour;
    public AudioClip L1_07_Starting_Spike_Walls;
    public AudioClip L1_08_Wrong_Run;
    public AudioClip L1_09_Fake_Spike_Wall_Wait;
    public AudioClip L1_10_Finish;

    private readonly HashSet<string> played = new HashSet<string>();

    private struct QueueItem
    {
        public string key;
        public AudioClip clip;
        public float delay;
    }

    private readonly Queue<QueueItem> queue = new Queue<QueueItem>();
    private Coroutine worker;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;

        if (source == null) source = GetComponent<AudioSource>();
        if (source == null) source = gameObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = false;
    }

    public bool HasPlayed(string key) => played.Contains(key);

    public void Enqueue(string key, AudioClip clip, float delay = 0f, bool playOnceKey = true)
    {
        if (clip == null) return;

        // Wakeup her zaman az geciksin (delay verilmediyse)
        if (key == "L1_01_Wakeup" && delay <= 0f)
            delay = wakeupExtraDelay;

        // playOnce ise daha önce çaldıysa hiç girme
        if (playOnceKey && played.Contains(key)) return;

        if (interruptInsteadOfQueue)
        {
            StopAllNarration(clearQueue: true);

            // Queue spam yemesin diye key'i anında işaretle
            if (playOnceKey) played.Add(key);

            queue.Enqueue(new QueueItem { key = key, clip = clip, delay = delay });
            StartWorkerIfNeeded();
            return;
        }

        // Queue mod: aynı key aynı frame’de 3 trigger’dan gelse bile 1 kere girsin
        if (playOnceKey) played.Add(key);

        queue.Enqueue(new QueueItem { key = key, clip = clip, delay = delay });
        StartWorkerIfNeeded();
    }

    // Eski isimler ile uyumluluk (triggerlar bunları çağırıyorsa bozulmasın)
    public void PlayOnce(string key, AudioClip clip, float delay = 0f)
        => Enqueue(key, clip, delay, playOnceKey: true);

    public void Play(AudioClip clip, float delay = 0f)
        => Enqueue("__no_key__" + Time.frameCount, clip, delay, playOnceKey: false);

    public void StopAllNarration(bool clearQueue = true)
    {
        if (worker != null)
        {
            StopCoroutine(worker);
            worker = null;
        }

        if (source != null)
        {
            source.Stop();
            source.clip = null;
        }

        if (clearQueue)
            queue.Clear();
    }

    private void StartWorkerIfNeeded()
    {
        if (worker == null)
            worker = StartCoroutine(Worker());
    }

    private IEnumerator Worker()
    {
        while (queue.Count > 0)
        {
            var item = queue.Dequeue();

            if (item.delay > 0f)
                yield return new WaitForSeconds(item.delay);

            if (item.clip == null) continue;

            source.clip = item.clip;
            source.loop = false;
            source.Play();

            while (source != null && source.isPlaying)
                yield return null;

            source.clip = null;
        }

        worker = null;
    }
}
