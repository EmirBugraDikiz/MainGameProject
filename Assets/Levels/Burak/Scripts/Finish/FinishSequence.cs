using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FinishSequence : MonoBehaviour
{
    [Header("Timings")]
    public float animationTotalDuration = 6.19f;

    [Header("UI")]
    public GameObject blackPanel;
    public GameObject titleImage;
    public GameObject exitHintText;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Audio Clips")]
    public AudioClip morningAmbience;
    public AudioClip endingHardSfx;
    public AudioClip menuWatchesYouMusic;

    [Header("Scene")]
    public string mainMenuSceneName = "MainMenu";

    bool waitingForExit = false;

    void Start()
    {
        if (blackPanel) blackPanel.SetActive(false);
        if (titleImage) titleImage.SetActive(false);
        if (exitHintText) exitHintText.SetActive(false);

        if (musicSource && morningAmbience)
        {
            musicSource.loop = true;
            musicSource.clip = morningAmbience;
            musicSource.Play();
        }

        StartCoroutine(Sequence());
    }

    void Update()
    {
        if (!waitingForExit) return;

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    IEnumerator Sequence()
    {
        yield return new WaitForSeconds(animationTotalDuration);

        if (blackPanel) blackPanel.SetActive(true);
        if (musicSource) musicSource.Stop();

        float sfxLen = PlaySfxAndGetLength(endingHardSfx);
        if (sfxLen > 0f) yield return new WaitForSeconds(sfxLen);

        if (titleImage) titleImage.SetActive(true);

        sfxLen = PlaySfxAndGetLength(endingHardSfx);
        if (sfxLen > 0f) yield return new WaitForSeconds(sfxLen);

        if (exitHintText) exitHintText.SetActive(true);

        if (musicSource && menuWatchesYouMusic)
        {
            musicSource.loop = true;
            musicSource.clip = menuWatchesYouMusic;
            musicSource.Play();
        }

        waitingForExit = true;
    }

    float PlaySfxAndGetLength(AudioClip clip)
    {
        if (!sfxSource || !clip) return 0f;

        sfxSource.Stop();
        sfxSource.clip = clip;
        sfxSource.loop = false;
        sfxSource.Play();
        return clip.length;
    }
}
