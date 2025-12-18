using UnityEngine;

public class CollectableItem : MonoBehaviour
{
    [Header("Eþya Tipi")]
    public ItemCollector.ItemType itemType;

    [Header("Toplama Yöntemi")]
    [Tooltip("True ise yaklaþtýðýnda otomatik toplanýr. False ise E tuþuna basýlmasý gerekir.")]
    public bool collectOnTrigger = true;

    // Ses bileþeni referansý
    private AudioSource audioSource;
    private bool isPlayerNearby = false;
    private bool isCollected = false; // Eþya toplandý mý kontrolü (Tekrar tetiklenmemesi için)

    private void Start()
    {
        // Objenin üzerindeki AudioSource'u al
        audioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Eðer zaten toplandýysa tekrar iþlem yapma
        if (isCollected) return;

        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;

            if (collectOnTrigger)
            {
                Collect(other.gameObject);
            }
            else
            {
                Debug.Log($"[{itemType}] Toplamak için 'E' tuþuna basýn.");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
        }
    }

    private void Update()
    {
        if (isCollected) return; // Toplandýysa update'i çalýþtýrma

        if (!collectOnTrigger && isPlayerNearby)
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame)
            {
                Collect(GameObject.FindGameObjectWithTag("Player"));
            }
#else
            if (Input.GetKeyDown(KeyCode.E))
            {
                Collect(GameObject.FindGameObjectWithTag("Player"));
            }
#endif
        }
    }

    private void Collect(GameObject player)
    {
        ItemCollector collector = player.GetComponent<ItemCollector>();

        if (collector != null)
        {
            // 1. Envantere ekle
            collector.CollectItem(itemType);

            // 2. Bayraðý kaldýr (tekrar toplanmasýný engelle)
            isCollected = true;

            // 3. Ses ve Yok Etme Ýþlemleri
            if (audioSource != null && audioSource.clip != null)
            {
                // Sesi çal
                audioSource.Play();

                // Objenin görüntüsünü kapat (MeshRenderer)
                Renderer myRenderer = GetComponent<Renderer>();
                if (myRenderer != null)
                    myRenderer.enabled = false;

                // Objenin fiziðini kapat (Collider) - Böylece içinden geçilebilir olur
                Collider myCollider = GetComponent<Collider>();
                if (myCollider != null)
                    myCollider.enabled = false;

                // Varsa alt objelerdeki (çocuklardaki) görselleri de kapatmak için:
                foreach (Transform child in transform)
                {
                    child.gameObject.SetActive(false);
                }

                // Sesi çalmasý için objeyi sesin süresi kadar sahnede tut, sonra yok et
                Destroy(gameObject, audioSource.clip.length);
            }
            else
            {
                // Ses yoksa hemen yok et
                Destroy(gameObject);
            }
        }
        else
        {
            Debug.LogError("Oyuncuda ItemCollector scripti bulunamadý.");
        }
    }
}