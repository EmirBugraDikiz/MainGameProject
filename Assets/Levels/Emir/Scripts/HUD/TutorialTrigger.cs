using UnityEngine;

public class TutorialTrigger : MonoBehaviour
{
    [TextArea] public string message =
        "WASD: Hareket\nSpace: Zıpla\nShift: Koş";

    public bool playOnce = true;
    bool played = false;

    void OnTriggerEnter(Collider other)
    {
        if (played && playOnce) return;

        // Player tag'in "Player" ise bunu kullan:
        if (!other.CompareTag("Player")) return;

        if (TutorialHintUI.Instance != null)
            TutorialHintUI.Instance.Show(message);

        played = true;
    }
}
