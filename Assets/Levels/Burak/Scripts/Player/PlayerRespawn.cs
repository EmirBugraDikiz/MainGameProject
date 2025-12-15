using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerRespawn : MonoBehaviour
{
    [Header("Respawn")]
    [Tooltip("İlk spawn noktası yerine özel bir spawn kullanılacaksa buraya verirsin.")]
    public Transform overrideSpawnPoint;

    [Header("Effects")]
    [Tooltip("Ölme / ekran kararma vb. efektleri yöneten script.")]
    public PlayerDeathEffect deathEffect;

    [Header("Level Start Wake Up")]
    [Tooltip("Sahne ilk yüklendiğinde bir kez çalınacak uyanma (kabustan kalkma) sesi.")]
    public AudioSource levelStartWakeAudio;
    [Tooltip("Bu sahneye ilk girdiğinde uyanma sesi çalınsın mı?")]
    public bool playWakeAudioOnStart = true;

    private CharacterController controller;
    private PlayerAbilitiesController abilities;

    private Vector3 defaultSpawnPos;
    private Quaternion defaultSpawnRot;

    private Transform currentCheckpoint;

    private float defaultFixedDeltaTime;
    private bool hasPlayedLevelStartWake = false;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        abilities  = GetComponent<PlayerAbilitiesController>();

        defaultSpawnPos = transform.position;
        defaultSpawnRot = transform.rotation;

        defaultFixedDeltaTime = Time.fixedDeltaTime;
    }

    private void Start()
    {
        // Sahne ilk yüklendiğinde "kabustan uyanma" sesi
        if (playWakeAudioOnStart && !hasPlayedLevelStartWake)
        {
            hasPlayedLevelStartWake = true;

            if (levelStartWakeAudio != null)
            {
                levelStartWakeAudio.Play();
            }

            // Not: Göz açılma animasyonunu PlayerDeathEffect veya ayrı bir
            // UI fade scriptinden tetikleyebilirsin. Buraya bağımlı kılmadık ki
            // compile error çıkmasın.
        }
    }

    public void SetCheckpoint(Transform checkpoint)
    {
        currentCheckpoint = checkpoint;
    }

    // Kill sadece DeathEffect'e delege ediyor
    public void Kill()
    {
        if (deathEffect != null)
        {
            deathEffect.StartDeathSequence(this);
        }
        else
        {
            Respawn();
        }
    }

    // DeathEffect burayı çağıracak
    public void Respawn()
    {
        // Zamanı normale çek (Kronos açıksa vs.)
        Time.timeScale      = 1f;
        Time.fixedDeltaTime = defaultFixedDeltaTime;

        Vector3    targetPos = defaultSpawnPos;
        Quaternion targetRot = defaultSpawnRot;

        if (overrideSpawnPoint != null)
        {
            targetPos = overrideSpawnPoint.position;
            targetRot = overrideSpawnPoint.rotation;
        }

        if (currentCheckpoint != null)
        {
            targetPos = currentCheckpoint.position;
            targetRot = currentCheckpoint.rotation;
        }

        controller.enabled = false;
        transform.position = targetPos;
        transform.rotation = targetRot;
        controller.enabled = true;

        if (abilities != null)
        {
            abilities.ResetVelocity();
        }
    }
}
