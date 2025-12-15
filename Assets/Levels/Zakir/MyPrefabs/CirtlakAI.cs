using UnityEngine;
using UnityEngine.AI; // NavMesh icin gerekli kutuphane
using UnityEngine.SceneManagement; // Sahne yönetimi için þart

public class CirtlakAI : MonoBehaviour
{
    // Robotun ruh halleri (State Machine)
    public enum State
    {
        Patrol, // Devriye (Oyuncuyu ariyor)
        Chase   // Kovalama (Saldiri!)
    }

    [Header("Ayarlar")]
    public State currentState; // Þu an ne yapýyor?
    public float patrolSpeed = 3.0f; // Gezme hýzý
    public float chaseSpeed = 6.5f;  // Kovalama hýzý (Oyuncudan hýzlý olsun!)
    public float detectionRange = 12.0f; // Görme mesafesi
    public float patrolRadius = 20.0f;   // Ne kadar uzaða devriye atsýn?

    [Header("Jump Scare Ayarlarý")]
    public GameObject jumpScareImage; // Ekrana çýkacak resim objesi
    public AudioClip screamSound;     // Çýðlýk sesi
    private AudioSource _audioSource; // Sesi çalacak hoparlör
    private bool _isCaught = false;   // Yakalandýk mý? (Kodu bir kere çalýþtýrmak için)

    [Header("Görsel")]
    public Transform sawBlade; // Testere objesi
    public float sawRotationSpeed = 800f; // Testere ne kadar hýzlý dönsün?

    // Bilesenler
    private NavMeshAgent _agent;
    private Transform _player;

    private void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _audioSource = GetComponent<AudioSource>(); // Hoparlörü bul

        // Oyuncuyu bul (Tag'i Player olan objeyi arar)
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _player = playerObj.transform;
        }
        else
        {
            Debug.LogError("HATA: Sahnede 'Player' etiketli karakter yok!");
        }

        // Oyuna devriye moduyla baþla
        currentState = State.Patrol;
        GoToRandomPoint();
    }

    private void Update()
    {
        if (_player == null) return;

        // Spastik testere dönüþü
        //// 1. Testereyi sürekli döndür (Viiiiiiinnn!)
        //if (sawBlade != null)
        //    sawBlade.Rotate(Vector3.right * sawRotationSpeed * Time.deltaTime);

        // 2. Mesafeyi ölç
        float distanceToPlayer = Vector3.Distance(transform.position, _player.position);

        // 3. Duruma göre davran
        switch (currentState)
        {
            case State.Patrol:
                PatrolLogic(distanceToPlayer);
                break;

            case State.Chase:
                ChaseLogic(distanceToPlayer);
                break;
        }
    }



    // --- DEVRÝYE MANTIÐI ---
    void PatrolLogic(float distance)
    {
        _agent.speed = patrolSpeed; // Hýzý düþür

        // Oyuncuyu gördü mü? (Menzile girdi mi?)
        if (distance < detectionRange)
        {
            currentState = State.Chase; // MODU DEÐÝÞTÝR
            Debug.Log("Cýrtlak: SENÝ GÖRDÜM!");
            return;
        }

        // Gittiði noktaya vardý mý?
        if (!_agent.pathPending && _agent.remainingDistance < 0.5f)
        {
            GoToRandomPoint(); // Yeni nokta seç
        }
    }

    // --- KOVALAMA MANTIÐI ---
    void ChaseLogic(float distance)
    {
        _agent.speed = chaseSpeed;
        _agent.SetDestination(_player.position);

        // Uzaklaþýrsa vazgeç
        if (distance > detectionRange * 1.5f) 
        {
            currentState = State.Patrol;
            GoToRandomPoint();
        }

        // ---> YENÝ KISIM: YAKALANMA <---
        // Oyuncuya çok yaklaþtýysa (Testere mesafesi)
        //if (distance < 2.0f) 
        //{
        //    Debug.Log("YAKALANDIN! Sahne Yeniden Yükleniyor...");

        //    // Sahneyi baþtan baþlat (Restart)
        //    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        //}

        //if (distance < 2.0f)
        //{
        //    Debug.Log("YAKALANDIN! Sahne Yeniden Yükleniyor...");

        //    // Sahneyi baþtan baþlat (Restart)
        //    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        //}
        // ---> YENÝ: Sadece bu olmalý <---
        if (distance < 2.0f && !_isCaught)
        {
            _isCaught = true;
            StartCoroutine(TriggerJumpScareSequence());
        }

    }

    // Haritada rastgele nokta bulma fonksiyonu
    void GoToRandomPoint()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += transform.position;

        NavMeshHit hit;
        // Mavi alandaki en yakýn geçerli noktayý bul
        NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, 1);

        _agent.SetDestination(hit.position);
    }

    // Editörde kýrmýzý görme alanýný çiz
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }

    System.Collections.IEnumerator TriggerJumpScareSequence()
    {
        Debug.Log("JUMP SCARE BAÞLADI!");

        // 1. Robotu Durdur (Ýçimizden geçmesin)
        _agent.isStopped = true;
        _agent.velocity = Vector3.zero;

        // 2. Korkunç Resmi Aç
        if (jumpScareImage != null)
            jumpScareImage.SetActive(true);

        // 3. Çýðlýk Sesini Çal
        if (screamSound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(screamSound);
        }

        // 4. Oyuncuyu Dondur (Opsiyonel: Kaçamasýn)
        Time.timeScale = 0f; // Zamaný durdur

        // 5. Bekle (Gerçek zamanlý bekleme - WaitForSecondsRealtime kullanmalýyýz çünkü zamaný durdurduk)
        yield return new WaitForSecondsRealtime(2.0f); // 2 saniye çýðlýðý dinle

        // 6. Zamaný Düzelt ve Sahneyi Yenile
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

}