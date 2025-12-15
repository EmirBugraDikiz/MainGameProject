using UnityEngine;
using UnityEngine.Rendering; // Volume efekti için
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem; // Yeni Input Sistemi kütüphanesi
#endif

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class FirstPersonController : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 4.0f;
        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 6.0f;
        [Tooltip("Rotation speed of the character")]
        public float RotationSpeed = 1.0f;
        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;
        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.1f;
        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;
        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;
        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.5f;
        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;
        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 90.0f;
        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -90.0f;

        // --- ETKİLEŞİM AYARLARI ---
        [Header("Interaction (Kapı vb.)")]
        public float InteractionDistance = 3.0f; 
        public LayerMask InteractionLayer; 
        // ---------------------------

        // cinemachine
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // OZEL YETENEKLER
        [Header("Player Abilities")]
        public bool CanDoubleJump = false; 
        private bool _doubleJumpUsed = false; 
        private float _jumpTimeStamp = 0.0f;
        
        [Header("Side Effects")]
        public Volume BlackWhiteVolume; 
        
        [Header("Time Ability")]
        public bool CanTimeSlow = false; 
        private bool _isTimeSlowed = false;
        private bool _reverseControls = false; 

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
#endif
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;

        private bool IsCurrentDeviceMouse
        {
            get
            {
                #if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
                #else
                return false;
                #endif
            }
        }

        private void Awake()
        {
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        private void Start()
        {
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
#else
            Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        private void Update()
        {
            JumpAndGravity();
            GroundedCheck();
            Move();
            
            // Yeni input sistemine göre düzenlenmiş fonksiyonlar:
            HandleTimeSlow(); 
            HandleInteraction(); 
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        private void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
        }

        private void CameraRotation()
        {
            if (_input.look.sqrMagnitude >= _threshold)
            {
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
                
                _cinemachineTargetPitch += _input.look.y * RotationSpeed * deltaTimeMultiplier;
                _rotationVelocity = _input.look.x * RotationSpeed * deltaTimeMultiplier;

                _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

                CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);

                transform.Rotate(Vector3.up * _rotationVelocity);
            }
        }

        private void Move()
        {
            Vector2 currentInput = _input.move;

            if (_reverseControls)
            {
                currentInput = -currentInput;
            }

            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

            if (currentInput == Vector2.zero) targetSpeed = 0.0f;

            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? currentInput.magnitude : 1f;

            if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            Vector3 inputDirection = new Vector3(currentInput.x, 0.0f, currentInput.y).normalized;

            if (currentInput != Vector2.zero)
            {
                inputDirection = transform.right * currentInput.x + transform.forward * currentInput.y;
            }

            _controller.Move(inputDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                _fallTimeoutDelta = FallTimeout;
                _doubleJumpUsed = false;

                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                    _jumpTimeStamp = Time.time;
                }

                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else 
            {
                bool timeIsOkey = (Time.time - _jumpTimeStamp) > 0.25f;

                if (CanDoubleJump && !_doubleJumpUsed && _input.jump && timeIsOkey)
                {
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                    _doubleJumpUsed = true;
                    _input.jump = false;

                    Debug.Log("Havada Zıpladım! (Siyah-Beyaz Geliyor)");
                    StartCoroutine(TriggerSideEffect());
                }

                _jumpTimeoutDelta = JumpTimeout;

                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }

                if (!_doubleJumpUsed) _input.jump = false;
            }

            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        // --- DÜZELTİLEN: ZAMAN YAVAŞLATMA (YENİ INPUT SİSTEMİ) ---
        private void HandleTimeSlow()
        {
            // Eski "Input.GetKeyDown(KeyCode.Q)" yerine:
            if (CanTimeSlow && Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
            {
                _isTimeSlowed = !_isTimeSlowed;

                if (_isTimeSlowed)
                {
                    Time.timeScale = 0.2f;
                    Time.fixedDeltaTime = 0.02f * Time.timeScale;
                    _reverseControls = true;
                    Debug.Log("Mod Açık: Zaman Yavaş, Kontroller TERS!");
                }
                else
                {
                    Time.timeScale = 1.0f;
                    Time.fixedDeltaTime = 0.02f;
                    _reverseControls = false;
                    Debug.Log("Mod Kapalı: Her şey normal.");
                }
            }
        }
        private void HandleInteraction()
        {
            // E tuşuna basılıp basılmadığını kontrol et
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) 
            {
                //Debug.Log("1. E Tuşuna Basıldı! Işın gönderiliyor..."); // Tuşa basınca kesinlikle bu çıkmalı

                Ray ray = new Ray(_mainCamera.transform.position, _mainCamera.transform.forward);
                RaycastHit hit;

                // Işını çizelim (Scene ekranında kırmızı çizgi görmek için)
                //Debug.DrawRay(_mainCamera.transform.position, _mainCamera.transform.forward * InteractionDistance, Color.red, 2f);

                // Işın atıyoruz
                if (Physics.Raycast(ray, out hit, InteractionDistance, InteractionLayer))
                {
                    //Debug.Log("2. Işın bir şeye çarptı: " + hit.collider.name); // Çarptığı objenin adını yazar

                    kapi kapiScripti = hit.collider.GetComponentInParent<kapi>();

                    if (kapiScripti != null)
                    {
                        //Debug.Log("3. Kapı scripti bulundu! Açılıyor.");
                        kapiScripti.KapiyiAcKapat();
                    }
                    else
                    {
                        //Debug.LogWarning("Işın çarptı ama 'kapi' scripti bulamadı. Yanlış objeye mi bakıyorsun?");
                    }
                }
                else
                {
                    // İşte burası eksikti! Işın boşa giderse bunu yazacak.
                    //Debug.LogError("HATA: Işın hiçbir şeye çarpmadı! Interaction Layer ayarını veya Mesafeyi kontrol et.");
                }
            }
        }
        // ---------------------------------------------------

        System.Collections.IEnumerator TriggerSideEffect()
        {
            if (BlackWhiteVolume != null)
            {
                BlackWhiteVolume.weight = 1f;
                yield return new WaitForSeconds(1f);
                BlackWhiteVolume.weight = 0f;
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
        }
    }
}