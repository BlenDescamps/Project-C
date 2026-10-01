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
        void OnRecalled(Transform holdParent, float duration);
    }
}
