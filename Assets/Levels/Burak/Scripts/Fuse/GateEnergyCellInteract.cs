using System.Collections;
using UnityEngine;

public class GateEnergyCellInteract : MonoBehaviour
{
    public enum State { IdleStep1, AnimStep1, IdleStep2, Busy, Done }

    [Header("References")]
    [SerializeField] private Transform cup;
    [SerializeField] private Transform mainKnob;
    [SerializeField] private Transform doorLeft;
    [SerializeField] private Transform doorRight;
    [SerializeField] private Collider interactTrigger;

    [Header("Energy Cell Socket")]
    [SerializeField] private Transform cellSocket; // ShieldMetall içindeki “takılacağı” boş nokta

    [Header("Audio Sources (GateController)")]
    [SerializeField] private AudioSource sfxSource;      // one-shot
    [SerializeField] private AudioSource machineSource;  // loop + fade

    [Header("Audio Clips")]
    [SerializeField] private AudioClip capOpenClip;      // Metal_Shield_Open.wav (kapak)
    [SerializeField] private AudioClip installFuseClip;  // Install_Fuse.wav
    [SerializeField] private AudioClip alarmClip;        // Allarm_Sound.wav (23s)
    [SerializeField] private AudioClip machineBitsClip;  // Machine Bits 5.wav (7s)

    [Header("Step 1 - Cup Rotation (local X)")]
    [SerializeField] private float cupStartX = -90f;
    [SerializeField] private float cupTargetX = 45f;
    [SerializeField] private float cupDuration = 1.0f;
    [SerializeField] private AnimationCurve cupEase = AnimationCurve.EaseInOut(0,0,1,1);

    [Header("Step 2 - Knob Rotation (local X)")]
    [SerializeField] private float knobStartX = -90f;
    [SerializeField] private float knobTargetX = 38f;
    [SerializeField] private float knobDuration = 0.6f;
    [SerializeField] private AnimationCurve knobEase = AnimationCurve.EaseInOut(0,0,1,1);

    [Header("Doors - Local Z Movement (alarm boyunca)")]
    [SerializeField] private float doorLeftStartZ = 0f;
    [SerializeField] private float doorLeftTargetZ = 1.9f;
    [SerializeField] private float doorRightStartZ = -1.433132f;
    [SerializeField] private float doorRightTargetZ = -3.18f;
    [SerializeField] private AnimationCurve doorsEase = AnimationCurve.EaseInOut(0,0,1,1);

    [Header("Timing")]
    [SerializeField] private float machineStartDelay = 1.2f; // alarm başladıktan kaç sn sonra kapı+motor başlasın
    [SerializeField] private float machineFadeOut = 1.2f;    // kapı bitince motor sesi yavaş sönsün

    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private State state = State.IdleStep1;
    private bool playerInside;

    private void Awake()
    {
        if (interactTrigger != null) interactTrigger.isTrigger = true;

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
            if (state == State.IdleStep1) StartCoroutine(Step1_OpenCap());
            else if (state == State.IdleStep2) StartCoroutine(Step2_InstallAndOpen());
        }
    }

    private IEnumerator Step1_OpenCap()
    {
        state = State.AnimStep1;

        PlayOneShot(sfxSource, capOpenClip);
        yield return RotateLocalX(cup, cupStartX, cupTargetX, cupDuration, cupEase);

        state = State.IdleStep2;
    }

    private IEnumerator Step2_InstallAndOpen()
    {
        if (state != State.IdleStep2) yield break;

        // Hücre var mı?
        if (!EnergyCellInventory.HasCell)
        {
            // burada istersen “hücre lazım” uyarı sesi/ekran mesajı
            yield break;
        }

        state = State.Busy;

        // knob anim
        yield return RotateLocalX(mainKnob, knobStartX, knobTargetX, knobDuration, knobEase);

        // hücreyi tak (toplanan objeyi alıp socket'e koyuyoruz)
        var cellObj = EnergyCellInventory.ConsumeCell();
        if (cellObj != null && cellSocket != null)
        {
            cellObj.SetActive(true);
            cellObj.transform.SetParent(cellSocket, false);
            cellObj.transform.localPosition = Vector3.zero;
            cellObj.transform.localRotation = Quaternion.identity;
            cellObj.transform.localScale = Vector3.one;
        }

        // install sesi
        PlayOneShot(sfxSource, installFuseClip);

        // alarm başlat
        float alarmLen = (alarmClip != null) ? alarmClip.length : 23f;
        PlayOneShot(sfxSource, alarmClip);

        // alarm başladıktan 1-2 sn sonra kapı + motor
        yield return new WaitForSeconds(machineStartDelay);

        // motor loop başlat
        StartMachineLoop();

        // kapı açılma: alarm bitene kadar tamamlanacak
        float doorDuration = Mathf.Max(0.1f, alarmLen - machineStartDelay);
        yield return MoveDoorsLocalZ(doorDuration);

        // kapı bitti -> motoru yavaşça kıs
        yield return FadeOutMachine(machineFadeOut);

        state = State.Done;
    }

    private void StartMachineLoop()
    {
        if (machineSource == null || machineBitsClip == null) return;
        machineSource.clip = machineBitsClip;
        machineSource.loop = true;
        machineSource.volume = 1f;
        if (!machineSource.isPlaying) machineSource.Play();
    }

    private IEnumerator FadeOutMachine(float fadeTime)
    {
        if (machineSource == null || !machineSource.isPlaying) yield break;

        float startVol = machineSource.volume;
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / fadeTime);
            machineSource.volume = Mathf.Lerp(startVol, 0f, n);
            yield return null;
        }
        machineSource.Stop();
        machineSource.volume = startVol; // bir sonraki kullanım için geri al
    }

    private IEnumerator MoveDoorsLocalZ(float duration)
    {
        Vector3 leftStart = doorLeft.localPosition;
        Vector3 rightStart = doorRight.localPosition;

        leftStart.z = doorLeftStartZ;
        rightStart.z = doorRightStartZ;
        doorLeft.localPosition = leftStart;
        doorRight.localPosition = rightStart;

        Vector3 leftEnd = new Vector3(leftStart.x, leftStart.y, doorLeftTargetZ);
        Vector3 rightEnd = new Vector3(rightStart.x, rightStart.y, doorRightTargetZ);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / duration);
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

        Vector3 startEuler = tr.localEulerAngles;
        startEuler.x = NormalizeAngle(fromX);
        tr.localEulerAngles = startEuler;

        Vector3 endEuler = startEuler;
        endEuler.x = NormalizeAngle(toX);

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

    private void PlayOneShot(AudioSource src, AudioClip clip)
    {
        if (src == null || clip == null) return;
        src.PlayOneShot(clip);
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
        a %= 360f;
        if (a < 0f) a += 360f;
        return a;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) playerInside = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) playerInside = false;
    }
}
