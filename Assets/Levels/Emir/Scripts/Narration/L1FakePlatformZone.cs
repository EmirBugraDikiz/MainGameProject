using UnityEngine;

[RequireComponent(typeof(Collider))]
public class L1FakePlatformZone : MonoBehaviour
{
    private void Reset()
    {
        var c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var tracker = other.GetComponentInParent<L1ZoneTracker>();
        if (tracker != null) tracker.inFakePlatformZone = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var tracker = other.GetComponentInParent<L1ZoneTracker>();
        if (tracker != null) tracker.inFakePlatformZone = false;
    }
}
