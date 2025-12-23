using UnityEngine;

public class L1DeathReasonZone : MonoBehaviour
{
    public enum Reason { FakePlatform, WrongRun }
    public Reason reason;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var tracker = other.GetComponentInParent<L1DeathTracker>();
        if (tracker != null) tracker.currentReason = reason;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var tracker = other.GetComponentInParent<L1DeathTracker>();
        if (tracker != null && tracker.currentReason == reason)
            tracker.currentReason = null;
    }
}
