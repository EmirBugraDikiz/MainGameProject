using UnityEngine;

public class kapi : MonoBehaviour
{
    [Header("Ayarlar")]
    public float acilmaMiktari = 90f; // Kapýnýn ne kadar döneceði (Örn: 90 derece)
    public float acilmaHizi = 5f;     // Dönüþ hýzý

    private bool acikMi = false;
    private Quaternion baslangicRotasyonu; // Kapýnýn ilk (kapalý) hali
    private Quaternion hedefRotasyon;      // Gitmesi gereken açý

    void Start()
    {
        // 1. Oyun baþladýðýnda kapý editörde nasýl duruyorsa, o açýyý "Kapalý" hali olarak kaydet.
        baslangicRotasyonu = transform.localRotation;
        
        // Baþlangýçta hedefimiz kapalý kalmak.
        hedefRotasyon = baslangicRotasyonu;
    }

    void Update()
    {
        // Kapýyý yumuþakça hedefe döndür
        transform.localRotation = Quaternion.Slerp(transform.localRotation, hedefRotasyon, Time.deltaTime * acilmaHizi);
    }

    public void KapiyiAcKapat()
    {
        acikMi = !acikMi;

        if (acikMi)
        {
            // 2. AÇIK HALÝ: Baþlangýç rotasyonunun üzerine "acilmaMiktari" kadar ekleme yap.
            // Quaternion'larda ekleme iþlemi çarpma (*) ile yapýlýr.
            Quaternion acilmaRotasyonu = Quaternion.Euler(0, acilmaMiktari, 0);
            hedefRotasyon = baslangicRotasyonu * acilmaRotasyonu;
        }
        else
        {
            // 3. KAPALI HALÝ: Direkt olarak baþlangýçta kaydettiðimiz orijinal duruma dön.
            hedefRotasyon = baslangicRotasyonu;
        }
    }
}