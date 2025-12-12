using UnityEngine;

public class ForwardTrigger : MonoBehaviour
{
    public SpikeCubeTrap trap;
    bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;
        trap.OnForwardTriggerEntered();
    }
}
