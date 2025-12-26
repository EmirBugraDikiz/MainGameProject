using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class KilitliKapi : MonoBehaviour
{
    // Singleton yap�s�: CollectableItem scriptinin bu kap�y� bulmas�n� sa�lar
    public static KilitliKapi instance;

    [Header("Kilit Ayarlar�")]
    public int gerekenObjeSayisi = 3; // Toplanmas� gereken anahtar/nesne say�s�
    public int suanToplanan = 0;     // �u ana kadar toplanan (Otomatik artacak)
    private bool kilitAcildi = false;

    [Header("Hareket Ayarlar�")]
    public float acilmaAcisi = 90f; // Kap�n�n ka� derece d�nece�i
    [Tooltip("D���k de�er = Yava� a��lma. �rn: 0.5 �ok a��rd�r, 5 �ok h�zl�d�r.")]
    public float acilmaHizi = 0.8f; // <-- YAVA� A�ILMASI ���N D���R�LD� (Eskisi 4't�)

    [Header("Sesler")]
    public AudioClip kilitliZorlamaSesi; // Kap� kilitliyken �alacak ses
    public AudioClip acilmaSesi;         // A��lma sesi
    public AudioClip kapanmaSesi;        // Kapanma sesi

    // Durum de�i�kenleri
    private bool kapiAcikMi = false;
    private Quaternion kapaliRotasyon;
    private Quaternion acikRotasyon;
    private AudioSource audioSource;

    void Awake()
    {
        // Sahnede bu scriptten sadece bir tane oldu�unu varsay�yoruz
        if (instance == null)
            instance = this;
    }

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        // Ba�lang�� rotasyonunu kaydet
        kapaliRotasyon = transform.localRotation;

        // A��k pozisyonu hesapla (Sadece Y ekseninde d�nd�r�r)
        acikRotasyon = Quaternion.Euler(kapaliRotasyon.eulerAngles.x, kapaliRotasyon.eulerAngles.y + acilmaAcisi, kapaliRotasyon.eulerAngles.z);
    }

    void Update()
    {
        // Kap� kilidi a��ld�ysa hareketi y�net
        if (kilitAcildi)
        {
            Quaternion hedef = kapiAcikMi ? acikRotasyon : kapaliRotasyon;

            // Slerp fonksiyonu p�r�zs�z ve yava� ge�i� sa�lar
            // Time.deltaTime * acilmaHizi form�l� h�z� belirler
            transform.localRotation = Quaternion.Slerp(transform.localRotation, hedef, Time.deltaTime * acilmaHizi);
        }
    }

    // Karakterin (Player) Raycast ile �a��rd��� fonksiyon
    public void KapiyiDene()
    {
        if (!kilitAcildi)
        {
            // E�er objeler hen�z tamamlanmad�ysa
            Debug.Log($"Kap� Kilitli! Eksik par�alar var. ({suanToplanan}/{gerekenObjeSayisi})");

            // Zorlama sesi �al
            if (kilitliZorlamaSesi != null) audioSource.PlayOneShot(kilitliZorlamaSesi);
        }
        else
        {
            // Kilit a��ksa kap� durumunu de�i�tir (A�/Kapat)
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

    // Toplanabilir e�yalar�n (CollectableItem) �a��rd��� fonksiyon
    public void ObjeToplandi()
    {
        suanToplanan++;
        Debug.Log("Kilit Parçası Toplandı: " + suanToplanan + "/" + gerekenObjeSayisi);

        if (!kilitAcildi && suanToplanan >= gerekenObjeSayisi)
        {
            kilitAcildi = true;
            Debug.Log("TÜM PARÇALAR TAMAM! Kapı kilidi açıldı.");

            // ARTIK OTOMATİK AÇMA YOK.
            // İstersen burada sadece bir "kilit açıldı" sesi çalabilirsin
            // (acilmaSesi değil, ayrı bir unlock sesi daha iyi olur).
            // örn: if (unlockSesi != null) audioSource.PlayOneShot(unlockSesi);
        }
    }
}