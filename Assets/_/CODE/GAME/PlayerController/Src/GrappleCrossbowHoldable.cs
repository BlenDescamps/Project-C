using UnityEngine;

namespace KJD.Game.PlayerController
{
    /// <summary>
    /// Arbalète grappin prototypée sur un Cylindre.
    /// Équipable en main via la Hotbar. Clic gauche pour tirer/relâcher, Clic droit pour ré-enrouler la corde et tracter l'objet.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(GrappleCable))]
    public class GrappleCrossbowHoldable : MonoBehaviour, IHoldable, ICustomActionHoldable
    {
        #region Publics

        public Transform Transform => transform;
        public Rigidbody Rigidbody => _rigidbody;
        public bool IsBeingHeld => _isBeingHeld;
        public bool IsHooked => _isHooked;
        public float CurrentRopeLength => _currentRopeLength;

        // ICustomActionHoldable
        public bool OverridesPrimaryAction => true;
        public bool OverridesSecondaryAction => true;

        public string GetPrimaryActionPrompt()
        {
            return _isHooked ? "[Clic G] Relâcher Grappin" : "[Clic G] Tirer Grappin";
        }

        public string GetSecondaryActionPrompt()
        {
            return _isHooked ? "[Clic D (Maintenir)] Re-enrouler" : "[Clic D] Poser";
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
            _cable.DisableCable();
        }

        private void FixedUpdate()
        {
            if (!_isHooked) return;

            UpdateTensionPhysics();
        }

        private void LateUpdate()
        {
            if (!_isHooked) return;

            Vector3 muzzlePos = GetMuzzlePosition();
            Vector3 anchorPos = GetCurrentAnchorPosition();

            _cable.UpdateCable(muzzlePos, anchorPos, _currentRopeLength, _isReeling);
            _isReeling = false;
        }

        private void OnDisable()
        {
            ReleaseGrapple();
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
        }

        public void OnDropped()
        {
            _isBeingHeld = false;
            ReleaseGrapple();

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
            if (!_isHooked)
            {
                FireGrapple();
            }
            else
            {
                ReleaseGrapple();
            }
        }

        public void OnPrimaryActionHeld() { }
        public void OnPrimaryActionReleased() { }

        public void OnSecondaryActionStarted() { }

        public void OnSecondaryActionHeld()
        {
            if (_isHooked)
            {
                ReelIn();
            }
        }

        public void OnSecondaryActionReleased() { }

        #endregion

        #region Main API

        /// <summary>
        /// Tire le grappin vers l'avant de la caméra.
        /// </summary>
        public void FireGrapple()
        {
            Camera cam = Camera.main;
            if (cam == null) cam = GetComponentInParent<Camera>();
            if (cam == null) return;

            Ray ray = new Ray(cam.transform.position, cam.transform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, _maxRange, _grappleMask, QueryTriggerInteraction.Ignore))
            {
                // Évite de s'accrocher à soi-même
                if (hit.collider.transform.root == transform.root) return;

                _isHooked = true;
                _hookedRigidbody = hit.collider.GetComponentInParent<Rigidbody>();
                _hookedTransform = hit.collider.transform;
                _localAnchorOffset = _hookedTransform.InverseTransformPoint(hit.point);
                _staticWorldAnchor = hit.point;

                Vector3 muzzlePos = GetMuzzlePosition();
                _currentRopeLength = Vector3.Distance(muzzlePos, hit.point);

                _cable.EnableCable();
                Debug.Log($"<b>[GrappleCrossbow]</b> 🎯 Harpon accroché sur <b>{hit.collider.name}</b> (Distance : {_currentRopeLength:F1}m)");
            }
            else
            {
                Debug.Log("<b>[GrappleCrossbow]</b> Tir dans le vide (hors de portée).");
            }
        }

        /// <summary>
        /// Décroche le grappin et rentre la corde.
        /// </summary>
        public void ReleaseGrapple()
        {
            if (!_isHooked) return;

            _isHooked = false;
            _hookedRigidbody = null;
            _hookedTransform = null;
            _cable.DisableCable();
            Debug.Log("<b>[GrappleCrossbow]</b> ⚡ Grappin décroché !");
        }

        /// <summary>
        /// Raccourcit la corde pour tracter l'objet accroché vers le joueur.
        /// </summary>
        public void ReelIn()
        {
            if (!_isHooked) return;

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
                    // Dans un cylindre Unity standard (haut de 2m selon Y), l'extrémité est à y = +1f
                    muzzle.transform.localPosition = new Vector3(0f, 1f, 0f);
                    muzzle.transform.localRotation = Quaternion.identity;
                    _muzzlePoint = muzzle.transform;
                }
            }
        }

        public Vector3 GetMuzzlePosition()
        {
            if (_muzzlePoint != null) return _muzzlePoint.position;
            return transform.position + transform.forward * 0.5f;
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

                // 1. Si on a accroché un Rigidbody (notre Cube !) : on le tire vers nous
                if (_hookedRigidbody != null && !_hookedRigidbody.isKinematic)
                {
                    // Force proportionnelle à l'étirement + correction de vélocité
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
        [SerializeField] private float _maxRange = 50f;

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
        [SerializeField] private Vector3 _heldLocalRotation = new Vector3(90f, 0f, 0f); // 90° X oriente le cylindre vers l'avant !
        [SerializeField] private Vector3 _heldScale = new Vector3(0.09f, 0.35f, 0.09f);
        [SerializeField] private string _promptText = "Ramasser Arbalète Grappin";

        [SerializeField] private Transform _muzzlePoint;

        private Rigidbody _rigidbody;
        private Collider _collider;
        private GrappleCable _cable;
        private ApparitionEffect _apparitionEffect;

        private bool _isBeingHeld;
        private bool _isHooked;
        private bool _isReeling;
        private float _currentRopeLength;

        private Rigidbody _hookedRigidbody;
        private Transform _hookedTransform;
        private Vector3 _localAnchorOffset;
        private Vector3 _staticWorldAnchor;
        private Vector3 _defaultScale;

        #endregion
    }
}
