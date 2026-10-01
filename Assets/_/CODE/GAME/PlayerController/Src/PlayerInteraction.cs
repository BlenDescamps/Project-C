using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KJD.Game.PlayerController
{
    [RequireComponent(typeof(PlayerController))]
    public class PlayerInteraction : MonoBehaviour
    {
        #region Publics

        public IInteractable CurrentTarget => _currentTarget;
        public IHoldable CurrentHeldItem => _heldItem;
        public bool IsHoldingItem => _heldItem != null;
        public bool IsChargingThrow => _isChargingThrow;
        public float CurrentChargeRatio => _chargeRatio;
        public bool IsRecalling => _isRecalling;
        public PlayerHotbar Hotbar => _hotbar;

        public void OnItemStashedByHotbar(IHoldable item)
        {
            if (_heldItem == item)
            {
                CancelThrowCharge();
                _heldItem = null;
                if (item != null && item.Transform != null)
                {
                    item.Transform.gameObject.SetActive(false);
                }
            }
        }

        public void OnItemUnstashedByHotbar(IHoldable item)
        {
            if (item != null && item.Transform != null)
            {
                item.Transform.gameObject.SetActive(true);
                _heldItem = item;
                _heldItem.OnPickedUp(_holdPoint);
            }
        }

        #endregion

        #region Unity API

        private void Awake()
        {
            _playerController = GetComponent<PlayerController>();
            _hotbar = GetComponent<PlayerHotbar>();
            EnsureCameraReference();
            EnsureHoldPoint();
            ResolveInputBindings();
        }

        private void Start()
        {
            EnsureCameraReference();
            EnsureHoldPoint();
        }

        private void OnEnable()
        {
            EnableInputActions();
        }

        private void OnDisable()
        {
            DisableInputActions();
            CancelThrowCharge();
            if (_recallCoroutine != null)
            {
                StopCoroutine(_recallCoroutine);
                _recallCoroutine = null;
                _isRecalling = false;
            }
        }

        private void Update()
        {
            PerformRaycast();
            HandleThrowCharging();
            UpdateHeldItemPosition();
        }

        private void OnGUI()
        {
            if (!_showDebugCrosshair) return;

            float centerX = Screen.width * 0.5f;
            float centerY = Screen.height * 0.5f;

            Color oldColor = GUI.color;
            GUI.color = _currentTarget != null ? Color.green : new Color(1f, 1f, 1f, 0.4f);

            // Réticule central
            GUI.Box(new Rect(centerX - 3, centerY - 3, 6, 6), GUIContent.none);

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };

            // 1. Jauge de charge du lancer
            if (_isChargingThrow && _heldItem != null)
            {
                float barWidth = 140f;
                float barHeight = 10f;
                float barX = centerX - (barWidth * 0.5f);
                float barY = centerY + 30f;

                // Fond de la barre
                GUI.color = new Color(0f, 0f, 0f, 0.6f);
                GUI.Box(new Rect(barX - 2, barY - 2, barWidth + 4, barHeight + 4), GUIContent.none);

                // Remplissage progressif (Jaune -> Orange -> Rouge)
                Color chargeColor = Color.Lerp(Color.yellow, Color.red, _chargeRatio);
                GUI.color = chargeColor;
                GUI.Box(new Rect(barX, barY, barWidth * _chargeRatio, barHeight), GUIContent.none);

                // Texte de puissance
                labelStyle.fontSize = 11;
                labelStyle.normal.textColor = chargeColor;
                string chargeText = Mathf.RoundToInt(_chargeRatio * 100f) + "%";
                GUI.Label(new Rect(centerX - 50, barY + 12, 100, 20), chargeText, labelStyle);
            }
            // 2. Prompt d'interaction quand on vise un objet
            else if (_currentTarget != null)
            {
                labelStyle.fontSize = 14;
                labelStyle.normal.textColor = Color.white;
                string prompt = _currentTarget.GetInteractionPrompt();
                GUI.Label(new Rect(centerX - 150, centerY + 18, 300, 30), "[E / X] " + prompt, labelStyle);
            }
            // 3. Indication quand on porte un objet
            else if (_heldItem != null)
            {
                labelStyle.fontSize = 12;
                labelStyle.normal.textColor = new Color(1f, 0.9f, 0.4f, 0.9f);
                GUI.Label(new Rect(centerX - 200, centerY + 18, 400, 30), "[Maintenir Clic G / RT] Charger Lancer  •  [Clic D / LT] Poser", labelStyle);
            }

            // 4. Indication de rappel si les mains sont libres
            if (_isRecalling)
            {
                labelStyle.fontSize = 12;
                labelStyle.normal.textColor = new Color(0.3f, 0.85f, 1f, 1f);
                GUI.Label(new Rect(centerX - 150, centerY + 45, 300, 20), "⚡ Rappel magique en cours...", labelStyle);
            }
            else if (_heldItem == null && _lastInteractedItem != null)
            {
                labelStyle.fontSize = 11;
                labelStyle.normal.textColor = new Color(0.4f, 0.9f, 1f, 0.85f);
                GUI.Label(new Rect(centerX - 150, centerY + 45, 300, 20), "[Q / LB] Rappeler l'objet", labelStyle);
            }

            GUI.color = oldColor;
        }

        private void OnDrawGizmosSelected()
        {
            if (_cameraTransform != null)
            {
                Gizmos.color = _currentTarget != null ? Color.green : Color.yellow;
                Gizmos.DrawRay(_cameraTransform.position, _cameraTransform.forward * _reachDistance);
                if (_holdPoint != null)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireSphere(_holdPoint.position, 0.15f);
                }
            }
        }

        #endregion

        #region Main API

        private void PerformRaycast()
        {
            EnsureCameraReference();
            if (_cameraTransform == null) return;

            Ray ray = new Ray(_cameraTransform.position, _cameraTransform.forward);
            RaycastHit[] hits;

            if (_useSphereCast)
            {
                hits = Physics.SphereCastAll(ray, _sphereCastRadius, _reachDistance, _interactionMask, QueryTriggerInteraction.Ignore);
            }
            else
            {
                hits = Physics.RaycastAll(ray, _reachDistance, _interactionMask, QueryTriggerInteraction.Ignore);
            }

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                if (hit.collider.transform.root == transform.root) continue;

                IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable != null && interactable.CanInteract(_playerController))
                {
                    _currentTarget = interactable;
                    return;
                }
            }

            _currentTarget = null;
        }

        private void HandleThrowCharging()
        {
            if (_heldItem == null || _isRecalling)
            {
                CancelThrowCharge();
                return;
            }

            bool isThrowPressed = _activeThrowAction != null && _activeThrowAction.IsPressed();

            if (isThrowPressed)
            {
                _isChargingThrow = true;
                float speed = _chargeTime > 0.01f ? (1f / _chargeTime) : 10f;
                _chargeRatio = Mathf.Clamp01(_chargeRatio + Time.deltaTime * speed);
            }
            else if (_isChargingThrow)
            {
                ExecuteChargedThrow();
            }
        }

        private void UpdateHeldItemPosition()
        {
            if (_heldItem == null || _holdPoint == null) return;

            Vector3 targetPos = _holdPoint.position;

            // Secousse de l'objet pendant la charge
            if (_isChargingThrow && _chargeRatio > 0.05f)
            {
                float shakeMagnitude = _maxShakeAmplitude * (_shakeIntensityPercentage / 100f) * _chargeRatio;
                Vector3 randomJitter = UnityEngine.Random.insideUnitSphere * shakeMagnitude;
                targetPos += randomJitter;
            }

            Rigidbody rb = _heldItem.Rigidbody;
            if (rb != null)
            {
                Vector3 toTarget = targetPos - rb.position;
                rb.linearVelocity = toTarget * _holdFollowSpeed;

                Quaternion targetRot = _holdPoint.rotation;
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, Time.deltaTime * _holdFollowSpeed));
            }
            else
            {
                _heldItem.Transform.position = targetPos;
                _heldItem.Transform.rotation = _holdPoint.rotation;
            }
        }

        public void TryInteract()
        {
            if (_isRecalling) return;

            if (_heldItem != null && _currentTarget != null)
            {
                _currentTarget.Interact(_playerController);
                return;
            }

            if (_heldItem == null && _currentTarget is IHoldable holdable)
            {
                PickUpItem(holdable);
                return;
            }

            if (_currentTarget != null)
            {
                _currentTarget.Interact(_playerController);
            }
            else if (_heldItem != null)
            {
                DropHeldItem();
            }
        }

        public void PickUpItem(IHoldable item)
        {
            if (item == null) return;

            CancelThrowCharge();
            _heldItem = item;
            _lastInteractedItem = item;
            _heldItem.OnPickedUp(_holdPoint);

            if (_hotbar != null)
            {
                _hotbar.AssignItemToActiveOrFirstSlot(item);
            }

            Debug.Log($"<b>[PlayerInteraction]</b> Objet ramassé et équipé en Hotbar : {item.Transform.name}");
        }

        public void DropHeldItem()
        {
            if (_heldItem == null || _isRecalling) return;

            CancelThrowCharge();

            IHoldable itemToDrop = _heldItem;
            _lastInteractedItem = itemToDrop;
            _heldItem = null;

            itemToDrop.OnDropped();

            if (_hotbar != null)
            {
                _hotbar.NotifyItemDropped(itemToDrop);
            }

            if (itemToDrop.Rigidbody != null)
            {
                Vector3 dropVelocity = (_cameraTransform != null ? _cameraTransform.forward : transform.forward) * _dropForwardForce;
                itemToDrop.Rigidbody.linearVelocity = dropVelocity;
            }

            Debug.Log($"<b>[PlayerInteraction]</b> Objet posé : {itemToDrop.Transform.name}");
        }

        private void ExecuteChargedThrow()
        {
            if (_heldItem == null) return;

            float calculatedForce = Mathf.Lerp(_minThrowForce, _maxThrowForce, _chargeRatio);

            IHoldable itemToThrow = _heldItem;
            _lastInteractedItem = itemToThrow;
            _heldItem = null;
            CancelThrowCharge();

            if (_hotbar != null)
            {
                _hotbar.NotifyItemThrown(itemToThrow);
            }

            Vector3 throwDirection = _cameraTransform != null ? _cameraTransform.forward : transform.forward;
            Vector3 finalThrowVelocity = (throwDirection * calculatedForce) + (_playerController.Velocity * _playerMomentumInheritance);

            itemToThrow.OnThrown(finalThrowVelocity);
            Debug.Log($"<b>[PlayerInteraction]</b> Lancer exécuté à {Mathf.RoundToInt(_chargeRatio * 100f)}% de force ({calculatedForce:F1} m/s) !");
        }

        /// <summary>
        /// Rappelle l'objet : dissolution magique au loin puis réapparition progressive dans la main (Touche Q / LB).
        /// </summary>
        public void TryRecallItem()
        {
            if (_heldItem != null || _isRecalling) return;

            IHoldable itemToRecall = null;

            // 1. Privilégie l'objet lié au slot actif de la Hotbar
            if (_hotbar != null)
            {
                itemToRecall = _hotbar.GetRecallableItemForActiveSlot();
            }

            // 2. Fallbacks
            if (itemToRecall == null)
            {
                itemToRecall = _lastInteractedItem;
            }

            if (itemToRecall == null)
            {
                itemToRecall = FindClosestHoldable();
            }

            if (itemToRecall != null)
            {
                if (_recallCoroutine != null) StopCoroutine(_recallCoroutine);
                _recallCoroutine = StartCoroutine(RecallSequenceRoutine(itemToRecall));
            }
        }

        private IEnumerator RecallSequenceRoutine(IHoldable itemToRecall)
        {
            _isRecalling = true;
            _lastInteractedItem = itemToRecall;

            Debug.Log($"<b>[PlayerInteraction]</b> ⚡ Dissolution au loin de : {itemToRecall.Transform.name} ({_recallDissolveDuration:F2}s)...");
            itemToRecall.OnRecallStarted(_recallDissolveDuration);

            float elapsed = 0f;
            while (elapsed < _recallDissolveDuration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Sécurité si l'objet ou holdPoint a disparu
            if (itemToRecall == null || itemToRecall.Transform == null || _holdPoint == null)
            {
                _isRecalling = false;
                _recallCoroutine = null;
                yield break;
            }

            Debug.Log($"<b>[PlayerInteraction]</b> ✨ Matérialisation en main de : {itemToRecall.Transform.name} ({_recallApparitionDuration:F2}s) !");
            _heldItem = itemToRecall;
            _heldItem.OnRecalled(_holdPoint, _recallApparitionDuration);

            if (_hotbar != null)
            {
                _hotbar.NotifyItemRecalled(itemToRecall);
            }

            elapsed = 0f;
            while (elapsed < _recallApparitionDuration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            _isRecalling = false;
            _recallCoroutine = null;
        }

        private IHoldable FindClosestHoldable()
        {
            KitchenPropHoldable[] props = UnityEngine.Object.FindObjectsByType<KitchenPropHoldable>(FindObjectsSortMode.None);
            IHoldable closest = null;
            float minDistance = float.MaxValue;
            Vector3 playerPos = transform.position;

            foreach (var prop in props)
            {
                if (prop.IsBeingHeld) continue;
                float d = Vector3.Distance(playerPos, prop.transform.position);
                if (d < minDistance)
                {
                    minDistance = d;
                    closest = prop;
                }
            }

            return closest;
        }

        private void CancelThrowCharge()
        {
            _isChargingThrow = false;
            _chargeRatio = 0f;
        }

        #endregion

        #region Tools and Utilities

        private void EnsureCameraReference()
        {
            if (_cameraTransform == null)
            {
                if (_playerController != null && _playerController.CameraTransform != null)
                {
                    _cameraTransform = _playerController.CameraTransform;
                }
                else
                {
                    Camera cam = GetComponentInChildren<Camera>();
                    if (cam != null) _cameraTransform = cam.transform;
                    else if (Camera.main != null) _cameraTransform = Camera.main.transform;
                }
            }
        }

        private void EnsureHoldPoint()
        {
            if (_holdPoint == null)
            {
                Transform parent = _cameraTransform != null ? _cameraTransform : transform;
                Transform existing = parent.Find("HoldPoint");
                if (existing != null)
                {
                    _holdPoint = existing;
                }
                else
                {
                    GameObject holdObj = new GameObject("HoldPoint");
                    holdObj.transform.SetParent(parent);
                    holdObj.transform.localPosition = new Vector3(0.3f, -0.3f, 0.85f);
                    holdObj.transform.localRotation = Quaternion.identity;
                    _holdPoint = holdObj.transform;
                }
            }
        }

        private void ResolveInputBindings()
        {
            _activeInteractAction = _interactAction != null ? _interactAction.action : null;
            _activeThrowAction = _throwAction != null ? _throwAction.action : null;
            _activeDropAction = _dropAction != null ? _dropAction.action : null;
            _activeRecallAction = _recallAction != null ? _recallAction.action : null;

            if (TryGetComponent<PlayerInput>(out var playerInput) && playerInput.actions != null)
            {
                var playerMap = playerInput.actions.FindActionMap("Player");
                if (playerMap != null)
                {
                    if (_activeInteractAction == null) _activeInteractAction = playerMap.FindAction("Interact");
                    if (_activeThrowAction == null) _activeThrowAction = playerMap.FindAction("Attack");
                    if (_activeDropAction == null) _activeDropAction = playerMap.FindAction("Crouch");
                    if (_activeRecallAction == null) _activeRecallAction = playerMap.FindAction("Recall");
                }
            }
        }

        private void EnableInputActions()
        {
            if (_activeInteractAction == null) ResolveInputBindings();

            if (_activeInteractAction != null)
            {
                _activeInteractAction.Enable();
                _activeInteractAction.performed += OnInteractTriggered;
                _activeInteractAction.started += OnInteractTriggered;
            }

            if (_activeThrowAction != null)
            {
                _activeThrowAction.Enable();
            }

            if (_activeDropAction != null)
            {
                _activeDropAction.Enable();
                _activeDropAction.performed += OnDropTriggered;
            }

            if (_activeRecallAction != null)
            {
                _activeRecallAction.Enable();
                _activeRecallAction.performed += OnRecallTriggered;
                _activeRecallAction.started += OnRecallTriggered;
            }
        }

        private void DisableInputActions()
        {
            if (_activeInteractAction != null)
            {
                _activeInteractAction.performed -= OnInteractTriggered;
                _activeInteractAction.started -= OnInteractTriggered;
                _activeInteractAction.Disable();
            }

            if (_activeThrowAction != null)
            {
                _activeThrowAction.Disable();
            }

            if (_activeDropAction != null)
            {
                _activeDropAction.performed -= OnDropTriggered;
                _activeDropAction.Disable();
            }

            if (_activeRecallAction != null)
            {
                _activeRecallAction.performed -= OnRecallTriggered;
                _activeRecallAction.started -= OnRecallTriggered;
                _activeRecallAction.Disable();
            }
        }

        private void OnInteractTriggered(InputAction.CallbackContext context)
        {
            if (Time.frameCount == _lastInteractFrame) return;
            _lastInteractFrame = Time.frameCount;

            TryInteract();
        }

        private void OnDropTriggered(InputAction.CallbackContext context)
        {
            DropHeldItem();
        }

        private void OnRecallTriggered(InputAction.CallbackContext context)
        {
            if (Time.frameCount == _lastRecallFrame) return;
            _lastRecallFrame = Time.frameCount;

            TryRecallItem();
        }

        #endregion

        #region Private and Protected

        [Header("--- INPUTS (New Input System) ---")]
        [Tooltip("Action d'interaction (E / Bouton X manette)")]
        [SerializeField] private InputActionReference _interactAction;

        [Tooltip("Action de lancer d'objet (Maintenir Clic Gauche / Gâchette Droite RT)")]
        [SerializeField] private InputActionReference _throwAction;

        [Tooltip("Action de dépose douce (Clic Droit / Gâchette Gauche LT)")]
        [SerializeField] private InputActionReference _dropAction;

        [Tooltip("Action de rappel de l'objet (Q au clavier / LB à la manette)")]
        [SerializeField] private InputActionReference _recallAction;

        [Header("--- DÉTECTION & PORTÉE ---")]
        [Tooltip("Distance maximale d'interaction en mètres")]
        [Range(1f, 5f)]
        [SerializeField] private float _reachDistance = 2.8f;

        [Tooltip("Utiliser un SphereCast pour une visée plus tolérante sur les petits ingrédients")]
        [SerializeField] private bool _useSphereCast = true;

        [Tooltip("Rayon du SphereCast")]
        [Range(0.05f, 0.4f)]
        [SerializeField] private float _sphereCastRadius = 0.18f;

        [Tooltip("Layers d'interaction (recommandé : Everything / ~0)")]
        [SerializeField] private LayerMask _interactionMask = ~0;

        [Header("--- TRANSPORT DE L'OBJET ---")]
        [Tooltip("Point d'ancrage de l'objet porté (enfant de la caméra)")]
        [SerializeField] private Transform _holdPoint;

        [Tooltip("Vitesse de suivi de l'objet porté (lissage physique)")]
        [Range(5f, 50f)]
        [SerializeField] private float _holdFollowSpeed = 25f;

        [Tooltip("Force lors d'une dépose douce (Clic Droit / LT)")]
        [Range(0.5f, 5f)]
        [SerializeField] private float _dropForwardForce = 2f;

        [Tooltip("Pourcentage de la vitesse du joueur transmise à l'objet lancé")]
        [Range(0f, 1f)]
        [SerializeField] private float _playerMomentumInheritance = 0.8f;

        [Header("--- CHARGE DU LANCER (Game Feel) ---")]
        [Tooltip("Force minimale lors d'un lancer instantané (simple tap)")]
        [Range(2f, 20f)]
        [SerializeField] private float _minThrowForce = 7f;

        [Tooltip("Force maximale lors d'un lancer pleinement chargé")]
        [Range(10f, 50f)]
        [SerializeField] private float _maxThrowForce = 28f;

        [Tooltip("Temps en secondes pour atteindre la force maximale de charge")]
        [Range(0.2f, 3f)]
        [SerializeField] private float _chargeTime = 0.9f;

        [Tooltip("Pourcentage d'intensité de la secousse de l'objet pendant la charge (0% = fixe, 100% = tremblement maximal)")]
        [Range(0f, 100f)]
        [SerializeField] private float _shakeIntensityPercentage = 60f;

        [Tooltip("Amplitude physique maximale de la secousse en mètres")]
        [Range(0.01f, 0.1f)]
        [SerializeField] private float _maxShakeAmplitude = 0.045f;

        [Header("--- RAPPEL D'OBJET (Recall / Apparition) ---")]
        [Tooltip("Durée de la dissolution de l'objet au loin avant son rappel")]
        [Range(0.1f, 2f)]
        [SerializeField] private float _recallDissolveDuration = 0.5f;

        [Tooltip("Durée de la réapparition (matérialisation) de l'objet dans la main")]
        [Range(0.2f, 3f)]
        [SerializeField] private float _recallApparitionDuration = 0.85f;

        [Header("--- DEBUG & RETOUR VISUEL ---")]
        [Tooltip("Affiche un réticule au centre, la jauge de charge et le texte d'aide à l'écran")]
        [SerializeField] private bool _showDebugCrosshair = true;

        // Références internes
        private PlayerController _playerController;
        private PlayerHotbar _hotbar;
        private Transform _cameraTransform;
        private IInteractable _currentTarget;
        private IHoldable _heldItem;
        private IHoldable _lastInteractedItem;
        private int _lastInteractFrame = -1;
        private int _lastRecallFrame = -1;
        private bool _isRecalling;
        private Coroutine _recallCoroutine;

        // Variables de charge
        private bool _isChargingThrow;
        private float _chargeRatio;

        private InputAction _activeInteractAction;
        private InputAction _activeThrowAction;
        private InputAction _activeDropAction;
        private InputAction _activeRecallAction;

        #endregion
    }
}
