using UnityEngine;

public class EnergyCellPickup : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip pickupClip; // Item_Pick_Up.wav
    [SerializeField] private GameObject visualToDisable; // genelde this.gameObject

    private void Awake()
    {
        if (visualToDisable == null) visualToDisable = gameObject;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        GateInventory.HasEnergyCell = true;

        // sesi dünyada bağımsız çal
        if (pickupClip != null)
            AudioSource.PlayClipAtPoint(pickupClip, transform.position, 1f);

        // objeyi yok et / kapat
        if (visualToDisable == null) visualToDisable = gameObject;
        visualToDisable.SetActive(false);

        var col = GetComponent<Collider>();
        if (col) col.enabled = false;
    }
}

public static class GateInventory
{
    public static bool HasEnergyCell = false;
}
