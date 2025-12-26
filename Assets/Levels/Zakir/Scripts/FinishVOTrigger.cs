using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FinishVOTrigger : MonoBehaviour
{
    [Header("Finish VO")]
    public AudioClip finishClip;   // L3_02_End
    [Range(0f, 1f)] public float volume = 1f;

    private AudioSource _source;
    private bool _played = false;

    void Awake()
    {
        _source = GetComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.spatialBlend = 0f; // 2D anlatıcı sesi
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_played) return;
        if (!other.CompareTag("Player")) return;

        _played = true;

        if (finishClip != null)
            _source.PlayOneShot(finishClip, volume);
    }
}
