using UnityEngine;

public class SlidePlatform : MonoBehaviour
{
    [Header("Movement")]
    public Transform targetPos;      // Platformun kayacağı hedef nokta
    public float moveTime = 0.3f;    // Kaç saniyede kayacak

    [Header("Audio")]
    public AudioSource sfxSource;    // Woosh burada çalacak
    public AudioClip wooshClip;
    [Range(0f, 1f)] public float wooshVolume = 0.9f;

    bool isMoving = false;
    Vector3 startPos;
    float t = 0f;

    void Start()
    {
        startPos = transform.position;

        // AudioSource otomatik al (istersen inspector'dan da verirsin)
        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();
    }

    public void Activate()
    {
        if (isMoving) return;

        isMoving = true;
        t = 0f;

        // Woosh sesi (hareket başlarken)
        if (sfxSource != null && wooshClip != null)
            sfxSource.PlayOneShot(wooshClip, wooshVolume);
    }

    void Update()
    {
        if (!isMoving) return;

        t += Time.deltaTime / moveTime;
        transform.position = Vector3.Lerp(startPos, targetPos.position, t);

        if (t >= 1f)
        {
            isMoving = false;
        }
    }
}
