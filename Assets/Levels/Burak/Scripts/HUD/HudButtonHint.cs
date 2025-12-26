using UnityEngine;

public class HudButtonHint : MonoBehaviour
{
    [Header("HUD")]
    public TutorialHintUI ui;

    [TextArea]
    public string message = "Butonu kullanmak için yaklaşıp [E] tuşuna bas.";

    [Header("Trigger")]
    public string requiredTag = "Player";

    [Header("Once Only")]
    public bool playOnce = true;
    public string uniqueKey = "L4_ButtonHint";

    private bool played;

    void Awake()
    {
        if (playOnce)
            played = PlayerPrefs.GetInt(uniqueKey, 0) == 1;
    }

    void OnTriggerEnter(Collider other)
    {
        if (played) return;
        if (!other.CompareTag(requiredTag)) return;

        if (ui != null)
            ui.Show(message);

        if (playOnce)
        {
            played = true;
            PlayerPrefs.SetInt(uniqueKey, 1);
            PlayerPrefs.Save();
        }
    }
}
