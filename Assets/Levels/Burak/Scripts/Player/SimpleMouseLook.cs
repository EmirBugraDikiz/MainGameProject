using UnityEngine;

public class ProfessionalMouseLook : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Karakterin kendisi (Sağa/Sola dönüş için)")]
    public Transform playerBody;

    [Header("Sensitivity Settings")]
    [Range(0.1f, 10f)]
    public float sensitivityX = 2.0f; // Yatay hassasiyet
    [Range(0.1f, 10f)]
    public float sensitivityY = 2.0f; // Dikey hassasiyet
    
    [Header("Restrictions")]
    public float topClamp = -90f; // Yukarı bakma sınırı
    public float bottomClamp = 90f; // Aşağı bakma sınırı

    [Header("Feel")]
    [Tooltip("Mouse hareketini yumuşatır. Rekabetçi oyunlar için false, sinematik his için true yapın.")]
    public bool useSmoothing = false;
    [Range(1f, 50f)]
    public float smoothTime = 25f;

    // İç değişkenler
    private float xRotation = 0f;
    private float mouseX, mouseY;
    private Quaternion targetRotationBody;
    private Quaternion targetRotationCam;

    void Start()
    {
        // Fareyi kilitle ve gizle
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Kamerayı ve rotasyonu mutlak sıfırdan başlat (Karşıya bakması için)
        xRotation = 0f;
        transform.localRotation = Quaternion.identity; // Kamerayı düzelt
        
        if (playerBody != null)
        {
            targetRotationBody = playerBody.rotation;
        }
        targetRotationCam = transform.localRotation;
    }

    void Update()
    {
        HandleInput();
    }

    void LateUpdate()
    {
        ApplyRotation();
    }

    void HandleInput()
    {
        // DeltaTime ile ÇARPMAYIN. Mouse input zaten bir "mesafe" verisidir.
        // Ham veri (Raw) her zaman daha keskindir.
        float rawMouseX = Input.GetAxisRaw("Mouse X") * sensitivityX;
        float rawMouseY = Input.GetAxisRaw("Mouse Y") * sensitivityY;

        // Yumuşatma ayarı
        if (useSmoothing)
        {
            // Yumuşak geçiş (Lerp benzeri ama daha frame-safe)
            mouseX = Mathf.Lerp(mouseX, rawMouseX, smoothTime * Time.deltaTime);
            mouseY = Mathf.Lerp(mouseY, rawMouseY, smoothTime * Time.deltaTime);
        }
        else
        {
            // Direkt ham veri (CS:GO / Valorant tarzı keskinlik)
            mouseX = rawMouseX;
            mouseY = rawMouseY;
        }
    }

    void ApplyRotation()
    {
        // 1. Dikey Hesaplama (Yukarı/Aşağı - Pitch)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, topClamp, bottomClamp);

        // Kamerayı çevir (Sadece X ekseni)
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // 2. Yatay Hesaplama (Sağa/Sola - Yaw)
        if (playerBody != null)
        {
            // Karakterin gövdesini Y ekseninde çevir
            playerBody.Rotate(Vector3.up * mouseX);
        }
    }
}