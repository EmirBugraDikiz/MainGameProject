using UnityEngine;

public class L1ZoneTracker : MonoBehaviour
{
    public bool inFakePlatformZone = false;
    public bool inWrongRunZone = false;

    private bool playedFakePlatformDeath = false;
    private bool playedWrongRun = false;

    public void OnPlayerKilled()
    {
        var m = L1NarrationManager.I;
        if (m == null) return;

        if (inFakePlatformZone && !playedFakePlatformDeath)
        {
            playedFakePlatformDeath = true;
            m.Enqueue("L1_04_Fake_Platform_After_Death", m.L1_04_Fake_Platform_After_Death, 0f, playOnceKey: true);
        }

        if (inWrongRunZone && !playedWrongRun)
        {
            playedWrongRun = true;
            m.Enqueue("L1_08_Wrong_Run", m.L1_08_Wrong_Run, 0f, playOnceKey: true);
        }
    }
}
