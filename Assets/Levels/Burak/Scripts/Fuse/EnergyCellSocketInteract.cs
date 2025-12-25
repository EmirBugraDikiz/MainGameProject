using System.Collections;
using UnityEngine;

public class EnergyCellSocketInteract : MonoBehaviour
{
    public enum State { IdleStep1, AnimStep1, IdleStep2, Installing, DoorsOpening, Done }

    [Header("Refs")]
    [SerializeField] private Transform cup;                 // kapak / shield cover
    [SerializeField] private Transform cellSocket;          // CellSocket transform
    [SerializeField] private GameObject installedVisual;    // EnergyCell_InstalledVisual (başta kapalı)
    [SerializeField] private Transform doorLeft;
    [SerializeField] private Transform doorRight;

    [Header("Interact")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private Collider interactTrigger;      // trigger collider
    private bool playerInside;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;         // kapak, install, denied
    [SerializeField] private AudioSource alarmSource;       // Allarm_Sound
    [SerializeField] private AudioSource machineSource;     // Machine Bits (loop + fade)

    [Header("Audio Clips")]
    [SerializeField] private AudioClip coverClip;           // ilk E: kapak sesi
    [SerializeField] private AudioClip installClip;         // Install_Fuse.wav
    [SerializeField] private AudioClip accessDeniedClip;    // Access_Denied.wav
    [SerializeField] private AudioClip alarmClip;           // Allarm_Sound.wav
    [SerializeField] private AudioClip machineBitsClip;     // Machine Bits 5.wav

    [Header("Narrator VO")]
    [SerializeField] private AudioClip narratorNoCellClip;  // L4_08_Without_Fuse.wav
    [SerializeField] private bool narratorNoCellOnce = true;
    private bool narratorNoCellPlayed = false;

    [SerializeField] private AudioClip narratorDoorOpenClip; // L4_09_Door_Open.wav
    [SerializeField] private bool narratorDoorOpenOnce = true;
    private bool narratorDoorOpenPlayed = false;

    [Header("Step1 - Cup Local X")]
    [SerializeField] private float cupStartX = -90f;
    [SerializeField] private float cupTargetX = 45f;
    [SerializeField] private float cupDuration = 1.3f;
    [SerializeField] private AnimationCurve cupEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Doors Local Z (alarm süresine yayılacak)")]
    [SerializeField] private float doorLeftStartZ = 0f;
    [SerializeField] private float doorLeftTargetZ = 1.9f;
    [SerializeField] private float doorRightStartZ = -1.433132f;
    [SerializeField] private float doorRightTargetZ = -3.18f;

    [Tooltip("Alarm başladıktan kaç saniye sonra kapılar açılmaya başlasın")]
    [SerializeField] private float doorStartDelay = 1.5f;

    [Tooltip("Machine bits sesinin en sonda fade out süresi")]
    [SerializeField] private float machineFadeOut = 1.0f;

    private State state = State.IdleStep1;

    private void Awake()
    {
        if (interactTrigger != null) interactTrigger.isTrigger = true;
        if (installedVisual != null) installedVisual.SetActive(false);

        ApplyLocalX(cup, cupStartX);
        SetLocalZ(doorLeft, doorLeftStartZ);
        SetLocalZ(doorRight, doorRightStartZ);
    }

    private void Update()
    {
        if (!playerInside) return;
        if (Input.GetKeyDown(interactKey))
            TryInteract();
    }

    private void TryInteract()
    {
        if (state == State.IdleStep1) StartCoroutine(Step1_OpenCover());
        else if (state == State.IdleStep2) StartCoroutine(Step2_InstallAndOpenDoors());
    }

    private IEnumerator Step1_OpenCover()
    {
        state = State.AnimStep1;

        PlayOneShot(sfxSource, coverClip);
        yield return RotateLocalX(cup, cupStartX, cupTargetX, cupDuration, cupEase);

        state = State.IdleStep2;
    }

    private IEnumerator Step2_InstallAndOpenDoors()
    {
        // Cell yoksa deny + narrator (1 kere)
        if (!GateInventory.HasEnergyCell)
        {
            PlayOneShot(sfxSource, accessDeniedClip);

            if (narratorNoCellClip != null && (!narratorNoCellOnce || !narratorNoCellPlayed))
            {
                narratorNoCellPlayed = true;

                if (NarratorAudioQueue.Instance != null)
                    NarratorAudioQueue.Instance.Enqueue(narratorNoCellClip);
            }

            yield break;
        }

        state = State.Installing;

        // ✅ Narrator: kapı açılıyor / güç verildi (1 kere)
        if (narratorDoorOpenClip != null && (!narratorDoorOpenOnce || !narratorDoorOpenPlayed))
        {
            narratorDoorOpenPlayed = true;

            if (NarratorAudioQueue.Instance != null)
                NarratorAudioQueue.Instance.Enqueue(narratorDoorOpenClip);
        }

        // 1) takma sesi
        PlayOneShot(sfxSource, installClip);

        // 2) görseli aktif et + socket’a hizala
        if (installedVisual != null)
        {
            installedVisual.SetActive(true);
            installedVisual.transform.position = cellSocket.position;
            installedVisual.transform.rotation = cellSocket.rotation;
        }

        // inventory tüket
        GateInventory.HasEnergyCell = false;

        // 3) alarm başlat
        if (alarmSource != null && alarmClip != null)
        {
            alarmSource.clip = alarmClip;
            alarmSource.loop = false;
            alarmSource.Play();
        }

        // alarm süresi üzerinden kapı süresini hesapla
        float alarmLen = (alarmClip != null) ? alarmClip.length : 5f;

        // 4) kapıları biraz gecikmeli başlat + machine bits başlat
        yield return new WaitForSeconds(doorStartDelay);

        state = State.DoorsOpening;

        // machine bits başla (alarmla eş zamanlı devam)
        float machineBaseVol = 1f;
        if (machineSource != null && machineBitsClip != null)
        {
            machineBaseVol = machineSource.volume;
            machineSource.clip = machineBitsClip;
            machineSource.loop = true;
            machineSource.volume = machineBaseVol;
            machineSource.Play();
        }

        // kapılar alarm bitene kadar açılsın
        float doorsDuration = Mathf.Max(0.1f, alarmLen - doorStartDelay);
        yield return MoveDoorsLocalZ(doorsDuration);

        // kapı bittiğinde machine bits’i fade out
        if (machineSource != null && machineSource.isPlaying)
            yield return FadeOutAndStop(machineSource, machineFadeOut);

        state = State.Done;
    }

    private IEnumerator MoveDoorsLocalZ(float duration)
    {
        Vector3 leftStart = doorLeft.localPosition;
        Vector3 leftEnd = new Vector3(leftStart.x, leftStart.y, doorLeftTargetZ);

        Vector3 rightStart = doorRight.localPosition;
        Vector3 rightEnd = new Vector3(rightStart.x, rightStart.y, doorRightTargetZ);

        leftStart.z = doorLeftStartZ;
        rightStart.z = doorRightStartZ;

        doorLeft.localPosition = leftStart;
        doorRight.localPosition = rightStart;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / duration);

            doorLeft.localPosition = Vector3.Lerp(leftStart, leftEnd, n);
            doorRight.localPosition = Vector3.Lerp(rightStart, rightEnd, n);

            yield return null;
        }

        doorLeft.localPosition = leftEnd;
        doorRight.localPosition = rightEnd;
    }

    private IEnumerator FadeOutAndStop(AudioSource src, float fadeTime)
    {
        float startVol = src.volume;
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            src.volume = Mathf.Lerp(startVol, 0f, t / fadeTime);
            yield return null;
        }
        src.Stop();
        src.volume = startVol;
    }

    private IEnumerator RotateLocalX(Transform tr, float fromX, float toX, float duration, AnimationCurve curve)
    {
        if (tr == null) yield break;

        Vector3 startEuler = tr.localEulerAngles;
        Vector3 endEuler = startEuler;

        startEuler.x = NormalizeAngle(fromX);
        tr.localEulerAngles = startEuler;

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

    private void PlayOneShot(AudioSource src, AudioClip clip)
    {
        if (src == null || clip == null) return;
        src.PlayOneShot(clip);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = false;
    }
}
