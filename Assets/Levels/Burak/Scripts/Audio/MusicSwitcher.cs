using System.Collections;
using UnityEngine;

public class MusicSwitcher : MonoBehaviour
{
    public AudioSource source;
    public float fadeDuration = 0.6f;

    private Coroutine current;

    private void Reset()
    {
        source = GetComponent<AudioSource>();
    }

    public void SwitchTo(AudioClip newClip, float targetVolume = -1f)
    {
        if (source == null || newClip == null) return;

        if (current != null) StopCoroutine(current);
        current = StartCoroutine(SwitchRoutine(newClip, targetVolume));
    }

    private IEnumerator SwitchRoutine(AudioClip newClip, float targetVolume)
    {
        float startVol = source.volume;

        // Fade out
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(startVol, 0f, t / fadeDuration);
            yield return null;
        }

        source.Stop();
        source.clip = newClip;
        source.Play();

        float endVol = (targetVolume >= 0f) ? targetVolume : startVol;

        // Fade in
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(0f, endVol, t / fadeDuration);
            yield return null;
        }

        source.volume = endVol;
        current = null;
    }
}
