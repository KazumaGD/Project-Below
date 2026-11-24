using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectBelow.Exploration
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController), typeof(PlayerInput))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        private const float GroundedVerticalSpeed = -2f;
        private const float PitchLimit = 85f;

        [SerializeField] private Transform _viewPivot;
        [SerializeField, Min(0f)] private float _moveSpeed = 4f;
        [SerializeField, Min(0f)] private float _mouseSensitivity = 0.12f;
        [SerializeField] private bool _movementEnabled = true;
        [SerializeField] private bool _lookEnabled = true;

        private CharacterController _characterController;
        private InputAction _moveAction;
        private InputAction _lookAction;
        private float _pitch;
        private float _verticalSpeed;
        private bool _ownsCursor;
        private bool _cursorCaptureEnabled = true;

        public Transform ViewPivot => _viewPivot;

        public bool MovementEnabled
        {
            get => _movementEnabled;
            set => _movementEnabled = value;
        }

        public bool LookEnabled
        {
            get => _lookEnabled;
            set => _lookEnabled = value;
        }

        public bool CursorCaptureEnabled
        {
            get => _cursorCaptureEnabled;
            set
            {
                if (_cursorCaptureEnabled == value)
                    return;

                _cursorCaptureEnabled = value;
                if (value && isActiveAndEnabled)
                    CaptureCursor();
                else
                    ReleaseCursor();
            }
        }

        public void TurnViewTowards(Vector3 target, float degreesThisFrame)
        {
            Vector3 direction = target - _viewPivot.position;
            if (direction.sqrMagnitude < 0.0001f)
                return;

            Vector3 horizontal = Vector3.ProjectOnPlane(direction, Vector3.up);
            if (horizontal.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(horizontal), degreesThisFrame);

            float targetPitch = -Mathf.Atan2(direction.y, horizontal.magnitude) * Mathf.Rad2Deg;
            _pitch = Mathf.MoveTowards(_pitch, Mathf.Clamp(targetPitch, -PitchLimit, PitchLimit), degreesThisFrame);
            _viewPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void Start()
        {
            if (_viewPivot == null || _viewPivot == transform || !_viewPivot.IsChildOf(transform))
            {
                Debug.LogError("Assign a child transform as the first-person view pivot.", this);
                enabled = false;
                return;
            }

            _characterController = GetComponent<CharacterController>();
            var playerInput = GetComponent<PlayerInput>();
            var playerActions = playerInput.actions?.FindActionMap("Player");
            _moveAction = playerActions?.FindAction("Move");
            _lookAction = playerActions?.FindAction("Look");

            if (_moveAction == null || _lookAction == null)
            {
                Debug.LogError("PlayerInput requires the Player action map with Move and Look actions.", this);
                enabled = false;
                return;
            }

            _pitch = Mathf.Clamp(Mathf.DeltaAngle(0f, _viewPivot.localEulerAngles.x), -PitchLimit, PitchLimit);
            CaptureCursor();
        }

        private void OnEnable()
        {
            if (_lookAction != null)
                CaptureCursor();
        }

        private void OnDisable()
        {
            ReleaseCursor();
        }

        private void Update()
        {
            bool hasCapturedCursor = _cursorCaptureEnabled && Application.isFocused && Cursor.lockState == CursorLockMode.Locked;
            if (_cursorCaptureEnabled && !hasCapturedCursor && Application.isFocused &&
                Mouse.current?.leftButton.wasPressedThisFrame == true)
                CaptureCursor();

            if (hasCapturedCursor && _lookEnabled)
                ApplyLook(_lookAction.ReadValue<Vector2>());

            Vector2 moveInput = hasCapturedCursor && _movementEnabled
                ? _moveAction.ReadValue<Vector2>()
                : Vector2.zero;
            ApplyMovement(moveInput, Time.deltaTime);
        }

        private void ApplyLook(Vector2 mouseDelta)
        {
            transform.Rotate(Vector3.up, mouseDelta.x * _mouseSensitivity, Space.Self);
            _pitch = Mathf.Clamp(_pitch - mouseDelta.y * _mouseSensitivity, -PitchLimit, PitchLimit);
            _viewPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void ApplyMovement(Vector2 moveInput, float deltaTime)
        {
            moveInput = Vector2.ClampMagnitude(moveInput, 1f);
            Vector3 velocity = (transform.right * moveInput.x + transform.forward * moveInput.y) * _moveSpeed;

            _verticalSpeed = _characterController.isGrounded
                ? GroundedVerticalSpeed
                : _verticalSpeed + Physics.gravity.y * deltaTime;
            velocity.y = _verticalSpeed;

            _characterController.Move(velocity * deltaTime);
        }

        private void CaptureCursor()
        {
            if (!_cursorCaptureEnabled || !Application.isFocused)
                return;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _ownsCursor = true;
        }

        private void ReleaseCursor()
        {
            if (!_ownsCursor)
                return;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _ownsCursor = false;
        }
    }
}
