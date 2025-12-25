using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(Collider))]
public class RickRollWorldScreenTrigger : MonoBehaviour
{
    public string playerTag = "Player";
    public bool playOnlyOnceEver = true;

    [Header("Narrator (queued)")]
    public AudioClip narratorClip;

    [Header("World Screen + Video")]
    public GameObject worldScreenRoot;   // RickRoll_Canvas
    public VideoPlayer videoPlayer;      // RickRoll_VideoPlayer

    private bool hasEverPlayed;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Awake()
    {
        if (worldScreenRoot) worldScreenRoot.SetActive(false);

        if (videoPlayer)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = true;
            videoPlayer.Stop();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (playOnlyOnceEver && hasEverPlayed) return;

        hasEverPlayed = true;

        // Önce narrator (queue), bitince video
        if (NarratorAudioQueue.Instance != null && narratorClip != null)
        {
            NarratorAudioQueue.Instance.Enqueue(narratorClip, 0f, StartRickRoll);
        }
        else
        {
            // Fallback
            StartRickRoll();
        }
    }

    private void StartRickRoll()
    {
        if (worldScreenRoot) worldScreenRoot.SetActive(true);

        if (videoPlayer)
        {
            videoPlayer.time = 0;
            videoPlayer.Play();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        if (videoPlayer) videoPlayer.Stop();
        if (worldScreenRoot) worldScreenRoot.SetActive(false);
    }
}
