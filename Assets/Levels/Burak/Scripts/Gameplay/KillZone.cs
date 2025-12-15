using UnityEngine;

[RequireComponent(typeof(Collider))]
public class KillZone : MonoBehaviour
{
    [Tooltip("Hangi tag'e sahip oyuncuyu öldüreceğiz")]
    public string playerTag = "Player";

    private void Reset()
    {
        // Otomatik olarak trigger açık olsun
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[KillZone] Trigger enter: {other.name}", this);

        // Root objeye bak (çoğu zaman PlayerCapsule Variant)
        Transform root = other.transform.root;

        // Tag kontrolü (hem collider'da hem root'ta dene)
        if (!other.CompareTag(playerTag) && !root.CompareTag(playerTag))
        {
            Debug.Log($"[KillZone] {other.name} / {root.name} playerTag ile eşleşmedi.", this);
            return;
        }

        // PlayerRespawn bul
        PlayerRespawn respawn =
            root.GetComponent<PlayerRespawn>() ??
            other.GetComponent<PlayerRespawn>();

        if (respawn != null)
        {
            Debug.Log("[KillZone] PlayerRespawn bulundu, Kill() çağrılıyor.", this);
            respawn.Kill();
        }
        else
        {
            Debug.LogWarning($"[KillZone] Player tagli objede PlayerRespawn bulunamadı. Root: {root.name}", this);
        }
    }
}
