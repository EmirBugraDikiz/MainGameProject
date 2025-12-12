using UnityEngine;

public class SpikeKill : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerDeath death = other.GetComponent<PlayerDeath>();
            if (death != null)
            {
                death.Die();
            }
        }
    }
}
