using UnityEngine;

namespace KJD.Game.PlayerController
{
    /// <summary>
    /// Interface pour les objets portables (ingrédients, casseroles, assiettes, débris).
    /// </summary>
    public interface IHoldable : IInteractable
    {
        Transform Transform { get; }
        Rigidbody Rigidbody { get; }

        void OnPickedUp(Transform holdParent);
        void OnDropped();
        void OnThrown(Vector3 force);

        /// <summary>
        /// Phase 1 du rappel : l'objet se fige et se dissout au loin.
        /// </summary>
        void OnRecallStarted(float dissolveDuration);

        /// <summary>
        /// Phase 2 du rappel : l'objet est téléporté dans la main et se matérialise (apparition).
        /// </summary>
        void OnRecalled(Transform holdParent, float apparitionDuration);
    }
}
