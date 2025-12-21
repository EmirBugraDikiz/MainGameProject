using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Ayarlar")]
    public float etkilesimMesafesi = 3f; // Kapýya ne kadar yakýndan basýlabilir?
    public Camera oyuncuKamerasi;        // Ray'in çýkacaðý kamera (FPS ise Main Camera)

    void Update()
    {
        // Oyuncu 'E' tuþuna bastý mý?
        if (Input.GetKeyDown(KeyCode.E))
        {
            EtkilesimeGir();
        }
    }

    void EtkilesimeGir()
    {
        // Kameranýn tam ortasýndan ileriye doðru bir ýþýn (Ray) oluþtur
        Ray ray = oyuncuKamerasi.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        // Iþýný fýrlat (Raycast)
        if (Physics.Raycast(ray, out hit, etkilesimMesafesi))
        {
            // --- SENÝN GÖNDERDÝÐÝN KOD BURADA ÇALIÞACAK ---

            // 1. Önce çarptýðýmýz objede "KilitliKapi" scripti var mý bak
            KilitliKapi kapiScript = hit.transform.GetComponent<KilitliKapi>();

            // 2. Eðer direkt objede yoksa, belki ebeveynindedir (Parent) kontrolü yap
            // (Çünkü bazen collider child'da, script parent'ta olabilir)
            if (kapiScript == null)
            {
                kapiScript = hit.transform.GetComponentInParent<KilitliKapi>();
            }

            // 3. Script bulunduysa fonksiyonu çalýþtýr
            if (kapiScript != null)
            {
                kapiScript.KapiyiDene();
            }
            // ----------------------------------------------
        }
    }
}