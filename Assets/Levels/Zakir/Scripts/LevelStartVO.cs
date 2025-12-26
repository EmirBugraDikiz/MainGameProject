using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class LevelStartVO : MonoBehaviour
{
    [Header("VO")]
    public AudioClip startClip;
    [Range(0f, 2f)] public float delay = 0.5f;
    [Range(0f, 1f)] public float volume = 1f;

    private AudioSource _source;

    void Awake()
    {
        _source = GetComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.spatialBlend = 0f; // 2D
    }

    void Start()
    {
        if (startClip != null)
            StartCoroutine(PlayDelayed());
    }

    IEnumerator PlayDelayed()
    {
        yield return new WaitForSeconds(delay);
        _source.PlayOneShot(startClip, volume);
    }
}
