using System.Collections;
using TMPro;
using UnityEngine;

public class TutorialHintUI : MonoBehaviour
{
    public static TutorialHintUI Instance;

    [Header("UI Refs")]
    public CanvasGroup panelGroup;
    public TextMeshProUGUI hintText;

    [Header("Timings")]
    public float fadeIn = 0.2f;
    public float stay = 3.5f;
    public float fadeOut = 0.3f;

    Coroutine routine;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // İstersen sahneler arası kalsın:
        // DontDestroyOnLoad(gameObject);

        HideInstant();
    }

    public void Show(string msg, float? stayOverride = null)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(ShowRoutine(msg, stayOverride ?? stay));
    }

    public void HideInstant()
    {
        if (panelGroup)
        {
            panelGroup.alpha = 0f;
            panelGroup.blocksRaycasts = false;
            panelGroup.interactable = false;
        }
    }

    IEnumerator ShowRoutine(string msg, float stayTime)
    {
        hintText.text = msg;

        panelGroup.blocksRaycasts = false;
        panelGroup.interactable = false;

        // Fade in
        yield return Fade(0f, 1f, fadeIn);

        // Stay
        yield return new WaitForSeconds(stayTime);

        // Fade out
        yield return Fade(1f, 0f, fadeOut);

        routine = null;
    }

    IEnumerator Fade(float from, float to, float t)
    {
        if (t <= 0f)
        {
            panelGroup.alpha = to;
            yield break;
        }

        float time = 0f;
        panelGroup.alpha = from;

        while (time < t)
        {
            time += Time.deltaTime;
            panelGroup.alpha = Mathf.Lerp(from, to, time / t);
            yield return null;
        }

        panelGroup.alpha = to;
    }
}
