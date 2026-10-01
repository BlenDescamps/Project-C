using UnityEngine;

namespace KJD.Game.PlayerController
{
    /// <summary>
    /// Composant à attacher sur n'importe quel objet (cube, poêle, légume) pour le rendre immédiatement manipulable et projetable.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class KitchenPropHoldable : MonoBehaviour, IHoldable
    {
        #region Publics

        public Transform Transform => transform;
        public Rigidbody Rigidbody => _rigidbody;
        public bool IsBeingHeld => _isBeingHeld;

        #endregion

        #region Unity API

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();
            _defaultDrag = _rigidbody.linearDamping;
            _defaultAngularDrag = _rigidbody.angularDamping;
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
            // L'interaction est gérée par PlayerInteraction.PickUpItem
        }

        public void OnPickedUp(Transform holdParent)
        {
            _isBeingHeld = true;
            _rigidbody.useGravity = false;
            _rigidbody.linearDamping = 8f;
            _rigidbody.angularDamping = 8f;

            if (_collider != null && holdParent != null)
            {
                CharacterController cc = holdParent.GetComponentInParent<CharacterController>();
                if (cc != null)
                {
                    Physics.IgnoreCollision(_collider, cc, true);
                }
            }
        }

        public void OnDropped()
        {
            _isBeingHeld = false;
            _rigidbody.useGravity = true;
            _rigidbody.linearDamping = _defaultDrag;
            _rigidbody.angularDamping = _defaultAngularDrag;
        }

        public void OnThrown(Vector3 force)
        {
            OnDropped();
            _rigidbody.linearVelocity = force;
            _rigidbody.AddTorque(Random.insideUnitSphere * 6f, ForceMode.Impulse);
        }

        public void OnRecalled(Transform holdParent, float duration)
        {
            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }

            transform.position = holdParent.position;
            transform.rotation = holdParent.rotation;

            OnPickedUp(holdParent);

            if (_apparitionEffect == null) _apparitionEffect = GetComponent<ApparitionEffect>();
            if (_apparitionEffect != null)
            {
                _apparitionEffect.PlayApparition(duration);
            }
        }

        #endregion

        #region Private and Protected

        [Header("--- PARAMÈTRES DU PROP ---")]
        [Tooltip("Nom ou action affichée à l'écran quand on regarde l'objet")]
        [SerializeField] private string _promptText = "Ramasser";

        private Rigidbody _rigidbody;
        private Collider _collider;
        private ApparitionEffect _apparitionEffect;
        private bool _isBeingHeld;
        private float _defaultDrag;
        private float _defaultAngularDrag;

        #endregion
    }
}
