using UnityEngine;
using UnityEngine.Rendering;

public class DoubleJumpVisual : MonoBehaviour
{
    public Volume volume;        // Global Volume
    public float fadeSpeed = 5f; // Efektin açılıp kapanma hızı

    private float targetWeight = 0f;

    void Update()
    {
        if (volume == null) return;

        volume.weight = Mathf.MoveTowards(
            volume.weight,
            targetWeight,
            Time.unscaledDeltaTime * fadeSpeed
        );
    }

    public void SetActive(bool active)
    {
        targetWeight = active ? 1f : 0f;
    }
}
