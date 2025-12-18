using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;             // Post-Processing Ana Kütüphanesi
using UnityEngine.Rendering.Universal;   // URP kullanýyorsan bu gereklidir (Built-in ise hata verirse sil)

public class CreatureAI : MonoBehaviour
{
    // Durum Makinesi
    public enum State { Patrol, Roaring, Chase, Attack }
    public State currentState;

    [Header("Hýz ve Süre Ayarlarý")]
    public float patrolSpeed = 2.0f;
    public float chaseSpeed = 6.0f;
    public float roarDuration = 2.6f;

    [Header("Adým Sesleri")]
    public AudioClip stepSound;    // Yürüme sesi buraya
    public float walkStepInterval = 0.6f; // Yürürken kaç saniyede bir ses çýksýn?
    public float runStepInterval = 0.35f; // Koþarken kaç saniyede bir ses çýksýn?
    private float _nextStepTime;

    [Header("Görüþ Ayarlarý")]
    public float viewRadius = 15f;
    [Range(0, 360)]
    public float viewAngle = 110f;
    public LayerMask obstacleMask;

    [Header("Sinematik ve Efektler")]
    public Transform headBone;
    public AudioClip roarSound;
    public AudioClip attackSound;
    public Volume horrorVolume;   // Post-Processing Volume

    private NavMeshAgent _agent;
    private Animator _anim;
    private Transform _player;
    private AudioSource _audio;
    private Camera _mainCam;

    // Post-Process Kontrolü için deðiþkenler
    private DepthOfField _dofComponent;
    private Vignette _vignetteComponent;

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _anim = GetComponent<Animator>();
        _audio = GetComponent<AudioSource>();
        _mainCam = Camera.main;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) _player = playerObj.transform;

        if (obstacleMask == 0) obstacleMask = LayerMask.GetMask("Default");

        // --- Volume Bileþenlerini Bulma ---
        if (horrorVolume != null && horrorVolume.profile != null)
        {
            // Depth of Field ve Vignette ayarlarýný çekiyoruz
            horrorVolume.profile.TryGet(out _dofComponent);
            horrorVolume.profile.TryGet(out _vignetteComponent);

            // Baþlangýçta Vignette rengini Koyu Kýrmýzýya zorla (Garanti olsun)
            if (_vignetteComponent != null)
            {
                _vignetteComponent.color.Override(new Color(0.3f, 0f, 0f)); // Koyu Kan Kýrmýzýsý
            }
        }

        //// DEGISTIRILEBILIR KISIM
        //// Scriptin Start() fonksiyonunun içi:

        //if (_vignetteComponent != null)
        //{
        //    // Rengi Koyu Kýrmýzý Yap
        //    _vignetteComponent.color.Override(new Color(0.5f, 0f, 0f));

        //    // ÞÝDDETÝ KODLA ZORLA (Bunu ekle!)
        //    _vignetteComponent.intensity.Override(0.8f); // 0.8 ile 1.0 arasý çok koyu yapar
        //}

        currentState = State.Patrol;
        GoToRandomPoint();
    }

    void Update()
    {
        if (_player == null) return;

        // NavMesh hýzý ile Animasyon hýzý senkronizasyonu
        _anim.SetFloat("Speed", _agent.velocity.magnitude);

        // --- ADIM SESÝ KONTROLÜ ---
        HandleFootsteps();

        // --- KAMERA KÝLÝDÝ (Saldýrý Aný) ---
        if (currentState == State.Attack && _mainCam != null)
        {
            Transform target = headBone != null ? headBone : transform;
            _mainCam.transform.LookAt(target);

            // BLUR DÜZELTME: Odak noktasýný sürekli canavarýn mesafesine ayarla
            if (_dofComponent != null)
            {
                float distanceToCreature = Vector3.Distance(_mainCam.transform.position, target.position);
                _dofComponent.focusDistance.value = distanceToCreature;
            }
        }

        switch (currentState)
        {
            case State.Patrol: PatrolLogic(); break;
            case State.Chase: ChaseLogic(); break;
        }
    }

    void HandleFootsteps()
    {
        // HATA DÜZELTME: Sadece hýz yetmez, diðer durumlarý da kontrol etmeliyiz.

        // 1. NavMesh aktif mi ve hareket ediyor mu?
        if (!_agent.isOnNavMesh || !_agent.enabled) return;

        // 2. Canavarýn hýzý 0.5'ten büyük mü? (Hareket ediyor mu?)
        bool isMoving = _agent.velocity.magnitude > 0.5f;

        // 3. Hedefe henüz varmadý mý? (Vardýysa ses çalmasýn)
        bool isNotAtDestination = _agent.remainingDistance > _agent.stoppingDistance;

        // 4. NavMesh durdurulmuþ mu? (Roar veya Attack durumunda durur)
        bool isAgentActive = !_agent.isStopped;

        // TÜM ÞARTLAR SAÐLANIYORSA SES ÇAL
        if (isMoving && isNotAtDestination && isAgentActive && currentState != State.Roaring && currentState != State.Attack)
        {
            if (Time.time >= _nextStepTime)
            {
                // Hangi aralýkla çalacaðýz? (Koþuyorsa sýk, yürüyorsa seyrek)
                float interval = (currentState == State.Chase) ? runStepInterval : walkStepInterval;

                // Sesi incelt/kalýnlaþtýr
                _audio.pitch = (currentState == State.Chase) ? 1.3f : 0.9f;

                if (stepSound != null) _audio.PlayOneShot(stepSound, 0.6f);

                _nextStepTime = Time.time + interval;
            }
        }
    }

    // --- DEVRÝYE ---
    void PatrolLogic()
    {
        _agent.speed = patrolSpeed;
        if (CanSeePlayer()) StartCoroutine(RoarRoutine());
        if (!_agent.pathPending && _agent.remainingDistance < 0.5f) GoToRandomPoint();
    }

    // --- KÜKREME ---
    System.Collections.IEnumerator RoarRoutine()
    {
        currentState = State.Roaring;
        _agent.isStopped = true;
        _agent.velocity = Vector3.zero;

        _audio.pitch = 1.0f; // Sesi normale döndür (Adým sesi bozmasýn)
        _anim.SetTrigger("Roar");
        if (roarSound != null) _audio.PlayOneShot(roarSound);

        yield return new WaitForSeconds(roarDuration);

        currentState = State.Chase;
        _agent.isStopped = false;
    }

    // --- KOVALAMA ---
    void ChaseLogic()
    {
        _agent.speed = chaseSpeed;
        if (_agent.isOnNavMesh) _agent.SetDestination(_player.position);

        if (Vector3.Distance(transform.position, _player.position) > viewRadius * 1.5f)
        {
            currentState = State.Patrol;
            GoToRandomPoint();
        }

        if (Vector3.Distance(transform.position, _player.position) < 2.0f)
        {
            StartCoroutine(AttackRoutine());
        }
    }

    // --- SALDIRI & SÝNEMATÝK ---
    System.Collections.IEnumerator AttackRoutine()
    {
        Debug.Log("SALDIRI BAÞLADI! KOD BURAYA GÝRDÝ."); // <--- Bunu ekle

        currentState = State.Attack;
        _agent.isStopped = true;
        _agent.velocity = Vector3.zero;

        if (horrorVolume != null)
        {
            Debug.Log("VOLUME BULUNDU, AÐIRLIK 1 YAPILIYOR."); // <--- Bunu ekle
            horrorVolume.weight = 1f;
        }
        else
        {
            Debug.Log("DÝKKAT: Horror Volume BOÞ GÖRÜNÜYOR!"); // <--- Bunu ekle
        }

        // 1. Zoom Yap
        if (_mainCam != null) _mainCam.fieldOfView = 30f;

        // 2. Korku Efektini Aç (Blur + Kýrmýzý Vignette)
        if (horrorVolume != null) horrorVolume.weight = 1f;

        // 3. Ses ve Animasyon
        _audio.pitch = 1.0f; // Sesi normale döndür
        _anim.SetTrigger("Attack");
        if (attackSound != null) _audio.PlayOneShot(attackSound);

        yield return new WaitForSeconds(2.5f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // --- GÖRÜÞ ---
    bool CanSeePlayer()
    {
        if (_player == null) return false;
        Vector3 eyePos = transform.position + Vector3.up * 1.6f;
        Vector3 playerEyePos = _player.position + Vector3.up * 1.6f;
        Vector3 dirToPlayer = (playerEyePos - eyePos).normalized;
        float dstToPlayer = Vector3.Distance(eyePos, playerEyePos);

        if (dstToPlayer < viewRadius)
        {
            if (Vector3.Angle(transform.forward, dirToPlayer) < viewAngle / 2)
            {
                if (!Physics.Raycast(eyePos, dirToPlayer, dstToPlayer, obstacleMask)) return true;
            }
        }
        return false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, viewRadius);
    }

    void GoToRandomPoint()
    {
        Vector3 randomDirection = Random.insideUnitSphere * 20f;
        randomDirection += transform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, 20f, 1)) _agent.SetDestination(hit.position);
    }
}
