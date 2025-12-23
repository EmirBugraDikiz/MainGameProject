using UnityEngine;

public class SpikeCubeTrap : MonoBehaviour
{
    [Header("Pozisyonlar")]
    public Transform backPos;
    public Transform frontPos;

    [Header("Zamanlar")]
    public float attackTime = 0.15f;
    public float waitAtFront = 0.3f;
    public float returnTime = 1.5f;
    public float autoAttackDelay = 2f;

    [Header("SFX")]
    [Tooltip("Boş bırakırsan otomatik AudioSource ekler ve 3D yapar.")]
    public AudioSource sfxSource;
    public AudioClip bladeCutClip;     // Blade_Cut.ogg
    public AudioClip chainWrapClip;    // Chain_Wrap.wav
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("SFX 3D Range")]
    [Tooltip("Custom rolloff + max distance ayarları. (İstiyorsan inspector'dan değiştirirsin)")]
    public float sfxMaxDistance = 55f;
    public float sfxMinDistance = 1.5f;

    private enum State { IdleAtBack, AttackingForward, WaitingFront, ReturningBack }
    [SerializeField] private State state = State.IdleAtBack;

    private State lastState = (State)(-1);

    private bool playerLanded = false;
    private bool hasAttackedSinceLanding = false;
    private float sinceLanding = 0f;
    private float t = 0f;

    void Start()
    {
        EnsureAudioSource();

        if (backPos != null)
            transform.position = backPos.position;

        // ilk state senkron
        HandleStateChange(force: true);
    }

    void Update()
    {
        if (backPos == null || frontPos == null) return;

        // landing sonrası auto attack timer
        if (playerLanded && !hasAttackedSinceLanding && state == State.IdleAtBack)
        {
            sinceLanding += Time.deltaTime;
            if (sinceLanding >= autoAttackDelay)
                StartAttack();
        }

        switch (state)
        {
            case State.AttackingForward:
                t += Time.deltaTime / Mathf.Max(0.0001f, attackTime);
                transform.position = Vector3.Lerp(backPos.position, frontPos.position, t);

                if (t >= 1f)
                {
                    state = State.WaitingFront;
                    t = 0f;
                    HandleStateChange();
                }
                break;

            case State.WaitingFront:
                t += Time.deltaTime;
                if (t >= waitAtFront)
                {
                    state = State.ReturningBack;
                    t = 0f;
                    HandleStateChange();
                }
                break;

            case State.ReturningBack:
                t += Time.deltaTime / Mathf.Max(0.0001f, returnTime);
                transform.position = Vector3.Lerp(frontPos.position, backPos.position, t);

                if (t >= 1f)
                {
                    state = State.IdleAtBack;
                    t = 0f;
                    HandleStateChange();
                }
                break;
        }
    }

    public void OnPlayerLanded()
    {
        playerLanded = true;
        hasAttackedSinceLanding = false;
        sinceLanding = 0f;
    }

    public void OnForwardTriggerEntered()
    {
        if (!hasAttackedSinceLanding && state == State.IdleAtBack)
            StartAttack();
    }

    private void StartAttack()
    {
        hasAttackedSinceLanding = true;
        state = State.AttackingForward;
        t = 0f;
        HandleStateChange();
    }

    private void EnsureAudioSource()
    {
        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
            if (sfxSource == null)
                sfxSource = gameObject.AddComponent<AudioSource>();
        }

        // --- 3D + Range ---
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 1f; // %100 3D

        // --- istediğin ayar: Custom Rolloff + Max Distance 55 ---
        sfxSource.rolloffMode = AudioRolloffMode.Custom;
        sfxSource.maxDistance = sfxMaxDistance;   // default 55
        sfxSource.minDistance = sfxMinDistance;   // default 1.5

        // Bonus: daha stabil
        sfxSource.dopplerLevel = 0f;
    }

    private void HandleStateChange(bool force = false)
    {
        if (!force && state == lastState) return;
        lastState = state;

        EnsureAudioSource();

        // - BladeCut: AttackingForward'a girerken bir kere
        // - ChainWrap: ReturningBack boyunca loop, IdleAtBack olunca dur

        if (state == State.AttackingForward)
        {
            // attack başlarken zincir loop varsa kes
            StopChainLoop();

            if (bladeCutClip != null)
                sfxSource.PlayOneShot(bladeCutClip, sfxVolume);
        }
        else if (state == State.ReturningBack)
        {
            StartChainLoop();
        }
        else if (state == State.IdleAtBack)
        {
            StopChainLoop();
        }
        // WaitingFront: hiçbir şey yapma
    }

    private void StartChainLoop()
    {
        if (chainWrapClip == null) return;

        // Zaten aynı clip loop çalıyorsa tekrar başlatma
        if (sfxSource.isPlaying && sfxSource.loop && sfxSource.clip == chainWrapClip) return;

        sfxSource.Stop();
        sfxSource.clip = chainWrapClip;
        sfxSource.loop = true;
        sfxSource.volume = sfxVolume;
        sfxSource.Play();
    }

    private void StopChainLoop()
    {
        if (sfxSource == null) return;

        if (sfxSource.loop && sfxSource.clip == chainWrapClip)
        {
            sfxSource.Stop();
            sfxSource.loop = false;
            sfxSource.clip = null;
        }
    }
}
