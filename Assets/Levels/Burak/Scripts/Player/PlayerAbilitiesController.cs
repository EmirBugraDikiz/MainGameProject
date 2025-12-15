using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerAbilitiesController : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Normal yürüme hızı")]
    public float walkSpeed = 4f;

    [Tooltip("Shift basılıyken koşma hızı")]
    public float sprintSpeed = 7f;

    [Tooltip("Yerçekimi (negatif olmalı)")]
    public float gravity = -9.81f;

    [Tooltip("Zıplama yüksekliği")]
    public float jumpHeight = 2f;

    [Header("Movement Feel")]
    [Tooltip("Yerdeyken hızlanma (ne kadar büyük, o kadar keskin kontrol)")]
    public float groundAcceleration = 25f;

    [Tooltip("Normalde havadayken yön değiştirme ivmesi (küçük tut -> milim kontrol)")]
    public float airAccelerationNormal = 3f;

    [Tooltip("Kronos açıkken havada yön değiştirme ivmesi (yüksek -> rahat havada kontrol)")]
    public float airAccelerationKronos = 18f;

    [Header("Double Jump")]
    public bool hasDoubleJump = false;
    public int maxJumpsWithAbility = 2;
    public DoubleJumpVisual doubleJumpVisual;

    [Header("Kronos (Slow Motion)")]
    public bool hasKronos = false;
    public KeyCode kronosKey = KeyCode.Q;
    [Range(0.05f, 1f)]
    public float slowMotionScale = 0.3f;

    [Header("Animation (Optional)")]
    public Animator playerAnimator;

    private CharacterController controller;
    private Vector3 velocity;     // XZ = yatay hız, Y = dikey (zıplama / düşüş)
    private int jumpsUsed = 0;

    private bool kronosActive = false;
    private float baseTimeScale = 1f;
    private float baseFixedDeltaTime;

    public bool controlsEnabled = true;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        baseFixedDeltaTime = Time.fixedDeltaTime;
    }

    void Update()
    {
        // Ölürken / cutscene'de falan kontrol kilitliyse hiçbir input alma
        if (!controlsEnabled)
        {
            // Tamamen donsun istiyorsan:
            velocity = Vector3.zero;
            controller.Move(Vector3.zero);
            return;
        }

        HandleMovement();
        HandleJump();
        HandleKronosToggle();
        UpdateAnimator();
    }


    void HandleMovement()
    {
        bool isGrounded = controller.isGrounded;

        // Yere değdiysek dikey hızı sıfıra yakınla
        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
            jumpsUsed = 0;

            if (doubleJumpVisual != null)
                doubleJumpVisual.SetActive(false);
        }

        // Input al
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        // Kronos yan etkisi: kontroller ters
        if (kronosActive)
        {
            x = -x;
            z = -z;
        }

        Vector3 inputDir = new Vector3(x, 0f, z);
        if (inputDir.sqrMagnitude > 1f)
            inputDir.Normalize();

        bool hasMoveInput = inputDir.sqrMagnitude > 0.0001f;

        // Sprint
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && hasMoveInput && isGrounded;
        float targetSpeed = hasMoveInput ? (isSprinting ? sprintSpeed : walkSpeed) : 0f;

        // Input'u world uzayına çevir (kameraya göre yürüme)
        Vector3 moveWorld = transform.right * inputDir.x + transform.forward * inputDir.z;
        if (moveWorld.sqrMagnitude > 1f)
            moveWorld.Normalize();

        Vector3 targetHorizontal = moveWorld * targetSpeed;

        // Şu anki yatay hız
        Vector3 currentHorizontal = new Vector3(velocity.x, 0f, velocity.z);

        // Hangi ivme? (yerde/kronos/havada)
        float accel;
        if (isGrounded)
            accel = groundAcceleration;
        else
            accel = kronosActive ? airAccelerationKronos : airAccelerationNormal;

        // Smooth hız değişimi
        currentHorizontal = Vector3.MoveTowards(
            currentHorizontal,
            targetHorizontal,
            accel * Time.deltaTime
        );

        velocity.x = currentHorizontal.x;
        velocity.z = currentHorizontal.z;

        // Yerçekimi
        velocity.y += gravity * Time.deltaTime;

        // Karakteri hareket ettir
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleJump()
    {
        bool isGrounded = controller.isGrounded;
        int allowedJumps = hasDoubleJump ? maxJumpsWithAbility : 1;

        if (Input.GetButtonDown("Jump"))
        {
            // 1) Zemindeyken sadece "normal zıplama" hakkı
            if (isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                jumpsUsed = 1; // birinci zıplama kullanıldı

                // double jump efekti zeminde aktif olmasın
                if (doubleJumpVisual != null)
                    doubleJumpVisual.SetActive(false);

                if (playerAnimator != null)
                    playerAnimator.SetTrigger("Jump");
            }
            // 2) Havada sadece "double jump" hakkı
            else if (hasDoubleJump && jumpsUsed < allowedJumps)
            {
                // Bu ister zeminden zıpladıktan sonra 2. zıplama olsun,
                // ister yüksekten düşerken hava zıplaması olsun → ikisi de "double jump" sayılır
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                
                // Artık hava hakkını da kullandı, tekrar zıplayamasın diye direkt limite çekiyoruz
                jumpsUsed = allowedJumps;

                if (doubleJumpVisual != null)
                    doubleJumpVisual.SetActive(true); // yan etki burada devreye girsin

                if (playerAnimator != null)
                    playerAnimator.SetTrigger("Jump");
            }
        }
    }


    void HandleKronosToggle()
    {
        if (!hasKronos)
            return;

        if (Input.GetKeyDown(kronosKey))
        {
            kronosActive = !kronosActive;

            if (kronosActive)
            {
                Time.timeScale = slowMotionScale;
                Time.fixedDeltaTime = baseFixedDeltaTime * slowMotionScale;
            }
            else
            {
                Time.timeScale = baseTimeScale;
                Time.fixedDeltaTime = baseFixedDeltaTime;
            }
        }
    }

    void UpdateAnimator()
    {
        if (playerAnimator == null) return;

        Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
        float speed = horizontal.magnitude;

        playerAnimator.SetFloat("Speed", speed);
        playerAnimator.SetBool("IsGrounded", controller.isGrounded);
    }

    // ==== Potions ====

    public void GrantDoubleJump()
    {
        hasDoubleJump = true;
        Debug.Log("Double jump ability granted.");
    }

    public void GrantKronos()
    {
        hasKronos = true;
        Debug.Log("Kronos ability granted.");
    }

    public void ResetVelocity()
    {
        velocity = Vector3.zero;
        jumpsUsed = 0;
    }

}
