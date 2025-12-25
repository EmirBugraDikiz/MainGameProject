using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum NarratorStepType
{
    SetIdle,
    SetTalk,
    SetAngry,
    PlayVoice,
    Wait,
    UnlockExitButton
}

[System.Serializable]
public class NarratorStep
{
    public NarratorStepType type;

    [Header("Voice")]
    public AudioClip voiceClip;

    [Header("Wait")]
    public float waitSeconds = 1f;
}

public class NarratorSequenceController : MonoBehaviour
{
    [Header("Refs")]
    public AudioSource voiceSource;
    public NarratorFaceController face;
    public ExitButtonInteract exitButton;

    [Header("Auto pacing")]
    public bool autoIdleAfterVoice = true;
    public float autoIdleGap = 1.0f;

    [Header("Sequence")]
    public NarratorStep[] steps;

    // Injected clips queue (early button etc.)
    private readonly Queue<AudioClip> injectedClips = new Queue<AudioClip>();
    private bool started;
    private bool running;

    // sadece injected (early press / finish inject) için talk zorlaması
    private bool talkForNextInjected = false;

    public bool IsRunning => running;

    // Normal inject (face'e dokunmaz)
    public void EnqueueInjected(AudioClip clip)
    {
        if (clip == null) return;
        injectedClips.Enqueue(clip);
    }

    // Inject ama konuşurken Talk'a geçsin (bitince Idle'a döner)
    public void EnqueueInjectedAsTalk(AudioClip clip)
    {
        if (clip == null) return;
        talkForNextInjected = true;
        injectedClips.Enqueue(clip);
    }

    /// <summary>
    /// Sequence çalışmıyorsa narrator voiceSource ile direkt çalar ve bitmesini bekletebilir.
    /// Sequence çalışıyorsa injected olarak kuyruğa atar.
    /// </summary>
    public IEnumerator PlayStandalone(AudioClip clip, bool asTalk)
    {
        if (clip == null || voiceSource == null) yield break;

        // Sequence çalışıyorsa queue'ya at, o arada çalınsın
        if (running)
        {
            if (asTalk) EnqueueInjectedAsTalk(clip);
            else EnqueueInjected(clip);
            yield break;
        }

        // Sequence yokken direkt çal (narrator'dan)
        // Eğer voiceSource başka bir şey çalıyorsa bitmesini bekle
        while (voiceSource.isPlaying)
            yield return null;

        if (asTalk) face?.SetTalk();

        voiceSource.clip = clip;
        voiceSource.Play();
        yield return new WaitWhile(() => voiceSource.isPlaying);

        // bitti -> idle + gap
        if (autoIdleAfterVoice)
        {
            face?.SetIdle();
            if (autoIdleGap > 0f)
                yield return new WaitForSeconds(autoIdleGap);
        }
    }

    public void StartSequence()
    {
        if (started) return;
        started = true;
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        running = true;

        foreach (var step in steps)
        {
            switch (step.type)
            {
                case NarratorStepType.SetIdle:
                    face?.SetIdle();
                    break;

                case NarratorStepType.SetTalk:
                    face?.SetTalk();
                    break;

                case NarratorStepType.SetAngry:
                    face?.SetAngry();
                    break;

                case NarratorStepType.PlayVoice:
                    yield return PlayVoiceAndThenInjected(step.voiceClip);
                    break;

                case NarratorStepType.Wait:
                    if (step.waitSeconds > 0f)
                        yield return new WaitForSeconds(step.waitSeconds);
                    break;

                case NarratorStepType.UnlockExitButton:
                    if (exitButton != null)
                    {
                        exitButton.Unlock();
                        Debug.Log("[NarratorSequenceController] UnlockExitButton step fired.");
                    }
                    else
                    {
                        Debug.LogWarning("[NarratorSequenceController] exitButton ref missing!");
                    }
                    break;
            }

            yield return null;
        }

        // Sequence bitince: injected kaldıysa bitir
        while (injectedClips.Count > 0)
            yield return PlayVoiceClip(injectedClips.Dequeue(), isInjected: true);

        running = false;
    }

    private IEnumerator PlayVoiceAndThenInjected(AudioClip mainClip)
    {
        if (mainClip != null)
            yield return PlayVoiceClip(mainClip, isInjected: false);

        while (injectedClips.Count > 0)
            yield return PlayVoiceClip(injectedClips.Dequeue(), isInjected: true);
    }

    private IEnumerator PlayVoiceClip(AudioClip clip, bool isInjected)
    {
        if (voiceSource == null || clip == null) yield break;

        // SADECE injected ve talk flag set ise talk'a geç
        if (isInjected && talkForNextInjected)
            face?.SetTalk();

        voiceSource.clip = clip;
        voiceSource.Play();
        yield return new WaitWhile(() => voiceSource.isPlaying);

        if (isInjected && talkForNextInjected)
            talkForNextInjected = false;

        if (autoIdleAfterVoice)
        {
            face?.SetIdle();
            if (autoIdleGap > 0f)
                yield return new WaitForSeconds(autoIdleGap);
        }
    }
}
