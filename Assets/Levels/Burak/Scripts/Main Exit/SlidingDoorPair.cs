using System.Collections;
using UnityEngine;

public class SlidingDoorPair : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform doorLeft;
    [SerializeField] private Transform doorRight;

    [Header("Local Z")]
    [SerializeField] private float leftStartZ = 0f;
    [SerializeField] private float leftTargetZ = 2f;

    [SerializeField] private float rightStartZ = -1.433132f;
    [SerializeField] private float rightTargetZ = -3.433132f;

    [Header("Timing")]
    [SerializeField] private float openDuration = 4f;
    [SerializeField] private AnimationCurve openEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Audio (Machine Bits 5)")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip machineBitsClip;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
    [SerializeField] private float fadeOutTime = 1.2f;

    [Header("Optional Reverb Tail")]
    [SerializeField] private bool addReverbTail = true;
    [SerializeField] private AudioReverbPreset reverbPreset = AudioReverbPreset.Hangar;
    [SerializeField] private float reverbDecayTime = 1.6f;

    private bool opened = false;
    private bool opening = false;

    private void Awake()
    {
        SetLocalZ(doorLeft, leftStartZ);
        SetLocalZ(doorRight, rightStartZ);
    }

    public void Open()
    {
        if (opened || opening) return;
        StartCoroutine(OpenRoutine());
    }

    private IEnumerator OpenRoutine()
    {
        opening = true;

        // start machine bits loop
        float restoreVol = 1f;
        if (sfxSource != null && machineBitsClip != null)
        {
            restoreVol = sfxSource.volume;

            if (addReverbTail)
            {
                var rev = sfxSource.GetComponent<AudioReverbFilter>();
                if (!rev) rev = sfxSource.gameObject.AddComponent<AudioReverbFilter>();
                rev.reverbPreset = reverbPreset;
                rev.decayTime = reverbDecayTime;
            }

            sfxSource.clip = machineBitsClip;
            sfxSource.loop = true;
            sfxSource.volume = restoreVol * sfxVolume;
            sfxSource.Play();
        }

        Vector3 l0 = doorLeft.localPosition; l0.z = leftStartZ;
        Vector3 r0 = doorRight.localPosition; r0.z = rightStartZ;

        Vector3 l1 = l0; l1.z = leftTargetZ;
        Vector3 r1 = r0; r1.z = rightTargetZ;

        doorLeft.localPosition = l0;
        doorRight.localPosition = r0;

        float t = 0f;
        while (t < openDuration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / openDuration);
            float e = openEase.Evaluate(n);

            if (doorLeft) doorLeft.localPosition = Vector3.Lerp(l0, l1, e);
            if (doorRight) doorRight.localPosition = Vector3.Lerp(r0, r1, e);

            yield return null;
        }

        if (doorLeft) doorLeft.localPosition = l1;
        if (doorRight) doorRight.localPosition = r1;

        // fade out loop
        if (sfxSource != null && sfxSource.isPlaying)
            yield return FadeOutAndStop(sfxSource, fadeOutTime, restoreVol);

        opened = true;
        opening = false;
    }

    private IEnumerator FadeOutAndStop(AudioSource src, float fadeTime, float restoreVol)
    {
        float startVol = src.volume;
        float t = 0f;

        while (t < fadeTime)
        {
            t += Time.deltaTime;
            src.volume = Mathf.Lerp(startVol, 0f, t / fadeTime);
            yield return null;
        }

        src.Stop();
        src.loop = false;
        src.clip = null;
        src.volume = restoreVol;
    }

    private void SetLocalZ(Transform tr, float z)
    {
        if (tr == null) return;
        Vector3 p = tr.localPosition;
        p.z = z;
        tr.localPosition = p;
    }
}
