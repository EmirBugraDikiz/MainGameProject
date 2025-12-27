using UnityEngine;

public class ProfessionalMouseLook : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Karakterin kendisi (Sağa/Sola dönüş için)")]
    public Transform playerBody;

    [Tooltip("Varsa otomatik bulunur. controlsEnabled=false iken mouse look tamamen durur.")]
    public PlayerAbilitiesController abilities;

    [Header("Sensitivity Settings")]
    [Range(0.1f, 10f)]
    public float sensitivityX = 2.0f;
    [Range(0.1f, 10f)]
    public float sensitivityY = 2.0f;

    [Header("Restrictions")]
    public float topClamp = -90f;
    public float bottomClamp = 90f;

    [Header("Feel")]
    public bool useSmoothing = false;
    [Range(1f, 50f)]
    public float smoothTime = 25f;

    // İç değişkenler
    private float xRotation = 0f;
    private float mouseX, mouseY;

    // ✅ Level başındaki "temiz" kamera ve body rotları
    private Quaternion _startCamLocalRot;
    private Quaternion _startBodyRot;

    // ✅ controlsEnabled false->true dönüşünde reset basmak için
    private bool _wasFrozenLastFrame = false;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Abilities auto-find
        if (abilities == null)
        {
            if (playerBody != null)
                abilities = playerBody.GetComponent<PlayerAbilitiesController>();

            if (abilities == null)
                abilities = GetComponentInParent<PlayerAbilitiesController>();
        }

        // Başlangıç rotlarını kaydet
        _startCamLocalRot = transform.localRotation;
        _startBodyRot = playerBody != null ? playerBody.rotation : Quaternion.identity;

        // Kamerayı düz başlat
        xRotation = 0f;
        mouseX = 0f;
        mouseY = 0f;
    }

    void Update()
    {
        // ✅ Jumpscare / death sırasında mouse look tamamen dursun
        if (abilities != null && !abilities.controlsEnabled)
        {
            mouseX = 0f;
            mouseY = 0f;
            _wasFrozenLastFrame = true;
            return;
        }

        // ✅ Tekrar açıldıysa (respawn sonrası) kamerayı level başındaki hale geri al
        if (_wasFrozenLastFrame)
        {
            ResetLookToLevelStart();
            _wasFrozenLastFrame = false;
        }

        HandleInput();
    }

    void LateUpdate()
    {
        if (abilities != null && !abilities.controlsEnabled)
            return;

        ApplyRotation();
    }

    void HandleInput()
    {
        float rawMouseX = Input.GetAxisRaw("Mouse X") * sensitivityX;
        float rawMouseY = Input.GetAxisRaw("Mouse Y") * sensitivityY;

        if (useSmoothing)
        {
            mouseX = Mathf.Lerp(mouseX, rawMouseX, smoothTime * Time.deltaTime);
            mouseY = Mathf.Lerp(mouseY, rawMouseY, smoothTime * Time.deltaTime);
        }
        else
        {
            mouseX = rawMouseX;
            mouseY = rawMouseY;
        }
    }

    void ApplyRotation()
    {
        // Pitch
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, topClamp, bottomClamp);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Yaw
        if (playerBody != null)
            playerBody.Rotate(Vector3.up * mouseX);
    }

    // ✅ Respawn sonrası kamera sapıtmasın diye "level start" rotuna reset
    public void ResetLookToLevelStart()
    {
        mouseX = 0f;
        mouseY = 0f;
        xRotation = 0f;

        transform.localRotation = _startCamLocalRot;

        if (playerBody != null)
            playerBody.rotation = _startBodyRot;
    }
}
