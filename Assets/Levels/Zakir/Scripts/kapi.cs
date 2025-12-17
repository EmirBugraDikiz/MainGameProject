using UnityEngine;

[RequireComponent(typeof(AudioSource))] // Bu satýr, objede AudioSource yoksa otomatik ekler
public class kapi : MonoBehaviour
{
    [Header("Ayarlar")]
    public float acilmaMiktari = 90f;
    public float acilmaHizi = 5f;

    [Header("Sesler")]
    public AudioClip acilmaSesi;   // Buraya "15419__pagancow..." dosyasýný sürükle
    public AudioClip kapanmaSesi;  // Buraya "652346__weak_hero..." dosyasýný sürükle

    private bool acikMi = false;
    private Quaternion baslangicRotasyonu;
    private Quaternion hedefRotasyon;
    private AudioSource audioSource; // Audio Source referansý

    void Start()
    {
        baslangicRotasyonu = transform.localRotation;
        hedefRotasyon = baslangicRotasyonu;

        // Scriptin takýlý olduðu objedeki Audio Source'u bulup deðiþkene atýyoruz
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        transform.localRotation = Quaternion.Slerp(transform.localRotation, hedefRotasyon, Time.deltaTime * acilmaHizi);
    }

    public void KapiyiAcKapat()
    {
        acikMi = !acikMi;

        if (acikMi)
        {
            // --- AÇILMA ---
            Quaternion acilmaRotasyonu = Quaternion.Euler(0, acilmaMiktari, 0);
            hedefRotasyon = baslangicRotasyonu * acilmaRotasyonu;

            // Açýlma sesi çal (Ses dosyasý atanmýþsa)
            if (audioSource != null && acilmaSesi != null)
            {
                audioSource.PlayOneShot(acilmaSesi);
            }
        }
        else
        {
            // --- KAPANMA ---
            hedefRotasyon = baslangicRotasyonu;

            // Kapanma sesi çal (Ses dosyasý atanmýþsa)
            if (audioSource != null && kapanmaSesi != null)
            {
                audioSource.PlayOneShot(kapanmaSesi);
            }
        }
    }
}