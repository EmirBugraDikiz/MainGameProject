using UnityEngine;

public class CollectableItem : MonoBehaviour
{
    [Header("Eþya Tipi")]
    // ItemCollector'daki enum'ý kullanýyoruz
    public ItemCollector.ItemType itemType;

    [Header("Toplama Yöntemi")]
    [Tooltip("True ise yaklaþtýðýnda otomatik toplanýr. False ise E tuþuna basýlmasý gerekir.")]
    public bool collectOnTrigger = true;

    private bool isPlayerNearby = false;

    // Eþyaya yaklaþýldýðýnda (Trigger'a girildiðinde)
    private void OnTriggerEnter(Collider other)
    {
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
                // Burada isterseniz ekranda bir UI mesajý gösterebilirsiniz.
            }
        }
    }

    // Eþyadan uzaklaþýldýðýnda (Trigger'dan çýkýldýðýnda)
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
        }
    }

    // Sadece "E" tuþu ile toplama yöntemini kullanýyorsanýz Update() gerekli
    private void Update()
    {
        // Eðer Eþya yakýnda ise, tetikleyici kapalýysa VE oyuncu "E" tuþuna bastýysa
        // Karakterin `FirstPersonController` scriptindeki `HandleInteraction()` metodu da Raycast ile
        // toplama yapabilir, ancak sizin Raycast kodunuz sadece kapý için optimize edilmiþ.
        // Bu yüzden, daha basit bir yol olarak burada Input sistemi kontrolü yapabiliriz.

        // **ÖNEMLÝ:** Eðer Raycast kullanmak istiyorsanýz, bu kodu silin ve `FirstPersonController`
        // scriptinizdeki `HandleInteraction()` metodunu düzenlemeniz gerekir (bir sonraki bölümde açýklayacaðým).

        if (!collectOnTrigger && isPlayerNearby)
        {
            // Yeni Input Sistemi ile 'E' tuþu kontrolü (Sizin Controller'ýnýzdaki gibi)
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame)
            {
                // Oyuncu objesini bul (Collider'ýn parent'ý)
                Collect(GameObject.FindGameObjectWithTag("Player"));
            }
#else
            // Eski Input Sistemi
            if (Input.GetKeyDown(KeyCode.E))
            {
                Collect(GameObject.FindGameObjectWithTag("Player"));
            }
#endif
        }
    }

    // Toplama iþlemini gerçekleþtiren ana fonksiyon
    private void Collect(GameObject player)
    {
        ItemCollector collector = player.GetComponent<ItemCollector>();

        if (collector != null)
        {
            collector.CollectItem(itemType);
            // Eþyayý sahneden kaldýr
            Destroy(gameObject);
        }
        else
        {
            Debug.LogError("Oyuncuda ItemCollector scripti bulunamadý. Lütfen eklediðinizden emin olun.");
        }
    }
}