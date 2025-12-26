using UnityEngine;

public class HudOnceGlobalOnTrigger : MonoBehaviour
{
    [Header("Unique Key (IMPORTANT)")]
    [Tooltip("Bu HUD sadece 1 kez çıksın diye global anahtar. Örn: L2_SwitchTutorial")]
    public string uniqueKey = "L2_SwitchTutorial";

    [Header("HUD")]
    [TextArea] public string message =
        "Elektrik kutusunu açmak için [E]'ye bas.\nİçerdeki kolu çekmek için tekrar [E]'ye bas.";

    public float stay = 4f;

    [Header("Trigger")]
    public string requiredTag = "Player";

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
        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
            return;

        if (PlayerPrefs.GetInt(uniqueKey, 0) == 1)
            return;

        if (TutorialHintUI.Instance != null)
            TutorialHintUI.Instance.Show(message, stay);

        PlayerPrefs.SetInt(uniqueKey, 1);
        PlayerPrefs.Save();
    }
}
