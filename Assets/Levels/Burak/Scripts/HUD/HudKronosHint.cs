using UnityEngine;

public class HudKronosHint : MonoBehaviour
{
    [Header("HUD")]
    public TutorialHintUI ui;

    [TextArea]
    public string message = "Kronos açıldı!\n[Q] ile zamanı yavaşlat.\nDikkat: Kronos aktifken kontroller ters!";

    [Header("Trigger")]
    public string requiredTag = "Player";

    [Header("Once Only")]
    public bool playOnce = true;
    public string uniqueKey = "L4_KronosHint";

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
            ui.Show(message);   // TutorialHintUI'nin Show(string) fonksiyonu varsa direkt çalışır.

        if (playOnce)
        {
            played = true;
            PlayerPrefs.SetInt(uniqueKey, 1);
            PlayerPrefs.Save();
        }
    }
}
