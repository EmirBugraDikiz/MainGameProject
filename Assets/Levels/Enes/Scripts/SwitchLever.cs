using UnityEngine;

public class SwitchLever : MonoBehaviour
{
    [Header("Parts")]
    public Transform cup;       // Cup
    public Transform mainKnob;  // Main Knob

    [Header("X Rot Targets (Local)")]
    public float cupClosedX = -90f;
    public float cupOpenX = 45f;

    public float knobUpX = -90f;
    public float knobDownX = 38.058f;

    [Header("Interact")]
    public KeyCode interactKey = KeyCode.E;
    public string playerTag = "Player";

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip metalShieldOpen; // Metal_Shield_Open
    public AudioClip switchOpen;      // Switch_Open
    [Range(0f, 1f)] public float volume = 1f;

    [Header("Smooth")]
    public bool smoothRotate = true;
    public float rotateSpeed = 10f;

    // 0: kapalı, 1: kapak açıldı, 2: şalter indirildi (tamamlandı)
    [SerializeField] private int state = 0;
    private bool playerInside = false;
    private bool countedToDoor = false;

    void Reset()
    {
        if (!sfxSource) sfxSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (!playerInside) return;

        if (Input.GetKeyDown(interactKey))
        {
            if (state == 0)
            {
                state = 1;
                PlayOneShot(metalShieldOpen);
                if (!smoothRotate && cup) SetLocalX(cup, cupOpenX);
            }
            else if (state == 1)
            {
                state = 2;
                PlayOneShot(switchOpen);
                if (!smoothRotate && mainKnob) SetLocalX(mainKnob, knobDownX);

                // Kapıya 1 kez saydır
                if (!countedToDoor && KilitliKapi.instance != null)
                {
                    countedToDoor = true;
                    KilitliKapi.instance.ObjeToplandi();
                }
            }
        }

        if (smoothRotate)
        {
            if (cup)
            {
                float targetX = (state >= 1) ? cupOpenX : cupClosedX;
                SmoothSetLocalX(cup, targetX);
            }

            if (mainKnob)
            {
                float targetX = (state >= 2) ? knobDownX : knobUpX;
                SmoothSetLocalX(mainKnob, targetX);
            }
        }
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (!clip) return;

        if (!sfxSource)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.spatialBlend = 1f;
            sfxSource.rolloffMode = AudioRolloffMode.Logarithmic;
            sfxSource.minDistance = 1f;
            sfxSource.maxDistance = 30f;
        }

        sfxSource.PlayOneShot(clip, volume);
    }

    private void SetLocalX(Transform t, float x)
    {
        var e = t.localEulerAngles;
        e.x = x;
        t.localEulerAngles = e;
    }

    private void SmoothSetLocalX(Transform t, float targetX)
    {
        var e = t.localEulerAngles;
        float currentX = NormalizeAngle(e.x);
        float target = NormalizeAngle(targetX);

        float newX = Mathf.LerpAngle(currentX, target, Time.deltaTime * rotateSpeed);
        e.x = newX;
        t.localEulerAngles = e;
    }

    private float NormalizeAngle(float a)
    {
        a %= 360f;
        if (a > 180f) a -= 360f;
        return a;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag)) playerInside = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag)) playerInside = false;
    }
}
