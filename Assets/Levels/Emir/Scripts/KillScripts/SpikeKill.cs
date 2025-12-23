using UnityEngine;

public class SpikeKill : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // 🔴 ZONE TRACKER BURADA ÇAĞRILMALI
        var tracker = other.GetComponentInParent<L1ZoneTracker>();
        if (tracker != null)
            tracker.OnPlayerKilled();

        var respawn = other.GetComponentInParent<PlayerRespawn>();
        if (respawn != null)
        {
            respawn.Kill();
            return;
        }

        var death = other.GetComponentInParent<PlayerDeath>();
        if (death != null)
            death.Die();
    }
}
