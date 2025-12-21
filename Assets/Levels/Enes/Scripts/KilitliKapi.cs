using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class KilitliKapi : MonoBehaviour
{
    // Singleton yapýsý: CollectableItem scriptinin bu kapýyý bulmasýný saðlar
    public static KilitliKapi instance;

    [Header("Kilit Ayarlarý")]
    public int gerekenObjeSayisi = 4; // Toplanmasý gereken anahtar/nesne sayýsý
    public int suanToplanan = 0;     // Þu ana kadar toplanan (Otomatik artacak)
    private bool kilitAcildi = false;

    [Header("Hareket Ayarlarý")]
    public float acilmaAcisi = 90f; // Kapýnýn kaç derece döneceði
    [Tooltip("Düþük deðer = Yavaþ açýlma. Örn: 0.5 çok aðýrdýr, 5 çok hýzlýdýr.")]
    public float acilmaHizi = 0.8f; // <-- YAVAÞ AÇILMASI ÝÇÝN DÜÞÜRÜLDÜ (Eskisi 4'tü)

    [Header("Sesler")]
    public AudioClip kilitliZorlamaSesi; // Kapý kilitliyken çalacak ses
    public AudioClip acilmaSesi;         // Açýlma sesi
    public AudioClip kapanmaSesi;        // Kapanma sesi

    // Durum deðiþkenleri
    private bool kapiAcikMi = false;
    private Quaternion kapaliRotasyon;
    private Quaternion acikRotasyon;
    private AudioSource audioSource;

    void Awake()
    {
        // Sahnede bu scriptten sadece bir tane olduðunu varsayýyoruz
        if (instance == null)
            instance = this;
    }

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        // Baþlangýç rotasyonunu kaydet
        kapaliRotasyon = transform.localRotation;

        // Açýk pozisyonu hesapla (Sadece Y ekseninde döndürür)
        acikRotasyon = Quaternion.Euler(kapaliRotasyon.eulerAngles.x, kapaliRotasyon.eulerAngles.y + acilmaAcisi, kapaliRotasyon.eulerAngles.z);
    }

    void Update()
    {
        // Kapý kilidi açýldýysa hareketi yönet
        if (kilitAcildi)
        {
            Quaternion hedef = kapiAcikMi ? acikRotasyon : kapaliRotasyon;

            // Slerp fonksiyonu pürüzsüz ve yavaþ geçiþ saðlar
            // Time.deltaTime * acilmaHizi formülü hýzý belirler
            transform.localRotation = Quaternion.Slerp(transform.localRotation, hedef, Time.deltaTime * acilmaHizi);
        }
    }

    // Karakterin (Player) Raycast ile çaðýrdýðý fonksiyon
    public void KapiyiDene()
    {
        if (!kilitAcildi)
        {
            // Eðer objeler henüz tamamlanmadýysa
            Debug.Log($"Kapý Kilitli! Eksik parçalar var. ({suanToplanan}/{gerekenObjeSayisi})");

            // Zorlama sesi çal
            if (kilitliZorlamaSesi != null) audioSource.PlayOneShot(kilitliZorlamaSesi);
        }
        else
        {
            // Kilit açýksa kapý durumunu deðiþtir (Aç/Kapat)
            kapiAcikMi = !kapiAcikMi;

            if (kapiAcikMi)
            {
                if (acilmaSesi != null) audioSource.PlayOneShot(acilmaSesi);
            }
            else
            {
                if (kapanmaSesi != null) audioSource.PlayOneShot(kapanmaSesi);
            }
        }
    }

    // Toplanabilir eþyalarýn (CollectableItem) çaðýrdýðý fonksiyon
    public void ObjeToplandi()
    {
        suanToplanan++;
        Debug.Log("Kilit Parçasý Toplandý: " + suanToplanan + "/" + gerekenObjeSayisi);

        if (suanToplanan >= gerekenObjeSayisi)
        {
            kilitAcildi = true;
            Debug.Log("TÜM PARÇALAR TAMAM! Kapý kilidi açýldý.");
            // Ýstersen buraya bir "Kilit açýlma sesi" (Unlock sound) ekleyebilirsin.
        }
    }
}