using UnityEngine;
using UnityEngine.Rendering; // Volume sistemini kullanmak için şart
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
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

		// cinemachine
		private float _cinemachineTargetPitch;

		// player
		private float _speed;
		private float _rotationVelocity;
		private float _verticalVelocity;
		private float _terminalVelocity = 53.0f;

        // OZEL YETENEKLER
        [Header("Player Abilities")]
        public bool CanDoubleJump = false; // Iksiri ictik mi?
        private bool _doubleJumpUsed = false; // Havadayken hakkinizi kullandik mi?
        private float _jumpTimeStamp = 0.0f;
        [Header("Side Effects")]
        public Volume BlackWhiteVolume; // Unity'den surukleyecegimiz efekt
        [Header("Time Ability")]
        public bool CanTimeSlow = false; // Hatanın sebebi bu satırın eksik olmasıydı
        private bool _isTimeSlowed = false;
        private bool _reverseControls = false; // Kontroller şu an ters mi?

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
			// get a reference to our main camera
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

			// reset our timeouts on start
			_jumpTimeoutDelta = JumpTimeout;
			_fallTimeoutDelta = FallTimeout;
		}

		private void Update()
		{
			JumpAndGravity();
			GroundedCheck();
			Move();
            HandleTimeSlow();
        }

		private void LateUpdate()
		{
			CameraRotation();
		}

		private void GroundedCheck()
		{
			// set sphere position, with offset
			Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
			Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
		}

		private void CameraRotation()
		{
			// if there is an input
			if (_input.look.sqrMagnitude >= _threshold)
			{
				//Don't multiply mouse input by Time.deltaTime
				float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
				
				_cinemachineTargetPitch += _input.look.y * RotationSpeed * deltaTimeMultiplier;
				_rotationVelocity = _input.look.x * RotationSpeed * deltaTimeMultiplier;

				// clamp our pitch rotation
				_cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

				// Update Cinemachine camera target pitch
				CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);

				// rotate the player left and right
				transform.Rotate(Vector3.up * _rotationVelocity);
			}
		}

        private void Move()
        {
            // --- ARAS İÇİN DÜZELTME: GİRDİYİ TERS ÇEVİRME ---
            // _input.move değerini doğrudan kullanmak yerine geçici bir değişkene alıyoruz.
            Vector2 currentInput = _input.move;

            // Eğer yan etki aktifse (Zaman normale döndüyse), girdiyi negatife çevir (Ters Yön)
            if (_reverseControls)
            {
                currentInput = -currentInput;
            }
            // -------------------------------------------------

            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

            // a simplistic acceleration and deceleration handling
            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            // BURADA ARTIK _input.move YERİNE currentInput KULLANIYORUZ
            if (currentInput == Vector2.zero) targetSpeed = 0.0f;

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? currentInput.magnitude : 1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            // normalise input direction
            Vector3 inputDirection = new Vector3(currentInput.x, 0.0f, currentInput.y).normalized;

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (currentInput != Vector2.zero)
            {
                // move
                inputDirection = transform.right * currentInput.x + transform.forward * currentInput.y;
            }

            // move the player
            _controller.Move(inputDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                // Düşme zamanlayıcısını sıfırla
                _fallTimeoutDelta = FallTimeout;

                // Yere basınca çift zıplama hakkını yenile
                _doubleJumpUsed = false;

                // Yerçekimi sıfırlama
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // Zıplama (Yerdeyken)
                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                    _jumpTimeStamp = Time.time; // Zaman damgası
                }

                // Zıplama bekleme süresi
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else // HAVADAYSAK
            {
                // --- ARAS DOUBLE JUMP MANTIGI ---
                bool timeIsOkey = (Time.time - _jumpTimeStamp) > 0.25f;

                if (CanDoubleJump && !_doubleJumpUsed && _input.jump && timeIsOkey)
                {
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity); // Fırlat
                    _doubleJumpUsed = true; // Hakkı ye
                    _input.jump = false; // Tuşu kapat

                    Debug.Log("Havada Zıpladım! (Siyah-Beyaz Geliyor)");

                    // Yan Etkiyi Başlat (Bu fonksiyon aşağıda tanımlı)
                    StartCoroutine(TriggerSideEffect());
                }
                // --------------------------------

                _jumpTimeoutDelta = JumpTimeout;

                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }

                if (!_doubleJumpUsed) _input.jump = false;
            }

            // Yerçekimi uygula
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        // --- AŞAĞIDAKİ FONKSİYONLAR EKSİKTİ, GERİ GELDİ ---

        // 1. ZAMAN YAVAŞLATMA (Mavi İksir)
        private void HandleTimeSlow()
        {
            if (CanTimeSlow && Input.GetKeyDown(KeyCode.Q))
            {
                _isTimeSlowed = !_isTimeSlowed;

                if (_isTimeSlowed)
                {
                    // Mod Açık: Zaman Yavaş, Kontroller TERS
                    Time.timeScale = 0.2f;
                    Time.fixedDeltaTime = 0.02f * Time.timeScale;

                    _reverseControls = true;

                    Debug.Log("Mod Açık: Zaman Yavaş, Kontroller TERS!");
                }
                else
                {
                    // Mod Kapalı: Her şey normal
                    Time.timeScale = 1.0f;
                    Time.fixedDeltaTime = 0.02f;

                    _reverseControls = false;

                    Debug.Log("Mod Kapalı: Her şey normal.");
                }
            }
        }

        // 2. SİYAH-BEYAZ EKRAN (Kırmızı İksir Yan Etkisi)
        // Bu silindiği için hata alıyordun, geri ekledik.
        System.Collections.IEnumerator TriggerSideEffect()
        {
            if (BlackWhiteVolume != null)
            {
                BlackWhiteVolume.weight = 1f; // Efekti aç
                yield return new WaitForSeconds(1f); // 1 saniye bekle
                BlackWhiteVolume.weight = 0f; // Efekti kapat
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

			// when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
			Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
		}
        
    }
}