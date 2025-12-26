using UnityEngine;

public class HudStartDoorHint : MonoBehaviour
{
    [Header("HUD")]
    [TextArea] public string message = "Kapıları açmak için [E] tuşuna bas.";
    public float stay = 3f;

    [Header("Trigger")]
    public string requiredTag = "Player";

    [Header("Once Only")]
    public bool playOnce = true;
    public string uniqueKey = "L2_DoorOpenHint";

#if UNITY_EDITOR
    [Header("Editor Only")]
    public bool resetOnPlayInEditor = true;
#endif

    void Awake()
    {
#if UNITY_EDITOR
        if (resetOnPlayInEditor && Application.isPlaying)
        {
            PlayerPrefs.DeleteKey(uniqueKey);
            PlayerPrefs.Save();
        }
#endif
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(requiredTag)) return;

        if (playOnce && PlayerPrefs.GetInt(uniqueKey, 0) == 1) return;

        if (TutorialHintUI.Instance != null)
            TutorialHintUI.Instance.Show(message, stay);

        if (playOnce)
        {
            PlayerPrefs.SetInt(uniqueKey, 1);
            PlayerPrefs.Save();
        }
    }
}
