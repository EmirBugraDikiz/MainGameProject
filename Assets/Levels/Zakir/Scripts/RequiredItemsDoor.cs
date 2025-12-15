using UnityEngine;

public class RequiredItemsDoor : MonoBehaviour
{
    [Header("Ayarlar")]
    public float openAngle = 90f; // Kapýnýn ne kadar döneceði
    public float openSpeed = 2f;  // Kapýnýn açýlma hýzý

    private ItemCollector playerCollector;
    private bool isLocked = true; // Baþlangýçta kapalý
    private bool isOpening = false;
    private Quaternion startRotation;
    private Quaternion openRotation;

    void Start()
    {
        // Kapýnýn baþlangýç ve bitiþ rotasyonlarýný hesapla
        startRotation = transform.localRotation;
        openRotation = Quaternion.Euler(transform.localEulerAngles + new Vector3(0, openAngle, 0));

        // Sahnedeki ItemCollector'ý bul
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerCollector = player.GetComponent<ItemCollector>();
        }
        else
        {
            Debug.LogError("Sahnedeki 'Player' tag'li objede ItemCollector bulunamadý.");
        }
    }

    void Update()
    {
        // Kapý otomatik olarak açýlacaksa, sürekli kontrol et
        if (isLocked && playerCollector != null && playerCollector.AreAllRequiredItemsCollected())
        {
            UnlockDoor();
        }

        // Kapý açýlýyorsa
        if (isOpening)
        {
            transform.localRotation = Quaternion.Slerp(transform.localRotation, openRotation, Time.deltaTime * openSpeed);

            // Açýlma tamamlandýysa
            if (Quaternion.Angle(transform.localRotation, openRotation) < 0.1f)
            {
                transform.localRotation = openRotation;
                isOpening = false;
            }
        }
    }

    // Kapýyý açma iþlemini baþlatan fonksiyon
    public void UnlockDoor()
    {
        if (isLocked)
        {
            isLocked = false;
            isOpening = true;
            Debug.Log("Gerekli tüm eþyalar toplandý, kapý açýlýyor!");
            // Ses efekti veya görsel efektler buraya eklenebilir.
        }
    }

    // NOT: Ýstenirse, kapýnýn kendiliðinden açýlmasýný deðil, 
    // oyuncu "E" tuþuyla etkileþime girdiðinde açýlmasýný da ayarlayabilirsiniz.
    // Bunun için `UnlockDoor()` metodunu oyuncunun Raycast'i ile çaðýrabilirsiniz,
    // ancak o durumda `isLocked` kontrolü eklemeniz gerekir.
    /*
    public void KapiyiAcKapat() // FirstPersonController'dan çaðrýlýrsa
    {
        if (!isLocked) // Sadece kilitli deðilse aç kapa
        {
            // Kapýnýn hareketini burada yap (açýk/kapalý durumunu deðiþtirerek)
        }
        else if (playerCollector.AreAllRequiredItemsCollected()) // Kilitli ama þartlar tamam
        {
             UnlockDoor(); // Aç ve isLocked'ý false yap
        }
        else
        {
             Debug.Log("Kapý kilitli. Tüm eþyalarý topla!");
        }
    }
    */
}