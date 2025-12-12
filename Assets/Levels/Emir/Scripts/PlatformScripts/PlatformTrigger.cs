using UnityEngine;

public class PlatformTrigger : MonoBehaviour
{
    public SlidePlatform platform; // 3. platform (SlidePlatform script’i olan)
    public float delay = 0.15f;    // Küçük bir gecikme (isteğe bağlı)

    bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        if (other.CompareTag("Player"))
        {
            triggered = true;
            StartCoroutine(StartPlatform());
        }
    }

    System.Collections.IEnumerator StartPlatform()
    {
        yield return new WaitForSeconds(delay);
        platform.Activate();
    }
}
