using UnityEngine;

public class SpikeCubeTrap : MonoBehaviour
{
    [Header("Pozisyonlar")]
    public Transform backPos;        // Küpün başlangıç (geride) pozisyonu
    public Transform frontPos;       // Küpün ileri fırladığı pozisyon (koridor tarafı)

    [Header("Zamanlar")]
    public float attackTime = 0.15f;   // İleri fırlama süresi (çok hızlı)
    public float waitAtFront = 0.3f;   // En önde bekleme süresi
    public float returnTime = 1.5f;    // Geri dönüş süresi (yavaş)
    public float autoAttackDelay = 2f; // Oyuncu zemine düştükten kaç sn sonra otomatik saldırı

    private enum State { IdleAtBack, AttackingForward, WaitingFront, ReturningBack }
    private State state = State.IdleAtBack;

    bool playerLanded = false;
    bool hasAttackedSinceLanding = false;
    float sinceLanding = 0f;
    float t = 0f;

    void Start()
    {
        if (backPos != null)
            transform.position = backPos.position;
    }

    void Update()
    {
        // Oyuncu zemine düştü, küp geride ve daha saldırmadıysa → süre say
        if (playerLanded && !hasAttackedSinceLanding && state == State.IdleAtBack)
        {
            sinceLanding += Time.deltaTime;
            if (sinceLanding >= autoAttackDelay)
            {
                StartAttack();
            }
        }

        switch (state)
        {
            case State.AttackingForward:
                t += Time.deltaTime / attackTime;
                transform.position = Vector3.Lerp(backPos.position, frontPos.position, t);

                if (t >= 1f)
                {
                    state = State.WaitingFront;
                    t = 0f;
                }
                break;

            case State.WaitingFront:
                t += Time.deltaTime;
                if (t >= waitAtFront)
                {
                    state = State.ReturningBack;
                    t = 0f;
                }
                break;

            case State.ReturningBack:
                t += Time.deltaTime / returnTime;
                transform.position = Vector3.Lerp(frontPos.position, backPos.position, t);

                if (t >= 1f)
                {
                    state = State.IdleAtBack;
                    t = 0f;
                    // İstersen burada tekrar oyuncu landing yaparsa yeni cycle başlatılabilir
                }
                break;
        }
    }

    // Oyuncu zemine düştüğünde çağrılacak
    public void OnPlayerLanded()
    {
        playerLanded = true;
        hasAttackedSinceLanding = false;
        sinceLanding = 0f;
    }

    // Oyuncu ileri trigger'dan geçtiğinde çağrılacak
    public void OnForwardTriggerEntered()
    {
        // Hala saldırmadıysa ve küp geride bekliyorsa → direkt saldır
        if (!hasAttackedSinceLanding && state == State.IdleAtBack)
        {
            StartAttack();
        }
    }

    void StartAttack()
    {
        hasAttackedSinceLanding = true;
        state = State.AttackingForward;
        t = 0f;
    }
}
