using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AudioSource))]
public class CreatureAI : MonoBehaviour
{
    public enum State { Patrol, Roaring, Chase, Search, Attack }
    public State currentState;

    [Header("Target")]
    public string playerTag = "Player";

    [Header("Speed")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 6f;
    public float searchSpeed = 3.5f;

    [Header("Vision")]
    public float viewRadius = 15f;
    [Range(0, 360)] public float viewAngle = 110f;

    [Tooltip("Inspector'da bunu EVERYTHING yap. Kod player layer'ını otomatik çıkaracak.")]
    public LayerMask obstacleMask = ~0; // default: Everything

    public float eyeHeight = 1.6f;

    [Header("Memory & Search")]
    public float memoryDuration = 5f;
    public float searchDuration = 8f;
    public float searchRadius = 6f;
    public float giveUpDistance = 40f;

    [Header("Combat")]
    public float attackDistance = 2f;
    public float roarDuration = 2.6f;

    [Header("Footsteps")]
    public AudioClip stepSound;
    public float walkStepInterval = 0.6f;
    public float runStepInterval = 0.35f;

    [Header("Cinematic")]
    public Transform headBone;
    public AudioClip roarSound;
    public AudioClip attackSound;
    public Volume horrorVolume;

    private NavMeshAgent _agent;
    private Animator _anim;
    private AudioSource _audio;
    private Camera _mainCam;
    private Transform _player;

    private float _nextStepTime;
    private bool _isAttacking;

    private Vector3 _lastSeenPos;
    private float _lastSeenTime = -999f;
    private float _searchEndTime = -999f;

    private DepthOfField _dof;
    private Vignette _vignette;

    private Behaviour _playerAbilities;
    private CharacterController _playerCC;

    private int _playerLayer = -1;

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _anim = GetComponent<Animator>();
        _audio = GetComponent<AudioSource>();
        _mainCam = Camera.main;

        var pObj = GameObject.FindGameObjectWithTag(playerTag);
        if (pObj != null) _player = pObj.transform;

        if (_player != null)
        {
            _playerLayer = _player.gameObject.layer;

            // Player layer'ını maskten çıkar (en kritik fix)
            obstacleMask &= ~(1 << _playerLayer);

            Component c = _player.GetComponent("PlayerAbilitiesController");
            if (c is Behaviour b) _playerAbilities = b;

            _playerCC = _player.GetComponent<CharacterController>();
        }

        if (horrorVolume != null && horrorVolume.profile != null)
        {
            horrorVolume.profile.TryGet(out _dof);
            horrorVolume.profile.TryGet(out _vignette);
            horrorVolume.weight = 0f;

            if (_vignette != null)
                _vignette.color.Override(new Color(0.4f, 0f, 0f));
        }

        currentState = State.Patrol;
        GoToRandomPoint();
    }

    void Update()
    {
        if (_player == null) return;

        _anim.SetFloat("Speed", _agent.velocity.magnitude);
        HandleFootsteps();

        if (currentState == State.Attack && _mainCam != null)
        {
            Transform target = headBone ? headBone : transform;
            _mainCam.transform.LookAt(target);

            if (_dof != null)
            {
                float d = Vector3.Distance(_mainCam.transform.position, target.position);
                _dof.focusDistance.value = d;
            }
        }

        switch (currentState)
        {
            case State.Patrol: PatrolLogic(); break;
            case State.Chase:  ChaseLogic(); break;
            case State.Search: SearchLogic(); break;
        }
    }

    void PatrolLogic()
    {
        _agent.speed = patrolSpeed;

        if (CanSeePlayer(out var seenPos))
        {
            _lastSeenPos = seenPos;
            _lastSeenTime = Time.time;
            StartCoroutine(RoarRoutine());
            return;
        }

        if (!_agent.pathPending && _agent.remainingDistance <= 0.5f)
            GoToRandomPoint();
    }

    System.Collections.IEnumerator RoarRoutine()
    {
        currentState = State.Roaring;
        _agent.isStopped = true;
        _agent.velocity = Vector3.zero;

        _anim.SetTrigger("Roar");
        if (roarSound) _audio.PlayOneShot(roarSound);

        yield return new WaitForSeconds(roarDuration);

        currentState = State.Chase;
        _agent.isStopped = false;
    }

    void ChaseLogic()
    {
        _agent.speed = chaseSpeed;

        if (Vector3.Distance(transform.position, _player.position) > giveUpDistance)
        {
            currentState = State.Patrol;
            GoToRandomPoint();
            return;
        }

        if (CanSeePlayer(out var seenPos))
        {
            _lastSeenPos = seenPos;
            _lastSeenTime = Time.time;

            if (_agent.isOnNavMesh)
                _agent.SetDestination(_player.position);
        }
        else
        {
            if (_agent.isOnNavMesh)
                _agent.SetDestination(_lastSeenPos);

            if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + 0.2f)
            {
                currentState = State.Search;
                _searchEndTime = Time.time + searchDuration;
                PickNewSearchPoint();
                return;
            }

            if (Time.time - _lastSeenTime > memoryDuration)
            {
                currentState = State.Search;
                _searchEndTime = Time.time + searchDuration;
                PickNewSearchPoint();
                return;
            }
        }

        if (!_isAttacking && Vector3.Distance(transform.position, _player.position) <= attackDistance)
            StartCoroutine(AttackRoutine());
    }

    void SearchLogic()
    {
        _agent.speed = searchSpeed;

        if (CanSeePlayer(out var seenPos))
        {
            _lastSeenPos = seenPos;
            _lastSeenTime = Time.time;
            currentState = State.Chase;
            return;
        }

        if (Time.time >= _searchEndTime)
        {
            currentState = State.Patrol;
            GoToRandomPoint();
            return;
        }

        if (!_agent.pathPending && _agent.remainingDistance <= 0.6f)
            PickNewSearchPoint();
    }

    void PickNewSearchPoint()
    {
        Vector3 point = GetRandomNavmeshPointNear(_lastSeenPos, searchRadius);
        if (_agent.isOnNavMesh) _agent.SetDestination(point);
    }

    System.Collections.IEnumerator AttackRoutine()
    {
        if (_isAttacking) yield break;
        _isAttacking = true;

        currentState = State.Attack;
        _agent.isStopped = true;
        _agent.velocity = Vector3.zero;

        if (_playerAbilities != null) _playerAbilities.enabled = false;
        if (_playerCC != null) _playerCC.enabled = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (horrorVolume != null) horrorVolume.weight = 1f;
        if (_mainCam != null) _mainCam.fieldOfView = 30f;

        _anim.SetTrigger("Attack");
        if (attackSound) _audio.PlayOneShot(attackSound);

        yield return new WaitForSeconds(2.5f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // =========================
    // VISION: "duvardan görme" kill switch
    // =========================
    bool CanSeePlayer(out Vector3 seenPos)
    {
        seenPos = Vector3.zero;
        if (_player == null) return false;

        Vector3 eye = transform.position + Vector3.up * eyeHeight;
        Vector3 target = _player.position + Vector3.up * 1.3f; // player aim height

        Vector3 dir = target - eye;
        float dist = dir.magnitude;

        if (dist > viewRadius) return false;
        dir /= dist;

        if (Vector3.Angle(transform.forward, dir) > viewAngle * 0.5f)
            return false;

        // RaycastAll: arada bir şey var mı? (Player layer zaten maskten çıkarıldı)
        RaycastHit[] hits = Physics.RaycastAll(
            eye, dir, dist, obstacleMask, QueryTriggerInteraction.Ignore);

        if (hits != null && hits.Length > 0)
        {
            // herhangi bir hit varsa: arada engel var demek -> GÖREMEZ
            // (çünkü player layer maskte yok, player hit'e girmeyecek)
            return false;
        }

        seenPos = _player.position;
        return true;
    }

    void HandleFootsteps()
    {
        if (!_agent.enabled || !_agent.isOnNavMesh) return;
        if (_agent.isStopped) return;

        bool moving = _agent.velocity.magnitude > 0.5f;
        if (!moving) return;

        if (Time.time >= _nextStepTime)
        {
            float interval = (currentState == State.Chase) ? runStepInterval : walkStepInterval;
            _audio.pitch = (currentState == State.Chase) ? 1.3f : 0.95f;

            if (stepSound) _audio.PlayOneShot(stepSound, 0.6f);
            _nextStepTime = Time.time + interval;
        }
    }

    void GoToRandomPoint()
    {
        Vector3 rnd = Random.insideUnitSphere * 20f + transform.position;
        if (NavMesh.SamplePosition(rnd, out NavMeshHit hit, 20f, NavMesh.AllAreas))
            _agent.SetDestination(hit.position);
    }

    Vector3 GetRandomNavmeshPointNear(Vector3 center, float radius)
    {
        Vector3 rnd = Random.insideUnitSphere * radius + center;
        if (NavMesh.SamplePosition(rnd, out NavMeshHit hit, radius, NavMesh.AllAreas))
            return hit.position;
        return center;
    }
}
