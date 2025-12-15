using UnityEngine;
using System.Collections;
// BU SATIR HATAYI ÇÖZER: Random komutunun Unity'den geleceðini belirtiyoruz.
using Random = UnityEngine.Random;

public class HorrorLight : MonoBehaviour
{
    [Header("Hedef")]
    public Transform player;
    public float activationDistance = 5.0f;

    [Header("Davranýþ Ayarlarý")]
    public bool randomizeMode = true;
    [Range(0, 2)]
    public int forcedMode = 0;

    // 0: Normal 
    // 1: Flicker (Titreme)
    // 2: Patlama (Sönük kalma)

    [Header("Flicker (Case 1) Ayarlarý")]
    public float soundDuration = 2.0f;
    public float minIntensity = 0.0f;
    public float maxIntensity = 1.5f;

    [Header("Patlama (Case 2) Ayarlarý")]
    public float blownOutDuration = 30.0f;
    public AudioClip blownSound;

    [Header("Bileþenler")]
    public AudioSource audioSource;
    private Light myLight;
    private float defaultIntensity;

    private bool isActive = false;
    private int currentMode;

    void Start()
    {
        myLight = GetComponent<Light>();
        defaultIntensity = myLight.intensity;

        if (player == null && GameObject.FindGameObjectWithTag("Player"))
            player = GameObject.FindGameObjectWithTag("Player").transform;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        audioSource.loop = false;

        PickMode();
    }

    void PickMode()
    {
        if (randomizeMode)
        {
            // Artýk burada hata vermeyecek
            currentMode = Random.Range(0, 3);
        }
        else
        {
            currentMode = forcedMode;
        }
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance < activationDistance && !isActive)
        {
            if (currentMode == 0)
            {
                isActive = true;
            }
            else
            {
                StartCoroutine(ProcessLightBehavior());
            }
        }
        else if (distance > activationDistance + 1.0f && isActive)
        {
            StopAllCoroutines();
            ResetSystem();
        }
    }

    IEnumerator ProcessLightBehavior()
    {
        isActive = true;

        switch (currentMode)
        {
            case 1:
                yield return StartCoroutine(LightLoopWithLimitedSound());
                break;

            case 2:
                yield return StartCoroutine(BlowOutEffect());
                break;

            case 0:
                break;
        }
    }

    IEnumerator LightLoopWithLimitedSound()
    {
        audioSource.Play();

        float currentSoundTimer = 0f;
        bool soundStopped = false;

        while (true)
        {
            myLight.intensity = Random.Range(minIntensity, maxIntensity);
            float waitTime = Random.Range(0.05f, 0.2f);

            if (!soundStopped)
            {
                currentSoundTimer += waitTime;
                if (currentSoundTimer >= soundDuration)
                {
                    audioSource.Stop();
                    soundStopped = true;
                }
            }
            yield return new WaitForSeconds(waitTime);
        }
    }

    IEnumerator BlowOutEffect()
    {
        if (blownSound != null) audioSource.PlayOneShot(blownSound);

        myLight.intensity = 0;

        yield return new WaitForSeconds(blownOutDuration);

        currentMode = 0;
        ResetSystem();
    }

    void ResetSystem()
    {
        isActive = false;
        myLight.intensity = defaultIntensity;
        audioSource.Stop();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = currentMode == 2 ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationDistance);
    }
}