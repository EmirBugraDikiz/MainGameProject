using System.Collections;
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

    [Header("Sequence")]
    public NarratorStep[] steps;

    private bool started;

    public void StartSequence()
    {
        if (started) return;
        started = true;
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
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
                    if (voiceSource != null && step.voiceClip != null)
                    {
                        voiceSource.clip = step.voiceClip;
                        voiceSource.Play();
                        yield return new WaitWhile(() => voiceSource.isPlaying);
                    }
                    break;

                case NarratorStepType.Wait:
                    yield return new WaitForSeconds(step.waitSeconds);
                    break;

                case NarratorStepType.UnlockExitButton:
                    exitButton?.Unlock();
                    break;
            }

            yield return null;
        }
    }
}
