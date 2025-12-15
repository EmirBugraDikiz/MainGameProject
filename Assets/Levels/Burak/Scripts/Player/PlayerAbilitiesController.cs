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

    [Header("Ceiling Fix (AAA)")]
    [Tooltip("Tavana çarpınca yukarı hızını ne kadar kıracağız? 0 = normal düşüş, -2 = daha snappy.")]
    public float ceilingHitDownVelocity = -2f;

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

    [Header("Player Audio")]
    [Tooltip("Player üstünde yoksa otomatik eklenir.")]
    public AudioSource playerAudioSource;

    [Tooltip("Yürüme/koşma adım sesi (Single_Step.wav)")]
    public AudioClip footstepClip;

    [Tooltip("Normal zıplama sesi (First_Jump.wav)")]
    public AudioClip firstJumpClip;

    [Tooltip("Double jump sesi (Double_Jump.wav)")]
    public AudioClip doubleJumpClip;

    [Range(0f, 1f)] public float footstepVolume = 0.65f;
    [Range(0f, 1f)] public float jumpVolume = 0.8f;

    [Tooltip("Yürümede adım aralığı (saniye)")]
    public float walkStepInterval = 0.48f;

    [Tooltip("Koşmada adım aralığı (saniye)")]
    public float sprintStepInterval = 0.30f;

    [Tooltip("Adım pitch random aralığı (çok küçük değişim doğal his verir)")]
    public Vector2 footstepPitchRange = new Vector2(0.95f, 1.05f);

    [Tooltip("Jump pitch random aralığı")]
    public Vector2 jumpPitchRange = new Vector2(0.98f, 1.02f);

    [Tooltip("Minimum hareket hızı eşiği. Altında adım sesi çalmaz.")]
    public float minSpeedForFootsteps = 0.15f;

    private CharacterController controller;
    private Vector3 velocity;     // XZ = yatay hız, Y = dikey (zıplama / düşüş)
    private int jumpsUsed = 0;

    private bool kronosActive = false;
    private float baseTimeScale = 1f;
    private float baseFixedDeltaTime;

    public bool controlsEnabled = true;

    // Footstep timer
    private float stepTimer = 0f;

    // Last move collision flags (ceiling fix)
    private CollisionFlags lastMoveFlags;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        baseFixedDeltaTime = Time.fixedDeltaTime;

        // AudioSource yoksa otomatik ekle
        if (playerAudioSource == null)
            playerAudioSource = GetComponent<AudioSource>();

        if (playerAudioSource == null)
            playerAudioSource = gameObject.AddComponent<AudioSource>();

        playerAudioSource.playOnAwake = false;
        playerAudioSource.loop = false;
        playerAudioSource.spatialBlend = 1f; // 3D (istersen 0 yap UI gibi olur)
        playerAudioSource.dopplerLevel = 0f;
    }

    void Update()
    {
        if (!controlsEnabled)
        {
            velocity = Vector3.zero;
            controller.Move(Vector3.zero);
            return;
        }

        HandleMovement();
        HandleJump();
        HandleKronosToggle();
        UpdateAnimator();
        HandleFootsteps();
    }

    void HandleMovement()
    {
        bool isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
            jumpsUsed = 0;

            if (doubleJumpVisual != null)
                doubleJumpVisual.SetActive(false);
        }

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        if (kronosActive)
        {
            x = -x;
            z = -z;
        }

        Vector3 inputDir = new Vector3(x, 0f, z);
        if (inputDir.sqrMagnitude > 1f)
            inputDir.Normalize();

        bool hasMoveInput = inputDir.sqrMagnitude > 0.0001f;

        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && hasMoveInput && isGrounded;
        float targetSpeed = hasMoveInput ? (isSprinting ? sprintSpeed : walkSpeed) : 0f;

        Vector3 moveWorld = transform.right * inputDir.x + transform.forward * inputDir.z;
        if (moveWorld.sqrMagnitude > 1f)
            moveWorld.Normalize();

        Vector3 targetHorizontal = moveWorld * targetSpeed;

        Vector3 currentHorizontal = new Vector3(velocity.x, 0f, velocity.z);

        float accel;
        if (isGrounded)
            accel = groundAcceleration;
        else
            accel = kronosActive ? airAccelerationKronos : airAccelerationNormal;

        currentHorizontal = Vector3.MoveTowards(
            currentHorizontal,
            targetHorizontal,
            accel * Time.deltaTime
        );

        velocity.x = currentHorizontal.x;
        velocity.z = currentHorizontal.z;

        // Yerçekimi
        velocity.y += gravity * Time.deltaTime;

        // Move + ceiling fix için flags yakala
        lastMoveFlags = controller.Move(velocity * Time.deltaTime);

        // ✅ AAA: Tavana çarptıysan yukarı hızını anında kır
        if ((lastMoveFlags & CollisionFlags.Above) != 0 && velocity.y > 0f)
        {
            velocity.y = ceilingHitDownVelocity; // -2f default: snappy düşüş
        }
    }

    void HandleJump()
    {
        bool isGrounded = controller.isGrounded;
        int allowedJumps = hasDoubleJump ? maxJumpsWithAbility : 1;

        if (Input.GetButtonDown("Jump"))
        {
            if (isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                jumpsUsed = 1;

                if (doubleJumpVisual != null)
                    doubleJumpVisual.SetActive(false);

                if (playerAnimator != null)
                    playerAnimator.SetTrigger("Jump");

                PlayJumpSfx(firstJumpClip);
            }
            else if (hasDoubleJump && jumpsUsed < allowedJumps)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                jumpsUsed = allowedJumps;

                if (doubleJumpVisual != null)
                    doubleJumpVisual.SetActive(true);

                if (playerAnimator != null)
                    playerAnimator.SetTrigger("Jump");

                PlayJumpSfx(doubleJumpClip);
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

    void HandleFootsteps()
    {
        if (footstepClip == null) return;

        bool isGrounded = controller.isGrounded;

        // Yerde değilse adım yok, timer’ı sıfırla ki yere inince “spam” olmasın
        if (!isGrounded)
        {
            stepTimer = 0f;
            return;
        }

        // Yatay hız
        Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
        float speed = horizontal.magnitude;

        // Çok yavaşsa adım yok
        if (speed < minSpeedForFootsteps)
        {
            stepTimer = 0f;
            return;
        }

        // Sprint mi? (shift basılı + gerçek hareket var)
        bool hasMoveInput = (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.01f) || (Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.01f);
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && hasMoveInput;

        float interval = isSprinting ? sprintStepInterval : walkStepInterval;

        stepTimer -= Time.deltaTime;
        if (stepTimer <= 0f)
        {
            PlayFootstep();
            stepTimer = interval;
        }
    }

    private void PlayFootstep()
    {
        if (playerAudioSource == null || footstepClip == null) return;

        float oldPitch = playerAudioSource.pitch;
        playerAudioSource.pitch = Random.Range(footstepPitchRange.x, footstepPitchRange.y);
        playerAudioSource.PlayOneShot(footstepClip, footstepVolume);
        playerAudioSource.pitch = oldPitch;
    }

    private void PlayJumpSfx(AudioClip clip)
    {
        if (playerAudioSource == null || clip == null) return;

        float oldPitch = playerAudioSource.pitch;
        playerAudioSource.pitch = Random.Range(jumpPitchRange.x, jumpPitchRange.y);
        playerAudioSource.PlayOneShot(clip, jumpVolume);
        playerAudioSource.pitch = oldPitch;
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
        stepTimer = 0f;
    }
}
