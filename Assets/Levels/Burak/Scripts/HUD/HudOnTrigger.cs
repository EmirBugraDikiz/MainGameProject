using UnityEngine;

public class HudOnTrigger : MonoBehaviour
{
    [Header("HUD")]
    [TextArea] public string message =
        "Çift Zıplama açıldı!\nHavada tekrar zıplamak için SPACE'e bir kez daha bas.";

    public float stay = 3.5f;
    public bool playOnce = true;

    [Header("Who triggers?")]
    public string requiredTag = "Player";

    bool played = false;

    private void OnTriggerEnter(Collider other)
    {
        if (playOnce && played) return;
        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag)) return;

        if (TutorialHintUI.Instance != null)
            TutorialHintUI.Instance.Show(message, stay);

        played = true;
    }
}
