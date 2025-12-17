using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("Etkileþim mesafesi")]
    public float interactionDistance = 3.0f;

    [Tooltip("Hangi layerdaki objelerle etkileþime girilecek?")]
    public LayerMask interactionLayer;

    [Tooltip("Kamera referansý (Ray buradan atýlacak)")]
    public Transform cameraTransform;

    [Tooltip("Etkileþim tuþu")]
    public KeyCode interactionKey = KeyCode.E;

    private void Start()
    {
        // Eðer kamera atanmadýysa otomatik bulmaya çalýþ
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        HandleInteraction();
    }

    private void HandleInteraction()
    {
        // Yeni scriptin yapýsýna uygun olarak eski Input sistemine çevirdim (Input.GetKeyDown)
        if (Input.GetKeyDown(interactionKey))
        {
            // Ray oluþtur
            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            RaycastHit hit;

            // Debug için sahne ekranýnda çizgiyi gör (Sadece editörde çalýþýr)
            Debug.DrawRay(cameraTransform.position, cameraTransform.forward * interactionDistance, Color.red, 1f);

            // Raycast at
            if (Physics.Raycast(ray, out hit, interactionDistance, interactionLayer))
            {
                // Çarptýðý objede 'kapi' scripti var mý?
                kapi kapiScripti = hit.collider.GetComponentInParent<kapi>();

                if (kapiScripti != null)
                {
                    kapiScripti.KapiyiAcKapat();
                    // Debug.Log("Kapý açýldý/kapandý.");
                }
            }
        }
    }
}