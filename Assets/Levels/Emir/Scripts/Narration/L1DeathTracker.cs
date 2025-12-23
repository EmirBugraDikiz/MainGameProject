using UnityEngine;

public class L1DeathTracker : MonoBehaviour
{
    public L1DeathReasonZone.Reason? currentReason = null;

    private PlayerRespawn respawn;

    private void Awake()
    {
        respawn = GetComponent<PlayerRespawn>();
    }

    // Bu fonksiyonu ölmeden hemen önce çağıracağız
    public void OnPlayerKilled()
    {
        var m = L1NarrationManager.I;
        if (m == null) return;

        if (currentReason == L1DeathReasonZone.Reason.FakePlatform)
        {
            m.PlayOnce("L1_04_Fake_Platform_After_Death", m.L1_04_Fake_Platform_After_Death, 0f);
        }
        else if (currentReason == L1DeathReasonZone.Reason.WrongRun)
        {
            m.PlayOnce("L1_08_Wrong_Run", m.L1_08_Wrong_Run, 0f);
        }
    }
}
