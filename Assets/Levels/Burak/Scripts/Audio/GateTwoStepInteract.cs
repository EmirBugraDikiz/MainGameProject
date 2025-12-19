using System.Collections;
using UnityEngine;

public class GateTwoStepInteract : MonoBehaviour
{
    public enum GateState
    {
        IdleStep1,       // Cup kapalı, ilk E bekleniyor
        AnimatingStep1,  // Cup açılıyor
        IdleStep2,       // Cup açık, ikinci E bekleniyor
        AnimatingStep2,  // Knob iniyor
        OpeningDoors,    // Kapılar açılıyor
        Done             // Her şey tamam
    }

    [Header("References")]
    [SerializeField] private Transform player;             // Opsiyonel: boş bırak, trigger ile yakalıyoruz
    [SerializeField] private Transform cup;                // Cup transform
    [SerializeField] private Transform mainKnob;           // Main Knob transform
    [SerializeField] private Transform doorLeft;           // Door_Left transform
    [SerializeField] private Transform doorRight;          // Door_Right transform
    [SerializeField] private Collider interactTrigger;     // Trigger collider (IsTrigger true)

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip switchOpenClip;     // Switch_Open.wav (1.326s)
    [SerializeField] private AudioClip metalShieldClip;    // Metal_Shield_Open.wav (2.282s)

    [Header("Step 1 - Cup Rotation (local)")]
    [SerializeField] private float cupStartX = -90f;
    [SerializeField] private float cupTargetX = 45f;
    [SerializeField] private float cupDuration = 1.326f;
    [SerializeField] private AnimationCurve cupEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Step 2 - Knob Rotation (local)")]
    [SerializeField] private float knobStartX = -90f;
    [SerializeField] private float knobTargetX = 38f;
    [SerializeField] private float knobDuration = 1.1f; // kol hareketi kısa ama "tok"; ses uzun kalabilir
    [Tooltip("AAA hissi: başta yavaş, sonra hızlanıp 'tık' diye iner")]
    [SerializeField] private AnimationCurve knobEase = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 0.2f),
        new Keyframe(0.6f, 0.25f, 0.2f, 2.5f),
        new Keyframe(1f, 1f, 0f, 0f)
    );

    [Header("Doors - Local Z Movement")]
    [SerializeField] private float doorLeftStartZ = 0f;
    [SerializeField] private float doorLeftTargetZ = 1.5f;
    [SerializeField] private float doorRightStartZ = -1.433132f;
    [SerializeField] private float doorRightTargetZ = -3.3f;
    [SerializeField] private float doorsDuration = 1.0f;
    [SerializeField] private AnimationCurve doorsEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private GateState state = GateState.IdleStep1;
    private bool playerInside;

    private void Reset()
    {
        // Inspector doldurmayı kolaylaştırır (istersen elle de verirsin)
        audioSource = GetComponent<AudioSource>();
    }

    private void Awake()
    {
        if (interactTrigger != null)
            interactTrigger.isTrigger = true;

        // Başlangıç poz/rotları garanti altına al (istersen kapatabilirsin)
        ApplyLocalX(cup, cupStartX);
        ApplyLocalX(mainKnob, knobStartX);
        SetLocalZ(doorLeft, doorLeftStartZ);
        SetLocalZ(doorRight, doorRightStartZ);
    }

    private void Update()
    {
        if (!playerInside) return;
        if (Input.GetKeyDown(interactKey))
        {
            TryInteract();
        }
    }

    private void TryInteract()
    {
        if (state == GateState.IdleStep1)
        {
            StartCoroutine(CupOpenSequence());
        }
        else if (state == GateState.IdleStep2)
        {
            StartCoroutine(KnobAndDoorsSequence());
        }
        // Done veya animasyon sırasında E basılırsa ignore (spam protection)
    }

    private IEnumerator CupOpenSequence()
    {
        state = GateState.AnimatingStep1;

        // Ses
        PlayOneShotSafe(switchOpenClip);

        // Cup rotation (local X)
        yield return RotateLocalX(cup, cupStartX, cupTargetX, cupDuration, cupEase);

        state = GateState.IdleStep2;
    }

    private IEnumerator KnobAndDoorsSequence()
    {
        state = GateState.AnimatingStep2;

        // Ses uzun, kol kısa: ses devam ederken kapı açılmasına geçebiliriz (daha sinematik)
        PlayOneShotSafe(metalShieldClip);

        // Knob rotation (local X) - AAA curve
        yield return RotateLocalX(mainKnob, knobStartX, knobTargetX, knobDuration, knobEase);

        // Kapılar aynı anda aynı hızda
        state = GateState.OpeningDoors;
        yield return MoveDoorsLocalZ();

        state = GateState.Done;
    }

    private IEnumerator MoveDoorsLocalZ()
    {
        Vector3 leftStart = doorLeft.localPosition;
        Vector3 leftEnd = new Vector3(leftStart.x, leftStart.y, doorLeftTargetZ);

        Vector3 rightStart = doorRight.localPosition;
        Vector3 rightEnd = new Vector3(rightStart.x, rightStart.y, doorRightTargetZ);

        // başlangıç değerlerini override etmek istiyorsan:
        leftStart.z = doorLeftStartZ;
        rightStart.z = doorRightStartZ;
        doorLeft.localPosition = leftStart;
        doorRight.localPosition = rightStart;

        float t = 0f;
        while (t < doorsDuration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / doorsDuration);
            float eased = doorsEase.Evaluate(n);

            doorLeft.localPosition = Vector3.LerpUnclamped(leftStart, leftEnd, eased);
            doorRight.localPosition = Vector3.LerpUnclamped(rightStart, rightEnd, eased);

            yield return null;
        }

        doorLeft.localPosition = leftEnd;
        doorRight.localPosition = rightEnd;
    }

    private IEnumerator RotateLocalX(Transform tr, float fromX, float toX, float duration, AnimationCurve curve)
    {
        if (tr == null) yield break;

        // mevcut local rotasyonun Y/Z’sini bozmadan X’i yönetiyoruz
        Vector3 startEuler = tr.localEulerAngles;
        Vector3 endEuler = startEuler;

        // Unity euler wrap (0-360) mevzusu: bizim verdiğimiz -90 gibi değerleri normalize edelim
        float startX = fromX;
        float endX = toX;

        // Başlangıca zorla oturt
        startEuler.x = NormalizeAngle(startX);
        tr.localEulerAngles = startEuler;

        endEuler.x = NormalizeAngle(endX);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / duration);
            float eased = curve.Evaluate(n);

            float x = Mathf.LerpAngle(startEuler.x, endEuler.x, eased);
            Vector3 e = tr.localEulerAngles;
            e.x = x;
            tr.localEulerAngles = e;

            yield return null;
        }

        tr.localEulerAngles = endEuler;
    }

    private void ApplyLocalX(Transform tr, float xDeg)
    {
        if (tr == null) return;
        Vector3 e = tr.localEulerAngles;
        e.x = NormalizeAngle(xDeg);
        tr.localEulerAngles = e;
    }

    private void SetLocalZ(Transform tr, float z)
    {
        if (tr == null) return;
        Vector3 p = tr.localPosition;
        p.z = z;
        tr.localPosition = p;
    }

    private float NormalizeAngle(float a)
    {
        // -90 -> 270 gibi
        a %= 360f;
        if (a < 0f) a += 360f;
        return a;
    }

    private void PlayOneShotSafe(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        audioSource.PlayOneShot(clip);
    }

    // Trigger ile player yakalama
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("TRIGGER ENTER: " + other.name + " tag=" + other.tag);
        if (other.CompareTag("Player"))
        {
            playerInside = true;
            if (player == null) player = other.transform;
            Debug.Log("PLAYER INSIDE = TRUE");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log("TRIGGER EXIT: " + other.name);
        if (other.CompareTag("Player"))
        {
            playerInside = false;
            Debug.Log("PLAYER INSIDE = FALSE");
        }
    }
}
