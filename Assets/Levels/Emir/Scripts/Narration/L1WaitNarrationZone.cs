using UnityEngine;

[RequireComponent(typeof(Collider))]
public class L1WaitNarrationZone : MonoBehaviour
{
    public float waitSeconds = 4f;

    [Tooltip("Bir kere çalsın mı?")]
    public bool playOnce = true;

    private bool inside = false;
    private float timer = 0f;
    private bool fired = false;

    private void Reset()
    {
        var c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        inside = true;
        timer = 0f;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        inside = false;
        timer = 0f;
    }

    private void Update()
    {
        if (!inside) return;

        if (playOnce && fired) return;

        timer += Time.deltaTime;
        if (timer >= waitSeconds)
        {
            fired = true;

            var m = L1NarrationManager.I;
            if (m == null) return;

            // queue: biri bitmeden öbürü başlamaz
            m.Enqueue("L1_09_Fake_Spike_Wall_Wait", m.L1_09_Fake_Spike_Wall_Wait, 0f, playOnceKey: true);
        }
    }
}
