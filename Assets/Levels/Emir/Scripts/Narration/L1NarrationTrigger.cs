using UnityEngine;

public class L1NarrationTrigger : MonoBehaviour
{
    public enum Line
    {
        L1_01_Wakeup,
        L1_02_jump1,
        L1_03_Fake_Platform,
        L1_04_Fake_Platform_After_Death,
        L1_05_Fake_Parkour,
        L1_06_New_Parkour,
        L1_07_Starting_Spike_Walls,
        L1_08_Wrong_Run,
        L1_09_Fake_Spike_Wall_Wait,
        L1_10_Finish
    }

    public Line line;
    public bool playOnce = true;
    public float delay = 0f;

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (playOnce && triggered) return;
        triggered = true;

        var m = L1NarrationManager.I;
        if (m == null) return;

        AudioClip clip = GetClip(m, line);
        if (clip == null) return;

        // Artık overlap yok: manager queue yapıyor
        if (playOnce)
            m.Enqueue(line.ToString(), clip, delay, playOnceKey: true);
        else
            m.Enqueue(line.ToString() + "_" + GetInstanceID(), clip, delay, playOnceKey: false);
    }

    private AudioClip GetClip(L1NarrationManager m, Line l)
    {
        switch (l)
        {
            case Line.L1_01_Wakeup: return m.L1_01_Wakeup;
            case Line.L1_02_jump1: return m.L1_02_jump1;
            case Line.L1_03_Fake_Platform: return m.L1_03_Fake_Platform;
            case Line.L1_04_Fake_Platform_After_Death: return m.L1_04_Fake_Platform_After_Death;
            case Line.L1_05_Fake_Parkour: return m.L1_05_Fake_Parkour;
            case Line.L1_06_New_Parkour: return m.L1_06_New_Parkour;
            case Line.L1_07_Starting_Spike_Walls: return m.L1_07_Starting_Spike_Walls;
            case Line.L1_08_Wrong_Run: return m.L1_08_Wrong_Run;
            case Line.L1_09_Fake_Spike_Wall_Wait: return m.L1_09_Fake_Spike_Wall_Wait;
            case Line.L1_10_Finish: return m.L1_10_Finish;
            default: return null;
        }
    }
}
