using UnityEngine;

public class SetFakeFinishTried : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        Level2NarratorFlags.FakeFinishTried = true;
    }
}
