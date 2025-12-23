using UnityEngine;

public class SpikeKill : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Player'ın ana objesinde PlayerRespawn vardır, collider child olabilir diye parent'tan da ara
        var respawn = other.GetComponentInParent<PlayerRespawn>();
        if (respawn != null)
        {
            respawn.Kill();
            return;
        }

        // fallback (istersen kalsın)
        var death = other.GetComponentInParent<PlayerDeath>();
        if (death != null)
            death.Die();
    }
}
