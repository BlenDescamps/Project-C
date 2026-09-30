using UnityEngine;
using UnityEngine.InputSystem;

namespace KJD.Game.PlayerController
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        #region Publics

        public bool IsGrounded => _characterController.isGrounded;
        public Vector3 Velocity => _characterController.velocity;
        public Transform CameraTransform => _cameraTransform;
        public bool IsSprinting => _isSprinting;

        #endregion

        #region Unity API

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();

            if (_cameraTransform == null)
            {
                Camera cam = GetComponentInChildren<Camera>();
                if (cam != null) _cameraTransform = cam.transform;
            }

            ResolveInputBindings();
            RecalculateJumpPhysics();
        }

        private void OnEnable()
        {
            EnableInputActions();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDisable()
        {
            DisableInputActions();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            HandleCameraLook();
            HandleGroundCheckAndTimers();
            HandleHorizontalMovement();
            HandleVerticalMovement();

            ApplyFinalMovement();
        }

        private void OnValidate()
        {
            RecalculateJumpPhysics();
        }

        #endregion

        #region Main API

        private void HandleCameraLook()
        {
            if (_cameraTransform == null) return;

            Vector2 lookInput = _activeLookAction != null ? _activeLookAction.ReadValue<Vector2>() : Vector2.zero;

            // Détecte si la visée provient d'une manette ou d'une souris
            bool isGamepad = _activeLookAction != null && _activeLookAction.activeControl != null &&
                             _activeLookAction.activeControl.device is Gamepad;

            float sensitivityX = isGamepad ? _gamepadSensitivityX : _mouseSensitivityX;
            float sensitivityY = isGamepad ? _gamepadSensitivityY : _mouseSensitivityY;

            float deltaMultiplier = isGamepad ? Time.deltaTime : 1f;

            float yaw = lookInput.x * sensitivityX * deltaMultiplier;
            float pitch = lookInput.y * sensitivityY * deltaMultiplier;

            if (_invertY) pitch = -pitch;

            transform.Rotate(Vector3.up * yaw);

            _cameraPitch = Mathf.Clamp(_cameraPitch - pitch, _minPitch, _maxPitch);
            _cameraTransform.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
        }

        private void HandleGroundCheckAndTimers()
        {
            if (_characterController.isGrounded)
            {
                _coyoteTimeCounter = _coyoteTime;
                if (_verticalVelocity < 0f)
                {
                    _verticalVelocity = _groundStickForce;
                }
            }
            else
            {
                _coyoteTimeCounter -= Time.deltaTime;
            }

            _jumpBufferCounter -= Time.deltaTime;
        }

        private void HandleHorizontalMovement()
        {
            Vector2 rawInput = _activeMoveAction != null ? _activeMoveAction.ReadValue<Vector2>() : Vector2.zero;
            Vector3 inputDirection = (transform.right * rawInput.x + transform.forward * rawInput.y).normalized;

            _isSprinting = _activeSprintAction != null && _activeSprintAction.IsPressed() && _characterController.isGrounded;
            float targetSpeed = _isSprinting ? _sprintSpeed : _walkSpeed;

            if (rawInput.sqrMagnitude < 0.01f)
            {
                targetSpeed = 0f;
            }

            Vector3 targetVelocity = inputDirection * targetSpeed;
            float accelRate = (rawInput.sqrMagnitude > 0.01f) ? _acceleration : _deceleration;

            if (!_characterController.isGrounded)
            {
                accelRate *= _airControl;
            }

            _currentHorizontalVelocity = Vector3.MoveTowards(_currentHorizontalVelocity, targetVelocity, accelRate * Time.deltaTime);
        }

        private void HandleVerticalMovement()
        {
            // Déclenchement du saut
            if (_jumpBufferCounter > 0f && _coyoteTimeCounter > 0f)
            {
                _verticalVelocity = _initialJumpVelocity;
                _jumpBufferCounter = 0f;
                _coyoteTimeCounter = 0f;
            }

            // Gravité modulable (Air time & descente incisive)
            float currentGravity = _gravity;

            if (_verticalVelocity < 0f)
            {
                // Descente plus rapide pour un saut qui a du punch
                currentGravity *= _fallGravityMultiplier;
            }
            else if (_activeJumpAction != null && !_activeJumpAction.IsPressed() && _verticalVelocity > 0f)
            {
                // Saut écourté si on relâche le bouton avant l'apex
                currentGravity *= _jumpCutMultiplier;
            }

            _verticalVelocity -= currentGravity * Time.deltaTime;
        }

        private void ApplyFinalMovement()
        {
            Vector3 finalMotion = _currentHorizontalVelocity;
            finalMotion.y = _verticalVelocity;

            _characterController.Move(finalMotion * Time.deltaTime);
        }

        #endregion

        #region Tools and Utilities

        private void ResolveInputBindings()
        {
            _activeMoveAction = _moveAction != null ? _moveAction.action : null;
            _activeLookAction = _lookAction != null ? _lookAction.action : null;
            _activeJumpAction = _jumpAction != null ? _jumpAction.action : null;
            _activeSprintAction = _sprintAction != null ? _sprintAction.action : null;

            // Fallback automatique si un PlayerInput est attaché
            if (TryGetComponent<PlayerInput>(out var playerInput) && playerInput.actions != null)
            {
                var playerMap = playerInput.actions.FindActionMap("Player");
                if (playerMap != null)
                {
                    if (_activeMoveAction == null) _activeMoveAction = playerMap.FindAction("Move");
                    if (_activeLookAction == null) _activeLookAction = playerMap.FindAction("Look");
                    if (_activeJumpAction == null) _activeJumpAction = playerMap.FindAction("Jump");
                    if (_activeSprintAction == null) _activeSprintAction = playerMap.FindAction("Sprint");
                }
            }
        }

        private void RecalculateJumpPhysics()
        {
            // Formules classiques de game feel (Celeste / Mario / Apex) :
            // Permet de régler directement la HAUTEUR et le TEMPS pour atteindre le sommet (Air Time).
            if (_timeToJumpApex <= 0.01f) _timeToJumpApex = 0.01f;

            _gravity = (2f * _jumpHeight) / (_timeToJumpApex * _timeToJumpApex);
            _initialJumpVelocity = _gravity * _timeToJumpApex;
        }

        private void EnableInputActions()
        {
            if (_activeMoveAction == null) ResolveInputBindings();

            if (_activeMoveAction != null) _activeMoveAction.Enable();
            if (_activeLookAction != null) _activeLookAction.Enable();
            if (_activeJumpAction != null)
            {
                _activeJumpAction.Enable();
                _activeJumpAction.performed += OnJumpPerformed;
            }
            if (_activeSprintAction != null) _activeSprintAction.Enable();
        }

        private void DisableInputActions()
        {
            if (_activeMoveAction != null) _activeMoveAction.Disable();
            if (_activeLookAction != null) _activeLookAction.Disable();
            if (_activeJumpAction != null)
            {
                _activeJumpAction.performed -= OnJumpPerformed;
                _activeJumpAction.Disable();
            }
            if (_activeSprintAction != null) _activeSprintAction.Disable();
        }

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            _jumpBufferCounter = _jumpBufferTime;
        }

        #endregion

        #region Private and Protected

        [Header("--- INPUT ACTIONS (New Input System) ---")]
        [Tooltip("Action de déplacement 2D (WASD / Stick Gauche)")]
        [SerializeField] private InputActionReference _moveAction;
        [Tooltip("Action de regard 2D (Souris / Stick Droit)")]
        [SerializeField] private InputActionReference _lookAction;
        [Tooltip("Action de saut (Espace / Bouton Sud - A / Croix)")]
        [SerializeField] private InputActionReference _jumpAction;
        [Tooltip("Action de sprint (Shift Gauche / Clic Stick Gauche)")]
        [SerializeField] private InputActionReference _sprintAction;

        [Header("--- DÉPLACEMENTS HORIZONTAUX ---")]
        [Tooltip("Vitesse de marche standard en mètres par seconde")]
        [Range(1f, 15f)]
        [SerializeField] private float _walkSpeed = 5.5f;

        [Tooltip("Vitesse de sprint en mètres par seconde")]
        [Range(2f, 25f)]
        [SerializeField] private float _sprintSpeed = 9f;

        [Tooltip("Vitesse d'accélération au sol")]
        [Range(5f, 50f)]
        [SerializeField] private float _acceleration = 25f;

        [Tooltip("Vitesse de freinage au sol (décélération nette)")]
        [Range(5f, 50f)]
        [SerializeField] private float _deceleration = 30f;

        [Tooltip("Contrôle directionnel dans les airs (0 = aucune influence, 1 = comme au sol)")]
        [Range(0f, 1f)]
        [SerializeField] private float _airControl = 0.5f;

        [Header("--- SAUT & AIR TIME (Game Feel) ---")]
        [Tooltip("Hauteur maximale du saut en mètres")]
        [Range(0.5f, 5f)]
        [SerializeField] private float _jumpHeight = 1.6f;

        [Tooltip("Temps en secondes pour atteindre le sommet du saut (Air Time)")]
        [Range(0.15f, 1f)]
        [SerializeField] private float _timeToJumpApex = 0.35f;

        [Tooltip("Multiplicateur de gravité lors de la retombée (évite les sauts flottants)")]
        [Range(1f, 3f)]
        [SerializeField] private float _fallGravityMultiplier = 1.8f;

        [Tooltip("Multiplicateur de gravité quand on relâche le saut plus tôt (saut court)")]
        [Range(1f, 3f)]
        [SerializeField] private float _jumpCutMultiplier = 2.0f;

        [Tooltip("Tolérance de saut après avoir quitté une plateforme (Coyote Time)")]
        [Range(0f, 0.3f)]
        [SerializeField] private float _coyoteTime = 0.15f;

        [Tooltip("Mémorisation de l'appui sur saut avant de toucher le sol (Jump Buffer)")]
        [Range(0f, 0.3f)]
        [SerializeField] private float _jumpBufferTime = 0.15f;

        [Tooltip("Force d'adhérence au sol lors des descentes de pentes")]
        [SerializeField] private float _groundStickForce = -2f;

        [Header("--- CAMÉRA & SENSIBILITÉ ---")]
        [Tooltip("Transform de la caméra attachée au joueur")]
        [SerializeField] private Transform _cameraTransform;

        [Tooltip("Sensibilité horizontale et verticale de la souris")]
        [Range(0.01f, 1f)]
        [SerializeField] private float _mouseSensitivityX = 0.12f;
        [Range(0.01f, 1f)]
        [SerializeField] private float _mouseSensitivityY = 0.12f;

        [Tooltip("Sensibilité de la manette (en degrés par seconde)")]
        [Range(20f, 300f)]
        [SerializeField] private float _gamepadSensitivityX = 140f;
        [Range(20f, 300f)]
        [SerializeField] private float _gamepadSensitivityY = 120f;

        [Tooltip("Angle vertical minimum (vers le bas)")]
        [Range(-90f, 0f)]
        [SerializeField] private float _minPitch = -85f;

        [Tooltip("Angle vertical maximum (vers le haut)")]
        [Range(0f, 90f)]
        [SerializeField] private float _maxPitch = 85f;

        [Tooltip("Inverser l'axe Y de la caméra")]
        [SerializeField] private bool _invertY = false;

        // Variables internes
        private CharacterController _characterController;
        private Vector3 _currentHorizontalVelocity;
        private float _verticalVelocity;
        private float _cameraPitch;
        private float _gravity;
        private float _initialJumpVelocity;
        private float _coyoteTimeCounter;
        private float _jumpBufferCounter;
        private bool _isSprinting;

        private InputAction _activeMoveAction;
        private InputAction _activeLookAction;
        private InputAction _activeJumpAction;
        private InputAction _activeSprintAction;

        #endregion
    }
}
