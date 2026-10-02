using UnityEngine;

namespace KJD.Game.PlayerController
{
    public enum GrappleState
    {
        Ready,       // Dans le cylindre, prêt au tir
        Flying,      // En plein vol vers la cible avec câble déroulé
        Hooked,      // Accroché à la cible (tension et rembobinage actifs)
        Retracting   // En cours de retour vers le cylindre
    }

    /// <summary>
    /// Arbalète grappin prototypée sur un Cylindre.
    /// Équipable en main via la Hotbar. Clic gauche pour tirer/relâcher, Clic droit pour ré-enrouler la corde et tracter l'objet.
    /// Lors du tir, la tête du grappin est propulsée visuellement dans l'air avec la corde qui se déroule derrière elle.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(GrappleCable))]
    public class GrappleCrossbowHoldable : MonoBehaviour, IHoldable, ICustomActionHoldable
    {
        #region Publics

        public Transform Transform => transform;
        public Rigidbody Rigidbody => _rigidbody;
        public bool IsBeingHeld => _isBeingHeld;
        public bool IsHooked => _state == GrappleState.Hooked;
        public GrappleState State => _state;
        public float CurrentRopeLength => _currentRopeLength;

        // ICustomActionHoldable
        public bool OverridesPrimaryAction => true;
        public bool OverridesSecondaryAction => true;

        public string GetPrimaryActionPrompt()
        {
            switch (_state)
            {
                case GrappleState.Flying:
                    return "[Clic G] Annuler Tir";
                case GrappleState.Hooked:
                    return "[Clic G] Relâcher Grappin";
                case GrappleState.Retracting:
                    return "Rembobinage...";
                default:
                    return "[Clic G] Tirer Grappin";
            }
        }

        public string GetSecondaryActionPrompt()
        {
            return _state == GrappleState.Hooked ? "[Clic D (Maintenir)] Re-enrouler" : "[Clic D] Poser";
        }

        #endregion

        #region Unity API

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();
            _cable = GetComponent<GrappleCable>();
            _defaultScale = transform.localScale;

            EnsureMuzzlePoint();
            EnsureHookHead();
            DockHookHead();
            _cable.DisableCable();
        }

        private void OnEnable()
        {
            if (_hookHead != null)
            {
                _hookHead.gameObject.SetActive(true);
            }
        }

        private void Update()
        {
            if (_state == GrappleState.Flying)
            {
                UpdateFlyingState(Time.deltaTime);
            }
            else if (_state == GrappleState.Retracting)
            {
                UpdateRetractingState(Time.deltaTime);
            }
        }

        private void FixedUpdate()
        {
            if (_state != GrappleState.Hooked) return;

            // Si la cible accrochée a été détruite entre-temps
            if (_hookedTransform == null && _hookedRigidbody != null)
            {
                ReleaseGrapple();
                return;
            }

            UpdateTensionPhysics();
        }

        private void LateUpdate()
        {
            Vector3 muzzlePos = GetMuzzlePosition();
            Quaternion muzzleRot = GetMuzzleRotation();

            switch (_state)
            {
                case GrappleState.Ready:
                    if (_hookHead != null)
                    {
                        _hookHead.position = muzzlePos;
                        _hookHead.rotation = muzzleRot;
                        _hookHead.localScale = _hookHeadWorldScale;
                    }
                    break;

                case GrappleState.Flying:
                case GrappleState.Retracting:
                    if (_hookHead != null)
                    {
                        float dist = Vector3.Distance(muzzlePos, _hookHead.position);
                        _cable.UpdateCable(muzzlePos, _hookHead.position, dist, _state == GrappleState.Retracting);
                    }
                    break;

                case GrappleState.Hooked:
                    Vector3 anchorPos = GetCurrentAnchorPosition();
                    if (_hookHead != null)
                    {
                        _hookHead.position = anchorPos;
                        _hookHead.localScale = _hookHeadWorldScale;
                    }
                    _cable.UpdateCable(muzzlePos, anchorPos, _currentRopeLength, _isReeling);
                    _isReeling = false;
                    break;
            }
        }

        private void OnDisable()
        {
            ReleaseGrapple();
            DockHookHead();
            if (_hookHead != null)
            {
                _hookHead.gameObject.SetActive(false);
            }
            if (_cable != null)
            {
                _cable.DisableCable();
            }
        }

        private void OnDestroy()
        {
            if (_hookHead != null)
            {
                Destroy(_hookHead.gameObject);
            }
        }

        #endregion

        #region IInteractable & IHoldable API

        public string GetInteractionPrompt()
        {
            return _promptText;
        }

        public bool CanInteract(PlayerController player)
        {
            return !_isBeingHeld;
        }

        public void Interact(PlayerController player)
        {
            // Prise en charge par PlayerInteraction.PickUpItem
        }

        public void OnPickedUp(Transform holdParent)
        {
            _isBeingHeld = true;
            _playerRoot = holdParent != null ? holdParent.root : null;

            _rigidbody.useGravity = false;
            _rigidbody.isKinematic = true;

            if (_collider != null)
            {
                _collider.enabled = false;
            }

            // Positionnement en main (forme d'arbalète tournée vers l'avant)
            if (holdParent != null)
            {
                transform.SetParent(holdParent);
                transform.localPosition = _heldLocalPosition;
                transform.localRotation = Quaternion.Euler(_heldLocalRotation);
                transform.localScale = _heldScale;
            }

            DockHookHead();
        }

        public void OnDropped()
        {
            _isBeingHeld = false;
            _playerRoot = null;
            ReleaseGrapple();
            DockHookHead();

            transform.SetParent(null);
            transform.localScale = _defaultScale;

            _rigidbody.isKinematic = false;
            _rigidbody.useGravity = true;

            if (_collider != null)
            {
                _collider.enabled = true;
            }
        }

        public void OnThrown(Vector3 force)
        {
            OnDropped();
            _rigidbody.linearVelocity = force;
            _rigidbody.AddTorque(Random.insideUnitSphere * 4f, ForceMode.Impulse);
        }

        public void OnRecallStarted(float dissolveDuration)
        {
            ReleaseGrapple();
            DockHookHead();

            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.useGravity = false;

            if (_apparitionEffect == null) _apparitionEffect = GetComponent<ApparitionEffect>();
            if (_apparitionEffect != null)
            {
                _apparitionEffect.PlayDissolve(dissolveDuration);
            }
        }

        public void OnRecalled(Transform holdParent, float apparitionDuration)
        {
            OnPickedUp(holdParent);

            if (_apparitionEffect == null) _apparitionEffect = GetComponent<ApparitionEffect>();
            if (_apparitionEffect != null)
            {
                _apparitionEffect.PlayApparition(apparitionDuration);
            }
        }

        #endregion

        #region ICustomActionHoldable API

        public void OnPrimaryActionStarted()
        {
            if (_state == GrappleState.Ready)
            {
                FireGrapple();
            }
            else if (_state == GrappleState.Flying || _state == GrappleState.Hooked)
            {
                ReleaseGrapple();
            }
        }

        public void OnPrimaryActionHeld() { }
        public void OnPrimaryActionReleased() { }

        public void OnSecondaryActionStarted() { }

        public void OnSecondaryActionHeld()
        {
            if (_state == GrappleState.Hooked)
            {
                ReelIn();
            }
        }

        public void OnSecondaryActionReleased() { }

        #endregion

        #region Main API

        /// <summary>
        /// Lance la tête de harpon vers la cible visée avec déroulement de la corde.
        /// </summary>
        public void FireGrapple()
        {
            if (_state != GrappleState.Ready) return;

            Camera cam = Camera.main;
            if (cam == null) cam = GetComponentInParent<Camera>();
            if (cam == null) return;

            EnsureMuzzlePoint();
            EnsureHookHead();

            Vector3 muzzlePos = GetMuzzlePosition();

            // Point ciblé au centre du réticule (visée convergente FPS)
            Vector3 targetAimPoint = cam.transform.position + (cam.transform.forward * _maxRange);
            if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit camHit, _maxRange, _grappleMask, QueryTriggerInteraction.Ignore))
            {
                // Vérifie que le rayon de visée ne touche pas le joueur lui-même
                if (_playerRoot == null || camHit.collider.transform.root != _playerRoot)
                {
                    targetAimPoint = camHit.point;
                }
            }

            _flyDirection = (targetAimPoint - muzzlePos).normalized;
            if (_flyDirection.sqrMagnitude < 0.001f)
            {
                _flyDirection = cam.transform.forward;
            }

            _hookHead.position = muzzlePos;
            _hookHead.rotation = Quaternion.LookRotation(_flyDirection);
            _hookHead.localScale = _hookHeadWorldScale;
            _hookHead.gameObject.SetActive(true);

            _distanceTraveled = 0f;
            _state = GrappleState.Flying;

            _cable.EnableCable();
            Debug.Log($"<b>[GrappleCrossbow]</b> 🏹 Harpon décoché vers la cible à {_projectileSpeed:F0} m/s !");
        }

        /// <summary>
        /// Décroche le grappin et amorce le rembobinage automatique vers l'arbalète.
        /// </summary>
        public void ReleaseGrapple()
        {
            if (_state == GrappleState.Ready) return;

            if (_state == GrappleState.Hooked || _state == GrappleState.Flying)
            {
                StartRetracting();
            }
            else if (_state == GrappleState.Retracting)
            {
                DockHookHead();
            }
        }

        /// <summary>
        /// Raccourcit la corde pour tracter l'objet accroché vers le joueur.
        /// </summary>
        public void ReelIn()
        {
            if (_state != GrappleState.Hooked) return;

            _isReeling = true;
            _currentRopeLength = Mathf.Max(_minRopeLength, _currentRopeLength - (_reelSpeed * Time.deltaTime));

            // Si l'objet arrive tout près du joueur, on l'attire avec force
            if (_hookedRigidbody != null)
            {
                Vector3 toMuzzle = (GetMuzzlePosition() - GetCurrentAnchorPosition()).normalized;
                _hookedRigidbody.AddForce(toMuzzle * _reelPullForce, ForceMode.Acceleration);
            }
        }

        #endregion

        #region Tools and Utilities

        private void UpdateFlyingState(float deltaTime)
        {
            if (_hookHead == null) return;

            float step = _projectileSpeed * deltaTime;
            Vector3 currentPos = _hookHead.position;

            // Détection de collision volumique le long de la trajectoire
            RaycastHit[] hits = Physics.SphereCastAll(currentPos, _hookCollisionRadius, _flyDirection, step, _grappleMask, QueryTriggerInteraction.Ignore);

            RaycastHit validHit = default;
            bool foundHit = false;
            float closestDist = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit h = hits[i];
                if (h.collider == null) continue;
                if (h.collider.isTrigger) continue;
                if (h.collider.transform.root == transform.root) continue;
                if (h.collider.transform.root == _hookHead) continue;
                if (_playerRoot != null && h.collider.transform.root == _playerRoot) continue;

                if (h.distance < closestDist)
                {
                    closestDist = h.distance;
                    validHit = h;
                    foundHit = true;
                }
            }

            if (foundHit)
            {
                Vector3 hitPoint = validHit.point;
                if (hitPoint == Vector3.zero)
                {
                    hitPoint = currentPos + (_flyDirection * validHit.distance);
                }

                AttachHook(validHit.collider, hitPoint, validHit.normal);
                return;
            }

            // Déplacement du projectile
            _hookHead.position += _flyDirection * step;
            _distanceTraveled += step;

            if (_distanceTraveled >= _maxRange)
            {
                Debug.Log("<b>[GrappleCrossbow]</b> 💨 Portée max atteinte sans impact. Rembobinage automatique.");
                StartRetracting();
            }
        }

        private void AttachHook(Collider hitCollider, Vector3 hitPoint, Vector3 hitNormal)
        {
            _state = GrappleState.Hooked;
            _hookedRigidbody = hitCollider.GetComponentInParent<Rigidbody>();
            _hookedTransform = hitCollider.transform;
            _localAnchorOffset = _hookedTransform.InverseTransformPoint(hitPoint);
            _staticWorldAnchor = hitPoint;

            if (_hookHead != null)
            {
                _hookHead.position = hitPoint;
                if (hitNormal != Vector3.zero)
                {
                    _hookHead.rotation = Quaternion.LookRotation(-hitNormal);
                }
            }

            Vector3 muzzlePos = GetMuzzlePosition();
            _currentRopeLength = Vector3.Distance(muzzlePos, hitPoint);

            Debug.Log($"<b>[GrappleCrossbow]</b> 🎯 Harpon planté dans <b>{hitCollider.name}</b> (Distance : {_currentRopeLength:F1}m)");
        }

        private void StartRetracting()
        {
            _state = GrappleState.Retracting;
            _hookedRigidbody = null;
            _hookedTransform = null;
        }

        private void UpdateRetractingState(float deltaTime)
        {
            if (_hookHead == null)
            {
                DockHookHead();
                return;
            }

            Vector3 muzzlePos = GetMuzzlePosition();
            Vector3 toMuzzle = muzzlePos - _hookHead.position;
            float dist = toMuzzle.magnitude;
            float step = _retractSpeed * deltaTime;

            if (dist <= step || dist < 0.4f)
            {
                DockHookHead();
                Debug.Log("<b>[GrappleCrossbow]</b> 🔄 Grappin rembobiné et armé !");
            }
            else
            {
                _hookHead.position += toMuzzle.normalized * step;
                _hookHead.rotation = Quaternion.LookRotation(toMuzzle);
            }
        }

        private void DockHookHead()
        {
            _state = GrappleState.Ready;
            _hookedRigidbody = null;
            _hookedTransform = null;

            if (_cable != null)
            {
                _cable.DisableCable();
            }

            EnsureMuzzlePoint();
            EnsureHookHead();

            if (_hookHead != null)
            {
                _hookHead.position = GetMuzzlePosition();
                _hookHead.rotation = GetMuzzleRotation();
                _hookHead.localScale = _hookHeadWorldScale;
                _hookHead.gameObject.SetActive(true);
            }
        }

        private void EnsureMuzzlePoint()
        {
            if (_muzzlePoint == null)
            {
                Transform existing = transform.Find("Muzzle");
                if (existing != null)
                {
                    _muzzlePoint = existing;
                }
                else
                {
                    GameObject muzzle = new GameObject("Muzzle");
                    muzzle.transform.SetParent(transform);
                    muzzle.transform.localPosition = new Vector3(0f, 1f, 0f);
                    muzzle.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                    _muzzlePoint = muzzle.transform;
                }
            }
            else
            {
                _muzzlePoint.localPosition = new Vector3(0f, 1f, 0f);
                _muzzlePoint.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            }
        }

        private void EnsureHookHead()
        {
            if (_hookHead != null) return;

            // Recherche si déjà présent
            GameObject existing = GameObject.Find("GrappleHookHead_" + gameObject.GetInstanceID());
            if (existing != null)
            {
                _hookHead = existing.transform;
                return;
            }

            // Création d'une tête de harpon stylisée (modèle 3 barbillons)
            GameObject hookGO = new GameObject("GrappleHookHead_" + gameObject.GetInstanceID());
            hookGO.transform.position = GetMuzzlePosition();
            hookGO.transform.rotation = GetMuzzleRotation();
            hookGO.transform.localScale = _hookHeadWorldScale;

            // Matériau partagé
            Material hookMat = null;
            Renderer parentRenderer = GetComponent<Renderer>();
            if (parentRenderer != null) hookMat = parentRenderer.sharedMaterial;

            // 1. Tige centrale du harpon (Cylindre aligné sur Z)
            GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shaft.name = "Shaft";
            shaft.transform.SetParent(hookGO.transform);
            shaft.transform.localPosition = new Vector3(0f, 0f, 0.05f);
            shaft.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shaft.transform.localScale = new Vector3(0.04f, 0.06f, 0.04f);
            DestroyImmediate(shaft.GetComponent<Collider>());
            if (hookMat != null) shaft.GetComponent<Renderer>().sharedMaterial = hookMat;

            // 2. Pointe avant effilée (Sphère ogivale)
            GameObject tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tip.name = "Tip";
            tip.transform.SetParent(hookGO.transform);
            tip.transform.localPosition = new Vector3(0f, 0f, 0.12f);
            tip.transform.localRotation = Quaternion.identity;
            tip.transform.localScale = new Vector3(0.055f, 0.055f, 0.10f);
            DestroyImmediate(tip.GetComponent<Collider>());
            if (hookMat != null) tip.GetComponent<Renderer>().sharedMaterial = hookMat;

            // 3. Trois barbillons / griffes orientés vers l'arrière
            for (int i = 0; i < 3; i++)
            {
                GameObject barb = GameObject.CreatePrimitive(PrimitiveType.Cube);
                barb.name = $"Barb_{i}";
                barb.transform.SetParent(hookGO.transform);

                float rotZ = i * 120f;
                Quaternion barbRot = Quaternion.Euler(0f, 0f, rotZ) * Quaternion.Euler(25f, 0f, 0f);
                Vector3 barbOffset = Quaternion.Euler(0f, 0f, rotZ) * new Vector3(0f, 0.03f, 0.04f);

                barb.transform.localPosition = barbOffset;
                barb.transform.localRotation = barbRot;
                barb.transform.localScale = new Vector3(0.015f, 0.02f, 0.06f);
                DestroyImmediate(barb.GetComponent<Collider>());
                if (hookMat != null) barb.GetComponent<Renderer>().sharedMaterial = hookMat;
            }

            _hookHead = hookGO.transform;
        }

        public Vector3 GetMuzzlePosition()
        {
            if (_muzzlePoint != null) return _muzzlePoint.position;
            return transform.position + (transform.up * (transform.lossyScale.y * 0.5f));
        }

        public Quaternion GetMuzzleRotation()
        {
            if (_muzzlePoint != null) return _muzzlePoint.rotation;
            return Quaternion.LookRotation(transform.up, -transform.forward);
        }

        public Vector3 GetCurrentAnchorPosition()
        {
            if (_hookedTransform != null)
            {
                return _hookedTransform.TransformPoint(_localAnchorOffset);
            }
            return _staticWorldAnchor;
        }

        private void UpdateTensionPhysics()
        {
            Vector3 muzzlePos = GetMuzzlePosition();
            Vector3 anchorPos = GetCurrentAnchorPosition();
            Vector3 toMuzzle = muzzlePos - anchorPos;
            float currentDist = toMuzzle.magnitude;

            // Si la distance dépasse la longueur de corde déployée : TENSION PHYSIQUE
            if (currentDist > _currentRopeLength)
            {
                float excessDistance = currentDist - _currentRopeLength;
                Vector3 pullDir = toMuzzle.normalized;

                // Si on a accroché un Rigidbody (notre Cube !) : on le tire vers nous
                if (_hookedRigidbody != null && !_hookedRigidbody.isKinematic)
                {
                    float tensionForce = (excessDistance * _tensionStiffness) + (_isReeling ? _reelPullForce : 0f);
                    _hookedRigidbody.AddForce(pullDir * tensionForce, ForceMode.Acceleration);

                    // Petit coup de sustentation vers le haut pour éviter qu'il s'enfonce dans le sol
                    if (anchorPos.y < muzzlePos.y - 0.2f)
                    {
                        _hookedRigidbody.AddForce(Vector3.up * 4.5f, ForceMode.Acceleration);
                    }
                }
            }
        }

        #endregion

        #region Private and Protected

        [Header("--- PARAMÈTRES DU GRAPPIN ---")]
        [Tooltip("Portée maximale du tir en mètres")]
        [Range(10f, 100f)]
        [SerializeField] private float _maxRange = 45f;

        [Tooltip("Vitesse de vol de la tête du grappin vers la cible en m/s")]
        [Range(15f, 120f)]
        [SerializeField] private float _projectileSpeed = 50f;

        [Tooltip("Vitesse de rembobinage automatique en m/s (en cas d'échec ou d'annulation)")]
        [Range(20f, 150f)]
        [SerializeField] private float _retractSpeed = 75f;

        [Tooltip("Rayon de détection de collision de la tête de harpon")]
        [Range(0.02f, 0.3f)]
        [SerializeField] private float _hookCollisionRadius = 0.07f;

        [Tooltip("Vitesse de ré-enroulement de la corde en m/s")]
        [Range(2f, 30f)]
        [SerializeField] private float _reelSpeed = 12f;

        [Tooltip("Force d'attraction lors du ré-enroulement")]
        [Range(5f, 60f)]
        [SerializeField] private float _reelPullForce = 28f;

        [Tooltip("Raideur de la corde quand elle est tendue à l'extrême")]
        [Range(10f, 100f)]
        [SerializeField] private float _tensionStiffness = 45f;

        [Tooltip("Longueur minimale de la corde rembobinée")]
        [SerializeField] private float _minRopeLength = 0.8f;

        [Tooltip("Layers pouvant être accrochés")]
        [SerializeField] private LayerMask _grappleMask = ~0;

        [Header("--- APPARENCE EN MAIN (Vue FPS) ---")]
        [SerializeField] private Vector3 _heldLocalPosition = new Vector3(0.28f, -0.22f, 0.5f);
        [SerializeField] private Vector3 _heldLocalRotation = new Vector3(90f, 0f, 0f);
        [SerializeField] private Vector3 _heldScale = new Vector3(0.09f, 0.35f, 0.09f);
        [SerializeField] private Vector3 _hookHeadWorldScale = Vector3.one;
        [SerializeField] private string _promptText = "Ramasser Arbalète Grappin";

        [SerializeField] private Transform _muzzlePoint;
        [SerializeField] private Transform _hookHead;

        private Rigidbody _rigidbody;
        private Collider _collider;
        private GrappleCable _cable;
        private ApparitionEffect _apparitionEffect;
        private Transform _playerRoot;

        private bool _isBeingHeld;
        private bool _isReeling;
        private float _currentRopeLength;
        private float _distanceTraveled;
        private Vector3 _flyDirection;

        private GrappleState _state = GrappleState.Ready;
        private Rigidbody _hookedRigidbody;
        private Transform _hookedTransform;
        private Vector3 _localAnchorOffset;
        private Vector3 _staticWorldAnchor;
        private Vector3 _defaultScale;

        #endregion
    }
}
