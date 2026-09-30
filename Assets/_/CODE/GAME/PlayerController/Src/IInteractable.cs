namespace KJD.Game.PlayerController
{
    /// <summary>
    /// Interface générique pour tous les éléments interactifs (portes, fourneaux, postes de découpe, etc.).
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Texte d'aide affiché au joueur (ex: "Prendre la Poêle", "Ouvrir le Four").
        /// </summary>
        string GetInteractionPrompt();

        /// <summary>
        /// Déclenche l'action quand le joueur appuie sur la touche Interagir (E / Bouton X / Carré).
        /// </summary>
        void Interact(PlayerController player);

        /// <summary>
        /// Vérifie si l'interaction est possible à cet instant.
        /// </summary>
        bool CanInteract(PlayerController player);
    }
}
